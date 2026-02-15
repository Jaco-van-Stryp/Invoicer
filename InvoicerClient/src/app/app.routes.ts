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
    loadComponent: () => import('./Components/Auth/login/login').then(m => m.Login),
    title: 'Sign In — Invoicer',
  },
  {
    path: 'register',
    loadComponent: () => import('./Components/Auth/register/register').then(m => m.Register),
    title: 'Register — Invoicer',
  },
  {
    path: 'create-company',
    loadComponent: () => import('./Components/Company/create-company/create-company').then(m => m.CreateCompany),
    title: 'Create Company — Invoicer',
  },
  {
    path: 'dashboard',
    loadComponent: () => import('./Components/Dashboard/dashboard/dashboard').then(m => m.Dashboard),
    title: 'Dashboard — Invoicer',
  },
];
