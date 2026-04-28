import { OnDestroy } from '@angular/core';
import { ConnectedPosition } from '@angular/cdk/overlay';
import { FormControl, FormGroup } from '@angular/forms';
import type { JsonObject, JsonValue } from '@chill-sharp/ng-client';
import { type ChillPropertySchema, type ChillPropertySelectOptionTuple, type ChillSchema } from '../models/chill-schema.models';
import { ChillService } from '../services/chill.service';
import { WorkspaceDialogService } from '../services/workspace-dialog.service';
import * as i0 from "@angular/core";
type FieldValueMap = Record<string, JsonValue>;
type ErrorMap = Record<string, string>;
type DraftTextMap = Record<string, string>;
interface LookupState {
    term: string;
    isSearching: boolean;
    error: string;
    results: JsonObject[];
    selectedGuid: string;
    selectedLabel: string;
    selectedShortLabel: string;
}
export declare class ChillPolymorphicInputComponent implements OnDestroy {
    readonly chill: ChillService;
    readonly dialog: WorkspaceDialogService;
    readonly form: import("@angular/core").InputSignal<FormGroup<Record<string, FormControl<JsonValue>>> | null>;
    readonly schema: import("@angular/core").InputSignal<ChillSchema | null>;
    readonly propertyNames: import("@angular/core").InputSignal<string[] | null>;
    readonly readonlyPropertyNames: import("@angular/core").InputSignal<string[] | null>;
    readonly externalErrors: import("@angular/core").InputSignal<Record<string, string> | null>;
    readonly showLabels: import("@angular/core").InputSignal<boolean>;
    readonly valueChange: import("@angular/core").OutputEmitterRef<Record<string, JsonValue>>;
    readonly validityChange: import("@angular/core").OutputEmitterRef<boolean>;
    readonly fieldBlur: import("@angular/core").OutputEmitterRef<Record<string, JsonValue>>;
    readonly lookupDialogOpenChange: import("@angular/core").OutputEmitterRef<boolean>;
    readonly editorDialogOpenChange: import("@angular/core").OutputEmitterRef<boolean>;
    readonly fieldValues: import("@angular/core").WritableSignal<FieldValueMap>;
    readonly draftTextValues: import("@angular/core").WritableSignal<DraftTextMap>;
    readonly errors: import("@angular/core").WritableSignal<ErrorMap>;
    readonly lookups: import("@angular/core").WritableSignal<Record<string, LookupState>>;
    readonly lookupDialogSelectionState: import("@angular/core").WritableSignal<Record<string, boolean>>;
    readonly editorDialogSelectionState: import("@angular/core").WritableSignal<Record<string, boolean>>;
    readonly lookupOverlayPositions: ConnectedPosition[];
    private readonly lookupSearchTimers;
    private readonly lookupRequestSequence;
    private controlSubscriptions;
    private isDestroyed;
    readonly properties: import("@angular/core").Signal<ChillPropertySchema[]>;
    readonly resolvedErrors: import("@angular/core").Signal<ErrorMap>;
    readonly isValid: import("@angular/core").Signal<boolean>;
    readonly readonlyPropertyNameSet: import("@angular/core").Signal<Set<string>>;
    /**
     * Rebuilds local field, error, and lookup state from the current form/schema pair and re-emits aggregate state.
     */
    constructor();
    /**
     * Clears control subscriptions and pending lookup timers when the component is destroyed.
     */
    ngOnDestroy(): void;
    /**
     * Identifies boolean fields so the template can render a checkbox instead of a text input.
     */
    isCheckbox(property: ChillPropertySchema): boolean;
    /**
     * Uses type and metadata hints to decide when a string field should render as multiline input.
     */
    isTextarea(property: ChillPropertySchema): boolean;
    /**
     * Identifies static select fields backed by metadata option tuples.
     */
    isSelect(property: ChillPropertySchema): boolean;
    /**
     * Flags JSON-string fields so the template can render the Monaco editor.
     */
    isJsonEditor(property: ChillPropertySchema): boolean;
    editorLanguage(property: ChillPropertySchema): 'json' | 'plaintext';
    /**
     * Returns true when the caller marked the property as read only.
     */
    isPropertyReadOnly(propertyName: string): boolean;
    /**
     * Checks whether a property uses single-value lookup behavior.
     */
    isLookup(property: ChillPropertySchema): boolean;
    /**
     * Checks whether a property uses multi-value lookup behavior.
     */
    isLookupCollection(property: ChillPropertySchema): boolean;
    /**
     * Flags date-only and date-time fields so they can render localized display text instead of raw storage values.
     */
    isCultureDateInput(property: ChillPropertySchema): boolean;
    isFormattedTextInput(property: ChillPropertySchema): boolean;
    /**
     * Resolves the native input type for scalar fields.
     */
    inputType(property: ChillPropertySchema): 'text' | 'number';
    /**
     * Resolves the numeric step value from metadata or property type defaults.
     */
    inputStep(property: ChillPropertySchema): string | null;
    /**
     * Uses metadata placeholder first, otherwise mirrors the field label when labels are visually hidden.
     */
    placeholder(property: ChillPropertySchema): string;
    /**
     * Converts string and numeric field values into the text representation expected by native inputs.
     */
    textValue(propertyName: string): string;
    /**
     * Reads a field value as a boolean for checkbox binding.
     */
    booleanValue(propertyName: string): boolean;
    /**
     * Returns the current lookup search term for a property.
     */
    lookupTerm(propertyName: string): string;
    /**
     * Exposes the current lookup result list for dropdown rendering.
     */
    lookupResults(propertyName: string): JsonObject[];
    /**
     * Returns the current lookup error message for a property.
     */
    lookupError(propertyName: string): string;
    /**
     * Returns whether a lookup search is currently running for a property.
     */
    lookupIsSearching(propertyName: string): boolean;
    /**
     * Measures the visible input slot so the detached overlay keeps the same width as the field.
     */
    lookupOverlayWidth(origin: HTMLElement | null): number;
    /**
     * Checks whether the dialog-based lookup picker can be opened for a property.
     */
    canOpenLookupDialog(property: ChillPropertySchema): boolean;
    /**
     * Joins the selected labels of a lookup collection into the compact summary shown in the input.
     */
    lookupCollectionSummary(propertyName: string): string;
    /**
     * Returns the selected collection lookup entities in storage order.
     */
    selectedLookupCollectionEntities(propertyName: string): JsonObject[];
    /**
     * Returns the selected single-lookup entity when one is currently stored in the field.
     */
    selectedLookupEntity(propertyName: string): JsonObject | null;
    /**
     * Returns whether the field currently holds a selected single lookup entity.
     */
    hasSelectedLookupEntity(propertyName: string): boolean;
    /**
     * Returns the full label shown inside the selected single-value lookup pill.
     */
    selectedLookupLabel(propertyName: string): string;
    /**
     * Returns the compact lookup label used when the selected pill becomes narrow.
     */
    selectedLookupShortLabel(propertyName: string): string;
    /**
     * Extracts non-empty labels from the current lookup collection value.
     */
    collectionLookupLabels(propertyName: string): string[];
    /**
     * Returns the merged validation message coming from local validation, form errors, or external errors.
     */
    validationMessage(propertyName: string): string;
    /**
     * Trims and type-normalizes free-text input on blur, then revalidates before notifying the parent.
     */
    normalizeTextOnBlur(property: ChillPropertySchema): void;
    /**
     * Tracks raw typing for date and date-time inputs until blur normalization rewrites the value in culture format.
     */
    updateTextInput(propertyName: string, value: string): void;
    /**
     * Stores the Monaco JSON editor content as a raw string inside the form control.
     */
    updateJsonInput(propertyName: string, value: string): void;
    openEditorDialog(property: ChillPropertySchema): Promise<void>;
    beginEditorDialogSelection(propertyName: string): void;
    /**
     * Reads the current scalar value for a metadata-backed select field.
     */
    selectValue(propertyName: string): string;
    /**
     * Stores the selected option value as the field value and forwards blur semantics to the parent.
     */
    updateSelectValue(property: ChillPropertySchema, value: string): void;
    /**
     * Returns normalized `[value, text]` tuples from property metadata for native select rendering.
     */
    selectOptions(property: ChillPropertySchema): ChillPropertySelectOptionTuple[];
    /**
     * Updates the typed lookup text, clears stale selection metadata, and starts debounced search when applicable.
     */
    updateLookupTerm(property: ChillPropertySchema, value: string): void;
    /**
     * Reopens lookup suggestions on focus when the field already has searchable text but no visible results.
     */
    handleLookupFocus(property: ChillPropertySchema): void;
    /**
     * Emits blur immediately and clears the popup list after a short delay so click selection can still complete.
     */
    handleLookupBlur(propertyName: string): void;
    /**
     * Forwards blur for controls that do not need blur-time value normalization.
     */
    emitFieldBlur(propertyName: string): void;
    private endEditorDialogSelection;
    beginLookupDialogSelection(propertyName: string): void;
    /**
     * Opens the CRUD picker dialog for entity lookups and maps the confirmed selection back into the field.
     */
    openLookupDialog(property: ChillPropertySchema): Promise<void>;
    /**
     * Stores a single lookup object, updates its display term, and marks the matching selected Guid.
     */
    selectLookupResult(property: ChillPropertySchema, result: JsonObject): void;
    /**
     * Stores multiple lookup objects and rebuilds the collection summary shown in the input.
     */
    selectLookupResults(property: ChillPropertySchema, results: JsonObject[]): void;
    /**
     * Removes the current lookup value and resets the transient search state for that field.
     */
    clearLookup(property: ChillPropertySchema): void;
    private endLookupDialogSelection;
    private isAnyLookupDialogOpen;
    /**
     * Resolves the first usable lookup label from common server payload field names.
     */
    lookupLabel(result: JsonObject): string;
    /**
     * Resolves a short lookup label from common compact-name fields before falling back to the full label.
     */
    lookupShortLabel(result: JsonObject): string;
    /**
     * Extracts the lookup Guid using either `Guid` or `guid`.
     */
    lookupGuid(result: JsonObject): string;
    /**
     * Matches a rendered lookup option against the currently selected single-value lookup Guid.
     */
    isLookupResultSelected(propertyName: string, result: JsonObject): boolean;
    /**
     * Removes one selected entity from a lookup collection while preserving the remaining selection order.
     */
    removeLookupCollectionEntity(property: ChillPropertySchema, entity: JsonObject): void;
    /**
     * Returns the Angular control for a schema property when the prepared form is available.
     */
    control(propertyName: string): FormControl<JsonValue> | null;
    /**
     * Executes a lookup query and ignores late responses from older requests so only the newest search wins.
     */
    private searchLookup;
    /**
     * Reads the current form values for the rendered properties and fills missing values with editor defaults.
     */
    private readFormValues;
    /**
     * Builds the initial lookup UI state from the already-selected form values.
     */
    private createLookupState;
    /**
     * Maps undefined form values to the empty value shape expected by the rendered control.
     */
    private normalizeFieldValue;
    /**
     * Validates every rendered property and returns only the fields that currently have local errors.
     */
    private validateAllFields;
    /**
     * Revalidates one field and adds or removes its local error entry.
     */
    private validateField;
    private setLocalError;
    private clearLocalError;
    private clearDraftTextValue;
    private shouldValidateOnChange;
    private shouldCommitTextOnBlur;
    /**
     * Reads server validation stored on the Angular control so backend errors participate in the merged output.
     */
    private readControlValidationMessage;
    /**
     * Routes validation through type-specific rules after handling required and empty-value cases.
     */
    private getValidationMessage;
    /**
     * Validates Guid input against the standard GUID format.
     */
    private validateGuid;
    /**
     * Validates integer input and applies configured numeric range rules.
     */
    private validateInteger;
    /**
     * Validates decimal input and applies configured numeric range rules.
     */
    private validateDecimal;
    /**
     * Applies shared min/max metadata checks after numeric parsing has already succeeded.
     */
    private validateNumericRange;
    /**
     * Validates a date string.
     */
    private validateDate;
    /**
     * Validates a time string.
     */
    private validateTime;
    /**
     * Reuses the date-time parser so validation and blur-time normalization accept the same formats.
     */
    private validateDateTime;
    /**
     * Reuses the duration parser so validation and blur-time normalization stay aligned.
     */
    private validateDuration;
    /**
     * Validates string values against length and regex metadata rules.
     */
    private validateString;
    /**
     * Validates that the field contains a JSON document while keeping the stored form value as text.
     */
    private validateJson;
    /**
     * Converts the raw text entered by the user into the normalized typed value stored in the form.
     */
    private normalizeBlurValue;
    /**
     * Parses a user-entered date into the normalized storage format.
     */
    private parseDateDisplayValue;
    /**
     * Parses a user-entered time into the normalized storage format.
     */
    private parseTimeDisplayValue;
    /**
     * Accepts ISO-like date-time text first, then falls back to `Date` parsing for looser user input.
     */
    private parseDateTimeDisplayValue;
    /**
     * Accepts both ISO durations and `d.hh:mm[:ss]`-style values and normalizes them for storage.
     */
    private parseDurationDisplayValue;
    /**
     * Validates year, month, and day parts before composing a normalized date.
     */
    private isValidDateParts;
    /**
     * Parses culture-aware short date input using the configured Chill UI culture.
     */
    private parseCultureDateParts;
    /**
     * Resolves the entity type targeted by a lookup property from explicit schema fields or metadata fallbacks.
     */
    private resolveLookupEntityChillType;
    /**
     * Chooses the query schema used by the ellipsis picker, preferring explicit schema hints over inferred defaults.
     */
    private resolveLookupQueryChillType;
    /**
     * Derives the dialog-specific view code from the caller schema view code.
     */
    private resolveLookupDialogViewCode;
    /**
     * Formats normalized storage dates into the user culture short-date representation.
     */
    private formatDateDisplayValue;
    /**
     * Formats normalized storage date-times into the user culture date order while preserving the typed time.
     */
    private formatDateTimeDisplayValue;
    /**
     * Formats normalized storage time values as `HH:MM`, keeping seconds only when they are non-zero.
     */
    private formatTimeDisplayValue;
    /**
     * Reads numeric validation metadata such as min, max, or length constraints.
     */
    private readMetadataNumber;
    /**
     * Checks whether a property is marked as required in metadata.
     */
    private isRequired;
    /**
     * Reads string metadata defensively so structured metadata values do not break string-only callers.
     */
    private metadataString;
    /**
     * Treats nullish values, blank strings, and empty arrays as empty for required validation.
     */
    private isEmptyValue;
    /**
     * Converts numeric strings and finite numbers into a comparable numeric value.
     */
    private readNumber;
    /**
     * Excludes unsupported schema properties from rendering.
     */
    private shouldSkipProperty;
    /**
     * Searches common API wrapper properties until it finds an array of lookup objects.
     */
    private extractLookupResults;
    /**
     * Writes a lookup error and optionally keeps the last result list visible for recovery.
     */
    private setLookupError;
    /**
     * Creates the default empty lookup state object.
     */
    private createEmptyLookupState;
    /**
     * Debounces lookup requests so rapid typing collapses into a single backend query.
     */
    private scheduleLookupSearch;
    /**
     * Cancels any pending debounced lookup search for a property.
     */
    private cancelLookupSearch;
    /**
     * Compares two lookup labels in a case-insensitive, trimmed form.
     */
    private matchesLookupLabel;
    /**
     * Emits only the blurred field and its latest cached value to match the parent component contract.
     */
    private notifyFieldBlur;
    /**
     * Keeps the Angular control and the local signal cache synchronized when the component updates a field itself.
     */
    private setFieldValue;
    /**
     * Rebuilds lookup display text when the underlying form value changes outside the lookup UI handlers.
     */
    private syncLookupState;
    /**
     * Builds a comma-separated summary from a lookup collection value.
     */
    private lookupCollectionSummaryFromValue;
    /**
     * Appends a newly selected lookup entity to a collection and resets the live search slot.
     */
    private appendLookupCollectionResult;
    /**
     * Merges collection lookup selections without duplicating the same entity Guid.
     */
    private mergeLookupCollectionResults;
    /**
     * Checks whether a JSON value is a non-array object.
     */
    private isJsonObject;
    /**
     * Avoids resetting field signals when the computed field map is unchanged.
     */
    private areRecordsEqual;
    /**
     * Avoids rewriting error state when the same field messages are already stored.
     */
    private areStringRecordsEqual;
    /**
     * Prevents lookup signal churn by comparing the full lookup state map before writing it.
     */
    private areLookupStatesEqual;
    /**
     * Compares the user-visible parts of two lookup entries, including result ordering.
     */
    private areLookupStateEntriesEqual;
    private closeLookupResults;
    static ɵfac: i0.ɵɵFactoryDeclaration<ChillPolymorphicInputComponent, never>;
    static ɵcmp: i0.ɵɵComponentDeclaration<ChillPolymorphicInputComponent, "app-chill-polymorphic-input", never, { "form": { "alias": "form"; "required": false; "isSignal": true; }; "schema": { "alias": "schema"; "required": false; "isSignal": true; }; "propertyNames": { "alias": "propertyNames"; "required": false; "isSignal": true; }; "readonlyPropertyNames": { "alias": "readonlyPropertyNames"; "required": false; "isSignal": true; }; "externalErrors": { "alias": "externalErrors"; "required": false; "isSignal": true; }; "showLabels": { "alias": "showLabels"; "required": false; "isSignal": true; }; }, { "valueChange": "valueChange"; "validityChange": "validityChange"; "fieldBlur": "fieldBlur"; "lookupDialogOpenChange": "lookupDialogOpenChange"; "editorDialogOpenChange": "editorDialogOpenChange"; }, never, never, true, never>;
}
export {};
//# sourceMappingURL=chill-polymorphic-input.component.d.ts.map