using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Media.DBContext.Migrations
{
    /// <inheritdoc />
    public partial class Changed_ShareRulesToGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "share_rules_groups",
                schema: "public",
                columns: table => new
                {
                    groups_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    share_rule_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_share_rules_groups", x => new { x.groups_group_id, x.share_rule_id });
                    table.ForeignKey(
                        name: "fk_share_rules_groups_groups_groups_group_id",
                        column: x => x.groups_group_id,
                        principalSchema: "public",
                        principalTable: "groups",
                        principalColumn: "group_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_share_rules_groups_share_rules_share_rule_id",
                        column: x => x.share_rule_id,
                        principalSchema: "public",
                        principalTable: "share_rules",
                        principalColumn: "share_rule_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_share_rules_groups_share_rule_id",
                schema: "public",
                table: "share_rules_groups",
                column: "share_rule_id");

            migrationBuilder.Sql(
                "INSERT INTO public.share_rules_groups (groups_group_id, share_rule_id) SELECT group_id, share_rule_id FROM public.share_rules;");

            migrationBuilder.DropForeignKey(
                name: "fk_share_rules_groups_group_id",
                schema: "public",
                table: "share_rules");

            migrationBuilder.DropIndex(
                name: "ix_share_rules_group_id",
                schema: "public",
                table: "share_rules");

            migrationBuilder.DropColumn(
                name: "group_id",
                schema: "public",
                table: "share_rules");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "group_id",
                schema: "public",
                table: "share_rules",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql(
                "UPDATE public.share_rules r SET group_id = (SELECT g.groups_group_id FROM public.share_rules_groups g WHERE g.share_rule_id = r.share_rule_id LIMIT 1);");

            migrationBuilder.Sql(
                "DELETE FROM public.share_rules WHERE group_id = '00000000-0000-0000-0000-000000000000';");

            migrationBuilder.DropTable(
                name: "share_rules_groups",
                schema: "public");

            migrationBuilder.CreateIndex(
                name: "ix_share_rules_group_id",
                schema: "public",
                table: "share_rules",
                column: "group_id");

            migrationBuilder.AddForeignKey(
                name: "fk_share_rules_groups_group_id",
                schema: "public",
                table: "share_rules",
                column: "group_id",
                principalSchema: "public",
                principalTable: "groups",
                principalColumn: "group_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
