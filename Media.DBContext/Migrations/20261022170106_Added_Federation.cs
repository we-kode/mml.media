using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Media.DBContext.Migrations
{
    /// <inheritdoc />
    public partial class Added_Federation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "federation_audit_log",
                schema: "public",
                columns: table => new
                {
                    entry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    action = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    source = table.Column<string>(type: "text", nullable: false),
                    target = table.Column<string>(type: "text", nullable: false),
                    transaction_id = table.Column<Guid>(type: "uuid", nullable: true),
                    record_ids = table.Column<string>(type: "jsonb", nullable: false),
                    message = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_federation_audit_log", x => x.entry_id);
                });

            migrationBuilder.CreateTable(
                name: "federation_invitations",
                schema: "public",
                columns: table => new
                {
                    invitation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code_hash = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    used_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_federation_invitations", x => x.invitation_id);
                });

            migrationBuilder.CreateTable(
                name: "federation_peers",
                schema: "public",
                columns: table => new
                {
                    peer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    instance = table.Column<string>(type: "text", nullable: false),
                    base_url = table.Column<string>(type: "text", nullable: false),
                    endpoint = table.Column<string>(type: "text", nullable: false),
                    jwks_uri = table.Column<string>(type: "text", nullable: false),
                    jwks = table.Column<string>(type: "text", nullable: true),
                    jwks_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    state = table.Column<int>(type: "integer", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_federation_peers", x => x.peer_id);
                });

            migrationBuilder.CreateTable(
                name: "federation_rules",
                schema: "public",
                columns: table => new
                {
                    rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    direction = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_federation_rules", x => x.rule_id);
                });

            migrationBuilder.CreateTable(
                name: "federation_jobs",
                schema: "public",
                columns: table => new
                {
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    peer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    record_id = table.Column<Guid>(type: "uuid", nullable: true),
                    transaction_id = table.Column<Guid>(type: "uuid", nullable: true),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    next_attempt_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_error = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_federation_jobs", x => x.job_id);
                    table.ForeignKey(
                        name: "fk_federation_jobs_federation_peers_peer_id",
                        column: x => x.peer_id,
                        principalSchema: "public",
                        principalTable: "federation_peers",
                        principalColumn: "peer_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "federation_record_links",
                schema: "public",
                columns: table => new
                {
                    link_id = table.Column<Guid>(type: "uuid", nullable: false),
                    peer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    local_record_id = table.Column<Guid>(type: "uuid", nullable: false),
                    remote_record_id = table.Column<Guid>(type: "uuid", nullable: false),
                    direction = table.Column<int>(type: "integer", nullable: false),
                    transaction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_federation_record_links", x => x.link_id);
                    table.ForeignKey(
                        name: "fk_federation_record_links_federation_peers_peer_id",
                        column: x => x.peer_id,
                        principalSchema: "public",
                        principalTable: "federation_peers",
                        principalColumn: "peer_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_federation_record_links_records_local_record_id",
                        column: x => x.local_record_id,
                        principalSchema: "public",
                        principalTable: "records",
                        principalColumn: "record_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "federation_rules_albums",
                schema: "public",
                columns: table => new
                {
                    albums_album_id = table.Column<Guid>(type: "uuid", nullable: false),
                    federation_rule_rule_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_federation_rules_albums", x => new { x.albums_album_id, x.federation_rule_rule_id });
                    table.ForeignKey(
                        name: "fk_federation_rules_albums_albums_albums_album_id",
                        column: x => x.albums_album_id,
                        principalSchema: "public",
                        principalTable: "albums",
                        principalColumn: "album_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_federation_rules_albums_federation_rules_federation_rule_ru",
                        column: x => x.federation_rule_rule_id,
                        principalSchema: "public",
                        principalTable: "federation_rules",
                        principalColumn: "rule_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "federation_rules_artists",
                schema: "public",
                columns: table => new
                {
                    artists_artist_id = table.Column<Guid>(type: "uuid", nullable: false),
                    federation_rule_rule_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_federation_rules_artists", x => new { x.artists_artist_id, x.federation_rule_rule_id });
                    table.ForeignKey(
                        name: "fk_federation_rules_artists_artists_artists_artist_id",
                        column: x => x.artists_artist_id,
                        principalSchema: "public",
                        principalTable: "artists",
                        principalColumn: "artist_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_federation_rules_artists_federation_rules_federation_rule_r",
                        column: x => x.federation_rule_rule_id,
                        principalSchema: "public",
                        principalTable: "federation_rules",
                        principalColumn: "rule_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "federation_rules_genres",
                schema: "public",
                columns: table => new
                {
                    federation_rule_rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    genres_genre_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_federation_rules_genres", x => new { x.federation_rule_rule_id, x.genres_genre_id });
                    table.ForeignKey(
                        name: "fk_federation_rules_genres_federation_rules_federation_rule_ru",
                        column: x => x.federation_rule_rule_id,
                        principalSchema: "public",
                        principalTable: "federation_rules",
                        principalColumn: "rule_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_federation_rules_genres_genres_genres_genre_id",
                        column: x => x.genres_genre_id,
                        principalSchema: "public",
                        principalTable: "genres",
                        principalColumn: "genre_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "federation_rules_groups",
                schema: "public",
                columns: table => new
                {
                    federation_rule_rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    groups_group_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_federation_rules_groups", x => new { x.federation_rule_rule_id, x.groups_group_id });
                    table.ForeignKey(
                        name: "fk_federation_rules_groups_federation_rules_federation_rule_ru",
                        column: x => x.federation_rule_rule_id,
                        principalSchema: "public",
                        principalTable: "federation_rules",
                        principalColumn: "rule_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_federation_rules_groups_groups_groups_group_id",
                        column: x => x.groups_group_id,
                        principalSchema: "public",
                        principalTable: "groups",
                        principalColumn: "group_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "federation_rules_peers",
                schema: "public",
                columns: table => new
                {
                    peers_peer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rules_rule_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_federation_rules_peers", x => new { x.peers_peer_id, x.rules_rule_id });
                    table.ForeignKey(
                        name: "fk_federation_rules_peers_federation_peers_peers_peer_id",
                        column: x => x.peers_peer_id,
                        principalSchema: "public",
                        principalTable: "federation_peers",
                        principalColumn: "peer_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_federation_rules_peers_federation_rules_rules_rule_id",
                        column: x => x.rules_rule_id,
                        principalSchema: "public",
                        principalTable: "federation_rules",
                        principalColumn: "rule_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_federation_audit_log_timestamp",
                schema: "public",
                table: "federation_audit_log",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "ix_federation_invitations_code_hash",
                schema: "public",
                table: "federation_invitations",
                column: "code_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_federation_jobs_next_attempt_at",
                schema: "public",
                table: "federation_jobs",
                column: "next_attempt_at");

            migrationBuilder.CreateIndex(
                name: "ix_federation_jobs_peer_id",
                schema: "public",
                table: "federation_jobs",
                column: "peer_id");

            migrationBuilder.CreateIndex(
                name: "ix_federation_peers_instance",
                schema: "public",
                table: "federation_peers",
                column: "instance",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_federation_record_links_local_record_id",
                schema: "public",
                table: "federation_record_links",
                column: "local_record_id");

            migrationBuilder.CreateIndex(
                name: "ix_federation_record_links_peer_id_local_record_id",
                schema: "public",
                table: "federation_record_links",
                columns: new[] { "peer_id", "local_record_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_federation_record_links_peer_id_transaction_id",
                schema: "public",
                table: "federation_record_links",
                columns: new[] { "peer_id", "transaction_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_federation_rules_albums_federation_rule_rule_id",
                schema: "public",
                table: "federation_rules_albums",
                column: "federation_rule_rule_id");

            migrationBuilder.CreateIndex(
                name: "ix_federation_rules_artists_federation_rule_rule_id",
                schema: "public",
                table: "federation_rules_artists",
                column: "federation_rule_rule_id");

            migrationBuilder.CreateIndex(
                name: "ix_federation_rules_genres_genres_genre_id",
                schema: "public",
                table: "federation_rules_genres",
                column: "genres_genre_id");

            migrationBuilder.CreateIndex(
                name: "ix_federation_rules_groups_groups_group_id",
                schema: "public",
                table: "federation_rules_groups",
                column: "groups_group_id");

            migrationBuilder.CreateIndex(
                name: "ix_federation_rules_peers_rules_rule_id",
                schema: "public",
                table: "federation_rules_peers",
                column: "rules_rule_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "federation_audit_log",
                schema: "public");

            migrationBuilder.DropTable(
                name: "federation_invitations",
                schema: "public");

            migrationBuilder.DropTable(
                name: "federation_jobs",
                schema: "public");

            migrationBuilder.DropTable(
                name: "federation_record_links",
                schema: "public");

            migrationBuilder.DropTable(
                name: "federation_rules_albums",
                schema: "public");

            migrationBuilder.DropTable(
                name: "federation_rules_artists",
                schema: "public");

            migrationBuilder.DropTable(
                name: "federation_rules_genres",
                schema: "public");

            migrationBuilder.DropTable(
                name: "federation_rules_groups",
                schema: "public");

            migrationBuilder.DropTable(
                name: "federation_rules_peers",
                schema: "public");

            migrationBuilder.DropTable(
                name: "federation_peers",
                schema: "public");

            migrationBuilder.DropTable(
                name: "federation_rules",
                schema: "public");
        }
    }
}
