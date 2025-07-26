using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartComply.Migrations
{
    /// <inheritdoc />
    public partial class AddShareToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ShareToken",
                table: "Audits",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShareToken",
                table: "Audits");
        }
    }
}
