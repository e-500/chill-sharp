import { OnDestroy, OnInit } from '@angular/core';
import type { AuthRole, AuthUser } from '../../models/chill-auth.models';
import type { WorkspaceTaskComponentInterface } from '../../models/workspace-task.models';
import { ChillService } from '../../services/chill.service';
import { WorkspaceToolbarService } from '../../services/workspace-toolbar.service';
import * as i0 from "@angular/core";
type PermissionSection = 'users' | 'roles';
export declare class PermissionsPageComponent implements WorkspaceTaskComponentInterface, OnInit, OnDestroy {
    static getComponentConfigurationJsonExample(): Record<string, never>;
    readonly chill: ChillService;
    readonly toolbar: WorkspaceToolbarService;
    readonly visible: import("@angular/core").InputSignal<boolean>;
    readonly toolbarScope: import("@angular/core").InputSignal<string>;
    readonly isLoading: import("@angular/core").WritableSignal<boolean>;
    readonly errorMessage: import("@angular/core").WritableSignal<string>;
    readonly activeSection: import("@angular/core").WritableSignal<PermissionSection>;
    readonly users: import("@angular/core").WritableSignal<AuthUser[]>;
    readonly roles: import("@angular/core").WritableSignal<AuthRole[]>;
    readonly canManagePermissions: import("@angular/core").WritableSignal<boolean>;
    readonly currentUser: import("@angular/core").Signal<AuthUser | null>;
    constructor();
    ngOnInit(): void;
    ngOnDestroy(): void;
    setActiveSection(section: PermissionSection): void;
    handleUserCreated(user: AuthUser): void;
    handleUserUpdated(user: AuthUser): void;
    handleRoleCreated(role: AuthRole): void;
    handleRoleUpdated(role: AuthRole): void;
    private loadPage;
    private resolveCurrentUser;
    private userLabel;
    private upsertUser;
    private upsertRole;
    static ɵfac: i0.ɵɵFactoryDeclaration<PermissionsPageComponent, never>;
    static ɵcmp: i0.ɵɵComponentDeclaration<PermissionsPageComponent, "app-permissions-page", never, { "visible": { "alias": "visible"; "required": false; "isSignal": true; }; "toolbarScope": { "alias": "toolbarScope"; "required": false; "isSignal": true; }; }, {}, never, never, true, never>;
}
export {};
//# sourceMappingURL=permissions-page.component.d.ts.map