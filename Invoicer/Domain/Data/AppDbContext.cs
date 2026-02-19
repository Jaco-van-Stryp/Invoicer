using Invoicer.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Invoicer.Domain.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<Company> Companies { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<Estimate> Estimates { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductInvoice> ProductInvoices { get; set; }
        public DbSet<ProductEstimate> ProductEstimates { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<AuthToken> AuthTokens { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<WaitingList> WaitingList { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();

            modelBuilder.Entity<User>().Property(u => u.RowVersion).IsRowVersion();

            modelBuilder
                .Entity<ProductInvoice>()
                .HasIndex(pi => new { pi.ProductId, pi.InvoiceId })
                .IsUnique();

            modelBuilder
                .Entity<ProductEstimate>()
                .HasIndex(pe => new { pe.ProductId, pe.EstimateId })
                .IsUnique();

            modelBuilder
                .Entity<Payment>()
                .HasOne(p => p.Invoice)
                .WithMany(i => i.Payments)
                .HasForeignKey(p => p.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Payment>().HasIndex(p => p.InvoiceId);

            modelBuilder.Entity<Payment>().HasIndex(p => new { p.CompanyId, p.PaidOn });
        }
    }
}
