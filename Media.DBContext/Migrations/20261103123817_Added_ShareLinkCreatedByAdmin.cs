using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Media.DBContext.Migrations
{
    /// <inheritdoc />
    public partial class Added_ShareLinkCreatedByAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "created_by_admin",
                schema: "public",
                table: "share_links",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // The creator of existing links is unknown, so they are kept like links of admins.
            migrationBuilder.Sql("UPDATE public.share_links SET created_by_admin = true;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "created_by_admin",
                schema: "public",
                table: "share_links");
        }
    }
}
