using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartComply.Migrations
{
    /// <inheritdoc />
    public partial class AddIsDeletedToCorrectiveAction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "CorrectiveActions",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "CorrectiveActions");
        }
    }
}
