using Microsoft.EntityFrameworkCore;
using LoanEngine.Api.Models.Entities;

namespace LoanEngine.Api.Models.Data
{
    public class LoanDbContext : DbContext
    {
        public LoanDbContext(DbContextOptions<LoanDbContext> options) : base(options)
        {
        }

        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<BankStatement> BankStatements => Set<BankStatement>();
        public DbSet<BankTransaction> BankTransactions => Set<BankTransaction>();
        public DbSet<LoanApplication> LoanApplications => Set<LoanApplication>();
        public DbSet<LoanEligibilityResult> LoanEligibilityResults => Set<LoanEligibilityResult>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Customer
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Name).IsRequired().HasMaxLength(150);
                entity.Property(c => c.Email).HasMaxLength(150);
                entity.Property(c => c.Phone).HasMaxLength(30);
            });

            // BankStatement
            modelBuilder.Entity<BankStatement>(entity =>
            {
                entity.HasKey(s => s.Id);
                entity.Property(s => s.FileName).IsRequired().HasMaxLength(255);
                entity.Property(s => s.AccountNumber).HasMaxLength(50);
                entity.Property(s => s.BankName).HasMaxLength(100);
                entity.Property(s => s.OpeningBalance).HasPrecision(18, 2);
                entity.Property(s => s.ClosingBalance).HasPrecision(18, 2);

                entity.HasOne(s => s.Customer)
                      .WithMany(c => c.BankStatements)
                      .HasForeignKey(s => s.CustomerId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // BankTransaction
            modelBuilder.Entity<BankTransaction>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Description).IsRequired().HasMaxLength(300);
                entity.Property(t => t.Debit).HasPrecision(18, 2);
                entity.Property(t => t.Credit).HasPrecision(18, 2);
                entity.Property(t => t.Balance).HasPrecision(18, 2);

                entity.HasOne(t => t.BankStatement)
                      .WithMany(s => s.Transactions)
                      .HasForeignKey(t => t.BankStatementId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // LoanApplication
            modelBuilder.Entity<LoanApplication>(entity =>
            {
                entity.HasKey(a => a.Id);
                entity.Property(a => a.RequestedAmount).HasPrecision(18, 2);
                entity.Property(a => a.AnnualInterestRate).HasPrecision(5, 2);
                entity.Property(a => a.Status).HasMaxLength(50);

                entity.HasOne(a => a.Customer)
                      .WithMany(c => c.LoanApplications)
                      .HasForeignKey(a => a.CustomerId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.BankStatement)
                      .WithMany()
                      .HasForeignKey(a => a.BankStatementId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // LoanEligibilityResult
            modelBuilder.Entity<LoanEligibilityResult>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.Property(r => r.MonthlyIncome).HasPrecision(18, 2);
                entity.Property(r => r.MonthlyExpenses).HasPrecision(18, 2);
                entity.Property(r => r.ExistingEmis).HasPrecision(18, 2);
                entity.Property(r => r.AverageBalance).HasPrecision(18, 2);
                entity.Property(r => r.DisposableIncome).HasPrecision(18, 2);
                entity.Property(r => r.RequestedEmi).HasPrecision(18, 2);
                entity.Property(r => r.TotalEmiWithExisting).HasPrecision(18, 2);
                entity.Property(r => r.MaxAffordableEmi).HasPrecision(18, 2);
                entity.Property(r => r.MaxEligibleLoanAmount).HasPrecision(18, 2);
                entity.Property(r => r.SuggestedPreApprovedAmount).HasPrecision(18, 2);
                entity.Property(r => r.FoirPercentage).HasPrecision(5, 2);
                entity.Property(r => r.RecommendationExplanation).HasMaxLength(1500);

                entity.HasOne(r => r.LoanApplication)
                      .WithOne(a => a.EligibilityResult)
                      .HasForeignKey<LoanEligibilityResult>(r => r.LoanApplicationId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
