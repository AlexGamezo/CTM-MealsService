using Microsoft.EntityFrameworkCore.Migrations;

namespace MealsService.Migrations
{
    public partial class addsoftdeleteflagforrecipes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShoppingListItems_ShoppingListItemId",
                table: "ShoppingListItemPreparation");

            migrationBuilder.AddColumn<bool>(
                name: "Deleted",
                table: "Recipes",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_ShoppingListItems_ShoppingListItemId",
                table: "ShoppingListItemPreparation",
                column: "ShoppingListItemId",
                principalTable: "ShoppingListItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShoppingListItems_ShoppingListItemId",
                table: "ShoppingListItemPreparation");

            migrationBuilder.DropColumn(
                name: "Deleted",
                table: "Recipes");

            migrationBuilder.AddForeignKey(
                name: "FK_ShoppingListItems_ShoppingListItemId",
                table: "ShoppingListItemPreparation",
                column: "ShoppingListItemId",
                principalTable: "ShoppingListItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
