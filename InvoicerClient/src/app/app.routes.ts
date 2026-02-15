import { Routes } from '@angular/router';
import { Login } from './Components/Auth/login/login';
import { Register } from './Components/Auth/register/register';
import { CreateCompany } from './Components/Company/create-company/create-company';
import { Dashboard } from './Components/Dashboard/dashboard/dashboard';

export const routes: Routes = [
  {
    path: '',
    component: Login,
    title: 'Login',
  },
  {
    path: 'register',
    component: Register,
    title: 'Register',
  },
  {
    path: 'create-company',
    component: CreateCompany,
    title: 'Create Company',
  },
  {
    path: 'dashboard',
    component: Dashboard,
    title: 'Dashboard',
  },
];
