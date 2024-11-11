using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AN.Ticket.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class AlterColumn_Per_SupportFile_Save_Asset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FilePath",
                table: "AssetFiles");

            migrationBuilder.AddColumn<byte[]>(
                name: "FileContent",
                table: "AssetFiles",
                type: "longblob",
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileContent",
                table: "AssetFiles");

            migrationBuilder.AddColumn<string>(
                name: "FilePath",
                table: "AssetFiles",
                type: "varchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
