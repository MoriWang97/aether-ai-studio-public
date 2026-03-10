using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiServiceApi.Migrations
{
    /// <inheritdoc />
    public partial class AddMysticAssistantEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MysticSessions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Question = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SessionData = table.Column<string>(type: "jsonb", nullable: false),
                    AnalysisResult = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MysticSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MysticSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MysticChatMessages",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    SessionId = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MysticChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MysticChatMessages_MysticSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "MysticSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MysticChatMessages_SessionId",
                table: "MysticChatMessages",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_MysticChatMessages_SessionId_Timestamp",
                table: "MysticChatMessages",
                columns: new[] { "SessionId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_MysticSessions_CreatedAt",
                table: "MysticSessions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MysticSessions_Type",
                table: "MysticSessions",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_MysticSessions_UserId",
                table: "MysticSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MysticSessions_UserId_Type_CreatedAt",
                table: "MysticSessions",
                columns: new[] { "UserId", "Type", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MysticChatMessages");

            migrationBuilder.DropTable(
                name: "MysticSessions");
        }
    }
}
