using System;
using System.Collections.Generic;

namespace LoanEngine.Api.Models.Entities
{
    public class BankStatement
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;
        public string FileName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string? BankName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal ClosingBalance { get; set; }
        public DateTime UploadDate { get; set; } = DateTime.UtcNow;
        public string? OcrRawJson { get; set; }

        public ICollection<BankTransaction> Transactions { get; set; } = new List<BankTransaction>();
    }
}
