using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FluxAI.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddImportedRuns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrainingRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SourceRunId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ScenarioId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    RunType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Origin = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Environment = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Seed = table.Column<long>(type: "INTEGER", nullable: false),
                    HorizonSeconds = table.Column<double>(type: "REAL", nullable: false),
                    ModelJson = table.Column<string>(type: "TEXT", nullable: true),
                    EffectiveParametersJson = table.Column<string>(type: "TEXT", nullable: false),
                    TrafficSummaryJson = table.Column<string>(type: "TEXT", nullable: false),
                    MetricCoverageJson = table.Column<string>(type: "TEXT", nullable: true),
                    ScenarioFingerprintJson = table.Column<string>(type: "TEXT", nullable: true),
                    LimitationsJson = table.Column<string>(type: "TEXT", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    PayloadSha256 = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    FinishedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingRuns_Scenarios_ScenarioId",
                        column: x => x.ScenarioId,
                        principalTable: "Scenarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RunArtifacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TrainingRunId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LogicalName = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Sha256 = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    SizeBytes = table.Column<long>(type: "INTEGER", nullable: true),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RunArtifacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RunArtifacts_TrainingRuns_TrainingRunId",
                        column: x => x.TrainingRunId,
                        principalTable: "TrainingRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RunMetrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TrainingRunId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MetricName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Value = table.Column<double>(type: "REAL", nullable: false),
                    Unit = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    ScopeType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    SignalControllerId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Episode = table.Column<int>(type: "INTEGER", nullable: true),
                    Step = table.Column<long>(type: "INTEGER", nullable: true),
                    SimulationTimeSeconds = table.Column<double>(type: "REAL", nullable: true),
                    SourceKey = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RunMetrics", x => x.Id);
                    table.CheckConstraint("CK_RunMetrics_SimulationTime", "SimulationTimeSeconds IS NULL OR SimulationTimeSeconds >= 0");
                    table.CheckConstraint("CK_RunMetrics_ValueFiniteRange", "Value > -1.0e308 AND Value < 1.0e308");
                    table.ForeignKey(
                        name: "FK_RunMetrics_TrainingRuns_TrainingRunId",
                        column: x => x.TrainingRunId,
                        principalTable: "TrainingRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RunArtifacts_TrainingRunId_LogicalName",
                table: "RunArtifacts",
                columns: new[] { "TrainingRunId", "LogicalName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RunMetrics_TrainingRunId_MetricName_ScopeType_SignalControllerId_SimulationTimeSeconds",
                table: "RunMetrics",
                columns: new[] { "TrainingRunId", "MetricName", "ScopeType", "SignalControllerId", "SimulationTimeSeconds" });

            migrationBuilder.CreateIndex(
                name: "IX_RunMetrics_TrainingRunId_SourceKey",
                table: "RunMetrics",
                columns: new[] { "TrainingRunId", "SourceKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingRuns_CreatedAtUtc_Id",
                table: "TrainingRuns",
                columns: new[] { "CreatedAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingRuns_IdempotencyKey",
                table: "TrainingRuns",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingRuns_ScenarioId_SourceRunId",
                table: "TrainingRuns",
                columns: new[] { "ScenarioId", "SourceRunId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RunArtifacts");

            migrationBuilder.DropTable(
                name: "RunMetrics");

            migrationBuilder.DropTable(
                name: "TrainingRuns");
        }
    }
}
