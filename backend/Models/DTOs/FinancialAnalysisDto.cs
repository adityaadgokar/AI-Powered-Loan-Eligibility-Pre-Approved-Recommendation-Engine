using System.Collections.Generic;

namespace LoanEngine.Api.Models.DTOs
{
    public class FinancialAnalysisDto
    {
        public decimal MonthlyVerifiedIncome { get; set; }
        public decimal MonthlyExpenses { get; set; }
        public decimal ExistingEmis { get; set; }
        public decimal AverageBalance { get; set; }
        public decimal LowestBalance { get; set; }
        public decimal MonthlyDisposableIncome { get; set; }
        public decimal NetCashFlow { get; set; }
        public decimal ExpenseRatio { get; set; }
        public decimal ExistingEmiBurdenRatio { get; set; }
        public int AnalyzedMonthsCount { get; set; }
        public List<string> DetectedSalaryEntries { get; set; } = new();
        public List<string> DetectedEmiEntries { get; set; } = new();
    }
}
