import { z } from "zod";

export const personDetailsSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  dietaryPreferences: z.array(z.number()),
  healthConcerns: z.array(z.number()),
  notes: z.string().nullable().optional(),
});

export const createPersonSchema = z.object({
  name: z.string().min(1, "Name is required").max(200),
  dietaryPreferences: z.array(z.coerce.number()).default([]),
  healthConcerns: z.array(z.coerce.number()).default([]),
  notes: z.string().max(1000).optional().or(z.literal("")),
});

export type PersonDetails = z.infer<typeof personDetailsSchema>;

export type CreatePersonInput = z.input<typeof createPersonSchema>;
export type CreatePersonData = z.output<typeof createPersonSchema>;