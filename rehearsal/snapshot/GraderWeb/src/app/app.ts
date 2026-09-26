import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { ConfirmDialog } from './shared/confirm-dialog';
import { SchoolYearPicker } from './shared/school-year-picker';
import { ThemeService } from './shared/theme';

@Component({
  imports: [RouterOutlet, RouterLink, RouterLinkActive, SchoolYearPicker, ConfirmDialog],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App {
  constructor() {
    // Applies the stored theme on startup.
    inject(ThemeService);
  }
}
