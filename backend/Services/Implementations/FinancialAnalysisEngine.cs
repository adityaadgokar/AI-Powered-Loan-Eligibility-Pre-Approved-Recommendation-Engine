using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LoanEngine.Api.Models.DTOs;
using LoanEngine.Api.Services.Interfaces;

namespace LoanEngine.Api.Services.Implementations
{
    public class FinancialAnalysisEngine : IFinancialAnalysisEngine
    {
        private static readonly string[] SalaryKeywords = new[]
        {
            "SALARY", "SAL ", "PAYROLL", "ACH SALARY", "NEFT SALARY", "SAL CR",
            "MONTHLY SALARY", "CREDIT BY SALARY", "SALARY CREDIT", "SALARY TRANS"
        };

        private static readonly string[] EmiKeywords = new[]
        {
            "EMI", "LOAN", "FINANCE", "BAJAJ", "HOME LOAN", "AUTO LOAN",
            "PERSONAL LOAN", "HDFC LOAN", "SBI LOAN", "ICICI LOAN", "NACH LOAN", "ACH LOAN", "MORTGAGE"
        };

        public FinancialAnalysisDto Analyze(ExtractedStatementDto statement)
        {
            var analysis = new FinancialAnalysisDto();

            if (statement?.Transactions == null || !statement.Transactions.Any())
            {
                return analysis;
            }

            var transactions = statement.Transactions;

            // 1. Identify distinct months present in statement
            var transactionDates = new List<DateTime>();
            foreach (var t in transactions)
            {
                if (DateTime.TryParse(t.Date, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt) ||
                    DateTime.TryParse(t.Date, out dt))
                {
                    transactionDates.Add(dt);
                }
            }

            int monthsCount = 1;
            if (transactionDates.Any())
            {
                var minDate = transactionDates.Min();
                var maxDate = transactionDates.Max();
                monthsCount = Math.Max(1, ((maxDate.Year - minDate.Year) * 12) + maxDate.Month - minDate.Month + 1);
            }
            analysis.AnalyzedMonthsCount = monthsCount;

            // 2. Identify Salary Credits
            var salaryCredits = new List<ExtractedTransactionDto>();
            foreach (var t in transactions.Where(x => x.Credit > 0))
            {
                var desc = t.Description?.ToUpperInvariant() ?? string.Empty;
                if (SalaryKeywords.Any(k => desc.Contains(k)))
                {
                    salaryCredits.Add(t);
                    analysis.DetectedSalaryEntries.Add($"{t.Date}: {t.Description} - ₹{t.Credit:N2}");
                }
            }

            decimal totalSalary = salaryCredits.Sum(s => s.Credit);
            decimal totalCredits = transactions.Sum(t => t.Credit);

            // If salary credits were detected explicitly, average them across months
            if (salaryCredits.Any())
            {
                analysis.MonthlyVerifiedIncome = Math.Round(totalSalary / monthsCount, 2);
            }
            else
            {
                // Fallback: If no explicit salary keyword, treat recurring major credits or average credits as income
                // Take regular recurring credits >= 15000 or average monthly credit
                var recurringCredits = transactions.Where(t => t.Credit >= 15000).ToList();
                if (recurringCredits.Any())
                {
                    analysis.MonthlyVerifiedIncome = Math.Round(recurringCredits.Sum(c => c.Credit) / monthsCount, 2);
                    foreach (var rc in recurringCredits)
                    {
                        analysis.DetectedSalaryEntries.Add($"{rc.Date}: {rc.Description} (Probable Income) - ₹{rc.Credit:N2}");
                    }
                }
                else
                {
                    analysis.MonthlyVerifiedIncome = Math.Round(totalCredits / monthsCount, 2);
                }
            }

            // 3. Identify Existing EMIs
            var emiDebits = new List<ExtractedTransactionDto>();
            foreach (var t in transactions.Where(x => x.Debit > 0))
            {
                var desc = t.Description?.ToUpperInvariant() ?? string.Empty;
                if (EmiKeywords.Any(k => desc.Contains(k)))
                {
                    emiDebits.Add(t);
                    analysis.DetectedEmiEntries.Add($"{t.Date}: {t.Description} - ₹{t.Debit:N2}");
                }
            }

            decimal totalEmis = emiDebits.Sum(e => e.Debit);
            analysis.ExistingEmis = monthsCount > 0 ? Math.Round(totalEmis / monthsCount, 2) : 0m;

            // 4. Identify Monthly Expenses (Total non-EMI debits averaged per month)
            var regularDebits = transactions.Where(t => t.Debit > 0 && !emiDebits.Contains(t)).ToList();
            decimal totalExpenses = regularDebits.Sum(t => t.Debit);
            analysis.MonthlyExpenses = monthsCount > 0 ? Math.Round(totalExpenses / monthsCount, 2) : 0m;

            // 5. Calculate Average Balance & Lowest Balance
            if (transactions.Any(t => t.Balance > 0))
            {
                analysis.AverageBalance = Math.Round(transactions.Average(t => t.Balance), 2);
                analysis.LowestBalance = Math.Round(transactions.Min(t => t.Balance), 2);
            }
            else if (statement.OpeningBalance.HasValue || statement.ClosingBalance.HasValue)
            {
                var ob = statement.OpeningBalance ?? 0m;
                var cb = statement.ClosingBalance ?? 0m;
                analysis.AverageBalance = Math.Round((ob + cb) / 2m, 2);
                analysis.LowestBalance = Math.Min(ob, cb);
            }

            // 6. Disposable Income & Net Cash Flow
            analysis.MonthlyDisposableIncome = Math.Max(0m, analysis.MonthlyVerifiedIncome - analysis.MonthlyExpenses - analysis.ExistingEmis);
            analysis.NetCashFlow = Math.Round(totalCredits - transactions.Sum(t => t.Debit), 2);

            // 7. Ratios
            if (analysis.MonthlyVerifiedIncome > 0)
            {
                analysis.ExpenseRatio = Math.Round((analysis.MonthlyExpenses / analysis.MonthlyVerifiedIncome) * 100m, 2);
                analysis.ExistingEmiBurdenRatio = Math.Round((analysis.ExistingEmis / analysis.MonthlyVerifiedIncome) * 100m, 2);
            }

            return analysis;
        }
    }
}
