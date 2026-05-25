export interface PersonDto {
  readonly id: string;
  readonly name: string;
  readonly dietaryPreferences: readonly number[];
  readonly healthConcerns: readonly number[];
  readonly notes?: string;
  readonly dateOfBirth?: string;
  readonly gender?: number;
}

export interface CreatePersonRequest {
  readonly name: string;
  readonly dietaryPreferences: readonly number[];
  readonly healthConcerns: readonly number[];
  readonly notes?: string;
  readonly householdId: string;
  readonly dateOfBirth?: string;
  readonly gender?: number;
}

export interface CreatePersonResponse {
  readonly id: string;
  readonly name: string;
}

export interface PersonDetailsDto {
  readonly id: string;
  readonly name: string;
  readonly dietaryPreferences: readonly number[];
  readonly healthConcerns: readonly number[];
  readonly notes?: string;
  readonly dateOfBirth?: string;
  readonly gender?: number;
}
