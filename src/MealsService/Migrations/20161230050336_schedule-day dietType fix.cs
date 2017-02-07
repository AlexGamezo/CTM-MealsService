using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MealsService.Migrations
{
    public partial class scheduledaydietTypefix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScheduleDays_DietTypes_DietTypeId1",
                table: "ScheduleDays");

            migrationBuilder.DropForeignKey(
                name: "FK_ScheduleDays_DietTypes_DietTypeIdId",
                table: "ScheduleDays");

            migrationBuilder.DropIndex(
                name: "IX_ScheduleDays_DietTypeId1",
                table: "ScheduleDays");

            migrationBuilder.DropIndex(
                name: "IX_ScheduleDays_DietTypeIdId",
                table: "ScheduleDays");

            migrationBuilder.DropColumn(
                name: "DietTypeId1",
                table: "ScheduleDays");

            migrationBuilder.DropColumn(
                name: "DietTypeIdId",
                table: "ScheduleDays");

            migrationBuilder.AddColumn<int>(
                name: "DietTypeId",
                table: "ScheduleDays",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleDays_DietTypeId",
                table: "ScheduleDays",
                column: "DietTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduleDays_DietTypes_DietTypeId",
                table: "ScheduleDays",
                column: "DietTypeId",
                principalTable: "DietTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScheduleDays_DietTypes_DietTypeId",
                table: "ScheduleDays");

            migrationBuilder.DropIndex(
                name: "IX_ScheduleDays_DietTypeId",
                table: "ScheduleDays");

            migrationBuilder.DropColumn(
                name: "DietTypeId",
                table: "ScheduleDays");

            migrationBuilder.AddColumn<int>(
                name: "DietTypeId1",
                table: "ScheduleDays",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DietTypeIdId",
                table: "ScheduleDays",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleDays_DietTypeId1",
                table: "ScheduleDays",
                column: "DietTypeId1");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleDays_DietTypeIdId",
                table: "ScheduleDays",
                column: "DietTypeIdId");

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduleDays_DietTypes_DietTypeId1",
                table: "ScheduleDays",
                column: "DietTypeId1",
                principalTable: "DietTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduleDays_DietTypes_DietTypeIdId",
                table: "ScheduleDays",
                column: "DietTypeIdId",
                principalTable: "DietTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
