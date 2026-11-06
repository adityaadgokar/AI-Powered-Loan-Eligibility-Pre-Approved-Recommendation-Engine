using System;

namespace LoanEngine.Api.Models.Entities
{
    public class LoanEligibilityResult
    {
        public int Id { get; set; }
        public int LoanApplicationId { get; set; }
        public LoanApplication LoanApplication { get; set; } = null!;

        public decimal MonthlyIncome { get; set; }
        public decimal MonthlyExpenses { get; set; }
        public decimal ExistingEmis { get; set; }
        public decimal AverageBalance { get; set; }
        public decimal DisposableIncome { get; set; }

        public decimal RequestedEmi { get; set; }
        public decimal TotalEmiWithExisting { get; set; }
        public decimal MaxAffordableEmi { get; set; }
        public decimal MaxEligibleLoanAmount { get; set; }
        public decimal SuggestedPreApprovedAmount { get; set; }

        public bool IsRequestedLoanAffordable { get; set; }
        public decimal FoirPercentage { get; set; } // Fixed Obligation to Income Ratio
        public string RecommendationExplanation { get; set; } = string.Empty;
        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    }
}
