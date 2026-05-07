export const routes = {
  home: () => "/",
  catalog: () => "/catalog",
  category: (slug: string) => `/catalog/${encodeURIComponent(slug)}`,
  product: (slug: string) => `/products/${encodeURIComponent(slug)}`,
  request: () => "/request",
  login: (returnTo?: string) => `/auth/login${returnTo ? `?returnTo=${encodeURIComponent(returnTo)}` : ""}`,
  register: (returnTo?: string) => `/auth/register${returnTo ? `?returnTo=${encodeURIComponent(returnTo)}` : ""}`,
  accountProfile: () => "/account/profile",
  accountRequests: () => "/account/requests",
  accountRequest: (number: string) => `/account/requests/${encodeURIComponent(number)}`,
};
