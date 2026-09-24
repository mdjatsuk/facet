using FacetApi.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FacetApi.Migrations;

[DbContext(typeof(FacetDbContext))]
[Migration("20260924000000_RemoveLegacyDemoAdmin")]
public partial class RemoveLegacyDemoAdmin : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DELETE FROM \"Users\" WHERE \"Id\" = 1 AND \"Username\" = 'admin' AND \"Role\" = 'Admin' AND \"Salt\" IS NULL;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Do not recreate the predictable demo administrator when rolling back.
    }
}
