using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IIASA.FieldSurvey.Migrations
{
    /// <inheritdoc />
    public partial class ReviewStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "FS_Surveys",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "FS_Surveys");
        }
    }
}
