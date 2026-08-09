# Handoff: Electronics Store — Storefront + Admin

## Overview
A B2B-leaning e-commerce web app for selling electronics (laptops, phones, audio, TVs, wearables, cameras, tablets, monitors, gaming). Two areas: a customer storefront and an admin back-office for managing the catalog and orders.

## About the Design Files
The files in this bundle are **design references built in HTML** (`Electronics Store.dc.html` + `styles.css`) — they show the intended look, layout, and interaction states, not production code to copy directly. The task is to **recreate these screens in the target codebase's existing environment** (React, Vue, etc. — whatever stack the project already uses), following its established component patterns, routing, and state management. If no environment exists yet, choose the framework best suited to the project and implement there.

`Electronics Store.dc.html` is a single static file with a top-right Store/Admin toggle switching between all screens (all screens are always in the DOM, gated by JS state) — that mechanism is a prototyping convenience only. In the real app these should be real routes/pages.

## Fidelity
**High-fidelity.** Colors, typography, spacing, and component states are final per the Nocturne design system (`nocturne-design-system-readme.md`, `styles.css`). Recreate pixel-accurately using the codebase's component library if it already implements Nocturne, or using the tokens/CSS classes in `styles.css` directly.

## Design System — Nocturne
Dark, compact, corporate/trustworthy tone. Key rules (full detail in `nocturne-design-system-readme.md`):
- Dark ground `--color-bg #161826`, text `--color-text #e9e9ed`, single accent `--color-accent #9184d9` (blurple). Tonal ramps 100–900 for neutral/accent.
- Type: Inter for headings and body, weight 500 max for headings (hierarchy = size/space, not bold).
- Buttons are **outlined**, never solid-filled (`.btn-primary` = accent border + transparent fill).
- 8px base radius, compact 0.7× spacing scale.
- Rules/dividers fade at both ends (`.hr`, table row rules) rather than stopping cleanly.
- Photos go through `.lighten` (mix-blend-mode: lighten) — not used here since all imagery is placeholder.
- Never flood large areas with the accent color; it's a line/glow accent only (exception: the home hero band, which uses `--color-section` tokens intentionally, per system's "one full-bleed stat/section band" allowance).

## Screens / Views

### Store — Home (`showHome`)
- Top nav: brand "Volt Electronics" + Home/Shop/Orders links + search/cart(badge)/account icons, right-aligned via `flex:1` spacer.
- Hero: full-bleed gradient band (`--color-section` → `--color-section-glow`, 135deg), left-aligned copy (kicker tag, H1, subcopy, 2 CTAs) + placeholder product image right.
- "Shop by category": 6-column grid of category cards (image placeholder + name + product count), clicking goes to listing.
- "Featured products": 4-column grid of product cards (image, optional badge tag, title, category+rating meta, price). Clicking goes to detail.
- Footer: 1-line copyright, low-opacity.

### Store — Product listing (`showListing`)
- Left sidebar (220px fixed): category checkboxes (from category data), price-range checkboxes (4 static buckets), "Clear filters" button.
- Main: header row (title + count, search input, sort select), 3-column product card grid — same card as featured but adds stock count and a small cart-icon button per card.

### Store — Product detail (`showDetail`)
- Breadcrumb text.
- 2-column: left = large image placeholder + 4 thumbnail placeholders; right = badge, H1, star rating + review count + stock, price row (price / struck old price / "Save X%" tag), fading `.hr`, description paragraph, spec rows (label/value, bottom-bordered), qty input + "Add to cart" primary button (full width of remaining space), secondary "Request bulk quote" block button.
- "You may also like": 4-card row, no badges, simpler (image, title, price).

### Store — Cart (`showCart`)
- H2 with item count.
- Left: cart line items (image, name, category, "Remove" ghost link with trash icon; qty stepper as a 3-cell `.seg`; line total). Divider between rows. "Continue shopping" ghost link below.
- Right (340px card): order summary — subtotal, shipping ($24 flat), estimated tax (8.75% of subtotal), fading `.hr`, total, primary "Proceed to checkout" block button.

