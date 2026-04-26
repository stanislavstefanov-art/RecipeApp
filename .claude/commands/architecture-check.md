Check the diff `git diff origin/$BASE...HEAD` against the four
RecipesApp architecture invariants. Report only violations of these
exact rules — ignore style.

## Rules

1. **No IRecipesDbContext in Application layer.** Any file under
   `Backend/src/Recipes.Application/**` that imports or references
   `IRecipesDbContext` (or `RecipesDbContext`) is a violation. Handlers
   must depend on repository interfaces (`IRecipeRepository`,
   `IMealPlanRepository`, etc.).

2. **No cross-aggregate manipulation in Application.** `Ingredient` and
   `RecipeStep` are entities owned by the `Recipe` aggregate root.
   Application-layer code may read them via a `Recipe` loaded from
   `IRecipeRepository`, but must not query, project, or mutate them
   without going through the aggregate.

3. **Commands have validators (when they accept user input).** Every
   new `Backend/src/Recipes.Application/**/*Command.cs` whose record
   has at least one parameter likely supplied by an HTTP caller (Guid,
   string, int, complex DTO) must have a sibling `*Validator.cs` in
   the same folder. Queries that accept only an id may skip the
   validator.

4. **AI-using slices have a CCAF doc.** Any new file under
   `Backend/src/Recipes.Application/Recipes/**` that injects a Claude
   service interface (`IRecipeCritiqueService`, `IRecipeScalingService`,
   `IRecipeBatchAnalysisService`, `IRecipeDraftReviewService`,
   `IClaudeRecipeImportClient`, etc.) must have an accompanying
   `Backend/Docs/CCAF/<id>-*.md` entry added in the same PR.

## Output

For each finding output a line with this exact format:
  FINDING|<file>|<line>|<rule number>|<one sentence description>

After all findings, emit a single line:
  GUARD_RESULT: {"violations": <N>}

where N is the integer count of findings (0 if none).
