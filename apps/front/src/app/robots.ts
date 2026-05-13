import type { MetadataRoute } from "next";
import { absoluteSiteUrl, getPublicSiteOrigin } from "@/lib/seo/site";

export default function robots(): MetadataRoute.Robots {
  return {
    rules: {
      userAgent: "*",
      allow: "/",
      disallow: ["/admin/", "/account/", "/auth/"],
    },
    sitemap: absoluteSiteUrl("/sitemap.xml"),
    host: getPublicSiteOrigin(),
  };
}
