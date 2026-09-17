import { OnInit } from '@angular/core';
import { ChillService } from '../services/chill.service';
import * as i0 from "@angular/core";
export declare class LoginPageComponent implements OnInit {
    readonly chill: ChillService;
    private readonly formBuilder;
    private readonly router;
    readonly isSubmitting: import("@angular/core").WritableSignal<boolean>;
    readonly errorMessage: import("@angular/core").WritableSignal<string>;
    readonly serviceStatusMessage: import("@angular/core").WritableSignal<string>;
    readonly serviceStatusKind: import("@angular/core").WritableSignal<"error" | "info" | "success">;
    readonly form: import("@angular/forms").FormGroup<{
        userNameOrEmail: import("@angular/forms").FormControl<string>;
        password: import("@angular/forms").FormControl<string>;
    }>;
    ngOnInit(): void;
    submit(): void;
    static ɵfac: i0.ɵɵFactoryDeclaration<LoginPageComponent, never>;
    static ɵcmp: i0.ɵɵComponentDeclaration<LoginPageComponent, "app-login-page", never, {}, {}, never, never, true, never>;
}
//# sourceMappingURL=login-page.component.d.ts.map