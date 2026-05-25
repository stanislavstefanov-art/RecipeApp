export interface ExpenseItemDto {
  id: string;
  description: string;
  quantity: number | null;
  unitPrice: number | null;
  totalPrice: number | null;
}

export interface ExpenseDto {
  id: string;
  amount: number;
  currency: string;
  expenseDate: string;
  category: number;
  description?: string;
  sourceType: number;
  items: ExpenseItemDto[];
}

export interface CreateExpenseItemRequest {
  description: string;
  quantity: number | null;
  unitPrice: number | null;
  totalPrice: number | null;
}

export interface CreateExpenseRequest {
  householdId: string;
  amount: number;
  currency: string;
  expenseDate: string;
  category: number;
  description?: string;
  sourceType: 1;
  items?: CreateExpenseItemRequest[];
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

export interface ExtractedReceiptItemDto {
  readonly description: string;
  readonly quantity: number | null;
  readonly unitPrice: number | null;
  readonly totalPrice: number | null;
}

export interface ExtractedReceiptDto {
  readonly amount: number | null;
  readonly currency: string | null;
  readonly date: string | null;
  readonly merchantName: string | null;
  readonly items: ExtractedReceiptItemDto[];
}
