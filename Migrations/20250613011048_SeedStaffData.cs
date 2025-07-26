using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SmartComply.Migrations
{
    /// <inheritdoc />
    public partial class SeedStaffData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Staffs",
                columns: new[] { "StaffId", "StaffBranchId", "StaffEmail", "StaffIsActive", "StaffName", "StaffPassword", "StaffRole" },
                values: new object[,]
                {
                    { 12, null, "admin@gmail.com", true, "Admin", "AQAAAAIAAYagAAAAEFU3VYzAtINidq7KyBiehNaIuFHYTMzDVDb+Ar/dY5DQnARn6xNgckqWwUDrLe1plA==", "Admin" },
                    { 13, null, "manager@gmail.com", true, "Manager", "AQAAAAIAAYagAAAAEJBLqq+TcLFGRYUXS5RKYJfOA3jrpvtXf8oWzfIQ8lDBLi2GzwhAwQKF2BVm/9OW3w==", "Manager" },
                    { 14, 4, "user@gmail.com", true, "User", "AQAAAAIAAYagAAAAEE/oLb4iyXp5FZ4e3rbNVykNPFPWXHvG6+AdPJBVUpV5bL8ZqPsmvMPFaZY8D014fQ==", "User" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Staffs",
                keyColumn: "StaffId",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Staffs",
                keyColumn: "StaffId",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Staffs",
                keyColumn: "StaffId",
                keyValue: 14);
        }
    }
}
