import { FormControl, FormGroup } from '@angular/forms';
import type { AuthUserDetailsResponse, JsonValue } from '@chill-sharp/ng-client';
import { type ChillSchema } from '../models/chill-schema.models';
import { ChillService } from '../services/chill.service';
import * as i0 from "@angular/core";
type UserProfileFormGroup = FormGroup<Record<string, FormControl<JsonValue>>>;
export declare class UserProfileDialogComponent {
    readonly chill: ChillService;
    private readonly dialog;
    readonly userGuid: import("@angular/core").InputSignal<string>;
    readonly isLoading: import("@angular/core").WritableSignal<boolean>;
    readonly isValid: import("@angular/core").WritableSignal<boolean>;
    readonly loadError: import("@angular/core").WritableSignal<string>;
    readonly user: import("@angular/core").WritableSignal<AuthUserDetailsResponse | null>;
    readonly schema: import("@angular/core").Signal<ChillSchema>;
    readonly form: UserProfileFormGroup;
    private readonly cultureNameOptions;
    private readonly dateFormatOptions;
    private readonly timeZoneOptions;
    private readonly properties;
    constructor();
    canDialogSubmit(): boolean;
    submit(): Promise<void>;
    private loadUser;
    private readString;
    static ɵfac: i0.ɵɵFactoryDeclaration<UserProfileDialogComponent, never>;
    static ɵcmp: i0.ɵɵComponentDeclaration<UserProfileDialogComponent, "app-user-profile-dialog", never, { "userGuid": { "alias": "userGuid"; "required": false; "isSignal": true; }; }, {}, never, never, true, never>;
}
export {};
//# sourceMappingURL=user-profile-dialog.component.d.ts.map