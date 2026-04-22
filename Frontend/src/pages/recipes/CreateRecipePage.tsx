import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useNavigate } from "react-router-dom";
import {
  createRecipeSchema,
  type CreateRecipeInput,
} from "../../features/recipes/schemas";
import { useCreateRecipe } from "../../features/recipes/hooks/useCreateRecipe";

export function CreateRecipePage() {
  const navigate = useNavigate();
  const mutation = useCreateRecipe();

  const form = useForm<CreateRecipeInput>({
    resolver: zodResolver(createRecipeSchema),
  });

  const onSubmit = async (values: CreateRecipeInput) => {
    const recipe = await mutation.mutateAsync(values);
    navigate(`/recipes/${recipe.id}`);
  };

  return (
    <div className="max-w-xl space-y-6">
      <h2 className="text-2xl font-semibold">Create recipe</h2>

      <form
        onSubmit={form.handleSubmit(onSubmit)}
        className="space-y-4 border p-6 rounded-xl bg-white"
      >
        <div>
          <label className="text-sm font-medium">Name</label>
          <input
            {...form.register("name")}
            className="w-full border px-3 py-2 rounded-lg"
          />
          {form.formState.errors.name && (
            <p className="text-sm text-red-600">
              {form.formState.errors.name.message}
            </p>
          )}
        </div>

        <button
          type="submit"
          className="bg-slate-900 text-white px-4 py-2 rounded-lg"
        >
          Create
        </button>
      </form>
    </div>
  );
}