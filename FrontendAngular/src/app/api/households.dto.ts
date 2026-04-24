export interface HouseholdListItemDto {
  readonly id: string;
  readonly name: string;
  readonly memberCount: number;
}

export interface CreateHouseholdRequest {
  readonly name: string;
}

export interface CreateHouseholdResponse {
  readonly id: string;
  readonly name: string;
}

export interface HouseholdMemberDto {
  readonly personId: string;
  readonly personName: string;
  readonly dietaryPreferences: readonly number[];
  readonly healthConcerns: readonly number[];
  readonly notes?: string;
}

export interface HouseholdDetailsDto {
  readonly id: string;
  readonly name: string;
  readonly members: readonly HouseholdMemberDto[];
}
