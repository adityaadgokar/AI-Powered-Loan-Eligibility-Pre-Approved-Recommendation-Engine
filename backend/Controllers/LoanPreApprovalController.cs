using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LoanEngine.Api.Models.DTOs;
using LoanEngine.Api.Services.Interfaces;

namespace LoanEngine.Api.Controllers
{
    /// <summary>
    /// Dedicated integration controller for external applications.
    /// Evaluates loan pre-approval and returns underwriting outputs with AI transaction categorization.
    /// </summary>
    [ApiController]
    [Route("api/loan/pre-approval")]
    public class LoanPreApprovalController : ControllerBase
    {
        private readonly ILoanRecommendationService _loanRecommendationService;
        private readonly ILogger<LoanPreApprovalController> _logger;

        public LoanPreApprovalController(
            ILoanRecommendationService loanRecommendationService,
            ILogger<LoanPreApprovalController> logger)
        {
            _loanRecommendationService = loanRecommendationService;
            _logger = logger;
        }

        /// <summary>
        /// Evaluate a loan pre-approval request from an external application.
        /// Accepts document File, RequestedLoan, Tenure (months), and InterestRate (% p.a.).
        /// Returns customer details, status, approvedAmount, preApprovedPotentialOffer, income & deductions,
        /// and automatic transaction categorization (Salary, Rent, Groceries, Shopping, Utility bills, Loan EMI, Medical expenses).
        /// </summary>
        [HttpPost]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(PreApprovalAssessmentResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PreApprovalAssessmentResponseDto>> AssessLoan([FromForm] PreApprovalAssessmentRequest form)
        {
            try
            {
                if (form.File == null || form.File.Length == 0)
                {
                    return BadRequest(new { message = "A document file (PDF or image) is required." });
                }

                if (form.RequestedLoan <= 0)
                {
                    return BadRequest(new { message = "Requested Loan amount is required and must be greater than 0." });
                }

                if (form.Tenure <= 0)
                {
                    return BadRequest(new { message = "Tenure (months) is required and must be greater than 0." });
                }

                if (form.InterestRate <= 0)
                {
                    return BadRequest(new { message = "Interest Rate (% p.a.) is required and must be greater than 0." });
                }

                var cancellationToken = HttpContext.RequestAborted;

                var processRequest = new ProcessStatementRequestDto
                {
                    CustomerName = string.Empty,
                    AccountNumber = null,
                    CustomerEmail = null,
                    CustomerPhone = null,
                    RequestedLoanAmount = form.RequestedLoan,
                    TenureMonths = form.Tenure,
                    AnnualInterestRate = form.InterestRate
                };

                using var stream = form.File.OpenReadStream();
                var result = await _loanRecommendationService.ProcessApplicationAsync(
                    stream, form.File.FileName, form.File.ContentType, processRequest, cancellationToken);

                // Resolve customer details; explicitly return "NA" if not retrievable
                var customerName = ResolveCustomerName(result.ExtractedStatement?.AccountHolder, result.CustomerName);
                var accountNumber = ResolveAccountNumber(result.ExtractedStatement?.AccountNumber, result.AccountNumber);

                // Underwriting decision
                var isAffordable = result.AffordabilityResult.IsRequestedLoanAffordable;
                var requestedAmount = result.AffordabilityResult.RequestedAmount > 0
                    ? result.AffordabilityResult.RequestedAmount
                    : form.RequestedLoan;

                var potentialOffer = result.AffordabilityResult.SuggestedPreApprovedAmount;
                var status = isAffordable ? "Approved" : "Disapproved";
                var approvedAmount = isAffordable ? requestedAmount : 0m;

                var remarks = !string.IsNullOrWhiteSpace(result.AffordabilityResult.RecommendationExplanation)
                    ? result.AffordabilityResult.RecommendationExplanation
                    : (isAffordable
                        ? $"Requested loan of ₹{requestedAmount:N0} is approved. Pre-approved potential offer up to ₹{potentialOffer:N0} is available."
                        : $"Requested loan of ₹{requestedAmount:N0} exceeds safe repayment capacity. Pre-approved potential offer up to ₹{potentialOffer:N0} is available.");

                // Compute cash flow & balance metrics from statement
                var totalCredit = result.ExtractedStatement?.Transactions != null && result.ExtractedStatement.Transactions.Any()
                    ? result.ExtractedStatement.Transactions.Sum(t => t.Credit)
                    : 0m;

                var totalDebit = result.ExtractedStatement?.Transactions != null && result.ExtractedStatement.Transactions.Any()
                    ? result.ExtractedStatement.Transactions.Sum(t => t.Debit)
                    : 0m;

                var totalAvailableBalance = result.ExtractedStatement?.ClosingBalance.HasValue == true && result.ExtractedStatement.ClosingBalance.Value > 0
                    ? result.ExtractedStatement.ClosingBalance.Value
                    : (result.ExtractedStatement?.Transactions?.LastOrDefault()?.Balance ?? result.FinancialAnalysis.AverageBalance);

                // Automatically classify transactions into financial categories
                var categorizedTransactions = new List<CategorizedTransactionDto>();
                var categoryBreakdown = new CategoryBreakdownDto();

                if (result.ExtractedStatement?.Transactions != null && result.ExtractedStatement.Transactions.Any())
                {
                    foreach (var tx in result.ExtractedStatement.Transactions)
                    {
                        var category = ClassifyTransaction(tx.Description ?? string.Empty, tx.Credit, tx.Debit);
                        categorizedTransactions.Add(new CategorizedTransactionDto
                        {
                            Date = tx.Date,
                            Description = tx.Description ?? string.Empty,
                            Debit = tx.Debit,
                            Credit = tx.Credit,
                            Balance = tx.Balance,
                            Category = category
                        });

                        var amount = tx.Debit > 0 ? tx.Debit : tx.Credit;
                        switch (category)
                        {
                            case "Salary":
                                categoryBreakdown.Salary += amount;
                                break;
                            case "Rent":
                                categoryBreakdown.Rent += amount;
                                break;
                            case "Groceries":
                                categoryBreakdown.Groceries += amount;
                                break;
                            case "Shopping":
                                categoryBreakdown.Shopping += amount;
                                break;
                            case "Utility bills":
                                categoryBreakdown.UtilityBills += amount;
                                break;
                            case "Loan EMI":
                                categoryBreakdown.LoanEmi += amount;
                                break;
                            case "Medical expenses":
                                categoryBreakdown.MedicalExpenses += amount;
                                break;
                            default:
                                categoryBreakdown.Others += amount;
                                break;
                        }
                    }
                }

                var response = new PreApprovalAssessmentResponseDto
                {
                    CustomerName = customerName,
                    AccountNumber = accountNumber,
                    Status = status,
                    ApprovedAmount = approvedAmount,
                    PreApprovedPotentialOffer = potentialOffer,
                    MonthlyIncome = result.FinancialAnalysis.MonthlyVerifiedIncome,
                    MonthlyDisposableIncome = result.FinancialAnalysis.MonthlyDisposableIncome,
                    MonthlyEmiCanPay = result.AffordabilityResult.AvailableEmiCapacity,
                    RequestedLoan = requestedAmount,
                    Tenure = form.Tenure,
                    InterestRate = form.InterestRate,
                    MonthlyEmi = result.AffordabilityResult.RequestedEmi,
                    FixedDeduction = result.FinancialAnalysis.ExistingEmis,
                    VariableDeduction = result.FinancialAnalysis.MonthlyExpenses,
                    TotalCredit = totalCredit,
                    TotalDebit = totalDebit,
                    TotalAvailableBalance = totalAvailableBalance,
                    CategoryBreakdown = categoryBreakdown,
                    CategorizedTransactions = categorizedTransactions,
                    Remarks = remarks
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing pre-approval assessment: {Message}", ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = $"Error processing assessment: {ex.Message}" });
            }
        }

        /// <summary>
        /// Classifies transactions into standard financial categories:
        /// Salary, Rent, Groceries, Shopping, Utility bills, Loan EMI, Medical expenses.
        /// </summary>
        private static string ClassifyTransaction(string description, decimal credit, decimal debit)
        {
            var desc = description.ToUpperInvariant();

            // 1. Salary
            if (credit > 0 && (desc.Contains("SALARY") || desc.Contains("PAYROLL") || desc.Contains("SAL ") ||
                               desc.Contains("SAL CR") || desc.Contains("STIPEND") || desc.Contains("WAGES") ||
                               desc.Contains("DIRECT DEP") || desc.Contains("MONTHLY SALARY") || desc.Contains("ACH SALARY") ||
                               desc.Contains("NEFT SALARY")))
            {
                return "Salary";
            }

            // 2. Loan EMI
            if (desc.Contains("EMI") || desc.Contains("LOAN") || desc.Contains("FINANCE") ||
                desc.Contains("BAJAJ") || desc.Contains("MORTGAGE") || desc.Contains("CREDIT CARD") ||
                desc.Contains("NACH") || desc.Contains("ACH LOAN"))
            {
                return "Loan EMI";
            }

            // 3. Rent
            if (desc.Contains("RENT") || desc.Contains("LANDLORD") || desc.Contains("LEASE") ||
                desc.Contains("TENANT") || desc.Contains("SOCIETY MAINT") || desc.Contains("MAINTENANCE") ||
                desc.Contains("FLAT RENT") || desc.Contains("PG RENT"))
            {
                return "Rent";
            }

            // 4. Medical expenses
            if (desc.Contains("MEDICAL") || desc.Contains("HOSPITAL") || desc.Contains("PHARMACY") ||
                desc.Contains("CLINIC") || desc.Contains("APOLLO") || desc.Contains("MEDPLUS") ||
                desc.Contains("1MG") || desc.Contains("PHARMEASY") || desc.Contains("NETMEDS") ||
                desc.Contains("DOCTOR") || desc.Contains("HEALTH") || desc.Contains("DIAGNOSTIC") ||
                desc.Contains("MEDICINE") || desc.Contains("DENTAL") || desc.Contains("LAB") ||
                desc.Contains("SURGERY") || desc.Contains("HEALTHCARE"))
            {
                return "Medical expenses";
            }

            // 5. Utility bills
            if (desc.Contains("UTILITY") || desc.Contains("ELECTRICITY") || desc.Contains("POWER") ||
                desc.Contains("WATER") || desc.Contains("GAS") || desc.Contains("WIFI") ||
                desc.Contains("BROADBAND") || desc.Contains("INTERNET") || desc.Contains("MOBILE BILL") ||
                desc.Contains("RECHARGE") || desc.Contains("AIRTEL") || desc.Contains("JIO") ||
                desc.Contains("VODAFONE") || desc.Contains("BILLPAY") || desc.Contains("BBPS") ||
                desc.Contains("DTH") || desc.Contains("BESCOM") || desc.Contains("MSEDCL") ||
                desc.Contains("TATAPLAY"))
            {
                return "Utility bills";
            }

            // 6. Groceries
            if (desc.Contains("GROCERY") || desc.Contains("GROCERIES") || desc.Contains("SUPERMARKET") ||
                desc.Contains("DMART") || desc.Contains("BIGBASKET") || desc.Contains("BLINKIT") ||
                desc.Contains("ZEPTO") || desc.Contains("INSTAMART") || desc.Contains("RELIANCE FRESH") ||
                desc.Contains("MORE") || desc.Contains("VEGETABLE") || desc.Contains("PROVISION") ||
                desc.Contains("MILK") || desc.Contains("DAIRY") || desc.Contains("FOOD BAZAAR") ||
                desc.Contains("SPENCER"))
            {
                return "Groceries";
            }

            // 7. Shopping
            if (desc.Contains("SHOPPING") || desc.Contains("AMAZON") || desc.Contains("FLIPKART") ||
                desc.Contains("MYNTRA") || desc.Contains("AJIO") || desc.Contains("ZARA") ||
                desc.Contains("H&M") || desc.Contains("LIFESTYLE") || desc.Contains("RETAIL") ||
                desc.Contains("MALL") || desc.Contains("STORE") || desc.Contains("PURCHASE") ||
                desc.Contains("POS") || desc.Contains("MEESHO") || desc.Contains("NYKAA") ||
                desc.Contains("CLOTHING") || desc.Contains("APPAREL"))
            {
                return "Shopping";
            }

            return "Others";
        }

        private static string ResolveCustomerName(string? extractedHolder, string? resultName)
        {
            if (!string.IsNullOrWhiteSpace(extractedHolder) && !IsPlaceholder(extractedHolder))
                return extractedHolder.Trim();

            if (!string.IsNullOrWhiteSpace(resultName) && !IsPlaceholder(resultName))
                return resultName.Trim();

            return "NA";
        }

        private static string ResolveAccountNumber(string? extractedAccount, string? resultAccount)
        {
            if (!string.IsNullOrWhiteSpace(extractedAccount) && !IsPlaceholder(extractedAccount))
                return extractedAccount.Trim();

            if (!string.IsNullOrWhiteSpace(resultAccount) && !IsPlaceholder(resultAccount))
                return resultAccount.Trim();

            return "NA";
        }

        private static bool IsPlaceholder(string value) =>
            string.Equals(value.Trim(), "Customer", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value.Trim(), "UNKNOWN", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value.Trim(), "Not Found", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value.Trim(), "N/A", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value.Trim(), "NA", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value.Trim(), "null", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value.Trim(), "Rahul Sharma", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value.Trim(), "XXXX1234", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value.Trim(), "Not Available", StringComparison.OrdinalIgnoreCase);
    }
}
