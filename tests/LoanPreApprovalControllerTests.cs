using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using LoanEngine.Api.Controllers;
using LoanEngine.Api.Models.DTOs;
using LoanEngine.Api.Services.Interfaces;

namespace LoanEngine.Tests
{
    public class LoanPreApprovalControllerTests
    {
        private readonly FakeLoanRecommendationService _fakeService;
        private readonly LoanPreApprovalController _controller;

        public LoanPreApprovalControllerTests()
        {
            _fakeService = new FakeLoanRecommendationService();
            _controller = new LoanPreApprovalController(
                _fakeService,
                NullLogger<LoanPreApprovalController>.Instance);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        private static IFormFile CreateDummyFile(string filename = "statement.pdf")
        {
            var bytes = Encoding.UTF8.GetBytes("dummy content");
            var stream = new MemoryStream(bytes);
            return new FormFile(stream, 0, bytes.Length, "file", filename)
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };
        }

        [Fact]
        public async Task AssessLoan_ShouldReturnBadRequest_WhenFileIsMissing()
        {
            var request = new PreApprovalAssessmentRequest
            {
                RequestedLoan = 500000m,
                Tenure = 36,
                InterestRate = 10.5m
            };

            var result = await _controller.AssessLoan(request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequest.Value);
        }

        [Fact]
        public async Task AssessLoan_ShouldReturnBadRequest_WhenRequestedLoanIsMissingOrZero()
        {
            var request = new PreApprovalAssessmentRequest
            {
                File = CreateDummyFile(),
                RequestedLoan = 0m,
                Tenure = 36,
                InterestRate = 10.5m
            };

            var result = await _controller.AssessLoan(request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequest.Value);
        }

        [Fact]
        public async Task AssessLoan_ShouldReturnBadRequest_WhenTenureIsMissingOrZero()
        {
            var request = new PreApprovalAssessmentRequest
            {
                File = CreateDummyFile(),
                RequestedLoan = 500000m,
                Tenure = 0,
                InterestRate = 10.5m
            };

            var result = await _controller.AssessLoan(request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequest.Value);
        }

        [Fact]
        public async Task AssessLoan_ShouldReturnBadRequest_WhenInterestRateIsMissingOrZero()
        {
            var request = new PreApprovalAssessmentRequest
            {
                File = CreateDummyFile(),
                RequestedLoan = 500000m,
                Tenure = 36,
                InterestRate = 0m
            };

            var result = await _controller.AssessLoan(request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequest.Value);
        }

