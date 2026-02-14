using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Invoicer.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueProductInvoiceIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductInvoices_ProductId",
                table: "ProductInvoices");

            migrationBuilder.CreateIndex(
                name: "IX_ProductInvoices_ProductId_InvoiceId",
                table: "ProductInvoices",
                columns: new[] { "ProductId", "InvoiceId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductInvoices_ProductId_InvoiceId",
                table: "ProductInvoices");

            migrationBuilder.CreateIndex(
                name: "IX_ProductInvoices_ProductId",
                table: "ProductInvoices",
                column: "ProductId");
        }
    }
}
