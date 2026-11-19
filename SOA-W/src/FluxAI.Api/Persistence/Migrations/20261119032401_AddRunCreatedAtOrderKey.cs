using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FluxAI.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRunCreatedAtOrderKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrainingRuns_CreatedAtUtc_Id",
                table: "TrainingRuns");

            migrationBuilder.AddColumn<long>(
                name: "CreatedAtOrderKey",
                table: "TrainingRuns",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingRuns_CreatedAtOrderKey_Id",
                table: "TrainingRuns",
                columns: new[] { "CreatedAtOrderKey", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrainingRuns_CreatedAtOrderKey_Id",
                table: "TrainingRuns");

            migrationBuilder.DropColumn(
                name: "CreatedAtOrderKey",
                table: "TrainingRuns");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingRuns_CreatedAtUtc_Id",
                table: "TrainingRuns",
                columns: new[] { "CreatedAtUtc", "Id" });
        }
    }
}
