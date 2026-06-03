using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ProfEval.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "professors",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    department = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    specialization = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_professors", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "students",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    registration = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    email_verified = table.Column<bool>(type: "boolean", nullable: false),
                    verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_students", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "verification_codes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    code = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    is_used = table.Column<bool>(type: "boolean", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_verification_codes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "evaluations",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    student_id = table.Column<int>(type: "integer", nullable: false),
                    professor_id = table.Column<int>(type: "integer", nullable: false),
                    score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    evaluation_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evaluations", x => x.id);
                    table.ForeignKey(
                        name: "FK_evaluations_professors_professor_id",
                        column: x => x.professor_id,
                        principalTable: "professors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_evaluations_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_evaluations_professor_id",
                table: "evaluations",
                column: "professor_id");

            migrationBuilder.CreateIndex(
                name: "IX_evaluations_student_id_professor_id",
                table: "evaluations",
                columns: new[] { "student_id", "professor_id" });

            migrationBuilder.CreateIndex(
                name: "IX_professors_email",
                table: "professors",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_students_email",
                table: "students",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_verification_codes_email_code",
                table: "verification_codes",
                columns: new[] { "email", "code" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "evaluations");

            migrationBuilder.DropTable(
                name: "verification_codes");

            migrationBuilder.DropTable(
                name: "professors");

            migrationBuilder.DropTable(
                name: "students");
        }
    }
}
