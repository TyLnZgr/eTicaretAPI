using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderShippingAddressSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ShippingAddressLine1",
                table: "Orders",
                type: "TEXT",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingAddressLine2",
                table: "Orders",
                type: "TEXT",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingCity",
                table: "Orders",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingCountryCode",
                table: "Orders",
                type: "TEXT",
                fixedLength: true,
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingDistrict",
                table: "Orders",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingPhoneNumber",
                table: "Orders",
                type: "TEXT",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingPostalCode",
                table: "Orders",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingRecipientFullName",
                table: "Orders",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_ShippingAddress_Complete",
                table: "Orders",
                sql: "(\"ShippingRecipientFullName\" IS NULL AND \"ShippingPhoneNumber\" IS NULL AND \"ShippingAddressLine1\" IS NULL AND \"ShippingAddressLine2\" IS NULL AND \"ShippingDistrict\" IS NULL AND \"ShippingCity\" IS NULL AND \"ShippingPostalCode\" IS NULL AND \"ShippingCountryCode\" IS NULL) OR (\"ShippingRecipientFullName\" IS NOT NULL AND \"ShippingPhoneNumber\" IS NOT NULL AND \"ShippingAddressLine1\" IS NOT NULL AND \"ShippingDistrict\" IS NOT NULL AND \"ShippingCity\" IS NOT NULL AND \"ShippingPostalCode\" IS NOT NULL AND \"ShippingCountryCode\" IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_ShippingAddressLine1_Valid",
                table: "Orders",
                sql: "\"ShippingAddressLine1\" IS NULL OR length(trim(\"ShippingAddressLine1\")) BETWEEN 5 AND 300");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_ShippingAddressLine2_Valid",
                table: "Orders",
                sql: "\"ShippingAddressLine2\" IS NULL OR length(trim(\"ShippingAddressLine2\")) BETWEEN 1 AND 300");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_ShippingCity_Valid",
                table: "Orders",
                sql: "\"ShippingCity\" IS NULL OR length(trim(\"ShippingCity\")) BETWEEN 1 AND 100");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_ShippingCountryCode_Valid",
                table: "Orders",
                sql: "\"ShippingCountryCode\" IS NULL OR (length(\"ShippingCountryCode\") = 2 AND \"ShippingCountryCode\" = upper(\"ShippingCountryCode\"))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_ShippingDistrict_Valid",
                table: "Orders",
                sql: "\"ShippingDistrict\" IS NULL OR length(trim(\"ShippingDistrict\")) BETWEEN 1 AND 100");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_ShippingPhoneNumber_Valid",
                table: "Orders",
                sql: "\"ShippingPhoneNumber\" IS NULL OR length(trim(\"ShippingPhoneNumber\")) BETWEEN 3 AND 30");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_ShippingPostalCode_Valid",
                table: "Orders",
                sql: "\"ShippingPostalCode\" IS NULL OR length(trim(\"ShippingPostalCode\")) BETWEEN 1 AND 20");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_ShippingRecipientFullName_Valid",
                table: "Orders",
                sql: "\"ShippingRecipientFullName\" IS NULL OR length(trim(\"ShippingRecipientFullName\")) BETWEEN 2 AND 200");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_ShippingAddress_Complete",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_ShippingAddressLine1_Valid",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_ShippingAddressLine2_Valid",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_ShippingCity_Valid",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_ShippingCountryCode_Valid",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_ShippingDistrict_Valid",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_ShippingPhoneNumber_Valid",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_ShippingPostalCode_Valid",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_ShippingRecipientFullName_Valid",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingAddressLine1",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingAddressLine2",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingCity",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingCountryCode",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingDistrict",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingPhoneNumber",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingPostalCode",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingRecipientFullName",
                table: "Orders");
        }
    }
}
