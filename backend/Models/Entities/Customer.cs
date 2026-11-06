using System;
using System.Collections.Generic;

namespace LoanEngine.Api.Models.Entities
{
    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<BankStatement> BankStatements { get; set; } = new List<BankStatement>();
        public ICollection<LoanApplication> LoanApplications { get; set; } = new List<LoanApplication>();
    }
}
