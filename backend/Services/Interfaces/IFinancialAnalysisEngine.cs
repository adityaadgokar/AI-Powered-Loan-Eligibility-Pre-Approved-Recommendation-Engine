using LoanEngine.Api.Models.DTOs;

namespace LoanEngine.Api.Services.Interfaces
{
    public interface IFinancialAnalysisEngine
    {
        FinancialAnalysisDto Analyze(ExtractedStatementDto statement);
    }
}
