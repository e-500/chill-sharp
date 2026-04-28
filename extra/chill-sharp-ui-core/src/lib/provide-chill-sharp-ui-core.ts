import { APP_INITIALIZER, Provider, inject } from '@angular/core';
import { CHILL_SHARP_CLIENT, ChillSharpNgClient, provideChillSharpClient } from '@chill-sharp/ng-client';
import { CHILL_BASE_URL, CHILL_CULTURE } from './chill.config';
import { SESSION_STORAGE_KEY, USER_PREFERENCES_STORAGE_KEY } from './storage-keys';
import { ChillService } from './services/chill.service';
import { WorkspaceTaskRegistryService } from './services/workspace-task-registry.service';

export function provideChillSharpUiCore(): Provider[] {
  return [
    ...provideChillSharpClient({
      baseUrl: CHILL_BASE_URL,
      options: {
        cultureName: readStoredCultureName(),
        accessToken: readStoredAccessToken(),
        fetchImpl: authAwareFetch,
        signalRWithCredentials: false
      }
    }),
    {
      provide: ChillSharpNgClient,
      useFactory: () => new ChillSharpNgClient(inject(CHILL_SHARP_CLIENT))
    },
    {
      provide: APP_INITIALIZER,
      multi: true,
      useFactory: () => () => inject(WorkspaceTaskRegistryService).initialize()
    },
    {
      provide: APP_INITIALIZER,
      multi: true,
      useFactory: () => () => inject(ChillService).initialize()
    }
  ];
}

async function authAwareFetch(input: RequestInfo | URL, init?: RequestInit): Promise<Response> {
  const method = (init?.method ?? 'GET').toUpperCase();
  const headers = new Headers(init?.headers);
  const accessToken = readStoredAccessToken();
  if (accessToken && !headers.has('Authorization')) {
    headers.set('Authorization', `Bearer ${accessToken}`);
  }

  return globalThis.fetch(input, {
    ...init,
    method,
    headers
  });
}

function readStoredAccessToken(): string {
  const rawSession = globalThis.localStorage?.getItem(SESSION_STORAGE_KEY);
  if (!rawSession) {
    return '';
  }

  try {
    const parsed = JSON.parse(rawSession) as { accessToken?: string };
    return parsed.accessToken?.trim() ?? '';
  } catch {
    return '';
  }
}

function readStoredCultureName(): string {
  const rawPreferences = globalThis.localStorage?.getItem(USER_PREFERENCES_STORAGE_KEY);
  if (!rawPreferences) {
    return CHILL_CULTURE;
  }

  try {
    const parsed = JSON.parse(rawPreferences) as { displayCultureName?: string };
    return parsed.displayCultureName?.trim() || CHILL_CULTURE;
  } catch {
    return CHILL_CULTURE;
  }
}
