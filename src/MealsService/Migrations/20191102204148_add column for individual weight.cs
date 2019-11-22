using Microsoft.EntityFrameworkCore.Migrations;

namespace MealsService.Migrations
{
    public partial class addcolumnforindividualweight : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "IndividualWeight",
                table: "Ingredients",
                nullable: false,
                defaultValue: 0.0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IndividualWeight",
                table: "Ingredients");
        }
    }
}
