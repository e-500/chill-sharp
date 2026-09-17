import { ChillService } from '../services/chill.service';
import { WorkspaceLayoutService } from '../services/workspace-layout.service';
import * as i0 from "@angular/core";
export declare class ChillI18nLabelComponent {
    private static readonly MIN_INPUT_WIDTH_CH;
    readonly chill: ChillService;
    readonly layout: WorkspaceLayoutService;
    readonly labelGuid: import("@angular/core").InputSignal<string>;
    readonly primaryDefaultText: import("@angular/core").InputSignal<string>;
    readonly secondaryDefaultText: import("@angular/core").InputSignal<string>;
    readonly editable: import("@angular/core").InputSignal<boolean>;
    readonly isEditing: import("@angular/core").WritableSignal<boolean>;
    readonly isSaving: import("@angular/core").WritableSignal<boolean>;
    readonly draftText: import("@angular/core").WritableSignal<string>;
    readonly inputWidth: import("@angular/core").WritableSignal<string>;
    readonly errorMessage: import("@angular/core").WritableSignal<string>;
    readonly text: import("@angular/core").Signal<string>;
    readonly editEnabled: import("@angular/core").Signal<boolean>;
    readonly canSave: import("@angular/core").Signal<boolean>;
    readonly editAriaLabel: import("@angular/core").Signal<string>;
    readonly saveAriaLabel: import("@angular/core").Signal<string>;
    readonly cancelAriaLabel: import("@angular/core").Signal<string>;
    startEditing(): void;
    cancel(): void;
    save(): void;
    private buildInputWidth;
    static ɵfac: i0.ɵɵFactoryDeclaration<ChillI18nLabelComponent, never>;
    static ɵcmp: i0.ɵɵComponentDeclaration<ChillI18nLabelComponent, "app-chill-i18n-label", never, { "labelGuid": { "alias": "labelGuid"; "required": true; "isSignal": true; }; "primaryDefaultText": { "alias": "primaryDefaultText"; "required": true; "isSignal": true; }; "secondaryDefaultText": { "alias": "secondaryDefaultText"; "required": true; "isSignal": true; }; "editable": { "alias": "editable"; "required": false; "isSignal": true; }; }, {}, never, never, true, never>;
}
//# sourceMappingURL=chill-i18n-label.component.d.ts.map