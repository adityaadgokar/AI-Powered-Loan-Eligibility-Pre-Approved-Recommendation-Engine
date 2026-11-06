import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FinancialAnalysisDto } from '../../models/loan-recommendation.models';

@Component({
  selector: 'app-financial-metrics',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './financial-metrics.component.html',
  styleUrls: ['./financial-metrics.component.css']
})
export class FinancialMetricsComponent {
  @Input() financialAnalysis!: FinancialAnalysisDto;
}
