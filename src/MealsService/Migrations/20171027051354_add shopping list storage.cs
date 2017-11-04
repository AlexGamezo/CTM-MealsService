using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MealsService.Migrations
{
    public partial class addshoppingliststorage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RecipeVotes_Meals_MealId",
                table: "RecipeVotes");

            migrationBuilder.DropIndex(
                name: "IX_RecipeVotes_MealId",
                table: "RecipeVotes");

            migrationBuilder.DropColumn(
                name: "MealId",
                table: "RecipeVotes");

            migrationBuilder.CreateTable(
                name: "MeasureType",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGeneratedOnAdd", true),
                    Name = table.Column<string>(nullable: true),
                    Short = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeasureType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShoppingListItems",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGeneratedOnAdd", true),
                    Amount = table.Column<float>(nullable: false),
                    Checked = table.Column<bool>(nullable: false),
                    IngredientId = table.Column<int>(nullable: false),
                    IngredientName = table.Column<string>(maxLength: 64, nullable: true),
                    ManuallyAdded = table.Column<bool>(nullable: false),
                    MeasureTypeId = table.Column<int>(nullable: false),
                    UserId = table.Column<int>(nullable: false),
                    WeekStart = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShoppingListItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShoppingListItemScheduleSlot",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGeneratedOnAdd", true),
                    ScheduleSlotId = table.Column<int>(nullable: false),
                    ShoppingListItemId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShoppingListItemScheduleSlot", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShoppingListItemScheduleSlot_ScheduleSlots_ScheduleSlotId",
                        column: x => x.ScheduleSlotId,
                        principalTable: "ScheduleSlots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShoppingListItemScheduleSlot_ShoppingListItems_ShoppingListItemId",
                        column: x => x.ShoppingListItemId,
                        principalTable: "ShoppingListItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddColumn<int>(
                name: "MeasureTypeId",
                table: "MealIngredients",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_RecipeVotes_RecipeId",
                table: "RecipeVotes",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_MealIngredients_MeasureTypeId",
                table: "MealIngredients",
                column: "MeasureTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingListItems_IngredientId",
                table: "ShoppingListItems",
                column: "IngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingListItems_MeasureTypeId",
                table: "ShoppingListItems",
                column: "MeasureTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingListItemScheduleSlot_ScheduleSlotId",
                table: "ShoppingListItemScheduleSlot",
                column: "ScheduleSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingListItemScheduleSlot_ShoppingListItemId",
                table: "ShoppingListItemScheduleSlot",
                column: "ShoppingListItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_MealIngredients_MeasureType_MeasureTypeId",
                table: "MealIngredients",
                column: "MeasureTypeId",
                principalTable: "MeasureType",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RecipeVotes_Meals_RecipeId",
                table: "RecipeVotes",
                column: "RecipeId",
                principalTable: "Meals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MealIngredients_MeasureType_MeasureTypeId",
                table: "MealIngredients");

            migrationBuilder.DropForeignKey(
                name: "FK_RecipeVotes_Meals_RecipeId",
                table: "RecipeVotes");

            migrationBuilder.DropIndex(
                name: "IX_RecipeVotes_RecipeId",
                table: "RecipeVotes");

            migrationBuilder.DropIndex(
                name: "IX_MealIngredients_MeasureTypeId",
                table: "MealIngredients");

            migrationBuilder.DropColumn(
                name: "MeasureTypeId",
                table: "MealIngredients");

            migrationBuilder.DropTable(
                name: "ShoppingListItemScheduleSlot");

            migrationBuilder.DropTable(
                name: "ShoppingListItems");

            migrationBuilder.DropTable(
                name: "MeasureType");

            migrationBuilder.AddColumn<int>(
                name: "MealId",
                table: "RecipeVotes",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecipeVotes_MealId",
                table: "RecipeVotes",
                column: "MealId");

            migrationBuilder.AddForeignKey(
                name: "FK_RecipeVotes_Meals_MealId",
                table: "RecipeVotes",
                column: "MealId",
                principalTable: "Meals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
