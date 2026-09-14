using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxLeasingAndDeadLetters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_ProcessedAtUtc_NextAttemptAtUtc_OccurredAtUtc",
                table: "OutboxMessages");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OutboxMessages_ProcessedState_Consistent",
                table: "OutboxMessages");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeadLetteredAtUtc",
                table: "OutboxMessages",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LockId",
                table: "OutboxMessages",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LockedBy",
                table: "OutboxMessages",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedUntilUtc",
                table: "OutboxMessages",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_LockId",
                table: "OutboxMessages",
                column: "LockId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedAtUtc_DeadLetteredAtUtc_NextAttemptAtUtc_LockedUntilUtc_OccurredAtUtc",
                table: "OutboxMessages",
                columns: new[] { "ProcessedAtUtc", "DeadLetteredAtUtc", "NextAttemptAtUtc", "LockedUntilUtc", "OccurredAtUtc" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_OutboxMessages_Lease_Consistent",
                table: "OutboxMessages",
                sql: "(\"LockId\" IS NULL AND \"LockedBy\" IS NULL AND \"LockedUntilUtc\" IS NULL) OR (\"LockId\" IS NOT NULL AND \"LockedBy\" IS NOT NULL AND \"LockedUntilUtc\" IS NOT NULL AND \"ProcessedAtUtc\" IS NULL AND \"DeadLetteredAtUtc\" IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OutboxMessages_LockedBy_Valid",
                table: "OutboxMessages",
                sql: "\"LockedBy\" IS NULL OR length(trim(\"LockedBy\")) BETWEEN 1 AND 200");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OutboxMessages_ProcessedState_Consistent",
                table: "OutboxMessages",
                sql: "NOT (\"ProcessedAtUtc\" IS NOT NULL AND \"DeadLetteredAtUtc\" IS NOT NULL) AND (\"ProcessedAtUtc\" IS NULL OR (\"NextAttemptAtUtc\" IS NULL AND \"LastError\" IS NULL AND \"LockId\" IS NULL)) AND (\"DeadLetteredAtUtc\" IS NULL OR (\"NextAttemptAtUtc\" IS NULL AND \"LockId\" IS NULL AND \"AttemptCount\" > 0))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_LockId",
                table: "OutboxMessages");

            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_ProcessedAtUtc_DeadLetteredAtUtc_NextAttemptAtUtc_LockedUntilUtc_OccurredAtUtc",
                table: "OutboxMessages");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OutboxMessages_Lease_Consistent",
                table: "OutboxMessages");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OutboxMessages_LockedBy_Valid",
                table: "OutboxMessages");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OutboxMessages_ProcessedState_Consistent",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "DeadLetteredAtUtc",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "LockId",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "LockedBy",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "LockedUntilUtc",
                table: "OutboxMessages");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedAtUtc_NextAttemptAtUtc_OccurredAtUtc",
                table: "OutboxMessages",
                columns: new[] { "ProcessedAtUtc", "NextAttemptAtUtc", "OccurredAtUtc" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_OutboxMessages_ProcessedState_Consistent",
                table: "OutboxMessages",
                sql: "\"ProcessedAtUtc\" IS NULL OR (\"NextAttemptAtUtc\" IS NULL AND \"LastError\" IS NULL)");
        }
    }
}
