using FluxAI.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace FluxAI.Api.Persistence;

public sealed class FluxAiDbContext(DbContextOptions<FluxAiDbContext> options) : DbContext(options)
{
    public DbSet<Scenario> Scenarios => Set<Scenario>();
    public DbSet<SignalController> SignalControllers => Set<SignalController>();
    public DbSet<TrainingConfiguration> TrainingConfigurations => Set<TrainingConfiguration>();
    public DbSet<TrainingConfigurationRevision> TrainingConfigurationRevisions => Set<TrainingConfigurationRevision>();
    public DbSet<GreenPhaseOverride> GreenPhaseOverrides => Set<GreenPhaseOverride>();
    public DbSet<TrainingRun> TrainingRuns => Set<TrainingRun>();
    public DbSet<RunMetric> RunMetrics => Set<RunMetric>();
    public DbSet<RunArtifact> RunArtifacts => Set<RunArtifact>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Scenario>(entity =>
        {
            entity.ToTable("Scenarios");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(100);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Environment).HasMaxLength(30).IsRequired();
            entity.Property(x => x.CatalogRevision).HasMaxLength(120).IsRequired();
            entity.Property(x => x.NetworkSha256).HasMaxLength(64).IsRequired();
            entity.Property(x => x.CatalogSha256).HasMaxLength(64).IsRequired();
            entity.Property(x => x.TimingProfileId).HasMaxLength(120).IsRequired();
            entity.Property(x => x.TimingProfileSha256).HasMaxLength(64).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
        });

        modelBuilder.Entity<SignalController>(entity =>
        {
            entity.ToTable("SignalControllers");
            entity.HasKey(x => new { x.ScenarioId, x.ControllerId });
            entity.Property(x => x.ScenarioId).HasMaxLength(100);
            entity.Property(x => x.ControllerId).HasMaxLength(100);
            entity.Property(x => x.OsmId).HasMaxLength(100).IsRequired();
            entity.Property(x => x.GreenPhaseIndexesJson).IsRequired();
            entity.Property(x => x.RedClearancePhaseIndexesJson).IsRequired();
            entity.Property(x => x.PhaseCatalogJson).IsRequired();
            entity.HasOne<Scenario>().WithMany().HasForeignKey(x => x.ScenarioId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TrainingConfiguration>(entity =>
        {
            entity.ToTable("TrainingConfigurations");
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.Id, x.ScenarioId });
            entity.Property(x => x.ScenarioId).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.CurrentRevision).IsConcurrencyToken();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.HasOne<Scenario>().WithMany().HasForeignKey(x => x.ScenarioId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => new { x.ScenarioId, x.Name });
        });

        modelBuilder.Entity<TrainingConfigurationRevision>(entity =>
        {
            entity.ToTable("TrainingConfigurationRevisions");
            entity.HasKey(x => new { x.ConfigurationId, x.Revision });
            entity.Property(x => x.GreenTimingJson).IsRequired();
            entity.Property(x => x.BlockingJson).IsRequired();
            entity.Property(x => x.PpoJson).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.HasOne<TrainingConfiguration>().WithMany()
                .HasForeignKey(x => x.ConfigurationId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GreenPhaseOverride>(entity =>
        {
            entity.ToTable("GreenPhaseOverrides", table =>
            {
                table.HasCheckConstraint("CK_GreenPhaseOverrides_DecisionMinimum", "DecisionMinimumSeconds IS NULL OR DecisionMinimumSeconds > 0");
                table.HasCheckConstraint("CK_GreenPhaseOverrides_Maximum", "MaximumSeconds IS NULL OR MaximumSeconds > 0");
            });
            entity.HasKey(x => new { x.ConfigurationId, x.Revision, x.ControllerId, x.PhaseIndex });
            entity.Property(x => x.ScenarioId).HasMaxLength(100).IsRequired();
            entity.Property(x => x.ControllerId).HasMaxLength(100).IsRequired();
            entity.HasOne<TrainingConfigurationRevision>().WithMany()
                .HasForeignKey(x => new { x.ConfigurationId, x.Revision })
                .HasPrincipalKey(x => new { x.ConfigurationId, x.Revision })
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<SignalController>().WithMany()
                .HasForeignKey(x => new { x.ScenarioId, x.ControllerId })
                .HasPrincipalKey(x => new { x.ScenarioId, x.ControllerId })
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TrainingRun>(entity =>
        {
            entity.ToTable("TrainingRuns");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.SourceRunId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.ScenarioId).HasMaxLength(100).IsRequired();
            entity.Property(x => x.RunType).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Origin).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Environment).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.Property(x => x.EffectiveParametersJson).IsRequired();
            entity.Property(x => x.TrafficSummaryJson).IsRequired();
            entity.Property(x => x.LimitationsJson).IsRequired();
            entity.Property(x => x.IdempotencyKey).HasMaxLength(128).IsRequired();
            entity.Property(x => x.PayloadSha256).HasMaxLength(64).IsRequired();
            entity.HasOne<Scenario>().WithMany().HasForeignKey(x => x.ScenarioId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => new { x.ScenarioId, x.SourceRunId }).IsUnique();
            entity.HasIndex(x => x.IdempotencyKey).IsUnique();
            entity.HasIndex(x => new { x.CreatedAtOrderKey, x.Id });
        });

        modelBuilder.Entity<RunMetric>(entity =>
        {
            entity.ToTable("RunMetrics", table =>
            {
                table.HasCheckConstraint("CK_RunMetrics_ValueFiniteRange", "Value > -1.0e308 AND Value < 1.0e308");
                table.HasCheckConstraint("CK_RunMetrics_SimulationTime", "SimulationTimeSeconds IS NULL OR SimulationTimeSeconds >= 0");
            });
            entity.HasKey(x => x.Id);
            entity.Property(x => x.MetricName).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Unit).HasMaxLength(40).IsRequired();
            entity.Property(x => x.ScopeType).HasMaxLength(30).IsRequired();
            entity.Property(x => x.SignalControllerId).HasMaxLength(100);
            entity.Property(x => x.SourceKey).HasMaxLength(200).IsRequired();
            entity.HasOne<TrainingRun>().WithMany().HasForeignKey(x => x.TrainingRunId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.TrainingRunId, x.SourceKey }).IsUnique();
            entity.HasIndex(x => new { x.TrainingRunId, x.MetricName, x.ScopeType, x.SignalControllerId, x.SimulationTimeSeconds });
        });

        modelBuilder.Entity<RunArtifact>(entity =>
        {
            entity.ToTable("RunArtifacts");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.LogicalName).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Sha256).HasMaxLength(64).IsRequired();
            entity.Property(x => x.ContentType).HasMaxLength(120);
            entity.HasOne<TrainingRun>().WithMany().HasForeignKey(x => x.TrainingRunId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.TrainingRunId, x.LogicalName }).IsUnique();
        });
    }
}
