using System.Collections.Generic;
using LoanEngine.Api.Models.DTOs;

namespace LoanEngine.Api.Services.Interfaces
{
    public interface IStatementValidationService
    {
        StatementValidationResult Validate(ExtractedStatementDto statement);
    }

    public class StatementValidationResult
    {
        public bool IsValid { get; set; } = true;
        public List<string> Warnings { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public decimal TotalCredits { get; set; }
        public decimal TotalDebits { get; set; }
        public bool BalanceMathMatches { get; set; } = true;
    }
}
