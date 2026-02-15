import { Routes } from '@angular/router';
import { Landing } from './Components/Landing/landing/landing';
import { Login } from './Components/Auth/login/login';
import { Register } from './Components/Auth/register/register';
import { CreateCompany } from './Components/Company/create-company/create-company';
import { Dashboard } from './Components/Dashboard/dashboard/dashboard';

export const routes: Routes = [
  {
    path: '',
    component: Landing,
    title: 'Invoicer — Effortless Invoicing for Modern Businesses',
  },
  {
    path: 'login',
    component: Login,
    title: 'Sign In — Invoicer',
  },
  {
    path: 'register',
    component: Register,
    title: 'Register — Invoicer',
  },
  {
    path: 'create-company',
    component: CreateCompany,
    title: 'Create Company — Invoicer',
  },
  {
    path: 'dashboard',
    component: Dashboard,
    title: 'Dashboard — Invoicer',
  },
];
