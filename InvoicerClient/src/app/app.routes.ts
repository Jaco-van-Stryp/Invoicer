import { Routes } from '@angular/router';
import { Landing } from './Components/Landing/landing/landing';

export const routes: Routes = [
  {
    path: '',
    component: Landing,
    title: 'Invoicer — Effortless Invoicing for Modern Businesses',
  },
  {
    path: 'login',
    loadComponent: () => import('./Components/Auth/login/login').then((m) => m.Login),
    title: 'Sign In — Invoicer',
  },
  {
    path: 'register',
    loadComponent: () => import('./Components/Auth/register/register').then((m) => m.Register),
    title: 'Register — Invoicer',
  },
  {
    path: 'create-company',
    loadComponent: () =>
      import('./Components/Company/create-company/create-company').then((m) => m.CreateCompany),
    title: 'Create Company — Invoicer',
  },
  {
    path: 'dashboard',
    loadComponent: () =>
      import('./Components/Dashboard/dashboard-layout/dashboard-layout').then(
        (m) => m.DashboardLayout,
      ),
    title: 'Dashboard — Invoicer',
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./Components/Dashboard/dashboard-home/dashboard-home').then(
            (m) => m.DashboardHome,
          ),
        title: 'Dashboard — Invoicer',
      },
      {
        path: 'clients',
        loadComponent: () =>
          import('./Components/Client/client-list/client-list').then((m) => m.ClientList),
        title: 'Clients — Invoicer',
      },
      {
        path: 'products',
        loadComponent: () =>
          import('./Components/Product/product-list/product-list').then((m) => m.ProductList),
        title: 'Products — Invoicer',
      },
      {
        path: 'invoices',
        loadComponent: () =>
          import('./Components/Invoice/invoice-list/invoice-list').then((m) => m.InvoiceList),
        title: 'Invoices — Invoicer',
      },
    ],
  },
];
