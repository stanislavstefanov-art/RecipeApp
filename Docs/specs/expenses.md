# Spec: expenses (list, create, monthly report with AI insights)

Framework-agnostic specification for expense tracking.
Both `/Frontend` (React) and `/FrontendAngular` (Angular) must satisfy this spec.

## Endpoints

### List expenses
- Method: `GET /api/expenses`
- Definition: `Backend/src/Recipes.Api/Endpoints/ExpensesEndpoints.cs:33-37`
- Response `200 OK`: array of `{ id, amount, currency, expenseDate, category, description, sourceType, sourceReferenceId? }`

### Create expense
- Method: `POST /api/expenses`
- Definition: `Backend/src/Recipes.Api/Endpoints/ExpensesEndpoints.cs:17-31`
- Request: `{ amount: number; currency: string; expenseDate: string; category: number; description: string; sourceType: number; sourceReferenceId?: string | null }`
  - amount > 0; currency max 10; category 1–6; description max 500; sourceType default 1 (Manual); sourceReferenceId omitted for manual entries
- Response `201 Created`: `{ id, amount, currency, expenseDate, category, description }`

### Monthly expense report
- Method: `GET /api/expenses/monthly-report?year={year}&month={month}`
- Definition: `Backend/src/Recipes.Api/Endpoints/ExpensesEndpoints.cs:39-43`
- Response `200 OK`:
  ```
  {
    year: number; month: number
    totalAmount: number; currency: string; expenseCount: number
    averageExpenseAmount: number; topCategory?: string
    foodPercentage: number
    largestExpense?: { amount: number; description: string; expenseDate: string; category: string } | null
    categories: Array<{ category: string; amount: number; percentage: number }>
  }
  ```

### Expense insights (AI)
- Method: `GET /api/expenses/insights?year={year}&month={month}`
- Definition: `Backend/src/Recipes.Api/Endpoints/ExpensesEndpoints.cs:45-49`
- Response `200 OK`:
  ```
  { summary: string; keyFindings: string[]; recommendations: string[]; confidence: number; needsReview: boolean; notes?: string }
  ```

## User-visible behavior

- The list page (`/expenses`) shows all expenses. Each entry shows description, category label, source type label, date (formatted), and amount with currency. Empty state: "No expenses yet." Loading and error states required.
- The list page has an inline **Create expense** form with fields:
  - **Amount** (number, step 0.01, required > 0)
  - **Currency** (text, required, default "BGN")
  - **Expense date** (date, required, default today)
  - **Category** (select, required): Food, Transport, Utilities, Entertainment, Health, Other
  - **Description** (textarea, required, max 500 chars)
  - `sourceType` sent as `1` (Manual); `sourceReferenceId` omitted.
  - On `201`, list refreshes and form resets to defaults.
  - Inline validation errors for each required field.
- The report page (`/expenses/report`) has a year/month query form (defaults to current year and month). Submitting the form fetches both the monthly report and insights for that period.
- The report shows summary cards: total amount, expense count, average, top category, food percentage, largest expense.
- Below the summary, a category breakdown lists each category with amount and percentage.
- An **Insights** panel shows the AI-generated `summary`, `keyFindings` list, `recommendations` list, and optional `notes`. Shows `needsReview` advisory if true. Insights load independently from the report — each has its own loading/error state.
- The list page has a **View report** link to `/expenses/report`.
- The report page has a back link to `/expenses`.

## Enum labels (hard-coded)

Category: 1=Food, 2=Transport, 3=Utilities, 4=Entertainment, 5=Health, 6=Other  
Source type: 1=Manual, 2=Shopping list item, 3=Meal plan

## Acceptance checklist

- [ ] `/expenses` shows expenses list with description, category, date, amount, or empty state.
- [ ] Create form: blank/invalid fields show inline errors and issue no request.
- [ ] On `201`, list refreshes and form resets.
- [ ] "View report" navigates to `/expenses/report`.
- [ ] `/expenses/report` defaults to current year/month and loads report + insights.
- [ ] Report shows summary cards, category breakdown.
- [ ] Insights panel shows summary, key findings, recommendations.
- [ ] `needsReview: true` shows advisory in insights panel.
- [ ] Changing year/month and submitting reloads both report and insights.

## Out of scope

- Editing or deleting expenses.
- Filtering/searching the expense list.

## Parity reference

React implementation: `Frontend/src/pages/expenses/ExpensesPage.tsx`, `Frontend/src/pages/expenses/ExpenseReportPage.tsx`
