"use client";

import { useCallback, useEffect, useId, useRef, type ReactNode } from "react";

type AdminCatalogModalProps = {
  children: ReactNode;
  closeLabel: string;
  confirmClose?: () => boolean;
  isCloseDisabled?: boolean;
  isOpen: boolean;
  onRequestClose: () => void;
  subtitle?: string;
  title: string;
};

export function AdminCatalogModal({
  children,
  closeLabel,
  confirmClose,
  isCloseDisabled = false,
  isOpen,
  onRequestClose,
  subtitle,
  title,
}: AdminCatalogModalProps) {
  const titleId = useId();
  const subtitleId = subtitle ? `${titleId}-subtitle` : undefined;
  const dialogRef = useRef<HTMLDivElement | null>(null);
  const previousFocusRef = useRef<HTMLElement | null>(null);
  const closeStateRef = useRef({ confirmClose, isCloseDisabled, onRequestClose });

  useEffect(() => {
    closeStateRef.current = { confirmClose, isCloseDisabled, onRequestClose };
  }, [confirmClose, isCloseDisabled, onRequestClose]);

  const requestClose = useCallback(() => {
    const closeState = closeStateRef.current;
    if (closeState.isCloseDisabled) return;
    if (closeState.confirmClose && !closeState.confirmClose()) return;
    closeState.onRequestClose();
  }, []);

  useEffect(() => {
    if (!isOpen) return;

    previousFocusRef.current = document.activeElement instanceof HTMLElement ? document.activeElement : null;

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key !== "Escape") return;
      event.preventDefault();
      requestClose();
    }

    document.addEventListener("keydown", handleKeyDown);
    dialogRef.current?.focus();

    return () => {
      document.removeEventListener("keydown", handleKeyDown);
      const previousFocus = previousFocusRef.current;
      if (previousFocus && document.contains(previousFocus)) {
        previousFocus.focus();
      }
      previousFocusRef.current = null;
    };
  }, [isOpen, requestClose]);

  if (!isOpen) return null;

  return (
    <div
      className="admin-catalog-modal"
      data-testid="admin-catalog-modal-backdrop"
      onMouseDown={(event) => {
        if (event.target === event.currentTarget) {
          requestClose();
        }
      }}
    >
      <div
        aria-describedby={subtitleId}
        aria-labelledby={titleId}
        aria-modal="true"
        className="admin-catalog-modal__dialog"
        ref={dialogRef}
        role="dialog"
        tabIndex={-1}
      >
        <header className="admin-catalog-modal__header">
          <div>
            <h2 id={titleId}>{title}</h2>
            {subtitle ? (
              <p className="admin-catalog-status" id={subtitleId}>
                {subtitle}
              </p>
            ) : null}
          </div>
          <button
            aria-label={closeLabel}
            className="admin-catalog-modal__close"
            disabled={isCloseDisabled}
            onClick={requestClose}
            type="button"
          >
            <span aria-hidden="true">×</span>
          </button>
        </header>
        <div className="admin-catalog-modal__body">{children}</div>
      </div>
    </div>
  );
}
