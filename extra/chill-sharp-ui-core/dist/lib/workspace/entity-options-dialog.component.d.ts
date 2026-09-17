import { FormControl, FormGroup } from '@angular/forms';
import type { JsonValue } from '@chill-sharp/ng-client';
import { type ChillEntityOptions, type ChillSchema } from '../models/chill-schema.models';
import { ChillService } from '../services/chill.service';
import * as i0 from "@angular/core";
type EntityOptionsFormGroup = FormGroup<Record<string, FormControl<JsonValue>>>;
export declare class EntityOptionsDialogComponent {
    readonly chill: ChillService;
    private readonly dialog;
    readonly chillType: import("@angular/core").InputSignal<string>;
    readonly displayName: import("@angular/core").InputSignal<string>;
    readonly isLoading: import("@angular/core").WritableSignal<boolean>;
    readonly isValid: import("@angular/core").WritableSignal<boolean>;
    readonly loadError: import("@angular/core").WritableSignal<string>;
    readonly entityOptions: import("@angular/core").WritableSignal<ChillEntityOptions | null>;
    readonly schema: import("@angular/core").Signal<ChillSchema>;
    readonly form: EntityOptionsFormGroup;
    private readonly properties;
    constructor();
    canDialogSubmit(): boolean;
    submit(): Promise<void>;
    private loadEntityOptions;
    private readOptionalString;
    private readBoolean;
    static ɵfac: i0.ɵɵFactoryDeclaration<EntityOptionsDialogComponent, never>;
    static ɵcmp: i0.ɵɵComponentDeclaration<EntityOptionsDialogComponent, "app-entity-options-dialog", never, { "chillType": { "alias": "chillType"; "required": false; "isSignal": true; }; "displayName": { "alias": "displayName"; "required": false; "isSignal": true; }; }, {}, never, never, true, never>;
}
export {};
//# sourceMappingURL=entity-options-dialog.component.d.ts.map