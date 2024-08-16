using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorAIChat.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutomaticAccountApproval",
                table: "Config",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutomaticAccountApproval",
                table: "Config");
        }
    }
}
