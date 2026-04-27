import { ChillService } from '../services/chill.service';
import * as i0 from "@angular/core";
export declare class RegisterPageComponent {
    readonly chill: ChillService;
    private readonly formBuilder;
    private readonly router;
    readonly isSubmitting: import("@angular/core").WritableSignal<boolean>;
    readonly errorMessage: import("@angular/core").WritableSignal<string>;
    readonly successMessage: import("@angular/core").WritableSignal<string>;
    readonly form: import("@angular/forms").FormGroup<{
        userName: import("@angular/forms").FormControl<string>;
        email: import("@angular/forms").FormControl<string>;
        displayName: import("@angular/forms").FormControl<string>;
        password: import("@angular/forms").FormControl<string>;
        confirmPassword: import("@angular/forms").FormControl<string>;
        createChillAuthUser: import("@angular/forms").FormControl<boolean>;
    }>;
    submit(): void;
    private readBrowserCultureName;
    private readBrowserTimeZone;
    static ɵfac: i0.ɵɵFactoryDeclaration<RegisterPageComponent, never>;
    static ɵcmp: i0.ɵɵComponentDeclaration<RegisterPageComponent, "app-register-page", never, {}, {}, never, never, true, never>;
}
//# sourceMappingURL=register-page.component.d.ts.map