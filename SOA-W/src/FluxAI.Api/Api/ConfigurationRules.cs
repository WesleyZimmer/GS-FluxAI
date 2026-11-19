using System.Text.Json;
using FluxAI.Api.Domain;

namespace FluxAI.Api.Api;

internal static class ConfigurationRules
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static bool TryNormalize(
        ConfigurationWriteRequest request,
        IReadOnlyList<SignalController> controllers,
        out NormalizedConfiguration normalized,
        out Dictionary<string, string[]> errors)
    {
        var issues = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        void Add(string key, string message)
        {
            if (!issues.TryGetValue(key, out var messages))
            {
                messages = [];
                issues.Add(key, messages);
            }
            messages.Add(message);
        }

        var name = request.Name?.Trim() ?? string.Empty;
        if (name.Length is < 1 or > 120)
            Add("name", "O nome deve ter entre 1 e 120 caracteres.");

        var timing = request.GreenTiming;
        var defaults = timing?.Default;
        if (defaults is null)
        {
            Add("greenTiming.default", "O perfil padrão de verde é obrigatório.");
        }
        else
        {
            ValidateSeconds(defaults.DecisionMinimumSeconds, "greenTiming.default.decisionMinimumSeconds", Add);
            ValidateSeconds(defaults.MaximumSeconds, "greenTiming.default.maximumSeconds", Add);
            if (double.IsFinite(defaults.DecisionMinimumSeconds) && double.IsFinite(defaults.MaximumSeconds)
                && defaults.DecisionMinimumSeconds > defaults.MaximumSeconds)
                Add("greenTiming.default", "O mínimo de decisão não pode ser maior que o máximo de verde.");
        }

        var blocking = request.Blocking;
        if (blocking is null)
        {
            Add("blocking", "Os parâmetros de bloqueio são obrigatórios.");
        }
        else
        {
            if (!double.IsFinite(blocking.ObservedCapacityFractionThreshold)
                || blocking.ObservedCapacityFractionThreshold <= 0
                || blocking.ObservedCapacityFractionThreshold > 1)
                Add("blocking.observedCapacityFractionThreshold", "Informe um valor maior que 0 e menor ou igual a 1.");
            if (blocking.ConfirmationSamples is < 2 or > 20)
                Add("blocking.confirmationSamples", "O número de amostras deve estar entre 2 e 20.");
            if (!double.IsFinite(blocking.MaximumMeasurementAgeSeconds)
                || blocking.MaximumMeasurementAgeSeconds <= 0
                || blocking.MaximumMeasurementAgeSeconds > 60)
                Add("blocking.maximumMeasurementAgeSeconds", "A idade máxima deve ser maior que 0 e menor ou igual a 60 segundos.");
        }

        var ppo = request.Ppo ?? new PpoParameters();
        ValidatePpo(ppo, Add);

        var overrides = timing?.PhaseOverrides ?? [];
        if (overrides.Count > 500)
            Add("greenTiming.phaseOverrides", "São permitidas no máximo 500 sobrescritas.");

        var controllersById = controllers.ToDictionary(x => x.ControllerId, StringComparer.Ordinal);
        var seen = new HashSet<(string ControllerId, int PhaseIndex)>();
        foreach (var item in overrides)
        {
            if (string.IsNullOrWhiteSpace(item.SignalControllerId) || item.SignalControllerId.Length > 100)
            {
                Add("greenTiming.phaseOverrides", "Cada sobrescrita precisa identificar um controlador válido.");
                continue;
            }
            if (!seen.Add((item.SignalControllerId, item.PhaseIndex)))
                Add("greenTiming.phaseOverrides", $"A fase {item.PhaseIndex} do controlador {item.SignalControllerId} foi informada mais de uma vez.");
            if (item.PhaseIndex < 0)
                Add("greenTiming.phaseOverrides", "O índice da fase não pode ser negativo.");
            if (item.DecisionMinimumSeconds is null && item.MaximumSeconds is null)
                Add("greenTiming.phaseOverrides", "Cada sobrescrita deve informar mínimo, máximo ou ambos.");
            if (item.DecisionMinimumSeconds is { } minimum)
                ValidateSeconds(minimum, "greenTiming.phaseOverrides.decisionMinimumSeconds", Add);
            if (item.MaximumSeconds is { } maximum)
                ValidateSeconds(maximum, "greenTiming.phaseOverrides.maximumSeconds", Add);

            if (!controllersById.TryGetValue(item.SignalControllerId, out var controller))
            {
                Add("greenTiming.phaseOverrides.signalControllerId", $"O controlador {item.SignalControllerId} não pertence ao cenário selecionado.");
                continue;
            }

            var greenIndexes = JsonSerializer.Deserialize<int[]>(controller.GreenPhaseIndexesJson, JsonOptions) ?? [];
            var phases = JsonSerializer.Deserialize<SignalPhaseCatalog[]>(controller.PhaseCatalogJson, JsonOptions) ?? [];
            if (!greenIndexes.Contains(item.PhaseIndex) || item.PhaseIndex >= phases.Length)
                Add("greenTiming.phaseOverrides.phaseIndex", $"A fase {item.PhaseIndex} não é uma fase verde conhecida do controlador {item.SignalControllerId}.");

            if (defaults is not null && item.DecisionMinimumSeconds is { } requestedMinimum
                && item.MaximumSeconds is { } requestedMaximum
                && double.IsFinite(requestedMinimum) && double.IsFinite(requestedMaximum)
                && requestedMinimum > requestedMaximum)
                Add("greenTiming.phaseOverrides", "O mínimo de uma fase não pode ser maior que seu máximo.");
        }

        var effective = new List<EffectivePhaseTiming>();
        if (defaults is not null)
        {
            foreach (var controller in controllers.OrderBy(x => x.ControllerId, StringComparer.Ordinal))
            {
                var greenIndexes = JsonSerializer.Deserialize<int[]>(controller.GreenPhaseIndexesJson, JsonOptions) ?? [];
                var phases = JsonSerializer.Deserialize<SignalPhaseCatalog[]>(controller.PhaseCatalogJson, JsonOptions) ?? [];
                foreach (var phaseIndex in greenIndexes.Order())
                {
                    if (phaseIndex < 0 || phaseIndex >= phases.Length)
                        continue;
                    var phaseOverride = overrides.FirstOrDefault(x =>
                        x.SignalControllerId == controller.ControllerId && x.PhaseIndex == phaseIndex);
                    var requestedMinimum = phaseOverride?.DecisionMinimumSeconds ?? defaults.DecisionMinimumSeconds;
                    var maximum = phaseOverride?.MaximumSeconds ?? defaults.MaximumSeconds;
                    if (double.IsFinite(requestedMinimum) && double.IsFinite(maximum) && requestedMinimum > maximum)
                        Add("greenTiming.phaseOverrides", $"Os limites da fase {phaseIndex} do controlador {controller.ControllerId} são incompatíveis.");
                    effective.Add(new EffectivePhaseTiming(
                        controller.ControllerId,
                        phaseIndex,
                        phases[phaseIndex].Duration,
                        requestedMinimum,
                        Math.Min(requestedMinimum, phases[phaseIndex].Duration),
                        maximum));
                }
            }
        }

        if (issues.Count > 0)
        {
            normalized = default!;
            errors = issues.ToDictionary(x => x.Key, x => x.Value.ToArray(), StringComparer.Ordinal);
            return false;
        }

        normalized = new NormalizedConfiguration(
            new GreenTimingRequest
            {
                Default = new GreenTimingDefault
                {
                    DecisionMinimumSeconds = defaults!.DecisionMinimumSeconds,
                    MaximumSeconds = defaults.MaximumSeconds
                },
                PhaseOverrides = overrides
                    .OrderBy(x => x.SignalControllerId, StringComparer.Ordinal)
                    .ThenBy(x => x.PhaseIndex)
                    .ToList()
            },
            new BlockingParameters
            {
                ObservedCapacityFractionThreshold = blocking!.ObservedCapacityFractionThreshold,
                ConfirmationSamples = blocking.ConfirmationSamples,
                MaximumMeasurementAgeSeconds = blocking.MaximumMeasurementAgeSeconds
            },
            ppo,
            effective);
        errors = [];
        return true;
    }

    public static IReadOnlyList<GreenPhaseOverride> ToEntities(
        Guid configurationId,
        int revision,
        string scenarioId,
        IReadOnlyList<PhaseTimingOverrideRequest> overrides) =>
        overrides.Select(x => new GreenPhaseOverride
        {
            ConfigurationId = configurationId,
            Revision = revision,
            ScenarioId = scenarioId,
            ControllerId = x.SignalControllerId,
            PhaseIndex = x.PhaseIndex,
            DecisionMinimumSeconds = x.DecisionMinimumSeconds,
            MaximumSeconds = x.MaximumSeconds
        }).ToArray();

    private static void ValidateSeconds(double value, string key, Action<string, string> add)
    {
        if (!double.IsFinite(value) || value <= 0 || value > 90)
            add(key, "O valor deve ser maior que 0 e menor ou igual a 90 segundos.");
    }

    private static void ValidatePpo(PpoParameters ppo, Action<string, string> add)
    {
        if (ppo.TotalTimesteps is < 128 or > 100_000) add("ppo.totalTimesteps", "Use um valor entre 128 e 100000.");
        if (ppo.CheckpointEvery is < 0 or > 100_000) add("ppo.checkpointEvery", "Use um valor entre 0 e 100000.");
        if (ppo.RolloutSteps is < 16 or > 4096) add("ppo.rolloutSteps", "Use um valor entre 16 e 4096.");
        if (ppo.BatchSize is < 8 or > 4096) add("ppo.batchSize", "Use um valor entre 8 e 4096.");
        else if (ppo.BatchSize > ppo.RolloutSteps) add("ppo.batchSize", "batchSize não pode exceder rolloutSteps.");
        if (ppo.Epochs is < 1 or > 100) add("ppo.epochs", "Use um valor entre 1 e 100.");
        if (!double.IsFinite(ppo.LearningRate) || ppo.LearningRate <= 0 || ppo.LearningRate > 0.1) add("ppo.learningRate", "Use um valor maior que 0 e menor ou igual a 0.1.");
        if (!InUnitInterval(ppo.Gamma)) add("ppo.gamma", "Use um valor entre 0 e 1.");
        if (!InUnitInterval(ppo.GaeLambda)) add("ppo.gaeLambda", "Use um valor entre 0 e 1.");
        if (!double.IsFinite(ppo.ClipRange) || ppo.ClipRange <= 0 || ppo.ClipRange > 1) add("ppo.clipRange", "Use um valor maior que 0 e menor ou igual a 1.");
        if (!InUnitInterval(ppo.EntropyCoefficient)) add("ppo.entropyCoefficient", "Use um valor entre 0 e 1.");
    }

    private static bool InUnitInterval(double value) => double.IsFinite(value) && value is >= 0 and <= 1;
}
