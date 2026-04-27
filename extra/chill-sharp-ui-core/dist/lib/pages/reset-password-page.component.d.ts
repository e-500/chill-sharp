import type { PasswordResetTokenResponse } from '../models/chill-auth.models';
import { ChillService } from '../services/chill.service';
import * as i0 from "@angular/core";
export declare class ResetPasswordPageComponent {
    readonly chill: ChillService;
    private readonly formBuilder;
    readonly isSubmitting: import("@angular/core").WritableSignal<boolean>;
    readonly errorMessage: import("@angular/core").WritableSignal<string>;
    readonly successMessage: import("@angular/core").WritableSignal<string>;
    readonly response: import("@angular/core").WritableSignal<PasswordResetTokenResponse | null>;
    readonly form: import("@angular/forms").FormGroup<{
        userNameOrEmail: import("@angular/forms").FormControl<string>;
    }>;
    submit(): void;
    static ɵfac: i0.ɵɵFactoryDeclaration<ResetPasswordPageComponent, never>;
    static ɵcmp: i0.ɵɵComponentDeclaration<ResetPasswordPageComponent, "app-reset-password-page", never, {}, {}, never, never, true, never>;
}
//# sourceMappingURL=reset-password-page.component.d.ts.map