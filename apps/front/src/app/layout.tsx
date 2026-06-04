import type { Metadata } from "next";
import { AuthProvider } from "@/components/auth/auth-provider";
import { SiteShell } from "@/components/layout/site-shell";
import { RequestDraftProvider } from "@/components/request/request-draft-provider";
import { buildOrganizationJsonLd, JsonLdScript } from "@/lib/seo/json-ld";
import { indexablePageMetadata } from "@/lib/seo/metadata";
import { siteMetadataBase } from "@/lib/seo/site";
import "./globals.css";
import "../styles/layout.css";
import "../styles/public.css";
import "../styles/account.css";
import "../styles/contact-modal.css";
import "../styles/admin-requests.css";
import "../styles/admin-catalog.css";
import "../styles/admin-homepage.css";
import "../styles/responsive.css";

const themeScript = `
(() => {
  try {
    const stored = window.localStorage.getItem("linecom.theme");
    const theme = stored === "light" || stored === "dark"
      ? stored
      : (window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light");
    document.documentElement.dataset.theme = theme;
    document.documentElement.style.colorScheme = theme;
  } catch {
    document.documentElement.dataset.theme = "light";
  }
})();
`;

export const metadata: Metadata = {
  ...indexablePageMetadata({
    title: "LineCom - каталог кабеля и компонентов",
    description: "Каталог кабеля, СКС, ВОЛС и сопутствующих компонентов для B2B-поставок по запросу.",
    canonicalPath: "/",
  }),
  applicationName: "LineCom",
  metadataBase: siteMetadataBase(),
  icons: {
    icon: [
      {
        url: "/linecom-tab-icon.svg",
        type: "image/svg+xml",
      },
    ],
  },
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="ru" suppressHydrationWarning>
      <head>
        <script dangerouslySetInnerHTML={{ __html: themeScript }} />
        <JsonLdScript data={buildOrganizationJsonLd()} />
      </head>
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
