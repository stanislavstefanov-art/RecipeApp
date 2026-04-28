import { useState, useRef, useEffect } from "react";
import { useTranslation } from "react-i18next";
import { useNavigate } from "react-router-dom";
import { useAuthStore } from "../store/authStore";

export function UserMenu() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { session, logout } = useAuthStore();
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    function handleClick(e: MouseEvent) {
      if (ref.current && !ref.current.contains(e.target as Node)) {
        setOpen(false);
      }
    }
    document.addEventListener("mousedown", handleClick);
    return () => document.removeEventListener("mousedown", handleClick);
  }, []);

  if (!session) return null;

  const handleLogout = () => {
    logout();
    setOpen(false);
    navigate("/login", { replace: true });
  };

  return (
    <div className="relative" ref={ref}>
      <button
        type="button"
        onClick={() => setOpen((o) => !o)}
        className="flex items-center gap-2 rounded-lg border px-3 py-1.5 text-sm hover:bg-slate-50"
      >
        <span className="max-w-[120px] truncate text-slate-700">
          {session.user.displayName}
        </span>
        <svg className="h-4 w-4 text-slate-400" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
          <path d="M5.25 7.5l4.75 5 4.75-5H5.25z" />
        </svg>
      </button>

      {open && (
        <div className="absolute right-0 z-10 mt-1 w-48 rounded-lg border bg-white shadow-lg">
          <div className="border-b px-4 py-2">
            <p className="text-xs text-slate-500">{session.user.email}</p>
          </div>
          <button
            type="button"
            onClick={handleLogout}
            className="w-full px-4 py-2 text-left text-sm text-slate-700 hover:bg-slate-50"
          >
            {t("auth.logout")}
          </button>
        </div>
      )}
    </div>
  );
}