        [Fact]
        public async Task AssessLoan_ShouldReturnApproved_WhenRequestedLoanIsAffordable()
        {
            _fakeService.NextResponse = new LoanRecommendationResponseDto
            {
                CustomerName = "Rajesh Verma",
                AccountNumber = "987654321098",
                ExtractedStatement = new ExtractedStatementDto
                {
                    ClosingBalance = 80000m,
                    Transactions = new System.Collections.Generic.List<ExtractedTransactionDto>
                    {
                        new() { Credit = 150000m, Debit = 0m, Balance = 150000m, Description = "SALARY CREDIT - ACME CORP" },
                        new() { Credit = 0m, Debit = 25000m, Balance = 125000m, Description = "RENT PAYMENT TO LANDLORD" },
                        new() { Credit = 0m, Debit = 15000m, Balance = 110000m, Description = "GROCERIES AT DMART" },
                        new() { Credit = 0m, Debit = 10000m, Balance = 100000m, Description = "PERSONAL LOAN EMI" },
                        new() { Credit = 0m, Debit = 5000m, Balance = 95000m, Description = "ELECTRICITY UTILITY BILL" },
                        new() { Credit = 0m, Debit = 8000m, Balance = 87000m, Description = "AMAZON SHOPPING" },
                        new() { Credit = 0m, Debit = 7000m, Balance = 80000m, Description = "APOLLO PHARMACY MEDICAL" }
                    }
                },
                FinancialAnalysis = new FinancialAnalysisDto
                {
                    MonthlyVerifiedIncome = 80000m,
                    MonthlyDisposableIncome = 45000m,
                    ExistingEmis = 10000m,
                    MonthlyExpenses = 25000m
                },
                AffordabilityResult = new LoanAffordabilityResultDto
                {
                    IsRequestedLoanAffordable = true,
                    RequestedAmount = 500000m,
                    SuggestedPreApprovedAmount = 750000m,
                    AvailableEmiCapacity = 22000m,
                    RequestedEmi = 16254m,
                    RecommendationExplanation = "Customer has strong repayment capacity."
                }
            };

            var request = new PreApprovalAssessmentRequest
            {
                File = CreateDummyFile(),
                RequestedLoan = 500000m,
                Tenure = 36,
                InterestRate = 10.5m
            };

            var actionResult = await _controller.AssessLoan(request);

            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var response = Assert.IsType<PreApprovalAssessmentResponseDto>(okResult.Value);

            Assert.Equal("Approved", response.Status);
            Assert.Equal(500000m, response.ApprovedAmount);
            Assert.Equal(750000m, response.PreApprovedPotentialOffer);
            Assert.Equal(80000m, response.MonthlyIncome);
            Assert.Equal(45000m, response.MonthlyDisposableIncome);
            Assert.Equal(22000m, response.MonthlyEmiCanPay);
            Assert.Equal("Rajesh Verma", response.CustomerName);
            Assert.Equal("987654321098", response.AccountNumber);
            Assert.Equal(500000m, response.RequestedLoan);
            Assert.Equal(36, response.Tenure);
            Assert.Equal(10.5m, response.InterestRate);
            Assert.Equal(10000m, response.FixedDeduction);
            Assert.Equal(25000m, response.VariableDeduction);
            Assert.Equal(150000m, response.TotalCredit);
            Assert.Equal(70000m, response.TotalDebit);
            Assert.Equal(80000m, response.TotalAvailableBalance);

            // Category breakdown assertions
            Assert.Equal(150000m, response.CategoryBreakdown.Salary);
            Assert.Equal(25000m, response.CategoryBreakdown.Rent);
            Assert.Equal(15000m, response.CategoryBreakdown.Groceries);
            Assert.Equal(10000m, response.CategoryBreakdown.LoanEmi);
            Assert.Equal(5000m, response.CategoryBreakdown.UtilityBills);
            Assert.Equal(8000m, response.CategoryBreakdown.Shopping);
            Assert.Equal(7000m, response.CategoryBreakdown.MedicalExpenses);

            // Categorized transactions list assertions
            Assert.Equal(7, response.CategorizedTransactions.Count);
            Assert.Equal("Salary", response.CategorizedTransactions[0].Category);
            Assert.Equal("Rent", response.CategorizedTransactions[1].Category);
            Assert.Equal("Groceries", response.CategorizedTransactions[2].Category);
            Assert.Equal("Loan EMI", response.CategorizedTransactions[3].Category);
            Assert.Equal("Utility bills", response.CategorizedTransactions[4].Category);
            Assert.Equal("Shopping", response.CategorizedTransactions[5].Category);
            Assert.Equal("Medical expenses", response.CategorizedTransactions[6].Category);
        }

        [Fact]
        public async Task AssessLoan_ShouldReturnDisapproved_WhenRequestedLoanExceedsCapacity()
        {
            _fakeService.NextResponse = new LoanRecommendationResponseDto
            {
                CustomerName = "Priya Sharma",
                AccountNumber = "123456789012",
                FinancialAnalysis = new FinancialAnalysisDto
                {
                    MonthlyVerifiedIncome = 40000m,
                    MonthlyDisposableIncome = 10000m
                },
                AffordabilityResult = new LoanAffordabilityResultDto
                {
                    IsRequestedLoanAffordable = false,
                    RequestedAmount = 1500000m,
                    SuggestedPreApprovedAmount = 300000m,
                    AvailableEmiCapacity = 8000m,
                    RequestedEmi = 48750m,
                    RecommendationExplanation = "Requested EMI exceeds safe FOIR limit."
                }
            };

            var request = new PreApprovalAssessmentRequest
            {
                File = CreateDummyFile(),
                RequestedLoan = 1500000m,
                Tenure = 36,
                InterestRate = 10.5m
            };

            var actionResult = await _controller.AssessLoan(request);

            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var response = Assert.IsType<PreApprovalAssessmentResponseDto>(okResult.Value);

            Assert.Equal("Disapproved", response.Status);
            Assert.Equal(0m, response.ApprovedAmount);
            Assert.Equal(300000m, response.PreApprovedPotentialOffer);
            Assert.Equal(40000m, response.MonthlyIncome);
            Assert.Equal(10000m, response.MonthlyDisposableIncome);
            Assert.Equal(8000m, response.MonthlyEmiCanPay);
            Assert.Equal("Priya Sharma", response.CustomerName);
            Assert.Equal("123456789012", response.AccountNumber);
        }

