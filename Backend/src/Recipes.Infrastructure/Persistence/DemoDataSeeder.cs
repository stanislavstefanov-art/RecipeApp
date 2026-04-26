using Recipes.Domain.Entities;
using Recipes.Domain.Enums;

namespace Recipes.Infrastructure.Persistence;

public sealed class DemoDataSeeder
{
    private readonly RecipesDbContext _db;

    public DemoDataSeeder(RecipesDbContext db)
    {
        _db = db;
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var stan      = new Person("Stanislav", [DietaryPreference.HighProtein], [], "Prefers high-protein meals; avoids overly sweet desserts.");
        var elena     = new Person("Elena", [DietaryPreference.Pescatarian], [HealthConcern.HighBloodPressure], "Watching sodium; loves Mediterranean cuisine.");
        var milo      = new Person("Milo", [], [], "Picky eater; loves pasta and bread.");
        var alex      = new Person("Alex", [DietaryPreference.Vegetarian], [HealthConcern.GlutenIntolerance], "No gluten; prefers hearty plant-based meals.");
        var jordan    = new Person("Jordan", [], [HealthConcern.Diabetes], "Diabetic; low refined-sugar.");

        _db.Persons.AddRange(stan, elena, milo, alex, jordan);

        var stefanovs = new Household("The Stefanovs");
        stefanovs.AddMember(stan);
        stefanovs.AddMember(elena);
        stefanovs.AddMember(milo);

        var roommates = new Household("Roommates");
        roommates.AddMember(alex);
        roommates.AddMember(jordan);

        _db.Households.AddRange(stefanovs, roommates);

        var carbonara = new Recipe("Pasta Carbonara");
        carbonara.AddIngredient("spaghetti", 400m, "g");
        carbonara.AddIngredient("guanciale", 150m, "g");
        carbonara.AddIngredient("egg yolks", 4m, "pcs");
        carbonara.AddIngredient("pecorino romano", 80m, "g");
        carbonara.AddIngredient("black pepper", 1m, "tsp");
        carbonara.AddStep("Bring a large pot of salted water to a boil and cook the spaghetti until al dente.");
        carbonara.AddStep("Render the guanciale in a dry pan over medium heat until crisp.");
        carbonara.AddStep("Whisk egg yolks with grated pecorino and a generous grind of black pepper.");
        carbonara.AddStep("Drain the pasta, reserving a cup of cooking water.");
        carbonara.AddStep("Off the heat, toss pasta with the guanciale, then add the egg-cheese mixture and a splash of pasta water until silky.");
        carbonara.AddVariation("vegetarian", "Replace guanciale with sautéed cremini mushrooms.", "Use 200 g cremini mushrooms in place of guanciale.");

        var greekSalad = new Recipe("Greek Salad");
        greekSalad.AddIngredient("cucumber", 1m, "pcs");
        greekSalad.AddIngredient("tomato", 3m, "pcs");
        greekSalad.AddIngredient("red onion", 0.5m, "pcs");
        greekSalad.AddIngredient("kalamata olives", 80m, "g");
        greekSalad.AddIngredient("feta", 150m, "g");
        greekSalad.AddIngredient("olive oil", 3m, "tbsp");
        greekSalad.AddIngredient("oregano", 1m, "tsp");
        greekSalad.AddStep("Chop cucumber, tomato and red onion into bite-sized pieces.");
        greekSalad.AddStep("Combine in a bowl with olives and crumble feta over the top.");
        greekSalad.AddStep("Dress with olive oil and oregano. Toss gently before serving.");

        var chickenCurry = new Recipe("Chicken Curry");
        chickenCurry.AddIngredient("chicken thighs", 600m, "g");
        chickenCurry.AddIngredient("onion", 1m, "pcs");
        chickenCurry.AddIngredient("garlic", 4m, "cloves");
        chickenCurry.AddIngredient("ginger", 20m, "g");
        chickenCurry.AddIngredient("curry powder", 2m, "tbsp");
        chickenCurry.AddIngredient("coconut milk", 400m, "ml");
        chickenCurry.AddIngredient("rice", 300m, "g");
        chickenCurry.AddStep("Brown the chicken thighs in a deep pan, then set aside.");
        chickenCurry.AddStep("Sauté chopped onion, garlic and ginger until fragrant.");
        chickenCurry.AddStep("Stir in curry powder and toast for 30 seconds.");
        chickenCurry.AddStep("Return chicken to the pan, pour in coconut milk and simmer for 25 minutes.");
        chickenCurry.AddStep("Serve over freshly cooked rice.");

        var bananaBread = new Recipe("Banana Bread");
        bananaBread.AddIngredient("ripe banana", 3m, "pcs");
        bananaBread.AddIngredient("butter", 100m, "g");
        bananaBread.AddIngredient("sugar", 150m, "g");
        bananaBread.AddIngredient("egg", 2m, "pcs");
        bananaBread.AddIngredient("flour", 250m, "g");
        bananaBread.AddIngredient("baking soda", 1m, "tsp");
        bananaBread.AddStep("Preheat oven to 175°C and line a loaf tin.");
        bananaBread.AddStep("Mash bananas in a large bowl.");
        bananaBread.AddStep("Whisk in melted butter, sugar and eggs.");
        bananaBread.AddStep("Fold flour and baking soda into the wet mixture.");
        bananaBread.AddStep("Pour into the tin and bake for 50–60 minutes until a skewer comes out clean.");

        var trayBake = new Recipe("Mediterranean Tray Bake");
        trayBake.AddIngredient("chicken drumsticks", 8m, "pcs");
        trayBake.AddIngredient("baby potatoes", 500m, "g");
        trayBake.AddIngredient("cherry tomatoes", 250m, "g");
        trayBake.AddIngredient("red bell pepper", 2m, "pcs");
        trayBake.AddIngredient("olive oil", 4m, "tbsp");
        trayBake.AddIngredient("oregano", 1m, "tbsp");
        trayBake.AddStep("Heat oven to 200°C.");
        trayBake.AddStep("Toss potatoes and peppers with olive oil and oregano on a sheet pan.");
        trayBake.AddStep("Nestle drumsticks among the vegetables.");
        trayBake.AddStep("Roast for 35 minutes; add tomatoes and roast 10 minutes more until everything is golden.");

        var lentilSoup = new Recipe("Lentil Soup");
        lentilSoup.AddIngredient("brown lentils", 250m, "g");
        lentilSoup.AddIngredient("carrot", 2m, "pcs");
        lentilSoup.AddIngredient("celery", 2m, "stalks");
        lentilSoup.AddIngredient("onion", 1m, "pcs");
        lentilSoup.AddIngredient("vegetable stock", 1.2m, "L");
        lentilSoup.AddIngredient("cumin", 1m, "tsp");
        lentilSoup.AddStep("Dice carrot, celery and onion; sweat in olive oil for 5 minutes.");
        lentilSoup.AddStep("Stir in cumin and lentils.");
        lentilSoup.AddStep("Pour in stock, bring to a boil, then simmer 30 minutes until lentils are tender.");
        lentilSoup.AddStep("Season to taste and finish with a squeeze of lemon.");

        var beefStew = new Recipe("Beef Stew");
        beefStew.AddIngredient("beef chuck", 800m, "g");
        beefStew.AddIngredient("potato", 4m, "pcs");
        beefStew.AddIngredient("carrot", 3m, "pcs");
        beefStew.AddIngredient("onion", 1m, "pcs");
        beefStew.AddIngredient("tomato paste", 2m, "tbsp");
        beefStew.AddIngredient("beef stock", 1m, "L");
        beefStew.AddStep("Sear cubed beef in a heavy pot until browned on all sides.");
        beefStew.AddStep("Remove beef; sauté onion, carrot and potato chunks.");
        beefStew.AddStep("Stir in tomato paste, return beef and pour in stock.");
        beefStew.AddStep("Cover and simmer for 90 minutes until beef is fork-tender.");

        var stirFry = new Recipe("Vegetable Stir-Fry");
        stirFry.AddIngredient("broccoli florets", 300m, "g");
        stirFry.AddIngredient("bell pepper", 1m, "pcs");
        stirFry.AddIngredient("carrot", 1m, "pcs");
        stirFry.AddIngredient("garlic", 3m, "cloves");
        stirFry.AddIngredient("soy sauce", 3m, "tbsp");
        stirFry.AddIngredient("sesame oil", 1m, "tbsp");
        stirFry.AddStep("Heat a wok over high heat with a splash of neutral oil.");
        stirFry.AddStep("Stir-fry garlic for 15 seconds.");
        stirFry.AddStep("Add carrot and broccoli; toss for 3 minutes.");
        stirFry.AddStep("Add bell pepper, soy sauce and sesame oil; toss 1 minute more and serve.");

        var risotto = new Recipe("Mushroom Risotto");
        risotto.AddIngredient("arborio rice", 320m, "g");
        risotto.AddIngredient("cremini mushrooms", 300m, "g");
        risotto.AddIngredient("onion", 1m, "pcs");
        risotto.AddIngredient("white wine", 150m, "ml");
        risotto.AddIngredient("vegetable stock", 1m, "L");
        risotto.AddIngredient("parmesan", 60m, "g");
        risotto.AddIngredient("butter", 30m, "g");
        risotto.AddStep("Sauté finely chopped onion in butter until translucent.");
        risotto.AddStep("Add mushrooms and cook until they release their water.");
        risotto.AddStep("Stir in rice and toast for 1 minute.");
        risotto.AddStep("Pour in wine, let it absorb, then add hot stock a ladle at a time, stirring until creamy.");
        risotto.AddStep("Off the heat, fold in grated parmesan.");

        var oats = new Recipe("Breakfast Oats");
        oats.AddIngredient("rolled oats", 80m, "g");
        oats.AddIngredient("milk", 250m, "ml");
        oats.AddIngredient("banana", 1m, "pcs");
        oats.AddIngredient("honey", 1m, "tbsp");
        oats.AddIngredient("walnuts", 20m, "g");
        oats.AddStep("Combine oats and milk in a small saucepan.");
        oats.AddStep("Bring to a gentle simmer and cook for 5 minutes, stirring often.");
        oats.AddStep("Top with sliced banana, honey and walnuts.");

        var recipes = new[] { carbonara, greekSalad, chickenCurry, bananaBread, trayBake, lentilSoup, beefStew, stirFry, risotto, oats };
        _db.Recipes.AddRange(recipes);

        var weekStart = DateOnly.FromDateTime(DateTime.Today);
        var mealPlan = new MealPlan("This week's plan", stefanovs.Id);

        AddSharedDinner(mealPlan, carbonara, weekStart, [stan, elena, milo]);
        AddSharedDinner(mealPlan, greekSalad, weekStart.AddDays(1), [stan, elena, milo]);
        AddSharedDinner(mealPlan, chickenCurry, weekStart.AddDays(2), [stan, elena, milo]);
        AddSharedDinner(mealPlan, trayBake, weekStart.AddDays(3), [stan, elena, milo]);
        AddSharedDinner(mealPlan, lentilSoup, weekStart.AddDays(4), [stan, elena, milo]);
        AddSharedDinner(mealPlan, beefStew, weekStart.AddDays(5), [stan, elena, milo]);
        AddSharedDinner(mealPlan, risotto, weekStart.AddDays(6), [stan, elena, milo]);

        _db.MealPlans.Add(mealPlan);

        var products = new[]
        {
            "spaghetti", "guanciale", "egg yolks", "pecorino romano",
            "cucumber", "tomato", "feta",
            "chicken thighs", "coconut milk", "rice",
            "chicken drumsticks", "baby potatoes", "bell pepper",
            "brown lentils", "carrot", "onion",
            "beef chuck", "potato",
            "arborio rice", "cremini mushrooms", "parmesan"
        }
            .Distinct()
            .Select(name => new Product(name))
            .ToList();

        _db.Products.AddRange(products);

        var shoppingList = new ShoppingList("This week's groceries");
        var pickedProducts = products.Take(12).ToList();
        for (var i = 0; i < pickedProducts.Count; i++)
        {
            shoppingList.AddItem(
                pickedProducts[i],
                quantity: 1m + i % 3,
                unit: i % 2 == 0 ? "pcs" : "pack",
                notes: null,
                sourceType: ShoppingListItemSourceType.MealPlan,
                sourceReferenceId: mealPlan.Id.Value);
        }

        _db.ShoppingLists.Add(shoppingList);

        await _db.SaveChangesAsync(cancellationToken);

        var halfPurchased = shoppingList.Items.Take(shoppingList.Items.Count / 2).ToList();
        foreach (var item in halfPurchased)
        {
            shoppingList.MarkItemPurchased(item.Id);
        }

        await _db.SaveChangesAsync(cancellationToken);

        var thisMonth = DateOnly.FromDateTime(DateTime.Today);
        var lastMonth = thisMonth.AddMonths(-1);

        _db.Expenses.AddRange(
            new Expense(42.30m,  "EUR", thisMonth.AddDays(-1),  ExpenseCategory.Food,          "Weekly groceries",                  ExpenseSourceType.ShoppingList, shoppingList.Id.Value),
            new Expense(18.75m,  "EUR", thisMonth.AddDays(-3),  ExpenseCategory.Food,          "Bakery",                            ExpenseSourceType.Manual),
            new Expense(56.40m,  "EUR", thisMonth.AddDays(-7),  ExpenseCategory.Food,          "Dinner out — Mediterranean",        ExpenseSourceType.Manual),
            new Expense(120.00m, "EUR", thisMonth.AddDays(-10), ExpenseCategory.Utilities,     "Electricity",                       ExpenseSourceType.Manual),
            new Expense(35.00m,  "EUR", thisMonth.AddDays(-12), ExpenseCategory.Transport,     "Monthly transit pass",              ExpenseSourceType.Manual),
            new Expense(48.20m,  "EUR", lastMonth.AddDays(2),   ExpenseCategory.Food,          "Weekly groceries",                  ExpenseSourceType.ShoppingList),
            new Expense(22.10m,  "EUR", lastMonth.AddDays(5),   ExpenseCategory.Entertainment, "Cinema",                            ExpenseSourceType.Manual),
            new Expense(67.95m,  "EUR", lastMonth.AddDays(9),   ExpenseCategory.Food,          "Restaurant",                        ExpenseSourceType.Manual),
            new Expense(115.00m, "EUR", lastMonth.AddDays(14),  ExpenseCategory.Utilities,     "Internet",                          ExpenseSourceType.Manual),
            new Expense(28.50m,  "EUR", lastMonth.AddDays(20),  ExpenseCategory.Health,        "Pharmacy",                          ExpenseSourceType.Manual));

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static void AddSharedDinner(MealPlan plan, Recipe recipe, DateOnly date, IReadOnlyList<Person> members)
    {
        var assignments = members
            .Select(m => (m.Id, recipe.Id, (Domain.Primitives.RecipeVariationId?)null, 1.0m, (string?)null))
            .ToList();

        plan.AddRecipe(
            recipe,
            date,
            MealType.Dinner,
            MealScope.Shared,
            assignments);
    }
}
