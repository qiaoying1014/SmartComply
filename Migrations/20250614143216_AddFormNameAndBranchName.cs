using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartComply.Migrations
{
    /// <inheritdoc />
    public partial class AddFormNameAndBranchName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FormName",
                table: "Forms",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BranchName",
                table: "Branches",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FormName",
                table: "Forms");

            migrationBuilder.DropColumn(
                name: "BranchName",
                table: "Branches");
        }
    }
}
