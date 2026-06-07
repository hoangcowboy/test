# Feature: Shop Fullstack — Mini E-Commerce

> **Status:** Approved by PO
> **Priority:** High
> **Sprint:** 2026-05-19
> **Story Points:** 21 (epic)
> **Stack:** Monorepo — `ShopApi/` (.NET 8) + `ShopWeb/` (React 18 + Vite + TS + Tailwind + shadcn)

## Why

POC cho khách hàng SMB cần một storefront tối giản: search → xem chi tiết → bỏ giỏ → checkout, plus admin quản lý sản phẩm. Mục tiêu là demo được khả năng multi-agent của Copilot từ folder trống → app chạy end-to-end (BE+FE) trong < 10 phút.

## What — Backend (`ShopApi/`)

### Endpoints

**User endpoints (no auth):**
```
GET    /api/products                  ?category=&page=&pageSize=  (list all, no search)
GET    /api/products/search           ?q=&category=&minPrice=&maxPrice=&sort=&page=&pageSize=
GET    /api/products/{id}             (with full images array)
GET    /api/products/featured         (top 4 latest)

GET    /api/categories                (with productCount each)
GET    /api/categories/{slug}/products  (products of single category, paged)

POST   /api/cart                      → returns { cartId }
GET    /api/cart/{cartId}             → items + total + count
POST   /api/cart/{cartId}/items       (productId, quantity)
DELETE /api/cart/{cartId}/items/{productId}
PATCH  /api/cart/{cartId}/items/{productId}   (quantity)
DELETE /api/cart/{cartId}             (clear all items)

POST   /api/orders                    (cartId, customer { name, email, phone, address, note? })
                                      → 201 with orderId, decrement stock atomically
GET    /api/orders/{id}               (single order detail — for confirmation page)

GET    /health                        (status check)
```

**Admin endpoints (require header `X-Admin: true`):**
```
GET    /api/admin/stats               → { totalProducts, totalOrders, revenue, lowStockCount }
GET    /api/admin/orders/recent       → 5 latest orders for dashboard

# Products
POST   /api/products                  (create — with images array)
PUT    /api/products/{id}             (update — with images array replace)
DELETE /api/products/{id}             (soft or hard delete)

# Categories
POST   /api/categories                (name, slug auto if not provided)
PUT    /api/categories/{id}
DELETE /api/categories/{id}           → 409 if has products

# Orders
GET    /api/admin/orders              ?status=&page=&pageSize=  (full list with filter)
PATCH  /api/admin/orders/{id}/status  (newStatus: Pending|Processing|Shipped|Delivered|Cancelled)
```

### Domain

- `Product { Id, Name, Description, Price, Stock, CategoryId, CreatedAt }` (NO single ImageUrl — see ProductImage below)
- `ProductImage { Id, ProductId, Url, AltText, DisplayOrder, IsPrimary }` — 1 product has N images (N: 1 → 5)
- `Category { Id, Name, Slug }`
- `Cart { Id, CreatedAt }` + `CartItem { CartId, ProductId, Quantity, UnitPrice }`
- `Order { Id, CartId, CustomerName, CustomerEmail, ShippingAddress, Total, CreatedAt, Status }`

### BE Acceptance Criteria