### Store — Checkout (`showCheckout`)
- Left: Shipping address form (2-col grid: name, company, street [span 2], city, state, zip, phone) + Payment method (`.seg` radio: Card/PayPal/Purchase order) + card fields (number [span 2], expiry, CVC).
- Right (340px card, sticky-style): line-item recap, total, primary "Place order", ghost "Back to cart".

### Store — Order confirmation (`showConfirm`)
- Centered, 560px max-width. Check-icon in a circular accent-bordered ring, H2 "Order confirmed", order number + note, summary card (total / estimated delivery / shipping-to), two buttons (View order primary, Continue shopping secondary).

### Store — Account / order history (`showAccount`)
- Left (200px): avatar-initial card (name + email) + 4-item nav list (Order history active, Addresses, Payment methods, Account settings — static, not wired).
- Right: order history `.table` — Order/Date/Items/Total/Status columns, status as colored `.tag` (Delivered = accent tint, Shipped = neutral tint, Processing = accent outline, Cancelled = dim neutral).

### Admin — layout shell
- Persistent left sidebar (220px, `--color-surface` bg): brand "Volt Admin" + 4 nav items with icon (Products/Categories/Orders/Analytics), active item gets accent text + accent-tinted background pill.
- Main content area, 28px/36px padding.

### Admin — Products (`showAdminProducts`, default)
- Header: H2 + primary "Add product" button (icon+label) → goes to form.
- Filter row: search input, category select, status select (static, not wired).
- `.table`: thumbnail placeholder, name, category, price, stock (as `.tag`, accent-tinted if <20 units = low stock, else neutral), status tag (static "Active"), row actions (edit → form, delete icon buttons, ghost style).

### Admin — Add/Edit product (`showAdminForm`)
- Back ghost-link to product list. H2 "Add product".
- 2-col form grid: name [span 2], SKU, category select, price, compare-at price, stock qty, status (`.seg` radio Active/Draft), description textarea [span 2], 3 image-upload placeholder slots [span 2], 3 spec key/value input rows [span 2].
- Footer actions: primary "Save product", secondary "Discard" (both currently just navigate back to list — wire to real submit/cancel).

### Admin — Orders (`showAdminOrders`)
- H2 "Orders". 4-card stat row (Total orders, Pending, Shipped, Revenue 30d).
- Status filter `.seg` (All/Pending/Shipped/Delivered — static, not wired).
- `.table`: Order/Customer/Date/Items/Total/Status (tag colored same scheme as account order history).

### Admin — Categories (`showAdminCategories`)
- H2 + primary "Add category" button (not wired).
- `.table`: Category/Products count/Total stock/Status tag ("Low stock" if stock<20 else "In stock")/row actions (edit, delete icons).

### Admin — Analytics (`showAdminAnalytics`)
- H2. 4-stat card row (Revenue 30d, Orders, AOV, Low stock alerts) with small delta/caption line under each number.
- Card containing a 7-bar CSS bar chart (Mon–Sun, static heights) labeled "Revenue, last 7 days".
- "Top products" `.table`: Product/Category/Units sold/Revenue.

## Interactions & Behavior
- All navigation in the prototype is a client-side state switch (mode: store|admin; storeScreen; adminScreen) — map to real routes, e.g. `/`, `/shop`, `/product/:id`, `/cart`, `/checkout`, `/order/:id/confirmation`, `/account/orders`; `/admin/products`, `/admin/products/new` (and `/admin/products/:id/edit`), `/admin/orders`, `/admin/categories`, `/admin/analytics`.
- Cart quantity stepper (+/−) is visual only in the prototype — wire to real state that recalculates line total, subtotal, tax, total.
- "Add to cart" on product detail, category tiles, and featured/listing cards navigate to cart/listing/detail in the prototype — wire to real add-to-cart and routing logic.
- Product/category/order data is hardcoded placeholder data in the prototype's logic — replace with real API calls.
- No client-side form validation is implemented in checkout or the admin product form — add validation before shipping.
- Hover/active/focus states for buttons, inputs, tags, table rows follow Nocturne's built-in states (see `styles.css` — hover tints from the accent/neutral ramps, `:focus-visible` = 2px accent outline, disabled = 45% opacity). Don't restyle these per component.

