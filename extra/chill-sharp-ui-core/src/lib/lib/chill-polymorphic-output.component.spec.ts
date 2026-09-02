import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { ChillService } from '../services/chill.service';
import { CHILL_PROPERTY_TYPE, type ChillSchema } from '../models/chill-schema.models';
import { ChillPolymorphicOutputComponent } from './chill-polymorphic-output.component';

describe('ChillPolymorphicOutputComponent', () => {
  let fixture: ComponentFixture<ChillPolymorphicOutputComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ChillPolymorphicOutputComponent],
      providers: [{ provide: ChillService, useValue: {
        T: (_id: string, english: string) => english,
        formatApiNumber: (value: number | string) => Number(value) === 1234.56 ? '1.234,56' : Number(value) === 1234 ? '1.234' : `#${value}`,
        formatDisplayDate: (value: string) => value === '2026-01-05' ? '05/01/2026' : `date:${value}`,
        formatDisplayTime: (value: string) => `time:${value}`,
        formatDisplayDateTime: (value: string) => value === '2026-08-30T10:00:00Z' ? '30/08/2026 12:00' : `date-time:${value}`,
        userPreferences: signal({
          displayCultureName: 'en-GB',
          displayTimeZone: 'Europe/Rome',
          displayDateFormat: 'DD/MM/YYYY',
          displayNumberFormat: '1.000,00'
        })
      } }]
    }).compileComponents();
    fixture = TestBed.createComponent(ChillPolymorphicOutputComponent);
  });

  it('uses the number output control and localized number formatting', () => {
    const schema: ChillSchema = {
      properties: [{ name: 'amount', propertyType: CHILL_PROPERTY_TYPE.Decimal, isNullable: false, metadata: { staticSuffix: '€' } }]
    };
    fixture.componentRef.setInput('schema', schema);
    fixture.componentRef.setInput('source', { properties: { amount: 12.5 } });
    fixture.componentRef.setInput('propertyName', 'amount');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('app-chill-polymorphic-output-number-control')).not.toBeNull();
    expect(fixture.nativeElement.textContent).toContain('#12.5€');
  });

  it('retains decimal digits before the static suffix', () => {
    const schema: ChillSchema = {
      properties: [{ name: 'amount', propertyType: CHILL_PROPERTY_TYPE.Decimal, isNullable: false, metadata: { staticSuffix: '€' } }]
    };
    fixture.componentRef.setInput('schema', schema);
    fixture.componentRef.setInput('source', { properties: { amount: 12.345 } });
    fixture.componentRef.setInput('propertyName', 'amount');
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('#12.345€');
  });

  it('preserves the user thousands and decimal separators before the static suffix', () => {
    const schema: ChillSchema = {
      properties: [{ name: 'amount', propertyType: CHILL_PROPERTY_TYPE.Decimal, isNullable: false, metadata: { staticSuffix: '€' } }]
    };
    fixture.componentRef.setInput('schema', schema);
    fixture.componentRef.setInput('source', { properties: { amount: 1234.56 } });
    fixture.componentRef.setInput('propertyName', 'amount');
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('1.234,56€');
  });

  it('formats invariant API decimal strings with the user decimal and grouping separators', () => {
    const schema: ChillSchema = {
      properties: [{ name: 'amount', propertyType: CHILL_PROPERTY_TYPE.Decimal, isNullable: false }]
    };
    fixture.componentRef.setInput('schema', schema);
    fixture.componentRef.setInput('source', { properties: { amount: '1234.56' } });
    fixture.componentRef.setInput('propertyName', 'amount');
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('1.234,56');
  });

  it('formats invariant API integer scalars with the configured grouping separator', () => {
    const schema: ChillSchema = {
      properties: [{ name: 'quantity', propertyType: CHILL_PROPERTY_TYPE.Integer, isNullable: false }]
    };
    fixture.componentRef.setInput('schema', schema);
    fixture.componentRef.setInput('source', { properties: { quantity: '1234' } });
    fixture.componentRef.setInput('propertyName', 'quantity');
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('1.234');
  });

  it('renders UTC backend date-times with a visible, breakable separator between intact date and time parts', () => {
    const schema: ChillSchema = { properties: [{ name: 'created', propertyType: CHILL_PROPERTY_TYPE.DateTime, isNullable: false }] };
    fixture.componentRef.setInput('schema', schema);
    fixture.componentRef.setInput('source', { properties: { created: '2026-08-30T10:00:00Z' } });
    fixture.componentRef.setInput('propertyName', 'created');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('app-chill-polymorphic-output-temporal-control')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('.polymorphic-output')?.getAttribute('title')).toBe('30/08/2026 12:00');
    expect(fixture.nativeElement.querySelectorAll('.spaced-parts__part').length).toBe(2);
    expect(fixture.nativeElement.querySelector('.spaced-parts__separator')?.textContent).toBe('\u00a0');
    expect(fixture.nativeElement.querySelector('.spaced-parts__separator + wbr')).not.toBeNull();
  });
});
