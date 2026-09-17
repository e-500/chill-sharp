import type { WorkspaceDialogRequest, WorkspaceDialogResult } from '../models/workspace-dialog.models';
import * as i0 from "@angular/core";
interface ActiveWorkspaceDialog<TResult = unknown> extends WorkspaceDialogRequest<TResult> {
    id: number;
    resolve: (result: WorkspaceDialogResult<TResult>) => void;
}
export declare class WorkspaceDialogService {
    private readonly dialogStackState;
    private nextDialogId;
    readonly dialogs: import("@angular/core").Signal<ActiveWorkspaceDialog<unknown>[]>;
    readonly activeDialog: import("@angular/core").Signal<ActiveWorkspaceDialog<unknown> | null>;
    openDialog<TResult>(request: WorkspaceDialogRequest<TResult>): Promise<WorkspaceDialogResult<TResult>>;
    confirmOk(title: string, description: string): Promise<boolean>;
    confirmYesNo(title: string, description: string): Promise<boolean>;
    confirm<TResult>(value?: TResult): void;
    cancel(): void;
    private cancelActiveDialog;
    static ɵfac: i0.ɵɵFactoryDeclaration<WorkspaceDialogService, never>;
    static ɵprov: i0.ɵɵInjectableDeclaration<WorkspaceDialogService>;
}
export {};
//# sourceMappingURL=workspace-dialog.service.d.ts.map