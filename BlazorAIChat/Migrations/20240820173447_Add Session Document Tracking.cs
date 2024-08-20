using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorAIChat.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionDocumentTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SessionDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SessionId = table.Column<string>(type: "TEXT", nullable: false),
                    DocId = table.Column<string>(type: "TEXT", nullable: false),
                    FileNameOrUrl = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionDocuments_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SessionDocuments_SessionId",
                table: "SessionDocuments",
                column: "SessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SessionDocuments");
        }
    }
}
