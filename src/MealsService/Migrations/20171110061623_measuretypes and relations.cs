using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MealsService.Migrations
{
    public partial class measuretypesandrelations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "MeasureType",
                newName: "MeasureTypes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MeasureType",
                table: "MeasureTypes");

            migrationBuilder.CreateTable(
                name: "IngredientMeasureTypes",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGeneratedOnAdd", true),
                    IngredientId = table.Column<int>(nullable: true),
                    MeasureTypeId = table.Column<int>(nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngredientMeasureTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IngredientMeasureTypes_Ingredients_IngredientId",
                        column: x => x.IngredientId,
                        principalTable: "Ingredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IngredientMeasureTypes_MeasureTypes_MeasureTypeId",
                        column: x => x.MeasureTypeId,
                        principalTable: "MeasureTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddPrimaryKey(
                name: "PK_MeasureTypes",
                table: "MeasureTypess",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MealIngredients_MeasureTypes_MeasureTypeId",
                table: "MealIngredients",
                column: "MeasureTypeId",
                principalTable: "MeasureTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ShoppingListItems_MeasureTypes_MeasureTypeId",
                table: "ShoppingListItems",
                column: "MeasureTypeId",
                principalTable: "MeasureTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MealIngredients_MeasureTypes_MeasureTypeId",
                table: "MealIngredients");

            migrationBuilder.DropForeignKey(
                name: "FK_ShoppingListItems_MeasureTypes_MeasureTypeId",
                table: "ShoppingListItems");

            migrationBuilder.RenameTable(
                name: "MeasureTypes",
                newName: "MeasureType");

            migrationBuilder.DropTable(
                name: "IngredientMeasureTypes");

            migrationBuilder.AddForeignKey(
                name: "FK_MealIngredients_MeasureType_MeasureTypeId",
                table: "MealIngredients",
                column: "MeasureTypeId",
                principalTable: "MeasureType",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ShoppingListItems_MeasureType_MeasureTypeId",
                table: "ShoppingListItems",
                column: "MeasureTypeId",
                principalTable: "MeasureType",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
