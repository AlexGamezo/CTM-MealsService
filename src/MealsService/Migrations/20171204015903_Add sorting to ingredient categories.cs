using Microsoft.EntityFrameworkCore.Migrations;

namespace MealsService.Migrations
{
    public partial class Addsortingtoingredientcategories : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "IngredientCategories",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Order",
                table: "IngredientCategories");
        }
    }
}
