import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { LoanService } from '../../services/loan.service';

export interface LoanFormSubmitData {
  file: File | null;
  customerName: string;
  accountNumber: string;
  requestedLoanAmount: number;
  tenureMonths: number;
  annualInterestRate: number;
}

@Component({
  selector: 'app-loan-form',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './loan-form.component.html',
  styleUrls: ['./loan-form.component.css']
})
export class LoanFormComponent {
  @Input() isLoading: boolean = false;
  @Input() progressPercent: number = 0;
  @Input() progressStage: string = '';
  @Input() errorMessage: string = '';

  @Input() set extractedCustomerName(name: string | undefined) {
    if (name) {
      this.customerName = name;
    }
  }

  @Input() set extractedAccountNumber(accNo: string | undefined) {
    if (accNo) {
      this.accountNumber = accNo;
    }
  }

  @Output() formSubmit = new EventEmitter<LoanFormSubmitData>();

  customerName: string = '';
  accountNumber: string = '';
  requestedLoanAmount: number | null = null;
  tenureMonths: number | null = null;
  annualInterestRate: number | null = null;

  selectedFile: File | null = null;
  selectedFileName: string = '';
  validationMessage: string = '';
  isSubmitted: boolean = false;
  fieldErrors: { [key: string]: string } = {};

  isExtracting: boolean = false;
  extractedAlert: string = '';
  private extractSub?: Subscription;

