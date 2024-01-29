using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Media.DBContext.Migrations
{
  public partial class Added_Cover : Migration
  {
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.AddColumn<string>(
          name: "cover",
          schema: "public",
          table: "records",
          type: "text",
          nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropColumn(
          name: "cover",
          schema: "public",
          table: "records");
    }
  }
}
