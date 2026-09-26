import { Component, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { DemoDataService } from '../api';
import { toApiError } from '../shared/api-error';
import { ConfirmService } from '../shared/confirm';
import { SchoolYearStore } from '../shared/school-year';
import { Theme, ThemeService } from '../shared/theme';

/** App settings: light/dark theme and resetting the demo data. */
@Component({
  selector: 'app-settings',
  templateUrl: './settings.html',
  styleUrl: './settings.css',
})
export class Settings {
  private readonly demoData = inject(DemoDataService);
  private readonly confirmService = inject(ConfirmService);
  private readonly schoolYears = inject(SchoolYearStore);
  protected readonly themeService = inject(ThemeService);

  protected readonly resetting = signal(false);
  protected readonly resetResult = signal<{ ok: boolean; message: string } | undefined>(undefined);

  protected setTheme(theme: Theme) {
    this.themeService.theme.set(theme);
  }

  protected async resetDemoData() {
    const confirmed = await this.confirmService.confirm({
      title: 'Reset demo data?',
      message:
        'All school years, classes, students, exams and results are deleted and replaced with generated demo data. This cannot be undone.',
      confirmLabel: 'Reset demo data',
    });
    if (!confirmed) {
      return;
    }

    this.resetResult.set(undefined);
    this.resetting.set(true);
    try {
      await firstValueFrom(this.demoData.replaceWithDemoData());
      this.schoolYears.reload();
      this.resetResult.set({ ok: true, message: 'The demo data has been reset.' });
    } catch (error) {
      this.resetResult.set({ ok: false, message: toApiError(error).message });
    } finally {
      this.resetting.set(false);
    }
  }
}