- **AC-BE-1:** Search "iPhone" → match Name ∪ Description, sort relevance default
- **AC-BE-2:** `minPrice`/`maxPrice` filter chính xác
- **AC-BE-3:** Category filter by `categoryId`
- **AC-BE-4:** Pagination meta đúng (`total`, `page`, `pageSize`, `totalPages`)
- **AC-BE-5:** Query < 2 ký tự → 400 `ProblemDetails`
- **AC-BE-6:** Unicode tiếng Việt (`điện thoại`) hoạt động
- **AC-BE-7:** `POST /orders` decrement stock atomic; stock không đủ → 409 `Conflict`
- **AC-BE-8:** Cart guest (không auth) — `cartId` là Guid trả về client lưu localStorage
- **AC-BE-9:** Admin endpoints chỉ là route guard placeholder (header `X-Admin: true`) — auth thực không scope
- **AC-BE-10:** Test coverage ≥ 80% cho Service layer, 100% endpoint coverage
- **AC-BE-11:** SQL injection an toàn (parameterized everywhere)
- **AC-BE-12:** CORS allow `http://localhost:5173`
- **AC-BE-13:** Seed data on first run: **≥ 100 products** across ≥ 8 categories, each product có **1-5 images** (mix primary + secondary). Idempotent — chạy lại không duplicate.
- **AC-BE-14:** `GET /api/products/{id}` response includes `images: [{url, altText, isPrimary, displayOrder}]` array sorted by displayOrder.
- **AC-BE-15:** Image URLs phải là **ảnh thật relevant** theo product — dùng Unsplash Source API: `https://source.unsplash.com/600x600/?{keywords}&sig={slug}-{i}` (keyword-matched real photos, no API key, sig param đảm bảo mỗi ảnh khác nhau). Seed.cs đã có Catalog map name→keywords cho 100 products.
- **AC-BE-16:** `GET /api/admin/stats` trả về `totalProducts`, `totalOrders`, `revenue` (sum totals), `lowStockCount` (products with stock < 5). Yêu cầu `X-Admin: true` header, không thì 401.
- **AC-BE-17:** `GET /api/admin/orders` hỗ trợ filter `?status=` (Pending|Processing|Shipped|Delivered|Cancelled) + pagination. `X-Admin` required.
- **AC-BE-18:** `PATCH /api/admin/orders/{id}/status` update status, validate transition (vd: Delivered không quay về Pending), return 200 với order updated. Required `X-Admin`.
- **AC-BE-19:** `DELETE /api/categories/{id}` returns 409 ProblemDetails nếu category còn sản phẩm; 204 nếu xóa thành công.
- **AC-BE-20:** Order entity có `Status` field (default `Pending`), `Items` (line items with snapshot UnitPrice, Quantity), `Total` (calculated), `Note` (optional from checkout).
- **AC-BE-21:** Admin endpoints không có `X-Admin: true` → 401 Unauthorized ProblemDetails (không phải 403, để rõ ràng).

### BE Non-functional

- p95 latency < 200ms với 10k products, SQLite InMemory cho demo
- pageSize max = 100
- Cancellation token wired end-to-end

## What — Frontend (`ShopWeb/`)

### Pages (routes)

**USER routes** (storefront — không cần auth):
```
/                          HomePage          (hero + featured + categories)
/products                  ProductListPage   (search + filter + sort + grid + pagination)
/products/:id              ProductDetailPage (gallery + desc + qty + add-to-cart)
/category/:slug            CategoryPage      (products of single category)
/cart                      CartPage          (full cart page, edit qty, remove, total)
/checkout                  CheckoutPage      (form + submit)
/orders/:id                OrderConfirmationPage  (thank-you, order summary, "back home")
/404                       NotFoundPage      (catch-all)
```

**ADMIN routes** (header `X-Admin: true` required — placeholder auth):
```
/admin                     AdminDashboardPage (stats cards + recent orders + sales chart)
/admin/products            AdminProductsPage  (data table: search/filter/CRUD/bulk)
/admin/products/new        AdminProductFormPage  (create — form full với images URLs)
/admin/products/:id/edit   AdminProductFormPage  (edit — same form, prefilled)
/admin/categories          AdminCategoriesPage (list + create/edit/delete dialogs)
/admin/orders              AdminOrdersPage    (list + status filter + update status)
/admin/orders/:id          AdminOrderDetailPage (line items + customer + status timeline)
```

### Global UI elements (apply to ALL pages)

- **Header (`<RootLayout>`):** logo + nav (Products, Categories dropdown) + search input + cart icon w/ badge + dark mode toggle + admin link (if `X-Admin` flag in localStorage)
- **Footer:** © + about + contact + social icons (placeholder)
- **Mobile:** hamburger menu on `< md`, drawer nav, search collapses to icon
- **Toast notifications:** sonner top-right, success/error/info variants
- **Loading:** every async render uses `<Skeleton>` placeholder, never blank
- **Empty state:** every list page handles "no items" with composed `<EmptyState icon msg cta>`
- **Error boundary:** Root-level catches render errors, shows "Something went wrong" + reload button
- **404 page:** SPA route fallback → "Page not found" + back-home button
- **Currency:** VND format `1.234.567 ₫` via `Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND" })`
- **Date:** `Intl.DateTimeFormat("vi-VN", { dateStyle: "medium", timeStyle: "short" })`

### USER FE Acceptance Criteria

#### Home (AC-FE-USER-1..3)
- **AC-FE-USER-1:** HomePage hero banner (background + headline + CTA "Shop now" → /products), featured 4 latest products (with primary image), 8 category cards (clickable → `/category/:slug`)
- **AC-FE-USER-2:** Header search bar global — gõ + Enter → navigate `/products?q=...`
- **AC-FE-USER-3:** Cart icon header có badge số lượng items, click mở Cart Sheet (drawer)

