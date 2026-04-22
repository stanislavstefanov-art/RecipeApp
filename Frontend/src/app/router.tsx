import { createBrowserRouter } from "react-router-dom";
import { AppLayout } from "../components/layout/AppLayout";
import { HomePage } from "../pages/HomePage";
import { RecipesPage } from "../pages/recipes/RecipesPage";
import { CreateRecipePage } from "../pages/recipes/CreateRecipePage";
import { RecipeDetailsPage } from "../pages/recipes/RecipeDetailsPage";
import { PersonsPage } from "../pages/persons/PersonsPage";
import { PersonDetailsPage } from "../pages/persons/PersonDetailsPage";
import { HouseholdsPage } from "../pages/households/HouseholdsPage";
import { HouseholdDetailsPage } from "../pages/households/HouseholdDetailsPage";
import { MealPlansPage } from "../pages/mealPlans/MealPlansPage";
import { MealPlanDetailsPage } from "../pages/mealPlans/MealPlanDetailsPage";
import { SuggestMealPlanPage } from "../pages/mealPlans/SuggestMealPlanPage";
import { MealPlanSuggestionReviewPage } from "../pages/mealPlans/MealPlanSuggestionReviewPage";
import { ShoppingListsPage } from "../pages/shoppingLists/ShoppingListsPage";
import { ShoppingListDetailsPage } from "../pages/shoppingLists/ShoppingListDetailsPage";
import { ExpensesPage } from "../pages/expenses/ExpensesPage";
import { ExpenseReportPage } from "../pages/expenses/ExpenseReportPage";

export const router = createBrowserRouter([
  {
    path: "/",
    element: <AppLayout />,
    children: [
      { index: true, element: <HomePage /> },
      { path: "recipes", element: <RecipesPage /> },
      { path: "recipes/new", element: <CreateRecipePage /> },
      { path: "recipes/:recipeId", element: <RecipeDetailsPage /> },
      { path: "persons", element: <PersonsPage /> },
      { path: "persons/:personId", element: <PersonDetailsPage /> },
      { path: "households", element: <HouseholdsPage /> },
      { path: "households/:householdId", element: <HouseholdDetailsPage /> },
      { path: "meal-plans", element: <MealPlansPage /> },
      { path: "meal-plans/suggest", element: <SuggestMealPlanPage /> },
      { path: "meal-plans/suggest/review", element: <MealPlanSuggestionReviewPage /> },
      { path: "meal-plans/:mealPlanId", element: <MealPlanDetailsPage /> },
      { path: "shopping-lists", element: <ShoppingListsPage /> },
      { path: "shopping-lists/:shoppingListId", element: <ShoppingListDetailsPage /> },
      { path: "expenses", element: <ExpensesPage /> },
      { path: "expenses/report", element: <ExpenseReportPage /> },
    ],
  },
]);