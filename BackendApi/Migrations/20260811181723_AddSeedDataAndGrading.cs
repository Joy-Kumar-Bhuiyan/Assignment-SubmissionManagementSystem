using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BackendApi.Migrations
{
    /// <inheritdoc />
    public partial class AddSeedDataAndGrading : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Feedback",
                table: "Submissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Marks",
                table: "Submissions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Submissions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Password = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Email", "Password", "Role" },
                values: new object[,]
                {
                    { 1, "admin@school.com", "AdminPassword123!", "Admin" },
                    { 2, "teacher@school.com", "TeacherPassword123!", "Teacher" },
                    { 3, "student@school.com", "StudentPassword123!", "Student" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropColumn(
                name: "Feedback",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "Marks",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Submissions");
        }
    }
}
