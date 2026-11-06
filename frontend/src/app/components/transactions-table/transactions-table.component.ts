import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ExtractedStatementDto, ExtractedTransactionDto } from '../../models/loan-recommendation.models';

@Component({
  selector: 'app-transactions-table',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './transactions-table.component.html',
  styleUrls: ['./transactions-table.component.css']
})
export class TransactionsTableComponent {
  @Input() extractedStatement!: ExtractedStatementDto;
  @Input() accountNumber: string = '';
  @Input() bankName?: string = '';

  transactionSearch: string = '';

  get filteredTransactions(): ExtractedTransactionDto[] {
    if (!this.extractedStatement?.transactions) return [];
    if (!this.transactionSearch.trim()) return this.extractedStatement.transactions;
    const query = this.transactionSearch.toLowerCase();
    return this.extractedStatement.transactions.filter(t =>
      t.description.toLowerCase().includes(query) ||
      t.date.includes(query) ||
      t.credit.toString().includes(query) ||
      t.debit.toString().includes(query)
    );
  }
}
