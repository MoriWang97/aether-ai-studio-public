using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiServiceApi.Migrations
{
    /// <inheritdoc />
    public partial class AddUsageStatistics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UsageLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Module = table.Column<int>(type: "integer", nullable: false),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RequestPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    HttpMethod = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsSuccess = table.Column<bool>(type: "boolean", nullable: false),
                    StatusCode = table.Column<int>(type: "integer", nullable: true),
                    ResponseTimeMs = table.Column<int>(type: "integer", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsageLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsageLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UsageLogs_Action",
                table: "UsageLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_UsageLogs_IsSuccess",
                table: "UsageLogs",
                column: "IsSuccess");

            migrationBuilder.CreateIndex(
                name: "IX_UsageLogs_Module",
                table: "UsageLogs",
                column: "Module");

            migrationBuilder.CreateIndex(
                name: "IX_UsageLogs_Module_Timestamp",
                table: "UsageLogs",
                columns: new[] { "Module", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_UsageLogs_Timestamp",
                table: "UsageLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_UsageLogs_Timestamp_Module_UserId",
                table: "UsageLogs",
                columns: new[] { "Timestamp", "Module", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_UsageLogs_UserId",
                table: "UsageLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UsageLogs_UserId_Timestamp",
                table: "UsageLogs",
                columns: new[] { "UserId", "Timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UsageLogs");
        }
    }
}
