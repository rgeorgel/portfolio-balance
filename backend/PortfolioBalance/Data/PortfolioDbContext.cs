using Microsoft.EntityFrameworkCore;
using PortfolioBalance.Models;

namespace PortfolioBalance.Data;

public class PortfolioDbContext : DbContext
{
    public PortfolioDbContext(DbContextOptions<PortfolioDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<InvestmentType> InvestmentTypes { get; set; }
    public DbSet<Investment> Investments { get; set; }
    public DbSet<UserInvestmentTypeAllocation> UserInvestmentTypeAllocations { get; set; }
    public DbSet<InvestmentHistory> InvestmentHistories { get; set; }
    public DbSet<InvestmentTransaction> InvestmentTransactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        modelBuilder.Entity<InvestmentType>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<Investment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.CurrentValue).HasPrecision(18, 2);
            entity.Property(e => e.Weight).HasPrecision(10, 4);

            entity.HasOne(e => e.User)
                .WithMany(u => u.Investments)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.InvestmentType)
                .WithMany(t => t.Investments)
                .HasForeignKey(e => e.InvestmentTypeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserInvestmentTypeAllocation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AllocationPercentage).HasPrecision(5, 2);

            entity.HasOne(e => e.User)
                .WithMany(u => u.InvestmentTypeAllocations)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.InvestmentType)
                .WithMany(t => t.UserAllocations)
                .HasForeignKey(e => e.InvestmentTypeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique constraint to prevent duplicate allocations for the same user and investment type
            entity.HasIndex(e => new { e.UserId, e.InvestmentTypeId }).IsUnique();
        });

        modelBuilder.Entity<InvestmentHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Value).HasPrecision(18, 2);
            entity.Property(e => e.UnitValue).HasPrecision(18, 2);
            entity.Property(e => e.Quantity).HasPrecision(18, 8);
            entity.Property(e => e.Notes).HasMaxLength(500);

            entity.HasOne(e => e.Investment)
                .WithMany(i => i.InvestmentHistories)
                .HasForeignKey(e => e.InvestmentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.InvestmentId);
            entity.HasIndex(e => e.RecordedDate);
        });

        modelBuilder.Entity<InvestmentTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.UnitValue).HasPrecision(18, 2);
            entity.Property(e => e.Quantity).HasPrecision(18, 8);
            entity.Property(e => e.Notes).HasMaxLength(500);

            entity.HasOne(e => e.Investment)
                .WithMany(i => i.InvestmentTransactions)
                .HasForeignKey(e => e.InvestmentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.InvestmentId);
            entity.HasIndex(e => e.TransactionDate);
        });

        // Seed initial investment types (these are shared across all users)
        modelBuilder.Entity<InvestmentType>().HasData(
            new InvestmentType { Id = 1, Name = "Ações Nacionais" },
            new InvestmentType { Id = 2, Name = "Fundos Imobiliários" },
            new InvestmentType { Id = 3, Name = "Criptomoedas" },
            new InvestmentType { Id = 4, Name = "Renda Fixa" },
            new InvestmentType { Id = 5, Name = "Ações Internacionais" }
        );
    }
}
