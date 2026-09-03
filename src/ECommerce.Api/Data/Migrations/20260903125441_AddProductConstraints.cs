using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProductConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_Products_IsActive_Valid",
                table: "Products",
                sql: "\"IsActive\" IN (0, 1)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Products_Name_Valid",
                table: "Products",
                sql: "length(trim(\"Name\")) BETWEEN 1 AND 200");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Products_Price_Positive",
                table: "Products",
                sql: "CAST(\"Price\" AS NUMERIC) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Products_StockQuantity_NonNegative",
                table: "Products",
                sql: "\"StockQuantity\" >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Products_IsActive_Valid",
                table: "Products");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Products_Name_Valid",
                table: "Products");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Products_Price_Positive",
                table: "Products");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Products_StockQuantity_NonNegative",
                table: "Products");
        }
    }
}
