using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AN.Ticket.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class AlterTable_TicketMessage_AssociationAttachment_In_Message : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TicketMessageId",
                table: "Attachments",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_TicketMessageId",
                table: "Attachments",
                column: "TicketMessageId");

            migrationBuilder.AddForeignKey(
                name: "FK_Attachments_TicketMessage_TicketMessageId",
                table: "Attachments",
                column: "TicketMessageId",
                principalTable: "TicketMessage",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attachments_TicketMessage_TicketMessageId",
                table: "Attachments");

            migrationBuilder.DropIndex(
                name: "IX_Attachments_TicketMessageId",
                table: "Attachments");

            migrationBuilder.DropColumn(
                name: "TicketMessageId",
                table: "Attachments");
        }
    }
}
