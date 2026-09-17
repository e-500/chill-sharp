import { ChillService } from '../services/chill.service';
import { WorkspaceDialogService } from '../services/workspace-dialog.service';
import { WorkspaceToolbarService } from '../services/workspace-toolbar.service';
import * as i0 from "@angular/core";
export declare class WorkspaceDialogHostComponent {
    readonly chill: ChillService;
    readonly dialog: WorkspaceDialogService;
    readonly toolbar: WorkspaceToolbarService;
    private readonly contentHosts;
    readonly isBusy: import("@angular/core").WritableSignal<boolean>;
    readonly errorMessage: import("@angular/core").WritableSignal<string>;
    readonly toolbarButtons: import("@angular/core").Signal<import("@chill-sharp/ui-core").WorkspaceToolbarButton[]>;
    private readonly contentRefs;
    private readonly activeDialog;
    private activeDialogId;
    constructor();
    isTopDialog(dialogId: number): boolean;
    cancel(dialogId?: number): void;
    confirm(dialogId?: number): Promise<void>;
    canConfirm(): boolean;
    private isDialogTask;
    private isDialogSubmitter;
    static ɵfac: i0.ɵɵFactoryDeclaration<WorkspaceDialogHostComponent, never>;
    static ɵcmp: i0.ɵɵComponentDeclaration<WorkspaceDialogHostComponent, "app-workspace-dialog-host", never, {}, {}, never, never, true, never>;
}
//# sourceMappingURL=workspace-dialog-host.component.d.ts.map