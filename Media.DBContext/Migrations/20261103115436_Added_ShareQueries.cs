using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Media.DBContext.Migrations
{
    /// <inheritdoc />
    public partial class Added_ShareQueries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "share_queries",
                schema: "public",
                columns: table => new
                {
                    share_query_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token = table.Column<string>(type: "text", nullable: false),
                    query_hash = table.Column<string>(type: "text", nullable: false),
                    query = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_share_queries", x => x.share_query_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_share_queries_last_used_at",
                schema: "public",
                table: "share_queries",
                column: "last_used_at");

            migrationBuilder.CreateIndex(
                name: "ix_share_queries_query_hash",
                schema: "public",
                table: "share_queries",
                column: "query_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_share_queries_token",
                schema: "public",
                table: "share_queries",
                column: "token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "share_queries",
                schema: "public");
        }
    }
}
