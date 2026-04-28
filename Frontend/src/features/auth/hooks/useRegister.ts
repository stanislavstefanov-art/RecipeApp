import { useMutation } from "@tanstack/react-query";
import { register } from "../api/authClient";
import { useAuthStore } from "../store/authStore";

export function useRegister() {
  const { login: storeLogin } = useAuthStore();

  return useMutation({
    mutationFn: register,
    onSuccess: (session) => storeLogin(session),
  });
}
