namespace LoanEngine.Api.Models.DTOs
{
    public class LoanRecommendationResponseDto
    {
        public int ApplicationId { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string? BankName { get; set; }
        public ExtractedStatementDto ExtractedStatement { get; set; } = new();
        public FinancialAnalysisDto FinancialAnalysis { get; set; } = new();
        public LoanAffordabilityResultDto AffordabilityResult { get; set; } = new();
    }
}
