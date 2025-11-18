using Microsoft.EntityFrameworkCore;
using PortfolioBalance.Models;

namespace PortfolioBalance.Data;

public class PortfolioDbContext : DbContext
{
    public PortfolioDbContext(DbContextOptions<PortfolioDbContext> options) : base(options)
    {
    }

    public DbSet<InvestmentType> InvestmentTypes { get; set; }
    public DbSet<Investment> Investments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<InvestmentType>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.AllocationPercentage).HasPrecision(5, 2);
        });

        modelBuilder.Entity<Investment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.CurrentValue).HasPrecision(18, 2);
            entity.Property(e => e.Weight).HasPrecision(10, 4);

            entity.HasOne(e => e.InvestmentType)
                .WithMany(t => t.Investments)
                .HasForeignKey(e => e.InvestmentTypeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Seed initial investment types
        modelBuilder.Entity<InvestmentType>().HasData(
            new InvestmentType { Id = 1, Name = "Ações Nacionais", AllocationPercentage = 20.0m },
            new InvestmentType { Id = 2, Name = "Fundos Imobiliários", AllocationPercentage = 20.0m },
            new InvestmentType { Id = 3, Name = "Criptomoedas", AllocationPercentage = 20.0m },
            new InvestmentType { Id = 4, Name = "Renda Fixa", AllocationPercentage = 20.0m },
            new InvestmentType { Id = 5, Name = "Ações Internacionais", AllocationPercentage = 20.0m }
        );
    }
}
