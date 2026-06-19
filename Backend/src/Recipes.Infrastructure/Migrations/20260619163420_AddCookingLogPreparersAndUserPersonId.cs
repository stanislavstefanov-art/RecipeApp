using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Recipes.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCookingLogPreparersAndUserPersonId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PersonId",
                table: "Users",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CookingLogPreparers",
                columns: table => new
                {
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CookingLogEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CookingLogPreparers", x => new { x.CookingLogEntryId, x.PersonId });
                    table.ForeignKey(
                        name: "FK_CookingLogPreparers_CookingLogEntries_CookingLogEntryId",
                        column: x => x.CookingLogEntryId,
                        principalTable: "CookingLogEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CookingLogPreparers");

            migrationBuilder.DropColumn(
                name: "PersonId",
                table: "Users");
        }
    }
}
