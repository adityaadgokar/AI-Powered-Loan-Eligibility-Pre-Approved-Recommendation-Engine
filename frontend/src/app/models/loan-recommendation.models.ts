export interface StatementPeriodDto {
  startDate?: string;
  endDate?: string;
}

export interface ExtractedTransactionDto {
  date: string;
  description: string;
  debit: number;
  credit: number;
  balance: number;
}

export interface ExtractedStatementDto {
  accountHolder: string;
  accountNumber: string;
  bankName?: string;
  statementPeriod?: StatementPeriodDto;
  openingBalance?: number;
  closingBalance?: number;
  transactions: ExtractedTransactionDto[];
}

export interface FinancialAnalysisDto {
  monthlyVerifiedIncome: number;
  monthlyExpenses: number;
  existingEmis: number;
  averageBalance: number;
  lowestBalance: number;
  monthlyDisposableIncome: number;
  netCashFlow: number;
  expenseRatio: number;
  existingEmiBurdenRatio: number;
  analyzedMonthsCount: number;
  detectedSalaryEntries: string[];
  detectedEmiEntries: string[];
}

export interface LoanAffordabilityResultDto {
  requestedAmount: number;
  tenureMonths: number;
  annualInterestRate: number;
  monthlyInterestRate: number;
  requestedEmi: number;
  existingEmis: number;
  totalEmiWithExisting: number;
  disposableIncome: number;
  remainingMonthlyIncomeAfterAllEmis: number;
  policyMaxFoirPercentage: number;
  maxAllowedTotalEmi: number;
  availableEmiCapacity: number;
  maxEligibleLoanAmount: number;
  suggestedPreApprovedAmount: number;
  isRequestedLoanAffordable: boolean;
  actualFoirPercentage: number;
  recommendationExplanation: string;
  eligibilityStatus: string;
}

export interface LoanRecommendationResponseDto {
  applicationId: number;
  customerId: number;
  customerName: string;
  accountNumber: string;
  bankName?: string;
  extractedStatement: ExtractedStatementDto;
  financialAnalysis: FinancialAnalysisDto;
  affordabilityResult: LoanAffordabilityResultDto;
}
