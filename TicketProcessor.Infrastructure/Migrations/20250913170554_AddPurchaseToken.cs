using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketProcessor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PurchaseToken",
                table: "reservations",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "reservations",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-999999999999"),
                column: "PurchaseToken",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PurchaseToken",
                table: "reservations");
        }
    }
}
