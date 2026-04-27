import { OnInit } from '@angular/core';
import type { JsonValue } from 'chill-sharp-ng-client';
import { type ChillTableCellEditCommitEvent, type ChillTableRowAction, type ChillTableSelectionColumn, type ChillTableSortChangeEvent, type ChillTableValidationFocus } from '../../lib/chill-table.component';
import { type ChillEntity, type ChillFormSubmitEvent, type ChillQuery, type ChillSchema, type ChillSchemaListItem } from '../../models/chill-schema.models';
import { ChillService } from '../../services/chill.service';
import { WorkspaceDialogService } from '../../services/workspace-dialog.service';
import { WorkspaceService } from '../../services/workspace.service';
import * as i0 from "@angular/core";
export interface I18nText {
    labelGuid: string;
    primaryDefaultText: string;
    secondaryDefaultText: string;
}
export declare class CrudPageComponentConfiguration {
    chillType: string;
    chillQuery?: string | null;
    viewCode?: string | null;
    disableAdd: boolean;
    disableCreate: boolean;
    disableEdit: boolean;
    disableInlineEdit: boolean;
    disableDelete: boolean;
    relationLabel?: string | I18nText | null;
    defaultValues?: Record<string, JsonValue> | null;
    fixedValues?: Record<string, JsonValue> | null;
    fixedQueryValues?: Record<string, JsonValue> | null;
    defaultQueryValues?: Record<string, JsonValue> | null;
    relations?: CrudPageComponentConfiguration[] | null;
}
export declare class CrudPageComponent implements OnInit {
    readonly chill: ChillService;
    readonly dialog: WorkspaceDialogService;
    readonly workspace: WorkspaceService;
    readonly selectionEnabled: import("@angular/core").InputSignal<boolean>;
    readonly multipleSelection: import("@angular/core").InputSignal<boolean>;
    readonly initialSelectedEntity: import("@angular/core").InputSignal<ChillEntity | null>;
    readonly initialSelectedEntities: import("@angular/core").InputSignal<ChillEntity[]>;
    readonly showTableHeader: import("@angular/core").InputSignal<boolean>;
    readonly showMobileTaskClose: import("@angular/core").InputSignal<boolean>;
    readonly componentConfiguration: import("@angular/core").InputSignal<CrudPageComponentConfiguration | null>;
    readonly isLoadingSchemaList: import("@angular/core").WritableSignal<boolean>;
    readonly isLoadingSchema: import("@angular/core").WritableSignal<boolean>;
    readonly isSearching: import("@angular/core").WritableSignal<boolean>;
    readonly isSaving: import("@angular/core").WritableSignal<boolean>;
    readonly errorMessage: import("@angular/core").WritableSignal<string>;
    readonly querySchemas: import("@angular/core").WritableSignal<ChillSchemaListItem[]>;
    readonly selectedQueryType: import("@angular/core").WritableSignal<string>;
    readonly querySchema: import("@angular/core").WritableSignal<ChillSchema | null>;
    readonly resultSchema: import("@angular/core").WritableSignal<ChillSchema | null>;
    readonly queryModel: import("@angular/core").WritableSignal<ChillQuery | null>;
    readonly results: import("@angular/core").WritableSignal<ChillEntity[]>;
    readonly selectedEntityKeys: import("@angular/core").WritableSignal<string[]>;
    readonly selectedViewCode: import("@angular/core").WritableSignal<string>;
    readonly serverWindowStartPage: import("@angular/core").WritableSignal<number>;
    readonly hasMoreServerPages: import("@angular/core").WritableSignal<boolean>;
    readonly normalizedConfiguration: import("@angular/core").Signal<CrudPageComponentConfiguration>;
    readonly readonlyQueryPropertyNames: import("@angular/core").Signal<string[]>;
    readonly readonlyEntityPropertyNames: import("@angular/core").Signal<string[]>;
    readonly currentPage: import("@angular/core").WritableSignal<number>;
    readonly pageSize = 20;
    readonly validationErrorMessage: import("@angular/core").Signal<string>;
    readonly validationFocus: import("@angular/core").Signal<ChillTableValidationFocus | null>;
    readonly currentFullTextSearch: import("@angular/core").Signal<string>;
    readonly pagedResults: import("@angular/core").Signal<ChillEntity[]>;
    readonly rowActions: import("@angular/core").Signal<ChillTableRowAction[]>;
    readonly activeRowActions: import("@angular/core").Signal<ChillTableRowAction[] | null>;
    readonly selectionColumn: import("@angular/core").Signal<ChillTableSelectionColumn | null>;
    /**
     * Initializes the component by setting up initial state and loading query schemas.
     */
    ngOnInit(): void;
    /**
     * Determines if the selection can be confirmed based on the selection mode and selected entities.
     */
    canConfirmSelection(): boolean;
    /**
     * Returns the dialog result based on the selection mode.
     */
    dialogResult(): ChillEntity | ChillEntity[] | null;
    /**
     * Selects a query schema and loads the corresponding result schema.
     */
    selectQuerySchema(chillType: string): void;
    /**
     * Performs a search using the provided query form event.
     */
    search(event: ChillFormSubmitEvent): void;
    applyOrdering(event: ChillTableSortChangeEvent): void;
    applyFullTextSearch(value: string): void;
    handleResultSchemaUpdated(schema: ChillSchema): void;
    closeActiveTask(): void;
    /**
     * Opens a search dialog for the current query schema.
     */
    openSearchDialog(): void;
    /**
     * Checks if the search dialog can be opened.
     */
    canOpenSearchDialog(): boolean;
    /**
     * Checks if a new entity can be added.
     */
    canAddEntity(): boolean;
    isAddDisabled(): boolean;
    isEditDisabled(): boolean;
    isInlineEditDisabled(): boolean;
    isDeleteDisabled(): boolean;
    isAttachmentCrud(): boolean;
    canOpenAttachmentUploadDialog(): boolean;
    /**
     * Checks if there are any pending entities that need to be saved.
     */
    hasPendingEntities(): boolean;
    /**
     * Saves all pending entities by validating and committing them.
     */
    savePendingEntities(): Promise<void>;
    /**
     * Checks if navigation to the previous page is possible.
     */
    canGoToPreviousPage(): boolean;
    /**
     * Checks if navigation to the next page is possible.
     */
    canGoToNextPage(): boolean;
    /**
     * Navigates to the previous page.
     */
    goToPreviousPage(): void;
    /**
     * Navigates to the next page.
     */
    goToNextPage(): void;
    /**
     * Returns the label for the current page.
     */
    pageLabel(): string;
    /**
     * Clears the error message.
     */
    clearErrorMessage(): void;
    /**
     * Opens a dialog for editing or adding an entity.
     */
    openEntityDialog(entity: ChillEntity): void;
    private loadQuerySchemas;
    private loadSelectedSchema;
    /**
     * Adds a new draft entity to the results.
     */
    add(): void;
    /**
     * Handles inline cell edit commits from the table.
     */
    handleInlineCellEdit(event: ChillTableCellEditCommitEvent): Promise<void>;
    private loadResultSchema;
    markEntityDeleted(entity: ChillEntity): void;
    private createQueryModel;
    private normalizeQuery;
    private createBackupQuerySchema;
    private normalizeCreateEntity;
    private stripSchemaPropertiesFromRoot;
    private buildChunkOperations;
    private refreshResults;
    private clearResultWindow;
    private executeQuery;
    private buildPaginationForPage;
    private calculateServerWindowStartPage;
    private serverWindowEntityCount;
    private hasLoadedPage;
    private removeIsNewEntity;
    private isDraftEntity;
    private pendingEntities;
    private mergeWithDraftEntities;
    private updatePendingStatuses;
    private isPendingEntity;
    private isDeletedEntity;
    private readEntityKey;
    openAttachmentUploadDialog(): Promise<void>;
    private readEntityChillType;
    private withCrudState;
    private cloneEntity;
    private readCrudStatus;
    private mergeEntityProperty;
    private autocompleteAndValidateEntity;
    private validatePendingEntities;
    private replaceEntity;
    private findEntityByKey;
    private isNewEntity;
    private prepareEntityForSchema;
    private readEntityPropertyValue;
    private sanitizeCrudState;
    private normalizeServerEntity;
    private prepareDialogEntity;
    private prepareSavedDialogEntity;
    private readChillStateObject;
    private normalizeDirtyProperties;
    private extractValidationEntityFields;
    private partitionValidationErrors;
    private extractEntities;
    private toEntityArray;
    private isJsonObject;
    private readStringValue;
    private configuredResultChillType;
    private configuredQueryChillType;
    private defaultEntityValues;
    private fixedEntityValues;
    private defaultQueryValues;
    private fixedQueryValues;
    private relations;
    private normalizeComponentConfiguration;
    private readConfigString;
    private readConfigBoolean;
    private readConfigRecord;
    private readRelationLabel;
    private readRelationConfigurations;
    private createRelationRowActions;
    private createAttachmentRowActions;
    private createAttachmentDownloadRowActions;
    private openRelation;
    private canOpenAttachmentCrud;
    private openAttachmentCrud;
    private resolveRelationConfiguration;
    private relationActionLabel;
    private resolveRelationLabel;
    private resolveConfigRecord;
    private readAttachmentTargetInfo;
    private resolveConfigValue;
    private downloadAttachment;
    private readAttachmentFileName;
    private createEntityMock;
    private isQuerySchema;
    schemaLabel(item: ChillSchemaListItem): string;
    private normalizeViewCode;
    private isEntitySelected;
    private toggleSelectedEntity;
    private selectedEntity;
    private selectedEntities;
    private readInitialSelectedEntityKeys;
    private readInitialSelectedEntities;
    static ɵfac: i0.ɵɵFactoryDeclaration<CrudPageComponent, never>;
    static ɵcmp: i0.ɵɵComponentDeclaration<CrudPageComponent, "app-crud-page", never, { "selectionEnabled": { "alias": "selectionEnabled"; "required": false; "isSignal": true; }; "multipleSelection": { "alias": "multipleSelection"; "required": false; "isSignal": true; }; "initialSelectedEntity": { "alias": "initialSelectedEntity"; "required": false; "isSignal": true; }; "initialSelectedEntities": { "alias": "initialSelectedEntities"; "required": false; "isSignal": true; }; "showTableHeader": { "alias": "showTableHeader"; "required": false; "isSignal": true; }; "showMobileTaskClose": { "alias": "showMobileTaskClose"; "required": false; "isSignal": true; }; "componentConfiguration": { "alias": "componentConfiguration"; "required": false; "isSignal": true; }; }, {}, never, never, true, never>;
}
//# sourceMappingURL=crud-page.component.d.ts.map