using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FluxAI.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Scenarios",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    CatalogRevision = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    NetworkSha256 = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CatalogSha256 = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    TimingProfileId = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    TimingProfileSha256 = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scenarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SignalControllers",
                columns: table => new
                {
                    ScenarioId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ControllerId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    OsmId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    X = table.Column<double>(type: "REAL", nullable: false),
                    Y = table.Column<double>(type: "REAL", nullable: false),
                    IsAdaptive = table.Column<bool>(type: "INTEGER", nullable: false),
                    GreenPhaseIndexesJson = table.Column<string>(type: "TEXT", nullable: false),
                    RedClearancePhaseIndexesJson = table.Column<string>(type: "TEXT", nullable: false),
                    PhaseCatalogJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignalControllers", x => new { x.ScenarioId, x.ControllerId });
                    table.ForeignKey(
                        name: "FK_SignalControllers_Scenarios_ScenarioId",
                        column: x => x.ScenarioId,
                        principalTable: "Scenarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TrainingConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ScenarioId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    CurrentRevision = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingConfigurations", x => x.Id);
                    table.UniqueConstraint("AK_TrainingConfigurations_Id_ScenarioId", x => new { x.Id, x.ScenarioId });
                    table.ForeignKey(
                        name: "FK_TrainingConfigurations_Scenarios_ScenarioId",
                        column: x => x.ScenarioId,
                        principalTable: "Scenarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TrainingConfigurationRevisions",
                columns: table => new
                {
                    ConfigurationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Revision = table.Column<int>(type: "INTEGER", nullable: false),
                    GreenTimingJson = table.Column<string>(type: "TEXT", nullable: false),
                    BlockingJson = table.Column<string>(type: "TEXT", nullable: false),
                    PpoJson = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingConfigurationRevisions", x => new { x.ConfigurationId, x.Revision });
                    table.ForeignKey(
                        name: "FK_TrainingConfigurationRevisions_TrainingConfigurations_ConfigurationId",
                        column: x => x.ConfigurationId,
                        principalTable: "TrainingConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GreenPhaseOverrides",
                columns: table => new
                {
                    ConfigurationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Revision = table.Column<int>(type: "INTEGER", nullable: false),
                    ControllerId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PhaseIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    ScenarioId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DecisionMinimumSeconds = table.Column<double>(type: "REAL", nullable: true),
                    MaximumSeconds = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GreenPhaseOverrides", x => new { x.ConfigurationId, x.Revision, x.ControllerId, x.PhaseIndex });
                    table.CheckConstraint("CK_GreenPhaseOverrides_DecisionMinimum", "DecisionMinimumSeconds IS NULL OR DecisionMinimumSeconds > 0");
                    table.CheckConstraint("CK_GreenPhaseOverrides_Maximum", "MaximumSeconds IS NULL OR MaximumSeconds > 0");
                    table.ForeignKey(
                        name: "FK_GreenPhaseOverrides_SignalControllers_ScenarioId_ControllerId",
                        columns: x => new { x.ScenarioId, x.ControllerId },
                        principalTable: "SignalControllers",
                        principalColumns: new[] { "ScenarioId", "ControllerId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GreenPhaseOverrides_TrainingConfigurationRevisions_ConfigurationId_Revision",
                        columns: x => new { x.ConfigurationId, x.Revision },
                        principalTable: "TrainingConfigurationRevisions",
                        principalColumns: new[] { "ConfigurationId", "Revision" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GreenPhaseOverrides_ScenarioId_ControllerId",
                table: "GreenPhaseOverrides",
                columns: new[] { "ScenarioId", "ControllerId" });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingConfigurations_ScenarioId_Name",
                table: "TrainingConfigurations",
                columns: new[] { "ScenarioId", "Name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GreenPhaseOverrides");

            migrationBuilder.DropTable(
                name: "SignalControllers");

            migrationBuilder.DropTable(
                name: "TrainingConfigurationRevisions");

            migrationBuilder.DropTable(
                name: "TrainingConfigurations");

            migrationBuilder.DropTable(
                name: "Scenarios");
        }
    }
}
