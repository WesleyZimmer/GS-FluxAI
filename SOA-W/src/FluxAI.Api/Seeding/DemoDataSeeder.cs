using System.Security.Cryptography;
using System.Text.Json;
using FluxAI.Api.Api;
using FluxAI.Api.Domain;
using FluxAI.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FluxAI.Api.Seeding;

internal static class DemoDataSeeder
{
    private const string ScenarioId = "fluxia-stage6-c2-8-signals";
    private static readonly Guid DefaultConfigurationId = Guid.Parse("d90f8532-b77e-4b9d-9d4d-0b5851b6a0c2");
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task SeedAsync(FluxAiDbContext db, CancellationToken cancellationToken = default)
    {
        var seedDirectory = Path.Combine(AppContext.BaseDirectory, "SeedData");
        var catalogPath = Path.Combine(seedDirectory, "pilot_signals_8_c2.json");
        var profilePath = Path.Combine(seedDirectory, "green_timing_profile_c2.json");
        if (!File.Exists(catalogPath) || !File.Exists(profilePath))
            throw new FileNotFoundException("Os arquivos do cenário de demonstração C2 não foram copiados para SeedData.");

        var catalogBytes = await File.ReadAllBytesAsync(catalogPath, cancellationToken);
        var profileBytes = await File.ReadAllBytesAsync(profilePath, cancellationToken);
        var catalog = JsonSerializer.Deserialize<CatalogFile>(catalogBytes, JsonOptions)
            ?? throw new InvalidDataException("O catálogo C2 está vazio ou inválido.");
        using var profileDocument = JsonDocument.Parse(profileBytes);
        var profileRoot = profileDocument.RootElement;
        var profileId = profileRoot.GetProperty("profile_id").GetString()
            ?? throw new InvalidDataException("O perfil C2 não contém profile_id.");
        var greenProfile = profileRoot.GetProperty("green");
        var blockingProfile = profileRoot.GetProperty("downstream_blocking");

        var catalogHash = Sha256(catalogBytes);
        var profileHash = Sha256(profileBytes);
        var now = DateTimeOffset.UtcNow;

        var scenario = await db.Scenarios.SingleOrDefaultAsync(x => x.Id == ScenarioId, cancellationToken);
        if (scenario is not null && scenario.CatalogSha256 != catalogHash)
            throw new InvalidOperationException("O cenário C2 já existe com outro hash de catálogo. Cadastre uma nova revisão em vez de substituir o histórico.");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (scenario is null)
        {
            scenario = new Scenario
            {
                Id = ScenarioId,
                Name = "Centro fluxAI C2 — piloto de 8 semáforos",
                Environment = "simulation",
                CatalogRevision = catalog.CatalogRevision,
                NetworkSha256 = catalog.NetworkSha256,
                CatalogSha256 = catalogHash,
                TimingProfileId = profileId,
                TimingProfileSha256 = profileHash,
                IsEnabled = true,
                CreatedAtUtc = now
            };
            db.Scenarios.Add(scenario);

            foreach (var signal in catalog.Signals)
            {
                db.SignalControllers.Add(new SignalController
                {
                    ScenarioId = ScenarioId,
                    ControllerId = signal.ControllerId,
                    OsmId = signal.OsmId,
                    X = signal.X,
                    Y = signal.Y,
                    IsAdaptive = signal.AdaptiveControl,
                    GreenPhaseIndexesJson = JsonSerializer.Serialize(signal.GreenPhaseIndexes, JsonOptions),
                    RedClearancePhaseIndexesJson = JsonSerializer.Serialize(signal.RedClearancePhaseIndexes, JsonOptions),
                    PhaseCatalogJson = JsonSerializer.Serialize(signal.Phases, JsonOptions)
                });
            }
        }

        if (!await db.TrainingConfigurations.AnyAsync(x => x.Id == DefaultConfigurationId, cancellationToken))
        {
            var configuration = new TrainingConfiguration
            {
                Id = DefaultConfigurationId,
                ScenarioId = ScenarioId,
                Name = "C2 baseline de demonstração",
                CurrentRevision = 1,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            var greenTiming = new GreenTimingRequest
            {
                Default = new GreenTimingDefault
                {
                    DecisionMinimumSeconds = greenProfile.GetProperty("decision_minimum_s").GetDouble(),
                    MaximumSeconds = greenProfile.GetProperty("maximum_s").GetDouble()
                }
            };
            var blocking = new BlockingParameters
            {
                ObservedCapacityFractionThreshold = blockingProfile.GetProperty("observed_capacity_fraction_threshold").GetDouble(),
                ConfirmationSamples = blockingProfile.GetProperty("blocked_confirmation_samples").GetInt32(),
                MaximumMeasurementAgeSeconds = blockingProfile.GetProperty("maximum_measurement_age_s").GetDouble()
            };
            var ppo = new PpoParameters();

            db.TrainingConfigurations.Add(configuration);
            db.TrainingConfigurationRevisions.Add(new TrainingConfigurationRevision
            {
                ConfigurationId = DefaultConfigurationId,
                Revision = 1,
                GreenTimingJson = JsonSerializer.Serialize(greenTiming, JsonOptions),
                BlockingJson = JsonSerializer.Serialize(blocking, JsonOptions),
                PpoJson = JsonSerializer.Serialize(ppo, JsonOptions),
                CreatedAtUtc = now
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static string Sha256(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
}
