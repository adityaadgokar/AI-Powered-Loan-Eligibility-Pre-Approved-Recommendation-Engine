using System;

namespace LoanEngine.Api.Models.Entities
{
    public class BankTransaction
    {
        public int Id { get; set; }
        public int BankStatementId { get; set; }
        public BankStatement BankStatement { get; set; } = null!;
        public DateTime TransactionDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal Balance { get; set; }
        public bool IsSalaryCredit { get; set; }
        public bool IsEmiDebit { get; set; }
    }
}
