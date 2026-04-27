import * as i0 from "@angular/core";
export declare class ChillTextEditorDialogComponent {
    private readonly dialog;
    readonly value: import("@angular/core").InputSignal<string>;
    readonly language: import("@angular/core").InputSignal<"json" | "plaintext">;
    readonly placeholder: import("@angular/core").InputSignal<string>;
    readonly disabled: import("@angular/core").InputSignal<boolean>;
    readonly draft: import("@angular/core").WritableSignal<string>;
    constructor();
    submit(): void;
    static ɵfac: i0.ɵɵFactoryDeclaration<ChillTextEditorDialogComponent, never>;
    static ɵcmp: i0.ɵɵComponentDeclaration<ChillTextEditorDialogComponent, "app-chill-text-editor-dialog", never, { "value": { "alias": "value"; "required": false; "isSignal": true; }; "language": { "alias": "language"; "required": false; "isSignal": true; }; "placeholder": { "alias": "placeholder"; "required": false; "isSignal": true; }; "disabled": { "alias": "disabled"; "required": false; "isSignal": true; }; }, {}, never, never, true, never>;
}
//# sourceMappingURL=chill-text-editor-dialog.component.d.ts.map