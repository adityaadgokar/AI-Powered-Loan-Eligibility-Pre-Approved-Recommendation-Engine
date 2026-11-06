import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ExtractedStatementDto, LoanRecommendationResponseDto } from '../models/loan-recommendation.models';

@Injectable({
  providedIn: 'root'
})
export class LoanService {
  private readonly baseUrl = `${environment.apiUrl}/loan`;

  constructor(private http: HttpClient) {}

  extractStatement(file: File): Observable<ExtractedStatementDto> {
    const formData = new FormData();
    formData.append('file', file, file.name);
    return this.http.post<ExtractedStatementDto>(`${this.baseUrl}/extract`, formData);
  }

  processStatement(formData: FormData): Observable<LoanRecommendationResponseDto> {
    return this.http.post<LoanRecommendationResponseDto>(`${this.baseUrl}/process`, formData);
  }
}
