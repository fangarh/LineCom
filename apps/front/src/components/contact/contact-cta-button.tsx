"use client";

import { useState } from "react";
import { ContactDialog } from "./contact-dialog";

type ContactCtaButtonProps = {
  className?: string;
};

export function ContactCtaButton({ className }: ContactCtaButtonProps) {
  const [isOpen, setIsOpen] = useState(false);

  return (
    <>
      <button className={className ?? "button button--primary"} type="button" onClick={() => setIsOpen(true)}>
        Связаться с нами
      </button>
      <ContactDialog isOpen={isOpen} onClose={() => setIsOpen(false)} />
    </>
  );
}
