using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeVault.Migrations
{
    /// <inheritdoc />
    public partial class FixStaticDateTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedDate", "LastModifiedDate" },
                values: new object[] { new DateTime(2025, 10, 15, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 10, 15, 0, 0, 0, 0, DateTimeKind.Utc) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedDate", "LastModifiedDate" },
                values: new object[] { new DateTime(2025, 10, 15, 12, 32, 21, 204, DateTimeKind.Utc).AddTicks(8473), new DateTime(2025, 10, 15, 12, 32, 21, 204, DateTimeKind.Utc).AddTicks(8561) });
        }
    }
}
