export interface PersonDto {
  readonly id: string;
  readonly name: string;
  readonly dietaryPreferences: readonly number[];
  readonly healthConcerns: readonly number[];
  readonly notes?: string;
}

export interface CreatePersonRequest {
  readonly name: string;
  readonly dietaryPreferences: readonly number[];
  readonly healthConcerns: readonly number[];
  readonly notes?: string;
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
}
