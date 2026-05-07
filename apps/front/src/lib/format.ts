export function formatDateTime(value: string): string {
  return new Intl.DateTimeFormat("ru-RU", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}

export function formatSku(sku: string | null): string {
  return sku ? `Артикул: ${sku}` : "Артикул не указан";
}
