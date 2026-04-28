import { z } from "zod";
import type { TFunction } from "i18next";

export function createLoginSchema(t: TFunction) {
  return z.object({
    email: z.string().min(1, t("validation.required")).email(t("validation.email")),
    password: z.string().min(1, t("validation.required")),
  });
}

export function createRegisterSchema(t: TFunction) {
  return z.object({
    email: z.string().min(1, t("validation.required")).email(t("validation.email")),
    password: z
      .string()
      .min(8, t("validation.minLength", { min: 8 }))
      .regex(/[a-zA-Z]/, t("validation.passwordLetter"))
      .regex(/[0-9]/, t("validation.passwordDigit")),
    displayName: z
      .string()
      .min(1, t("validation.required"))
      .max(100, t("validation.maxLength", { max: 100 })),
  });
}

export type LoginInput = z.infer<ReturnType<typeof createLoginSchema>>;
export type RegisterInput = z.infer<ReturnType<typeof createRegisterSchema>>;
