import { apiClient } from "./client";
import {
  householdDetailsSchema,
  householdListItemSchema,
  type CreateHouseholdInput,
} from "../features/households/schemas";

export async function getHouseholds() {
  const res = await apiClient.get("/api/households");
  return householdListItemSchema.array().parse(res.data);
}

export async function getHousehold(householdId: string) {
  const res = await apiClient.get(`/api/households/${householdId}`);
  return householdDetailsSchema.parse(res.data);
}

export async function createHousehold(input: CreateHouseholdInput) {
  const res = await apiClient.post("/api/households", input);
  return householdListItemSchema.pick({ id: true, name: true }).extend({
    memberCount: householdListItemSchema.shape.memberCount.optional().default(0),
  }).parse(res.data);
}

export async function addPersonToHousehold(
  householdId: string,
  personId: string,
) {
  await apiClient.post(`/api/households/${householdId}/members/${personId}`);
}