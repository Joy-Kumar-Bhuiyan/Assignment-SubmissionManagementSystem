using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BackendApi.Migrations
{
    /// <inheritdoc />
    public partial class UnifyTeacherAllocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO "TeacherAllocations" ("TeacherEmail", "ClassId", "SubjectId")
                SELECT "TeacherEmail", "ClassId", "SubjectId"
                FROM "TeacherMappings"
                ON CONFLICT ("TeacherEmail", "ClassId", "SubjectId") DO NOTHING;
                """);

            migrationBuilder.DropTable(
                name: "TeacherMappings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TeacherMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClassId = table.Column<int>(type: "integer", nullable: false),
                    SubjectId = table.Column<int>(type: "integer", nullable: false),
                    TeacherEmail = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_TeacherMappings_TeacherEmail_ClassId_SubjectId",
                table: "TeacherMappings",
                columns: new[] { "TeacherEmail", "ClassId", "SubjectId" },
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO "TeacherMappings" ("TeacherEmail", "ClassId", "SubjectId")
                SELECT "TeacherEmail", "ClassId", "SubjectId"
                FROM "TeacherAllocations"
                ON CONFLICT ("TeacherEmail", "ClassId", "SubjectId") DO NOTHING;
                """);
        }
    }
}
