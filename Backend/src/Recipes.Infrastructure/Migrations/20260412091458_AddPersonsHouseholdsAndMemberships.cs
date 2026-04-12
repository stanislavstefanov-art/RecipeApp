using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Recipes.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonsHouseholdsAndMemberships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RecipeId",
                table: "MealPlanEntries",
                newName: "BaseRecipeId");

            migrationBuilder.RenameIndex(
                name: "IX_MealPlanEntries_RecipeId",
                table: "MealPlanEntries",
                newName: "IX_MealPlanEntries_BaseRecipeId");

            migrationBuilder.AddColumn<Guid>(
                name: "HouseholdId",
                table: "MealPlans",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "Scope",
                table: "MealPlanEntries",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Households",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Households", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MealPlanPersonAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MealPlanEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedRecipeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PortionMultiplier = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MealPlanPersonAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MealPlanPersonAssignments_MealPlanEntries_MealPlanEntryId",
                        column: x => x.MealPlanEntryId,
                        principalTable: "MealPlanEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Persons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DietaryPreferences = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HealthConcerns = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Persons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HouseholdMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HouseholdId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseholdMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HouseholdMembers_Households_HouseholdId",
                        column: x => x.HouseholdId,
                        principalTable: "Households",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdMembers_HouseholdId_PersonId",
                table: "HouseholdMembers",
                columns: new[] { "HouseholdId", "PersonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MealPlanPersonAssignments_MealPlanEntryId",
                table: "MealPlanPersonAssignments",
                column: "MealPlanEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_MealPlanPersonAssignments_PersonId",
                table: "MealPlanPersonAssignments",
                column: "PersonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HouseholdMembers");

            migrationBuilder.DropTable(
                name: "MealPlanPersonAssignments");

            migrationBuilder.DropTable(
                name: "Persons");

            migrationBuilder.DropTable(
                name: "Households");

            migrationBuilder.DropColumn(
                name: "HouseholdId",
                table: "MealPlans");

            migrationBuilder.DropColumn(
                name: "Scope",
                table: "MealPlanEntries");

            migrationBuilder.RenameColumn(
                name: "BaseRecipeId",
                table: "MealPlanEntries",
                newName: "RecipeId");

            migrationBuilder.RenameIndex(
                name: "IX_MealPlanEntries_BaseRecipeId",
                table: "MealPlanEntries",
                newName: "IX_MealPlanEntries_RecipeId");
        }
    }
}
