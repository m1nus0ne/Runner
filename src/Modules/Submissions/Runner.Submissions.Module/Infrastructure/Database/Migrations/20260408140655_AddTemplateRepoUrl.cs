using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Runner.Submissions.Module.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddTemplateRepoUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TemplateRepoUrl",
                schema: "submissions",
                table: "assignments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TemplateRepoUrl",
                schema: "submissions",
                table: "assignments");
        }
    }
}
