import { useMutation } from "@tanstack/react-query";
import { useMsal } from "@azure/msal-react";
import { entraExchange } from "../api/authClient";
import { useAuthStore } from "../store/authStore";

const LOGIN_REQUEST = { scopes: ["openid", "profile", "email"] };

export function useEntraLogin() {
  const { instance } = useMsal();
  const { login: storeLogin } = useAuthStore();

  return useMutation({
    mutationFn: async () => {
      const result = await instance.loginPopup(LOGIN_REQUEST);
      const idToken = result.idToken;
      return entraExchange(idToken);
    },
    onSuccess: (session) => storeLogin(session),
  });
}
