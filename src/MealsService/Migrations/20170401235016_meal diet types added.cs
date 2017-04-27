using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MealsService.Migrations
{
    public partial class mealdiettypesadded : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MealDietTypes",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGeneratedOnAdd", true),
                    DietTypeId = table.Column<int>(nullable: false),
                    MealId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MealDietTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MealDietTypes_DietTypes_DietTypeId",
                        column: x => x.DietTypeId,
                        principalTable: "DietTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MealDietTypes_Meals_MealId",
                        column: x => x.MealId,
                        principalTable: "Meals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddColumn<string>(
                name: "MealTypesList",
                table: "MenuPreferences",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MealDietTypes_DietTypeId",
                table: "MealDietTypes",
                column: "DietTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_MealDietTypes_MealId",
                table: "MealDietTypes",
                column: "MealId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MealTypesList",
                table: "MenuPreferences");

            migrationBuilder.DropTable(
                name: "MealDietTypes");
        }
    }
}
