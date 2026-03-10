using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiServiceApi.Migrations
{
    /// <inheritdoc />
    public partial class AddLegalAssistantEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LegalCases",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CaseType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CaseData = table.Column<string>(type: "jsonb", nullable: false),
                    AnalysisResult = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegalCases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LegalCases_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LegalEvidences",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    CaseId = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    FileUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    Transcript = table.Column<string>(type: "text", nullable: true),
                    ExtractedInfo = table.Column<string>(type: "text", nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: true),
                    MimeType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegalEvidences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LegalEvidences_LegalCases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "LegalCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LegalCases_CaseType",
                table: "LegalCases",
                column: "CaseType");

            migrationBuilder.CreateIndex(
                name: "IX_LegalCases_CreatedAt",
                table: "LegalCases",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LegalCases_Status",
                table: "LegalCases",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_LegalCases_UserId",
                table: "LegalCases",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LegalCases_UserId_IsDeleted_CaseType",
                table: "LegalCases",
                columns: new[] { "UserId", "IsDeleted", "CaseType" });

            migrationBuilder.CreateIndex(
                name: "IX_LegalEvidences_CaseId",
                table: "LegalEvidences",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_LegalEvidences_CreatedAt",
                table: "LegalEvidences",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LegalEvidences_Type",
                table: "LegalEvidences",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LegalEvidences");

            migrationBuilder.DropTable(
                name: "LegalCases");
        }
    }
}
