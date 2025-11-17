using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FacetApi.Migrations
{
    /// <inheritdoc />
    public partial class addAdminRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Password", "Role", "Username" },
                values: new object[] { "admin1", "Admin", "admin" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Password", "Role", "Username" },
                values: new object[] { "testpass", "User", "testuser" });
        }
    }
}
