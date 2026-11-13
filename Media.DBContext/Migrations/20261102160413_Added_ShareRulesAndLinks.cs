using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Media.DBContext.Migrations
{
    /// <inheritdoc />
    public partial class Added_ShareRulesAndLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "share_links",
                schema: "public",
                columns: table => new
                {
                    share_link_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    single_use = table.Column<bool>(type: "boolean", nullable: false),
                    used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    record_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_share_links", x => x.share_link_id);
                });

            migrationBuilder.CreateTable(
                name: "share_rules",
                schema: "public",
                columns: table => new
                {
                    share_rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_share_rules", x => x.share_rule_id);
                    table.ForeignKey(
                        name: "fk_share_rules_groups_group_id",
                        column: x => x.group_id,
                        principalSchema: "public",
                        principalTable: "groups",
                        principalColumn: "group_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "share_rules_albums",
                schema: "public",
                columns: table => new
                {
                    albums_album_id = table.Column<Guid>(type: "uuid", nullable: false),
                    share_rule_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_share_rules_albums", x => new { x.albums_album_id, x.share_rule_id });
                    table.ForeignKey(
                        name: "fk_share_rules_albums_albums_albums_album_id",
                        column: x => x.albums_album_id,
                        principalSchema: "public",
                        principalTable: "albums",
                        principalColumn: "album_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_share_rules_albums_share_rules_share_rule_id",
                        column: x => x.share_rule_id,
                        principalSchema: "public",
                        principalTable: "share_rules",
                        principalColumn: "share_rule_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "share_rules_artists",
                schema: "public",
                columns: table => new
                {
                    artists_artist_id = table.Column<Guid>(type: "uuid", nullable: false),
                    share_rule_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_share_rules_artists", x => new { x.artists_artist_id, x.share_rule_id });
                    table.ForeignKey(
                        name: "fk_share_rules_artists_artists_artists_artist_id",
                        column: x => x.artists_artist_id,
                        principalSchema: "public",
                        principalTable: "artists",
                        principalColumn: "artist_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_share_rules_artists_share_rules_share_rule_id",
                        column: x => x.share_rule_id,
                        principalSchema: "public",
                        principalTable: "share_rules",
                        principalColumn: "share_rule_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "share_rules_genres",
                schema: "public",
                columns: table => new
                {
                    genres_genre_id = table.Column<Guid>(type: "uuid", nullable: false),
                    share_rule_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_share_rules_genres", x => new { x.genres_genre_id, x.share_rule_id });
                    table.ForeignKey(
                        name: "fk_share_rules_genres_genres_genres_genre_id",
                        column: x => x.genres_genre_id,
                        principalSchema: "public",
                        principalTable: "genres",
                        principalColumn: "genre_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_share_rules_genres_share_rules_share_rule_id",
                        column: x => x.share_rule_id,
                        principalSchema: "public",
                        principalTable: "share_rules",
                        principalColumn: "share_rule_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_share_links_expires_at",
                schema: "public",
                table: "share_links",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_share_links_token_hash",
                schema: "public",
                table: "share_links",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_share_rules_group_id",
                schema: "public",
                table: "share_rules",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_share_rules_albums_share_rule_id",
                schema: "public",
                table: "share_rules_albums",
                column: "share_rule_id");

            migrationBuilder.CreateIndex(
                name: "ix_share_rules_artists_share_rule_id",
                schema: "public",
                table: "share_rules_artists",
                column: "share_rule_id");

            migrationBuilder.CreateIndex(
                name: "ix_share_rules_genres_share_rule_id",
                schema: "public",
                table: "share_rules_genres",
                column: "share_rule_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "share_links",
                schema: "public");

            migrationBuilder.DropTable(
                name: "share_rules_albums",
                schema: "public");

            migrationBuilder.DropTable(
                name: "share_rules_artists",
                schema: "public");

            migrationBuilder.DropTable(
                name: "share_rules_genres",
                schema: "public");

            migrationBuilder.DropTable(
                name: "share_rules",
                schema: "public");
        }
    }
}
