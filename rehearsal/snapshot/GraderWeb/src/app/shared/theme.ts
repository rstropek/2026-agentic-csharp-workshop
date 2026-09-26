import { DOCUMENT, effect, inject, Service, signal } from '@angular/core';

export type Theme = 'light' | 'dark';

/** Key in local storage; also read by the inline script in index.html that avoids a light flash. */
const storageKey = 'grader-theme';

/**
 * Light/dark theme. Light is the default, the OS setting is ignored. The choice is remembered in
 * local storage and applied as `data-theme` attribute on `<html>`, which switches the color tokens.
 */
@Service()
export class ThemeService {
  private readonly document = inject(DOCUMENT);

  readonly theme = signal<Theme>(readStoredTheme(this.document));

  constructor() {
    effect(() => {
      const theme = this.theme();
      this.document.documentElement.dataset['theme'] = theme;
      try {
        this.document.defaultView?.localStorage.setItem(storageKey, theme);
      } catch {
        // Storage not available (e.g. disabled cookies): the theme is not remembered.
      }
    });
  }
}

function readStoredTheme(document: Document): Theme {
  try {
    return document.defaultView?.localStorage.getItem(storageKey) === 'dark' ? 'dark' : 'light';
  } catch {
    return 'light';
  }
}
