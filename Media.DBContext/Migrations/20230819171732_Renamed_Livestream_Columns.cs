using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Media.DBContext.Migrations
{
    public partial class Renamed_Livestream_Columns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_groups_livestreams_livestreams_livestreams_livestream_id",
                schema: "public",
                table: "groups_livestreams");

            migrationBuilder.RenameColumn(
                name: "display_name",
                schema: "public",
                table: "livestreams",
                newName: "title");

            migrationBuilder.RenameColumn(
                name: "livestream_id",
                schema: "public",
                table: "livestreams",
                newName: "record_id");

            migrationBuilder.RenameColumn(
                name: "livestreams_livestream_id",
                schema: "public",
                table: "groups_livestreams",
                newName: "livestreams_record_id");

            migrationBuilder.RenameIndex(
                name: "ix_groups_livestreams_livestreams_livestream_id",
                schema: "public",
                table: "groups_livestreams",
                newName: "ix_groups_livestreams_livestreams_record_id");

            migrationBuilder.AddForeignKey(
                name: "fk_groups_livestreams_livestreams_livestreams_record_id",
                schema: "public",
                table: "groups_livestreams",
                column: "livestreams_record_id",
                principalSchema: "public",
                principalTable: "livestreams",
                principalColumn: "record_id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_groups_livestreams_livestreams_livestreams_record_id",
                schema: "public",
                table: "groups_livestreams");

            migrationBuilder.RenameColumn(
                name: "title",
                schema: "public",
                table: "livestreams",
                newName: "display_name");

            migrationBuilder.RenameColumn(
                name: "record_id",
                schema: "public",
                table: "livestreams",
                newName: "livestream_id");

            migrationBuilder.RenameColumn(
                name: "livestreams_record_id",
                schema: "public",
                table: "groups_livestreams",
                newName: "livestreams_livestream_id");

            migrationBuilder.RenameIndex(
                name: "ix_groups_livestreams_livestreams_record_id",
                schema: "public",
                table: "groups_livestreams",
                newName: "ix_groups_livestreams_livestreams_livestream_id");

            migrationBuilder.AddForeignKey(
                name: "fk_groups_livestreams_livestreams_livestreams_livestream_id",
                schema: "public",
                table: "groups_livestreams",
                column: "livestreams_livestream_id",
                principalSchema: "public",
                principalTable: "livestreams",
                principalColumn: "livestream_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
