import { z } from "zod";
import type { TFunction } from "i18next";

export const expenseSchema = z.object({
  id: z.string().uuid(),
  amount: z.number(),
  currency: z.string(),
  expenseDate: z.string(),
  category: z.number(),
  description: z.string(),
  sourceType: z.number(),
  sourceReferenceId: z.string().uuid().nullable().optional(),
});

export const monthlyExpenseCategoryBreakdownSchema = z.object({
  category: z.string(),
  amount: z.number(),
  percentage: z.number(),
});

export const monthlyExpenseLargestItemSchema = z.object({
  amount: z.number(),
  description: z.string(),
  expenseDate: z.string(),
  category: z.string(),
});

export const monthlyExpenseReportSchema = z.object({
  year: z.number(),
  month: z.number(),
  totalAmount: z.number(),
  currency: z.string(),
  expenseCount: z.number(),
  averageExpenseAmount: z.number(),
  topCategory: z.string().nullable().optional(),
  foodPercentage: z.number(),
  largestExpense: monthlyExpenseLargestItemSchema.nullable().optional(),
  categories: z.array(monthlyExpenseCategoryBreakdownSchema),
});

export const expenseInsightSchema = z.object({
  summary: z.string(),
  keyFindings: z.array(z.string()),
  recommendations: z.array(z.string()),
  confidence: z.number(),
  needsReview: z.boolean(),
  notes: z.string().nullable().optional(),
});

export const createExpenseSchema = (t: TFunction) =>
  z.object({
    amount: z.coerce.number().gt(0, t("validation.greaterThan", { min: 0 })),
    currency: z.string().min(1, t("validation.required")).max(10, t("validation.maxLength", { max: 10 })),
    expenseDate: z.string().min(1, t("validation.required")),
    category: z.coerce.number().int().min(1, t("validation.required")),
    description: z.string().min(1, t("validation.required")).max(500, t("validation.maxLength", { max: 500 })),
    sourceType: z.coerce.number().int().min(1),
    sourceReferenceId: z.string().uuid().nullable().optional(),
  });

export const monthlyExpenseQuerySchema = z.object({
  year: z.coerce.number().int().min(2000).max(3000),
  month: z.coerce.number().int().min(1).max(12),
});

export type Expense = z.infer<typeof expenseSchema>;
export type MonthlyExpenseReport = z.infer<typeof monthlyExpenseReportSchema>;
export type ExpenseInsight = z.infer<typeof expenseInsightSchema>;

export type CreateExpenseInput = z.input<ReturnType<typeof createExpenseSchema>>;
export type CreateExpenseData = z.output<ReturnType<typeof createExpenseSchema>>;

export type MonthlyExpenseQueryInput = z.input<typeof monthlyExpenseQuerySchema>;
export type MonthlyExpenseQueryData = z.output<typeof monthlyExpenseQuerySchema>;
