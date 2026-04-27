import { ElementRef, OnDestroy, OnInit } from '@angular/core';
import type { JsonValue } from 'chill-sharp-ng-client';
import type { ChillEntity, ChillPropertySchema, ChillSchema } from '../models/chill-schema.models';
import { ChillService } from '../services/chill.service';
import * as i0 from "@angular/core";
export declare class ChillPolymorphicOutputComponent implements OnInit, OnDestroy {
    readonly chill: ChillService;
    readonly host: ElementRef<HTMLElement>;
    readonly source: import("@angular/core").InputSignal<ChillEntity | null>;
    readonly schema: import("@angular/core").InputSignal<ChillSchema | null>;
    readonly propertyName: import("@angular/core").InputSignal<string>;
    readonly hostWidth: import("@angular/core").WritableSignal<number | null>;
    private resizeObserver;
    readonly property: import("@angular/core").Signal<ChillPropertySchema | null>;
    readonly value: import("@angular/core").Signal<JsonValue | undefined>;
    readonly spacedDisplayParts: import("@angular/core").Signal<string[] | null>;
    readonly displayText: import("@angular/core").Signal<string>;
    readonly titleText: import("@angular/core").Signal<string>;
    /**
     * Tracks the rendered width so object labels can switch to their short form in tight cells.
     */
    ngOnInit(): void;
    /**
     * Disconnects the resize observer when the component is destroyed.
     */
    ngOnDestroy(): void;
    /**
     * Reads a property from the entity bag first, then falls back to top-level camelCase/PascalCase fields.
     */
    private readPropertyValue;
    /**
     * Formats scalars, dates, arrays, and entity-like objects using the schema type and display context.
     */
    private formatValue;
    /**
     * Formats valid date strings with the local date formatter and otherwise preserves the raw value.
     */
    private formatDate;
    /**
     * Formats valid date-time strings with the local formatter and otherwise preserves the raw value.
     */
    private formatDateTime;
    private buildSpacedDisplayParts;
    private formatTime;
    private formatNumber;
    /**
     * Chooses the most useful label from an object payload and optionally prefers `ShortLabel` in narrow cells.
     */
    private formatObjectValue;
    /**
     * Converts a property name to PascalCase to match server payloads that expose both casing styles.
     */
    private toPascalCase;
    /**
     * Checks whether a JSON value is a non-array object.
     */
    private isJsonObject;
    /**
     * Returns the first non-empty string, number, or boolean found among the candidate keys.
     */
    private readObjectText;
    /**
     * Treats cells narrower than 140px as compact enough to prefer short labels.
     */
    private shouldPreferShortLabel;
    private shouldRenderAsSpacedParts;
    static ɵfac: i0.ɵɵFactoryDeclaration<ChillPolymorphicOutputComponent, never>;
    static ɵcmp: i0.ɵɵComponentDeclaration<ChillPolymorphicOutputComponent, "app-chill-polymorphic-output", never, { "source": { "alias": "source"; "required": false; "isSignal": true; }; "schema": { "alias": "schema"; "required": false; "isSignal": true; }; "propertyName": { "alias": "propertyName"; "required": true; "isSignal": true; }; }, {}, never, never, true, never>;
}
//# sourceMappingURL=chill-polymorphic-output.component.d.ts.map