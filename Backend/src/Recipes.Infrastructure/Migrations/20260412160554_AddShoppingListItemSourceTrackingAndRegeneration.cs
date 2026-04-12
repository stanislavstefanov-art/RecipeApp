using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Recipes.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddShoppingListItemSourceTrackingAndRegeneration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SourceReferenceId",
                table: "ShoppingListItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceType",
                table: "ShoppingListItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingListItems_SourceType_SourceReferenceId",
                table: "ShoppingListItems",
                columns: new[] { "SourceType", "SourceReferenceId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShoppingListItems_SourceType_SourceReferenceId",
                table: "ShoppingListItems");

            migrationBuilder.DropColumn(
                name: "SourceReferenceId",
                table: "ShoppingListItems");

            migrationBuilder.DropColumn(
                name: "SourceType",
                table: "ShoppingListItems");
        }
    }
}
