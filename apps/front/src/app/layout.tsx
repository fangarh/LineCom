import type { Metadata } from "next";
import { AuthProvider } from "@/components/auth/auth-provider";
import { SiteShell } from "@/components/layout/site-shell";
import { RequestDraftProvider } from "@/components/request/request-draft-provider";
import { indexablePageMetadata } from "@/lib/seo/metadata";
import { siteMetadataBase } from "@/lib/seo/site";
import "./globals.css";

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
    description: "Каталог кабеля, СКС, ВОЛС и сопутствующих компонентов с заявками по запросу.",
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
