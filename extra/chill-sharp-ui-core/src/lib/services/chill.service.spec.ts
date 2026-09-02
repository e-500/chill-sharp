import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { ChillSharpNgClient } from '@chill-sharp/ng-client';
import { of } from 'rxjs';
import { SESSION_STORAGE_KEY, USER_PREFERENCES_STORAGE_KEY } from '../storage-keys';
import { ChillService } from './chill.service';

describe('ChillService user-preference formatting', () => {
  let service: ChillService;
  let originalFetch: typeof fetch;

  beforeEach(() => {
    localStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify({
      accessToken: 'test-token',
      accessTokenExpiresUtc: '',
      refreshToken: '',
      refreshTokenExpiresUtc: '',
      userId: 'user-1',
      userName: 'preference-test'
    }));
    localStorage.setItem(USER_PREFERENCES_STORAGE_KEY, JSON.stringify({
      userId: 'user-1',
      preferences: {
        displayCultureName: 'en-GB',
        displayTimeZone: 'Europe/Rome',
        displayDateFormat: 'DD/MM/YYYY',
        displayNumberFormat: '1.000,00',
        preferredTheme: 'cini'
      }
    }));

    originalFetch = globalThis.fetch;
    globalThis.fetch = (() => Promise.resolve(new Response('ok'))) as typeof fetch;

    TestBed.configureTestingModule({
      providers: [
        ChillService,
        {
          provide: ChillSharpNgClient,
          useValue: {
            getRawClient: () => ({ applyAuthToken: () => undefined }),
            getTexts: () => of([])
          }
        },
        { provide: Router, useValue: { navigate: () => Promise.resolve(true) } }
      ]
    });
    service = TestBed.inject(ChillService);
  });

  afterEach(() => {
    globalThis.fetch = originalFetch;
    localStorage.removeItem(SESSION_STORAGE_KEY);
    localStorage.removeItem(USER_PREFERENCES_STORAGE_KEY);
    TestBed.resetTestingModule();
  });

  it('uses the authenticated en-GB/DD/MM preference rather than the browser locale', () => {
    expect(service.currentCultureName()).toBe('en-GB');
    expect(service.currentDateFormat()).toBe('DD/MM/YYYY');
    expect(service.formatDisplayDate('2026-01-05')).toBe('05/01/2026');
    expect(service.parseDisplayDate('31/12/2026')).toBe('2026-12-31');
  });

  it('formats invariant backend numeric scalars and parses the configured display pattern', () => {
    expect(service.formatApiNumber('1234567.8')).toBe('1.234.567,80');
    expect(service.formatApiNumber(1234567.8)).toBe('1.234.567,80');
    expect(service.parseDisplayDecimal('1.234.567,80')).toBe(1234567.8);
    expect(service.parseDisplayInteger('1.234')).toBe(1234);
  });

  it('converts UTC backend date-times to the Europe/Rome local time across daylight-saving boundaries', () => {
    expect(service.formatDisplayDateTime('2026-01-15T10:30:45Z')).toBe('15/01/2026 11:30:45');
    expect(service.formatDisplayDateTime('2026-08-30T10:00:00+00:00')).toBe('30/08/2026 12:00');
  });

  it('serializes entered local date-times using the configured IANA time zone', () => {
    expect(service.parseDisplayDateTime('15/01/2026 11:30')).toBe('2026-01-15T11:30:00+01:00');
    expect(service.parseDisplayDateTime('30/08/2026 12:00')).toBe('2026-08-30T12:00:00+02:00');
  });
});

describe('ChillService anonymous startup defaults', () => {
  let service: ChillService;
  let originalFetch: typeof fetch;
  let languageDescriptor: PropertyDescriptor | undefined;
  let languagesDescriptor: PropertyDescriptor | undefined;

  beforeEach(() => {
    localStorage.removeItem(SESSION_STORAGE_KEY);
    localStorage.removeItem(USER_PREFERENCES_STORAGE_KEY);
    languageDescriptor = Object.getOwnPropertyDescriptor(globalThis.navigator, 'language');
    languagesDescriptor = Object.getOwnPropertyDescriptor(globalThis.navigator, 'languages');
    Object.defineProperty(globalThis.navigator, 'language', { configurable: true, value: 'en-US' });
    Object.defineProperty(globalThis.navigator, 'languages', { configurable: true, value: ['en-US'] });

    originalFetch = globalThis.fetch;
    globalThis.fetch = (() => Promise.resolve(new Response('ok'))) as typeof fetch;

    TestBed.configureTestingModule({
      providers: [
        ChillService,
        {
          provide: ChillSharpNgClient,
          useValue: {
            getRawClient: () => ({ applyAuthToken: () => undefined }),
            getTexts: () => of([])
          }
        },
        { provide: Router, useValue: { navigate: () => Promise.resolve(true) } }
      ]
    });
    service = TestBed.inject(ChillService);
  });

  afterEach(() => {
    globalThis.fetch = originalFetch;
    if (languageDescriptor) {
      Object.defineProperty(globalThis.navigator, 'language', languageDescriptor);
    } else {
      delete (globalThis.navigator as { language?: string }).language;
    }
    if (languagesDescriptor) {
      Object.defineProperty(globalThis.navigator, 'languages', languagesDescriptor);
    } else {
      delete (globalThis.navigator as { languages?: readonly string[] }).languages;
    }
    TestBed.resetTestingModule();
  });

  it('uses browser language, date format, number format, and time zone before login', () => {
    expect(service.isAuthenticated()).toBeFalse();
    expect(service.currentCultureName()).toBe('en-US');
    expect(service.currentDateFormat()).toBe('MM/DD/YYYY');
    expect(service.currentNumberFormat()).toBe('en-US');
    expect(service.currentTimeZone()).toBe(Intl.DateTimeFormat().resolvedOptions().timeZone || 'UTC');
    expect(service.formatDisplayDate('2026-01-05')).toBe('01/05/2026');
    expect(service.formatApiNumber('1234.56')).toBe('1,234.56');
    expect(service.formatDisplayDateTime('2026-01-15T12:00:00Z')).toMatch(/^01\/15\/2026 /);
  });
});
