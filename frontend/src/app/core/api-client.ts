import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  AdminOrderListItem, AdminOrderStats, AdminProductDetail, AdminProductListItem, Analytics,
  AppConfig, AuthResponse, Cart, Category, CheckoutRequest, CheckoutResponse, ContentPage,
  Courier, DeliveryOrder, ImportProductsResult, OrderDetail, OrderSummary, PagedResult,
  ProductDetail, ProductImage, ProductListItem, ProductQuery, SaveProductRequest,
} from './api.types';
import { I18nService } from './i18n.service';

/** Typed access to the Volt Electronics API. Base URL is relative — dev uses the
 *  Angular proxy (proxy.conf.json), production the nginx reverse proxy. */
@Injectable({ providedIn: 'root' })
export class ApiClient {
  private http = inject(HttpClient);
  private i18n = inject(I18nService);

  /** Catalog and content reads are localized server-side; every such call sends the UI language. */
  private get lang(): string {
    return this.i18n.lang();
  }

  // auth
  register(email: string, password: string, fullName: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>('/api/auth/register', { email, password, fullName });
  }
  login(email: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>('/api/auth/login', { email, password });
  }
  refresh(refreshToken: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>('/api/auth/refresh', { refreshToken });
  }
  logout(refreshToken: string): Observable<void> {
    return this.http.post<void>('/api/auth/logout', { refreshToken });
  }

  // catalog
  getProducts(query: ProductQuery): Observable<PagedResult<ProductListItem>> {
    let params = new HttpParams().set('lang', this.lang);
    if (query.page) params = params.set('page', query.page);
    if (query.pageSize) params = params.set('pageSize', query.pageSize);
    for (const id of query.categoryIds ?? []) params = params.append('categoryIds', id);
    for (const band of query.priceBands ?? []) params = params.append('priceBands', band);
    if (query.search) params = params.set('search', query.search);
    if (query.sort) params = params.set('sort', query.sort);
    return this.http.get<PagedResult<ProductListItem>>('/api/products', { params });
  }
  getFeatured(count = 4): Observable<ProductListItem[]> {
    return this.http.get<ProductListItem[]>('/api/products/featured', { params: { count, lang: this.lang } });
  }
  getProduct(slug: string): Observable<ProductDetail> {
    return this.http.get<ProductDetail>(`/api/products/${encodeURIComponent(slug)}`, { params: { lang: this.lang } });
  }
  getProductsByIds(ids: number[]): Observable<ProductListItem[]> {
    let params = new HttpParams().set('lang', this.lang);
    for (const id of ids) params = params.append('ids', id);
    return this.http.get<ProductListItem[]>('/api/products/by-ids', { params });
  }
  getProductsForCompare(ids: number[]): Observable<ProductDetail[]> {
    let params = new HttpParams().set('lang', this.lang);
    for (const id of ids) params = params.append('ids', id);
    return this.http.get<ProductDetail[]>('/api/products/compare', { params });
  }
  getCategories(): Observable<Category[]> {
    return this.http.get<Category[]>('/api/categories');
  }
  /** With fallback (default), a missing translation resolves to the default language. */
  getContent(key: string, lang?: string, fallback = true): Observable<ContentPage> {
    const params = new HttpParams().set('lang', lang ?? this.lang).set('fallback', fallback);
    return this.http.get<ContentPage>(`/api/content/${encodeURIComponent(key)}`, { params });
  }

  // cart
  getCart(): Observable<Cart> { return this.http.get<Cart>('/api/cart'); }
  addCartItem(productId: number, qty: number): Observable<Cart> {
    return this.http.post<Cart>('/api/cart/items', { productId, qty });
  }
  updateCartItem(productId: number, qty: number): Observable<Cart> {
    return this.http.put<Cart>(`/api/cart/items/${productId}`, { qty });
  }
  removeCartItem(productId: number): Observable<Cart> {
    return this.http.delete<Cart>(`/api/cart/items/${productId}`);
  }
  clearCart(): Observable<Cart> { return this.http.delete<Cart>('/api/cart'); }
  mergeCart(): Observable<Cart> { return this.http.post<Cart>('/api/cart/merge', {}); }
  setCartCurrency(currency: string): Observable<Cart> {
    return this.http.put<Cart>('/api/cart/currency', { currency });
  }

  // checkout / orders
  checkout(request: CheckoutRequest): Observable<CheckoutResponse> {
    return this.http.post<CheckoutResponse>('/api/checkout', request);
  }
  getMyOrders(): Observable<OrderSummary[]> {
    return this.http.get<OrderSummary[]>('/api/orders');
  }
  getOrder(orderNumber: string, email?: string): Observable<OrderDetail> {
    const params = email ? new HttpParams().set('email', email) : undefined;
    return this.http.get<OrderDetail>(`/api/orders/${encodeURIComponent(orderNumber)}`, { params });
  }

  getConfig(): Observable<AppConfig> { return this.http.get<AppConfig>('/api/config'); }

