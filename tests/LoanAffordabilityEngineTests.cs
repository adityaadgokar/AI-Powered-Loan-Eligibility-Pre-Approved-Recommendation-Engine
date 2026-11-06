using System.Collections.Generic;
using Xunit;
using LoanEngine.Api.Models.DTOs;
using LoanEngine.Api.Services.Implementations;

namespace LoanEngine.Tests
{
    public class LoanAffordabilityEngineTests
    {
        private readonly LoanAffordabilityEngine _engine = new();

        [Fact]
        public void CalculateEmi_ShouldCalculateAccurately_ForStandardScenario()
        {
            // Principal: ₹5,00,000, 12% p.a., 60 months
            decimal principal = 500000m;
            decimal annualRate = 12.0m;
            int tenureMonths = 60;

            var emi = _engine.CalculateEmi(principal, annualRate, tenureMonths);

            // Theoretical formula: 500000 * 0.01 * (1.01)^60 / ((1.01)^60 - 1) = 11122.22
            Assert.Equal(11122.22m, emi);
        }

        [Fact]
        public void CalculateMaxPrincipalFromEmi_ShouldInvertCorrectly()
        {
            // At ₹20,000 monthly available EMI capacity, 12% p.a., 60 months
            decimal availableEmi = 20000m;
            decimal annualRate = 12.0m;
            int tenureMonths = 60;

            var maxPrincipal = _engine.CalculateMaxPrincipalFromEmi(availableEmi, annualRate, tenureMonths);

            // Expected around ₹8,99,100
            Assert.True(maxPrincipal > 899000m && maxPrincipal < 900000m);
        }

        [Fact]
        public void EvaluateAffordability_ShouldRecommendEnhancedPreApprovedLoan_WhenCustomerHasSurplusCapacity()
        {
            // Customer profile from POC description:
            // Income = ₹75,000, Expenses = ₹25,000, Existing EMI = ₹10,000
            var financialAnalysis = new FinancialAnalysisDto
            {
                MonthlyVerifiedIncome = 75000m,
                MonthlyExpenses = 25000m,
                ExistingEmis = 10000m,
                AverageBalance = 40000m,
                MonthlyDisposableIncome = 40000m
            };

            decimal requestedAmount = 500000m;
            int tenureMonths = 60;
            decimal annualRate = 12.0m;

            var result = _engine.EvaluateAffordability(requestedAmount, tenureMonths, annualRate, financialAnalysis, maxFoirPercentage: 40.0m);

            // 1. Requested loan should be approved
            Assert.True(result.IsRequestedLoanAffordable);
            Assert.Equal(11122.22m, result.RequestedEmi);
            Assert.Equal(21122.22m, result.TotalEmiWithExisting);

            // 2. Policy limit: 40% of 75,000 = 30,000
            Assert.Equal(30000m, result.MaxAllowedTotalEmi);

            // 3. Available capacity: 30,000 - 10,000 = 20,000
            Assert.Equal(20000m, result.AvailableEmiCapacity);

            // 4. Pre-approved offer should be higher than ₹5,00,000 (around ₹8.9 Lakhs)
            Assert.True(result.SuggestedPreApprovedAmount > requestedAmount);
            Assert.Equal(890000m, result.SuggestedPreApprovedAmount);
        }
    }

    public class FinancialAnalysisEngineTests
    {
        private readonly FinancialAnalysisEngine _engine = new();

        [Fact]
        public void Analyze_ShouldExtractCorrectMetrics_FromTestStatement()
        {
            var statement = new ExtractedStatementDto
            {
                AccountHolder = "Test User",
                AccountNumber = "XXXX1234",
                BankName = "Test Bank",
                OpeningBalance = 35000m,
                ClosingBalance = 80000m,
                Transactions = new List<ExtractedTransactionDto>
                {
                    new() { Date = "2026-01-01", Description = "SALARY CREDIT", Debit = 0m, Credit = 75000m, Balance = 110000m },
                    new() { Date = "2026-01-03", Description = "RENT PAYMENT", Debit = 15000m, Credit = 0m, Balance = 95000m },
                    new() { Date = "2026-01-05", Description = "PERSONAL LOAN EMI", Debit = 10000m, Credit = 0m, Balance = 85000m },
                    new() { Date = "2026-02-01", Description = "SALARY CREDIT", Debit = 0m, Credit = 75000m, Balance = 160000m },
                    new() { Date = "2026-02-05", Description = "PERSONAL LOAN EMI", Debit = 10000m, Credit = 0m, Balance = 150000m },
                    new() { Date = "2026-03-01", Description = "SALARY CREDIT", Debit = 0m, Credit = 75000m, Balance = 225000m },
                    new() { Date = "2026-03-05", Description = "PERSONAL LOAN EMI", Debit = 10000m, Credit = 0m, Balance = 215000m }
                }
            };

            var analysis = _engine.Analyze(statement);

            // Verified salary = ₹75,000
            Assert.Equal(75000m, analysis.MonthlyVerifiedIncome);
            // Existing personal loan EMI = ₹10,000/mo
            Assert.Equal(10000m, analysis.ExistingEmis);
            // Average balance positive
            Assert.True(analysis.AverageBalance > 0m);
            // Disposable income calculated
            Assert.True(analysis.MonthlyDisposableIncome > 0m);
        }
    }
}
