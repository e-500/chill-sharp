import { AfterViewInit, OnChanges, OnDestroy, SimpleChanges } from '@angular/core';
import * as i0 from "@angular/core";
export declare class ChillJsonInputComponent implements AfterViewInit, OnChanges, OnDestroy {
    readonly value: import("@angular/core").InputSignal<string>;
    readonly placeholder: import("@angular/core").InputSignal<string>;
    readonly invalid: import("@angular/core").InputSignal<boolean>;
    readonly disabled: import("@angular/core").InputSignal<boolean>;
    readonly language: import("@angular/core").InputSignal<"json" | "plaintext">;
    readonly minHeight: import("@angular/core").InputSignal<string>;
    readonly maxHeight: import("@angular/core").InputSignal<string>;
    readonly mobileFullHeight: import("@angular/core").InputSignal<boolean>;
    readonly valueChange: import("@angular/core").OutputEmitterRef<string>;
    readonly blur: import("@angular/core").OutputEmitterRef<void>;
    private editorHost?;
    private readonly zone;
    private monaco;
    private editor;
    private model;
    private resizeObserver;
    private themeObserver;
    private suppressValueEmit;
    ngAfterViewInit(): Promise<void>;
    ngOnChanges(changes: SimpleChanges): void;
    ngOnDestroy(): void;
    private applyTheme;
    protected editorStyle(): Record<string, string>;
    private defaultAriaLabel;
    static ɵfac: i0.ɵɵFactoryDeclaration<ChillJsonInputComponent, never>;
    static ɵcmp: i0.ɵɵComponentDeclaration<ChillJsonInputComponent, "app-chill-json-input", never, { "value": { "alias": "value"; "required": false; "isSignal": true; }; "placeholder": { "alias": "placeholder"; "required": false; "isSignal": true; }; "invalid": { "alias": "invalid"; "required": false; "isSignal": true; }; "disabled": { "alias": "disabled"; "required": false; "isSignal": true; }; "language": { "alias": "language"; "required": false; "isSignal": true; }; "minHeight": { "alias": "minHeight"; "required": false; "isSignal": true; }; "maxHeight": { "alias": "maxHeight"; "required": false; "isSignal": true; }; "mobileFullHeight": { "alias": "mobileFullHeight"; "required": false; "isSignal": true; }; }, { "valueChange": "valueChange"; "blur": "blur"; }, never, never, true, never>;
}
//# sourceMappingURL=chill-json-input.component.d.ts.map