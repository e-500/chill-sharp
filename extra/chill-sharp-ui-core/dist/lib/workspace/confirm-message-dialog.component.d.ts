import { WorkspaceDialogService } from '../services/workspace-dialog.service';
import * as i0 from "@angular/core";
export interface ConfirmMessageDialogButton<TResult = unknown> {
    label: string;
    value: TResult;
    primary?: boolean;
}
export declare class ConfirmMessageDialogComponent {
    readonly dialog: WorkspaceDialogService;
    readonly description: import("@angular/core").InputSignal<string>;
    readonly buttons: import("@angular/core").InputSignal<ConfirmMessageDialogButton<unknown>[]>;
    select(value: unknown): void;
    static ɵfac: i0.ɵɵFactoryDeclaration<ConfirmMessageDialogComponent, never>;
    static ɵcmp: i0.ɵɵComponentDeclaration<ConfirmMessageDialogComponent, "app-confirm-message-dialog", never, { "description": { "alias": "description"; "required": false; "isSignal": true; }; "buttons": { "alias": "buttons"; "required": false; "isSignal": true; }; }, {}, never, never, true, never>;
}
//# sourceMappingURL=confirm-message-dialog.component.d.ts.map