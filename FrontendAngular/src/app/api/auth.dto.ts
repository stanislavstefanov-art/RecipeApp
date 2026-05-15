export interface AuthUserDto {
  readonly id: string;
  readonly email: string;
  readonly displayName: string;
  readonly provider: string;
}

export interface AuthSessionDto {
  readonly token: string;
  readonly expiresAt: string;
  readonly user: AuthUserDto;
}

export interface MeResponseDto {
  readonly user: AuthUserDto;
  readonly households: readonly { readonly id: string; readonly name: string }[];
}
