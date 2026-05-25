using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Recipes.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CascadeDeletePersonMembershipsOnPersonDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_PersonMemberships_PersonId",
                table: "PersonMemberships",
                column: "PersonId");

            migrationBuilder.AddForeignKey(
                name: "FK_PersonMemberships_Persons_PersonId",
                table: "PersonMemberships",
                column: "PersonId",
                principalTable: "Persons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PersonMemberships_Persons_PersonId",
                table: "PersonMemberships");

            migrationBuilder.DropIndex(
                name: "IX_PersonMemberships_PersonId",
                table: "PersonMemberships");
        }
    }
}
