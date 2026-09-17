import { ChillService } from '../services/chill.service';
import * as i0 from "@angular/core";
export declare class ConfirmResetPageComponent {
    readonly chill: ChillService;
    private readonly formBuilder;
    private readonly route;
    private readonly router;
    readonly isSubmitting: import("@angular/core").WritableSignal<boolean>;
    readonly errorMessage: import("@angular/core").WritableSignal<string>;
    readonly successMessage: import("@angular/core").WritableSignal<string>;
    readonly form: import("@angular/forms").FormGroup<{
        userId: import("@angular/forms").FormControl<string>;
        resetToken: import("@angular/forms").FormControl<string>;
        newPassword: import("@angular/forms").FormControl<string>;
        confirmPassword: import("@angular/forms").FormControl<string>;
    }>;
    submit(): void;
    static ɵfac: i0.ɵɵFactoryDeclaration<ConfirmResetPageComponent, never>;
    static ɵcmp: i0.ɵɵComponentDeclaration<ConfirmResetPageComponent, "app-confirm-reset-page", never, {}, {}, never, never, true, never>;
}
//# sourceMappingURL=confirm-reset-page.component.d.ts.map