using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Recipes.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_HouseholdMembers",
                table: "HouseholdMembers");

            migrationBuilder.DropIndex(
                name: "IX_HouseholdMembers_HouseholdId_PersonId",
                table: "HouseholdMembers");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "HouseholdMembers");

            migrationBuilder.RenameColumn(
                name: "PersonId",
                table: "HouseholdMembers",
                newName: "UserId");

            migrationBuilder.AddColumn<Guid>(
                name: "HouseholdId",
                table: "Recipes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "HouseholdId",
                table: "Persons",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "JoinedAt",
                table: "HouseholdMembers",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddPrimaryKey(
                name: "PK_HouseholdMembers",
                table: "HouseholdMembers",
                columns: new[] { "HouseholdId", "UserId" });

            migrationBuilder.CreateTable(
                name: "PersonMemberships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HouseholdId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonMemberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonMemberships_Households_HouseholdId",
                        column: x => x.HouseholdId,
                        principalTable: "Households",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AuthProvider = table.Column<int>(type: "int", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EntraObjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastLoginAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_HouseholdId",
                table: "Recipes",
                column: "HouseholdId",
                filter: "[HouseholdId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Persons_HouseholdId",
                table: "Persons",
                column: "HouseholdId",
                filter: "[HouseholdId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdMembers_UserId",
                table: "HouseholdMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonMemberships_HouseholdId_PersonId",
                table: "PersonMemberships",
                columns: new[] { "HouseholdId", "PersonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_EntraObjectId",
                table: "Users",
                column: "EntraObjectId",
                unique: true,
                filter: "[EntraObjectId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_HouseholdMembers_Users_UserId",
                table: "HouseholdMembers",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HouseholdMembers_Users_UserId",
                table: "HouseholdMembers");

            migrationBuilder.DropTable(
                name: "PersonMemberships");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Recipes_HouseholdId",
                table: "Recipes");

            migrationBuilder.DropIndex(
                name: "IX_Persons_HouseholdId",
                table: "Persons");

            migrationBuilder.DropPrimaryKey(
                name: "PK_HouseholdMembers",
                table: "HouseholdMembers");

            migrationBuilder.DropIndex(
                name: "IX_HouseholdMembers_UserId",
                table: "HouseholdMembers");

            migrationBuilder.DropColumn(
                name: "HouseholdId",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "HouseholdId",
                table: "Persons");

            migrationBuilder.DropColumn(
                name: "JoinedAt",
                table: "HouseholdMembers");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "HouseholdMembers",
                newName: "PersonId");

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "HouseholdMembers",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_HouseholdMembers",
                table: "HouseholdMembers",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdMembers_HouseholdId_PersonId",
                table: "HouseholdMembers",
                columns: new[] { "HouseholdId", "PersonId" },
                unique: true);
        }
    }
}
