using Microsoft.AspNetCore.Http;

namespace LoanEngine.Api.Models.DTOs
{
    public class StatementExtractRequest
    {
        public IFormFile? File { get; set; }
    }

    public class StatementUploadRequest
    {
        public IFormFile? File { get; set; }
        public string? CustomerName { get; set; }
        public string? AccountNumber { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }
        public decimal? RequestedLoanAmount { get; set; }
        public int? TenureMonths { get; set; }
        public decimal? AnnualInterestRate { get; set; }
    }
}
