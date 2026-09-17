import { FormControl, FormGroup } from '@angular/forms';
import type { JsonValue } from '@chill-sharp/ng-client';
import { type ChillEntity, type ChillOrdering, type ChillPropertySchema, type ChillSchema } from '../models/chill-schema.models';
import { ChillService } from '../services/chill.service';
import { WorkspaceDialogService } from '../services/workspace-dialog.service';
import { WorkspaceLayoutService } from '../services/workspace-layout.service';
import * as i0 from "@angular/core";
interface ColumnLayoutState {
    name: string;
    displayName: string;
    hidden: boolean;
    widthProportion: number;
}
type TableColumn = ChillPropertySchema & {
    hidden: boolean;
    displayName: string;
    widthProportion: number;
};
export interface ChillTableRowAction {
    icon?: string;
    iconClass?: string;
    label?: string;
    labelGuid?: string | null;
    primaryDefaultText?: string | null;
    secondaryDefaultText?: string | null;
    ariaLabel?: string;
    disabled?: (entity: ChillEntity) => boolean;
    handler: (entity: ChillEntity) => void;
}
export interface ChillTableSelectionColumn {
    ariaLabel?: string;
    disabled?: (entity: ChillEntity) => boolean;
    isSelected: (entity: ChillEntity) => boolean;
    toggle: (entity: ChillEntity, selected: boolean) => void;
}
export interface ChillTableCellEditCommitEvent {
    entity: ChillEntity;
    propertyName: string;
    value: JsonValue;
    dirtyProperties: string[];
}
export interface ChillTableValidationFocus {
    entityKey: string;
    propertyName: string;
}
export interface ChillTableSortChangeEvent {
    propertyName: string;
    direction: 'ASC' | 'DESC' | null;
}
interface ActiveCellEditState {
    entityKey: string;
    propertyName: string;
    entity: ChillEntity;
    form: FormGroup<Record<string, FormControl<JsonValue>>>;
    originalValue: JsonValue | null;
    isValid: boolean;
    isLookupDialogOpen: boolean;
    isCommitting: boolean;
}
interface ActiveRowActionMenuState {
    entityKey: string;
    top: number;
    left: number;
}
export declare class ChillTableComponent {
    readonly chill: ChillService;
    readonly dialog: WorkspaceDialogService | null;
    readonly layout: WorkspaceLayoutService;
    readonly schema: import("@angular/core").InputSignal<ChillSchema | null>;
    readonly entities: import("@angular/core").InputSignal<ChillEntity[]>;
    readonly selectionColumn: import("@angular/core").InputSignal<ChillTableSelectionColumn | null>;
    readonly rowAction: import("@angular/core").InputSignal<ChillTableRowAction | null>;
    readonly rowActions: import("@angular/core").InputSignal<ChillTableRowAction[] | null>;
    readonly enableInlineEditing: import("@angular/core").InputSignal<boolean>;
    readonly readonlyPropertyNames: import("@angular/core").InputSignal<string[] | null>;
    readonly validationFocus: import("@angular/core").InputSignal<ChillTableValidationFocus | null>;
    readonly showSchemaHeader: import("@angular/core").InputSignal<boolean>;
    readonly ordering: import("@angular/core").InputSignal<ChillOrdering | null>;
    readonly enableFullTextSearch: import("@angular/core").InputSignal<boolean>;
    readonly fullTextSearch: import("@angular/core").InputSignal<string>;
    readonly showMobileTaskClose: import("@angular/core").InputSignal<boolean>;
    readonly columnWidthOptions: readonly [0.25, 0.5, 1, 2, 3, 4, 5];
    readonly propertyTypeOptions: import("@chill-sharp/ui-core").ChillPropertyTypeOption[];
    readonly cellEditCommit: import("@angular/core").OutputEmitterRef<ChillTableCellEditCommitEvent>;
    readonly sortChange: import("@angular/core").OutputEmitterRef<ChillTableSortChangeEvent>;
    readonly fullTextSearchChange: import("@angular/core").OutputEmitterRef<string>;
    readonly schemaUpdated: import("@angular/core").OutputEmitterRef<ChillSchema>;
    readonly mobileTaskClose: import("@angular/core").OutputEmitterRef<void>;
    private readonly fullTextSearchInput;
    readonly isEditLayoutMode: import("@angular/core").WritableSignal<boolean>;
    readonly isSavingLayout: import("@angular/core").WritableSignal<boolean>;
    readonly isRefreshingSchema: import("@angular/core").WritableSignal<boolean>;
    readonly layoutError: import("@angular/core").WritableSignal<string>;
    readonly dragColumnName: import("@angular/core").WritableSignal<string>;
    readonly layoutState: import("@angular/core").WritableSignal<ColumnLayoutState[]>;
    readonly activeCellEdit: import("@angular/core").WritableSignal<ActiveCellEditState | null>;
    readonly activeRowActionMenu: import("@angular/core").WritableSignal<ActiveRowActionMenuState | null>;
    readonly displayedEntities: import("@angular/core").WritableSignal<ChillEntity[]>;
    readonly schemaRefreshTick: import("@angular/core").WritableSignal<number>;
    readonly isFullTextSearchOpen: import("@angular/core").WritableSignal<boolean>;
    readonly fullTextSearchText: import("@angular/core").WritableSignal<string>;
    readonly rowRefreshFlashKeys: import("@angular/core").WritableSignal<ReadonlySet<string>>;
    private readonly entityNotificationSubscriptions;
    private readonly rowRefreshFlashTimers;
    private subscribedNotificationChillType;
    /**
     * Wires reactive state for layout persistence, live entity updates, validation-driven focus, and inline edit completion.
     */
    constructor();
    /**
     * Releases per-entity live update subscriptions when the table is destroyed.
     */
    ngOnDestroy(): void;
    /**
     * Merges schema properties with persisted layout preferences and preserves the saved column order.
     */
    readonly columns: import("@angular/core").Signal<TableColumn[]>;
    /**
     * Filters the resolved column list down to visible columns.
     */
    readonly visibleColumns: import("@angular/core").Signal<TableColumn[]>;
    readonly tableMinimumWidth: import("@angular/core").Signal<string>;
    readonly mobileTableMinimumWidth: import("@angular/core").Signal<string>;
    /**
     * Filters the resolved column list down to hidden columns.
     */
    readonly hiddenColumns: import("@angular/core").Signal<TableColumn[]>;
    /**
     * Hides row selection while the user is editing layout metadata.
     */
    readonly hasSelectionColumn: import("@angular/core").Signal<boolean>;
    /**
     * Normalizes the single-action and multi-action inputs into one action list.
     */
    readonly resolvedRowActions: import("@angular/core").Signal<ChillTableRowAction[]>;
    /**
     * Hides row actions while the user is editing layout metadata.
     */
    readonly hasActionColumn: import("@angular/core").Signal<boolean>;
    readonly readonlyPropertyNameSet: import("@angular/core").Signal<Set<string>>;
    /**
     * Builds a stable row key, preferring Guid-like identifiers before falling back to labels or index.
     */
    trackByEntity(index: number, entity: ChillEntity): string;
    /**
     * Enters layout-edit mode immediately, or persists the current layout when toggled off.
     */
    toggleEditLayoutMode(): void;
    /**
     * Applies an in-memory display-name override for the selected column.
     */
    updateColumnDisplayName(columnName: string, value: string): void;
    /**
     * Marks a column as visible or hidden inside the pending layout state.
     */
    updateColumnHidden(columnName: string, hidden: boolean): void;
    toggleFullTextSearch(): void;
    updateFullTextSearchText(value: string): void;
    refreshSchemaFromModel(): void;
    submitFullTextSearch(): void;
    resetFullTextSearch(): void;
    closeMobileTask(): void;
    updateColumnWidthProportion(columnName: string, direction: -1 | 1): void;
    canDecreaseColumnWidth(column: TableColumn): boolean;
    canIncreaseColumnWidth(column: TableColumn): boolean;
    columnWidthLabel(column: TableColumn): string;
    columnWidthPercent(column: TableColumn): number;
    openPropertySettings(property: ChillPropertySchema): void;
    /**
     * Moves a hidden column back into the visible portion of the saved layout ordering.
     */
    revealColumn(columnName: string): void;
    /**
     * Records which column is being dragged during layout editing.
     */
    beginDrag(event: DragEvent, columnName: string): void;
    /**
     * Enables the column drop target only while layout editing is active.
     */
    allowDrop(event: DragEvent): void;
    /**
     * Reorders the pending layout by moving the dragged column onto the target position.
     */
    dropColumn(targetColumnName: string): void;
    /**
     * Clears the active drag marker after drag completes or is cancelled.
     */
    endDrag(): void;
    /**
     * Invokes the configured row action with the current entity.
     */
    runRowAction(action: ChillTableRowAction, entity: ChillEntity, menu?: HTMLDetailsElement): void;
    /**
     * Toggles the floating row-action menu anchored to the trigger button.
     */
    toggleRowActionMenu(event: MouseEvent, entity: ChillEntity): void;
    /**
     * Returns true when the floating row-action menu belongs to the provided entity.
     */
    isRowActionMenuOpen(entity: ChillEntity): boolean;
    /**
     * Closes the floating row-action menu.
     */
    closeRowActionMenu(): void;
    /**
     * Exposes the computed fixed-position style for the active row-action menu.
     */
    rowActionMenuStyle(): Record<string, string> | null;
    /**
     * Maps a few common semantic action names to icons and otherwise returns the provided icon verbatim.
     */
    rowActionIcon(action: ChillTableRowAction): string;
    /**
     * Applies Material Symbols automatically for common semantic row actions.
     */
    rowActionIconClass(action: ChillTableRowAction): string;
    /**
     * Derives a readable row-action label when the host does not provide one.
     */
    rowActionLabel(action: ChillTableRowAction): string;
    toggleColumnSort(column: TableColumn): void;
    sortDirectionFor(column: TableColumn): 'ASC' | 'DESC' | null;
    isColumnEditing(column: TableColumn): boolean;
    isColumnReadOnly(column: TableColumn): boolean;
    canSortColumn(column: TableColumn): boolean;
    isPropertyTypeOptionDisabled(property: ChillPropertySchema, propertyType: number): boolean;
    updatePropertyType(property: ChillPropertySchema, value: number | string): void;
    private readActiveOrdering;
    private isColumnEditorControl;
    /**
     * Forwards row selection changes to the hosting selection controller.
     */
    toggleRowSelection(entity: ChillEntity, selected: boolean): void;
    /**
     * Reads the current selection state from the hosting selection controller.
     */
    isRowSelected(entity: ChillEntity): boolean;
    /**
     * Delegates row-selection disabled state to the host when provided.
     */
    isRowSelectionDisabled(entity: ChillEntity): boolean;
    /**
     * Evaluates whether a row action should be disabled for the current entity.
     */
    isRowActionDisabled(action: ChillTableRowAction, entity: ChillEntity): boolean;
    handleDocumentClick(): void;
    handleWindowResize(): void;
    handleWindowScroll(): void;
    /**
     * Treats non-pristine CRUD states as pending so the row can render transient styling.
     */
    isPendingRow(entity: ChillEntity): boolean;
    /**
     * Uses the normalized CRUD status to identify deleted rows.
     */
    isDeletedRow(entity: ChillEntity): boolean;
    isRefreshFlashRow(entity: ChillEntity): boolean;
    /**
     * Creates a single-property edit session for the chosen cell using a fresh schema-driven form.
     */
    activateCellEdit(entity: ChillEntity, column: TableColumn): void;
    /**
     * Matches the requested cell against the current inline edit session.
     */
    isCellEditing(entity: ChillEntity, column: TableColumn): boolean;
    /**
     * Clears the committing flag when the active editor changes its tracked property value.
     */
    handleCellValueChange(value: Record<string, JsonValue>): void;
    /**
     * Keeps the active edit session aligned with the child editor validity state.
     */
    handleCellValidityChange(isValid: boolean): void;
    /**
     * Keeps inline editing alive while a lookup picker dialog owns the focus outside the table cell.
     */
    handleLookupDialogOpenChange(isOpen: boolean): void;
    /**
     * Commits the edit only when focus leaves the entire editor, not when it moves within the editor.
     */
    handleCellFocusOut(event: FocusEvent): void;
    /**
     * Supports Enter-to-commit and Escape-to-cancel without letting the event leak to the row.
     */
    handleCellEditorKeydown(event: KeyboardEvent): void;
    /**
     * Drops the current inline edit session without emitting a commit.
     */
    cancelCellEdit(): void;
    /**
     * Emits a cell commit only for valid edits whose value actually changed from the original snapshot.
     */
    commitCellEdit(): void;
    /**
     * Extracts per-field validation errors from the row chill state in a template-friendly shape.
     */
    rowFieldErrors(entity: ChillEntity): Record<string, string>;
    /**
     * Detects either field-level or generic validation errors stored in the row chill state.
     */
    rowHasValidationErrors(entity: ChillEntity): boolean;
    /**
     * Keeps live entity subscriptions aligned with the current schema type and visible entity set.
     */
    private syncEntityNotificationSubscriptions;
    /**
     * Unsubscribes from all live entity notifications and clears the associated bookkeeping.
     */
    private clearEntityNotificationSubscriptions;
    private clearRowRefreshFlashTimers;
    private pinnedColumnWidthRem;
    /**
     * Refreshes locally displayed rows only for remote update notifications that contain a Guid.
     */
    private handleEntityNotifications;
    /**
     * Reloads a row from the server, merges remote changes into non-dirty fields, and warns on conflicts.
     */
    private refreshDisplayedEntity;
    /**
     * Persists the current column layout into schema metadata and updates local state with the saved result.
     */
    private saveLayout;
    /**
     * Reads persisted column layout from schema metadata and falls back to schema order when unavailable.
     */
    private readLayoutState;
    private isMonacoEditorEventTarget;
    /**
     * Stores visible columns before hidden ones so the persisted layout can be rendered directly.
     */
    private normalizeLayoutForSave;
    private applyPropertyWidthProportions;
    private readPropertyWidthProportion;
    private normalizeColumnWidthProportion;
    private findColumnWidthOptionIndex;
    /**
     * Normalizes schema metadata from camelCase or legacy payload shapes into a mutable string map.
     */
    private readSchemaMetadata;
    /**
     * Reads a property from the entity bag first, then from direct camelCase or PascalCase fields.
     */
    private readPropertyValue;
    /**
     * Converts primitive entity properties into trimmed text for keys such as Guid or Label.
     */
    private readEntityText;
    /**
     * Returns the normalized lowercase CRUD status used by row rendering logic.
     */
    private readCrudState;
    /**
     * Type guard for JSON object records.
     */
    private isJsonObjectRecord;
    /**
     * Returns the raw chill state payload attached to the entity.
     */
    private readChillState;
    /**
     * Normalizes chill state into a predictable CRUD model with defaults for new and deleting rows.
     */
    private readCrudStateObject;
    /**
     * Merges a CRUD-state patch onto the entity while keeping derived `isNew` and `isDeleting` flags consistent.
     */
    private withCrudState;
    /**
     * Removes undefined entries before persisting CRUD state back onto the entity payload.
     */
    private sanitizeCrudState;
    /**
     * Resets a freshly loaded server entity back to a pristine local CRUD state.
     */
    private normalizeServerEntity;
    private savePropertySchema;
    private applyUpdatedSchema;
    /**
     * Skips live refreshes for a short window after a local save so the row keeps the just-returned server copy.
     */
    private shouldIgnoreEntityNotification;
    private buildRefreshFindRequest;
    private buildTablePropertyRequest;
    /**
     * Ensures a server entity exposes every schema property through the `properties` bag expected by the table.
     */
    private prepareEntityForSchema;
    /**
     * Replaces a row in the displayed collection and pushes fresh values into any active editor for that row.
     */
    private replaceDisplayedEntity;
    private flashRefreshedRow;
    /**
     * Uses normalized CRUD state to detect client-side draft rows.
     */
    private isNewEntity;
    /**
     * Collects the property names whose form controls are currently dirty.
     */
    private readDirtyControlNames;
    /**
     * Reads the row Guid using either camelCase or PascalCase server field names.
     */
    private readEntityGuid;
    /**
     * Normalizes a JSON value into trimmed text when it is already a string.
     */
    private readStringValue;
    /**
     * Reads a boolean flag from the raw chill state object.
     */
    private readChillStateBoolean;
    /**
     * Compares an entity Guid with an incoming Guid after trimming the incoming value.
     */
    private sameEntityGuid;
    /**
     * Converts a property name to PascalCase for payloads that expose both casing styles.
     */
    private toPascalCase;
    /**
     * Uses JSON serialization as a pragmatic deep-equality check for editor values and server payloads.
     */
    private areJsonValuesEqual;
    /**
     * Computes a viewport-clamped fixed position for the row-action menu.
     */
    private buildRowActionMenuState;
    static ɵfac: i0.ɵɵFactoryDeclaration<ChillTableComponent, never>;
    static ɵcmp: i0.ɵɵComponentDeclaration<ChillTableComponent, "app-chill-table", never, { "schema": { "alias": "schema"; "required": false; "isSignal": true; }; "entities": { "alias": "entities"; "required": false; "isSignal": true; }; "selectionColumn": { "alias": "selectionColumn"; "required": false; "isSignal": true; }; "rowAction": { "alias": "rowAction"; "required": false; "isSignal": true; }; "rowActions": { "alias": "rowActions"; "required": false; "isSignal": true; }; "enableInlineEditing": { "alias": "enableInlineEditing"; "required": false; "isSignal": true; }; "readonlyPropertyNames": { "alias": "readonlyPropertyNames"; "required": false; "isSignal": true; }; "validationFocus": { "alias": "validationFocus"; "required": false; "isSignal": true; }; "showSchemaHeader": { "alias": "showSchemaHeader"; "required": false; "isSignal": true; }; "ordering": { "alias": "ordering"; "required": false; "isSignal": true; }; "enableFullTextSearch": { "alias": "enableFullTextSearch"; "required": false; "isSignal": true; }; "fullTextSearch": { "alias": "fullTextSearch"; "required": false; "isSignal": true; }; "showMobileTaskClose": { "alias": "showMobileTaskClose"; "required": false; "isSignal": true; }; }, { "cellEditCommit": "cellEditCommit"; "sortChange": "sortChange"; "fullTextSearchChange": "fullTextSearchChange"; "schemaUpdated": "schemaUpdated"; "mobileTaskClose": "mobileTaskClose"; }, never, never, true, never>;
}
export {};
//# sourceMappingURL=chill-table.component.d.ts.map