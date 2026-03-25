using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Runner.Submissions.Module.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "submissions");

            migrationBuilder.CreateTable(
                name: "assignments",
                schema: "submissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    GitLabProjectId = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CoverageThreshold = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assignments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "submissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "user_profiles",
                schema: "submissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GitHubLogin = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    GitHubId = table.Column<long>(type: "bigint", nullable: false),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_profiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "submissions",
                schema: "submissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    GitHubUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Branch = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    GitLabPipelineId = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_submissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_submissions_assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalSchema: "submissions",
                        principalTable: "assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "check_results",
                schema: "submissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalTests = table.Column<int>(type: "integer", nullable: false),
                    PassedTests = table.Column<int>(type: "integer", nullable: false),
                    FailedTests = table.Column<int>(type: "integer", nullable: false),
                    RawNUnitXml = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_check_results", x => x.Id);
                    table.ForeignKey(
                        name: "FK_check_results_submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalSchema: "submissions",
                        principalTable: "submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "test_group_results",
                schema: "submissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CheckResultId = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Passed = table.Column<int>(type: "integer", nullable: false),
                    Failed = table.Column<int>(type: "integer", nullable: false),
                    ErrorType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_test_group_results", x => x.Id);
                    table.ForeignKey(
                        name: "FK_test_group_results_check_results_CheckResultId",
                        column: x => x.CheckResultId,
                        principalSchema: "submissions",
                        principalTable: "check_results",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_check_results_SubmissionId",
                schema: "submissions",
                table: "check_results",
                column: "SubmissionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_ProcessedAt_RetryCount",
                schema: "submissions",
                table: "outbox_messages",
                columns: new[] { "ProcessedAt", "RetryCount" });

            migrationBuilder.CreateIndex(
                name: "IX_submissions_AssignmentId",
                schema: "submissions",
                table: "submissions",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_test_group_results_CheckResultId",
                schema: "submissions",
                table: "test_group_results",
                column: "CheckResultId");

            migrationBuilder.CreateIndex(
                name: "IX_user_profiles_GitHubId",
                schema: "submissions",
                table: "user_profiles",
                column: "GitHubId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_profiles_GitHubLogin",
                schema: "submissions",
                table: "user_profiles",
                column: "GitHubLogin",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "submissions");

            migrationBuilder.DropTable(
                name: "test_group_results",
                schema: "submissions");

            migrationBuilder.DropTable(
                name: "user_profiles",
                schema: "submissions");

            migrationBuilder.DropTable(
                name: "check_results",
                schema: "submissions");

            migrationBuilder.DropTable(
                name: "submissions",
                schema: "submissions");

            migrationBuilder.DropTable(
                name: "assignments",
                schema: "submissions");
        }
    }
}
