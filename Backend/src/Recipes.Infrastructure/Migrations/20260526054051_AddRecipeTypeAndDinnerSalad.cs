using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Recipes.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRecipeTypeAndDinnerSalad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RecipeType",
                table: "Recipes",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<Guid>(
                name: "SaladRecipeId",
                table: "MealPlanEntries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MealPlanEntries_SaladRecipeId",
                table: "MealPlanEntries",
                column: "SaladRecipeId",
                filter: "[SaladRecipeId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_MealPlanEntries_Recipes_SaladRecipeId",
                table: "MealPlanEntries",
                column: "SaladRecipeId",
                principalTable: "Recipes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MealPlanEntries_Recipes_SaladRecipeId",
                table: "MealPlanEntries");

            migrationBuilder.DropIndex(
                name: "IX_MealPlanEntries_SaladRecipeId",
                table: "MealPlanEntries");

            migrationBuilder.DropColumn(
                name: "RecipeType",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "SaladRecipeId",
                table: "MealPlanEntries");
        }
    }
}
