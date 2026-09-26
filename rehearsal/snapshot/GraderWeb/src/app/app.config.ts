import { provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideDefaultClient } from './api';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    // Route parameters (e.g. :classId) are bound to component inputs.
    provideRouter(routes, withComponentInputBinding()),
    // The generated client registers a class-based interceptor, which requires withInterceptorsFromDi().
    provideHttpClient(withInterceptorsFromDi()),
    // Paths in the OpenAPI spec already start with /api, and the API is served from the same origin
    // (dev: via proxy.conf.json, prod: by the .NET backend). The generated default is '/api', which would double it.
    provideDefaultClient({ basePath: '' }),
  ],
};
