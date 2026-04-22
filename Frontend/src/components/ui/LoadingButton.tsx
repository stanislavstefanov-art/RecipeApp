import type { ButtonHTMLAttributes, PropsWithChildren } from "react";

type Props = PropsWithChildren<
  ButtonHTMLAttributes<HTMLButtonElement> & {
    isLoading?: boolean;
    loadingText?: string;
  }
>;

export function LoadingButton({
  children,
  isLoading = false,
  loadingText = "Working...",
  className,
  disabled,
  ...rest
}: Props) {
  return (
    <button
      {...rest}
      disabled={disabled || isLoading}
      className={className}
    >
      {isLoading ? loadingText : children}
    </button>
  );
}