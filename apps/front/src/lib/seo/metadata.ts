import type { Metadata } from "next";

type IndexablePageMetadataInput = {
  title: string;
  description?: string | null;
  canonicalPath: string;
};

export function indexablePageMetadata({
  title,
  description,
  canonicalPath,
}: IndexablePageMetadataInput): Metadata {
  return {
    title,
    description: description || undefined,
    alternates: {
      canonical: canonicalPath,
    },
    robots: {
      index: true,
      follow: true,
      googleBot: {
        index: true,
        follow: true,
        "max-image-preview": "large",
        "max-snippet": -1,
        "max-video-preview": -1,
      },
    },
  };
}

export function noindexPageMetadata(title: string): Metadata {
  return {
    title,
    robots: {
      index: false,
      follow: false,
    },
  };
}
