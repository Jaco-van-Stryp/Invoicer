import { Routes } from '@angular/router';
import { Login } from './Components/Auth/login/login';
import { Register } from './Components/Auth/register/register';
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
    path: 'dashboard',
    component: Dashboard,
    title: 'Dashboard',
  },
];
