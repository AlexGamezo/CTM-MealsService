using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace MealsService.Migrations
{
    public partial class StatsSnapshots : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImpactStatements",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Alt = table.Column<string>(nullable: true),
                    ImpactType = table.Column<int>(nullable: false),
                    ParametersRaw = table.Column<string>(nullable: true),
                    RefUrl = table.Column<string>(nullable: true),
                    Text = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImpactStatements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StatSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Challenges = table.Column<int>(nullable: false),
                    Goal = table.Column<int>(nullable: false),
                    MealsPerDay = table.Column<int>(nullable: false),
                    Streak = table.Column<int>(nullable: false),
                    UserId = table.Column<int>(nullable: false),
                    Value = table.Column<int>(nullable: false),
                    Week = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatSnapshots", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImpactStatements");

            migrationBuilder.DropTable(
                name: "StatSnapshots");
        }
    }
}
