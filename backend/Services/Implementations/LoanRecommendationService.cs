using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LoanEngine.Api.Models.Data;
using LoanEngine.Api.Models.DTOs;
using LoanEngine.Api.Models.Entities;
using LoanEngine.Api.Services.Interfaces;

namespace LoanEngine.Api.Services.Implementations
{
    public class LoanRecommendationService : ILoanRecommendationService
    {
        private readonly IGeminiDocumentExtractorService _geminiExtractor;
        private readonly IStatementValidationService _validator;
        private readonly IFinancialAnalysisEngine _financialAnalyzer;
        private readonly ILoanAffordabilityEngine _affordabilityEngine;
        private readonly LoanDbContext _dbContext;
        private readonly ILogger<LoanRecommendationService> _logger;

        public LoanRecommendationService(
            IGeminiDocumentExtractorService geminiExtractor,
            IStatementValidationService validator,
            IFinancialAnalysisEngine financialAnalyzer,
            ILoanAffordabilityEngine affordabilityEngine,
            LoanDbContext dbContext,
            ILogger<LoanRecommendationService> logger)
        {
            _geminiExtractor = geminiExtractor;
            _validator = validator;
            _financialAnalyzer = financialAnalyzer;
            _affordabilityEngine = affordabilityEngine;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<LoanRecommendationResponseDto> ProcessApplicationAsync(
            Stream fileStream,
            string fileName,
            string contentType,
            ProcessStatementRequestDto request,
            CancellationToken cancellationToken = default)
        {
            // 1. OCR Extraction via Gemini
            _logger.LogInformation("Calling Gemini OCR extraction for file: {FileName}", fileName);
            var extractedStatement = await _geminiExtractor.ExtractStatementDataAsync(fileStream, fileName, contentType, cancellationToken);

            // 2. Validation
            var validationResult = _validator.Validate(extractedStatement);
            _logger.LogInformation("Statement validation result: Valid={IsValid}, Credits={Credits}, Debits={Debits}",
                validationResult.IsValid, validationResult.TotalCredits, validationResult.TotalDebits);

            // 3. Financial Analysis
            var financialAnalysis = _financialAnalyzer.Analyze(extractedStatement);

            // 4. Loan Affordability & Recommendation
            var affordabilityResult = _affordabilityEngine.EvaluateAffordability(
                request.RequestedLoanAmount,
                request.TenureMonths,
                request.AnnualInterestRate,
                financialAnalysis,
                maxFoirPercentage: 40.0m);

            // 5. Persist to SQL Server Database
            int applicationId = 0;
            int customerId = 0;
            try
            {
                // Ensure customer
                var customer = await _dbContext.Customers
                    .FirstOrDefaultAsync(c => c.Name == request.CustomerName || (c.Email != null && c.Email == request.CustomerEmail), cancellationToken);

                if (customer == null)
                {
                    customer = new Customer
                    {
                        Name = string.IsNullOrWhiteSpace(request.CustomerName) ? (extractedStatement.AccountHolder ?? "Customer") : request.CustomerName,
                        Email = request.CustomerEmail,
                        Phone = request.CustomerPhone,
                        CreatedAt = DateTime.UtcNow
                    };
                    _dbContext.Customers.Add(customer);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
                customerId = customer.Id;

                var resolvedAccountNumber = !string.IsNullOrWhiteSpace(request.AccountNumber) 
                    ? request.AccountNumber 
                    : (!string.IsNullOrWhiteSpace(extractedStatement.AccountNumber) ? extractedStatement.AccountNumber : "UNKNOWN");

                // Create BankStatement entity
                var bankStatement = new BankStatement
                {
                    CustomerId = customer.Id,
                    FileName = fileName,
                    AccountNumber = resolvedAccountNumber,
                    BankName = extractedStatement.BankName ?? "Bank",
                    OpeningBalance = extractedStatement.OpeningBalance ?? 0m,
                    ClosingBalance = extractedStatement.ClosingBalance ?? 0m,
                    UploadDate = DateTime.UtcNow,
                    OcrRawJson = JsonSerializer.Serialize(extractedStatement)
                };

                if (DateTime.TryParse(extractedStatement.StatementPeriod?.StartDate, out var sDate))
                    bankStatement.StartDate = sDate;
                if (DateTime.TryParse(extractedStatement.StatementPeriod?.EndDate, out var eDate))
                    bankStatement.EndDate = eDate;

                _dbContext.BankStatements.Add(bankStatement);
                await _dbContext.SaveChangesAsync(cancellationToken);

                // Add transactions
                if (extractedStatement.Transactions != null && extractedStatement.Transactions.Any())
                {
                    foreach (var tx in extractedStatement.Transactions)
                    {
                        var tEntity = new BankTransaction
                        {
                            BankStatementId = bankStatement.Id,
                            TransactionDate = DateTime.TryParse(tx.Date, out var tDate) ? tDate : DateTime.UtcNow,
                            Description = tx.Description ?? string.Empty,
                            Debit = tx.Debit,
                            Credit = tx.Credit,
                            Balance = tx.Balance,
                            IsSalaryCredit = tx.Credit > 0 && (tx.Description?.Contains("SALARY", StringComparison.OrdinalIgnoreCase) ?? false),
                            IsEmiDebit = tx.Debit > 0 && ((tx.Description?.Contains("EMI", StringComparison.OrdinalIgnoreCase) ?? false) || (tx.Description?.Contains("LOAN", StringComparison.OrdinalIgnoreCase) ?? false))
                        };
                        _dbContext.BankTransactions.Add(tEntity);
                    }
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }

                // Create LoanApplication entity
                var loanApp = new LoanApplication
                {
                    CustomerId = customer.Id,
                    BankStatementId = bankStatement.Id,
                    RequestedAmount = request.RequestedLoanAmount,
                    TenureMonths = request.TenureMonths,
                    AnnualInterestRate = request.AnnualInterestRate,
                    Status = affordabilityResult.IsRequestedLoanAffordable ? "Approved/Pre-Approved" : "Referred/Ineligible",
                    CreatedAt = DateTime.UtcNow
                };
                _dbContext.LoanApplications.Add(loanApp);
                await _dbContext.SaveChangesAsync(cancellationToken);
                applicationId = loanApp.Id;

                // Save LoanEligibilityResult entity
                var resultEntity = new LoanEligibilityResult
                {
                    LoanApplicationId = loanApp.Id,
                    MonthlyIncome = financialAnalysis.MonthlyVerifiedIncome,
                    MonthlyExpenses = financialAnalysis.MonthlyExpenses,
                    ExistingEmis = financialAnalysis.ExistingEmis,
                    AverageBalance = financialAnalysis.AverageBalance,
                    DisposableIncome = financialAnalysis.MonthlyDisposableIncome,
                    RequestedEmi = affordabilityResult.RequestedEmi,
                    TotalEmiWithExisting = affordabilityResult.TotalEmiWithExisting,
                    MaxAffordableEmi = affordabilityResult.MaxAllowedTotalEmi,
                    MaxEligibleLoanAmount = affordabilityResult.MaxEligibleLoanAmount,
                    SuggestedPreApprovedAmount = affordabilityResult.SuggestedPreApprovedAmount,
                    IsRequestedLoanAffordable = affordabilityResult.IsRequestedLoanAffordable,
                    FoirPercentage = affordabilityResult.ActualFoirPercentage,
                    RecommendationExplanation = affordabilityResult.RecommendationExplanation,
                    CalculatedAt = DateTime.UtcNow
                };
                _dbContext.LoanEligibilityResults.Add(resultEntity);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to persist to SQL Server database. Will still return calculation result.");
            }

            var finalAccountNumber = !string.IsNullOrWhiteSpace(request.AccountNumber)
                ? request.AccountNumber
                : (!string.IsNullOrWhiteSpace(extractedStatement.AccountNumber) ? extractedStatement.AccountNumber : "UNKNOWN");

            return new LoanRecommendationResponseDto
            {
                ApplicationId = applicationId,
                CustomerId = customerId,
                CustomerName = string.IsNullOrWhiteSpace(request.CustomerName) ? (extractedStatement.AccountHolder ?? "Customer") : request.CustomerName,
                AccountNumber = finalAccountNumber,
                BankName = extractedStatement.BankName,
                ExtractedStatement = extractedStatement,
                FinancialAnalysis = financialAnalysis,
                AffordabilityResult = affordabilityResult
            };
        }

        public async Task<ExtractedStatementDto> ExtractStatementOnlyAsync(
            Stream fileStream,
            string fileName,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Calling quick statement extraction for file: {FileName}", fileName);
            return await _geminiExtractor.ExtractStatementDataAsync(fileStream, fileName, contentType, cancellationToken);
        }

        public async Task<LoanRecommendationResponseDto?> GetApplicationByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var app = await _dbContext.LoanApplications
                .Include(a => a.Customer)
                .Include(a => a.BankStatement)
                    .ThenInclude(s => s.Transactions)
                .Include(a => a.EligibilityResult)
                .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

            if (app == null) return null;

            var res = app.EligibilityResult;
            var stmt = app.BankStatement;

            return new LoanRecommendationResponseDto
            {
                ApplicationId = app.Id,
                CustomerId = app.CustomerId,
                CustomerName = app.Customer.Name,
                AccountNumber = stmt.AccountNumber,
                BankName = stmt.BankName,
                ExtractedStatement = new ExtractedStatementDto
                {
                    AccountHolder = app.Customer.Name,
                    AccountNumber = stmt.AccountNumber,
                    BankName = stmt.BankName,
                    OpeningBalance = stmt.OpeningBalance,
                    ClosingBalance = stmt.ClosingBalance,
                    StatementPeriod = new StatementPeriodDto
                    {
                        StartDate = stmt.StartDate?.ToString("yyyy-MM-dd"),
                        EndDate = stmt.EndDate?.ToString("yyyy-MM-dd")
                    },
                    Transactions = stmt.Transactions.Select(t => new ExtractedTransactionDto
                    {
                        Date = t.TransactionDate.ToString("yyyy-MM-dd"),
                        Description = t.Description,
                        Debit = t.Debit,
                        Credit = t.Credit,
                        Balance = t.Balance
                    }).ToList()
                },
                FinancialAnalysis = new FinancialAnalysisDto
                {
                    MonthlyVerifiedIncome = res?.MonthlyIncome ?? 0m,
                    MonthlyExpenses = res?.MonthlyExpenses ?? 0m,
                    ExistingEmis = res?.ExistingEmis ?? 0m,
                    AverageBalance = res?.AverageBalance ?? 0m,
                    MonthlyDisposableIncome = res?.DisposableIncome ?? 0m
                },
                AffordabilityResult = new LoanAffordabilityResultDto
                {
                    RequestedAmount = app.RequestedAmount,
                    TenureMonths = app.TenureMonths,
                    AnnualInterestRate = app.AnnualInterestRate,
                    RequestedEmi = res?.RequestedEmi ?? 0m,
                    ExistingEmis = res?.ExistingEmis ?? 0m,
                    TotalEmiWithExisting = res?.TotalEmiWithExisting ?? 0m,
                    MaxAllowedTotalEmi = res?.MaxAffordableEmi ?? 0m,
                    MaxEligibleLoanAmount = res?.MaxEligibleLoanAmount ?? 0m,
                    SuggestedPreApprovedAmount = res?.SuggestedPreApprovedAmount ?? 0m,
                    IsRequestedLoanAffordable = res?.IsRequestedLoanAffordable ?? false,
                    ActualFoirPercentage = res?.FoirPercentage ?? 0m,
                    RecommendationExplanation = res?.RecommendationExplanation ?? string.Empty
                }
            };
        }
    }
}
