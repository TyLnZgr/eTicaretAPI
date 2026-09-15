using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInboxAndCustomerNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CustomerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrderId = table.Column<int>(type: "INTEGER", nullable: true),
                    SourceMessageId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    IsRead = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReadAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerNotifications", x => x.Id);
                    table.CheckConstraint("CK_CustomerNotifications_IsRead_Valid", "\"IsRead\" IN (0, 1)");
                    table.CheckConstraint("CK_CustomerNotifications_Message_Valid", "length(trim(\"Message\")) BETWEEN 1 AND 1000");
                    table.CheckConstraint("CK_CustomerNotifications_ReadState_Consistent", "(\"IsRead\" = 0 AND \"ReadAtUtc\" IS NULL) OR (\"IsRead\" = 1 AND \"ReadAtUtc\" IS NOT NULL)");
                    table.CheckConstraint("CK_CustomerNotifications_Title_Valid", "length(trim(\"Title\")) BETWEEN 1 AND 200");
                    table.CheckConstraint("CK_CustomerNotifications_Type_Valid", "\"Type\" IN (1)");
                    table.ForeignKey(
                        name: "FK_CustomerNotifications_AspNetUsers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerNotifications_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "InboxMessages",
                columns: table => new
                {
                    MessageId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ConsumerName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboxMessages", x => new { x.MessageId, x.ConsumerName });
                    table.CheckConstraint("CK_InboxMessages_ConsumerName_Valid", "length(trim(\"ConsumerName\")) BETWEEN 1 AND 200");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerNotifications_CustomerId_IsRead_CreatedAtUtc",
                table: "CustomerNotifications",
                columns: new[] { "CustomerId", "IsRead", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerNotifications_OrderId",
                table: "CustomerNotifications",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerNotifications_SourceMessageId",
                table: "CustomerNotifications",
                column: "SourceMessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InboxMessages_ProcessedAtUtc",
                table: "InboxMessages",
                column: "ProcessedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerNotifications");

            migrationBuilder.DropTable(
                name: "InboxMessages");
        }
    }
}
