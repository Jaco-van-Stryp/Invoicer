import { Routes } from '@angular/router';
import { Login } from './Components/Auth/login/login';

export const routes: Routes = [
  {
    path: '',
    component: Login,
    title: 'Login',
  },
];
