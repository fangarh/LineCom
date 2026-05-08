# LineCom Homepage Design

## Goal

Build a polished selling homepage for LineCom that presents the company as a B2B supplier of cable, optical, SCS, rack, and installation components, while keeping the existing request-based commerce model: no public prices, cart, online payment, or order checkout.

The homepage should also make "подбор под задачу" a primary value proposition: LineCom is not only a catalog, but a supplier that helps assemble a practical set of positions for a concrete installation or procurement task.

## Approved Direction

Use direction A, "product showcase":

- strong first viewport with LineCom positioning and two primary actions: go to catalog and build a request;
- visual emphasis on real catalog products and popular categories;
- a curated "popular positions" section for the first release;
- a visible "подбор под задачу" message that connects products into a practical request scenario;
- clear reasons to choose LineCom;
- navigation links for `О нас` and `Доставка`, with the dedicated pages implemented separately.

## Homepage Structure

The homepage should replace the current catalog-like landing screen with these sections:

1. Hero
   - H1: LineCom helps assemble cable, optical, and network component requests for a concrete task without procurement noise.
   - Supporting copy explains B2B selection, request flow, working with organizations, and the ability to describe a task rather than manually know every component in advance.
   - Primary CTA links to `/catalog`.
   - Secondary CTA links to `/request`.
   - The right side uses a product-led visual composition based on actual catalog images where available.

2. Popular Positions
   - Show a curated set of 6 to 8 products.
   - Products should prioritize common demand groups: twisted-pair cable, patch cords, optical adapters or pigtails, optical cross/rack items, SFP/media conversion, rack or installation accessories.
   - For this release, selection can be implemented in frontend code by matching category/product text and requiring a product image where possible.
   - Future admin selection should be isolated behind a helper/component boundary so the UI can later consume an admin-managed featured-products API without redesign.

3. Why LineCom
   - Explain practical buyer value:
     - product selection for real installation tasks, such as SCS line assembly, optical connection, rack installation, patching, and mounting consumables;
     - request-based B2B workflow;
     - cable, optical, SCS, rack, and installation categories in one catalog;
     - support for recurring organization purchases.

4. Category Entry Points
   - Show several high-level catalog directions with links to the catalog or category pages when available.
   - Use restrained, scan-friendly cards or rows; no nested card layouts.

5. Request Flow
   - Briefly explain the flow: choose positions or describe the task, add quantities/comments, send request, get follow-up.
   - Keep wording clear that price and availability are clarified through the request process.
   - This section should make it obvious that a buyer can ask LineCom to help complete the set of missing components instead of treating the catalog as a self-service checkout.

## Navigation

Update the shared header navigation:

- `Каталог` -> `/catalog`
- `О нас` -> `/about`
- `Доставка` -> `/delivery`
- `Заявка` -> `/request`
- `Мои заявки` remains available, but can be less visually dominant than the public navigation links.

The `/about` and `/delivery` pages can be minimal follow-up pages or separate follow-up implementation, depending on the implementation plan. The homepage task should not expand into full copywriting for these pages unless explicitly requested.

## Data Flow

- Keep homepage as a Next.js server component.
- Use existing `getProducts` and `getCategoryTree` public API helpers.
- Add a small frontend-only helper for selecting featured products from the returned product list.
- If API data is unavailable, render a graceful homepage without failing the whole page: keep hero and value sections visible, and show a concise empty state for popular positions.
- Do not introduce a new backend endpoint for this first release.

## Visual System

Follow existing LineCom visual language:

- graphite/dark header;
- yellow accent;
- light industrial gray page background;
- compact B2B typography;
- border radius no larger than the existing 8px pattern for cards/buttons;
- real product images where possible, no generic stock-like hero image.

The page should feel more polished than the current catalog entry screen, but still operational and B2B-focused rather than a decorative marketing page.

## Out Of Scope

- Admin UI for choosing homepage products.
- New backend schema or API for featured products.
- Full `О нас` and `Доставка` content pages.
- Public prices, cart, payment, online order checkout, or stock promises.

## Acceptance Criteria

- Homepage has a distinct selling hero and no longer looks like a plain catalog index.
- Several popular products render from real catalog data.
- Header includes `О нас` and `Доставка`.
- Existing catalog, product, request, auth, and account flows continue to work.
- Mobile layout has no horizontal overflow or text collisions.
- Frontend tests and production build pass.
