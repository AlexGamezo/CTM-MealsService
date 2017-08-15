using Microsoft.EntityFrameworkCore.Migrations;

namespace MealsService.Migrations
{
    public partial class Addedingredientimages : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExcludedTags",
                table: "ScheduleGenerations",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Image",
                table: "Ingredients",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExcludedTags",
                table: "ScheduleGenerations");

            migrationBuilder.DropColumn(
                name: "Image",
                table: "Ingredients");
        }
    }
}
