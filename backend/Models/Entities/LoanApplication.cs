using System;

namespace LoanEngine.Api.Models.Entities
{
    public class LoanApplication
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;
        public int BankStatementId { get; set; }
        public BankStatement BankStatement { get; set; } = null!;
        public decimal RequestedAmount { get; set; }
        public int TenureMonths { get; set; }
        public decimal AnnualInterestRate { get; set; }
        public string Status { get; set; } = "Processed";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public LoanEligibilityResult? EligibilityResult { get; set; }
    }
}
