using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Media.DBContext.Migrations
{
    /// <inheritdoc />
    public partial class Added_Notifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "notifications",
                schema: "public",
                columns: table => new
                {
                    notification_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    severity = table.Column<int>(type: "integer", nullable: false),
                    valid_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    valid_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    client_ids = table.Column<List<string>>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notifications", x => x.notification_id);
                });

            migrationBuilder.CreateTable(
                name: "groups_notifications",
                schema: "public",
                columns: table => new
                {
                    groups_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    notification_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_groups_notifications", x => new { x.groups_group_id, x.notification_id });
                    table.ForeignKey(
                        name: "fk_groups_notifications_groups_groups_group_id",
                        column: x => x.groups_group_id,
                        principalSchema: "public",
                        principalTable: "groups",
                        principalColumn: "group_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_groups_notifications_notifications_notification_id",
                        column: x => x.notification_id,
                        principalSchema: "public",
                        principalTable: "notifications",
                        principalColumn: "notification_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_groups_notifications_notification_id",
                schema: "public",
                table: "groups_notifications",
                column: "notification_id");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_valid_to",
                schema: "public",
                table: "notifications",
                column: "valid_to");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "groups_notifications",
                schema: "public");

            migrationBuilder.DropTable(
                name: "notifications",
                schema: "public");
        }
    }
}
