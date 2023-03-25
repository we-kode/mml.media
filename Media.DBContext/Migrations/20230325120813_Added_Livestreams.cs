using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Media.DBContext.Migrations
{
    public partial class Added_Livestreams : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "livestreams",
                schema: "public",
                columns: table => new
                {
                    livestream_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: false),
                    url = table.Column<string>(type: "text", nullable: true),
                    provider_type = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_livestreams", x => x.livestream_id);
                });

            migrationBuilder.CreateTable(
                name: "groups_livestreams",
                schema: "public",
                columns: table => new
                {
                    groups_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    livestreams_livestream_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_groups_livestreams", x => new { x.groups_group_id, x.livestreams_livestream_id });
                    table.ForeignKey(
                        name: "fk_groups_livestreams_groups_groups_group_id",
                        column: x => x.groups_group_id,
                        principalSchema: "public",
                        principalTable: "groups",
                        principalColumn: "group_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_groups_livestreams_livestreams_livestreams_livestream_id",
                        column: x => x.livestreams_livestream_id,
                        principalSchema: "public",
                        principalTable: "livestreams",
                        principalColumn: "livestream_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_groups_livestreams_livestreams_livestream_id",
                schema: "public",
                table: "groups_livestreams",
                column: "livestreams_livestream_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "groups_livestreams",
                schema: "public");

            migrationBuilder.DropTable(
                name: "livestreams",
                schema: "public");
        }
    }
}
