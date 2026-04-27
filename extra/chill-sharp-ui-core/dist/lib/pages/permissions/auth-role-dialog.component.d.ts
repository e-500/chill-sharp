import { FormControl, FormGroup } from '@angular/forms';
import type { JsonValue } from 'chill-sharp-ng-client';
import { type ChillSchema } from '../../models/chill-schema.models';
import { ChillService } from '../../services/chill.service';
import * as i0 from "@angular/core";
type AuthRoleFormGroup = FormGroup<Record<string, FormControl<JsonValue>>>;
export declare class AuthRoleDialogComponent {
    readonly chill: ChillService;
    private readonly dialog;
    readonly roleGuid: import("@angular/core").InputSignal<string>;
    readonly isLoading: import("@angular/core").WritableSignal<boolean>;
    readonly isValid: import("@angular/core").WritableSignal<boolean>;
    readonly loadError: import("@angular/core").WritableSignal<string>;
    readonly isEditMode: import("@angular/core").Signal<boolean>;
    readonly schema: import("@angular/core").Signal<ChillSchema>;
    readonly form: AuthRoleFormGroup;
    private readonly properties;
    constructor();
    canDialogSubmit(): boolean;
    submit(): Promise<void>;
    private loadRole;
    private populateForm;
    private readPayload;
    private readString;
    static ɵfac: i0.ɵɵFactoryDeclaration<AuthRoleDialogComponent, never>;
    static ɵcmp: i0.ɵɵComponentDeclaration<AuthRoleDialogComponent, "app-auth-role-dialog", never, { "roleGuid": { "alias": "roleGuid"; "required": false; "isSignal": true; }; }, {}, never, never, true, never>;
}
export {};
//# sourceMappingURL=auth-role-dialog.component.d.ts.map