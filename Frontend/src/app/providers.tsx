import type { PropsWithChildren } from "react";
import { PublicClientApplication } from "@azure/msal-browser";
import { MsalProvider } from "@azure/msal-react";
import {
  QueryClient,
  QueryClientProvider,
} from "@tanstack/react-query";

const msalInstance = new PublicClientApplication({
  auth: {
    clientId: import.meta.env.VITE_ENTRA_CLIENT_ID ?? "",
    authority: `https://login.microsoftonline.com/${import.meta.env.VITE_ENTRA_TENANT_ID ?? "common"}`,
    redirectUri: import.meta.env.VITE_ENTRA_REDIRECT_URI ?? window.location.origin,
  },
  cache: {
    cacheLocation: "localStorage",
  },
});

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      refetchOnWindowFocus: false,
    },
  },
});

export function AppProviders({ children }: PropsWithChildren) {
  return (
    <MsalProvider instance={msalInstance}>
      <QueryClientProvider client={queryClient}>
        {children}
      </QueryClientProvider>
    </MsalProvider>
  );
}