import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { PermissionAction, PermissionEffect, PermissionScope } from '../../models/chill-auth.models';
import { ChillService } from '../../services/chill.service';
import type { PermissionEditorRow } from './permission-editor.component';
import { PermissionMatrixComponent } from './permission-matrix.component';

describe('PermissionMatrixComponent', () => {
  async function createComponent(): Promise<ComponentFixture<PermissionMatrixComponent>> {
    await TestBed.configureTestingModule({
      imports: [PermissionMatrixComponent],
      providers: [{
        provide: ChillService,
        useValue: {
          T: (_labelGuid: string, primaryDefaultText: string) => primaryDefaultText,
          formatError: (error: unknown) => `${error ?? ''}`,
          getModuleList: () => of(['Sales']),
          getEntityList: () => of(['Invoice']),
          getQueryList: () => of(['OpenInvoices']),
          getCommandList: () => of(['ApproveInvoice']),
          getPropertyList: () => of(['Total'])
        }
      }]
    }).compileComponents();

    const fixture = TestBed.createComponent(PermissionMatrixComponent);
    fixture.detectChanges();
    return fixture;
  }

  function rule(
    effect: PermissionEffect,
    action: PermissionAction,
    scope: PermissionScope,
    module = 'Sales',
    entityName = '',
    propertyName = ''
  ): PermissionEditorRow {
    return {
      localId: `${effect}-${action}-${scope}-${entityName}-${propertyName}`,
      guid: '',
      effect,
      action,
      scope,
      module,
      entityName,
      propertyName,
      appliesToAllProperties: false,
      description: ''
    };
  }

  function permissionCell(fixture: ComponentFixture<PermissionMatrixComponent>, label: string): HTMLButtonElement | null {
    return fixture.nativeElement.querySelector(`button.permission-state[aria-label="${label}"]`);
  }

  function expand(fixture: ComponentFixture<PermissionMatrixComponent>, label: string): void {
    const button = fixture.nativeElement.querySelector(`button.tree-toggle[aria-label="Expand ${label}"]`) as HTMLButtonElement;
    button.click();
    fixture.detectChanges();
  }

  it('shows modules collapsed and expands through entities to properties', async () => {
    const fixture = await createComponent();
    expect(fixture.nativeElement.textContent).toContain('Sales');
    expect(fixture.nativeElement.textContent).not.toContain('Entities');

    expand(fixture, 'Sales');
    expect(fixture.nativeElement.textContent).toContain('Entities');
    expect(fixture.nativeElement.textContent).toContain('Queries');
    expect(fixture.nativeElement.textContent).toContain('Commands');
    expect(fixture.nativeElement.textContent).not.toContain('Invoice');

    expand(fixture, 'Entities');
    expect(fixture.nativeElement.textContent).toContain('Invoice');
    expect(fixture.nativeElement.textContent).not.toContain('Total');

    expand(fixture, 'Invoice');
    expect(fixture.nativeElement.textContent).toContain('Total');
  });

  it('renders role allows and denies as outlined inherited states', async () => {
    const fixture = await createComponent();
    fixture.componentRef.setInput('inheritedRows', [
      rule(PermissionEffect.Allow, PermissionAction.Query, PermissionScope.Module),
      rule(PermissionEffect.Deny, PermissionAction.Create, PermissionScope.Module)
    ]);
    fixture.detectChanges();

    expect(permissionCell(fixture, 'Sales, Query: Inherited allow')?.classList.contains('inherited-allow')).toBeTrue();
    expect(permissionCell(fixture, 'Sales, Create: Inherited deny')?.classList.contains('inherited-deny')).toBeTrue();
  });

  it('shows a direct user override filled instead of the inherited role state', async () => {
    const fixture = await createComponent();
    fixture.componentRef.setInput('inheritedRows', [
      rule(PermissionEffect.Allow, PermissionAction.Query, PermissionScope.Module)
    ]);
    fixture.componentRef.setInput('rows', [
      rule(PermissionEffect.Deny, PermissionAction.Query, PermissionScope.Module)
    ]);
    fixture.detectChanges();

    expect(permissionCell(fixture, 'Sales, Query: Deny')?.classList.contains('deny')).toBeTrue();
    expect(permissionCell(fixture, 'Sales, Query: Inherited allow')).toBeNull();
  });

  it('cycles a local permission through allow, deny, and unchecked', async () => {
    const fixture = await createComponent();
    let emittedRows: PermissionEditorRow[] | undefined;
    fixture.componentInstance.rowsChange.subscribe((rows) => emittedRows = rows);

    permissionCell(fixture, 'Sales, Query: Unchecked')?.click();
    expect(emittedRows?.[0].effect).toBe(PermissionEffect.Allow);
    fixture.componentRef.setInput('rows', emittedRows ?? []);
    fixture.detectChanges();
    expect(permissionCell(fixture, 'Sales, Query: Allow')?.classList.contains('allow')).toBeTrue();

    permissionCell(fixture, 'Sales, Query: Allow')?.click();
    expect(emittedRows?.[0].effect).toBe(PermissionEffect.Deny);
    fixture.componentRef.setInput('rows', emittedRows ?? []);
    fixture.detectChanges();
    expect(permissionCell(fixture, 'Sales, Query: Deny')?.classList.contains('deny')).toBeTrue();

    permissionCell(fixture, 'Sales, Query: Deny')?.click();
    expect(emittedRows).toEqual([]);
  });
});