using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VereinMember.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPrivacyConsent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PrivacyConsentIp",
                schema: "Membership",
                table: "Member",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PrivacyConsentedAt",
                schema: "Membership",
                table: "Member",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrivacyConsentIp",
                schema: "Membership",
                table: "Member");

            migrationBuilder.DropColumn(
                name: "PrivacyConsentedAt",
                schema: "Membership",
                table: "Member");
        }
    }
}
