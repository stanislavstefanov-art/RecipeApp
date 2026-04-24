export interface ExpenseDto {
  id: string;
  amount: number;
  currency: string;
  expenseDate: string;
  category: number;
  description?: string;
  sourceType: number;
}

export interface CreateExpenseRequest {
  amount: number;
  currency: string;
  expenseDate: string;
  category: number;
  description?: string;
  sourceType: 1;
}

export interface CreateExpenseResponse {
  id: string;
}

export interface MonthlyExpenseReportDto {
  year: number;
  month: number;
  totalAmount: number;
  currency: string;
  byCategory: MonthlyExpenseByCategoryDto[];
}

export interface MonthlyExpenseByCategoryDto {
  category: number;
  totalAmount: number;
  count: number;
}

export interface ExpenseInsightDto {
  year: number;
  month: number;
  insights: string[];
}
