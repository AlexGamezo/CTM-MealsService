using Microsoft.EntityFrameworkCore.Migrations;

namespace MealsService.Migrations
{
    public partial class addIsMeasuredVolumefield : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsMeasuredVolume",
                table: "Ingredients",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsMeasuredVolume",
                table: "Ingredients");
        }
    }
}
