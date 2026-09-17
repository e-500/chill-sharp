import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { WorkspaceService, type WorkspaceTaskInstance } from '../services/workspace.service';

@Component({
  selector: 'app-workspace-taskbar',
  standalone: true,
  imports: [CommonModule, FormsModule],
  styles: `
    .taskbar {
      min-width: 0;
      width: 100%;
    }

    .taskbar__mobile {
      display: none;
      width: 100%;
    }

    .taskbar__mobile-select {
      width: 100%;
      height: 3rem;
      min-height: 3rem;
      border: 1px solid var(--border-color);
      border-radius: 3rem;
      background: var(--surface-0);
      color: var(--text-main);
      padding: 0.5rem 0.75rem;
      font: inherit;
    }

    @media (max-width: 720px) {
      .taskbar__desktop {
        display: none;
      }

      .taskbar__mobile {
        display: block;
        flex: 1 1 auto;
        min-width: 0;
      }
    }
  `,
  template: `
    <div class="taskbar">
      @if (workspace.openTasks().length === 0) {
        <p class="taskbar__empty">Open tasks will appear here.</p>
      } @else {
        <div class="taskbar__mobile">
          <select
            class="taskbar__mobile-select"
            [ngModel]="workspace.activeTask()?.id ?? ''"
            (ngModelChange)="activateTask($event)">
            @for (task of workspace.openTasks(); track task.id) {
              <option [value]="task.id">{{ task.title }}</option>
            }
          </select>
        </div>

        <div class="taskbar__desktop">
          @for (task of workspace.openTasks(); track task.id) {
            <div class="taskbar__item" [class.active]="workspace.activeTask()?.id === task.id">
              <button type="button" class="taskbar__link" (click)="activateTask(task.id)">
                @if (taskBreadcrumb(task); as breadcrumb) {
                  <span class="taskbar__breadcrumb">{{ breadcrumb }} &gt;</span>
                }
                {{ task.title }}
              </button>
              <button
                type="button"
                class="taskbar__close"
                (click)="closeTask(task.id)"
                [attr.aria-label]="'Close ' + task.title">
                x
              </button>
            </div>
          }
        </div>
      }
    </div>
  `
})
export class WorkspaceTaskbarComponent {
  readonly workspace = inject(WorkspaceService);

  activateTask(taskId: string): void {
    void this.workspace.activateTask(taskId);
  }

  closeTask(taskId: string): void {
    void this.workspace.closeTask(taskId);
  }

  taskBreadcrumb(task: WorkspaceTaskInstance): string {
    if (!task.parentTaskId) {
      return '';
    }

    const componentConfiguration = task.inputs?.['componentConfiguration'];
    if (!this.isRecord(componentConfiguration)) {
      return '';
    }

    const fixedQueryValues = componentConfiguration['fixedQueryValues'];
    if (!this.isRecord(fixedQueryValues)) {
      return '';
    }

    return Object.values(fixedQueryValues)
      .map((value) => this.entityBreadcrumbLabel(value))
      .filter((label): label is string => !!label)
      .join(', ');
  }

  private entityBreadcrumbLabel(value: unknown): string | null {
    if (!this.isRecord(value)) {
      return null;
    }

    const properties = this.isRecord(value['properties']) ? value['properties'] : null;
    return this.readText(value, ['shortLabel', 'ShortLabel'])
      || this.readText(properties, ['shortLabel', 'ShortLabel'])
      || this.readText(value, ['label', 'Label'])
      || this.readText(properties, ['label', 'Label'])
      || this.readText(value, ['guid', 'Guid']).slice(0, 8)
      || null;
  }

  private readText(record: Record<string, unknown> | null, keys: string[]): string {
    if (!record) {
      return '';
    }

    for (const key of keys) {
      const value = record[key];
      if (typeof value === 'string' && value.trim()) {
        return value.trim();
      }
    }

    return '';
  }

  private isRecord(value: unknown): value is Record<string, unknown> {
    return !!value && typeof value === 'object' && !Array.isArray(value);
  }
}
