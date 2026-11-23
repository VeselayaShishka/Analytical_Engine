using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Analytical_Engine.Migrations
{
    /// <inheritdoc />
    public partial class AddDatasetUniqueNameAndRecordPaginationIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Datasets_UserId",
                table: "Datasets");

            migrationBuilder.DropIndex(
                name: "IX_DataRecords_DatasetVersionId",
                table: "DataRecords");

            migrationBuilder.CreateIndex(
                name: "IX_Datasets_UserId_Name",
                table: "Datasets",
                columns: new[] { "UserId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DataRecords_DatasetVersionId_RowNumber",
                table: "DataRecords",
                columns: new[] { "DatasetVersionId", "RowNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Datasets_UserId_Name",
                table: "Datasets");

            migrationBuilder.DropIndex(
                name: "IX_DataRecords_DatasetVersionId_RowNumber",
                table: "DataRecords");

            migrationBuilder.CreateIndex(
                name: "IX_Datasets_UserId",
                table: "Datasets",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DataRecords_DatasetVersionId",
                table: "DataRecords",
                column: "DatasetVersionId");
        }
    }
}
