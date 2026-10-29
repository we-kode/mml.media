using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Media.DBContext.Migrations
{
    /// <inheritdoc />
    public partial class Added_FederationProgressAndExclusions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "size",
                schema: "public",
                table: "federation_jobs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "transferred",
                schema: "public",
                table: "federation_jobs",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "federation_exclusions",
                schema: "public",
                columns: table => new
                {
                    peer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    record_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_federation_exclusions", x => new { x.peer_id, x.record_id });
                    table.ForeignKey(
                        name: "fk_federation_exclusions_federation_peers_peer_id",
                        column: x => x.peer_id,
                        principalSchema: "public",
                        principalTable: "federation_peers",
                        principalColumn: "peer_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_federation_exclusions_records_record_id",
                        column: x => x.record_id,
                        principalSchema: "public",
                        principalTable: "records",
                        principalColumn: "record_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_federation_exclusions_record_id",
                schema: "public",
                table: "federation_exclusions",
                column: "record_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "federation_exclusions",
                schema: "public");

            migrationBuilder.DropColumn(
                name: "size",
                schema: "public",
                table: "federation_jobs");

            migrationBuilder.DropColumn(
                name: "transferred",
                schema: "public",
                table: "federation_jobs");
        }
    }
}
