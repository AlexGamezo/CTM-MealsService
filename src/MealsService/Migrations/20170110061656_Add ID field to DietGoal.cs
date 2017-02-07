using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;

namespace MealsService.Migrations
{
    public partial class AddIDfieldtoDietGoal : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_DietGoals",
                table: "DietGoals");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "DietGoals",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_DietGoals",
                table: "DietGoals",
                column: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_DietGoals",
                table: "DietGoals");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "DietGoals");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DietGoals",
                table: "DietGoals",
                column: "UserId");
        }
    }
}
