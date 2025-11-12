using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FacetApi.Migrations
{
    /// <inheritdoc />
    public partial class AddIsTemporaryField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsTemporary",
                table: "Documents",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsTemporary",
                table: "Documents");
        }
    }
}
