"use client";

import type { RequestDraftProduct } from "@/lib/request-draft/types";
import { useRequestDraft } from "./request-draft-provider";

type AddToRequestButtonProps = {
  product: RequestDraftProduct;
  className?: string;
};

export function AddToRequestButton({ product, className }: AddToRequestButtonProps) {
  const { dispatch } = useRequestDraft();

  return (
    <button
      className={className ?? "button button--primary"}
      type="button"
      onClick={() => dispatch({ type: "addProduct", product })}
    >
      Добавить в заявку
    </button>
  );
}
