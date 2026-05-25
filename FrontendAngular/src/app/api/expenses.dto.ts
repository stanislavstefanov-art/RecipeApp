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
  householdId: string;
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
  expenseCount: number;
  averageExpenseAmount: number;
  topCategory: number | null;
  byCategory: MonthlyExpenseByCategoryDto[];
}

export interface MonthlyExpenseByCategoryDto {
  category: number;
  totalAmount: number;
  count: number;
  percentage: number;
}

export interface ExpenseInsightDto {
  summary: string;
  keyFindings: string[];
  recommendations: string[];
  confidence: number;
  needsReview: boolean;
  notes: string | null;
}

export interface ExtractedReceiptDto {
  readonly amount: number | null;
  readonly currency: string | null;
  readonly date: string | null;
  readonly merchantName: string | null;
}
