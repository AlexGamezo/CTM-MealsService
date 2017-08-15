using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MealsService.Migrations
{
    public partial class addedtrackingofschedulegenerations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScheduleGenerations",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGeneratedOnAdd", true),
                    Created = table.Column<DateTime>(nullable: false)
                        .Annotation("MySql:ValueGeneratedOnAdd", true),
                    UserId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleGenerations", x => x.Id);
                });

            migrationBuilder.AlterColumn<DateTime>(
                name: "Modified",
                table: "ScheduleDays",
                nullable: false,
                oldClrType: typeof(DateTime))
                .Annotation("MySql:ValueGeneratedOnAddOrUpdate", true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "Created",
                table: "ScheduleDays",
                nullable: false,
                oldClrType: typeof(DateTime))
                .Annotation("MySql:ValueGeneratedOnAdd", true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScheduleGenerations");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Modified",
                table: "ScheduleDays",
                nullable: false,
                oldClrType: typeof(DateTime))
                .OldAnnotation("MySql:ValueGeneratedOnAddOrUpdate", true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "Created",
                table: "ScheduleDays",
                nullable: false,
                oldClrType: typeof(DateTime))
                .OldAnnotation("MySql:ValueGeneratedOnAdd", true);
        }
    }
}
