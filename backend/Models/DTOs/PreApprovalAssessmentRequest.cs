using Microsoft.AspNetCore.Http;

namespace LoanEngine.Api.Models.DTOs
{
    /// <summary>
    /// Request model for the lightweight pre-approval integration endpoint.
    /// Strictly contains the 4 required parameters: File, RequestedLoan, Tenure, InterestRate.
    /// </summary>
    public class PreApprovalAssessmentRequest
    {
        /// <summary>
        /// Bank statement or payslip document (PDF or image).
        /// </summary>
        public IFormFile? File { get; set; }

        /// <summary>
        /// Requested loan principal amount.
        /// </summary>
        public decimal RequestedLoan { get; set; }

        /// <summary>
        /// Loan tenure in months (e.g. 12, 24, 36, 60).
        /// </summary>
        public int Tenure { get; set; }

        /// <summary>
        /// Annual interest rate (% p.a., e.g. 10.5 for 10.5%).
        /// </summary>
        public decimal InterestRate { get; set; }
    }
}
