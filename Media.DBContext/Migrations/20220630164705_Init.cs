using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Media.DBContext.Migrations
{
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateTable(
                name: "albums",
                schema: "public",
                columns: table => new
                {
                    album_id = table.Column<Guid>(type: "uuid", nullable: false),
                    album_name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_albums", x => x.album_id);
                });

            migrationBuilder.CreateTable(
                name: "artists",
                schema: "public",
                columns: table => new
                {
                    artist_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_artists", x => x.artist_id);
                });

            migrationBuilder.CreateTable(
                name: "genres",
                schema: "public",
                columns: table => new
                {
                    genre_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_genres", x => x.genre_id);
                });

            migrationBuilder.CreateTable(
                name: "groups",
                schema: "public",
                columns: table => new
                {
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_groups", x => x.group_id);
                });

            migrationBuilder.CreateTable(
                name: "settings",
                schema: "public",
                columns: table => new
                {
                    setting_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_settings", x => x.setting_id);
                });

            migrationBuilder.CreateTable(
                name: "records",
                schema: "public",
                columns: table => new
                {
                    record_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mime_type = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    track_number = table.Column<int>(type: "integer", nullable: false),
                    artist_id = table.Column<Guid>(type: "uuid", nullable: true),
                    genre_id = table.Column<Guid>(type: "uuid", nullable: true),
                    album_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_records", x => x.record_id);
                    table.ForeignKey(
                        name: "fk_records_albums_album_id",
                        column: x => x.album_id,
                        principalSchema: "public",
                        principalTable: "albums",
                        principalColumn: "album_id");
                    table.ForeignKey(
                        name: "fk_records_artists_artist_id",
                        column: x => x.artist_id,
                        principalSchema: "public",
                        principalTable: "artists",
                        principalColumn: "artist_id");
                    table.ForeignKey(
                        name: "fk_records_genres_genre_id",
                        column: x => x.genre_id,
                        principalSchema: "public",
                        principalTable: "genres",
                        principalColumn: "genre_id");
                });

            migrationBuilder.CreateTable(
                name: "groups_records",
                schema: "public",
                columns: table => new
                {
                    groups_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    records_record_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_groups_records", x => new { x.groups_group_id, x.records_record_id });
                    table.ForeignKey(
                        name: "fk_groups_records_groups_groups_group_id",
                        column: x => x.groups_group_id,
                        principalSchema: "public",
                        principalTable: "groups",
                        principalColumn: "group_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_groups_records_records_records_record_id",
                        column: x => x.records_record_id,
                        principalSchema: "public",
                        principalTable: "records",
                        principalColumn: "record_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_groups_records_records_record_id",
                schema: "public",
                table: "groups_records",
                column: "records_record_id");

            migrationBuilder.CreateIndex(
                name: "ix_records_album_id",
                schema: "public",
                table: "records",
                column: "album_id");

            migrationBuilder.CreateIndex(
                name: "ix_records_artist_id",
                schema: "public",
                table: "records",
                column: "artist_id");

            migrationBuilder.CreateIndex(
                name: "ix_records_genre_id",
                schema: "public",
                table: "records",
                column: "genre_id");

            migrationBuilder.CreateIndex(
                name: "ix_settings_key",
                schema: "public",
                table: "settings",
                column: "key",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "groups_records",
                schema: "public");

            migrationBuilder.DropTable(
                name: "settings",
                schema: "public");

            migrationBuilder.DropTable(
                name: "groups",
                schema: "public");

            migrationBuilder.DropTable(
                name: "records",
                schema: "public");

            migrationBuilder.DropTable(
                name: "albums",
                schema: "public");

            migrationBuilder.DropTable(
                name: "artists",
                schema: "public");

            migrationBuilder.DropTable(
                name: "genres",
                schema: "public");
        }
    }
}
