using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerAddresses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerAddresses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CustomerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Label = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    RecipientFullName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    PhoneNumber = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    AddressLine1 = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    AddressLine2 = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    District = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    City = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PostalCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CountryCode = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 2, nullable: false),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerAddresses", x => x.Id);
                    table.CheckConstraint("CK_CustomerAddresses_AddressLine1_Valid", "length(trim(\"AddressLine1\")) BETWEEN 5 AND 300");
                    table.CheckConstraint("CK_CustomerAddresses_AddressLine2_Valid", "\"AddressLine2\" IS NULL OR length(trim(\"AddressLine2\")) BETWEEN 1 AND 300");
                    table.CheckConstraint("CK_CustomerAddresses_City_Valid", "length(trim(\"City\")) BETWEEN 1 AND 100");
                    table.CheckConstraint("CK_CustomerAddresses_CountryCode_Valid", "length(\"CountryCode\") = 2 AND \"CountryCode\" = upper(\"CountryCode\")");
                    table.CheckConstraint("CK_CustomerAddresses_District_Valid", "length(trim(\"District\")) BETWEEN 1 AND 100");
                    table.CheckConstraint("CK_CustomerAddresses_IsDefault_Valid", "\"IsDefault\" IN (0, 1)");
                    table.CheckConstraint("CK_CustomerAddresses_Label_Valid", "length(trim(\"Label\")) BETWEEN 1 AND 100");
                    table.CheckConstraint("CK_CustomerAddresses_PhoneNumber_Valid", "length(trim(\"PhoneNumber\")) BETWEEN 3 AND 30");
                    table.CheckConstraint("CK_CustomerAddresses_PostalCode_Valid", "length(trim(\"PostalCode\")) BETWEEN 1 AND 20");
                    table.CheckConstraint("CK_CustomerAddresses_RecipientFullName_Valid", "length(trim(\"RecipientFullName\")) BETWEEN 2 AND 200");
                    table.ForeignKey(
                        name: "FK_CustomerAddresses_AspNetUsers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAddresses_CustomerId_UpdatedAtUtc",
                table: "CustomerAddresses",
                columns: new[] { "CustomerId", "UpdatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_CustomerAddresses_CustomerId_Default",
                table: "CustomerAddresses",
                column: "CustomerId",
                unique: true,
                filter: "\"IsDefault\" = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerAddresses");
        }
    }
}
