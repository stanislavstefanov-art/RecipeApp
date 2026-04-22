import { apiClient } from "./client";
import {
  personDetailsSchema,
  type CreatePersonData,
} from "../features/persons/schemas";

export async function getPersons() {
  const res = await apiClient.get("/api/persons");
  return personDetailsSchema.array().parse(res.data);
}

export async function getPerson(personId: string) {
  const res = await apiClient.get(`/api/persons/${personId}`);
  return personDetailsSchema.parse(res.data);
}

export async function createPerson(input: CreatePersonData) {
  const payload = {
    ...input,
    notes: input.notes || null,
  };

  const res = await apiClient.post("/api/persons", payload);
  return personDetailsSchema.pick({ id: true, name: true }).parse(res.data);
}