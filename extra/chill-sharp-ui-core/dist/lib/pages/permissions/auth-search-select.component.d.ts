import * as i0 from "@angular/core";
export interface AuthSearchSelectOption {
    id: string;
    label: string;
    description?: string;
    keywords?: string;
}
export declare class AuthSearchSelectComponent {
    readonly options: import("@angular/core").InputSignal<AuthSearchSelectOption[]>;
    readonly selectedId: import("@angular/core").InputSignal<string>;
    readonly placeholder: import("@angular/core").InputSignal<string>;
    readonly emptyMessage: import("@angular/core").InputSignal<string>;
    readonly noResultsMessage: import("@angular/core").InputSignal<string>;
    readonly clearAriaLabel: import("@angular/core").InputSignal<string>;
    readonly selectionChange: import("@angular/core").OutputEmitterRef<string>;
    readonly searchTerm: import("@angular/core").WritableSignal<string>;
    readonly isOpen: import("@angular/core").WritableSignal<boolean>;
    readonly selectedOption: import("@angular/core").Signal<AuthSearchSelectOption | null>;
    readonly filteredOptions: import("@angular/core").Signal<AuthSearchSelectOption[]>;
    updateSearchTerm(value: string): void;
    openResults(): void;
    closeResultsSoon(): void;
    selectOption(id: string): void;
    clearSelection(): void;
    static ɵfac: i0.ɵɵFactoryDeclaration<AuthSearchSelectComponent, never>;
    static ɵcmp: i0.ɵɵComponentDeclaration<AuthSearchSelectComponent, "app-auth-search-select", never, { "options": { "alias": "options"; "required": false; "isSignal": true; }; "selectedId": { "alias": "selectedId"; "required": false; "isSignal": true; }; "placeholder": { "alias": "placeholder"; "required": false; "isSignal": true; }; "emptyMessage": { "alias": "emptyMessage"; "required": false; "isSignal": true; }; "noResultsMessage": { "alias": "noResultsMessage"; "required": false; "isSignal": true; }; "clearAriaLabel": { "alias": "clearAriaLabel"; "required": false; "isSignal": true; }; }, { "selectionChange": "selectionChange"; }, never, never, true, never>;
}
//# sourceMappingURL=auth-search-select.component.d.ts.map