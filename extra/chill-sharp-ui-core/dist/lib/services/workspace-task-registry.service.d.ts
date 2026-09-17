import type { WorkspaceTaskComponentType } from '../models/workspace-task.models';
import * as i0 from "@angular/core";
export interface WorkspaceTaskDefinition {
    componentName: string;
    title: string;
    description: string;
    kind: 'builtin' | 'remote';
    componentConfigurationJsonExample?: string | null;
    usesTaskConfigurationInputs?: boolean;
    showInQuickLaunch: boolean;
    loadComponent: () => Promise<WorkspaceTaskComponentType>;
}
export declare class WorkspaceTaskRegistryService {
    private readonly document;
    private readonly definitionsState;
    private readonly initializationErrorState;
    private readonly remoteEntryLoads;
    private readonly remoteComponentLoads;
    private initialized;
    readonly definitions: import("@angular/core").Signal<WorkspaceTaskDefinition[]>;
    readonly initializationError: import("@angular/core").Signal<string>;
    initialize(): Promise<void>;
    getTaskDefinition(componentName: string): WorkspaceTaskDefinition | null;
    resolveComponent(componentName: string): Promise<WorkspaceTaskComponentType | null>;
    private registerBuiltInTasks;
    private loadRemoteTaskSources;
    private registerRemoteTask;
    private registerDefinition;
    private serializeComponentConfigurationJsonExample;
    private loadRemoteComponent;
    private ensureRemoteEntry;
    static ɵfac: i0.ɵɵFactoryDeclaration<WorkspaceTaskRegistryService, never>;
    static ɵprov: i0.ɵɵInjectableDeclaration<WorkspaceTaskRegistryService>;
}
//# sourceMappingURL=workspace-task-registry.service.d.ts.map