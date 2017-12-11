using Microsoft.EntityFrameworkCore.Migrations;

namespace MealsService.Migrations
{
    public partial class addedsomeindexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ShoppingListItems_UserId_WeekStart",
                table: "ShoppingListItems",
                columns: new[] { "UserId", "WeekStart" });

            migrationBuilder.CreateIndex(
                name: "IX_RecipeVotes_UserId",
                table: "RecipeVotes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleGenerations_UserId",
                table: "ScheduleGenerations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleDays_UserId",
                table: "ScheduleDays",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DietGoals_UserId",
                table: "DietGoals",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShoppingListItems_UserId_WeekStart",
                table: "ShoppingListItems");

            migrationBuilder.DropIndex(
                name: "IX_RecipeVotes_UserId",
                table: "RecipeVotes");

            migrationBuilder.DropIndex(
                name: "IX_ScheduleGenerations_UserId",
                table: "ScheduleGenerations");

            migrationBuilder.DropIndex(
                name: "IX_ScheduleDays_UserId",
                table: "ScheduleDays");

            migrationBuilder.DropIndex(
                name: "IX_DietGoals_UserId",
                table: "DietGoals");
        }
    }
}
