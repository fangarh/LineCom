import type { AdminProductDuplicateCandidate } from "@/lib/api/admin-catalog";

type AdminProductDuplicatePanelProps = {
  duplicateCandidates: AdminProductDuplicateCandidate[];
  isCheckingDuplicates: boolean;
  isLoadingDetail: boolean;
  onCheckDuplicateCandidates: () => void;
};

export function AdminProductDuplicatePanel({
  duplicateCandidates,
  isCheckingDuplicates,
  isLoadingDetail,
  onCheckDuplicateCandidates,
}: AdminProductDuplicatePanelProps) {
  return (
    <section className="admin-product-manager__duplicates" aria-label="Кандидаты дублей">
      <div className="admin-product-manager__head">
        <h2>Дубли</h2>
        <button
          className="button button--secondary"
          disabled={isCheckingDuplicates || isLoadingDetail}
          onClick={onCheckDuplicateCandidates}
          type="button"
        >
          Проверить дубли
        </button>
      </div>
      {duplicateCandidates.length ? (
        <table className="admin-product-manager__duplicate-table">
          <tbody>
            {duplicateCandidates.map((candidate) => (
              <tr key={candidate.id}>
                <td>
                  <strong>{candidate.name}</strong>
                  <small>{candidate.slug}</small>
                </td>
                <td>{candidate.sku ?? "Без SKU"}</td>
                <td>{candidate.categoryName}</td>
                <td>{candidate.brandName ?? "Без бренда"}</td>
                <td>{Math.round(candidate.similarity * 100)}%</td>
              </tr>
            ))}
          </tbody>
        </table>
      ) : (
        <p className="admin-catalog-status">Кандидаты не загружены.</p>
      )}
    </section>
  );
}
