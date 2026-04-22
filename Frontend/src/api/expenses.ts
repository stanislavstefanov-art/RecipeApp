import { apiClient } from "./client";
import {
  expenseInsightSchema,
  expenseSchema,
  monthlyExpenseReportSchema,
  type CreateExpenseInput,
} from "../features/expenses/schemas";

export async function getExpenses() {
  const res = await apiClient.get("/api/expenses");
  return expenseSchema.array().parse(res.data);
}

export async function createExpense(input: CreateExpenseInput) {
  const payload = {
    ...input,
    sourceReferenceId: input.sourceReferenceId || null,
  };

  const res = await apiClient.post("/api/expenses", payload);
  return expenseSchema.parse(res.data);
}

export async function getMonthlyExpenseReport(year: number, month: number) {
  const res = await apiClient.get(`/api/expenses/monthly-report?year=${year}&month=${month}`);
  return monthlyExpenseReportSchema.parse(res.data);
}

export async function getExpenseInsights(year: number, month: number) {
  const res = await apiClient.get(`/api/expenses/insights?year=${year}&month=${month}`);
  return expenseInsightSchema.parse(res.data);
}