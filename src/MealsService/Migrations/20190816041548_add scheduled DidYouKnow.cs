using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MealsService.Migrations
{
    public partial class addscheduledDidYouKnow : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShoppingListItems_Preparations_PreparationId",
                table: "ShoppingListItems");

            migrationBuilder.AlterColumn<int>(
                name: "PreparationId",
                table: "ShoppingListItems",
                nullable: true,
                oldClrType: typeof(int));

            migrationBuilder.CreateTable(
                name: "DidYouKnowStats",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Text = table.Column<string>(nullable: true),
                    Link = table.Column<string>(nullable: true),
                    Date = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DidYouKnowStats", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_ShoppingListItems_Preparations_PreparationId",
                table: "ShoppingListItems",
                column: "PreparationId",
                principalTable: "Preparations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShoppingListItems_Preparations_PreparationId",
                table: "ShoppingListItems");

            migrationBuilder.DropTable(
                name: "DidYouKnowStats");

            migrationBuilder.AlterColumn<int>(
                name: "PreparationId",
                table: "ShoppingListItems",
                nullable: false,
                oldClrType: typeof(int),
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ShoppingListItems_Preparations_PreparationId",
                table: "ShoppingListItems",
                column: "PreparationId",
                principalTable: "Preparations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
