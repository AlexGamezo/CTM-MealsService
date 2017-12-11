using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MealsService.Migrations
{
    public partial class addedscheduleconfirmation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScheduleSlotConfirmations",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGeneratedOnAdd", true),
                    Confirm = table.Column<int>(nullable: false),
                    ScheduleSlotId = table.Column<int>(nullable: false),
                    UserId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleSlotConfirmations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduleSlotConfirmations_ScheduleSlots_ScheduleSlotId",
                        column: x => x.ScheduleSlotId,
                        principalTable: "ScheduleSlots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleSlotConfirmations_ScheduleSlotId",
                table: "ScheduleSlotConfirmations",
                column: "ScheduleSlotId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleSlotConfirmations_UserId_ScheduleSlotId",
                table: "ScheduleSlotConfirmations",
                columns: new[] { "UserId", "ScheduleSlotId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScheduleSlotConfirmations");
        }
    }
}
