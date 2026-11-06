namespace LoanEngine.Api.Models.DTOs
{
    public class LoanAffordabilityResultDto
    {
        public decimal RequestedAmount { get; set; }
        public int TenureMonths { get; set; }
        public decimal AnnualInterestRate { get; set; }
        public decimal MonthlyInterestRate { get; set; }

        public decimal RequestedEmi { get; set; }
        public decimal ExistingEmis { get; set; }
        public decimal TotalEmiWithExisting { get; set; }
        public decimal DisposableIncome { get; set; }
        public decimal RemainingMonthlyIncomeAfterAllEmis { get; set; }

        public decimal PolicyMaxFoirPercentage { get; set; } = 40.0m;
        public decimal MaxAllowedTotalEmi { get; set; }
        public decimal AvailableEmiCapacity { get; set; }
        public decimal MaxEligibleLoanAmount { get; set; }
        public decimal SuggestedPreApprovedAmount { get; set; }

        public bool IsRequestedLoanAffordable { get; set; }
        public decimal ActualFoirPercentage { get; set; }
        public string RecommendationExplanation { get; set; } = string.Empty;
        public string EligibilityStatus { get; set; } = "Eligible";
    }
}
