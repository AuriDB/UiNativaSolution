using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WEB_UI.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordReset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetExpira",
                table: "Sujetos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetHash",
                table: "Sujetos",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordResetExpira",
                table: "Sujetos");

            migrationBuilder.DropColumn(
                name: "PasswordResetHash",
                table: "Sujetos");
        }
    }
}
