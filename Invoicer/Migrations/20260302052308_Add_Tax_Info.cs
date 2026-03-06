using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Invoicer.Migrations
{
    /// <inheritdoc />
    public partial class Add_Tax_Info : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsTaxed",
                table: "ProductInvoices",
                type: "boolean",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.AddColumn<bool>(
                name: "IsTaxed",
                table: "ProductEstimates",
                type: "boolean",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.AddColumn<string>(
                name: "TaxName",
                table: "Invoices",
                type: "text",
                nullable: true
            );

            migrationBuilder.AddColumn<decimal>(
                name: "TaxRate",
                table: "Invoices",
                type: "numeric",
                nullable: false,
                defaultValue: 0m
            );

            migrationBuilder.AddColumn<string>(
                name: "TaxName",
                table: "Estimates",
                type: "text",
                nullable: true
            );

            migrationBuilder.AddColumn<decimal>(
                name: "TaxRate",
                table: "Estimates",
                type: "numeric",
                nullable: false,
                defaultValue: 0m
            );

            migrationBuilder.AddColumn<string>(
                name: "TaxName",
                table: "Companies",
                type: "text",
                nullable: true
            );

            migrationBuilder.AddColumn<decimal>(
                name: "TaxRate",
                table: "Companies",
                type: "numeric",
                nullable: false,
                defaultValue: 0m
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "IsTaxed", table: "ProductInvoices");

            migrationBuilder.DropColumn(name: "IsTaxed", table: "ProductEstimates");

            migrationBuilder.DropColumn(name: "TaxName", table: "Invoices");

            migrationBuilder.DropColumn(name: "TaxRate", table: "Invoices");

            migrationBuilder.DropColumn(name: "TaxName", table: "Estimates");

            migrationBuilder.DropColumn(name: "TaxRate", table: "Estimates");

            migrationBuilder.DropColumn(name: "TaxName", table: "Companies");

            migrationBuilder.DropColumn(name: "TaxRate", table: "Companies");
        }
    }
}
