using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using FluxAI.Api.Domain;
using FluxAI.Api.Persistence;
using FluxAI.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FluxAI.Api.Api;

internal static partial class RunEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly Regex Sha256Pattern = Sha256Regex();
    private static readonly Regex MetricNamePattern = MetricNameRegex();

    public static void Map(WebApplication app)
    {
        app.MapPost("/api/v1/run-imports", ImportRunAsync)
            .RequireAuthorization(FluxAiAuthentication.WorkerPolicy);
        app.MapGet("/api/v1/runs", ListRunsAsync)
            .RequireAuthorization(FluxAiAuthentication.InternalReadPolicy);
        app.MapGet("/api/v1/runs/{id:guid}", GetRunAsync)
            .RequireAuthorization(FluxAiAuthentication.InternalReadPolicy);
        app.MapGet("/api/v1/runs/{id:guid}/metrics", ListMetricsAsync)
            .RequireAuthorization(FluxAiAuthentication.InternalReadPolicy);
        app.MapGet("/api/v1/runs/{id:guid}/signal-results", ListSignalResultsAsync)
            .RequireAuthorization(FluxAiAuthentication.InternalReadPolicy);
    }

    private static async Task<IResult> ImportRunAsync(
        RunImportRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        HttpContext http,
        FluxAiDbContext db,
        CancellationToken cancellationToken)
    {
        if (!IsValidIdempotencyKey(idempotencyKey))
            return Problem(StatusCodes.Status400BadRequest, "Idempotency-Key inválida", "Informe uma chave de 16 a 128 caracteres: letras, números, ponto, hífen, sublinhado ou til.");

        var scenario = await db.Scenarios.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == request.ScenarioId, cancellationToken);
        if (scenario is null || !scenario.IsEnabled)
            return Problem(StatusCodes.Status404NotFound, "Cenário não encontrado", "A importação deve referenciar um cenário habilitado no catálogo da API.");

        var controllerIds = await db.SignalControllers.AsNoTracking()
            .Where(x => x.ScenarioId == scenario.Id)
            .Select(x => x.ControllerId)
            .ToHashSetAsync(cancellationToken);
        var errors = Validate(request, scenario, controllerIds);
        if (errors.Count > 0)
            return Results.ValidationProblem(errors, statusCode: StatusCodes.Status422UnprocessableEntity);

        var payloadHash = HashCanonicalRequest(request);
        var existingByKey = await db.TrainingRuns.AsNoTracking()
            .SingleOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);
        if (existingByKey is not null)
            return await ExistingImportResultAsync(existingByKey, request, payloadHash, http, db, cancellationToken);

        var existingBySource = await db.TrainingRuns.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ScenarioId == request.ScenarioId && x.SourceRunId == request.SourceRunId, cancellationToken);
        if (existingBySource is not null)
            return await ExistingImportResultAsync(existingBySource, request, payloadHash, http, db, cancellationToken);

        var createdAtUtc = DateTimeOffset.UtcNow;
        var run = new TrainingRun
        {
            Id = Guid.NewGuid(),
            SourceRunId = request.SourceRunId.Trim(),
            ScenarioId = request.ScenarioId,
            RunType = request.RunType,
            Origin = "imported",
            Environment = request.Environment,
            Status = "succeeded",
            Seed = request.Seed,
            HorizonSeconds = request.HorizonSeconds,
            ModelJson = request.Model is null ? null : JsonSerializer.Serialize(request.Model, JsonOptions),
            EffectiveParametersJson = request.EffectiveParameters is { ValueKind: JsonValueKind.Object } parameters
                ? JsonSerializer.Serialize(parameters, JsonOptions)
                : "{}",
            TrafficSummaryJson = JsonSerializer.Serialize(request.TrafficSummary, JsonOptions),
            MetricCoverageJson = request.Coverage is null ? null : JsonSerializer.Serialize(request.Coverage, JsonOptions),
            ScenarioFingerprintJson = JsonSerializer.Serialize(request.ScenarioFingerprint, JsonOptions),
            LimitationsJson = JsonSerializer.Serialize(request.Limitations, JsonOptions),
            IdempotencyKey = idempotencyKey!,
            PayloadSha256 = payloadHash,
            CreatedAtUtc = createdAtUtc,
            CreatedAtOrderKey = createdAtUtc.ToUnixTimeMilliseconds(),
            StartedAtUtc = request.StartedAtUtc.ToUniversalTime(),
            FinishedAtUtc = request.FinishedAtUtc.ToUniversalTime()
        };

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.TrainingRuns.Add(run);
        db.RunMetrics.AddRange(request.Metrics.Select(metric => new RunMetric
        {
            Id = Guid.NewGuid(),
            TrainingRunId = run.Id,
            MetricName = metric.MetricName,
            Value = metric.Value,
            Unit = metric.Unit,
            ScopeType = metric.Scope.Type,
            SignalControllerId = metric.Scope.SignalControllerId,
            Episode = metric.Episode,
            Step = metric.Step,
            SimulationTimeSeconds = metric.SimulationTimeSeconds,
            SourceKey = metric.SourceKey
        }));
        db.RunArtifacts.AddRange(request.Artifacts.Select(artifact => new RunArtifact
        {
            Id = Guid.NewGuid(),
            TrainingRunId = run.Id,
            LogicalName = artifact.LogicalName,
            Sha256 = artifact.Sha256.ToLowerInvariant(),
            SizeBytes = artifact.SizeBytes,
            ContentType = artifact.ContentType
        }));

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            db.ChangeTracker.Clear();
            var concurrent = await db.TrainingRuns.AsNoTracking().SingleOrDefaultAsync(
                x => x.IdempotencyKey == idempotencyKey || (x.ScenarioId == request.ScenarioId && x.SourceRunId == request.SourceRunId),
                cancellationToken);
            if (concurrent is null)
                throw;
            return await ExistingImportResultAsync(concurrent, request, payloadHash, http, db, cancellationToken);
        }

        http.Response.Headers.CacheControl = "no-store";
        http.Response.Headers.Location = $"/api/v1/runs/{run.Id}";
        return Results.Created($"/api/v1/runs/{run.Id}", await ToResponseAsync(run, db, cancellationToken));
    }

    private static async Task<IResult> ExistingImportResultAsync(
        TrainingRun existing,
        RunImportRequest request,
        string payloadHash,
        HttpContext http,
        FluxAiDbContext db,
        CancellationToken cancellationToken)
    {
        var sameIdentity = existing.ScenarioId == request.ScenarioId && existing.SourceRunId == request.SourceRunId.Trim();
        if (!sameIdentity || !CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(existing.PayloadSha256), Convert.FromHexString(payloadHash)))
            return Problem(StatusCodes.Status409Conflict, "Conflito de importação", "A chave de idempotência ou a identidade da execução já está associada a outros dados.");

        http.Response.Headers.CacheControl = "no-store";
        http.Response.Headers.Location = $"/api/v1/runs/{existing.Id}";
        http.Response.Headers["Idempotency-Replayed"] = "true";
        return Results.Ok(await ToResponseAsync(existing, db, cancellationToken));
    }

    private static async Task<IResult> ListRunsAsync(
        string? scenarioId,
        string? status,
        string? runType,
        int? pageSize,
        string? pageToken,
        HttpContext http,
        FluxAiDbContext db,
        CancellationToken cancellationToken)
    {
        if (!TryPage(pageSize, pageToken, out var size, out var offset, out var pageError))
            return Problem(StatusCodes.Status400BadRequest, "Paginação inválida", pageError);
        if (!IsOptionalEnum(status, "queued", "running", "succeeded", "failed", "interrupted") ||
            !IsOptionalEnum(runType, "training", "evaluation", "diagnostic", "replay"))
            return Problem(StatusCodes.Status400BadRequest, "Filtro inválido", "status ou runType não pertence aos valores documentados.");

        var query = db.TrainingRuns.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(scenarioId)) query = query.Where(x => x.ScenarioId == scenarioId);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);
        if (!string.IsNullOrWhiteSpace(runType)) query = query.Where(x => x.RunType == runType);

        var rows = await query.OrderByDescending(x => x.CreatedAtOrderKey).ThenBy(x => x.Id)
            .Skip(offset).Take(size + 1).ToListAsync(cancellationToken);
        var hasMore = rows.Count > size;
        if (hasMore) rows.RemoveAt(rows.Count - 1);
        var items = new List<TrainingRunResponse>(rows.Count);
        foreach (var row in rows)
            items.Add(await ToResponseAsync(row, db, cancellationToken));
        http.Response.Headers.CacheControl = "no-store";
        var nextToken = hasMore ? EncodeOffset(offset + rows.Count) : null;
        return Results.Ok(new RunPage(items, new PageInfo(size, nextToken, hasMore)));
    }

    private static async Task<IResult> GetRunAsync(
        Guid id,
        HttpContext http,
        FluxAiDbContext db,
        CancellationToken cancellationToken)
    {
        var run = await db.TrainingRuns.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (run is null) return Problem(StatusCodes.Status404NotFound, "Execução não encontrada", null);
        http.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await ToResponseAsync(run, db, cancellationToken));
    }

    private static async Task<IResult> ListMetricsAsync(
        Guid id,
        string? metricName,
        string? scope,
        string? signalControllerId,
        double? fromSimulationSeconds,
        double? toSimulationSeconds,
        int? pageSize,
        string? pageToken,
        HttpContext http,
        FluxAiDbContext db,
        CancellationToken cancellationToken)
    {
        var run = await db.TrainingRuns.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (run is null) return Problem(StatusCodes.Status404NotFound, "Execução não encontrada", null);
        if (!TryPage(pageSize, pageToken, out var size, out var offset, out var pageError))
            return Problem(StatusCodes.Status400BadRequest, "Paginação inválida", pageError);
        if (!string.IsNullOrWhiteSpace(metricName) && (metricName.Length > 128 || !MetricNamePattern.IsMatch(metricName)))
            return Problem(StatusCodes.Status400BadRequest, "metricName inválido", "Use o identificador documentado de métrica.");
        if (!IsOptionalEnum(scope, "network", "signalController"))
            return Problem(StatusCodes.Status400BadRequest, "scope inválido", "scope deve ser network ou signalController.");
        if (scope == "signalController" && string.IsNullOrWhiteSpace(signalControllerId))
            return Validation("signalControllerId", "É obrigatório quando scope=signalController.");
        if (scope == "network" && !string.IsNullOrWhiteSpace(signalControllerId))
            return Validation("signalControllerId", "Não se aplica a scope=network.");
        if ((fromSimulationSeconds is not null && (!double.IsFinite(fromSimulationSeconds.Value) || fromSimulationSeconds < 0)) ||
            (toSimulationSeconds is not null && (!double.IsFinite(toSimulationSeconds.Value) || toSimulationSeconds < 0)) ||
            (fromSimulationSeconds is not null && toSimulationSeconds is not null && fromSimulationSeconds > toSimulationSeconds))
            return Validation("simulationTime", "O intervalo deve conter valores finitos não negativos e from deve ser menor ou igual a to.");
        if (!string.IsNullOrWhiteSpace(signalControllerId) && !await db.SignalControllers.AnyAsync(
                x => x.ScenarioId == run.ScenarioId && x.ControllerId == signalControllerId, cancellationToken))
            return Validation("signalControllerId", "O controlador não pertence ao cenário desta execução.");

        var query = db.RunMetrics.AsNoTracking().Where(x => x.TrainingRunId == id);
        if (!string.IsNullOrWhiteSpace(metricName)) query = query.Where(x => x.MetricName == metricName);
        if (!string.IsNullOrWhiteSpace(scope)) query = query.Where(x => x.ScopeType == scope);
        if (!string.IsNullOrWhiteSpace(signalControllerId)) query = query.Where(x => x.SignalControllerId == signalControllerId);
        if (fromSimulationSeconds is not null) query = query.Where(x => x.SimulationTimeSeconds >= fromSimulationSeconds);
        if (toSimulationSeconds is not null) query = query.Where(x => x.SimulationTimeSeconds <= toSimulationSeconds);

        var rows = await query.OrderBy(x => x.MetricName).ThenBy(x => x.ScopeType)
            .ThenBy(x => x.SignalControllerId).ThenBy(x => x.SimulationTimeSeconds).ThenBy(x => x.SourceKey)
            .Skip(offset).Take(size + 1).ToListAsync(cancellationToken);
        var hasMore = rows.Count > size;
        if (hasMore) rows.RemoveAt(rows.Count - 1);
        http.Response.Headers.CacheControl = "no-store";
        var nextToken = hasMore ? EncodeOffset(offset + rows.Count) : null;
        MetricCoverage? coverage = null;
        var importedCoverage = Deserialize<MetricCoverage>(run.MetricCoverageJson);
        if (metricName?.StartsWith("queue.", StringComparison.Ordinal) == true && scope != "network" &&
            fromSimulationSeconds is null && toSimulationSeconds is null && importedCoverage is not null)
        {
            var observedSamples = await query.LongCountAsync(cancellationToken);
            var expectedSamples = importedCoverage.ExpectedSamples;
            if (!string.IsNullOrWhiteSpace(signalControllerId))
            {
                var controllerCount = await db.SignalControllers.CountAsync(x => x.ScenarioId == run.ScenarioId, cancellationToken);
                expectedSamples = controllerCount == 0 ? 0 : expectedSamples / controllerCount;
            }
            coverage = new MetricCoverage
            {
                ExpectedSamples = expectedSamples,
                ObservedSamples = observedSamples,
                Complete = expectedSamples == observedSamples,
                Note = importedCoverage.Note
            };
        }
        return Results.Ok(new MetricPage(rows.Select(ToMetricResponse).ToArray(), coverage, new PageInfo(size, nextToken, hasMore)));
    }

    private static async Task<IResult> ListSignalResultsAsync(
        Guid id,
        string? signalControllerId,
        int? pageSize,
        string? pageToken,
        HttpContext http,
        FluxAiDbContext db,
        CancellationToken cancellationToken)
    {
        var run = await db.TrainingRuns.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (run is null) return Problem(StatusCodes.Status404NotFound, "Execução não encontrada", null);
        if (!TryPage(pageSize, pageToken, out var size, out var offset, out var pageError))
            return Problem(StatusCodes.Status400BadRequest, "Paginação inválida", pageError);
        if (!string.IsNullOrWhiteSpace(signalControllerId) && !await db.SignalControllers.AnyAsync(
                x => x.ScenarioId == run.ScenarioId && x.ControllerId == signalControllerId, cancellationToken))
            return Validation("signalControllerId", "O controlador não pertence ao cenário desta execução.");

        var metrics = db.RunMetrics.AsNoTracking().Where(x => x.TrainingRunId == id && x.ScopeType == "signalController");
        if (!string.IsNullOrWhiteSpace(signalControllerId)) metrics = metrics.Where(x => x.SignalControllerId == signalControllerId);
        var rows = await metrics.OrderBy(x => x.SignalControllerId).ThenBy(x => x.MetricName).ToListAsync(cancellationToken);
        var controllerCount = await db.SignalControllers.CountAsync(x => x.ScenarioId == run.ScenarioId, cancellationToken);
        var expectedTotal = Deserialize<MetricCoverage>(run.MetricCoverageJson)?.ExpectedSamples ?? 0;
        var expectedPerController = controllerCount == 0 ? 0 : expectedTotal / controllerCount;
        var items = rows.GroupBy(x => new { x.SignalControllerId, x.MetricName, x.Unit })
            .OrderBy(group => group.Key.SignalControllerId, StringComparer.Ordinal)
            .ThenBy(group => group.Key.MetricName, StringComparer.Ordinal)
            .Select(group =>
            {
                var samples = group.ToArray();
                var observed = samples.LongLength;
                var complete = expectedPerController > 0 && observed >= expectedPerController;
                var metric = new MetricPointResponse(
                    group.Key.MetricName,
                    samples.Average(x => x.Value),
                    group.Key.Unit,
                    new MetricScopeRequest { Type = "signalController", SignalControllerId = group.Key.SignalControllerId },
                    null,
                    null,
                    null,
                    $"aggregate:{group.Key.MetricName}:mean");
                return new SignalResultResponse(group.Key.SignalControllerId!, metric,
                    new MetricCoverage { ExpectedSamples = expectedPerController, ObservedSamples = observed, Complete = complete,
                        Note = "Média aritmética das amostras observadas; lacunas não foram preenchidas com zero." });
            })
            .ToList();
        var page = items.Skip(offset).Take(size + 1).ToList();
        var hasMore = page.Count > size;
        if (hasMore) page.RemoveAt(page.Count - 1);
        http.Response.Headers.CacheControl = "no-store";
        var nextToken = hasMore ? EncodeOffset(offset + page.Count) : null;
        return Results.Ok(new SignalResultPage(page, new PageInfo(size, nextToken, hasMore)));
    }

    private static Dictionary<string, string[]> Validate(RunImportRequest request, Scenario scenario, HashSet<string> controllerIds)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        void Add(string field, string message)
        {
            if (errors.TryGetValue(field, out var previous)) errors[field] = [.. previous, message];
            else errors[field] = [message];
        }

        if (string.IsNullOrWhiteSpace(request.SourceRunId) || request.SourceRunId.Length > 200 || request.SourceRunId != request.SourceRunId.Trim()) Add("sourceRunId", "Deve conter de 1 a 200 caracteres, sem espaços nas extremidades.");
        if (string.IsNullOrWhiteSpace(request.ScenarioId) || request.ScenarioId.Length > 100) Add("scenarioId", "Deve conter de 1 a 100 caracteres.");
        if (!IsOptionalEnum(request.RunType, "training", "evaluation", "diagnostic", "replay")) Add("runType", "Valor inválido.");
        if (!IsOptionalEnum(request.Environment, "simulation", "replay")) Add("environment", "Valor inválido.");
        if (request.Seed < 0) Add("seed", "Deve ser não negativo.");
        if (!double.IsFinite(request.HorizonSeconds) || request.HorizonSeconds < 0) Add("horizonSeconds", "Deve ser finito e não negativo.");
        if (request.FinishedAtUtc < request.StartedAtUtc) Add("finishedAtUtc", "Deve ser maior ou igual a startedAtUtc.");

        var fingerprint = request.ScenarioFingerprint;
        if (!IsSha(fingerprint.NetworkSha256) || !string.Equals(fingerprint.NetworkSha256, scenario.NetworkSha256, StringComparison.OrdinalIgnoreCase)) Add("scenarioFingerprint.networkSha256", "Hash ausente, inválido ou diferente do cenário cadastrado.");
        if (!IsSha(fingerprint.CatalogSha256) || !string.Equals(fingerprint.CatalogSha256, scenario.CatalogSha256, StringComparison.OrdinalIgnoreCase)) Add("scenarioFingerprint.catalogSha256", "Hash ausente, inválido ou diferente do catálogo cadastrado.");
        if (!IsSha(fingerprint.TimingProfileSha256) || !string.Equals(fingerprint.TimingProfileSha256, scenario.TimingProfileSha256, StringComparison.OrdinalIgnoreCase)) Add("scenarioFingerprint.timingProfileSha256", "Hash ausente, inválido ou diferente do perfil cadastrado.");
        if (fingerprint.RouteDemandSha256 is not null && !IsSha(fingerprint.RouteDemandSha256)) Add("scenarioFingerprint.routeDemandSha256", "Deve ser um SHA-256 hexadecimal.");

        if (request.Model is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Model.Algorithm) || request.Model.Algorithm.Length > 100) Add("model.algorithm", "Deve conter de 1 a 100 caracteres.");
            if (request.Model.ArtifactId?.Length > 120) Add("model.artifactId", "Máximo de 120 caracteres.");
            if (request.Model.Sha256 is not null && !IsSha(request.Model.Sha256)) Add("model.sha256", "Deve ser um SHA-256 hexadecimal.");
        }
        if (request.EffectiveParameters is { ValueKind: not JsonValueKind.Object }) Add("effectiveParameters", "Deve ser um objeto JSON quando informado.");

        ValidateTraffic(request.TrafficSummary, Add);
        if (request.Metrics.Count > 10000) Add("metrics", "Máximo de 10.000 métricas por importação.");
        var sourceKeys = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < request.Metrics.Count; index++)
        {
            var metric = request.Metrics[index];
            var prefix = $"metrics[{index}]";
            if (string.IsNullOrWhiteSpace(metric.MetricName) || metric.MetricName.Length > 128 || !MetricNamePattern.IsMatch(metric.MetricName)) Add($"{prefix}.metricName", "Nome de métrica inválido.");
            if (!double.IsFinite(metric.Value)) Add($"{prefix}.value", "Deve ser um número finito.");
            if (string.IsNullOrWhiteSpace(metric.Unit) || metric.Unit.Length > 40) Add($"{prefix}.unit", "Deve conter de 1 a 40 caracteres.");
            if (!sourceKeys.Add(metric.SourceKey)) Add($"{prefix}.sourceKey", "A chave de origem deve ser única dentro da execução.");
            if (string.IsNullOrWhiteSpace(metric.SourceKey) || metric.SourceKey.Length > 200) Add($"{prefix}.sourceKey", "Deve conter de 1 a 200 caracteres.");
            if (metric.Scope.Type == "network")
            {
                if (metric.Scope.SignalControllerId is not null) Add($"{prefix}.scope.signalControllerId", "Não se aplica ao escopo network.");
            }
            else if (metric.Scope.Type == "signalController")
            {
                if (string.IsNullOrWhiteSpace(metric.Scope.SignalControllerId) || !controllerIds.Contains(metric.Scope.SignalControllerId))
                    Add($"{prefix}.scope.signalControllerId", "Informe um controlador pertencente ao cenário.");
            }
            else Add($"{prefix}.scope.type", "Deve ser network ou signalController.");
            if (metric.Episode is < 1) Add($"{prefix}.episode", "Deve ser maior que zero.");
            if (metric.Step is < 0) Add($"{prefix}.step", "Deve ser não negativo.");
            if (metric.SimulationTimeSeconds is not null &&
                (!double.IsFinite(metric.SimulationTimeSeconds.Value) || metric.SimulationTimeSeconds < 0 || metric.SimulationTimeSeconds > request.HorizonSeconds))
                Add($"{prefix}.simulationTimeSeconds", "Deve estar entre zero e o horizonte simulado.");
        }

        if (request.Coverage is not null)
        {
            if (request.Coverage.ExpectedSamples < 0 || request.Coverage.ObservedSamples < 0 || request.Coverage.ObservedSamples > request.Coverage.ExpectedSamples)
                Add("coverage", "A cobertura deve ter contagens não negativas e observadas menores ou iguais às esperadas.");
            if (request.Coverage.Complete != (request.Coverage.ExpectedSamples == request.Coverage.ObservedSamples))
                Add("coverage.complete", "Deve corresponder à igualdade entre amostras esperadas e observadas.");
            if (request.Coverage.Note?.Length > 300) Add("coverage.note", "Máximo de 300 caracteres.");
        }

        if (request.Artifacts.Count is < 1 or > 100) Add("artifacts", "Informe entre 1 e 100 artefatos.");
        var artifactNames = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < request.Artifacts.Count; index++)
        {
            var artifact = request.Artifacts[index];
            if (string.IsNullOrWhiteSpace(artifact.LogicalName) || artifact.LogicalName.Length > 160) Add($"artifacts[{index}].logicalName", "Deve conter de 1 a 160 caracteres.");
            if (!artifactNames.Add(artifact.LogicalName)) Add($"artifacts[{index}].logicalName", "O nome lógico deve ser único.");
            if (!IsSha(artifact.Sha256)) Add($"artifacts[{index}].sha256", "Deve ser um SHA-256 hexadecimal.");
            if (artifact.SizeBytes is < 0) Add($"artifacts[{index}].sizeBytes", "Deve ser não negativo.");
            if (artifact.ContentType?.Length > 120) Add($"artifacts[{index}].contentType", "Máximo de 120 caracteres.");
        }
        if (request.Limitations.Count > 50) Add("limitations", "Máximo de 50 limitações.");
        for (var i = 0; i < request.Limitations.Count; i++)
            if (request.Limitations[i].Length > 500) Add($"limitations[{i}]", "Máximo de 500 caracteres.");
        return errors;
    }

    private static void ValidateTraffic(TrafficRunSummary traffic, Action<string, string> add)
    {
        CheckCount(traffic.Departed, "trafficSummary.departed", add);
        CheckCount(traffic.Arrived, "trafficSummary.arrived", add);
        CheckCount(traffic.ActiveAtEnd, "trafficSummary.activeAtEnd", add);
        CheckCount(traffic.DuePendingAtEnd, "trafficSummary.duePendingAtEnd", add);
        CheckCount(traffic.CollisionEventsConfirmed, "trafficSummary.collisionEventsConfirmed", add);
        CheckCount(traffic.CollisionVehicleSteps, "trafficSummary.collisionVehicleSteps", add);
        CheckCount(traffic.TeleportedVehicles, "trafficSummary.teleportedVehicles", add);
        CheckDuration(traffic.MeanCompletedDurationSeconds, "trafficSummary.meanCompletedDurationSeconds", add);
        CheckDuration(traffic.MeanCompletedWaitingSeconds, "trafficSummary.meanCompletedWaitingSeconds", add);
    }

    private static void CheckCount(long? value, string field, Action<string, string> add)
    {
        if (value is < 0) add(field, "Deve ser não negativo.");
    }

    private static void CheckDuration(double? value, string field, Action<string, string> add)
    {
        if (value is not null && (!double.IsFinite(value.Value) || value.Value < 0)) add(field, "Deve ser finito e não negativo.");
    }

    private static bool IsSha(string value) => !string.IsNullOrWhiteSpace(value) && Sha256Pattern.IsMatch(value);

    private static bool IsValidIdempotencyKey(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length is >= 16 and <= 128 && value.All(character =>
            char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or '~' or '-');

    private static bool IsOptionalEnum(string? value, params string[] allowed) =>
        value is null || allowed.Contains(value, StringComparer.Ordinal);

    private static string HashCanonicalRequest(RunImportRequest request)
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(request, JsonOptions));
        using var memory = new MemoryStream();
        using (var writer = new Utf8JsonWriter(memory)) WriteCanonical(document.RootElement, writer);
        return Convert.ToHexString(SHA256.HashData(memory.ToArray())).ToLowerInvariant();
    }

    private static void WriteCanonical(JsonElement element, Utf8JsonWriter writer)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject().OrderBy(x => x.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    WriteCanonical(property.Value, writer);
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray()) WriteCanonical(item, writer);
                writer.WriteEndArray();
                break;
            default:
                element.WriteTo(writer);
                break;
        }
    }

    private static async Task<TrainingRunResponse> ToResponseAsync(TrainingRun run, FluxAiDbContext db, CancellationToken cancellationToken)
    {
        var artifacts = await db.RunArtifacts.AsNoTracking().Where(x => x.TrainingRunId == run.Id)
            .OrderBy(x => x.LogicalName)
            .Select(x => new RunArtifactResponse(x.Id, x.LogicalName, x.Sha256, x.SizeBytes, x.ContentType))
            .ToListAsync(cancellationToken);
        return new TrainingRunResponse(
            run.Id,
            run.SourceRunId,
            run.ScenarioId,
            run.RunType,
            run.Origin,
            run.Environment,
            run.Status,
            run.Seed,
            run.HorizonSeconds,
            Deserialize<RunScenarioFingerprint>(run.ScenarioFingerprintJson),
            Deserialize<RunModel>(run.ModelJson),
            Deserialize<JsonElement>(run.EffectiveParametersJson) is { ValueKind: JsonValueKind.Object } effective ? effective : JsonDocument.Parse("{}").RootElement.Clone(),
            Deserialize<TrafficRunSummary>(run.TrafficSummaryJson),
            Deserialize<MetricCoverage>(run.MetricCoverageJson),
            artifacts,
            run.CreatedAtUtc,
            run.StartedAtUtc,
            run.FinishedAtUtc,
            Deserialize<List<string>>(run.LimitationsJson) ?? []);
    }

    private static MetricPointResponse ToMetricResponse(RunMetric metric) => new(
        metric.MetricName,
        metric.Value,
        metric.Unit,
        new MetricScopeRequest { Type = metric.ScopeType, SignalControllerId = metric.SignalControllerId },
        metric.Episode,
        metric.Step,
        metric.SimulationTimeSeconds,
        metric.SourceKey);

    private static T? Deserialize<T>(string? json) =>
        string.IsNullOrWhiteSpace(json) ? default : JsonSerializer.Deserialize<T>(json, JsonOptions);

    private static bool TryPage(int? requestedSize, string? token, out int size, out int offset, out string error)
    {
        size = requestedSize ?? 50;
        offset = 0;
        error = string.Empty;
        if (size is < 1 or > 100)
        {
            error = "pageSize deve estar entre 1 e 100.";
            return false;
        }
        if (string.IsNullOrEmpty(token)) return true;
        try
        {
            var base64 = token.Replace('-', '+').Replace('_', '/');
            base64 = base64.PadRight(base64.Length + ((4 - base64.Length % 4) % 4), '=');
            var value = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
            if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out offset) || offset < 0)
                throw new FormatException();
            return true;
        }
        catch (FormatException)
        {
            error = "pageToken inválido.";
            return false;
        }
    }

    private static string EncodeOffset(int offset) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(offset.ToString(CultureInfo.InvariantCulture)))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static IResult Validation(string field, string message) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { [field] = [message] }, statusCode: StatusCodes.Status422UnprocessableEntity);

    private static IResult Problem(int status, string title, string? detail) => Results.Problem(
        statusCode: status,
        title: title,
        detail: detail,
        type: "about:blank");

    [GeneratedRegex("^[A-Fa-f0-9]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex Sha256Regex();

    [GeneratedRegex("^[A-Za-z][A-Za-z0-9_.-]{0,127}$", RegexOptions.CultureInvariant)]
    private static partial Regex MetricNameRegex();
}
