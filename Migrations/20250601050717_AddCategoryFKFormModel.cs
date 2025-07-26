using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartComply.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryFKFormModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "Forms");

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "Forms",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Forms_CategoryId",
                table: "Forms",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Forms_ComplianceCategories_CategoryId",
                table: "Forms",
                column: "CategoryId",
                principalTable: "ComplianceCategories",
                principalColumn: "CategoryId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Forms_ComplianceCategories_CategoryId",
                table: "Forms");

            migrationBuilder.DropIndex(
                name: "IX_Forms_CategoryId",
                table: "Forms");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Forms");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Forms",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
