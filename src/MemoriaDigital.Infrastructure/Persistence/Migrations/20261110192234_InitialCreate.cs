using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemoriaDigital.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Analyses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    RiskLevel = table.Column<string>(type: "TEXT", nullable: true),
                    FailureReason = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Analyses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AnalysisInputs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Content = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    Reference = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    MediaType = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    AnalysisId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalysisInputs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnalysisInputs_Analyses_AnalysisId",
                        column: x => x.AnalysisId,
                        principalTable: "Analyses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Indicators",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Severity = table.Column<string>(type: "TEXT", nullable: false),
                    Evidence = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    DetectedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    AnalysisId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Indicators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Indicators_Analyses_AnalysisId",
                        column: x => x.AnalysisId,
                        principalTable: "Analyses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Recommendations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Priority = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    AnalysisId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recommendations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Recommendations_Analyses_AnalysisId",
                        column: x => x.AnalysisId,
                        principalTable: "Analyses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisInputs_AnalysisId",
                table: "AnalysisInputs",
                column: "AnalysisId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Indicators_AnalysisId",
                table: "Indicators",
                column: "AnalysisId");

            migrationBuilder.CreateIndex(
                name: "IX_Recommendations_AnalysisId",
                table: "Recommendations",
                column: "AnalysisId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnalysisInputs");

            migrationBuilder.DropTable(
                name: "Indicators");

            migrationBuilder.DropTable(
                name: "Recommendations");

            migrationBuilder.DropTable(
                name: "Analyses");
        }
    }
}
