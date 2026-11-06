using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LoanEngine.Api.Models.DTOs
{
    /// <summary>
    /// Compact response tailored for third-party application integration.
    /// Returns essential underwriting decisions, income metrics, and AI transaction categorization.
    /// </summary>
    public class PreApprovalAssessmentResponseDto
    {
        /// <summary>
        /// Customer name extracted directly from the uploaded document (returns "NA" if not retrievable).
        /// </summary>
        [JsonPropertyName("customerName")]
        public string CustomerName { get; set; } = "NA";

        /// <summary>
        /// Bank account number extracted directly from the uploaded document (returns "NA" if not retrievable).
        /// </summary>
        [JsonPropertyName("accountNumber")]
        public string AccountNumber { get; set; } = "NA";

        /// <summary>
        /// Application decision status ("Approved" or "Disapproved").
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; } = "Disapproved";

        /// <summary>
        /// Final approved loan amount. If approved, reflects the approved amount; if disapproved, 0.
        /// </summary>
        [JsonPropertyName("approvedAmount")]
        public decimal ApprovedAmount { get; set; }

        /// <summary>
        /// Pre-approved potential offer computed by the loan engine (can be less, equal, or greater than requested loan).
        /// </summary>
        [JsonPropertyName("preApprovedPotentialOffer")]
        public decimal PreApprovedPotentialOffer { get; set; }

        /// <summary>
        /// Verified monthly income extracted from bank statement cash flow.
        /// </summary>
        [JsonPropertyName("monthlyIncome")]
        public decimal MonthlyIncome { get; set; }

        /// <summary>
        /// Net monthly disposable income remaining after living expenses and debt payments.
        /// </summary>
        [JsonPropertyName("monthlyDisposableIncome")]
        public decimal MonthlyDisposableIncome { get; set; }

        /// <summary>
        /// Monthly EMI the customer can afford to pay under policy FOIR (40%).
        /// </summary>
        [JsonPropertyName("monthlyEmiCanPay")]
        public decimal MonthlyEmiCanPay { get; set; }

        /// <summary>
        /// Requested loan principal amount.
        /// </summary>
        [JsonPropertyName("requestedLoan")]
        public decimal RequestedLoan { get; set; }

        /// <summary>
        /// Loan tenure in months.
        /// </summary>
        [JsonPropertyName("tenure")]
        public int Tenure { get; set; }

        /// <summary>
        /// Annual interest rate (% p.a.).
        /// </summary>
        [JsonPropertyName("interestRate")]
        public decimal InterestRate { get; set; }

        /// <summary>
        /// Calculated monthly EMI for the requested loan.
        /// </summary>
        [JsonPropertyName("monthlyEmi")]
        public decimal MonthlyEmi { get; set; }

        /// <summary>
        /// Fixed monthly debt deductions (e.g., existing loan EMIs).
        /// </summary>
        [JsonPropertyName("fixedDeduction")]
        public decimal FixedDeduction { get; set; }

        /// <summary>
        /// Variable monthly living expenses (e.g., groceries, utility bills, personal expenses).
        /// </summary>
        [JsonPropertyName("variableDeduction")]
        public decimal VariableDeduction { get; set; }

        /// <summary>
        /// Total credit inflows across the bank statement.
        /// </summary>
        [JsonPropertyName("totalCredit")]
        public decimal TotalCredit { get; set; }

        /// <summary>
        /// Total debit outflows across the bank statement.
        /// </summary>
        [JsonPropertyName("totalDebit")]
        public decimal TotalDebit { get; set; }

        /// <summary>
        /// Total available balance (closing balance or latest account balance).
        /// </summary>
        [JsonPropertyName("totalAvailableBalance")]
        public decimal TotalAvailableBalance { get; set; }

        /// <summary>
        /// Aggregated spending breakdown across key spending categories.
        /// </summary>
        [JsonPropertyName("categoryBreakdown")]
        public CategoryBreakdownDto CategoryBreakdown { get; set; } = new();

        /// <summary>
        /// List of statement transactions automatically classified into financial categories.
        /// </summary>
        [JsonPropertyName("categorizedTransactions")]
        public List<CategorizedTransactionDto> CategorizedTransactions { get; set; } = new();

        /// <summary>
        /// Detailed underwriting remarks explaining the decision rationale.
        /// </summary>
        [JsonPropertyName("remarks")]
        public string Remarks { get; set; } = string.Empty;
    }

    /// <summary>
    /// Cumulative spending breakdown by AI categorized transaction types.
    /// </summary>
    public class CategoryBreakdownDto
    {
        [JsonPropertyName("salary")]
        public decimal Salary { get; set; }

        [JsonPropertyName("rent")]
        public decimal Rent { get; set; }

        [JsonPropertyName("groceries")]
        public decimal Groceries { get; set; }

        [JsonPropertyName("shopping")]
        public decimal Shopping { get; set; }

        [JsonPropertyName("utilityBills")]
        public decimal UtilityBills { get; set; }

        [JsonPropertyName("loanEmi")]
        public decimal LoanEmi { get; set; }

        [JsonPropertyName("medicalExpenses")]
        public decimal MedicalExpenses { get; set; }

        [JsonPropertyName("others")]
        public decimal Others { get; set; }
    }

    /// <summary>
    /// Individual statement transaction enriched with an AI category.
    /// </summary>
    public class CategorizedTransactionDto
    {
        [JsonPropertyName("date")]
        public string Date { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("debit")]
        public decimal Debit { get; set; }

        [JsonPropertyName("credit")]
        public decimal Credit { get; set; }

        [JsonPropertyName("balance")]
        public decimal Balance { get; set; }

        [JsonPropertyName("category")]
        public string Category { get; set; } = "Others";
    }
}

