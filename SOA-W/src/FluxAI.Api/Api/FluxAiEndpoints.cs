using System.Globalization;
using System.Text;
using System.Text.Json;
using FluxAI.Api.Domain;
using FluxAI.Api.Persistence;
using FluxAI.Api.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace FluxAI.Api.Api;

internal static class FluxAiEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static void Map(WebApplication app)
    {
        app.MapGet("/healthz", async (FluxAiDbContext db, CancellationToken cancellationToken) =>
            await db.Database.CanConnectAsync(cancellationToken)
                ? Results.Ok(new { status = "ok", database = "connected" })
                : Results.Problem("O banco SQLite não está disponível.", statusCode: StatusCodes.Status503ServiceUnavailable));

        app.MapGet("/api/v1/scenarios", ListScenariosAsync);
        app.MapGet("/api/v1/signal-controllers", ListSignalControllersAsync);
        app.MapPost("/api/v1/configurations", CreateConfigurationAsync)
            .RequireAuthorization(FluxAiAuthentication.OperatorPolicy);
        app.MapGet("/api/v1/configurations/{id:guid}", GetConfigurationAsync)
            .RequireAuthorization(FluxAiAuthentication.OperatorPolicy);
        app.MapPut("/api/v1/configurations/{id:guid}", ReplaceConfigurationAsync)
            .RequireAuthorization(FluxAiAuthentication.OperatorPolicy);
    }

    private static async Task<IResult> ListScenariosAsync(
        int? pageSize,
        string? pageToken,
        FluxAiDbContext db,
        CancellationToken cancellationToken)
    {
        if (!TryPage(pageSize, pageToken, out var size, out var offset, out var error))
            return Results.Problem(statusCode: 400, title: "Parâmetros de paginação inválidos", detail: error);

        var rows = await db.Scenarios.AsNoTracking().OrderBy(x => x.Id)
            .Skip(offset).Take(size + 1)
            .Select(x => new ScenarioItem(
                x.Id,
                x.Name,
                x.CatalogRevision,
                x.NetworkSha256,
                x.CatalogSha256,
                x.TimingProfileId,
                x.Environment,
                db.SignalControllers.Count(controller => controller.ScenarioId == x.Id),
                x.IsEnabled))
            .ToListAsync(cancellationToken);

        var hasMore = rows.Count > size;
        if (hasMore)
            rows.RemoveAt(rows.Count - 1);
        var nextToken = hasMore ? EncodeOffset(offset + rows.Count) : null;
        return Results.Ok(new ScenarioPage(rows, new PageInfo(size, nextToken, hasMore)));
    }

    private static async Task<IResult> ListSignalControllersAsync(
        string? scenarioId,
        int? pageSize,
        string? pageToken,
        FluxAiDbContext db,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(scenarioId))
            return Results.Problem(statusCode: 400, title: "scenarioId é obrigatório");
        if (!TryPage(pageSize, pageToken, out var size, out var offset, out var error))
            return Results.Problem(statusCode: 400, title: "Parâmetros de paginação inválidos", detail: error);
        if (!await db.Scenarios.AnyAsync(x => x.Id == scenarioId, cancellationToken))
            return Results.Problem(statusCode: 404, title: "Cenário não encontrado");

        var entities = await db.SignalControllers.AsNoTracking()
            .Where(x => x.ScenarioId == scenarioId)
            .OrderBy(x => x.ControllerId)
            .Skip(offset).Take(size + 1)
            .ToListAsync(cancellationToken);
        var hasMore = entities.Count > size;
        if (hasMore)
            entities.RemoveAt(entities.Count - 1);
        var items = entities.Select(x => new SignalControllerItem(
            x.ControllerId,
            x.ScenarioId,
            x.IsAdaptive,
            JsonSerializer.Deserialize<SignalPhaseResponse[]>(x.PhaseCatalogJson, JsonOptions) ?? []))
            .ToArray();
        var nextToken = hasMore ? EncodeOffset(offset + items.Length) : null;
        return Results.Ok(new SignalControllerPage(items, new PageInfo(size, nextToken, hasMore)));
    }

    private static async Task<IResult> CreateConfigurationAsync(
        ConfigurationWriteRequest request,
        HttpContext http,
        FluxAiDbContext db,
        CancellationToken cancellationToken)
    {
        var scenario = await db.Scenarios.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == request.ScenarioId, cancellationToken);
        if (scenario is null || !scenario.IsEnabled)
            return Results.Problem(statusCode: 404, title: "Cenário não encontrado ou desabilitado");

        var controllers = await db.SignalControllers.AsNoTracking()
            .Where(x => x.ScenarioId == scenario.Id).ToListAsync(cancellationToken);
        if (!ConfigurationRules.TryNormalize(request, controllers, out var normalized, out var errors))
            return Results.ValidationProblem(errors, statusCode: StatusCodes.Status422UnprocessableEntity);

        var now = DateTimeOffset.UtcNow;
        var entity = new TrainingConfiguration
        {
            Id = Guid.NewGuid(),
            ScenarioId = scenario.Id,
            Name = request.Name.Trim(),
            CurrentRevision = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        db.TrainingConfigurations.Add(entity);
        db.TrainingConfigurationRevisions.Add(ToRevision(entity.Id, 1, normalized, now));
        db.GreenPhaseOverrides.AddRange(ConfigurationRules.ToEntities(entity.Id, 1, scenario.Id, normalized.GreenTiming.PhaseOverrides));
        await db.SaveChangesAsync(cancellationToken);

        var response = ToResponse(entity, normalized, now, now);
        SetResponseHeaders(http, response.Revision);
        return Results.Created($"/api/v1/configurations/{entity.Id}", response);
    }

    private static async Task<IResult> GetConfigurationAsync(
        Guid id,
        HttpContext http,
        FluxAiDbContext db,
        CancellationToken cancellationToken)
    {
        var loaded = await LoadConfigurationAsync(id, db, cancellationToken);
        if (loaded is null)
            return Results.Problem(statusCode: 404, title: "Configuração não encontrada");
        var response = loaded;
        SetResponseHeaders(http, response.Revision);
        return Results.Ok(response);
    }

    private static async Task<IResult> ReplaceConfigurationAsync(
        Guid id,
        ConfigurationWriteRequest request,
        HttpContext http,
        FluxAiDbContext db,
        CancellationToken cancellationToken)
    {
        if (!TryReadETag(http.Request.Headers["If-Match"].ToString(), out var expectedRevision))
            return Results.Problem(statusCode: 400, title: "If-Match deve conter o ETag da revisão atual");

        var entity = await db.TrainingConfigurations.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
            return Results.Problem(statusCode: 404, title: "Configuração não encontrada");
        if (entity.CurrentRevision != expectedRevision)
            return Results.Problem(statusCode: 412, title: "If-Match não corresponde à revisão atual");
        if (!string.Equals(request.ScenarioId, entity.ScenarioId, StringComparison.Ordinal))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["scenarioId"] = ["Uma configuração não pode ser movida para outro cenário."] }, statusCode: 422);

        var scenario = await db.Scenarios.AsNoTracking().SingleAsync(x => x.Id == entity.ScenarioId, cancellationToken);
        var controllers = await db.SignalControllers.AsNoTracking()
            .Where(x => x.ScenarioId == scenario.Id).ToListAsync(cancellationToken);
        if (!ConfigurationRules.TryNormalize(request, controllers, out var normalized, out var errors))
            return Results.ValidationProblem(errors, statusCode: StatusCodes.Status422UnprocessableEntity);

        var oldRevision = entity.CurrentRevision;
        var nextRevision = checked(oldRevision + 1);
        var now = DateTimeOffset.UtcNow;
        entity.Name = request.Name.Trim();
        entity.CurrentRevision = nextRevision;
        entity.UpdatedAtUtc = now;
        db.Entry(entity).Property(x => x.CurrentRevision).OriginalValue = expectedRevision;
        db.TrainingConfigurationRevisions.Add(ToRevision(id, nextRevision, normalized, now));
        db.GreenPhaseOverrides.AddRange(ConfigurationRules.ToEntities(id, nextRevision, scenario.Id, normalized.GreenTiming.PhaseOverrides));

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Problem(statusCode: 412, title: "If-Match não corresponde à revisão atual");
        }

        var response = ToResponse(entity, normalized, entity.CreatedAtUtc, now);
        SetResponseHeaders(http, response.Revision);
        return Results.Ok(response);
    }

    private static async Task<ConfigurationResponse?> LoadConfigurationAsync(
        Guid id,
        FluxAiDbContext db,
        CancellationToken cancellationToken)
    {
        var configuration = await db.TrainingConfigurations.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (configuration is null)
            return null;
        var revision = await db.TrainingConfigurationRevisions.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ConfigurationId == id && x.Revision == configuration.CurrentRevision, cancellationToken);
        if (revision is null)
            throw new InvalidOperationException($"A revisão atual da configuração {id} não existe.");

        var green = JsonSerializer.Deserialize<GreenTimingRequest>(revision.GreenTimingJson, JsonOptions)
            ?? throw new InvalidDataException("greenTiming persistido inválido.");
        var blocking = JsonSerializer.Deserialize<BlockingParameters>(revision.BlockingJson, JsonOptions)
            ?? throw new InvalidDataException("blocking persistido inválido.");
        var ppo = JsonSerializer.Deserialize<PpoParameters>(revision.PpoJson, JsonOptions)
            ?? throw new InvalidDataException("ppo persistido inválido.");
        var controllers = await db.SignalControllers.AsNoTracking()
            .Where(x => x.ScenarioId == configuration.ScenarioId).ToListAsync(cancellationToken);
        var syntheticRequest = new ConfigurationWriteRequest
        {
            Name = configuration.Name,
            ScenarioId = configuration.ScenarioId,
            GreenTiming = green,
            Blocking = blocking,
            Ppo = ppo
        };
        if (!ConfigurationRules.TryNormalize(syntheticRequest, controllers, out var normalized, out var validationErrors))
            throw new InvalidDataException($"A revisão {revision.Revision} da configuração {id} não é válida: {JsonSerializer.Serialize(validationErrors, JsonOptions)}");
        return ToResponse(configuration, normalized, configuration.CreatedAtUtc, configuration.UpdatedAtUtc);
    }

    private static TrainingConfigurationRevision ToRevision(
        Guid id,
        int revision,
        NormalizedConfiguration normalized,
        DateTimeOffset createdAtUtc) => new()
    {
        ConfigurationId = id,
        Revision = revision,
        GreenTimingJson = JsonSerializer.Serialize(normalized.GreenTiming, JsonOptions),
        BlockingJson = JsonSerializer.Serialize(normalized.Blocking, JsonOptions),
        PpoJson = JsonSerializer.Serialize(normalized.Ppo, JsonOptions),
        CreatedAtUtc = createdAtUtc
    };

    private static ConfigurationResponse ToResponse(
        TrainingConfiguration entity,
        NormalizedConfiguration normalized,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc) => new(
        entity.Id,
        entity.Name,
        entity.ScenarioId,
        entity.CurrentRevision,
        normalized.GreenTiming,
        normalized.EffectivePhaseTimings,
        normalized.Blocking,
        normalized.Ppo,
        createdAtUtc,
        updatedAtUtc);

    private static void SetResponseHeaders(HttpContext http, int revision)
    {
        http.Response.Headers.ETag = $"\"{revision}\"";
        http.Response.Headers.CacheControl = "no-store";
    }

    private static bool TryReadETag(string? header, out int revision)
    {
        revision = 0;
        if (string.IsNullOrWhiteSpace(header))
            return false;
        var value = header.Trim();
        if (value.Length < 3 || value[0] != '"' || value[^1] != '"')
            return false;
        return int.TryParse(value[1..^1], NumberStyles.None, CultureInfo.InvariantCulture, out revision) && revision > 0;
    }

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
        if (string.IsNullOrEmpty(token))
            return true;
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
}
