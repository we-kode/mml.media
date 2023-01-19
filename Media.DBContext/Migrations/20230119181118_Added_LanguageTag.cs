using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Media.DBContext.Migrations
{
    public partial class Added_LanguageTag : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "language_id",
                schema: "public",
                table: "records",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "languages",
                schema: "public",
                columns: table => new
                {
                    language_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_languages", x => x.language_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_records_language_id",
                schema: "public",
                table: "records",
                column: "language_id");

            migrationBuilder.AddForeignKey(
                name: "fk_records_languages_language_id",
                schema: "public",
                table: "records",
                column: "language_id",
                principalSchema: "public",
                principalTable: "languages",
                principalColumn: "language_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_records_languages_language_id",
                schema: "public",
                table: "records");

            migrationBuilder.DropTable(
                name: "languages",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "ix_records_language_id",
                schema: "public",
                table: "records");

            migrationBuilder.DropColumn(
                name: "language_id",
                schema: "public",
                table: "records");
        }
    }
}
