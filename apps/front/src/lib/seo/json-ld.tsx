import type { PublicProductDetail } from "@/lib/api/catalog";
import { routes } from "@/lib/routes";
import { absoluteSiteUrl } from "./site";

type JsonLdValue = Record<string, unknown>;

type BreadcrumbJsonLdItem = {
  name: string;
  path: string;
};

function toAbsolutePublicUrl(value: string) {
  try {
    return new URL(value).href;
  } catch {
    return absoluteSiteUrl(value);
  }
}

export function serializeJsonLd(data: JsonLdValue) {
  return JSON.stringify(data).replace(/</g, "\\u003c");
}

export function JsonLdScript({ data }: { data: JsonLdValue }) {
  return (
    <script
      type="application/ld+json"
      dangerouslySetInnerHTML={{
        __html: serializeJsonLd(data),
      }}
    />
  );
}

export function buildOrganizationJsonLd(): JsonLdValue {
  return {
    "@context": "https://schema.org",
    "@type": ["Organization", "LocalBusiness"],
    name: "LineCom",
    legalName: "ООО «ЛАЙНКОМ»",
    url: absoluteSiteUrl("/"),
    logo: absoluteSiteUrl("/linecom-logo-full.png"),
    taxID: "7801724840",
    identifier: [
      {
        "@type": "PropertyValue",
        propertyID: "ОГРН",
        value: "1237800078845",
      },
      {
        "@type": "PropertyValue",
        propertyID: "КПП",
        value: "780101001",
      },
    ],
    email: "Linecom.sup@gmail.com",
    telephone: "+79313064350",
    address: {
      "@type": "PostalAddress",
      postalCode: "199406",
      addressCountry: "RU",
      addressLocality: "Санкт-Петербург",
      streetAddress: "ул. Шевченко, дом 23, корпус 1, литера А, помещение 1-Н, офис 2-1",
    },
  };
}

export function buildProductJsonLd(product: PublicProductDetail): JsonLdValue {
  return {
    "@context": "https://schema.org",
    "@type": "Product",
    name: product.h1 ?? product.name,
    description: product.seo.description ?? product.shortDescription ?? product.description ?? undefined,
    sku: product.sku ?? undefined,
    brand: product.brand
      ? {
          "@type": "Brand",
          name: product.brand.name,
        }
      : undefined,
    category: product.category.name,
    image: product.images.map((image) => toAbsolutePublicUrl(image.url)),
    url: absoluteSiteUrl(routes.product(product.slug)),
  };
}

export function buildBreadcrumbListJsonLd(items: BreadcrumbJsonLdItem[]): JsonLdValue {
  return {
    "@context": "https://schema.org",
    "@type": "BreadcrumbList",
    itemListElement: items.map((item, index) => ({
      "@type": "ListItem",
      position: index + 1,
      name: item.name,
      item: absoluteSiteUrl(item.path),
    })),
  };
}
