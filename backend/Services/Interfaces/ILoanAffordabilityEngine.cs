using LoanEngine.Api.Models.DTOs;

namespace LoanEngine.Api.Services.Interfaces
{
    public interface ILoanAffordabilityEngine
    {
        decimal CalculateEmi(decimal principal, decimal annualInterestRate, int tenureMonths);
        decimal CalculateMaxPrincipalFromEmi(decimal maxMonthlyEmi, decimal annualInterestRate, int tenureMonths);
        LoanAffordabilityResultDto EvaluateAffordability(
            decimal requestedAmount,
            int tenureMonths,
            decimal annualInterestRate,
            FinancialAnalysisDto financialAnalysis,
            decimal maxFoirPercentage = 40.0m);
    }
}
