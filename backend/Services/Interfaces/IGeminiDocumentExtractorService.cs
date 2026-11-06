using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LoanEngine.Api.Models.DTOs;

namespace LoanEngine.Api.Services.Interfaces
{
    public interface IGeminiDocumentExtractorService
    {
        Task<ExtractedStatementDto> ExtractStatementDataAsync(Stream documentStream, string fileName, string contentType, CancellationToken cancellationToken = default);
    }
}
