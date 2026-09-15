using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderCreationIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "Orders",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestFingerprint",
                table: "Orders",
                type: "TEXT",
                fixedLength: true,
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerId_IdempotencyKey",
                table: "Orders",
                columns: new[] { "CustomerId", "IdempotencyKey" },
                unique: true,
                filter: "\"IdempotencyKey\" IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_IdempotencyData_Complete",
                table: "Orders",
                sql: "(\"IdempotencyKey\" IS NULL AND \"RequestFingerprint\" IS NULL) OR (\"IdempotencyKey\" IS NOT NULL AND \"RequestFingerprint\" IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_IdempotencyKey_Valid",
                table: "Orders",
                sql: "\"IdempotencyKey\" IS NULL OR length(\"IdempotencyKey\") BETWEEN 8 AND 100");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_RequestFingerprint_Valid",
                table: "Orders",
                sql: "\"RequestFingerprint\" IS NULL OR (length(\"RequestFingerprint\") = 64 AND \"RequestFingerprint\" = upper(\"RequestFingerprint\"))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_CustomerId_IdempotencyKey",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_IdempotencyData_Complete",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_IdempotencyKey_Valid",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_RequestFingerprint_Valid",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RequestFingerprint",
                table: "Orders");
        }
    }
}
