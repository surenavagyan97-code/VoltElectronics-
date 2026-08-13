import { Routes } from '@angular/router';
import { adminGuard, authGuard } from './core/guards';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./store/store-layout').then((m) => m.StoreLayout),
    children: [
      { path: '', loadComponent: () => import('./store/home').then((m) => m.HomePage) },
      { path: 'shop', loadComponent: () => import('./store/shop').then((m) => m.ShopPage) },
      { path: 'contact', loadComponent: () => import('./store/contact').then((m) => m.ContactPage) },
      { path: 'product/:slug', loadComponent: () => import('./store/product-detail').then((m) => m.ProductDetailPage) },
      { path: 'cart', loadComponent: () => import('./store/cart-page').then((m) => m.CartPage) },
      { path: 'checkout', loadComponent: () => import('./store/checkout').then((m) => m.CheckoutPage) },
      { path: 'confirmation/:orderNumber', loadComponent: () => import('./store/confirmation').then((m) => m.ConfirmationPage) },
      { path: 'login', loadComponent: () => import('./store/login').then((m) => m.LoginPage) },
      { path: 'register', loadComponent: () => import('./store/register').then((m) => m.RegisterPage) },
      {
        path: 'account/orders',
        canActivate: [authGuard],
        loadComponent: () => import('./store/account-orders').then((m) => m.AccountOrdersPage),
      },
    ],
  },
  {
    path: 'admin',
    canActivate: [adminGuard],
    loadComponent: () => import('./admin/admin-layout').then((m) => m.AdminLayout),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'products' },
      { path: 'products', loadComponent: () => import('./admin/admin-products').then((m) => m.AdminProductsPage) },
      { path: 'products/new', loadComponent: () => import('./admin/admin-product-form').then((m) => m.AdminProductFormPage) },
      { path: 'products/:id', loadComponent: () => import('./admin/admin-product-form').then((m) => m.AdminProductFormPage) },
      { path: 'categories', loadComponent: () => import('./admin/admin-categories').then((m) => m.AdminCategoriesPage) },
      { path: 'orders', loadComponent: () => import('./admin/admin-orders').then((m) => m.AdminOrdersPage) },
      { path: 'analytics', loadComponent: () => import('./admin/admin-analytics').then((m) => m.AdminAnalyticsPage) },
    ],
  },
  { path: '**', redirectTo: '' },
];
