using Microsoft.EntityFrameworkCore;
using OutBox_Project.Models;

namespace OutBox_Project
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<TransactionLog> TransactionLogs { get; set; }
        public DbSet<OutboxMessage> OutboxMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 👤 User <-> Wallet (1:1)
            modelBuilder.Entity<User>()
                .HasOne(u => u.Wallet)
                .WithOne(w => w.User)
                .HasForeignKey<Wallet>(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // 💰 Wallet <-> Transactions (1:N)
            modelBuilder.Entity<Wallet>()
                .HasMany(w => w.Transactions)
                .WithOne(t => t.Wallet)
                .HasForeignKey(t => t.WalletId)
                .OnDelete(DeleteBehavior.Cascade);

            // 🔁 Transaction <-> TransactionLog (1:1)
            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Log)
                .WithOne(l => l.Transaction)
                .HasForeignKey<TransactionLog>(l => l.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);

            // 🕒 Index for ordering transactions per wallet
            modelBuilder.Entity<Transaction>()
                .HasIndex(t => new { t.WalletId, t.CreatedAt });

            // 📦 OutboxMessage — no foreign keys, no navigation
            modelBuilder.Entity<OutboxMessage>(entity =>
            {
                entity.ToTable("OutboxMessages");
                entity.HasKey(o => o.Id);
                entity.Property(o => o.AggregateType)
                      .HasMaxLength(100);
                entity.Property(o => o.Type)
                      .HasMaxLength(50);
                entity.Property(o => o.Status)
                      .HasConversion<int>(); // store enum as int
                entity.HasIndex(o => new { o.AggregateId, o.Sequence }); // for ordering & deduplication
            });
        }
    }
}
