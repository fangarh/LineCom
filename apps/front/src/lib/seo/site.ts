const DEFAULT_PUBLIC_SITE_ORIGIN = "http://127.0.0.1:3000";

export function normalizeSiteOrigin(value: string | null | undefined) {
  const trimmed = value?.trim();
  if (!trimmed) {
    return DEFAULT_PUBLIC_SITE_ORIGIN;
  }

  try {
    const parsed = new URL(trimmed);
    if (parsed.protocol !== "http:" && parsed.protocol !== "https:") {
      return DEFAULT_PUBLIC_SITE_ORIGIN;
    }

    return parsed.origin;
  } catch {
    return DEFAULT_PUBLIC_SITE_ORIGIN;
  }
}

export function getPublicSiteOrigin() {
  return normalizeSiteOrigin(process.env.LINECOM_PUBLIC_SITE_ORIGIN);
}

export function siteMetadataBase() {
  return new URL(getPublicSiteOrigin());
}

export function absoluteSiteUrl(path: string) {
  const normalizedPath = path.startsWith("/") ? path : `/${path}`;
  return `${getPublicSiteOrigin()}${normalizedPath}`;
}