#### Product List (AC-FE-USER-4..8)
- **AC-FE-USER-4:** Search box debounce 300ms, URL syncs `?q=...` (sharable)
- **AC-FE-USER-5:** Filter sidebar: category multi-select, price range (min/max input hoặc slider), in-stock checkbox
- **AC-FE-USER-6:** Sort dropdown: Relevance | Price ↑ | Price ↓ | Name A-Z | Newest
- **AC-FE-USER-7:** Grid responsive: 1 col `< sm`, 2 `sm`, 3 `md`, 4 `lg+`. Card: aspect-square image (primary), name (2-line clamp), price VND, stock badge ("In stock" green / "Low (<5)" orange / "Out" red), click → detail
- **AC-FE-USER-8:** Pagination footer: prev / 1 2 3 ... / next + "Showing X-Y of Z"

#### Product Detail (AC-FE-USER-9..11)
- **AC-FE-USER-9:** Image gallery: primary image (aspect-square ~600px) + thumbnail strip (4 thumbs, click swap primary, keyboard arrow keys nav)
- **AC-FE-USER-10:** Right column: name (h1), category breadcrumb, price VND, stock badge, description (preserved newlines), qty stepper (− / input / +, min 1, max stock), "Add to cart" button (disabled if out-of-stock)
- **AC-FE-USER-11:** Click "Add to cart" → POST /api/cart/.../items → toast success "Added X to cart" + cart badge updates (optimistic)

#### Cart (AC-FE-USER-12..15)
- **AC-FE-USER-12:** Cart Sheet (header icon click) shows compact list: image + name + qty + line total, "View cart" + "Checkout" buttons
- **AC-FE-USER-13:** CartPage full: table-like layout, image + name + price each + qty stepper (immediate save) + line total + remove icon + cart total + "Continue shopping" + "Checkout"
- **AC-FE-USER-14:** Cart persist trong `localStorage` (key `cartId`). Reload page → same cart restored
- **AC-FE-USER-15:** Empty cart state: icon + "Your cart is empty" + "Browse products" CTA

#### Checkout (AC-FE-USER-16..18)
- **AC-FE-USER-16:** Form: name (required, 2-100 chars), email (required, valid), phone (required, regex VN `^(0|\+84)\d{9,10}$`), address (required, min 10 chars), note (optional, max 500)
- **AC-FE-USER-17:** Order summary right column: line items + subtotal + shipping (free for demo) + total. Submit button disabled if invalid OR cart empty
- **AC-FE-USER-18:** Submit → POST /api/orders → on 201: redirect `/orders/:id` + toast success + clear cart. On 409 stock: toast error "Insufficient stock for X" + keep cart. On 400: inline field errors

#### Order Confirmation (AC-FE-USER-19..20)
- **AC-FE-USER-19:** Show order number, customer info, line items, total, status badge, created date. "Back to home" + "Order again" buttons
- **AC-FE-USER-20:** Direct visit `/orders/:id` (refresh) loads order (BE GET endpoint)

#### Cross-cutting USER (AC-FE-USER-21..23)
- **AC-FE-USER-21:** Mobile responsive: all pages render correctly on iPhone SE (375px) — no horizontal scroll, touch targets ≥ 44px
- **AC-FE-USER-22:** Dark mode toggle in header → all surfaces flip correctly, no white flash, persists in `localStorage`
- **AC-FE-USER-23:** 404 page renders for unknown routes (e.g. `/foo`) with "back home" link

### ADMIN FE Acceptance Criteria

#### Admin Dashboard (AC-FE-ADMIN-1..3)
- **AC-FE-ADMIN-1:** `/admin` shows 4 stat cards: Total Products, Total Orders, Revenue (sum), Low Stock (<5) count. Cards link to relevant list page
- **AC-FE-ADMIN-2:** "Recent Orders" table (5 latest) — order #, customer, total, status badge, date. Click row → `/admin/orders/:id`
- **AC-FE-ADMIN-3:** Top-right header has "Back to Storefront" link + admin user badge (placeholder "Admin")

#### Admin Products (AC-FE-ADMIN-4..9)
- **AC-FE-ADMIN-4:** `/admin/products` shows data table: ID (short), thumbnail, name, category, price, stock, actions (Edit / Delete icons). Sort by clicking column headers
- **AC-FE-ADMIN-5:** Search box (debounce 300ms) — filter products by name. Category filter dropdown. Pagination
- **AC-FE-ADMIN-6:** "+ Add Product" button → navigate `/admin/products/new` OR open Dialog (either is OK)
- **AC-FE-ADMIN-7:** Create form fields: name (req), description (textarea), price (numeric, min 0), stock (integer, min 0), category (select), images (array of URL inputs — add/remove rows, mark one as primary, displayOrder auto-set). Submit → POST → toast success → back to list
- **AC-FE-ADMIN-8:** Edit form prefills existing values. Submit → PUT → toast success → list refresh
- **AC-FE-ADMIN-9:** Delete icon → ConfirmDialog "Delete product X?" → if confirm → DELETE → toast success → row removed from table (TanStack Query invalidate)

