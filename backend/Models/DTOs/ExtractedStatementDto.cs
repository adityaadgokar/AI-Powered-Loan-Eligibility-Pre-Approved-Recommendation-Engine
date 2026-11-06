using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LoanEngine.Api.Models.DTOs
{
    public class ExtractedStatementDto
    {
        [JsonPropertyName("accountHolder")]
        public string AccountHolder { get; set; } = string.Empty;

        [JsonPropertyName("accountNumber")]
        public string AccountNumber { get; set; } = string.Empty;

        [JsonPropertyName("bankName")]
        public string? BankName { get; set; }

        [JsonPropertyName("statementPeriod")]
        public StatementPeriodDto? StatementPeriod { get; set; }

        [JsonPropertyName("openingBalance")]
        public decimal? OpeningBalance { get; set; }

        [JsonPropertyName("closingBalance")]
        public decimal? ClosingBalance { get; set; }

        [JsonPropertyName("transactions")]
        public List<ExtractedTransactionDto> Transactions { get; set; } = new();
    }

    public class StatementPeriodDto
    {
        [JsonPropertyName("startDate")]
        public string? StartDate { get; set; }

        [JsonPropertyName("endDate")]
        public string? EndDate { get; set; }
    }

    public class ExtractedTransactionDto
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
    }
}
