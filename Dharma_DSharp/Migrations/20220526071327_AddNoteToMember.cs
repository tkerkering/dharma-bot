using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dharma_DSharp.Migrations
{
    public partial class AddNoteToMember : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Member",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Member");
        }
    }
}
