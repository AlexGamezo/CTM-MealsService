using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MealsService.Migrations
{
    public partial class addedDietGoalandMenuPreferencemodels : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DietGoals",
                columns: table => new
                {
                    UserId = table.Column<int>(nullable: false),
                    Current = table.Column<int>(nullable: false),
                    ReductionRate = table.Column<int>(nullable: false),
                    Target = table.Column<int>(nullable: false),
                    TargetDietId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DietGoals", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_DietGoals_DietTypes_TargetDietId",
                        column: x => x.TargetDietId,
                        principalTable: "DietTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MenuPreferences",
                columns: table => new
                {
                    UserId = table.Column<int>(nullable: false),
                    MealStyle = table.Column<int>(nullable: false),
                    ShoppingFreq = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuPreferences", x => x.UserId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DietGoals_TargetDietId",
                table: "DietGoals",
                column: "TargetDietId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DietGoals");

            migrationBuilder.DropTable(
                name: "MenuPreferences");
        }
    }
}
