import type { Metadata } from "next";
import { Geist, Geist_Mono } from "next/font/google";
import { AuthProvider } from "@/components/auth/auth-provider";
import { SiteShell } from "@/components/layout/site-shell";
import { RequestDraftProvider } from "@/components/request/request-draft-provider";
import "./globals.css";

const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin", "cyrillic"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin", "cyrillic"],
});

export const metadata: Metadata = {
  title: "LineCom - каталог кабеля и компонентов",
  description: "Каталог кабеля, СКС, ВОЛС и сопутствующих компонентов с заявками по запросу.",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="ru" className={`${geistSans.variable} ${geistMono.variable}`}>
      <body>
        <AuthProvider>
          <RequestDraftProvider>
            <SiteShell>{children}</SiteShell>
          </RequestDraftProvider>
        </AuthProvider>
      </body>
    </html>
  );
}
