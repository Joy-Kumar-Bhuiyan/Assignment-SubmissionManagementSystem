using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BackendApi.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminEntitiesAndMappings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_Classes_ClassId",
                table: "Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_TeacherAllocations_Classes_ClassId",
                table: "TeacherAllocations");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Classes_ClassId",
                table: "Users");

            migrationBuilder.CreateTable(
                name: "ClassEntity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Section = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassEntity", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TeacherMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TeacherEmail = table.Column<string>(type: "text", nullable: false),
                    ClassId = table.Column<int>(type: "integer", nullable: false),
                    SubjectId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeacherMappings_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeacherMappings_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TeacherMappings_ClassId",
                table: "TeacherMappings",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherMappings_SubjectId",
                table: "TeacherMappings",
                column: "SubjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_ClassEntity_ClassId",
                table: "Assignments",
                column: "ClassId",
                principalTable: "ClassEntity",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TeacherAllocations_ClassEntity_ClassId",
                table: "TeacherAllocations",
                column: "ClassId",
                principalTable: "ClassEntity",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_ClassEntity_ClassId",
                table: "Users",
                column: "ClassId",
                principalTable: "ClassEntity",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_ClassEntity_ClassId",
                table: "Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_TeacherAllocations_ClassEntity_ClassId",
                table: "TeacherAllocations");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_ClassEntity_ClassId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "ClassEntity");

            migrationBuilder.DropTable(
                name: "TeacherMappings");

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_Classes_ClassId",
                table: "Assignments",
                column: "ClassId",
                principalTable: "Classes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TeacherAllocations_Classes_ClassId",
                table: "TeacherAllocations",
                column: "ClassId",
                principalTable: "Classes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Classes_ClassId",
                table: "Users",
                column: "ClassId",
                principalTable: "Classes",
                principalColumn: "Id");
        }
    }
}
