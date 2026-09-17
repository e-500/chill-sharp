import { FormControl, FormGroup } from '@angular/forms';
import type { JsonValue } from '@chill-sharp/ng-client';
import { type ChillSchema } from '../../models/chill-schema.models';
import { ChillService } from '../../services/chill.service';
import * as i0 from "@angular/core";
type AuthUserFormGroup = FormGroup<Record<string, FormControl<JsonValue>>>;
export declare class AuthUserDialogComponent {
    readonly chill: ChillService;
    private readonly dialog;
    readonly userGuid: import("@angular/core").InputSignal<string>;
    readonly isLoading: import("@angular/core").WritableSignal<boolean>;
    readonly isValid: import("@angular/core").WritableSignal<boolean>;
    readonly loadError: import("@angular/core").WritableSignal<string>;
    readonly isEditMode: import("@angular/core").Signal<boolean>;
    readonly schema: import("@angular/core").Signal<ChillSchema>;
    readonly form: AuthUserFormGroup;
    private readonly cultureNameOptions;
    private readonly dateFormatOptions;
    private readonly timeZoneOptions;
    private readonly properties;
    constructor();
    canDialogSubmit(): boolean;
    submit(): Promise<void>;
    private loadUser;
    private populateForm;
    private readPayload;
    private readString;
    private readBoolean;
    private readBrowserCultureName;
    private readBrowserTimeZone;
    static ɵfac: i0.ɵɵFactoryDeclaration<AuthUserDialogComponent, never>;
    static ɵcmp: i0.ɵɵComponentDeclaration<AuthUserDialogComponent, "app-auth-user-dialog", never, { "userGuid": { "alias": "userGuid"; "required": false; "isSignal": true; }; }, {}, never, never, true, never>;
}
export {};
//# sourceMappingURL=auth-user-dialog.component.d.ts.map