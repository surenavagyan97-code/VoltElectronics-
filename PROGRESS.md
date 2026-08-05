# Volt Electronics — build progress

Full plan: `C:\Users\suren.avagyan\.claude\plans\snug-baking-orbit.md`
Mockup: `Electronics Store mockup\design_handoff_electronics_store\` (open `Electronics Store.dc.html` in a browser to see it)

Stack (agreed): .NET 10 Web API + EF Core + MSSQL · Angular 20 (signals, standalone) · **Ameriabank vPOS** redirect payments (Armenian gateway; Visa/MC/ArCa; replaces the earlier Stripe plan) with a built-in **Fake dev gateway** for local runs · accounts **and** guest checkout · full-stack docker-compose.

## ✅ Done

**Scaffolding**
- `backend/` solution: Domain / Application / Infrastructure / Api + Tests projects, all wired; builds clean on .NET 10.0.103
- `frontend/` Angular 20 workspace (`volt-electronics`); `npm run build` passes
- `docker-compose.yml` (mssql + api + web), `backend/Dockerfile`, `frontend/Dockerfile` + `nginx.conf` (SPA + `/api`, `/uploads`, `/swagger` proxy), `.env.example`, `.gitignore`

**Backend — data layer (complete)**
- Entities: Category, Product (+Images, +Specs), Cart/CartItem (guest via client GUID), Order/OrderItem (price snapshots, shipping address, `PaymentId`/`PaymentProvider`, `CartId` for post-payment cart clearing), RefreshToken; enums ProductStatus (now incl. `Archived`), OrderStatus
- `AppDbContext` (IdentityDbContext, precision/indexes configured); migrations: `InitialCreate`, `ReplaceStripeWithPaymentProvider` (renames `StripePaymentIntentId` → `PaymentId`, adds `PaymentProvider`)
- `DbSeeder`: roles (Admin/Customer), admin user `admin@volt.local` (password `Admin123$`, overridable via `Seed:AdminPassword`), the mockup's 9 categories + 10 products with specs/descriptions, 32 demo orders over 30 days for the admin dashboards
- API startup: migrate + seed with retry loop (docker-friendly)

**Backend — auth (complete)**
- ASP.NET Identity + JWT bearer; `TokenService` (access token + hashed rotating refresh tokens), `AuthService` (register/login/refresh/logout), `AuthController` (`/api/auth/*`)
- Swagger with Bearer support at `/swagger`; CORS for `http://localhost:4200`

**Backend — payments (complete; Stripe fully removed)**
- `Application/Payments/IPaymentProvider` — redirect-gateway abstraction: `InitPaymentAsync` (returns gateway pay-page URL) + `VerifyCallbackAsync` (server-side verification of the return redirect; query params never trusted)
- `Infrastructure/Payments/AmeriaVposProvider` — Ameriabank vPOS REST: `InitPayment` → hosted pay page → `GetPaymentDetails` verification (paid = ResponseCode `"00"` + OrderStatus `2`/deposited). Test env `https://servicestest.ameriabank.am/VPOS`; currency configurable (AMD/USD/EUR/RUB, must match merchant account); `OrderIdOffset` for the bank-assigned test OrderID range
- `Infrastructure/Payments/FakePaymentProvider` — same redirect flow with a self-hosted pay page (`/api/payments/fake/pay`, success/decline buttons); default provider so the whole flow runs with **zero external credentials**
- Selection via `Payments:Provider` = `Fake` | `Ameria` (see `.env.example`); `Payments:CallbackBaseUrl` (public API URL the bank redirects to), `Payments:FrontendBaseUrl` (where shoppers land after the callback)

**Backend — catalog, cart, checkout, orders (complete)**
- `CatalogService` + `ProductsController`/`CategoriesController`: list w/ filters (categories, price bands lt250|250-750|750-1500|gt1500), search, sort (featured/price/rating), paging; detail by slug w/ related products; categories w/ counts; featured
- `CartService` + `CartController`: get/add/update/remove/clear for auth users **and** guests (`X-Cart-Id` GUID header); `POST /api/cart/merge` folds guest cart into user cart after login; stock-aware quantity validation
- `OrderService`: `POST /api/checkout` (server-side re-pricing via `Pricing`, creates PendingPayment order + gateway payment, returns `{orderNumber, paymentUrl}`); `GET /api/payments/callback` (verifies with provider, idempotent: success → Processing + stock decrement + cart cleared, failure → reason recorded; 302-redirects shopper to `/confirmation/{orderNumber}?paid=0|1`); `GET /api/orders` (user), `GET /api/orders/{orderNumber}` (owner or guest via `?email=`); `GET /api/config` (provider name + currency, no secrets in FE bundle)

**Backend — admin (complete; all `[Authorize(Roles="Admin")]` under `/api/admin/*`)**
- Products CRUD (all statuses, unique slug/SKU validation, delete → archive when order history exists) + image upload to `wwwroot/uploads` (5MB, jpg/png/webp/gif) and image delete
- Categories CRUD (delete blocked while products exist)
- Orders: paged list w/ status filter + search, stat cards endpoint, status update
- Analytics: revenue/orders 30d + delta vs prior 30d, AOV, revenue-by-day 7d, top-5 products, low-stock (<20) list

**Frontend (complete, matches mockup)**
- `styles.css` = full Nocturne token set + component classes + `.ph/.row/.col` helpers ported from the handoff
- Core: `ApiClient` (typed), `AuthStore` (signals + localStorage), `CartStore` (signals; guest GUID in localStorage), functional interceptor (JWT attach + `X-Cart-Id`; 401 → one refresh → retry), `authGuard`/`adminGuard`, lazy routes, dev proxy `proxy.conf.json` → `http://localhost:5002`
- Store: layout w/ nav + live cart badge; home (hero, categories, featured); shop (category/price-band checkboxes, debounced search, sort, paging); product detail (gallery, specs, qty add-to-cart, related); cart (qty steppers, totals); login/register (guest cart merges in)
- Checkout: contact + shipping form, payment info card ("redirected to secure payment page"), places order then `window.location.href = paymentUrl`; guest email kept in sessionStorage for confirmation access
- Confirmation `/confirmation/:orderNumber`: polls order briefly (gateway callback races), success + failed-payment states, reloads cart (server cleared it)
- Account: order history table
- Admin: sidebar layout; products table (search/paging/status+stock tags, delete confirm dialog); product form (create/edit, specs editor, image upload/remove — edit mode); categories inline add/edit/delete; orders (stat cards, status seg-filter, search, inline status dropdown); analytics (stat cards w/ deltas, CSS bar chart of 7-day revenue, top products, low-stock table)

**Tests + docs (complete)**
- 20/20 xUnit tests pass (`cd backend; dotnet test`): Pricing math, CartService (guest carts, stock limits, merge incl. clamp-to-stock), OrderService (empty-cart/stock-race rejection, server-side re-pricing, callback success → stock decrement + cart clear, decline → reason kept + cart kept, replay idempotency, guest email access control), FakePaymentProvider
- README.md written (quick start, Ameria vPOS onboarding, architecture)

**E2E smoke test (passed 2026-08-05, Fake gateway)**
- Catalog/cart/config endpoints ✓ · checkout → fake pay page → success callback → order Processing, stock 24→22, cart cleared, replay idempotent ✓ · declined callback → PendingPayment + reason, cart kept ✓ · wrong guest email → 404 ✓ · admin login, products/orders/stats/analytics, category create+delete, order status update ✓ · unauthorized admin → 401 ✓ · Angular dev server serves & proxies `/api` ✓
- Fixed during E2E: `/api/admin/analytics` 500 — EF Core can't translate a record-constructor projection inside GroupBy; now projects to an anonymous type and maps in memory (`AdminService.GetAnalyticsAsync`)

## ⬜ Left to do

1. **Visual parity pass** vs mockup in a real browser (all pages built to match, but not eyeballed yet)
2. **Full docker compose run** (`docker compose up --build`) — mssql-only + local API/FE verified; the containerized api/web images not yet built
3. *(Optional)* Request Ameriabank vPOS test credentials (vpos@ameriabank.am) and verify the real gateway path with `PAYMENTS_PROVIDER=Ameria`

## How to run

```powershell
# DB only
docker compose up mssql -d          # needs .env (already created from .env.example)
# API (applies migrations + seeds on start) — http://localhost:5002, swagger at /swagger
cd backend; dotnet run --project src/VoltElectronics.Api
# FE dev server with /api proxy — http://localhost:4200
cd frontend; npm start
```

- Payments default to the **Fake gateway** — checkout redirects to a local pay page with "Pay now" / "Simulate declined card" buttons; no credentials needed.
- To use real Ameriabank vPOS test env: set `Payments:Provider=Ameria` + `Payments:Ameria:*` credentials (or the `AMERIA_*` vars from `.env.example` under docker).
- Admin: `admin@volt.local` / `Admin123$` → `/admin`.
- Note: `Payments:CallbackBaseUrl` must be the API's public URL (`http://localhost:5002` dev / `http://localhost:8080` compose) — the gateway redirects the shopper's browser there.
