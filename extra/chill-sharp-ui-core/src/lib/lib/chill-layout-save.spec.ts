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
  let getSchema: jasmine.Spy;
  let schema: ChillSchema;

  beforeEach(async () => {
    response = new Subject<ChillSchema | null>();
    setSchema = jasmine.createSpy('setSchema').and.callFake(() => response);
    getSchema = jasmine.createSpy('getSchema').and.callFake(() => response);
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
          getSchema,
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

  it('saves and restores a new column after schema refresh and CRUD parent propagation', () => {
    schema.metadata!['chill-table-component'] = JSON.stringify({
      columns: [{ name: 'Name', displayName: 'Product name', hidden: false }]
    });
    schema.properties = schema.properties!.filter((property) => property.name === 'Name');
    const fixture = TestBed.createComponent(ChillTableComponent);
    fixture.componentRef.setInput('schema', structuredClone(schema));
    fixture.detectChanges();
    const component = fixture.componentInstance;
    // The CRUD page sends a new schema input back to the table for each update.
    component.schemaUpdated.subscribe((updated) => {
      fixture.componentRef.setInput('schema', structuredClone(updated));
    });

    component.toggleEditLayoutMode();
    component.refreshSchemaFromModel();
    expect(getSchema).toHaveBeenCalledOnceWith('Model.Product', 'default', undefined, true);
    const refreshedSchema = structuredClone(schema);
    refreshedSchema.properties!.push({ name: 'Price', displayName: 'Price', isNullable: false });
    response.next(refreshedSchema);
    response.complete();
    fixture.detectChanges();

    expect(component.isEditLayoutMode()).toBeTrue();
    expect(component.columns().map((column) => column.name)).toEqual(['Name', 'Price']);
    component.updateColumnDisplayName('Price', 'Retail price');
    component.updateColumnHidden('Name', true);
    response = new Subject<ChillSchema | null>();
    component.toggleEditLayoutMode();
    expect(setSchema).toHaveBeenCalledTimes(1);
    if (!setSchema.calls.any()) {
      return;
    }
    const persistedSchema = structuredClone(setSchema.calls.mostRecent().args[0] as ChillSchema);
    response.next(structuredClone(persistedSchema));
    response.complete();
    fixture.detectChanges();
    expect(component.isEditLayoutMode()).toBeFalse();
    fixture.destroy();

    const reopenedFixture = TestBed.createComponent(ChillTableComponent);
    reopenedFixture.componentRef.setInput('schema', persistedSchema);
    reopenedFixture.detectChanges();
    expect(reopenedFixture.componentInstance.visibleColumns().map((column) => column.name)).toEqual(['Price']);
    expect(reopenedFixture.componentInstance.visibleColumns()[0].displayName).toBe('Retail price');
    expect(reopenedFixture.componentInstance.hiddenColumns().map((column) => column.name)).toEqual(['Name']);
  });

  for (const replacement of [null, { chillType: 'Model.Category' }, { chillViewCode: 'details' }]) {
    it(`exits layout editing when the schema changes to ${JSON.stringify(replacement)}`, () => {
      const fixture = TestBed.createComponent(ChillTableComponent);
      fixture.componentRef.setInput('schema', schema);
      fixture.detectChanges();
      fixture.componentInstance.toggleEditLayoutMode();
      fixture.componentRef.setInput('schema', replacement ? { ...schema, ...replacement } : null);
      fixture.detectChanges();

      expect(fixture.componentInstance.isEditLayoutMode()).toBeFalse();
      expect(setSchema).not.toHaveBeenCalled();
    });
  }

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
