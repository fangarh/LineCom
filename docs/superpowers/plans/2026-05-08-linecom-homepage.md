# LineCom Homepage Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the approved product-showcase homepage with a primary "подбор под задачу" value proposition, curated popular products, and header navigation links for About and Delivery.

**Architecture:** Keep the homepage as a Next.js server component using existing public catalog API helpers. Add small frontend helpers/components for featured-product selection and homepage sections so a future admin-selected featured-products API can replace the source without redesigning the page.

**Tech Stack:** Next.js App Router, React server components, existing CSS in `globals.css`, existing catalog API client, Vitest/Next build verification.

---

## File Structure

- Modify `apps/front/src/lib/routes.ts`: add `about()` and `delivery()` route helpers.
- Modify `apps/front/src/components/layout/site-header.tsx`: add public nav links `О нас` and `Доставка`, keep account/request links available.
- Create `apps/front/src/lib/homepage/featured-products.ts`: select curated popular products from API results behind a small helper boundary.
- Create `apps/front/src/lib/homepage/featured-products.test.ts`: verify curated selection priorities and fallback behavior.
- Modify `apps/front/src/app/page.tsx`: replace catalog-index homepage with approved selling homepage sections.
- Modify `apps/front/src/app/globals.css`: add homepage-specific layout, hero, featured products, value, category entry, and request-flow styles.
- Create `apps/front/src/app/about/page.tsx`: minimal public page so the new header link does not 404.
- Create `apps/front/src/app/delivery/page.tsx`: minimal public page so the new header link does not 404.

---

### Task 1: Route And Navigation Surface

**Files:**
- Modify: `apps/front/src/lib/routes.ts`
- Modify: `apps/front/src/components/layout/site-header.tsx`
- Create: `apps/front/src/app/about/page.tsx`
- Create: `apps/front/src/app/delivery/page.tsx`

- [ ] Add `routes.about()` and `routes.delivery()`.
- [ ] Update header public nav to include `Каталог`, `О нас`, `Доставка`, `Заявка`.
- [ ] Keep `Мои заявки` available in the header without making it more prominent than public navigation.
- [ ] Add minimal About page with LineCom positioning and a link back to catalog/request.
- [ ] Add minimal Delivery page with delivery/request process copy and a link back to catalog/request.
- [ ] Run the existing header test or frontend tests after changes.

### Task 2: Featured Products Selection Helper

**Files:**
- Create: `apps/front/src/lib/homepage/featured-products.ts`
- Create: `apps/front/src/lib/homepage/featured-products.test.ts`

- [ ] Define a helper that accepts `PublicProductListItem[]` and returns up to 8 featured products.
- [ ] Prioritize products with images and text matching common demand groups: twisted pair, patch cords, optics, optical cross/rack, SFP/media conversion, rack/accessories, installation consumables.
- [ ] Deduplicate products by id.
- [ ] Fall back to image-bearing products, then any products, if the curated groups are underfilled.
- [ ] Add tests for priority matching, deduplication, image preference, and fallback.

### Task 3: Homepage Markup

**Files:**
- Modify: `apps/front/src/app/page.tsx`

- [ ] Keep API loading through `getProducts` and `getCategoryTree`.
- [ ] Request enough products to allow curated selection, then pass them through the helper.
- [ ] Build the hero with the selected "подбор под задачу" copy, catalog CTA, and request CTA.
- [ ] Build the right-side product visual from the first several image-bearing featured products.
- [ ] Build the popular positions section using real product data and existing request button behavior.
- [ ] Build "Почему LineCom", category entry points, and request-flow sections.
- [ ] Render graceful empty states if API data is unavailable.

### Task 4: Homepage Styling And Responsive QA

**Files:**
- Modify: `apps/front/src/app/globals.css`

- [ ] Add styles for the approved product-showcase layout while preserving existing catalog/product/request pages.
- [ ] Keep radii at the existing 8px pattern or smaller.
- [ ] Ensure first viewport has a strong hero and a hint of the next section.
- [ ] Ensure mobile layout has no horizontal overflow or text collisions.
- [ ] Avoid public price/cart/payment/order checkout language.

### Task 5: Verification And Commit

**Files:**
- Verify frontend only.

- [ ] Run `npm.cmd test` from `apps/front`.
- [ ] Run `npm.cmd run build` from `apps/front`.
- [ ] Run a scope search for forbidden commerce/payment/order language in new homepage/navigation files.
- [ ] Start or reuse the local frontend server and inspect `/`, `/about`, `/delivery`, and `/catalog`.
- [ ] Capture desktop and mobile screenshots for visual QA.
- [ ] Commit implementation with message `feat: add selling homepage`.
- [ ] Push branch or merge to `main` according to user direction.
