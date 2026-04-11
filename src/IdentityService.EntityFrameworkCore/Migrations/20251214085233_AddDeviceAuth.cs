using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityService.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeviceAuths",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OtpSecret = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    AuthToken = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    AuthTokenExpiry = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RefreshTokenHash = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    DeviceName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsTrusted = table.Column<bool>(type: "bit", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceAuths", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceAuths_AuthToken",
                table: "DeviceAuths",
                column: "AuthToken");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceAuths_DeviceId",
                table: "DeviceAuths",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceAuths_DeviceId_UserId",
                table: "DeviceAuths",
                columns: new[] { "DeviceId", "UserId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeviceAuths");
        }
    }
}
