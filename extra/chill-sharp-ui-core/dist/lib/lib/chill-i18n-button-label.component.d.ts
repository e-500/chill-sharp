import { ChillService } from '../services/chill.service';
import { WorkspaceLayoutService } from '../services/workspace-layout.service';
import * as i0 from "@angular/core";
export declare class ChillI18nButtonLabelComponent {
    readonly chill: ChillService;
    readonly layout: WorkspaceLayoutService;
    readonly labelGuid: import("@angular/core").InputSignal<string>;
    readonly primaryDefaultText: import("@angular/core").InputSignal<string>;
    readonly secondaryDefaultText: import("@angular/core").InputSignal<string>;
    readonly editable: import("@angular/core").InputSignal<boolean>;
    readonly isEditing: import("@angular/core").WritableSignal<boolean>;
    readonly isSaving: import("@angular/core").WritableSignal<boolean>;
    readonly draftText: import("@angular/core").WritableSignal<string>;
    readonly errorMessage: import("@angular/core").WritableSignal<string>;
    readonly text: import("@angular/core").Signal<string>;
    readonly editEnabled: import("@angular/core").Signal<boolean>;
    readonly canSave: import("@angular/core").Signal<boolean>;
    readonly editAriaLabel: import("@angular/core").Signal<string>;
    readonly saveAriaLabel: import("@angular/core").Signal<string>;
    startEditing(event?: Event): void;
    cancel(event?: Event): void;
    save(event?: Event): void;
    swallow(event?: Event): void;
    static ɵfac: i0.ɵɵFactoryDeclaration<ChillI18nButtonLabelComponent, never>;
    static ɵcmp: i0.ɵɵComponentDeclaration<ChillI18nButtonLabelComponent, "app-chill-i18n-button-label", never, { "labelGuid": { "alias": "labelGuid"; "required": true; "isSignal": true; }; "primaryDefaultText": { "alias": "primaryDefaultText"; "required": true; "isSignal": true; }; "secondaryDefaultText": { "alias": "secondaryDefaultText"; "required": true; "isSignal": true; }; "editable": { "alias": "editable"; "required": false; "isSignal": true; }; }, {}, never, never, true, never>;
}
//# sourceMappingURL=chill-i18n-button-label.component.d.ts.map