        [Fact]
        public async Task AssessLoan_ShouldExtractCustomerDetails_FromExtractedStatement()
        {
            _fakeService.NextResponse = new LoanRecommendationResponseDto
            {
                CustomerName = "Customer",
                AccountNumber = "UNKNOWN",
                ExtractedStatement = new ExtractedStatementDto
                {
                    AccountHolder = "Vikram Malhotra",
                    AccountNumber = "554433221100"
                },
                FinancialAnalysis = new FinancialAnalysisDto
                {
                    MonthlyVerifiedIncome = 60000m,
                    MonthlyDisposableIncome = 25000m
                },
                AffordabilityResult = new LoanAffordabilityResultDto
                {
                    IsRequestedLoanAffordable = true,
                    RequestedAmount = 400000m,
                    SuggestedPreApprovedAmount = 550000m,
                    AvailableEmiCapacity = 16000m,
                    RequestedEmi = 13000m
                }
            };

            var request = new PreApprovalAssessmentRequest
            {
                File = CreateDummyFile(),
                RequestedLoan = 400000m,
                Tenure = 36,
                InterestRate = 11.0m
            };

            var actionResult = await _controller.AssessLoan(request);

            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var response = Assert.IsType<PreApprovalAssessmentResponseDto>(okResult.Value);

            Assert.Equal("Vikram Malhotra", response.CustomerName);
            Assert.Equal("554433221100", response.AccountNumber);
            Assert.Equal("Approved", response.Status);
            Assert.Equal(400000m, response.ApprovedAmount);
            Assert.Equal(550000m, response.PreApprovedPotentialOffer);
        }

        [Fact]
        public async Task AssessLoan_ShouldReturnNA_WhenCustomerNameAndAccountNumberCannotBeRetrieved()
        {
            _fakeService.NextResponse = new LoanRecommendationResponseDto
            {
                CustomerName = "Customer", // placeholder
                AccountNumber = "UNKNOWN", // placeholder
                ExtractedStatement = new ExtractedStatementDto
                {
                    AccountHolder = "",
                    AccountNumber = ""
                },
                FinancialAnalysis = new FinancialAnalysisDto
                {
                    MonthlyVerifiedIncome = 50000m,
                    MonthlyDisposableIncome = 20000m
                },
                AffordabilityResult = new LoanAffordabilityResultDto
                {
                    IsRequestedLoanAffordable = true,
                    RequestedAmount = 300000m,
                    SuggestedPreApprovedAmount = 400000m,
                    AvailableEmiCapacity = 12000m,
                    RequestedEmi = 9800m
                }
            };

            var request = new PreApprovalAssessmentRequest
            {
                File = CreateDummyFile(),
                RequestedLoan = 300000m,
                Tenure = 36,
                InterestRate = 10.5m
            };

            var actionResult = await _controller.AssessLoan(request);

            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var response = Assert.IsType<PreApprovalAssessmentResponseDto>(okResult.Value);

            Assert.Equal("NA", response.CustomerName);
            Assert.Equal("NA", response.AccountNumber);
        }
    }

    public class FakeLoanRecommendationService : ILoanRecommendationService
    {
        public LoanRecommendationResponseDto NextResponse { get; set; } = new();

        public Task<LoanRecommendationResponseDto> ProcessApplicationAsync(
            Stream fileStream,
            string fileName,
            string contentType,
            ProcessStatementRequestDto request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(NextResponse);
        }

        public Task<ExtractedStatementDto> ExtractStatementOnlyAsync(
            Stream fileStream,
            string fileName,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ExtractedStatementDto());
        }

        public Task<LoanRecommendationResponseDto?> GetApplicationByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<LoanRecommendationResponseDto?>(NextResponse);
        }
    }
}
