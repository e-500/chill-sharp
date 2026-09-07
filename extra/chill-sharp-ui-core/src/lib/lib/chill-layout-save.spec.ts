import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { FormControl, FormGroup } from '@angular/forms';
import { Subject } from 'rxjs';
import { ChillSchema } from '../models/chill-schema.models';
import { ChillService } from '../services/chill.service';
import { WorkspaceLayoutService } from '../services/workspace-layout.service';
import { ChillFormComponent } from './chill-form.component';
import { ChillTableComponent } from './chill-table.component';

describe('Layout save propagation', () => {
  let response: Subject<ChillSchema | null>;
  let setSchema: jasmine.Spy;
  let schema: ChillSchema;

  beforeEach(async () => {
    response = new Subject<ChillSchema | null>();
    setSchema = jasmine.createSpy('setSchema').and.callFake(() => response);
    schema = {
      chillType: 'Model.Product',
      chillViewCode: 'default',
      metadata: { unrelated: 'preserved' },
      properties: [
        { name: 'Name', displayName: 'Name', isNullable: false },
        { name: 'Price', displayName: 'Price', isNullable: false }
      ]
    };
    await TestBed.configureTestingModule({
      imports: [ChillFormComponent, ChillTableComponent],
      providers: [
        { provide: ChillService, useValue: {
          setSchema,
          T: (_id: string, english: string) => english,
          formatError: (error: Error) => error.message,
          prepareForm: () => new FormGroup({ Name: new FormControl('unsaved value') })
        } },
        { provide: WorkspaceLayoutService, useValue: { isLayoutEditingEnabled: signal(true) } }
      ]
    })
      .overrideComponent(ChillTableComponent, { set: { template: '', imports: [] } })
      .overrideComponent(ChillFormComponent, { set: { template: '', imports: [] } })
      .compileComponents();
  });

  it('publishes each saved table layout and refreshes columns from the server response', () => {
    const fixture = TestBed.createComponent(ChillTableComponent);
    fixture.componentRef.setInput('schema', schema);
    fixture.detectChanges();
    const component = fixture.componentInstance;
    const updates: ChillSchema[] = [];
    component.schemaUpdated.subscribe((updated) => updates.push(structuredClone(updated)));

    for (const hidden of [true, false]) {
      response = new Subject<ChillSchema | null>();
      component.toggleEditLayoutMode();
      component.updateColumnHidden('Name', hidden);
      component.toggleEditLayoutMode();
      expect(component.isSavingLayout()).toBeTrue();
      const request = setSchema.calls.mostRecent().args[0] as ChillSchema;
      expect(String(request.metadata?.['unrelated'])).toBe('preserved');
      const saved = structuredClone(request);
      saved.properties![0].metadata = { widthProportion: '3' };
      response.next(saved);
      response.complete();
      fixture.detectChanges();

      expect(component.isSavingLayout()).toBeFalse();
      expect(component.isEditLayoutMode()).toBeFalse();
      expect(component.columns().find((column) => column.name === 'Name')?.hidden).toBe(hidden);
      expect(component.columns().find((column) => column.name === 'Name')?.widthProportion).toBe(3);
      expect(JSON.stringify(schema.metadata)).toBe(JSON.stringify(saved.metadata));
      expect(JSON.stringify(updates.at(-1)?.metadata)).toBe(JSON.stringify(saved.metadata));
    }
    expect(updates.length).toBe(2);
  });

  it('publishes the returned form layout while preserving unsaved form controls', () => {
    const fixture = TestBed.createComponent(ChillFormComponent);
    fixture.componentRef.setInput('schema', schema);
    fixture.detectChanges();
    const component = fixture.componentInstance;
    const originalForm = component.form();
    const updated = jasmine.createSpy('schemaUpdated');
    component.schemaUpdated.subscribe(updated);
    component.toggleEditMode();
    component.layoutState.update((layout) => ({ ...layout, columnCount: 3 }));
    component.toggleEditMode();
    const request = setSchema.calls.mostRecent().args[0] as ChillSchema;
    const saved = structuredClone(request);
    const savedLayout = { ...component.layoutState(), columnCount: 4 };
    saved.metadata!['chill-form-component'] = JSON.stringify(savedLayout);
    response.next(saved);
    response.complete();
    fixture.detectChanges();

    expect(component.layoutState()).toEqual(savedLayout);
    expect(updated).toHaveBeenCalledOnceWith(schema);
    expect(JSON.stringify(schema.metadata)).toBe(JSON.stringify(saved.metadata));
    expect(component.form()).toBe(originalForm);
    expect(String(component.form()?.getRawValue()['Name'])).toBe('unsaved value');
    expect(component.isSavingLayout()).toBeFalse();
    expect(component.isEditMode()).toBeFalse();
  });

  it('keeps the persisted schema and parent unchanged when a layout save fails', () => {
    const fixture = TestBed.createComponent(ChillTableComponent);
    fixture.componentRef.setInput('schema', schema);
    fixture.detectChanges();
    const component = fixture.componentInstance;
    const updated = jasmine.createSpy('schemaUpdated');
    component.schemaUpdated.subscribe(updated);
    component.toggleEditLayoutMode();
    component.updateColumnHidden('Name', true);
    component.toggleEditLayoutMode();
    response.error(new Error('Save failed'));

    expect(updated).not.toHaveBeenCalled();
    expect(JSON.stringify(schema.metadata)).toBe(JSON.stringify({ unrelated: 'preserved' }));
    expect(component.isSavingLayout()).toBeFalse();
    expect(component.isEditLayoutMode()).toBeTrue();
    expect(component.layoutError()).toBe('Save failed');
  });
});
