using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LoanEngine.Api.Models.DTOs;

namespace LoanEngine.Api.Services.Interfaces
{
    public interface ILoanRecommendationService
    {
        Task<LoanRecommendationResponseDto> ProcessApplicationAsync(
            Stream fileStream,
            string fileName,
            string contentType,
            ProcessStatementRequestDto request,
            CancellationToken cancellationToken = default);

        Task<ExtractedStatementDto> ExtractStatementOnlyAsync(
            Stream fileStream,
            string fileName,
            string contentType,
            CancellationToken cancellationToken = default);

        Task<LoanRecommendationResponseDto?> GetApplicationByIdAsync(int id, CancellationToken cancellationToken = default);
    }
}
