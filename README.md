# Volt Electronics

A full-stack electronics store: **.NET 10 Web API** (EF Core + MSSQL, ASP.NET Identity + JWT) and an **Angular 20** storefront + admin panel (signals, standalone components), styled after the Nocturne design system mockup in `Electronics Store mockup/`.

Payments go through **Ameriabank vPOS** (Armenian payment gateway — Visa, Mastercard and ArCa cards, AMD/USD/EUR/RUB merchant accounts) using its hosted-page redirect flow. A built-in **fake dev gateway** with the identical flow lets you run everything locally with zero credentials.

## Features

**Storefront** — home (hero, categories, featured), shop with filters/search/sort/paging, product detail with gallery + specs + related items, cart (guest carts via client GUID; merged into the account on login), checkout with guest **or** account flow, redirect payment, confirmation page, order history.

**Admin** (`/admin`, role-gated) — products CRUD with image upload and spec editor, categories CRUD, orders with stat cards + status management, analytics (30-day revenue/orders with deltas, 7-day revenue chart, top products, low-stock alerts).

## Quick start (Docker)

```bash
cp .env.example .env        # defaults work out of the box
docker compose up --build
```

- Store: http://localhost — API/Swagger: http://localhost:8080/swagger
- Admin login: `admin@volt.local` / `Admin123$` (change via `ADMIN_PASSWORD` in `.env`)
- Payments use the **fake gateway** by default: checkout redirects to a local pay page with **Pay now** / **Simulate declined card** buttons.

The API applies migrations and seeds demo data (10 products, 9 categories, 32 demo orders) on first start.

## Local development

```powershell
docker compose up mssql -d                            # database only
cd backend; dotnet run --project src/VoltElectronics.Api   # API on http://localhost:5002 (+ /swagger)
cd frontend; npm install; npm start                   # Angular on http://localhost:4200 (proxies /api + /uploads)
```

Run tests:

```powershell
cd backend; dotnet test        # pricing, cart merge, checkout/stock, payment callback (idempotency incl.)
```

## Payments: Ameriabank vPOS

The flow (both providers): `POST /api/checkout` re-prices the cart server-side, creates a `PendingPayment` order, calls the gateway's `InitPayment` and returns the hosted pay-page URL. The browser is redirected there; after the attempt the gateway sends the shopper back to `GET /api/payments/callback`, which **verifies the result server-side** (`GetPaymentDetails` — the query string is never trusted), marks the order paid (decrement stock, clear cart) or failed, and redirects to the confirmation page.

To use the real Ameriabank test environment:

1. Request test credentials from Ameriabank (vpos@ameriabank.am) — you'll get a `ClientID`, username/password and a test OrderID range.
2. In `.env` (or `appsettings`):

```bash
PAYMENTS_PROVIDER=Ameria
AMERIA_CLIENT_ID=<guid from the bank>
AMERIA_USERNAME=<username>
AMERIA_PASSWORD=<password>
AMERIA_CURRENCY=USD              # must match your merchant account (AMD for dram)
AMERIA_ORDER_ID_OFFSET=3200000   # shift order ids into your assigned test range
PAYMENTS_CALLBACK_BASE_URL=http://localhost:8080   # public URL of the API
```

For production, point `AMERIA_BASE_URL` at `https://services.ameriabank.am/VPOS` and use a publicly reachable `PAYMENTS_CALLBACK_BASE_URL` (the bank redirects the shopper's browser there).

Adding another Armenian provider (Idram, Telcell, ArCa iPay) means implementing the two-method `IPaymentProvider` interface (`Application/Payments`) and registering it in `Infrastructure/DependencyInjection.cs`.

## Architecture

```
backend/
  src/VoltElectronics.Domain          entities + enums (no dependencies)
  src/VoltElectronics.Application     DTOs + service interfaces (ICatalogService, ICartService,
                                      IOrderService, IAdminService, IPaymentProvider, Pricing)
  src/VoltElectronics.Infrastructure  EF Core (AppDbContext, migrations, seeder), service
                                      implementations, auth (Identity/JWT), payment providers
  src/VoltElectronics.Api             controllers, JWT setup, Swagger, static /uploads
  tests/VoltElectronics.Tests         xUnit + SQLite in-memory
frontend/
  src/app/core                        ApiClient, AuthStore/CartStore (signals), JWT+refresh
                                      interceptor, guards
  src/app/store                       storefront pages
  src/app/admin                       admin pages
  src/styles.css                      Nocturne design tokens + components (from the mockup handoff)
```

Notes:

- **Guest carts**: the client generates a GUID, sent as `X-Cart-Id`; login merges it into the user cart (`POST /api/cart/merge`).
- **Money math** lives in one place (`Pricing`): flat $24 shipping, 8.75% tax, always recomputed server-side at checkout.
- **Order history is immutable**: order items snapshot name/price; deleting a sold product archives it instead.
- The payment callback is **idempotent** — replays/refreshes can't double-decrement stock.
- The frontend gets runtime config from `GET /api/config`; no secrets or environment values are baked into the bundle.
