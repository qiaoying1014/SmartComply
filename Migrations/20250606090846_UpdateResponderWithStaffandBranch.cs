using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartComply.Migrations
{
    /// <inheritdoc />
    public partial class UpdateResponderWithStaffandBranch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "FormResponders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StaffId",
                table: "FormResponders",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormResponders_BranchId",
                table: "FormResponders",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_FormResponders_StaffId",
                table: "FormResponders",
                column: "StaffId");

            migrationBuilder.AddForeignKey(
                name: "FK_FormResponders_Branches_BranchId",
                table: "FormResponders",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_FormResponders_Staffs_StaffId",
                table: "FormResponders",
                column: "StaffId",
                principalTable: "Staffs",
                principalColumn: "StaffId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FormResponders_Branches_BranchId",
                table: "FormResponders");

            migrationBuilder.DropForeignKey(
                name: "FK_FormResponders_Staffs_StaffId",
                table: "FormResponders");

            migrationBuilder.DropIndex(
                name: "IX_FormResponders_BranchId",
                table: "FormResponders");

            migrationBuilder.DropIndex(
                name: "IX_FormResponders_StaffId",
                table: "FormResponders");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "FormResponders");

            migrationBuilder.DropColumn(
                name: "StaffId",
                table: "FormResponders");
        }
    }
}
