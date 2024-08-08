using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace Media.DBContext.Migrations
{
  public partial class Added_Artist_CreationDate : Migration
  {
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "created_at",
            schema: "public",
            table: "artists",
            type: "timestamp with time zone",
            nullable: false,
            defaultValue: DateTime.UtcNow);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "created_at",
            schema: "public",
            table: "artists");
    }
  }
}