#### Admin Categories (AC-FE-ADMIN-10..12)
- **AC-FE-ADMIN-10:** `/admin/categories` list: name, slug, product count, edit / delete actions
- **AC-FE-ADMIN-11:** Create dialog: name input → slug auto-generated (kebab-case, editable). Submit → POST
- **AC-FE-ADMIN-12:** Delete: confirm dialog. If category has products → 409 ProblemDetails → toast error "Cannot delete — has X products"

#### Admin Orders (AC-FE-ADMIN-13..15)
- **AC-FE-ADMIN-13:** `/admin/orders` list: order #, customer name, total, status badge, date. Filter by status (Pending / Processing / Shipped / Delivered / Cancelled). Pagination
- **AC-FE-ADMIN-14:** `/admin/orders/:id` detail: customer + line items + total + status. Status dropdown to update — PATCH /api/orders/:id/status → toast success
- **AC-FE-ADMIN-15:** Status badge color-coded: Pending (yellow), Processing (blue), Shipped (purple), Delivered (green), Cancelled (red)

#### Cross-cutting ADMIN (AC-FE-ADMIN-16..17)
- **AC-FE-ADMIN-16:** Admin route guard: visiting `/admin*` without `localStorage.setItem("X-Admin", "true")` → redirect `/` with toast "Admin access required"
- **AC-FE-ADMIN-17:** Hidden trigger: visit `/admin-login` → small form "Enter access key" → submit "demo" → set localStorage flag → redirect `/admin`. (Placeholder, not real auth — but demoable)

### Cross-cutting FE (apply both User + Admin)

- **AC-FE-X1:** Loading: every `useQuery` consumer shows `<Skeleton>` placeholder while pending
- **AC-FE-X2:** Empty: every list shows `<EmptyState>` when 0 items
- **AC-FE-X3:** Error: TanStack Query error → toast.error with retry button (`refetch`)
- **AC-FE-X4:** Dark mode in BOTH user + admin
- **AC-FE-X5:** Accessibility: keyboard nav (Tab/Esc/Enter), aria-label icon buttons, contrast AA, focus-visible ring
- **AC-FE-X6:** TypeScript strict — `npm run build` 0 errors, no `any`, no `// @ts-ignore`
- **AC-FE-X7:** No console.log / console.error in committed code (filter via lint or grep)
- **AC-FE-X8:** Hot reload works on save (Vite HMR — verify in dev)

### UI Quality bar (apply throughout)

- **AC-FE-UI-1 — Typography:** Inter font family (sans-serif), heading scale: h1=2.25rem/700, h2=1.875rem/600, h3=1.5rem/600. Body 1rem/400 line-height 1.5. Tracking tight on headings (`tracking-tight`).
- **AC-FE-UI-2 — Spacing rhythm:** Use Tailwind spacing scale (4/6/8/12/16/24px). Section padding `py-12 md:py-16`. Card padding `p-4 md:p-6`. NEVER pixel-push `[13px]`.
- **AC-FE-UI-3 — Colors:** shadcn slate base + brand accent. Hero gradient `from-brand-500 to-brand-700`. Stock badges: green-600 (in stock), amber-600 (low), red-600 (out). No raw gray-XXX — use semantic tokens (`text-foreground`, `text-muted-foreground`, `border`).
- **AC-FE-UI-4 — Hover & transitions:** All clickable elements have `transition-colors` + `hover:` state. Product cards `hover:shadow-lg transition-shadow duration-200 hover:-translate-y-0.5`. Buttons feedback within 100ms.
- **AC-FE-UI-5 — Image gallery interaction:** Primary image has subtle hover zoom (`hover:scale-105 transition-transform`). Thumbnail strip with selected ring (`ring-2 ring-brand-500`). Smooth crossfade khi swap (`opacity` transition 200ms).
- **AC-FE-UI-6 — Skeleton parity:** Skeleton shapes phải match component thật (cùng aspect-ratio, cùng spacing). Shimmer animation via `animate-pulse`.
- **AC-FE-UI-7 — Empty states with illustration:** Cart empty / search no-results dùng `lucide-react` icon size-12 + heading + description + CTA button. Color `text-muted-foreground`. Centered.
- **AC-FE-UI-8 — Hero section:** HomePage hero phải có: gradient background (slate-900 → brand-700 dark mode, brand-50 → brand-100 light), h1 lớn (text-5xl md:text-6xl tracking-tight), subtitle muted, 2 CTAs (primary + ghost outline). Optional ambient background pattern.
- **AC-FE-UI-9 — Cards:** Product cards dùng shadcn `<Card>` với border subtle, shadow on hover, rounded-xl. Image area aspect-square với rounded-t-xl, content area `p-3 md:p-4`.
- **AC-FE-UI-10 — Form polish:** Form errors hiện inline với icon + red border, success state với green checkmark. Submit button disabled state mờ đi 50%, loading state có spinner + "Processing..." text.
- **AC-FE-UI-11 — Toasts:** Success toast green-tinted, error red-tinted, có icon (lucide CheckCircle / XCircle / AlertCircle). Position top-right, max 3 stacked.
- **AC-FE-UI-12 — Image error fallback:** `<SafeImage>` wrapper component: nếu Unsplash fail load → `onError` swap sang `https://placehold.co/600x600/eee/666?text={altText}` với text encoded. Không bao giờ hiển thị broken img icon trên storefront.
- **AC-FE-UI-13 — Mobile bottom nav (USER):** trên mobile (`< md`), thêm fixed bottom nav với 4 icons: Home / Search / Cart / Account (admin). Tablet+ giữ top nav như cũ.
- **AC-FE-UI-14 — Admin sidebar:** Admin pages có `<AdminLayout>` với sidebar trái (lg+), top bar mobile. Nav items: Dashboard / Products / Categories / Orders với icon + label. Active route highlight.
- **AC-FE-UI-15 — Smooth page transitions:** Route change có fade-in animation 150ms (Framer Motion hoặc Tailwind class). Không phải spinner full-screen, chỉ stagger render.

