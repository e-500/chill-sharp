import { OnDestroy } from '@angular/core';
import { type CrudPageComponentConfiguration } from '../../pages/crud/crud-page.component';
import type { ChillEntity } from '../../models/chill-schema.models';
import type { WorkspaceTaskComponentInterface, WorkspaceTaskConfiguration } from '../../models/workspace-task.models';
import { ChillService } from '../../services/chill.service';
import { WorkspaceDialogService } from '../../services/workspace-dialog.service';
import { WorkspaceToolbarService } from '../../services/workspace-toolbar.service';
import * as i0 from "@angular/core";
export declare class CrudTaskComponent implements WorkspaceTaskComponentInterface, OnDestroy {
    static getComponentConfigurationJsonExample(): WorkspaceTaskConfiguration | null;
    readonly chill: ChillService;
    readonly dialog: WorkspaceDialogService | null;
    readonly toolbar: WorkspaceToolbarService;
    readonly selectionEnabled: import("@angular/core").InputSignal<boolean>;
    readonly multipleSelection: import("@angular/core").InputSignal<boolean>;
    readonly initialSelectedEntity: import("@angular/core").InputSignal<ChillEntity | null>;
    readonly initialSelectedEntities: import("@angular/core").InputSignal<ChillEntity[]>;
    readonly componentConfiguration: import("@angular/core").InputSignal<WorkspaceTaskConfiguration | null>;
    readonly taskTitle: import("@angular/core").InputSignal<string>;
    readonly taskDescription: import("@angular/core").InputSignal<string>;
    readonly toolbarScope: import("@angular/core").InputSignal<string>;
    readonly visible: import("@angular/core").InputSignal<boolean>;
    private readonly page;
    resolvedComponentConfiguration(): CrudPageComponentConfiguration | null;
    constructor();
    submit(): void;
    canDialogSubmit(): boolean;
    isAllSaved(): boolean;
    ngOnDestroy(): void;
    static ɵfac: i0.ɵɵFactoryDeclaration<CrudTaskComponent, never>;
    static ɵcmp: i0.ɵɵComponentDeclaration<CrudTaskComponent, "app-crud-task", never, { "selectionEnabled": { "alias": "selectionEnabled"; "required": false; "isSignal": true; }; "multipleSelection": { "alias": "multipleSelection"; "required": false; "isSignal": true; }; "initialSelectedEntity": { "alias": "initialSelectedEntity"; "required": false; "isSignal": true; }; "initialSelectedEntities": { "alias": "initialSelectedEntities"; "required": false; "isSignal": true; }; "componentConfiguration": { "alias": "componentConfiguration"; "required": false; "isSignal": true; }; "taskTitle": { "alias": "taskTitle"; "required": false; "isSignal": true; }; "taskDescription": { "alias": "taskDescription"; "required": false; "isSignal": true; }; "toolbarScope": { "alias": "toolbarScope"; "required": false; "isSignal": true; }; "visible": { "alias": "visible"; "required": false; "isSignal": true; }; }, {}, never, never, true, never>;
}
//# sourceMappingURL=crud-task.component.d.ts.map