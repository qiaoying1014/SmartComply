using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SmartComply.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StaffBranchId",
                table: "Staffs",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Branch",
                columns: table => new
                {
                    BranchId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BranchAddress = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branch", x => x.BranchId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Staffs_StaffBranchId",
                table: "Staffs",
                column: "StaffBranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_Staffs_Branch_StaffBranchId",
                table: "Staffs",
                column: "StaffBranchId",
                principalTable: "Branch",
                principalColumn: "BranchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Staffs_Branch_StaffBranchId",
                table: "Staffs");

            migrationBuilder.DropTable(
                name: "Branch");

            migrationBuilder.DropIndex(
                name: "IX_Staffs_StaffBranchId",
                table: "Staffs");

            migrationBuilder.DropColumn(
                name: "StaffBranchId",
                table: "Staffs");
        }
    }
}