### FE Non-functional

- Bundle JS gzip < 200kb cho route initial
- Lighthouse Performance ≥ 80 (desktop), Accessibility ≥ 95
- No layout shift trên product card khi image load (`aspect-square`)

## Out of Scope

- ❌ Real auth (only header placeholder cho admin)
- ❌ Payment integration (order là final)
- ❌ Real image upload (dùng placeholder URL)
- ❌ Email notification
- ❌ i18n (English only labels, VN có thể trong content)
- ❌ Mobile native app
- ❌ Inventory webhook, returns/refunds

## Sample requests

### Search

```http
GET /api/products/search?q=phone&minPrice=10000000&sort=relevance&page=1&pageSize=12
```

```json
{
  "items": [
    { "id": "...", "name": "iPhone 16 Pro", "price": 30000000, "stock": 5, "imageUrl": "https://...", "score": 3.0 }
  ],
  "meta": { "total": 47, "page": 1, "pageSize": 12, "totalPages": 4 }
}
```

### Create order

```http
POST /api/orders
{
  "cartId": "9f1b...",
  "customer": { "name": "Nguyen Van A", "email": "a@x.com", "address": "123 Le Loi, Q1, HCM" }
}
→ 201 { "orderId": "...", "total": 30000000, "status": "Pending" }
```

## Hand-off — Multi-agent flow

```
spec.md → @planner /plan-feature
       → @architect /design-feature (Mermaid: BE layers + FE component tree + flow)
       → /bootstrap-project  (or part of /deliver-feature Phase 0a)
       → /bootstrap-frontend (or part of /deliver-feature Phase 0b)
       → @backend-dev /scaffold-feature  ([BE] TODOs only)
       → @frontend-dev /implement-frontend ([FE] TODOs only)
       → @tester /generate-tests  (BE: xUnit ≥80%, FE: Vitest cho hooks + 1 e2e Playwright smoke)
       → @reviewer /review-feature (REVIEW.md)

Or one-shot: /deliver-feature #file:feature-specs/shop-fullstack.spec.md
```

## TODO tagging convention

Trong `TODO.md`, planner phải tag mỗi task:
- `[BE]` — implement bởi `@backend-dev`
- `[FE]` — implement bởi `@frontend-dev`
- `[BE+FE]` — task xuyên stack (vd: API contract change cần update cả 2 phía)
- `[TEST]` — test task, xử lý bởi `@tester` sau

## Definition of Done

- ✅ `dotnet build` + `dotnet test` (BE) cả hai pass
- ✅ `npm run build` (FE) pass
- ✅ Cả 2 chạy được: `dotnet run --project ShopApi` (port 5080) + `npm run dev --prefix ShopWeb` (port 5173)
- ✅ Manual smoke: search → detail → add-to-cart → checkout → confirmation working
- ✅ `REVIEW.md` approved
- ✅ Commit history rõ `[ai]` prefix, 1 TODO = 1 commit
