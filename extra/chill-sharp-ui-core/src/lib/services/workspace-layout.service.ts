import { Injectable, computed, effect, inject, signal } from '@angular/core';
import { WORKSPACE_LAYOUT_EDITING_STORAGE_KEY } from '../storage-keys';
import { ChillService } from './chill.service';

@Injectable({
  providedIn: 'root'
})
export class WorkspaceLayoutService {
  private readonly chill = inject(ChillService);
  private readonly layoutEditingEnabledState = signal(this.readStoredLayoutEditingState());

  readonly canEditLayout = this.chill.canManageSchema;
  readonly isLayoutEditingEnabled = computed(() => this.canEditLayout() && this.layoutEditingEnabledState());

  constructor() {
    effect(() => {
      globalThis.localStorage?.setItem(
        WORKSPACE_LAYOUT_EDITING_STORAGE_KEY,
        this.layoutEditingEnabledState() ? 'true' : 'false'
      );
    });
  }

  toggleLayoutEditingEnabled(): void {
    if (!this.canEditLayout()) {
      return;
    }

    this.layoutEditingEnabledState.update((enabled) => !enabled);
  }

  private readStoredLayoutEditingState(): boolean {
    return globalThis.localStorage?.getItem(WORKSPACE_LAYOUT_EDITING_STORAGE_KEY)?.trim().toLowerCase() === 'true';
  }
}
