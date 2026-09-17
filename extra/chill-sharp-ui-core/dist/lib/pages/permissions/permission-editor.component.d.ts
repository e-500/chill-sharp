import type { EditableAuthPermissionRule } from '../../models/chill-auth.models';
import { PermissionAction, PermissionEffect, PermissionScope } from '../../models/chill-auth.models';
import { ChillService } from '../../services/chill.service';
import * as i0 from "@angular/core";
export interface PermissionEditorRow extends EditableAuthPermissionRule {
    localId: string;
}
export declare class PermissionEditorComponent {
    readonly chill: ChillService;
    readonly rows: import("@angular/core").InputSignal<PermissionEditorRow[]>;
    readonly rowsChange: import("@angular/core").OutputEmitterRef<PermissionEditorRow[]>;
    readonly lookupErrorMessage: import("@angular/core").WritableSignal<string>;
    readonly moduleOptions: import("@angular/core").WritableSignal<string[]>;
    readonly entityOptionsByKey: import("@angular/core").WritableSignal<Record<string, string[]>>;
    readonly propertyOptionsByKey: import("@angular/core").WritableSignal<Record<string, string[]>>;
    readonly editingRowIds: import("@angular/core").WritableSignal<string[]>;
    readonly permissionEffectOptions: {
        value: PermissionEffect;
        label: string;
    }[];
    readonly permissionActionOptions: {
        value: PermissionAction;
        label: string;
    }[];
    readonly permissionScopeOptions: {
        value: PermissionScope;
        label: string;
    }[];
    constructor();
    addPermissionRule(): void;
    startEditingRow(rowId: string): void;
    stopEditingRow(rowId: string): void;
    isEditingRow(rowId: string): boolean;
    updatePermissionRow(rowId: string, key: keyof PermissionEditorRow, value: PermissionEditorRow[keyof PermissionEditorRow]): void;
    updatePropertySelection(rowId: string, value: string): void;
    removePermissionRow(rowId: string): void;
    effectLabel(value: number): string;
    actionLabel(value: number): string;
    scopeLabel(value: number): string;
    targetLabel(row: PermissionEditorRow): string;
    descriptionLabel(row: PermissionEditorRow): string;
    entityOptionsFor(row: PermissionEditorRow): string[];
    propertyOptionsFor(row: PermissionEditorRow): string[];
    propertySelectValueFor(row: PermissionEditorRow): string;
    private loadModuleOptions;
    private ensureOptionsForRows;
    private syncEditingRows;
    private ensureEntityOptions;
    private ensurePropertyOptions;
    private entityOptionsKey;
    private propertyOptionsKey;
    static ɵfac: i0.ɵɵFactoryDeclaration<PermissionEditorComponent, never>;
    static ɵcmp: i0.ɵɵComponentDeclaration<PermissionEditorComponent, "app-permission-editor", never, { "rows": { "alias": "rows"; "required": false; "isSignal": true; }; }, { "rowsChange": "rowsChange"; }, never, never, true, never>;
}
//# sourceMappingURL=permission-editor.component.d.ts.map