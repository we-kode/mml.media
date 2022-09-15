using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Media.DBContext.Migrations
{
  public partial class Added_RecordFilePath : Migration
  {
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.AddColumn<string>(
          name: "file_path",
          schema: "public",
          table: "records",
          type: "text",
          nullable: false,
          defaultValue: "");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropColumn(
          name: "file_path",
          schema: "public",
          table: "records");
    }
  }
}
