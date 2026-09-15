using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionalOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DeduplicationKey = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Payload = table.Column<string>(type: "TEXT", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AttemptCount = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    NextAttemptAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastError = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                    table.CheckConstraint("CK_OutboxMessages_AttemptCount_NonNegative", "\"AttemptCount\" >= 0");
                    table.CheckConstraint("CK_OutboxMessages_DeduplicationKey_Valid", "length(trim(\"DeduplicationKey\")) BETWEEN 1 AND 200");
                    table.CheckConstraint("CK_OutboxMessages_Payload_NotEmpty", "length(trim(\"Payload\")) > 0");
                    table.CheckConstraint("CK_OutboxMessages_ProcessedState_Consistent", "\"ProcessedAtUtc\" IS NULL OR (\"NextAttemptAtUtc\" IS NULL AND \"LastError\" IS NULL)");
                    table.CheckConstraint("CK_OutboxMessages_Type_Valid", "length(trim(\"Type\")) BETWEEN 1 AND 200");
                });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_DeduplicationKey",
                table: "OutboxMessages",
                column: "DeduplicationKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedAtUtc_NextAttemptAtUtc_OccurredAtUtc",
                table: "OutboxMessages",
                columns: new[] { "ProcessedAtUtc", "NextAttemptAtUtc", "OccurredAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OutboxMessages");
        }
    }
}
