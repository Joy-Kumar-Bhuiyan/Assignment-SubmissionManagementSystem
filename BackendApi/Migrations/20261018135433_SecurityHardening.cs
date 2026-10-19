using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BackendApi.Migrations
{
    /// <inheritdoc />
    public partial class SecurityHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            // Merge the accidentally duplicated legacy class table into the canonical
            // Classes table before changing the foreign keys. Matching by the natural
            // (Name, Section) key avoids corrupting relationships when numeric IDs overlap.
            migrationBuilder.Sql(
                """
                INSERT INTO "Classes" ("Name", "Section")
                SELECT DISTINCT legacy."Name", legacy."Section"
                FROM "ClassEntity" AS legacy
                WHERE NOT EXISTS (
                    SELECT 1 FROM "Classes" AS current
                    WHERE current."Name" = legacy."Name"
                      AND current."Section" = legacy."Section"
                );

                UPDATE "Assignments" AS item
                SET "ClassId" = current."Id"
                FROM "ClassEntity" AS legacy
                JOIN "Classes" AS current
                  ON current."Name" = legacy."Name"
                 AND current."Section" = legacy."Section"
                WHERE item."ClassId" = legacy."Id";

                UPDATE "TeacherAllocations" AS item
                SET "ClassId" = current."Id"
                FROM "ClassEntity" AS legacy
                JOIN "Classes" AS current
                  ON current."Name" = legacy."Name"
                 AND current."Section" = legacy."Section"
                WHERE item."ClassId" = legacy."Id";

                UPDATE "Users" AS item
                SET "ClassId" = current."Id"
                FROM "ClassEntity" AS legacy
                JOIN "Classes" AS current
                  ON current."Name" = legacy."Name"
                 AND current."Section" = legacy."Section"
                WHERE item."ClassId" = legacy."Id";
                """);

            migrationBuilder.DropTable(
                name: "ClassEntity");

            migrationBuilder.DropIndex(
                name: "IX_Submissions_AssignmentId",
                table: "Submissions");

            migrationBuilder.Sql(
                """
                UPDATE "Users" SET "Email" = lower(trim("Email"));
                UPDATE "TeacherAllocations" SET "TeacherEmail" = lower(trim("TeacherEmail"));
                UPDATE "TeacherMappings" SET "TeacherEmail" = lower(trim("TeacherEmail"));
                UPDATE "Submissions" SET "StudentEmail" = lower(trim("StudentEmail"));
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "Users",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "character varying(254)",
                maxLength: 254,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "TokenVersion",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "TeacherEmail",
                table: "TeacherMappings",
                type: "character varying(254)",
                maxLength: 254,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "TeacherEmail",
                table: "TeacherAllocations",
                type: "character varying(254)",
                maxLength: 254,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Value",
                table: "SystemSettings",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Key",
                table: "SystemSettings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "StudentEmail",
                table: "Submissions",
                type: "character varying(254)",
                maxLength: 254,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Submissions",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Feedback",
                table: "Submissions",
                type: "character varying(5000)",
                maxLength: 5000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AnswerText",
                table: "Submissions",
                type: "character varying(50000)",
                maxLength: 50000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<bool>(
                name: "IsLate",
                table: "Submissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Subjects",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Subjects",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Section",
                table: "Classes",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Classes",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Assignments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Assignments",
                type: "character varying(10000)",
                maxLength: 10000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeacherMappings_TeacherEmail_ClassId_SubjectId",
                table: "TeacherMappings",
                columns: new[] { "TeacherEmail", "ClassId", "SubjectId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeacherAllocations_TeacherEmail_ClassId_SubjectId",
                table: "TeacherAllocations",
                columns: new[] { "TeacherEmail", "ClassId", "SubjectId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_Key",
                table: "SystemSettings",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_AssignmentId_StudentEmail",
                table: "Submissions",
                columns: new[] { "AssignmentId", "StudentEmail" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Submissions_Marks",
                table: "Submissions",
                sql: "\"Marks\" IS NULL OR \"Marks\" >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_Code",
                table: "Subjects",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Classes_Name_Section",
                table: "Classes",
                columns: new[] { "Name", "Section" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Assignments_MaxMarks",
                table: "Assignments",
                sql: "\"MaxMarks\" > 0 AND \"MaxMarks\" <= 10000");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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

            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_TeacherMappings_TeacherEmail_ClassId_SubjectId",
                table: "TeacherMappings");

            migrationBuilder.DropIndex(
                name: "IX_TeacherAllocations_TeacherEmail_ClassId_SubjectId",
                table: "TeacherAllocations");

            migrationBuilder.DropIndex(
                name: "IX_SystemSettings_Key",
                table: "SystemSettings");

            migrationBuilder.DropIndex(
                name: "IX_Submissions_AssignmentId_StudentEmail",
                table: "Submissions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Submissions_Marks",
                table: "Submissions");

            migrationBuilder.DropIndex(
                name: "IX_Subjects_Code",
                table: "Subjects");

            migrationBuilder.DropIndex(
                name: "IX_Classes_Name_Section",
                table: "Classes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Assignments_MaxMarks",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "TokenVersion",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsLate",
                table: "Submissions");

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "Users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(16)",
                oldMaxLength: 16);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(254)",
                oldMaxLength: 254);

            migrationBuilder.AlterColumn<string>(
                name: "TeacherEmail",
                table: "TeacherMappings",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(254)",
                oldMaxLength: 254);

            migrationBuilder.AlterColumn<string>(
                name: "TeacherEmail",
                table: "TeacherAllocations",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(254)",
                oldMaxLength: 254);

            migrationBuilder.AlterColumn<string>(
                name: "Value",
                table: "SystemSettings",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AlterColumn<string>(
                name: "Key",
                table: "SystemSettings",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "StudentEmail",
                table: "Submissions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(254)",
                oldMaxLength: 254);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Submissions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "Feedback",
                table: "Submissions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(5000)",
                oldMaxLength: 5000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AnswerText",
                table: "Submissions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50000)",
                oldMaxLength: 50000);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Subjects",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Subjects",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "Section",
                table: "Classes",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Classes",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Assignments",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Assignments",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10000)",
                oldMaxLength: 10000);

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

            migrationBuilder.Sql(
                """
                INSERT INTO "ClassEntity" ("Id", "Name", "Section")
                SELECT "Id", "Name", "Section" FROM "Classes";

                SELECT setval(
                    pg_get_serial_sequence('"ClassEntity"', 'Id'),
                    COALESCE((SELECT MAX("Id") FROM "ClassEntity"), 1)
                );
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_AssignmentId",
                table: "Submissions",
                column: "AssignmentId");

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
    }
}
