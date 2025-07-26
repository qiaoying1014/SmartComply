using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartComply.Migrations
{
  /// <inheritdoc />
  public partial class AddCorrectiveActionTable : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.CreateTable(
          name: "CorrectiveActions",
          columns: table => new
          {
            CorrectiveActionId = table.Column<int>(type: "integer", nullable: false)
                  .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            AuditId = table.Column<int>(type: "integer", nullable: false),
            Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
            RootCause = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
            ProposedAction = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
            ResponsiblePerson = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
            TargetDate = table.Column<DateTime>(type: "date", nullable: false),
            CompletionDate = table.Column<DateTime>(type: "date", nullable: true),
            Status = table.Column<int>(type: "integer", nullable: false),
            Remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
            CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            BeforeActionPhotoPath = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
            AfterActionPhotoPath = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_CorrectiveActions", x => x.CorrectiveActionId);
            table.ForeignKey(
                      name: "FK_CorrectiveActions_Audits_AuditId",
                      column: x => x.AuditId,
                      principalTable: "Audits",
                      principalColumn: "AuditId",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateIndex(
          name: "IX_CorrectiveActions_AuditId",
          table: "CorrectiveActions",
          column: "AuditId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropTable(
          name: "CorrectiveActions");
    }
  }
}
