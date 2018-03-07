using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace MealsService.Migrations
{
    public partial class AddMealTypetoPreparationmodel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MealId",
                newName: "PreparationId",
                table: "ShoppingListeItemMeal");

            migrationBuilder.RenameTable(
                name: "ShoppingListItemMeal", newName: "ShoppingListItemPreparation");

            migrationBuilder.AddColumn<int>(
                name: "MealType",
                table: "Preparations",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PreparationId",
                newName: "MealId",
                table: "ShoppingListItemPreparation");

            migrationBuilder.RenameTable(
                name: "ShoppingListItemPreparation", newName:"ShoppingListItemMeal");

            migrationBuilder.DropColumn(
                name: "MealType",
                table: "Preparations");
        }
    }
}
