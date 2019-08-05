using Microsoft.EntityFrameworkCore.Migrations;

namespace MealsService.Migrations
{
    public partial class migratepreparationshoppingListItemrelationship : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShoppingListItemPreparation");

            migrationBuilder.AddColumn<int>(
                name: "PreparationId",
                table: "ShoppingListItems",
                nullable: true,
                defaultValue: null);

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingListItems_PreparationId",
                table: "ShoppingListItems",
                column: "PreparationId");

            migrationBuilder.AddForeignKey(
                name: "FK_ShoppingListItems_Preparations_PreparationId",
                table: "ShoppingListItems",
                column: "PreparationId",
                principalTable: "Preparations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShoppingListItems_Preparations_PreparationId",
                table: "ShoppingListItems");

            migrationBuilder.DropIndex(
                name: "IX_ShoppingListItems_PreparationId",
                table: "ShoppingListItems");

            migrationBuilder.DropColumn(
                name: "PreparationId",
                table: "ShoppingListItems");

            migrationBuilder.CreateTable(
                name: "ShoppingListItemPreparation",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PreparationId = table.Column<int>(nullable: false),
                    ShoppingListItemId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShoppingListItemPreparation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShoppingListItemPreparation_Preparations_PreparationId",
                        column: x => x.PreparationId,
                        principalTable: "Preparations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShoppingListItemPreparation_ShoppingListItems_ShoppingListIt~",
                        column: x => x.ShoppingListItemId,
                        principalTable: "ShoppingListItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingListItemPreparation_PreparationId",
                table: "ShoppingListItemPreparation",
                column: "PreparationId");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingListItemPreparation_ShoppingListItemId",
                table: "ShoppingListItemPreparation",
                column: "ShoppingListItemId");
        }
    }
}
