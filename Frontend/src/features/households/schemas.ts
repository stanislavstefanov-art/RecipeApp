import { z } from "zod";

export const householdMemberSchema = z.object({
  personId: z.string().uuid(),
  personName: z.string(),
  dietaryPreferences: z.array(z.number()),
  healthConcerns: z.array(z.number()),
  notes: z.string().nullable().optional(),
});

export const householdDetailsSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  members: z.array(householdMemberSchema),
});

export const householdListItemSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  memberCount: z.number(),
});

export const createHouseholdSchema = z.object({
  name: z.string().min(1, "Name is required").max(200),
});

export type HouseholdDetails = z.infer<typeof householdDetailsSchema>;
export type HouseholdListItem = z.infer<typeof householdListItemSchema>;
export type CreateHouseholdInput = z.input<typeof createHouseholdSchema>;
export type CreateHouseholdData = z.output<typeof createHouseholdSchema>;