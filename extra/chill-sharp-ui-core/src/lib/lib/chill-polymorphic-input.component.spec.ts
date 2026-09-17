import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, FormGroup } from '@angular/forms';
import type { JsonValue } from '@chill-sharp/ng-client';
import { ChillService } from '../services/chill.service';
import { WorkspaceDialogService } from '../services/workspace-dialog.service';
import { ChillPolymorphicInputComponent } from './chill-polymorphic-input.component';
import { CHILL_PROPERTY_TYPE, type ChillSchema } from '../models/chill-schema.models';

describe('ChillPolymorphicInputComponent', () => {
  let fixture: ComponentFixture<ChillPolymorphicInputComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ChillPolymorphicInputComponent],
      providers: [
        { provide: ChillService, useValue: {
          T: (_id: string, english: string) => english,
          formatDisplayNumber: (value: number) => String(value),
          readDisplayNumber: (value: string | number) => Number(value)
        } },
        { provide: WorkspaceDialogService, useValue: { openDialog: jasmine.createSpy('openDialog') } }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ChillPolymorphicInputComponent);
  });

  it('renders the control matching each schema property type', () => {
    const schema: ChillSchema = {
      properties: [
        { name: 'enabled', displayName: 'Enabled', propertyType: CHILL_PROPERTY_TYPE.Boolean, isNullable: false },
        { name: 'quantity', displayName: 'Quantity', propertyType: CHILL_PROPERTY_TYPE.Integer, isNullable: false },
        { name: 'kind', displayName: 'Kind', propertyType: CHILL_PROPERTY_TYPE.Select, isNullable: false, metadata: { options: [['a', 'A']] } },
        { name: 'notes', displayName: 'Notes', propertyType: CHILL_PROPERTY_TYPE.String, isNullable: false, customFormat: 'textarea' },
        { name: 'payload', displayName: 'Payload', propertyType: CHILL_PROPERTY_TYPE.Json, isNullable: false }
      ]
    };
    const form = new FormGroup<Record<string, FormControl<JsonValue>>>({
      enabled: new FormControl<JsonValue>(false), quantity: new FormControl<JsonValue>(2), kind: new FormControl<JsonValue>('a'), notes: new FormControl<JsonValue>(''), payload: new FormControl<JsonValue>('{}')
    });

    fixture.componentRef.setInput('schema', schema);
    fixture.componentRef.setInput('form', form);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('app-chill-polymorphic-boolean-control')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('app-chill-polymorphic-scalar-control')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('app-chill-polymorphic-select-control')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('app-chill-polymorphic-textarea-control')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('app-chill-polymorphic-editor-control')).not.toBeNull();
  });

  it('emits the normalized current field value on blur', () => {
    const schema: ChillSchema = { properties: [{ name: 'name', displayName: 'Name', propertyType: CHILL_PROPERTY_TYPE.String, isNullable: false }] };
    const form = new FormGroup<Record<string, FormControl<JsonValue>>>({ name: new FormControl<JsonValue>('Ada') });
    const blurValues: Record<string, JsonValue>[] = [];
    fixture.componentInstance.fieldBlur.subscribe((value) => blurValues.push(value));
    fixture.componentRef.setInput('schema', schema);
    fixture.componentRef.setInput('form', form);
    fixture.detectChanges();

    (fixture.nativeElement.querySelector('input') as HTMLInputElement).dispatchEvent(new Event('blur'));

    expect(blurValues.length).toBe(1);
    expect(String(blurValues[0]?.['name'] ?? '')).toBe('Ada');
  });

  it('renders a static suffix without including it in the scalar value', () => {
    const schema: ChillSchema = {
      properties: [{ name: 'amount', propertyType: CHILL_PROPERTY_TYPE.Decimal, isNullable: false, metadata: { staticSuffix: '€' } }]
    };
    const form = new FormGroup<Record<string, FormControl<JsonValue>>>({ amount: new FormControl<JsonValue>(12.5) });
    fixture.componentRef.setInput('schema', schema);
    fixture.componentRef.setInput('form', form);
    fixture.detectChanges();
    expect(String(form.controls['amount'].value)).toBe('12.5');
    expect(fixture.nativeElement.querySelector('.scalar-input__suffix')?.textContent).toContain('€');
  });
});
