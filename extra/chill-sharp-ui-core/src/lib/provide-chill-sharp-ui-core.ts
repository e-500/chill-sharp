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
    const parsed = JSON.parse(rawSession) as { accessToken?: string; accessTokenExpiresUtc?: string };
    const accessToken = parsed.accessToken?.trim() ?? '';
    if (!accessToken || isAccessTokenExpired(parsed.accessTokenExpiresUtc, accessToken)) {
      if (accessToken) {
        globalThis.localStorage?.removeItem(SESSION_STORAGE_KEY);
      }
      return '';
    }

    return accessToken;
  } catch {
    return '';
  }
}

function isAccessTokenExpired(accessTokenExpiresUtc: string | undefined, accessToken: string): boolean {
  const expiresAt = Date.parse(accessTokenExpiresUtc ?? '');
  if (Number.isFinite(expiresAt)) {
    return expiresAt <= Date.now();
  }

  const [, payload] = accessToken.split('.');
  if (!payload) {
    return false;
  }

  try {
    const normalizedPayload = payload.replace(/-/g, '+').replace(/_/g, '/');
    const decodedPayload = globalThis.atob(normalizedPayload.padEnd(Math.ceil(normalizedPayload.length / 4) * 4, '='));
    const expiresAtSeconds = JSON.parse(decodedPayload).exp;
    return typeof expiresAtSeconds === 'number'
      && Number.isFinite(expiresAtSeconds)
      && expiresAtSeconds * 1_000 <= Date.now();
  } catch {
    return false;
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
