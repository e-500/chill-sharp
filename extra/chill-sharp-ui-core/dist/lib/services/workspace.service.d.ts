import { ParamMap } from '@angular/router';
import type { CrudPageComponentConfiguration } from '../pages/crud/crud-page.component';
import type { ChillMenuItem } from '../models/chill-menu.models';
import type { WorkspaceTaskComponent, WorkspaceTaskComponentType, WorkspaceTaskConfiguration } from '../models/workspace-task.models';
import { WorkspaceTaskDefinition } from './workspace-task-registry.service';
import * as i0 from "@angular/core";
export type WorkspaceTheme = 'bright' | 'dark' | 'soft' | 'cini';
interface WorkspaceTaskRoute {
    taskType: string;
    queryParams?: Record<string, string>;
}
export interface WorkspaceTaskInstance {
    id: string;
    taskType: string;
    title: string;
    description: string;
    component: WorkspaceTaskComponentType;
    toolbarScope: string;
    menuItemGuid?: string | null;
    inputs?: Record<string, unknown>;
    route: WorkspaceTaskRoute;
}
export interface OpenCrudTaskRequest {
    chillType: string;
    viewCode?: string | null;
    displayName?: string | null;
    queryChillType?: string | null;
    componentConfiguration?: CrudPageComponentConfiguration | null;
}
export interface OpenWorkspaceTaskRequest {
    componentName: string;
    title?: string | null;
    description?: string | null;
    configuration?: WorkspaceTaskConfiguration | null;
}
export declare class WorkspaceService {
    private readonly document;
    private readonly router;
    private readonly chill;
    private readonly dialog;
    private readonly layout;
    private readonly taskRegistry;
    private readonly destroyRef;
    private readonly drawerOpenState;
    private readonly activeTaskIdState;
    private readonly openTaskInstancesState;
    private taskComponentResolver;
    private readonly storedThemePreference;
    private readonly hasExplicitThemePreferenceState;
    private readonly themeState;
    readonly availableTasks: import("@angular/core").Signal<WorkspaceTaskDefinition[]>;
    readonly isDrawerOpen: import("@angular/core").Signal<boolean>;
    readonly theme: import("@angular/core").Signal<WorkspaceTheme>;
    readonly isLayoutEditingEnabled: import("@angular/core").Signal<boolean>;
    readonly openTasks: import("@angular/core").Signal<WorkspaceTaskInstance[]>;
    readonly activeTask: import("@angular/core").Signal<WorkspaceTaskInstance | null>;
    constructor();
    activateTaskFromRoute(taskType: string | null, queryParams: ParamMap): Promise<void>;
    openTask(componentName: string, navigate?: boolean): Promise<void>;
    openWorkspaceTask(request: OpenWorkspaceTaskRequest): Promise<void>;
    openCrudTask(request: OpenCrudTaskRequest): void;
    openMenuItem(item: ChillMenuItem): Promise<void>;
    isMenuItemActive(item: ChillMenuItem): boolean;
    activateTask(taskInstanceId: string): Promise<void>;
    closeTask(taskInstanceId: string): Promise<void>;
    toggleDrawer(): void;
    closeDrawer(): void;
    setTheme(theme: WorkspaceTheme): void;
    toggleLayoutEditingEnabled(): void;
    reset(): Promise<boolean>;
    registerTaskComponentResolver(resolver: ((taskId: string) => WorkspaceTaskComponent | null) | null): void;
    canUnloadWorkspace(): boolean;
    private openTaskInstance;
    private navigateToTask;
    private resolveTaskFromRoute;
    private createStaticTaskInstance;
    private createMenuTaskInstance;
    private getTaskDefinition;
    private findTaskByRoute;
    private findTaskByMenuItemGuid;
    private isSameTaskRoute;
    private bindSystemThemePreference;
    private readStoredThemePreference;
    private readSystemThemePreference;
    private createDefaultRoute;
    private buildWorkspaceQueryParams;
    private readActiveMenuItemGuid;
    private findActiveTaskIdForMenuItemGuid;
    private findTaskIdByMenuItemGuid;
    private restoreMenuTaskFromRoute;
    private loadMenuItemsByGuids;
    private confirmAllTasksCanBeClosed;
    private confirmTaskCanBeLeft;
    private normalizeComponentName;
    private normalizeConfiguration;
    private serializeConfiguration;
    private deserializeConfiguration;
    private parseMenuConfiguration;
    private toWorkspaceTaskConfiguration;
    private buildCrudTaskConfiguration;
    private readConfigString;
    static ɵfac: i0.ɵɵFactoryDeclaration<WorkspaceService, never>;
    static ɵprov: i0.ɵɵInjectableDeclaration<WorkspaceService>;
}
export {};
//# sourceMappingURL=workspace.service.d.ts.map