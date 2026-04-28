import { apiClient } from "../../../api/client";
import type { AuthSession } from "../store/authStore";

export type RegisterInput = {
  email: string;
  password: string;
  displayName: string;
};

export type LoginInput = {
  email: string;
  password: string;
};

export type MeResponse = {
  user: AuthSession["user"];
  households: Array<{ id: string; name: string }>;
};

export async function register(input: RegisterInput): Promise<AuthSession> {
  const res = await apiClient.post("/api/auth/register", input);
  return res.data as AuthSession;
}

export async function login(input: LoginInput): Promise<AuthSession> {
  const res = await apiClient.post("/api/auth/login", input);
  return res.data as AuthSession;
}

export async function entraExchange(idToken: string): Promise<AuthSession> {
  const res = await apiClient.post("/api/auth/entra/exchange", { idToken });
  return res.data as AuthSession;
}

export async function getMe(): Promise<MeResponse> {
  const res = await apiClient.get("/api/auth/me");
  return res.data as MeResponse;
}
