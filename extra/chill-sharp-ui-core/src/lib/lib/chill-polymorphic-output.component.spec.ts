import { ComponentFixture, TestBed } from '@angular/core/testing';
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
        formatDisplayNumber: (value: number) => `#${value}`,
        parseDisplayDecimal: (value: string) => Number(value),
        formatDisplayDate: (value: string) => `date:${value}`,
        formatDisplayTime: (value: string) => `time:${value}`,
        formatDisplayDateTime: (value: string) => `dateTime:${value}`
      } }]
    }).compileComponents();
    fixture = TestBed.createComponent(ChillPolymorphicOutputComponent);
  });

  it('uses the number output control and localized number formatting', () => {
    const schema: ChillSchema = { properties: [{ name: 'amount', propertyType: CHILL_PROPERTY_TYPE.Decimal, isNullable: false }] };
    fixture.componentRef.setInput('schema', schema);
    fixture.componentRef.setInput('source', { properties: { amount: 12.5 } });
    fixture.componentRef.setInput('propertyName', 'amount');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('app-chill-polymorphic-output-number-control')).not.toBeNull();
    expect(fixture.nativeElement.textContent).toContain('#12.5');
  });

  it('uses the temporal output control and preserves date parts without wrapping', () => {
    const schema: ChillSchema = { properties: [{ name: 'created', propertyType: CHILL_PROPERTY_TYPE.DateTime, isNullable: false }] };
    fixture.componentRef.setInput('schema', schema);
    fixture.componentRef.setInput('source', { properties: { created: '2026-08-30T10:00:00' } });
    fixture.componentRef.setInput('propertyName', 'created');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('app-chill-polymorphic-output-temporal-control')).not.toBeNull();
    expect(fixture.nativeElement.textContent).toContain('dateTime:2026-08-30T10:00:00');
    expect(fixture.nativeElement.querySelectorAll('.spaced-parts__part').length).toBeGreaterThan(0);
  });
});
