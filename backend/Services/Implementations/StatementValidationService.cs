using System;
using System.Linq;
using LoanEngine.Api.Models.DTOs;
using LoanEngine.Api.Services.Interfaces;

namespace LoanEngine.Api.Services.Implementations
{
    public class StatementValidationService : IStatementValidationService
    {
        public StatementValidationResult Validate(ExtractedStatementDto statement)
        {
            var result = new StatementValidationResult();

            if (statement == null)
            {
                result.IsValid = false;
                result.Errors.Add("Statement data is null or could not be parsed.");
                return result;
            }

            if (statement.Transactions == null || !statement.Transactions.Any())
            {
                result.IsValid = false;
                result.Errors.Add("No transactions found in the extracted bank statement.");
                return result;
            }

            result.TotalCredits = statement.Transactions.Sum(t => t.Credit);
            result.TotalDebits = statement.Transactions.Sum(t => t.Debit);

            if (statement.OpeningBalance.HasValue && statement.ClosingBalance.HasValue)
            {
                var expectedClosing = statement.OpeningBalance.Value + result.TotalCredits - result.TotalDebits;
                var difference = Math.Abs(expectedClosing - statement.ClosingBalance.Value);
                if (difference > 1.0m) // allowance for minor rounding in OCR
                {
                    result.BalanceMathMatches = false;
                    result.Warnings.Add($"Calculated closing balance ({expectedClosing:N2}) differs from reported closing balance ({statement.ClosingBalance.Value:N2}) by {difference:N2}.");
                }
            }

            // Check for valid dates
            int invalidDates = 0;
            foreach (var t in statement.Transactions)
            {
                if (!DateTime.TryParse(t.Date, out _))
                {
                    invalidDates++;
                }
            }

            if (invalidDates > 0)
            {
                result.Warnings.Add($"{invalidDates} transaction(s) have unparseable date formats.");
            }

            return result;
        }
    }
}
