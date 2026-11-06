namespace LoanEngine.Api.Models.DTOs
{
    public class ProcessStatementRequestDto
    {
        public string CustomerName { get; set; } = string.Empty;
        public string? AccountNumber { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }
        public decimal RequestedLoanAmount { get; set; }
        public int TenureMonths { get; set; }
        public decimal AnnualInterestRate { get; set; } = 12.0m;
    }
}
