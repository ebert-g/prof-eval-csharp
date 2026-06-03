using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProfEval.Api.Migrations
{
    /// <inheritdoc />
    public partial class MakeEvaluationAnonymous : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_evaluations_students_student_id",
                table: "evaluations");

            migrationBuilder.AlterColumn<int>(
                name: "student_id",
                table: "evaluations",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "anonymous_token",
                table: "evaluations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_evaluations_anonymous_token",
                table: "evaluations",
                column: "anonymous_token",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_evaluations_students_student_id",
                table: "evaluations",
                column: "student_id",
                principalTable: "students",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_evaluations_students_student_id",
                table: "evaluations");

            migrationBuilder.DropIndex(
                name: "IX_evaluations_anonymous_token",
                table: "evaluations");

            migrationBuilder.DropColumn(
                name: "anonymous_token",
                table: "evaluations");

            migrationBuilder.AlterColumn<int>(
                name: "student_id",
                table: "evaluations",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_evaluations_students_student_id",
                table: "evaluations",
                column: "student_id",
                principalTable: "students",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
