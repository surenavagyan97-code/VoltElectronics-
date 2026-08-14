// Mirrors the backend DTOs (camelCase over the wire).

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

// Catalog
export interface Category {
  id: number;
  name: string;
  slug: string;
  productCount: number;
  imageUrl: string | null;
}

export interface ProductListItem {
  id: number;
  name: string;
  slug: string;
  category: string;
  categoryId: number;
  price: number;
  compareAtPrice: number | null;
  badge: string | null;
  rating: number;
  reviewCount: number;
  stock: number;
  imageUrl: string | null;
}

export interface ProductSpec { name: string; value: string; }
export interface ProductImage { id: number; url: string; thumbUrl: string; cardUrl: string; sortOrder: number; }

export interface ProductDetail extends Omit<ProductListItem, 'imageUrl'> {
  sku: string;
  description: string;
  images: ProductImage[];
  specs: ProductSpec[];
  related: ProductListItem[];
}

export interface ProductQuery {
  page?: number;
  pageSize?: number;
  categoryIds?: number[];
  priceBands?: string[];
  search?: string;
  sort?: string;
}

// Editable storefront pages (one body per key + language; readers fall back to English)
export interface ContentPage {
  key: string;
  lang: string;
  body: string;
  updatedAt: string;
}

/** One language's display texts; each field falls back to the product's canonical value. */
export interface ProductTranslation { lang: string; name: string | null; description: string | null; }

// Cart
export interface CartItem {
  productId: number;
  name: string;
  slug: string;
  category: string;
  price: number;
  qty: number;
  lineTotal: number;
  stock: number;
  imageUrl: string | null;
}

export interface Cart {
  id: string;
  items: CartItem[];
  count: number;
  subtotal: number;
  shipping: number;
  tax: number;
  total: number;
  currency: string;
}

// Auth
export interface AuthUser { id: string; email: string; fullName: string; roles: string[]; }
export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
  user: AuthUser;
}

// Orders / checkout
export interface CheckoutRequest {
  email: string;
  fullName: string;
  company?: string | null;
  street: string;
  city: string;
  state: string;
  zip: string;
  phone?: string | null;
}
export interface CheckoutResponse { orderNumber: string; paymentUrl: string; }

export interface OrderItem {
  productId: number;
  productName: string;
  slug: string | null;
  imageUrl: string | null;
  unitPrice: number;
  qty: number;
}
export interface OrderSummary {
  orderNumber: string;
  status: string;
  createdAt: string;
  total: number;
  itemCount: number;
  currency: string;
}
export interface OrderDetail {
  orderNumber: string;
  status: string;
  createdAt: string;
  paidAt: string | null;
  paymentFailureReason: string | null;
  shipFullName: string;
  shipCompany: string | null;
  shipStreet: string;
  shipCity: string;
  shipState: string;
  shipZip: string;
  shipPhone: string | null;
  subtotal: number;
  shippingCost: number;
  tax: number;
  total: number;
  currency: string;
  items: OrderItem[];
}

// Admin
export interface AdminProductListItem {
  id: number;
  name: string;
  slug: string;
  sku: string;
  category: string;
  categoryId: number;
  price: number;
  compareAtPrice: number | null;
  stock: number;
  status: string;
  badge: string | null;
  imageUrl: string | null;
}
export interface AdminProductDetail {
  id: number;
  name: string;
  slug: string;
  sku: string;
  categoryId: number;
  description: string;
  price: number;
  compareAtPrice: number | null;
  stock: number;
  status: string;
  badge: string | null;
  rating: number;
  reviewCount: number;
  images: ProductImage[];
  specs: ProductSpec[];
  translations: ProductTranslation[];
}
export interface SaveProductRequest {
  name: string;
  sku: string;
  categoryId: number;
  description: string;
  price: number;
  compareAtPrice: number | null;
  stock: number;
  status: string;
  badge: string | null;
  specs: ProductSpec[];
  translations: ProductTranslation[];
}
export interface ImportRowError { rowNumber: number; error: string; }
export interface ImportProductsResult {
  created: number;
  updated: number;
  errors: ImportRowError[];
}
export interface AdminOrderListItem {
  orderNumber: string;
  customer: string;
  email: string;
  createdAt: string;
  total: number;
  currency: string;
  status: string;
  itemCount: number;
  courierId: string | null;
  courierName: string | null;
}
export interface Courier {
  id: string;
  email: string;
  fullName: string;
  activeOrderCount: number;
}

// Delivery (what a courier sees about an assigned order)
export interface DeliveryOrderItem { productName: string; qty: number; }
export interface DeliveryOrder {
  orderNumber: string;
  status: string;
  createdAt: string;
  fullName: string;
  phone: string | null;
  street: string;
  city: string;
  state: string;
  zip: string;
  total: number;
  currency: string;
  items: DeliveryOrderItem[];
}
export interface AdminOrderStats {
  total: number;
  pendingPayment: number;
  processing: number;
  shipped: number;
  delivered: number;
  cancelled: number;
}
export interface RevenueDay { day: string; revenue: number; orders: number; }
export interface TopProduct { productId: number; name: string; unitsSold: number; revenue: number; }
/** Revenue figures are always normalized to the store's base currency (see AppConfig.currency),
 *  regardless of what currency individual orders were charged in. */
export interface Analytics {
  revenue30d: number;
  revenueDeltaPct: number;
  orders30d: number;
  ordersDeltaPct: number;
  averageOrderValue30d: number;
  lowStockCount: number;
  revenueByDay7d: RevenueDay[];
  topProducts: TopProduct[];
  lowStockProducts: AdminProductListItem[];
}

export interface AppConfig {
  paymentProvider: string;
  currency: string;
  supportedCurrencies: string[];
  rates: Record<string, number>;
}
