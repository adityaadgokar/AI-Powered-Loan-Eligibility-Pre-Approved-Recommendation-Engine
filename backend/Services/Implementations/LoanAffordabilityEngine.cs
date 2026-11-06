using System;
using LoanEngine.Api.Models.DTOs;
using LoanEngine.Api.Services.Interfaces;

namespace LoanEngine.Api.Services.Implementations
{
    public class LoanAffordabilityEngine : ILoanAffordabilityEngine
    {
        public decimal CalculateEmi(decimal principal, decimal annualInterestRate, int tenureMonths)
        {
            if (principal <= 0 || tenureMonths <= 0)
                return 0m;

            if (annualInterestRate <= 0)
                return Math.Round(principal / tenureMonths, 2);

            // r = monthly interest rate
            double r = (double)(annualInterestRate / (12m * 100m));
            int n = tenureMonths;
            double p = (double)principal;

            // EMI = [P * r * (1 + r)^n] / [(1 + r)^n - 1]
            double pow = Math.Pow(1 + r, n);
            double emi = (p * r * pow) / (pow - 1);

            return Math.Round((decimal)emi, 2);
        }

        public decimal CalculateMaxPrincipalFromEmi(decimal maxMonthlyEmi, decimal annualInterestRate, int tenureMonths)
        {
            if (maxMonthlyEmi <= 0 || tenureMonths <= 0)
                return 0m;

            if (annualInterestRate <= 0)
                return Math.Round(maxMonthlyEmi * tenureMonths, 2);

            // r = monthly interest rate
            double r = (double)(annualInterestRate / (12m * 100m));
            int n = tenureMonths;
            double emi = (double)maxMonthlyEmi;

            // P = [EMI * ((1 + r)^n - 1)] / [r * (1 + r)^n]
            double pow = Math.Pow(1 + r, n);
            double principal = (emi * (pow - 1)) / (r * pow);

            return Math.Round((decimal)principal, 2);
        }

        public LoanAffordabilityResultDto EvaluateAffordability(
            decimal requestedAmount,
            int tenureMonths,
            decimal annualInterestRate,
            FinancialAnalysisDto financialAnalysis,
            decimal maxFoirPercentage = 40.0m)
        {
            var result = new LoanAffordabilityResultDto
            {
                RequestedAmount = requestedAmount,
                TenureMonths = tenureMonths,
                AnnualInterestRate = annualInterestRate,
                MonthlyInterestRate = Math.Round(annualInterestRate / (12m * 100m), 6),
                ExistingEmis = financialAnalysis.ExistingEmis,
                DisposableIncome = financialAnalysis.MonthlyDisposableIncome,
                PolicyMaxFoirPercentage = maxFoirPercentage
            };

            // 1. Calculate requested EMI
            result.RequestedEmi = CalculateEmi(requestedAmount, annualInterestRate, tenureMonths);
            result.TotalEmiWithExisting = result.RequestedEmi + result.ExistingEmis;

            // 2. Policy limit: Max allowed total EMI (FOIR rule)
            result.MaxAllowedTotalEmi = Math.Round(financialAnalysis.MonthlyVerifiedIncome * (maxFoirPercentage / 100m), 2);
            result.AvailableEmiCapacity = Math.Max(0m, result.MaxAllowedTotalEmi - result.ExistingEmis);

            // 3. Compute remaining income after all EMIs
            result.RemainingMonthlyIncomeAfterAllEmis = financialAnalysis.MonthlyVerifiedIncome - financialAnalysis.MonthlyExpenses - result.TotalEmiWithExisting;

            // 4. Actual FOIR with requested loan
            if (financialAnalysis.MonthlyVerifiedIncome > 0)
            {
                result.ActualFoirPercentage = Math.Round((result.TotalEmiWithExisting / financialAnalysis.MonthlyVerifiedIncome) * 100m, 2);
            }

            // 5. Affordability decision
            bool fitsWithinFoir = result.TotalEmiWithExisting <= result.MaxAllowedTotalEmi;
            bool fitsWithinDisposableIncome = result.RequestedEmi <= result.DisposableIncome;

            result.IsRequestedLoanAffordable = fitsWithinFoir && fitsWithinDisposableIncome && financialAnalysis.MonthlyVerifiedIncome > 0;

            // 6. Calculate Maximum Eligible Loan Amount based on available EMI capacity
            var rawMaxPrincipal = CalculateMaxPrincipalFromEmi(result.AvailableEmiCapacity, annualInterestRate, tenureMonths);
            result.MaxEligibleLoanAmount = Math.Max(0m, rawMaxPrincipal);

            // Round suggested pre-approved amount to nearest ₹10,000 or ₹50,000 for neat banking presentation
            decimal roundedPreApproved = Math.Floor(result.MaxEligibleLoanAmount / 10000m) * 10000m;
            result.SuggestedPreApprovedAmount = roundedPreApproved;

            // 7. Generate concise decision explanation
            if (result.IsRequestedLoanAffordable)
            {
                result.EligibilityStatus = "Eligible - Pre-Approved Potential";
                if (result.SuggestedPreApprovedAmount > requestedAmount)
                {
                    result.RecommendationExplanation =
                        $"Customer has consistent monthly income of ₹{financialAnalysis.MonthlyVerifiedIncome:N0}, manageable existing liabilities of ₹{financialAnalysis.ExistingEmis:N0}, and healthy disposable income of ₹{financialAnalysis.MonthlyDisposableIncome:N0}. " +
                        $"The requested loan of ₹{requestedAmount:N0} (EMI ₹{result.RequestedEmi:N0}) is fully affordable under the {maxFoirPercentage}% FOIR policy. " +
                        $"Based on surplus repayment capacity, customer is eligible for an enhanced pre-approved loan of up to ₹{result.SuggestedPreApprovedAmount:N0}!";
                }
                else
                {
                    result.RecommendationExplanation =
                        $"Customer has adequate income and manageable existing obligations to service the requested loan of ₹{requestedAmount:N0} (EMI ₹{result.RequestedEmi:N0}) with healthy debt utilization ({result.ActualFoirPercentage}% FOIR).";
                }
            }
            else
            {
                result.EligibilityStatus = "High Debt Burden / Ineligible";
                result.RecommendationExplanation =
                    $"Requested loan EMI (₹{result.RequestedEmi:N0}) exceeds safe repayment parameters. " +
                    $"Total debt obligations would reach {result.ActualFoirPercentage}% FOIR (policy maximum: {maxFoirPercentage}%). " +
                    $"Maximum affordable loan under policy is ₹{result.SuggestedPreApprovedAmount:N0}.";
            }

            return result;
        }
    }
}
