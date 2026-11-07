using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NEText.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupChats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Conversations",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Nickname",
                table: "ConversationParticipants",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "ConversationParticipants",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Member");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "Nickname",
                table: "ConversationParticipants");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "ConversationParticipants");
        }
    }
}