## State Management
Minimal: current nav mode/screen (→ routing), cart items (id, qty), and in the admin form, the in-progress product draft. All product/category/order lists should come from a backend/API in the real app instead of the static arrays in the prototype.

## Design Tokens
See `styles.css` `:root` for the authoritative values. Summary:
- Colors: bg `#161826`, surface `#232532`, text `#e9e9ed`, accent `#9184d9`, divider = 16% text on transparent. Neutral and accent ramps 100–900 (light→dark), see file.
- Section/divider band colors: `--color-section #262a60`, `--color-section-glow #353b80`, `--color-section-ghost #4c5397` (hero/divider bands only, not general UI).
- Font: Inter (heading weight 500), body 15px/1.55. Heading sizes: h1 42px, h2 32px, h3 25px, h4 20px, h5 16px, h6 13px (uppercase, 0.08em tracking).
- Spacing scale (0.7× density): 2.8 / 5.6 / 8.4 / 11.2 / 16.8 / 22.4px (space-1…8).
- Radius: sm 4px, md 8px, lg 14px.
- Shadows: sm/md/lg — hairline edge + ambient darkness (see file), tuned for the dark ground.

## Assets
All product/category imagery is a striped placeholder box (`.ph` class — diagonal repeating gradient in neutral tones) with a monospace label describing the intended shot (e.g. "laptop — product photo"). Replace with real product photography; per Nocturne guidance, shoot on dark backgrounds and wrap in `.lighten` once real.

Icons are inline SVGs, hand-drawn to match the Phosphor icon style Nocturne specifies (stroke-based, 1.6px stroke, currentColor) — swap for actual Phosphor icons (phosphoricons.com) in the real build.

## Theming — dark (default) and light

The app ships **both a dark and a light theme**. They are the same markup: only the design-system tokens change.

**How it works.** `styles.css` declares Nocturne's tokens on `:root` (dark). `nocturne-light-theme.css` re-declares them under a `[data-theme="light"]` selector. Put that attribute on any container and everything inside it — cards, buttons, inputs, tables, tags, nav — switches over, because all component CSS reads the tokens rather than literal colors.

```html
<link rel="stylesheet" href="styles.css">
<link rel="stylesheet" href="nocturne-light-theme.css">

<body data-theme="light">   <!-- omit the attribute (or use "dark") for the default dark theme -->
```

In the real app, drive that attribute from a theme store / context and persist the choice (localStorage or user settings); optionally initialize from `prefers-color-scheme`. Do **not** fork components or write per-theme component styles — if something doesn't adapt, it means that element hardcodes a color instead of using a token, and the fix is to move it onto a token.

**The one rule when adding new UI: choose ramp steps by role, not by number.** The light theme reverses each tonal ramp end-for-end (100↔900, 200↔800, …), keeping hue and chroma fixed, so every token holds its *relative* value against the ground in both themes. A step picked as a foreground-on-surface (this design uses `--color-accent-300`) stays readable in both. A step picked only because it looked right on dark (e.g. `--color-accent-700` as text) inverts into a near-invisible pale tint on white. Accent fills in this design use `--color-accent-500`.

In the prototype the Dark/Light control sits in a small bar above the app chrome — that bar is a **prototype affordance only**. In production, put the theme control wherever it belongs in your product (account/settings menu) and drop the bar.

## Files
- `Electronics Store.dc.html` — full design, all screens (view source for exact markup/classes/inline styles per screen).
- `styles.css` — Nocturne design-system tokens + component CSS classes used throughout (dark theme on `:root`).
- `nocturne-light-theme.css` — light-theme token overrides under `[data-theme="light"]`, with derivation notes.
- `nocturne-design-system-readme.md` — full design-system usage guide (color/type/spacing rules, do's/don'ts).
