import { NavLink, Outlet } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { useUiStore } from "../../stores/uiStore";
import { ToastViewport } from "../ui/ToastViewport";
import { LanguageSwitcher } from "../ui/LanguageSwitcher";

const navClass = ({ isActive }: { isActive: boolean }) =>
  isActive
    ? "font-medium text-blue-600"
    : "text-slate-600 hover:text-slate-900";

export function AppLayout() {
  const { isNavOpen, toggleNav, setNavOpen } = useUiStore();
  const { t } = useTranslation();

  return (
    <div className="min-h-screen">
      <ToastViewport />

      <header className="border-b bg-white">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-4 py-4 sm:px-6">
          <div>
            <h1 className="text-lg font-semibold sm:text-xl">RecipeApp</h1>
            <p className="text-xs text-slate-500 sm:text-sm">
              Recipes, meal plans, shopping, expenses
            </p>
          </div>

          <div className="flex items-center gap-3">
            <LanguageSwitcher />

            <button
              type="button"
              onClick={toggleNav}
              className="rounded-lg border px-3 py-2 text-sm md:hidden"
            >
              {isNavOpen ? t('common.close') : t('common.menu')}
            </button>
          </div>

          <nav className="hidden gap-4 text-sm md:flex">
            <NavLink to="/" className={navClass}>
              {t('nav.home')}
            </NavLink>
            <NavLink to="/recipes" className={navClass}>
              {t('nav.recipes')}
            </NavLink>
            <NavLink to="/persons" className={navClass}>
              {t('nav.persons')}
            </NavLink>
            <NavLink to="/households" className={navClass}>
              {t('nav.households')}
            </NavLink>
            <NavLink to="/meal-plans" className={navClass}>
              {t('nav.mealPlans')}
            </NavLink>
            <NavLink to="/shopping-lists" className={navClass}>
              {t('nav.shoppingLists')}
            </NavLink>
            <NavLink to="/expenses" className={navClass}>
              {t('nav.expenses')}
            </NavLink>
            <NavLink to="/expenses/report" className={navClass}>
              {t('nav.reports')}
            </NavLink>
          </nav>
        </div>

        {isNavOpen ? (
          <nav className="border-t bg-white px-4 py-3 md:hidden sm:px-6">
            <div className="flex flex-col gap-2 text-sm">
              <NavLink to="/" className={navClass} onClick={() => setNavOpen(false)}>
                {t('nav.home')}
              </NavLink>
              <NavLink to="/recipes" className={navClass} onClick={() => setNavOpen(false)}>
                {t('nav.recipes')}
              </NavLink>
              <NavLink to="/persons" className={navClass} onClick={() => setNavOpen(false)}>
                {t('nav.persons')}
              </NavLink>
              <NavLink to="/households" className={navClass} onClick={() => setNavOpen(false)}>
                {t('nav.households')}
              </NavLink>
              <NavLink to="/meal-plans" className={navClass} onClick={() => setNavOpen(false)}>
                {t('nav.mealPlans')}
              </NavLink>
              <NavLink to="/shopping-lists" className={navClass} onClick={() => setNavOpen(false)}>
                {t('nav.shoppingLists')}
              </NavLink>
              <NavLink to="/expenses" className={navClass} onClick={() => setNavOpen(false)}>
                {t('nav.expenses')}
              </NavLink>
              <NavLink to="/expenses/report" className={navClass} onClick={() => setNavOpen(false)}>
                {t('nav.reports')}
              </NavLink>
            </div>
          </nav>
        ) : null}
      </header>

      <main className="mx-auto max-w-6xl px-4 py-6 sm:px-6 sm:py-8">
        <Outlet />
      </main>
    </div>
  );
}
