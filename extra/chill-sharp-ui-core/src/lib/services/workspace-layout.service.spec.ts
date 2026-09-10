import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { WORKSPACE_LAYOUT_EDITING_STORAGE_KEY } from '../storage-keys';
import { ChillService } from './chill.service';
import { WorkspaceLayoutService } from './workspace-layout.service';

describe('WorkspaceLayoutService authorization', () => {
  const canManageSchema = signal(false);

  beforeEach(() => {
    localStorage.setItem(WORKSPACE_LAYOUT_EDITING_STORAGE_KEY, 'true');
    TestBed.configureTestingModule({
      providers: [
        WorkspaceLayoutService,
        { provide: ChillService, useValue: { canManageSchema: canManageSchema.asReadonly() } }
      ]
    });
  });

  afterEach(() => {
    localStorage.removeItem(WORKSPACE_LAYOUT_EDITING_STORAGE_KEY);
    canManageSchema.set(false);
    TestBed.resetTestingModule();
  });

  it('does not expose or toggle layout editing without schema-management access', () => {
    const service = TestBed.inject(WorkspaceLayoutService);

    expect(service.canEditLayout()).toBeFalse();
    expect(service.isLayoutEditingEnabled()).toBeFalse();

    service.toggleLayoutEditingEnabled();

    expect(service.isLayoutEditingEnabled()).toBeFalse();
  });

  it('allows the stored layout-editing preference once schema-management access is granted', () => {
    const service = TestBed.inject(WorkspaceLayoutService);

    canManageSchema.set(true);

    expect(service.canEditLayout()).toBeTrue();
    expect(service.isLayoutEditingEnabled()).toBeTrue();
  });
});
