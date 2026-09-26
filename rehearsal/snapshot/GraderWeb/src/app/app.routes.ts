import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'classes' },
  {
    path: 'classes',
    title: 'Classes · Grader',
    loadComponent: () => import('./classes/class-list').then((m) => m.ClassList),
  },
  {
    path: 'classes/new',
    title: 'New class · Grader',
    loadComponent: () => import('./classes/class-editor').then((m) => m.ClassEditor),
  },
  {
    path: 'classes/:classId',
    title: 'Class · Grader',
    loadComponent: () => import('./classes/class-editor').then((m) => m.ClassEditor),
  },
  {
    path: 'exams',
    title: 'Exams · Grader',
    loadComponent: () => import('./exams/exams').then((m) => m.Exams),
  },
  {
    path: 'settings',
    title: 'Settings · Grader',
    loadComponent: () => import('./settings/settings').then((m) => m.Settings),
  },
  { path: '**', redirectTo: 'classes' },
];
