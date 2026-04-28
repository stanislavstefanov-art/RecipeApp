import { useMutation } from "@tanstack/react-query";
import { login } from "../api/authClient";
import { useAuthStore } from "../store/authStore";

export function useLogin() {
  const { login: storeLogin } = useAuthStore();

  return useMutation({
    mutationFn: login,
    onSuccess: (session) => storeLogin(session),
  });
}
