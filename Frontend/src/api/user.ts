import { z } from "zod";
import { apiClient } from "./client";

const userProfileSchema = z.object({
  userId: z.string().uuid(),
  email: z.string(),
  displayName: z.string(),
  personId: z.string().uuid().nullable().default(null),
});

export type UserProfile = z.infer<typeof userProfileSchema>;

export async function getUserProfile(): Promise<UserProfile> {
  const res = await apiClient.get("/api/user/me");
  return userProfileSchema.parse(res.data);
}
