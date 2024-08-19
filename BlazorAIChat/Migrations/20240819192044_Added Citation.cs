using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorAIChat.Migrations
{
    /// <inheritdoc />
    public partial class AddedCitation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Citations",
                table: "Messages",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Citations",
                table: "Messages");
        }
    }
}
