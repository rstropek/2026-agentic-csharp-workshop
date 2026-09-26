import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', loadComponent: () => import('./calculator/calculator').then((m) => m.Calculator) },
  { path: '**', redirectTo: '' },
];
