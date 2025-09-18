using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arceus.Migrations
{
    /// <inheritdoc />
    public partial class InitV4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "version",
                table: "accounts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "version",
                table: "accounts",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);
        }
    }
}
