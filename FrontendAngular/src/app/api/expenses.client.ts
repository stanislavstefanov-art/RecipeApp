import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import {
  CreateExpenseRequest,
  CreateExpenseResponse,
  ExpenseDto,
  ExpenseInsightDto,
  MonthlyExpenseReportDto,
} from './expenses.dto';

const API_BASE_URL = 'http://localhost:5117';

@Injectable({ providedIn: 'root' })
export class ExpensesClient {
  private readonly http = inject(HttpClient);

  list(): Observable<ExpenseDto[]> {
    return this.http.get<ExpenseDto[]>(`${API_BASE_URL}/api/expenses`);
  }

  create(payload: CreateExpenseRequest): Observable<CreateExpenseResponse> {
    return this.http.post<CreateExpenseResponse>(`${API_BASE_URL}/api/expenses`, payload);
  }

  monthlyReport(year: number, month: number): Observable<MonthlyExpenseReportDto> {
    return this.http.get<MonthlyExpenseReportDto>(
      `${API_BASE_URL}/api/expenses/monthly-report?year=${year}&month=${month}`,
    );
  }

  insights(year: number, month: number): Observable<ExpenseInsightDto> {
    return this.http.get<ExpenseInsightDto>(
      `${API_BASE_URL}/api/expenses/insights?year=${year}&month=${month}`,
    );
  }
}
