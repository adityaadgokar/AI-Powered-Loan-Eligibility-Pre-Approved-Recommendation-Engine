using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using LoanEngine.Api.Models.DTOs;
using LoanEngine.Api.Services.Interfaces;

namespace LoanEngine.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Route("api/loan")]
    public class LoanProcessingController : ControllerBase
    {
        private readonly ILoanRecommendationService _loanRecommendationService;

        public LoanProcessingController(ILoanRecommendationService loanRecommendationService)
        {
            _loanRecommendationService = loanRecommendationService;
        }

        /// <summary>
        /// Process an uploaded bank statement and calculate loan affordability & pre-approved recommendations.
        /// </summary>
        [HttpPost("process")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<LoanRecommendationResponseDto>> ProcessStatement(
            [FromForm] StatementUploadRequest form)
        {
            try
            {
                if (form.File == null || form.File.Length == 0)
                {
                    return BadRequest(new { message = "A bank statement file (PDF or image) is required." });
                }

                if (!form.RequestedLoanAmount.HasValue || form.RequestedLoanAmount.Value <= 0)
                {
                    return BadRequest(new { message = "Requested Loan Amount is required and must be greater than 0." });
                }

                if (!form.TenureMonths.HasValue || form.TenureMonths.Value <= 0)
                {
                    return BadRequest(new { message = "Tenure in months is required and must be greater than 0." });
                }

                if (!form.AnnualInterestRate.HasValue || form.AnnualInterestRate.Value <= 0)
                {
                    return BadRequest(new { message = "Annual Interest Rate (% p.a.) is required and must be greater than 0." });
                }

                var cancellationToken = HttpContext.RequestAborted;

                var request = new ProcessStatementRequestDto
                {
                    CustomerName = form.CustomerName ?? string.Empty,
                    AccountNumber = form.AccountNumber,
                    CustomerEmail = form.CustomerEmail,
                    CustomerPhone = form.CustomerPhone,
                    RequestedLoanAmount = form.RequestedLoanAmount.Value,
                    TenureMonths = form.TenureMonths.Value,
                    AnnualInterestRate = form.AnnualInterestRate.Value
                };

                using var stream = form.File.OpenReadStream();
                var response = await _loanRecommendationService.ProcessApplicationAsync(
                    stream, form.File.FileName, form.File.ContentType, request, cancellationToken);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error processing statement: {ex.Message}" });
            }
        }

        /// <summary>
        /// Read and extract customer details (Customer Name, Account Number) directly from bank statement.
        /// </summary>
        [HttpPost("extract")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ExtractedStatementDto>> ExtractStatement([FromForm] StatementExtractRequest form)
        {
            try
            {
                if (form.File == null || form.File.Length == 0)
                {
                    return BadRequest(new { message = "A bank statement file (PDF or image) is required." });
                }

                var cancellationToken = HttpContext.RequestAborted;
                using var stream = form.File.OpenReadStream();
                var extracted = await _loanRecommendationService.ExtractStatementOnlyAsync(
                    stream, form.File.FileName, form.File.ContentType, cancellationToken);

                return Ok(extracted);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error extracting statement: {ex.Message}" });
            }
        }

        /// <summary>
        /// Retrieve previously processed loan application details.
        /// </summary>
        [HttpGet("history/{id:int}")]
        public async Task<ActionResult<LoanRecommendationResponseDto>> GetById(int id)
        {
            var cancellationToken = HttpContext.RequestAborted;
            var result = await _loanRecommendationService.GetApplicationByIdAsync(id, cancellationToken);
            if (result == null)
            {
                return NotFound(new { message = $"Loan application #{id} not found." });
            }
            return Ok(result);
        }
    }
}
