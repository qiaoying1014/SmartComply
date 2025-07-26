using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartComply.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedDbContextChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Staffs_Branch_StaffBranchId",
                table: "Staffs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Branch",
                table: "Branch");

            migrationBuilder.RenameTable(
                name: "Branch",
                newName: "Branches");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Branches",
                table: "Branches",
                column: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_Staffs_Branches_StaffBranchId",
                table: "Staffs",
                column: "StaffBranchId",
                principalTable: "Branches",
                principalColumn: "BranchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Staffs_Branches_StaffBranchId",
                table: "Staffs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Branches",
                table: "Branches");

            migrationBuilder.RenameTable(
                name: "Branches",
                newName: "Branch");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Branch",
                table: "Branch",
                column: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_Staffs_Branch_StaffBranchId",
                table: "Staffs",
                column: "StaffBranchId",
                principalTable: "Branch",
                principalColumn: "BranchId");
        }
    }
}
