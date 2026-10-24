using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace Media.DBContext.Migrations
{
    /// <inheritdoc />
    public partial class Added_OwnerInstanceColumn_Record : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "owner_instance",
                schema: "public",
                table: "records",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql($"UPDATE public.records SET owner_instance = '{Environment.GetEnvironmentVariable("INSTANCE")}' WHERE owner_instance = ''");
    }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "owner_instance",
                schema: "public",
                table: "records");
        }
    }
}
