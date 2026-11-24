using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Analytical_Engine.Migrations
{
    /// <inheritdoc />
    public partial class AddReportUserCreatedAtIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reports_UserId",
                table: "Reports");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_UserId_CreatedAt",
                table: "Reports",
                columns: new[] { "UserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reports_UserId_CreatedAt",
                table: "Reports");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_UserId",
                table: "Reports",
                column: "UserId");
        }
    }
}
