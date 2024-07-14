using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Media.DBContext.Migrations
{
  public partial class AddedCover : Migration
  {
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.AddColumn<string>(
          name: "cover",
          schema: "public",
          table: "albums",
          type: "text",
          nullable: true);

      migrationBuilder.AddColumn<string>(
          name: "cover",
          schema: "public",
          table: "artists",
          type: "text",
          nullable: true);

      migrationBuilder.AddColumn<string>(
          name: "cover",
          schema: "public",
          table: "genres",
          type: "text",
          nullable: true);

      migrationBuilder.AddColumn<string>(
          name: "cover",
          schema: "public",
          table: "livestreams",
          type: "text",
          nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropColumn(
          name: "cover",
          schema: "public",
          table: "albums");

      migrationBuilder.AddColumn<string>(
          name: "cover",
          schema: "public",
          table: "artists");

      migrationBuilder.AddColumn<string>(
          name: "cover",
          schema: "public",
          table: "genres");

      migrationBuilder.AddColumn<string>(
          name: "cover",
          schema: "public",
          table: "livestreams");
    }
  }
}
