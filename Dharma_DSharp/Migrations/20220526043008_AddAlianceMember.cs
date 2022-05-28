using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dharma_DSharp.Migrations
{
    public partial class AddAlianceMember : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Member",
                columns: table => new
                {
                    DiscordUserId = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DiscordUserName = table.Column<string>(type: "TEXT", nullable: false),
                    DiscordDisplayName = table.Column<string>(type: "TEXT", nullable: false),
                    PhantasyUserId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Member", x => x.DiscordUserId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Member");
        }
    }
}
