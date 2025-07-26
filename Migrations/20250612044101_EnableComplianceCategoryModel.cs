using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartComply.Migrations
{
    /// <inheritdoc />
    public partial class EnableComplianceCategoryModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FormResponders_Audits_AuditId",
                table: "FormResponders");

            migrationBuilder.RenameColumn(
                name: "CategoryIsActive",
                table: "ComplianceCategories",
                newName: "CategoryIsEnabled");

            migrationBuilder.AlterColumn<int>(
                name: "AuditId",
                table: "FormResponders",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FormResponders_Audits_AuditId",
                table: "FormResponders",
                column: "AuditId",
                principalTable: "Audits",
                principalColumn: "AuditId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FormResponders_Audits_AuditId",
                table: "FormResponders");

            migrationBuilder.RenameColumn(
                name: "CategoryIsEnabled",
                table: "ComplianceCategories",
                newName: "CategoryIsActive");

            migrationBuilder.AlterColumn<int>(
                name: "AuditId",
                table: "FormResponders",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_FormResponders_Audits_AuditId",
                table: "FormResponders",
                column: "AuditId",
                principalTable: "Audits",
                principalColumn: "AuditId");
        }
    }
}
