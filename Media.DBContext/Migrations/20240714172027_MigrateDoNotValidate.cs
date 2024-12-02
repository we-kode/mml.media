using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Media.DBContext.Migrations
{
  public partial class MigrateDoNotValidate : Migration
  {
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.UpdateData(
        table: "settings",
        keyColumn: "value",
        keyValue: "dontValidate",
        column: "value",
        value: "doNotValidate",
        schema: "public"
      );
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.UpdateData(
        table: "settings",
        keyColumn: "value",
        keyValue: "doNotValidate",
        column: "value",
        value: "dontValidate",
        schema: "public"
      );
    }
  }
}
