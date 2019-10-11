using Microsoft.EntityFrameworkCore.Migrations;

namespace VacationPlannerWeb.Migrations
{
    public partial class ManagerUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ManagerUserId",
                table: "AspNetUsers",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ManagerUserId",
                table: "AspNetUsers");
        }
    }
}
