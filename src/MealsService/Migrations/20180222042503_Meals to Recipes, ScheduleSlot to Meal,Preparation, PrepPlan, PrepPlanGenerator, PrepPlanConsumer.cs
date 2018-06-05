using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MealsService.Migrations
{
    public partial class MealstoRecipesScheduleSlottoMealPreparationPrepPlanPrepPlanGeneratorPrepPlanConsumer : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RecipeSteps_Meals_MealId",
                table: "RecipeSteps");

            migrationBuilder.DropForeignKey(
                name: "FK_RecipeVotes_Meals_RecipeId",
                table: "RecipeVotes");

            migrationBuilder.RenameColumn(
                name: "MealId", newName: "RecipeId", table: "ScheduleSlots");

            migrationBuilder.RenameColumn(
                name: "MealId", newName: "RecipeId", table: "MealDietTypes");

            migrationBuilder.RenameTable(
                name: "Meals", newName: "Recipes");

            migrationBuilder.RenameTable(
                name: "MealDietTypes", newName: "RecipeDietTypes");

            migrationBuilder.RenameColumn(
                name: "MealId", newName: "RecipeId", table: "MealIngredients");

            migrationBuilder.RenameColumn(
                name: "ScheduleSlotId", newName: "MealId", table: "ShoppingListItemScheduleSlot");

            migrationBuilder.RenameTable(
                name: "MealIngredients", newName: "RecipeIngredients");
            
            migrationBuilder.RenameTable(
                name: "ShoppingListItemScheduleSlot", newName: "ShoppingListItemMeal");

            migrationBuilder.RenameTable(
                name: "ScheduleSlots", newName: "Meals");

            migrationBuilder.AddColumn<int>(
                name: "PreparationId",
                table: "Meals",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Servings",
                table: "Meals",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsLeftovers",
                table: "Meals",
                nullable: false,
                defaultValue: false);

            migrationBuilder.RenameColumn(
                name: "MealStyle", newName: "RecipeStyle",
                table: "MenuPreferences");

            migrationBuilder.DropIndex(name: "IX_RecipeSteps_MealId", table: "RecipeSteps");

            migrationBuilder.RenameColumn(
                name: "MealId",
                table: "RecipeSteps",
                newName: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeSteps_RecipeId",
                table: "RecipeSteps",
                column:"RecipeId");

            migrationBuilder.CreateTable(
                name: "PrepPlans",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    NumTargetDays = table.Column<int>(nullable: false),
                    UserId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrepPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PrepPlanGenerators",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DayOfWeek = table.Column<int>(nullable: false),
                    MealType = table.Column<int>(nullable: false),
                    NumServings = table.Column<int>(nullable: false),
                    PrepPlanId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrepPlanGenerators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrepPlanGenerators_PrepPlans_PrepPlanId",
                        column: x => x.PrepPlanId,
                        principalTable: "PrepPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Preparations",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RecipeId = table.Column<int>(nullable: false),
                    ScheduleDayId = table.Column<int>(nullable: false),
                    UserId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Preparations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Preparations_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Preparations_ScheduleDays_ScheduleDayId",
                        column: x => x.ScheduleDayId,
                        principalTable: "ScheduleDays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PrepPlanConsumers",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DayOfWeek = table.Column<int>(nullable: false),
                    GeneratorId = table.Column<int>(nullable: false),
                    MealType = table.Column<int>(nullable: false),
                    NumServings = table.Column<int>(nullable: false),
                    PrepPlanId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrepPlanConsumers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrepPlanConsumers_PrepPlanGenerators_GeneratorId",
                        column: x => x.GeneratorId,
                        principalTable: "PrepPlanGenerators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PrepPlanConsumers_PrepPlans_PrepPlanId",
                        column: x => x.PrepPlanId,
                        principalTable: "PrepPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Meals_PreparationId",
                table: "Meals",
                column: "PreparationId");

            migrationBuilder.CreateIndex(
                name: "IX_Meals_RecipeId",
                table: "Meals",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_Meals_ScheduleDayId",
                table: "Meals",
                column: "ScheduleDayId");

            migrationBuilder.CreateIndex(
                name: "IX_Preparations_RecipeId",
                table: "Preparations",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_Preparations_ScheduleDayId",
                table: "Preparations",
                column: "ScheduleDayId");

            migrationBuilder.CreateIndex(
                name: "IX_PrepPlanConsumers_GeneratorId",
                table: "PrepPlanConsumers",
                column: "GeneratorId");

            migrationBuilder.CreateIndex(
                name: "IX_PrepPlanConsumers_PrepPlanId",
                table: "PrepPlanConsumers",
                column: "PrepPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_PrepPlanGenerators_PrepPlanId",
                table: "PrepPlanGenerators",
                column: "PrepPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_IngredientId",
                table: "RecipeIngredients",
                column: "IngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_MeasureTypeId",
                table: "RecipeIngredients",
                column: "MeasureTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_RecipeId",
                table: "RecipeIngredients",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingListItemMeal_MealId",
                table: "ShoppingListItemMeal",
                column: "MealId");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingListItemMeal_ShoppingListItemId",
                table: "ShoppingListItemMeal",
                column: "ShoppingListItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_Meals_Preparations_PreparationId",
                table: "Meals",
                column: "PreparationId",
                principalTable: "Preparations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Meals_Recipes_RecipeId",
                table: "Meals",
                column: "RecipeId",
                principalTable: "Recipes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Meals_ScheduleDays_ScheduleDayId",
                table: "Meals",
                column: "ScheduleDayId",
                principalTable: "ScheduleDays",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RecipeSteps_Recipes_RecipeId",
                table: "RecipeSteps",
                column: "RecipeId",
                principalTable: "Recipes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RecipeVotes_Recipes_RecipeId",
                table: "RecipeVotes",
                column: "RecipeId",
                principalTable: "Recipes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Meals_Preparations_PreparationId",
                table: "Meals");

            migrationBuilder.DropForeignKey(
                name: "FK_Meals_Recipes_RecipeId",
                table: "Meals");

            migrationBuilder.DropForeignKey(
                name: "FK_Meals_ScheduleDays_ScheduleDayId",
                table: "Meals");

            migrationBuilder.DropForeignKey(
                name: "FK_RecipeSteps_Recipes_RecipeId",
                table: "RecipeSteps");

            migrationBuilder.DropForeignKey(
                name: "FK_RecipeVotes_Recipes_RecipeId",
                table: "RecipeVotes");

            migrationBuilder.DropTable(
                name: "Preparations");

            migrationBuilder.DropTable(
                name: "PrepPlanConsumers");

            migrationBuilder.DropTable(
                name: "RecipeDietTypes");

            migrationBuilder.DropTable(
                name: "RecipeIngredients");

            migrationBuilder.DropTable(
                name: "ShoppingListItemMeal");

            migrationBuilder.DropTable(
                name: "PrepPlanGenerators");

            migrationBuilder.DropTable(
                name: "Recipes");

            migrationBuilder.DropTable(
                name: "PrepPlans");

            migrationBuilder.DropIndex(
                name: "IX_Meals_PreparationId",
                table: "Meals");

            migrationBuilder.DropIndex(
                name: "IX_Meals_RecipeId",
                table: "Meals");

            migrationBuilder.DropIndex(
                name: "IX_Meals_ScheduleDayId",
                table: "Meals");

            migrationBuilder.DropColumn(
                name: "RecipeStyle",
                table: "MenuPreferences");

            migrationBuilder.DropColumn(
                name: "ConfirmStatus",
                table: "Meals");

            migrationBuilder.DropColumn(
                name: "IsChallenge",
                table: "Meals");

            migrationBuilder.DropColumn(
                name: "IsLeftovers",
                table: "Meals");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Meals");

            migrationBuilder.RenameColumn(
                name: "RecipeId",
                table: "RecipeSteps",
                newName: "MealId");

            migrationBuilder.RenameIndex(
                name: "IX_RecipeSteps_RecipeId",
                table: "RecipeSteps",
                newName: "IX_RecipeSteps_MealId");

            migrationBuilder.RenameColumn(
                name: "Servings",
                table: "Meals",
                newName: "PrepTime");

            migrationBuilder.RenameColumn(
                name: "ScheduleDayId",
                table: "Meals",
                newName: "NumServings");

            migrationBuilder.RenameColumn(
                name: "RecipeId",
                table: "Meals",
                newName: "MealType");

            migrationBuilder.RenameColumn(
                name: "PreparationId",
                table: "Meals",
                newName: "CookTime");

            migrationBuilder.AddColumn<int>(
                name: "MealStyle",
                table: "MenuPreferences",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Brief",
                table: "Meals",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Meals",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Image",
                table: "Meals",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Meals",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Meals",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "Meals",
                type: "varchar(200)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MealDietTypes",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
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

            migrationBuilder.CreateTable(
                name: "MealIngredients",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Amount = table.Column<float>(nullable: false),
                    AmountType = table.Column<string>(nullable: true),
                    IngredientId = table.Column<int>(nullable: false),
                    MealId = table.Column<int>(nullable: false),
                    MeasureTypeId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MealIngredients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MealIngredients_Ingredients_IngredientId",
                        column: x => x.IngredientId,
                        principalTable: "Ingredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MealIngredients_Meals_MealId",
                        column: x => x.MealId,
                        principalTable: "Meals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MealIngredients_MeasureTypes_MeasureTypeId",
                        column: x => x.MeasureTypeId,
                        principalTable: "MeasureTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScheduleSlots",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ConfirmStatus = table.Column<int>(nullable: false),
                    IsChallenge = table.Column<bool>(nullable: false),
                    MealId = table.Column<int>(nullable: false),
                    ScheduleDayId = table.Column<int>(nullable: false),
                    Type = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduleSlots_Meals_MealId",
                        column: x => x.MealId,
                        principalTable: "Meals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScheduleSlots_ScheduleDays_ScheduleDayId",
                        column: x => x.ScheduleDayId,
                        principalTable: "ScheduleDays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShoppingListItemScheduleSlot",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
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

            migrationBuilder.CreateIndex(
                name: "IX_Meals_Slug",
                table: "Meals",
                column: "Slug");

            migrationBuilder.CreateIndex(
                name: "IX_MealDietTypes_DietTypeId",
                table: "MealDietTypes",
                column: "DietTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_MealDietTypes_MealId",
                table: "MealDietTypes",
                column: "MealId");

            migrationBuilder.CreateIndex(
                name: "IX_MealIngredients_IngredientId",
                table: "MealIngredients",
                column: "IngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_MealIngredients_MealId",
                table: "MealIngredients",
                column: "MealId");

            migrationBuilder.CreateIndex(
                name: "IX_MealIngredients_MeasureTypeId",
                table: "MealIngredients",
                column: "MeasureTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleSlots_MealId",
                table: "ScheduleSlots",
                column: "MealId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleSlots_ScheduleDayId",
                table: "ScheduleSlots",
                column: "ScheduleDayId");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingListItemScheduleSlot_ScheduleSlotId",
                table: "ShoppingListItemScheduleSlot",
                column: "ScheduleSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingListItemScheduleSlot_ShoppingListItemId",
                table: "ShoppingListItemScheduleSlot",
                column: "ShoppingListItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_RecipeSteps_Meals_MealId",
                table: "RecipeSteps",
                column: "MealId",
                principalTable: "Meals",
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
    }
}
