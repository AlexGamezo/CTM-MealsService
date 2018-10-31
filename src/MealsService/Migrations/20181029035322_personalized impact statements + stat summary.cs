using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace MealsService.Migrations
{
    public partial class personalizedimpactstatementsstatsummary : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CalcType",
                table: "ImpactStatements",
                nullable: false,
                defaultValue: 0);

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

            migrationBuilder.CreateTable(
                name: "StatSummaries",
                columns: table => new
                {
                    UserId = table.Column<int>(nullable: false),
                    CurrentStreak = table.Column<int>(nullable: false),
                    MealsPerWeek = table.Column<int>(nullable: false),
                    NumChallenges = table.Column<int>(nullable: false),
                    NumMeals = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatSummaries", x => x.UserId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MeasureConverters_SourceMeasureId",
                table: "MeasureConverters",
                column: "SourceMeasureId");

            migrationBuilder.CreateIndex(
                name: "IX_MeasureConverters_TargetMeasureTypeId",
                table: "MeasureConverters",
                column: "TargetMeasureTypeId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MeasureConverters");

            migrationBuilder.DropTable(
                name: "StatSummaries");

            migrationBuilder.DropColumn(
                name: "CalcType",
                table: "ImpactStatements");
        }
    }
}
