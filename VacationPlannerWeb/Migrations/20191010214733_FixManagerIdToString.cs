using Microsoft.EntityFrameworkCore.Migrations;

namespace VacationPlannerWeb.Migrations
{
    public partial class FixManagerIdToString : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ManagerUserId",
                table: "AspNetUsers",
                nullable: true,
                oldClrType: typeof(int),
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "ManagerUserId",
                table: "AspNetUsers",
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);
        }
    }
}
