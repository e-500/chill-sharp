import { APP_INITIALIZER, InjectionToken, Provider, inject } from '@angular/core';
import { CHILL_SHARP_CLIENT, ChillSharpNgClient, provideChillSharpClient } from '@chill-sharp/ng-client';
import { CHILL_BASE_URL, CHILL_CULTURE } from './chill.config';
import { SESSION_STORAGE_KEY, USER_PREFERENCES_STORAGE_KEY } from './storage-keys';
import { ChillService } from './services/chill.service';
import { WorkspaceTaskRegistryService } from './services/workspace-task-registry.service';

export interface ChillSharpUiCoreOptions {
  /** Client-owned theme identifiers in addition to the built-in bright, dark, and soft themes. */
  additionalThemes?: readonly string[];
}

export const CHILL_SHARP_UI_CORE_OPTIONS = new InjectionToken<ChillSharpUiCoreOptions>('CHILL_SHARP_UI_CORE_OPTIONS');

export function provideChillSharpUiCore(options: ChillSharpUiCoreOptions = {}): Provider[] {
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
      provide: CHILL_SHARP_UI_CORE_OPTIONS,
      useValue: options
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
  const rawSession = globalThis.localStorage?.getItem(SESSION_STORAGE_KEY);
  const rawPreferences = globalThis.localStorage?.getItem(USER_PREFERENCES_STORAGE_KEY);
  if (!rawSession || !rawPreferences) {
    return readBrowserCultureName();
  }

  try {
    const session = JSON.parse(rawSession) as { userId?: string };
    const preferences = JSON.parse(rawPreferences) as {
      userId?: string;
      preferences?: { displayCultureName?: string };
    };
    return session.userId?.trim() && session.userId.trim() === preferences.userId?.trim()
      ? preferences.preferences?.displayCultureName?.trim() || readBrowserCultureName()
      : readBrowserCultureName();
  } catch {
    return readBrowserCultureName();
  }
}

function readBrowserCultureName(): string {
  const languages = globalThis.navigator?.languages;
  const browserCultureName = languages?.find((language) => typeof language === 'string' && language.trim())
    ?? globalThis.navigator?.language
    ?? '';
  return browserCultureName.trim() || CHILL_CULTURE;
}
