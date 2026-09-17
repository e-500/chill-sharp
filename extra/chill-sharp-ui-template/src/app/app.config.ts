import { APP_INITIALIZER, ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideChillSharpUiCore } from '@chill-sharp/ui-core';
import { appRoutes } from './app.routes';
import { CLIENT_APP_CONFIG } from '../config/app-config';
import { provideClientTemplateProviders } from './core/providers/client-template.providers';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(appRoutes),
    ...provideChillSharpUiCore(),
    ...provideClientTemplateProviders(),
    {
      provide: APP_INITIALIZER,
      multi: true,
      useFactory: () => () => {
        globalThis.document.title = CLIENT_APP_CONFIG.appName;
      }
    }
  ]
};
