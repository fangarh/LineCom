"use client";

import type { RequestDraftProduct } from "@/lib/request-draft/types";
import { useRequestDraft } from "./request-draft-provider";

type AddToRequestButtonProps = {
  product: RequestDraftProduct;
  className?: string;
};

export function AddToRequestButton({ product, className }: AddToRequestButtonProps) {
  const { state, dispatch } = useRequestDraft();
  const quantity = state.items.find((item) => item.productId === product.productId)?.quantity ?? 0;
  const label = quantity > 0 ? `В заявке: ${quantity}` : "Добавить в заявку";

  return (
    <button
      className={className ?? "button button--primary"}
      type="button"
      onClick={() => dispatch({ type: "addProduct", product })}
    >
      <span aria-live="polite">{label}</span>
    </button>
  );
}
