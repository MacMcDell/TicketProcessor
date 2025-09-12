using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TicketProcessor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "venues",
                columns: new[] { "Id", "Capacity", "Created", "CreatedBy", "IsDeleted", "LastModified", "LastModifiedBy", "Name" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), 900, new DateTimeOffset(new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, false, new DateTimeOffset(new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Commodore Ballroom" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), 2765, new DateTimeOffset(new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, false, new DateTimeOffset(new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Queen Elizabeth Theatre" }
                });

            migrationBuilder.InsertData(
                table: "events",
                columns: new[] { "Id", "Created", "CreatedBy", "Description", "IsDeleted", "LastModified", "LastModifiedBy", "StartsAt", "Title", "VenueId" },
                values: new object[,]
                {
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), new DateTimeOffset(new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Tour kickoff", false, new DateTimeOffset(new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new DateTimeOffset(new DateTime(2025, 10, 1, 20, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "The Alpines", new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), new DateTimeOffset(new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, false, new DateTimeOffset(new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new DateTimeOffset(new DateTime(2025, 10, 8, 20, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "DJ Night", new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), new DateTimeOffset(new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, false, new DateTimeOffset(new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new DateTimeOffset(new DateTime(2025, 10, 15, 20, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Symphonic Rock", new Guid("22222222-2222-2222-2222-222222222222") }
                });

            migrationBuilder.InsertData(
                table: "event_ticket_types",
                columns: new[] { "Id", "Capacity", "Created", "CreatedBy", "EventId", "IsDeleted", "LastModified", "LastModifiedBy", "Name", "Price", "Sold" },
                values: new object[,]
                {
                    { new Guid("d1111111-1111-1111-1111-111111111111"), 800, new DateTimeOffset(new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), false, new DateTimeOffset(new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "GA", 49.99m, 0 },
                    { new Guid("d2222222-2222-2222-2222-222222222222"), 100, new DateTimeOffset(new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), false, new DateTimeOffset(new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "VIP", 129.00m, 0 },
                    { new Guid("d3333333-3333-3333-3333-333333333333"), 850, new DateTimeOffset(new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), false, new DateTimeOffset(new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "GA", 39.00m, 0 },
                    { new Guid("d4444444-4444-4444-4444-444444444444"), 50, new DateTimeOffset(new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), false, new DateTimeOffset(new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "VIP", 95.00m, 0 },
                    { new Guid("d5555555-5555-5555-5555-555555555555"), 2200, new DateTimeOffset(new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), false, new DateTimeOffset(new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "GA", 59.00m, 0 },
                    { new Guid("d6666666-6666-6666-6666-666666666666"), 300, new DateTimeOffset(new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), false, new DateTimeOffset(new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "VIP", 149.00m, 0 },
                    { new Guid("d7777777-7777-7777-7777-777777777777"), 265, new DateTimeOffset(new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), false, new DateTimeOffset(new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Balcony", 79.00m, 0 }
                });

            migrationBuilder.InsertData(
                table: "reservations",
                columns: new[] { "Id", "Created", "CreatedBy", "EventTicketTypeId", "ExpiresAt", "IdempotencyKey", "IsDeleted", "LastModified", "LastModifiedBy", "Quantity", "Status" },
                values: new object[] { new Guid("99999999-9999-9999-9999-999999999999"), new DateTimeOffset(new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("d1111111-1111-1111-1111-111111111111"), new DateTimeOffset(new DateTime(2025, 10, 1, 21, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "seed-res-1", false, new DateTimeOffset(new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 3, 0 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "event_ticket_types",
                keyColumn: "Id",
                keyValue: new Guid("d2222222-2222-2222-2222-222222222222"));

            migrationBuilder.DeleteData(
                table: "event_ticket_types",
                keyColumn: "Id",
                keyValue: new Guid("d3333333-3333-3333-3333-333333333333"));

            migrationBuilder.DeleteData(
                table: "event_ticket_types",
                keyColumn: "Id",
                keyValue: new Guid("d4444444-4444-4444-4444-444444444444"));

            migrationBuilder.DeleteData(
                table: "event_ticket_types",
                keyColumn: "Id",
                keyValue: new Guid("d5555555-5555-5555-5555-555555555555"));

            migrationBuilder.DeleteData(
                table: "event_ticket_types",
                keyColumn: "Id",
                keyValue: new Guid("d6666666-6666-6666-6666-666666666666"));

            migrationBuilder.DeleteData(
                table: "event_ticket_types",
                keyColumn: "Id",
                keyValue: new Guid("d7777777-7777-7777-7777-777777777777"));

            migrationBuilder.DeleteData(
                table: "reservations",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-999999999999"));

            migrationBuilder.DeleteData(
                table: "event_ticket_types",
                keyColumn: "Id",
                keyValue: new Guid("d1111111-1111-1111-1111-111111111111"));

            migrationBuilder.DeleteData(
                table: "events",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

            migrationBuilder.DeleteData(
                table: "events",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"));

            migrationBuilder.DeleteData(
                table: "events",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

            migrationBuilder.DeleteData(
                table: "venues",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"));

            migrationBuilder.DeleteData(
                table: "venues",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"));
        }
    }
}