  // admin
  adminGetProducts(page: number, pageSize: number, search?: string): Observable<PagedResult<AdminProductListItem>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search) params = params.set('search', search);
    return this.http.get<PagedResult<AdminProductListItem>>('/api/admin/products', { params });
  }
  adminGetProduct(id: number): Observable<AdminProductDetail> {
    return this.http.get<AdminProductDetail>(`/api/admin/products/${id}`);
  }
  adminCreateProduct(request: SaveProductRequest): Observable<{ id: number }> {
    return this.http.post<{ id: number }>('/api/admin/products', request);
  }
  adminUpdateProduct(id: number, request: SaveProductRequest): Observable<void> {
    return this.http.put<void>(`/api/admin/products/${id}`, request);
  }
  adminDeleteProduct(id: number): Observable<void> {
    return this.http.delete<void>(`/api/admin/products/${id}`);
  }
  adminUploadImage(productId: number, file: File): Observable<ProductImage> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<ProductImage>(`/api/admin/products/${productId}/images`, form);
  }
  adminRemoveImage(productId: number, imageId: number): Observable<void> {
    return this.http.delete<void>(`/api/admin/products/${productId}/images/${imageId}`);
  }
  adminExportProducts(): Observable<Blob> {
    return this.http.get('/api/admin/products/export', { responseType: 'blob' });
  }
  adminDownloadImportTemplate(): Observable<Blob> {
    return this.http.get('/api/admin/products/import/template', { responseType: 'blob' });
  }
  adminImportProducts(file: File): Observable<ImportProductsResult> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<ImportProductsResult>('/api/admin/products/import', form);
  }

  adminGetCategories(): Observable<Category[]> {
    return this.http.get<Category[]>('/api/admin/categories');
  }
  adminCreateCategory(name: string): Observable<Category> {
    return this.http.post<Category>('/api/admin/categories', { name });
  }
  adminUpdateCategory(id: number, name: string): Observable<void> {
    return this.http.put<void>(`/api/admin/categories/${id}`, { name });
  }
  adminDeleteCategory(id: number): Observable<void> {
    return this.http.delete<void>(`/api/admin/categories/${id}`);
  }
  adminUploadCategoryImage(categoryId: number, file: File): Observable<{ imageUrl: string }> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<{ imageUrl: string }>(`/api/admin/categories/${categoryId}/image`, form);
  }
  adminRemoveCategoryImage(categoryId: number): Observable<void> {
    return this.http.delete<void>(`/api/admin/categories/${categoryId}/image`);
  }

  adminUpdateContent(key: string, lang: string, body: string): Observable<void> {
    return this.http.put<void>(`/api/admin/content/${encodeURIComponent(key)}`, { lang, body });
  }

  adminGetOrders(page: number, pageSize: number, status?: string, search?: string, courierId?: string): Observable<PagedResult<AdminOrderListItem>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (status) params = params.set('status', status);
    if (search) params = params.set('search', search);
    if (courierId) params = params.set('courierId', courierId);
    return this.http.get<PagedResult<AdminOrderListItem>>('/api/admin/orders', { params });
  }
  adminGetOrderStats(): Observable<AdminOrderStats> {
    return this.http.get<AdminOrderStats>('/api/admin/orders/stats');
  }
  adminGetOrder(orderNumber: string): Observable<OrderDetail> {
    return this.http.get<OrderDetail>(`/api/admin/orders/${encodeURIComponent(orderNumber)}`);
  }
  adminUpdateOrderStatus(orderNumber: string, status: string): Observable<void> {
    return this.http.put<void>(`/api/admin/orders/${encodeURIComponent(orderNumber)}/status`, { status });
  }
  adminAssignOrderCourier(orderNumber: string, courierId: string | null): Observable<void> {
    return this.http.put<void>(`/api/admin/orders/${encodeURIComponent(orderNumber)}/courier`, { courierId });
  }

  adminGetCouriers(): Observable<Courier[]> {
    return this.http.get<Courier[]>('/api/admin/couriers');
  }
  adminCreateCourier(email: string, password: string, fullName: string): Observable<Courier> {
    return this.http.post<Courier>('/api/admin/couriers', { email, password, fullName });
  }
  adminDeleteCourier(id: string): Observable<void> {
    return this.http.delete<void>(`/api/admin/couriers/${encodeURIComponent(id)}`);
  }

  // delivery (courier's own assignments)
  deliveryGetOrders(status?: string): Observable<DeliveryOrder[]> {
    const params = status ? new HttpParams().set('status', status) : undefined;
    return this.http.get<DeliveryOrder[]>('/api/delivery/orders', { params });
  }
  deliveryUpdateOrderStatus(orderNumber: string, status: string): Observable<void> {
    return this.http.put<void>(`/api/delivery/orders/${encodeURIComponent(orderNumber)}/status`, { status });
  }

  adminGetAnalytics(): Observable<Analytics> {
    return this.http.get<Analytics>('/api/admin/analytics');
  }
}
