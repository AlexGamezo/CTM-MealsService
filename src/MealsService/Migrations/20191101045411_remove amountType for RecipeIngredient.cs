using Microsoft.EntityFrameworkCore.Migrations;

namespace MealsService.Migrations
{
    public partial class removeamountTypeforRecipeIngredient : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AmountType",
                table: "RecipeIngredients");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AmountType",
                table: "RecipeIngredients",
                maxLength: 32,
                nullable: true);
        }
    }
}
