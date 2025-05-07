using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConnectThruEventsBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Admins",
                keyColumn: "Id",
                keyValue: new Guid("d4e8fa5e-4c1b-4a8b-9c1a-5a53dfb6e688"),
                column: "PasswordHash",
                value: "$2a$11$YZ/H/4v/iqxp3oAlgaQCiOAjXZLVzUIqPXy1atHMc4VlDNHCrPq5");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Admins",
                keyColumn: "Id",
                keyValue: new Guid("d4e8fa5e-4c1b-4a8b-9c1a-5a53dfb6e688"),
                column: "PasswordHash",
                value: "hashedpassword");
        }
    }
}
