using Microsoft.EntityFrameworkCore;

namespace Invoicer.Domain.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions options)
            : base(options) { }

        public DbSet<Infrastructure.Entities.Company> Companies { get; set; }
        public DbSet<Infrastructure.Entities.Client> Clients { get; set; }
        public DbSet<Infrastructure.Entities.Invoice> Invoices { get; set; }
        public DbSet<Infrastructure.Entities.Product> Products { get; set; }
        public DbSet<Infrastructure.Entities.ProductInvoice> ProductInvoices { get; set; }
        public DbSet<Infrastructure.Entities.AuthToken> AuthTokens { get; set; }
        public DbSet<Infrastructure.Entities.User> Users { get; set; }
    }
}
