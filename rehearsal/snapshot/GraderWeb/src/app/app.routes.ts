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
    children: [
      {
        path: '',
        loadComponent: () => import('./exams/no-exam-selected').then((m) => m.NoExamSelected),
      },
      {
        path: 'new',
        title: 'New exam · Grader',
        loadComponent: () => import('./exams/exam-editor').then((m) => m.ExamEditor),
      },
      {
        path: ':examId',
        title: 'Exam · Grader',
        loadComponent: () => import('./exams/exam-editor').then((m) => m.ExamEditor),
      },
    ],
  },
  {
    path: 'assistant',
    title: 'Assistant · Grader',
    loadComponent: () => import('./assistant/assistant').then((m) => m.Assistant),
  },
  {
    path: 'settings',
    title: 'Settings · Grader',
    loadComponent: () => import('./settings/settings').then((m) => m.Settings),
  },
  { path: '**', redirectTo: 'classes' },
];
