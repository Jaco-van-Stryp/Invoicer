using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Invoicer.Migrations
{
    /// <inheritdoc />
    public partial class softDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Clients",
                type: "boolean",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.CreateIndex(
                name: "IX_Clients_CompanyId_IsDeleted",
                table: "Clients",
                columns: new[] { "CompanyId", "IsDeleted" }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_Clients_CompanyId_IsDeleted", table: "Clients");

            migrationBuilder.DropColumn(name: "IsDeleted", table: "Clients");
        }
    }
}
