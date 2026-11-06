import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LoanAffordabilityResultDto } from '../../models/loan-recommendation.models';

@Component({
  selector: 'app-hero-recommendation',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './hero-recommendation.component.html',
  styleUrls: ['./hero-recommendation.component.css']
})
export class HeroRecommendationComponent {
  @Input() affordabilityResult!: LoanAffordabilityResultDto;

  get isGreaterOrEqual(): boolean {
    if (!this.affordabilityResult) return false;
    return (this.affordabilityResult.suggestedPreApprovedAmount || 0) >= (this.affordabilityResult.requestedAmount || 0);
  }

  get amountDifference(): number {
    if (!this.affordabilityResult) return 0;
    return (this.affordabilityResult.suggestedPreApprovedAmount || 0) - (this.affordabilityResult.requestedAmount || 0);
  }
}
