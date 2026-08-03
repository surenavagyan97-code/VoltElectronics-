# Volt Electronics — build progress

Full plan: `C:\Users\suren.avagyan\.claude\plans\snug-baking-orbit.md`
Mockup: `Electronics Store mockup\design_handoff_electronics_store\` (open `Electronics Store.dc.html` in a browser to see it)

Stack (agreed): .NET 10 Web API + EF Core + MSSQL · Angular 20 (signals, standalone) · Stripe Elements (Payment Intents + webhook) · accounts **and** guest checkout · full-stack docker-compose.

## ✅ Done

**Scaffolding**
- `backend/` solution: Domain / Application / Infrastructure / Api + Tests projects, all wired; builds clean on .NET 10.0.103
- `frontend/` Angular 20 workspace (`volt-electronics`); `npm run build` passes (still the default scaffold UI)
- `docker-compose.yml` (mssql + api + web), `backend/Dockerfile`, `frontend/Dockerfile` + `nginx.conf` (SPA + `/api`, `/uploads`, `/swagger` proxy), `.env.example`, `.gitignore`

**Backend — data layer (complete)**
- Entities: Category, Product (+Images, +Specs), Cart/CartItem (guest via client GUID), Order/OrderItem (price snapshots, shipping address, Stripe PI id, `CartId` for post-payment cart clearing), RefreshToken; enums ProductStatus, OrderStatus
- `AppDbContext` (IdentityDbContext, precision/indexes configured), `InitialCreate` migration generated
- `DbSeeder`: roles (Admin/Customer), admin user `admin@volt.local` (password `Admin123$`, overridable via `Seed:AdminPassword`), the mockup's 9 categories + 10 products with specs/descriptions, 32 demo orders over 30 days for the admin dashboards
- API startup: migrate + seed with retry loop (docker-friendly)

**Backend — auth (complete)**
- ASP.NET Identity + JWT bearer; `TokenService` (access token + hashed rotating refresh tokens), `AuthService` (register/login/refresh/logout), `AuthController` (`/api/auth/*`)
- Swagger with Bearer support at `/swagger`; CORS for `http://localhost:4200`
- Config: `appsettings.Development.json` has local dev conn string (`localhost,1433`, sa / `VoltDev!Passw0rd`) and dev JWT key; production values come from env vars (see `.env.example`)

**Backend — application contracts (started)**
- `Application/Common`: `PagedResult<T>`, `Pricing` (flat $24 shipping, 8.75% tax — single source of truth), `Slug`
- `Application/Catalog`: DTOs + `ICatalogService` (list w/ filters/search/sort/price-bands, detail by slug, categories, featured)
- `Application/Cart`: DTOs + `ICartService` (get/add/update/remove/clear + `MergeAsync` for guest→user on login); `CartKey` = UserId or guest GUID from `X-Cart-Id` header

## ⬜ Left to do (in order)

1. **Catalog BE impl** — `CatalogService` in Infrastructure + `ProductsController`/`CategoriesController`
2. **Cart BE impl** — `CartService` + `CartController` (auth users + `X-Cart-Id` guests, merge endpoint called after login)
3. **Checkout + Stripe BE** — `IOrderService`/`OrderService`: `POST /api/checkout` (re-price server-side, create Order PendingPayment + PaymentIntent, return clientSecret + orderNumber); `POST /api/webhooks/stripe` (signature-verified; succeeded → Processing, decrement stock, clear cart via `Order.CartId`; failed → record reason); `GET /api/orders`, `GET /api/orders/{orderNumber}` (owner or guest `?email=`); `GET /api/config` returning Stripe publishable key (so FE needs no build-time key)
4. **Admin BE** — products CRUD + image upload (`wwwroot/uploads`), categories CRUD, orders list/status-update + stat cards, analytics endpoint (revenue 30d + delta, orders, AOV, low-stock<20, revenue-by-day 7d, top products) — all `[Authorize(Roles = "Admin")]`
5. **FE foundation** — port Nocturne `styles.css` tokens + component classes into `src/styles.css` (+ `.ph/.row/.col` helpers from mockup); core services: `ApiClient`, `AuthStore`, `CartStore` (signals), interceptors (JWT attach, 401→refresh→retry); routes + store layout (nav bar w/ cart badge)
6. **FE store pages** — home, shop listing (filters/search/sort), product detail, cart, login/register
7. **FE checkout** — `@stripe/stripe-js` Payment Element styled with Nocturne tokens, confirmation page polling order status
8. **FE account** — order history page
9. **FE admin** — sidebar layout + products table/form, categories, orders, analytics (CSS bar chart)
10. **Polish** — unit tests (Pricing, cart merge, checkout stock validation), README (setup, `stripe listen --forward-to localhost:5000/api/webhooks/stripe`, admin creds), full `docker compose up --build` E2E with Stripe test card `4242 4242 4242 4242`, visual parity pass vs mockup

## How to run what exists today

```powershell
# DB only
docker compose up mssql -d          # needs .env (copy .env.example)
# API (applies migration + seeds on start)
cd backend; dotnet run --project src/VoltElectronics.Api   # swagger at http://localhost:5xxx/swagger
# FE (default Angular scaffold for now)
cd frontend; npm start
```

Working auth endpoints: `POST /api/auth/register | login | refresh | logout`.
