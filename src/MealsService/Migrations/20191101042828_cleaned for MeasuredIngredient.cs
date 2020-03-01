using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MealsService.Migrations
{
    public partial class cleanedforMeasuredIngredient : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IngredientMeasureTypes");

            migrationBuilder.DropTable(
                name: "MeasureConverters");

            migrationBuilder.DropTable(
                name: "MeasureTypes");

            migrationBuilder.DropIndex(
                name: "IX_ShoppingListItems_MeasureTypeId",
                table: "ShoppingListItems");

            migrationBuilder.DropIndex(
                name: "IX_RecipeIngredients_MeasureTypeId",
                table: "RecipeIngredients");

            migrationBuilder.DropColumn(
                name: "IngredientName",
                table: "ShoppingListItems");

            migrationBuilder.DropColumn(
                name: "MeasureTypeId",
                table: "ShoppingListItems");

            migrationBuilder.DropColumn(
                name: "MeasureTypeId",
                table: "RecipeIngredients");

            migrationBuilder.DropColumn(
                name: "MeasurementType",
                table: "Ingredients");

            migrationBuilder.AlterColumn<double>(
                name: "Amount",
                table: "ShoppingListItems",
                nullable: false,
                oldClrType: typeof(float));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<float>(
                name: "Amount",
                table: "ShoppingListItems",
                nullable: false,
                oldClrType: typeof(double));

            migrationBuilder.AddColumn<string>(
                name: "IngredientName",
                table: "ShoppingListItems",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MeasureTypeId",
                table: "ShoppingListItems",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MeasureTypeId",
                table: "RecipeIngredients",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "MeasurementType",
                table: "Ingredients",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MeasureTypes",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(maxLength: 32, nullable: true),
                    Short = table.Column<string>(maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeasureTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IngredientMeasureTypes",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IngredientId = table.Column<int>(nullable: false),
                    MeasureTypeId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngredientMeasureTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IngredientMeasureTypes_Ingredients_IngredientId",
                        column: x => x.IngredientId,
                        principalTable: "Ingredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IngredientMeasureTypes_MeasureTypes_MeasureTypeId",
                        column: x => x.MeasureTypeId,
                        principalTable: "MeasureTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MeasureConverters",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ConvertType = table.Column<int>(nullable: false),
                    Factor = table.Column<double>(nullable: false),
                    SourceMeasureId = table.Column<int>(nullable: false),
                    TargetMeasureId = table.Column<int>(nullable: false),
                    TargetMeasureTypeId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeasureConverters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeasureConverters_MeasureTypes_SourceMeasureId",
                        column: x => x.SourceMeasureId,
                        principalTable: "MeasureTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MeasureConverters_MeasureTypes_TargetMeasureTypeId",
                        column: x => x.TargetMeasureTypeId,
                        principalTable: "MeasureTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingListItems_MeasureTypeId",
                table: "ShoppingListItems",
                column: "MeasureTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_MeasureTypeId",
                table: "RecipeIngredients",
                column: "MeasureTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_IngredientMeasureTypes_IngredientId",
                table: "IngredientMeasureTypes",
                column: "IngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_IngredientMeasureTypes_MeasureTypeId",
                table: "IngredientMeasureTypes",
                column: "MeasureTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_MeasureConverters_SourceMeasureId",
                table: "MeasureConverters",
                column: "SourceMeasureId");

            migrationBuilder.CreateIndex(
                name: "IX_MeasureConverters_TargetMeasureTypeId",
                table: "MeasureConverters",
                column: "TargetMeasureTypeId");
        }
    }
}
