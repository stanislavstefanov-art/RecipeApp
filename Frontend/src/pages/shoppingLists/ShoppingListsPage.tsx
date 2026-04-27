import { Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { LoadingState, ErrorState, EmptyState } from "../../components/ui/PageState";
import { CreateShoppingListForm } from "../../features/shoppingLists/components/CreateShoppingListForm";
import { useShoppingLists } from "../../features/shoppingLists/hooks/useShoppingLists";
import { SectionHeader } from "../../components/ui/SectionHeader";

export function ShoppingListsPage() {
  const { t } = useTranslation();
  const { data, isLoading, isError, error } = useShoppingLists();

  return (
    <div className="grid gap-6 lg:grid-cols-[1fr_420px]">
      <div className="space-y-6">
        <SectionHeader
          title={t('shoppingLists.title')}
          description={t('shoppingLists.noItems')}
        />

        {isLoading ? (
          <LoadingState title={t('shoppingLists.title')} />
        ) : isError ? (
          <ErrorState
            title={t('shoppingLists.title')}
            message={error instanceof Error ? error.message : undefined}
          />
        ) : !data || data.length === 0 ? (
          <EmptyState title={t('shoppingLists.noShoppingLists')} />
        ) : (
          <div className="grid gap-4">
            {data.map((shoppingList) => (
              <Link
                key={shoppingList.id}
                to={`/shopping-lists/${shoppingList.id}`}
                className="rounded-xl border bg-white p-5 hover:shadow-sm"
              >
                <h3 className="font-medium">{shoppingList.name}</h3>
                <p className="mt-1 text-sm text-slate-500">
                  {t('shoppingLists.itemCountLabel', { count: shoppingList.items.length })}
                </p>
              </Link>
            ))}
          </div>
        )}
      </div>

      <div>
        <CreateShoppingListForm />
      </div>
    </div>
  );
}
