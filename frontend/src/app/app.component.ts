import { Component, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LoanService } from './services/loan.service';
import { LoanRecommendationResponseDto } from './models/loan-recommendation.models';
import { LoanFormComponent, LoanFormSubmitData } from './components/loan-form/loan-form.component';
import { HeroRecommendationComponent } from './components/hero-recommendation/hero-recommendation.component';
import { FinancialMetricsComponent } from './components/financial-metrics/financial-metrics.component';
import { TransactionsTableComponent } from './components/transactions-table/transactions-table.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    LoanFormComponent,
    HeroRecommendationComponent,
    FinancialMetricsComponent,
    TransactionsTableComponent
  ],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnDestroy {
  title = 'AI-Powered Loan Recommendation Engine';

  isLoading: boolean = false;
  progressPercent: number = 0;
  progressStage: string = '';
  errorMessage: string = '';
  result: LoanRecommendationResponseDto | null = null;

  private progressInterval: ReturnType<typeof setInterval> | null = null;

  constructor(private loanService: LoanService) {}

  ngOnDestroy(): void {
    this.stopProgressInterval();
  }

  handleFormSubmit(data: LoanFormSubmitData): void {
    if (!data.file) {
      this.errorMessage = 'Please select a bank statement PDF or image to process.';
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    this.result = null;

    this.startProgress();

    const formData = new FormData();
    formData.append('file', data.file, data.file.name);
    if (data.customerName) formData.append('customerName', data.customerName);
    if (data.accountNumber) formData.append('accountNumber', data.accountNumber);
    formData.append('requestedLoanAmount', data.requestedLoanAmount.toString());
    formData.append('tenureMonths', data.tenureMonths.toString());
    formData.append('annualInterestRate', data.annualInterestRate.toString());

    this.loanService.processStatement(formData).subscribe({
      next: (response) => {
        this.finishProgress(() => {
          this.result = response;
          this.isLoading = false;
        });
      },
      error: (err) => {
        console.error('Error processing statement:', err);
        this.stopProgressInterval();
        this.progressPercent = 0;
        this.progressStage = '';
        this.errorMessage = err.error?.message || err.message || 'Failed to process statement. Please check your backend connection and Gemini API configuration.';
        this.isLoading = false;
      }
    });
  }

  private startProgress(): void {
    this.stopProgressInterval();
    this.progressPercent = 5;
    this.progressStage = 'Uploading bank statement document...';

    this.progressInterval = setInterval(() => {
      if (this.progressPercent < 20) {
        this.progressPercent += 3;
        this.progressStage = 'Uploading bank statement document...';
      } else if (this.progressPercent < 55) {
        this.progressPercent += 2;
        this.progressStage = 'Extracting statement transactions & financial details...';
      } else if (this.progressPercent < 75) {
        this.progressPercent += 1;
        this.progressStage = 'Analyzing monthly income, recurring salary & living expenses...';
      } else if (this.progressPercent < 88) {
        this.progressPercent += 1;
        this.progressStage = 'Auditing existing loan EMIs & account balances...';
      } else if (this.progressPercent < 96) {
        this.progressPercent += 1;
        this.progressStage = 'Calculating 40% FOIR policy & Pre-Approved loan eligibility...';
      } else if (this.progressPercent < 98) {
        this.progressPercent = 98;
        this.progressStage = 'Finalizing recommendation & saving audit records...';
      }
    }, 180);
  }

  private finishProgress(onComplete: () => void): void {
    this.stopProgressInterval();
    this.progressPercent = 100;
    this.progressStage = 'Analysis Complete! 100%';
    setTimeout(() => {
      onComplete();
    }, 400);
  }

  private stopProgressInterval(): void {
    if (this.progressInterval) {
      clearInterval(this.progressInterval);
      this.progressInterval = null;
    }
  }
}
