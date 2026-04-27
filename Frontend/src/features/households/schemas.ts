import { z } from "zod";
import type { TFunction } from "i18next";

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

export const createHouseholdSchema = (t: TFunction) =>
  z.object({
    name: z.string().min(1, t("validation.required")).max(200, t("validation.maxLength", { max: 200 })),
  });

export type HouseholdDetails = z.infer<typeof householdDetailsSchema>;
export type HouseholdListItem = z.infer<typeof householdListItemSchema>;
export type CreateHouseholdInput = z.input<ReturnType<typeof createHouseholdSchema>>;
export type CreateHouseholdData = z.output<ReturnType<typeof createHouseholdSchema>>;
