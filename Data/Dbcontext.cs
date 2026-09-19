using crud.Models;
using CRUD.Models;
using Microsoft.EntityFrameworkCore;

namespace CRUD.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Item> Items { get; set; }

        public DbSet<Customer> Customers { get; set; }

        public DbSet<Borrow> Borrows { get; set; }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<BorrowItem> BorrowItems { get; set; }
        public DbSet<UserModel> Users { get; set; }
        public DbSet<Penalty> Penalties { get; set; }
        public DbSet<Reservation> Reservation { get; set; }
        public DbSet<ReservationItems> ReservationItems{ get; set; }

        protected override void OnModelCreating(
           ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Penalty>()
                .HasOne(p => p.Borrow)
                .WithOne()
                .HasForeignKey<Penalty>(p => p.BorrowID);

            // BorrowItem -> Borrow
            modelBuilder.Entity<BorrowItem>()
                .HasOne(bi => bi.Borrow)
                .WithMany(b => b.BorrowItems)
                .HasForeignKey(bi => bi.BorrowID)
                .OnDelete(DeleteBehavior.NoAction);

            // BorrowItem -> Item
            modelBuilder.Entity<BorrowItem>()
                .HasOne(bi => bi.Item)
                .WithMany()
                .HasForeignKey(bi => bi.ItemID)
                .OnDelete(DeleteBehavior.NoAction);

            // Borrow -> Item
            modelBuilder.Entity<Borrow>()
                .HasOne(b => b.Item)
                .WithMany()
                .HasForeignKey(b => b.ItemID)
                .OnDelete(DeleteBehavior.NoAction);

            // Borrow -> Customer
            modelBuilder.Entity<Borrow>()
                .HasOne(b => b.Customer)
                .WithMany(c => c.Borrows)
                .HasForeignKey(b => b.CustomerID)
                .OnDelete(DeleteBehavior.NoAction);

            // Decimal precision
            modelBuilder.Entity<Item>()
                .Property(i => i.Amount)
                .HasPrecision(18, 2);

            // Item code (e.g. "TUX001")
            modelBuilder.Entity<Item>()
                .Property(i => i.ItemCode)
                .HasMaxLength(20);

            modelBuilder.Entity<Item>()
                .HasIndex(i => i.ItemCode)
                .IsUnique();

            modelBuilder.Entity<Payment>()
                .Property(p => p.AmountPaid)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Penalty>()
                .Property(p => p.PenaltyPerDay)
                   .HasPrecision(18, 2);

            modelBuilder.Entity<Penalty>()
                .Property(p => p.PenaltyAmount)
                .HasPrecision(18, 2);

        }
    }
}
