using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductSoftDeleteAndAuditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Products",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                table: "Products",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Products",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "Products",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddCheckConstraint(
                name: "CK_Products_DeletionState_Valid",
                table: "Products",
                sql: "(\"IsDeleted\" = 0 AND \"DeletedAtUtc\" IS NULL) OR (\"IsDeleted\" = 1 AND \"DeletedAtUtc\" IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Products_IsDeleted_Valid",
                table: "Products",
                sql: "\"IsDeleted\" IN (0, 1)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Products_DeletionState_Valid",
                table: "Products");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Products_IsDeleted_Valid",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "Products");
        }
    }
}
