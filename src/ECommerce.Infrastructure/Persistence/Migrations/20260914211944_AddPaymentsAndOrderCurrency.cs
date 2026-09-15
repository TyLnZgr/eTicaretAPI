using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentsAndOrderCurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_Status_Valid",
                table: "Orders");

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Orders",
                type: "TEXT",
                fixedLength: true,
                maxLength: 3,
                nullable: false,
                defaultValue: "TRY");

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrderId = table.Column<int>(type: "INTEGER", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    RequestFingerprint = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 64, nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 3, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Provider = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ProviderPaymentId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    FailureCode = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.CheckConstraint("CK_Payments_Amount_Positive", "CAST(\"Amount\" AS NUMERIC) > 0");
                    table.CheckConstraint("CK_Payments_Currency_Valid", "length(\"Currency\") = 3 AND \"Currency\" = upper(\"Currency\")");
                    table.CheckConstraint("CK_Payments_IdempotencyKey_Valid", "length(trim(\"IdempotencyKey\")) BETWEEN 8 AND 100");
                    table.CheckConstraint("CK_Payments_Provider_Valid", "length(trim(\"Provider\")) BETWEEN 1 AND 100");
                    table.CheckConstraint("CK_Payments_RequestFingerprint_Valid", "length(\"RequestFingerprint\") = 64");
                    table.CheckConstraint("CK_Payments_Result_Consistent", "(\"Status\" = 1 AND \"ProviderPaymentId\" IS NULL AND \"FailureCode\" IS NULL) OR (\"Status\" = 2 AND \"ProviderPaymentId\" IS NOT NULL AND \"FailureCode\" IS NULL) OR (\"Status\" = 3 AND \"FailureCode\" IS NOT NULL)");
                    table.CheckConstraint("CK_Payments_Status_Valid", "\"Status\" IN (1, 2, 3)");
                    table.ForeignKey(
                        name: "FK_Payments_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_Currency_Valid",
                table: "Orders",
                sql: "length(\"Currency\") = 3 AND \"Currency\" = upper(\"Currency\")");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_Status_Valid",
                table: "Orders",
                sql: "\"Status\" IN (1, 2, 3, 4, 5, 6)");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_OrderId_CreatedAtUtc",
                table: "Payments",
                columns: new[] { "OrderId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_OrderId_IdempotencyKey",
                table: "Payments",
                columns: new[] { "OrderId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Payments_OrderId_Active",
                table: "Payments",
                column: "OrderId",
                unique: true,
                filter: "\"Status\" IN (1, 2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_Currency_Valid",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_Status_Valid",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Orders");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_Status_Valid",
                table: "Orders",
                sql: "\"Status\" IN (1, 2, 3, 4, 5)");
        }
    }
}