  constructor(private loanService: LoanService) {}

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.handleFileSelected(input.files[0]);
    }
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    if (event.dataTransfer?.files && event.dataTransfer.files.length > 0) {
      this.handleFileSelected(event.dataTransfer.files[0]);
    }
  }

  private handleFileSelected(file: File): void {
    this.selectedFile = file;
    this.selectedFileName = file.name;
    this.clearFieldError('file');
    this.extractedAlert = '';

    // Automatically read and prefill customer name & account number from statement
    this.readStatementDetails(file);

    if (this.isSubmitted) {
      this.validateForm();
    }
  }

  private readStatementDetails(file: File): void {
    this.isExtracting = true;
    this.extractSub?.unsubscribe();
    this.extractSub = this.loanService.extractStatement(file).subscribe({
      next: (statement) => {
        this.isExtracting = false;
        let detailsFound: string[] = [];

        if (statement.accountHolder) {
          this.customerName = statement.accountHolder;
          detailsFound.push(`Customer: "${statement.accountHolder}"`);
        }
        if (statement.accountNumber) {
          this.accountNumber = statement.accountNumber;
          detailsFound.push(`Account No: "${statement.accountNumber}"`);
        }

        if (detailsFound.length > 0) {
          this.extractedAlert = `Successfully read and prefilled ${detailsFound.join(' and ')} from statement.`;
        }
      },
      error: (err) => {
        this.isExtracting = false;
        console.warn('Initial statement read fallback: Details will be processed during full analysis.', err);
      }
    });
  }

  onFieldChange(fieldName: string): void {
    if (this.isSubmitted) {
      this.validateForm();
    } else {
      this.clearFieldError(fieldName);
    }
  }

  clearFieldError(fieldName: string): void {
    if (this.fieldErrors[fieldName]) {
      delete this.fieldErrors[fieldName];
    }
    if (Object.keys(this.fieldErrors).length === 0) {
      this.validationMessage = '';
    }
  }

  validateForm(): boolean {
    this.fieldErrors = {};

    // 1. Bank Statement Document (Required)
    if (!this.selectedFile) {
      this.fieldErrors['file'] = 'Bank statement document is required. Please upload a PDF or image.';
    } else {
      const allowedExtensions = ['.pdf', '.png', '.jpg', '.jpeg'];
      const fileNameLower = this.selectedFile.name.toLowerCase();
      const hasValidExt = allowedExtensions.some(ext => fileNameLower.endsWith(ext));
      if (!hasValidExt) {
        this.fieldErrors['file'] = 'Invalid file type. Please upload a PDF, PNG, or JPG file.';
      } else if (this.selectedFile.size === 0) {
        this.fieldErrors['file'] = 'The selected file is empty. Please upload a valid document.';
      } else if (this.selectedFile.size > 25 * 1024 * 1024) {
        this.fieldErrors['file'] = 'File size exceeds 25 MB limit. Please upload a smaller document.';
      }
    }

    // 2. Requested Loan Amount (Required)
    const loanAmountVal = this.requestedLoanAmount;
    if (loanAmountVal === null || loanAmountVal === undefined || loanAmountVal === ('' as unknown as number) || isNaN(Number(loanAmountVal))) {
      this.fieldErrors['requestedLoanAmount'] = 'Requested Loan Amount is required.';
    } else if (Number(loanAmountVal) <= 0) {
      this.fieldErrors['requestedLoanAmount'] = 'Loan amount must be greater than ₹0.';
    } else if (Number(loanAmountVal) < 10000) {
      this.fieldErrors['requestedLoanAmount'] = 'Minimum loan amount is ₹10,000.';
    } else if (Number(loanAmountVal) > 100000000) {
      this.fieldErrors['requestedLoanAmount'] = 'Maximum loan amount cannot exceed ₹10,00,00,000.';
    }

    // 3. Tenure in Months (Required)
    const tenureVal = this.tenureMonths;
    if (tenureVal === null || tenureVal === undefined || tenureVal === ('' as unknown as number) || isNaN(Number(tenureVal))) {
      this.fieldErrors['tenureMonths'] = 'Tenure (Months) is required.';
    } else if (Number(tenureVal) <= 0 || !Number.isInteger(Number(tenureVal))) {
      this.fieldErrors['tenureMonths'] = 'Tenure must be a positive whole number.';
    } else if (Number(tenureVal) < 6) {
      this.fieldErrors['tenureMonths'] = 'Minimum tenure is 6 months.';
    } else if (Number(tenureVal) > 360) {
      this.fieldErrors['tenureMonths'] = 'Maximum tenure cannot exceed 360 months (30 years).';
    }

    // 4. Annual Interest Rate (% p.a.) (Required)
    const rateVal = this.annualInterestRate;
    if (rateVal === null || rateVal === undefined || rateVal === ('' as unknown as number) || isNaN(Number(rateVal))) {
      this.fieldErrors['annualInterestRate'] = 'Interest Rate (% p.a.) is required.';
    } else if (Number(rateVal) <= 0) {
      this.fieldErrors['annualInterestRate'] = 'Interest rate must be greater than 0% p.a.';
    } else if (Number(rateVal) < 1.0) {
      this.fieldErrors['annualInterestRate'] = 'Minimum interest rate is 1.0% p.a.';
    } else if (Number(rateVal) > 50.0) {
      this.fieldErrors['annualInterestRate'] = 'Interest rate cannot exceed 50.0% p.a.';
    }

    const errorCount = Object.keys(this.fieldErrors).length;
    if (errorCount > 0) {
      this.validationMessage = `Please complete all required fields correctly (${errorCount} field${errorCount > 1 ? 's' : ''} require attention).`;
      return false;
    }

    this.validationMessage = '';
    return true;
  }

  onReset(): void {
    this.extractSub?.unsubscribe();
    this.selectedFile = null;
    this.selectedFileName = '';
    this.customerName = '';
    this.accountNumber = '';
    this.requestedLoanAmount = null;
    this.tenureMonths = null;
    this.annualInterestRate = null;
    this.validationMessage = '';
    this.isSubmitted = false;
    this.fieldErrors = {};
    this.isExtracting = false;
    this.extractedAlert = '';
  }

  onSubmit(): void {
    this.isSubmitted = true;
    if (!this.validateForm()) {
      return;
    }

    // Cancel in-flight quick-extract if still active so it doesn't collide with the full evaluation call
    if (this.isExtracting && this.extractSub) {
      this.extractSub.unsubscribe();
      this.isExtracting = false;
    }

    this.formSubmit.emit({
      file: this.selectedFile,
      customerName: this.customerName.trim(),
      accountNumber: this.accountNumber.trim(),
      requestedLoanAmount: Number(this.requestedLoanAmount),
      tenureMonths: Number(this.tenureMonths),
      annualInterestRate: Number(this.annualInterestRate)
    });
  }
}
