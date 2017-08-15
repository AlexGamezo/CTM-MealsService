using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MealsService.Migrations
{
    public partial class IngredientTagsandCurrentDietId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGeneratedOnAdd", true),
                    Name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IngredientTags",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGeneratedOnAdd", true),
                    IngredientId = table.Column<int>(nullable: false),
                    TagId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngredientTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IngredientTags_Ingredients_IngredientId",
                        column: x => x.IngredientId,
                        principalTable: "Ingredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IngredientTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddColumn<int>(
                name: "CurrentDietTypeId",
                table: "MenuPreferences",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<DateTime>(
                name: "Modified",
                table: "ScheduleDays",
                nullable: false,
                oldClrType: typeof(DateTime))
                .OldAnnotation("MySql:ValueGeneratedOnAddOrUpdate", true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "Created",
                table: "ScheduleDays",
                nullable: false,
                oldClrType: typeof(DateTime))
                .OldAnnotation("MySql:ValueGeneratedOnAdd", true);

            migrationBuilder.CreateIndex(
                name: "IX_MenuPreferences_CurrentDietTypeId",
                table: "MenuPreferences",
                column: "CurrentDietTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_IngredientTags_IngredientId",
                table: "IngredientTags",
                column: "IngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_IngredientTags_TagId",
                table: "IngredientTags",
                column: "TagId");

            migrationBuilder.AddForeignKey(
                name: "FK_MenuPreferences_DietTypes_CurrentDietTypeId",
                table: "MenuPreferences",
                column: "CurrentDietTypeId",
                principalTable: "DietTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MenuPreferences_DietTypes_CurrentDietTypeId",
                table: "MenuPreferences");

            migrationBuilder.DropIndex(
                name: "IX_MenuPreferences_CurrentDietTypeId",
                table: "MenuPreferences");

            migrationBuilder.DropColumn(
                name: "CurrentDietTypeId",
                table: "MenuPreferences");

            migrationBuilder.DropTable(
                name: "IngredientTags");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Modified",
                table: "ScheduleDays",
                nullable: false,
                oldClrType: typeof(DateTime))
                .Annotation("MySql:ValueGeneratedOnAddOrUpdate", true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "Created",
                table: "ScheduleDays",
                nullable: false,
                oldClrType: typeof(DateTime))
                .Annotation("MySql:ValueGeneratedOnAdd", true);
        }
    }
}
