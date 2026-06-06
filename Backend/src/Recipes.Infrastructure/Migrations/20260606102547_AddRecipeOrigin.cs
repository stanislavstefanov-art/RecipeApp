using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Recipes.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRecipeOrigin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Origin",
                table: "Recipes",
                type: "int",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Origin",
                table: "Recipes");
        }
    }
}
