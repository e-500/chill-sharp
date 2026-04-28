import * as i0 from '@angular/core';
import { Component, signal, computed, Injectable, inject, effect, input, ElementRef, Directive, output, NgZone, ViewChild, viewChild, HostListener, DestroyRef, viewChildren, ViewContainerRef, APP_INITIALIZER } from '@angular/core';
import { RouterOutlet, Router, ActivatedRoute, RouterLink } from '@angular/router';
import * as i1$1 from '@angular/common';
import { CommonModule, DOCUMENT, NgComponentOutlet } from '@angular/common';
import * as i1 from '@angular/forms';
import { FormControl, FormGroup, FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ChillSharpNgClient, ChillSharpClientError, provideChillSharpClient, CHILL_SHARP_CLIENT } from '@chill-sharp/ng-client';
import { from, switchMap, throwError, map, catchError, Observable, tap, firstValueFrom, Subscription, combineLatest } from 'rxjs';
import * as i2 from '@angular/cdk/overlay';
import { OverlayModule } from '@angular/cdk/overlay';

const DEBUG_CHILL_BASE_URL = 'http://localhost:6002/api/chill';
const CHILL_BASE_URL = normalizeChillBaseUrl(globalThis.CHILLSHARP_API_URL?.trim() || DEBUG_CHILL_BASE_URL);
const CHILL_CULTURE = 'it-IT';
const CHILL_PRIMARY_TEXT_CULTURE = 'en-US';
const CHILL_SECONDARY_TEXT_CULTURE = 'it-IT';
function normalizeChillBaseUrl(value) {
    const normalizedValue = value.replace(/\/+$/, '');
    if (normalizedValue.toLowerCase().endsWith('/chill')) {
        return normalizedValue;
    }
    return `${normalizedValue}/chill`;
}

const CHILL_UI_STORAGE_KEY_PREFIX = 'chill-sharp-ui';
const SESSION_STORAGE_KEY = `${CHILL_UI_STORAGE_KEY_PREFIX}.chill-auth-session`;
const USER_PREFERENCES_STORAGE_KEY = `${CHILL_UI_STORAGE_KEY_PREFIX}.user-preferences`;
const WORKSPACE_THEME_STORAGE_KEY = `${CHILL_UI_STORAGE_KEY_PREFIX}.workspace-theme`;
const WORKSPACE_LAYOUT_EDITING_STORAGE_KEY = `${CHILL_UI_STORAGE_KEY_PREFIX}.workspace-layout-editing`;

class ChillSharpUiRootComponent {
    static { this.ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: ChillSharpUiRootComponent, deps: [], target: i0.ɵɵFactoryTarget.Component }); }
    static { this.ɵcmp = i0.ɵɵngDeclareComponent({ minVersion: "14.0.0", version: "19.2.21", type: ChillSharpUiRootComponent, isStandalone: true, selector: "chill-sharp-ui-root", ngImport: i0, template: '<router-outlet />', isInline: true, dependencies: [{ kind: "directive", type: RouterOutlet, selector: "router-outlet", inputs: ["name", "routerOutletData"], outputs: ["activate", "deactivate", "attach", "detach"], exportAs: ["outlet"] }] }); }
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: ChillSharpUiRootComponent, decorators: [{
            type: Component,
            args: [{
                    selector: 'chill-sharp-ui-root',
                    standalone: true,
                    imports: [RouterOutlet],
                    template: '<router-outlet />'
                }]
        }] });

var PermissionEffect;
(function (PermissionEffect) {
    PermissionEffect[PermissionEffect["Allow"] = 1] = "Allow";
    PermissionEffect[PermissionEffect["Deny"] = 2] = "Deny";
})(PermissionEffect || (PermissionEffect = {}));
var PermissionAction;
(function (PermissionAction) {
    PermissionAction[PermissionAction["FullControl"] = 0] = "FullControl";
    PermissionAction[PermissionAction["Query"] = 1] = "Query";
    PermissionAction[PermissionAction["Create"] = 2] = "Create";
    PermissionAction[PermissionAction["Update"] = 3] = "Update";
    PermissionAction[PermissionAction["Delete"] = 4] = "Delete";
    PermissionAction[PermissionAction["See"] = 5] = "See";
    PermissionAction[PermissionAction["Modify"] = 6] = "Modify";
})(PermissionAction || (PermissionAction = {}));
var PermissionScope;
(function (PermissionScope) {
    PermissionScope[PermissionScope["Module"] = 1] = "Module";
    PermissionScope[PermissionScope["Entity"] = 2] = "Entity";
    PermissionScope[PermissionScope["Property"] = 3] = "Property";
})(PermissionScope || (PermissionScope = {}));

class WorkspaceDialogService {
    constructor() {
        this.dialogStackState = signal([]);
        this.nextDialogId = 1;
        this.dialogs = computed(() => this.dialogStackState());
        this.activeDialog = computed(() => {
            const dialogStack = this.dialogStackState();
            return dialogStack.length > 0 ? dialogStack[dialogStack.length - 1] : null;
        });
    }
    openDialog(request) {
        return new Promise((resolve) => {
            this.dialogStackState.update((current) => [...current, {
                    id: this.nextDialogId++,
                    okLabel: 'OK',
                    cancelLabel: 'Cancel',
                    showOkButton: true,
                    showCancelButton: true,
                    ...request,
                    resolve
                }]);
        });
    }
    async confirmOk(title, description) {
        const { ConfirmMessageDialogComponent } = await Promise.resolve().then(function () { return confirmMessageDialog_component; });
        const result = await this.openDialog({
            title,
            component: ConfirmMessageDialogComponent,
            showOkButton: false,
            showCancelButton: false,
            inputs: {
                description,
                buttons: [
                    {
                        label: 'OK',
                        value: true,
                        primary: true
                    }
                ]
            }
        });
        return result.status === 'confirmed' && result.value === true;
    }
    async confirmYesNo(title, description) {
        const { ConfirmMessageDialogComponent } = await Promise.resolve().then(function () { return confirmMessageDialog_component; });
        const result = await this.openDialog({
            title,
            component: ConfirmMessageDialogComponent,
            showOkButton: false,
            showCancelButton: false,
            inputs: {
                description,
                buttons: [
                    {
                        label: 'No',
                        value: false
                    },
                    {
                        label: 'Yes',
                        value: true,
                        primary: true
                    }
                ]
            }
        });
        return result.status === 'confirmed' && result.value === true;
    }
    confirm(value) {
        const dialogStack = this.dialogStackState();
        const activeDialog = dialogStack.length > 0
            ? dialogStack[dialogStack.length - 1]
            : null;
        if (!activeDialog) {
            return;
        }
        this.dialogStackState.update((current) => current.slice(0, -1));
        activeDialog.resolve({
            status: 'confirmed',
            value
        });
    }
    cancel() {
        this.cancelActiveDialog();
    }
    cancelActiveDialog() {
        const dialogStack = this.dialogStackState();
        const activeDialog = dialogStack.length > 0 ? dialogStack[dialogStack.length - 1] : null;
        if (!activeDialog) {
            return;
        }
        this.dialogStackState.update((current) => current.slice(0, -1));
        activeDialog.resolve({
            status: 'cancelled'
        });
    }
    static { this.ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: WorkspaceDialogService, deps: [], target: i0.ɵɵFactoryTarget.Injectable }); }
    static { this.ɵprov = i0.ɵɵngDeclareInjectable({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: WorkspaceDialogService, providedIn: 'root' }); }
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: WorkspaceDialogService, decorators: [{
            type: Injectable,
            args: [{
                    providedIn: 'root'
                }]
        }] });

const TEXT_QUEUE_DELAY_MS = 50;
const CHILL_PROPERTY_TYPE$2 = {
    Unknown: 0,
    Guid: 1,
    Integer: 10,
    Decimal: 20,
    Date: 30,
    Time: 40,
    DateTime: 50,
    Duration: 60,
    Boolean: 70,
    String: 80,
    Text: 81,
    Select: 90,
    Json: 99,
    ChillEntity: 1000,
    ChillEntityCollection: 1010,
    ChillQuery: 1100
};
class ChillService {
    constructor() {
        this.chill = inject(ChillSharpNgClient);
        this.dialog = inject(WorkspaceDialogService);
        this.sessionState = signal(this.readStoredSession());
        this.currentUserGuidState = signal('');
        this.userPreferencesState = signal(this.readStoredUserPreferences());
        this.textVersion = signal(0);
        this.textCache = new Map();
        this.pendingTextRequests = new Map();
        this.inFlightTextRequests = new Set();
        this.pendingTextResolvers = new Map();
        this.permissionRuleOwners = new Map();
        this.isTimeZoneAlignmentPromptOpen = false;
        this.textQueueHandle = null;
        this.session = this.sessionState.asReadonly();
        this.isAuthenticated = computed(() => this.sessionState() !== null);
        this.userName = computed(() => this.sessionState()?.userName ?? '');
        this.displayCultureName = computed(() => this.userPreferencesState().displayCultureName);
        this.displayTimeZone = computed(() => this.userPreferencesState().displayTimeZone);
        this.displayDateFormat = computed(() => this.userPreferencesState().displayDateFormat);
        this.displayNumberFormat = computed(() => this.userPreferencesState().displayNumberFormat);
        this.syncClientSession(this.sessionState());
        this.logStartupDiagnostics();
    }
    async initialize() {
        const userGuid = await this.resolveCurrentUserGuid();
        if (!userGuid) {
            return;
        }
        const user = await this.loadCurrentUserPreferences(userGuid, {
            promptForTimeZoneMismatch: false,
            clearSessionOnNotFound: true
        });
        if (!user) {
            return;
        }
        globalThis.setTimeout(() => {
            void this.promptForTimeZoneAlignment(userGuid, user);
        }, 0);
    }
    version() {
        const versionFn = this.chill.version;
        if (typeof versionFn !== 'function') {
            return this.T('1EB1A234-D374-48B1-9E14-C9A7BAE1C31D', 'Client version is unavailable on the current ChillSharp instance.', 'La versione del client non è disponibile nell\'istanza corrente di ChillSharp.');
        }
        return versionFn.call(this.chill);
    }
    currentCultureName() {
        return this.displayCultureName().trim() || CHILL_CULTURE;
    }
    currentTimeZone() {
        return this.displayTimeZone().trim() || this.readBrowserTimeZone();
    }
    currentDateFormat() {
        const configuredFormat = this.displayDateFormat().trim();
        return configuredFormat || this.defaultDateFormatForCulture(this.currentCultureName());
    }
    currentNumberFormat() {
        return this.displayNumberFormat().trim() || this.currentCultureName();
    }
    T(labelGuid, primaryDefaultText, secondaryDefaultText) {
        this.textVersion();
        const key = this.normalizeLabelGuid(labelGuid);
        const fallbackText = this.selectDefaultText(primaryDefaultText, secondaryDefaultText);
        if (!key) {
            return fallbackText;
        }
        const cachedText = this.textCache.get(key);
        if (cachedText !== undefined) {
            return cachedText;
        }
        this.enqueueTextRequest(key, primaryDefaultText, secondaryDefaultText, fallbackText);
        return fallbackText;
    }
    async TAsync(labelGuid, primaryDefaultText, secondaryDefaultText) {
        const key = this.normalizeLabelGuid(labelGuid);
        const fallbackText = this.selectDefaultText(primaryDefaultText, secondaryDefaultText);
        if (!key) {
            return fallbackText;
        }
        const cachedText = this.textCache.get(key);
        if (cachedText !== undefined) {
            return cachedText;
        }
        return new Promise((resolve) => {
            const pendingResolvers = this.pendingTextResolvers.get(key) ?? [];
            pendingResolvers.push(resolve);
            this.pendingTextResolvers.set(key, pendingResolvers);
            this.enqueueTextRequest(key, primaryDefaultText, secondaryDefaultText, fallbackText);
        });
    }
    test() {
        const testUrl = this.buildApiUrl('test');
        return from(globalThis.fetch(testUrl)).pipe(switchMap((response) => {
            if (!response.ok) {
                return throwError(() => new Error(`Unexpected error executing GET ${testUrl}: ${response.status} ${response.statusText}`.trim()));
            }
            return from(response.text());
        }), map((response) => response.trim()), catchError((error) => this.rethrowFriendlyError(error)));
    }
    getSchema(chillType, chillViewCode, cultureName, update = false) {
        return this.chill.getSchema(chillType, chillViewCode, this.resolveCultureName(cultureName), update).pipe(map((response) => this.normalizeSchema(response)), catchError((error) => this.rethrowFriendlyError(error)));
    }
    setSchema(schema) {
        const request = {
            chillType: schema.chillType ?? '',
            chillViewCode: schema.chillViewCode ?? '',
            displayName: schema.displayName ?? '',
            handleAttachments: schema.handleAttachments === true,
            enableMCP: schema.enableMCP === true,
            mcpDescription: schema.mcpDescription?.trim() ?? '',
            queryRelatedChillType: schema.queryRelatedChillType ?? null,
            metadata: this.serializeMetadataRecord(schema.metadata),
            relations: this.toSchemaRelationDtos(schema.relations),
            properties: (schema.properties ?? []).map((property) => ({
                name: property.name,
                displayName: property.displayName ?? property.name,
                propertyType: (property.propertyType ?? 0),
                simplePropertyType: property.simplePropertyType ?? '',
                mcpDescription: property.mcpDescription ?? '',
                referenceChillType: property.referenceChillType ?? property.chillType ?? null,
                referenceChillTypeQuery: property.referenceChillTypeQuery ?? null,
                metadata: this.serializeMetadataRecord(property.metadata)
            }))
        };
        return this.chill.setSchema(request).pipe(map((response) => this.normalizeSchema(response)), catchError((error) => this.rethrowFriendlyError(error)));
    }
    getSchemaList(cultureName) {
        const client = this.chill;
        const resolvedCultureName = this.resolveCultureName(cultureName);
        if (typeof client.getSchemaList === 'function') {
            return this.chill.getSchemaList(resolvedCultureName).pipe(map((response) => (response ?? [])), catchError((error) => this.rethrowFriendlyError(error)));
        }
        const rawClient = client.getRawClient?.();
        if (typeof rawClient?.getSchemaList === 'function') {
            return from(rawClient.getSchemaList(resolvedCultureName)).pipe(map((response) => (response ?? [])), catchError((error) => this.rethrowFriendlyError(error)));
        }
        return throwError(() => new Error(this.T('25A8B513-F55B-428D-B85F-49A6D39F165A', 'The current ChillSharp client does not expose getSchemaList().', 'Il client ChillSharp corrente non espone getSchemaList().')));
    }
    setText(labelGuid, value, cultureName) {
        const normalizedLabelGuid = this.normalizeLabelGuid(labelGuid);
        const normalizedValue = value.trim();
        return this.chill.setText({
            labelGuid: normalizedLabelGuid,
            cultureName: this.resolveCultureName(cultureName),
            value: normalizedValue
        }).pipe(map((response) => {
            const resolvedLabelGuid = this.readJsonString(response, 'LabelGuid') ?? normalizedLabelGuid;
            const resolvedValue = this.readJsonString(response, 'Value') ?? normalizedValue;
            this.textCache.set(this.normalizeLabelGuid(resolvedLabelGuid), resolvedValue);
            this.textVersion.update((current) => current + 1);
            return resolvedValue;
        }), catchError((error) => this.rethrowFriendlyError(error)));
    }
    watchEntityChanges(chillType, guid) {
        const client = this.chill;
        if (typeof client.watchEntityChanges === 'function') {
            return client.watchEntityChanges(chillType, guid).pipe(map((changes) => changes.map((change) => this.normalizeEntityChangeNotification(change))), catchError((error) => this.rethrowFriendlyError(error)));
        }
        const rawClient = client.getRawClient?.();
        if (typeof rawClient?.subscribeToEntityChanges === 'function') {
            const subscribeToEntityChanges = rawClient.subscribeToEntityChanges.bind(rawClient);
            return new Observable((subscriber) => {
                let remoteSubscription = null;
                let isClosed = false;
                void subscribeToEntityChanges(chillType, async (changes) => {
                    subscriber.next(changes.map((change) => this.normalizeEntityChangeNotification(change)));
                }, guid)
                    .then(async (subscription) => {
                    remoteSubscription = subscription;
                    if (isClosed) {
                        await subscription.unsubscribe();
                    }
                })
                    .catch((error) => {
                    subscriber.error(error);
                });
                return () => {
                    isClosed = true;
                    if (remoteSubscription) {
                        void remoteSubscription.unsubscribe();
                    }
                };
            }).pipe(catchError((error) => this.rethrowFriendlyError(error)));
        }
        return throwError(() => new Error(this.T('19AEF9E0-85E9-40DF-A786-22A61C52A9A3', 'The current ChillSharp client does not expose entity-change notifications.', 'Il client ChillSharp corrente non espone le notifiche di modifica entità.')));
    }
    watchEntity(chillType, guid) {
        return this.watchEntityChanges(chillType, guid);
    }
    watchChillType(chillType) {
        return this.watchEntityChanges(chillType, null);
    }
    disconnectEntityChanges() {
        const client = this.chill;
        if (typeof client.disconnectEntityChanges === 'function') {
            return client.disconnectEntityChanges().pipe(catchError((error) => this.rethrowFriendlyError(error)));
        }
        const rawClient = client.getRawClient?.();
        if (typeof rawClient?.disconnectEntityChanges === 'function') {
            return from(rawClient.disconnectEntityChanges()).pipe(catchError((error) => this.rethrowFriendlyError(error)));
        }
        return throwError(() => new Error(this.T('BB7AA5AE-3615-47CE-B026-D7D17989D18D', 'The current ChillSharp client does not expose notification disconnection.', 'Il client ChillSharp corrente non espone la disconnessione delle notifiche.')));
    }
    getAuthUsers() {
        return this.chill.getAuthUserList().pipe(map((response) => this.normalizeAuthUsers(response)), catchError((error) => this.rethrowFriendlyError(error)));
    }
    getAuthUserDetails(userGuid) {
        return this.chill.getAuthUser(userGuid).pipe(map((response) => response), catchError((error) => this.rethrowFriendlyError(error)));
    }
    createAuthUser(request) {
        return this.chill.setAuthUser({
            guid: null,
            externalId: request.externalId,
            userName: request.userName,
            displayName: request.displayName,
            displayCultureName: request.displayCultureName,
            displayTimeZone: request.displayTimeZone,
            displayDateFormat: request.displayDateFormat,
            displayNumberFormat: request.displayNumberFormat,
            isActive: request.isActive,
            canManagePermissions: request.canManagePermissions,
            canManageSchema: request.canManageSchema,
            menuHierarchy: request.menuHierarchy,
            roleGuids: [],
            permissions: []
        }).pipe(map((response) => this.normalizeAuthUser(response)), catchError((error) => this.rethrowFriendlyError(error)));
    }
    updateAuthUser(userGuid, request) {
        return this.chill.getAuthUser(userGuid).pipe(map((response) => this.buildSetAuthUserRequest(response, request)), switchMap((payload) => this.chill.setAuthUser(payload)), map((response) => this.normalizeAuthUser(response)), catchError((error) => this.rethrowFriendlyError(error)));
    }
    updateUserProfile(userGuid, request) {
        return this.chill.getAuthUser(userGuid).pipe(map((response) => this.buildSetAuthUserRequest(response, {
            externalId: response.externalId,
            userName: response.userName,
            displayName: request.displayName,
            displayCultureName: request.displayCultureName,
            displayTimeZone: request.displayTimeZone,
            displayDateFormat: request.displayDateFormat,
            displayNumberFormat: request.displayNumberFormat,
            isActive: response.isActive,
            canManagePermissions: response.canManagePermissions,
            canManageSchema: response.canManageSchema,
            menuHierarchy: response.menuHierarchy ?? ''
        })), switchMap((payload) => this.chill.setAuthUser(payload)), map((response) => response), tap((response) => {
            if (this.isCurrentUser(userGuid)) {
                this.persistUserPreferences(this.toStoredUserPreferences(response));
            }
        }), catchError((error) => this.rethrowFriendlyError(error)));
    }
    getAuthUserRoles(userGuid) {
        return this.chill.getAuthUser(userGuid).pipe(map((response) => this.normalizeAuthRoles(response.roles)), catchError((error) => this.rethrowFriendlyError(error)));
    }
    getAuthUserAccess(userGuid) {
        return this.chill.getAuthUser(userGuid).pipe(map((response) => ({
            user: this.normalizeAuthUser(response),
            roles: this.normalizeAuthRoles(response.roles),
            permissions: this.normalizeAuthPermissionRules(response.permissions, { kind: 'user', guid: userGuid })
        })), catchError((error) => this.rethrowFriendlyError(error)));
    }
    saveAuthUserAccess(userGuid, roleGuids, permissions) {
        return this.chill.getAuthUser(userGuid).pipe(map((response) => this.buildSetAuthUserRequest(response, undefined, () => this.normalizeGuidList(roleGuids), () => permissions.map((permission) => this.toAuthPermissionRuleItem(permission)))), switchMap((payload) => this.chill.setAuthUser(payload)), map((response) => ({
            user: this.normalizeAuthUser(response),
            roles: this.normalizeAuthRoles(response.roles),
            permissions: this.normalizeAuthPermissionRules(response.permissions, { kind: 'user', guid: userGuid })
        })), catchError((error) => this.rethrowFriendlyError(error)));
    }
    assignAuthRole(userGuid, roleGuid) {
        return this.chill.getAuthUser(userGuid).pipe(map((response) => this.buildSetAuthUserRequest(response, undefined, (roleGuids) => [...new Set([...roleGuids, roleGuid])])), switchMap((payload) => this.chill.setAuthUser(payload)), map(() => void 0), catchError((error) => this.rethrowFriendlyError(error)));
    }
    removeAuthRole(userGuid, roleGuid) {
        return this.chill.getAuthUser(userGuid).pipe(map((response) => this.buildSetAuthUserRequest(response, undefined, (roleGuids) => roleGuids.filter((entry) => entry !== roleGuid))), switchMap((payload) => this.chill.setAuthUser(payload)), map(() => void 0), catchError((error) => this.rethrowFriendlyError(error)));
    }
    getAuthRoles() {
        return this.chill.getAuthRoleList().pipe(map((response) => this.normalizeAuthRoles(response)), catchError((error) => this.rethrowFriendlyError(error)));
    }
    getModuleList() {
        const client = this.chill;
        const source = typeof client.getModuleList === 'function'
            ? client.getModuleList.bind(this.chill)
            : client.getAuthModuleList?.bind(this.chill);
        if (!source) {
            return throwError(() => new Error('The current ChillSharp client does not expose getModuleList().'));
        }
        return source().pipe(map((response) => this.normalizeStringList(response)), catchError((error) => this.rethrowFriendlyError(error)));
    }
    getEntityList(module) {
        const client = this.chill;
        const source = typeof client.getEntityList === 'function'
            ? client.getEntityList.bind(this.chill)
            : client.getAuthEntityList?.bind(this.chill);
        if (!source) {
            return throwError(() => new Error('The current ChillSharp client does not expose getEntityList().'));
        }
        return source(module).pipe(map((response) => this.normalizeStringList(response)), catchError((error) => this.rethrowFriendlyError(error)));
    }
    getQueryList(module) {
        const client = this.chill;
        const source = typeof client.getQueryList === 'function'
            ? client.getQueryList.bind(this.chill)
            : client.getAuthQueryList?.bind(this.chill);
        if (!source) {
            return throwError(() => new Error('The current ChillSharp client does not expose getQueryList().'));
        }
        return source(module).pipe(map((response) => this.normalizeStringList(response)), catchError((error) => this.rethrowFriendlyError(error)));
    }
    getPropertyList(chillType) {
        const client = this.chill;
        const source = typeof client.getPropertyList === 'function'
            ? client.getPropertyList.bind(this.chill)
            : client.getAuthPropertyList?.bind(this.chill);
        if (!source) {
            return throwError(() => new Error('The current ChillSharp client does not expose getPropertyList().'));
        }
        return source(chillType).pipe(map((response) => this.normalizeStringList(response)), catchError((error) => this.rethrowFriendlyError(error)));
    }
    getAuthRoleAccess(roleGuid) {
        return this.chill.getAuthRole(roleGuid).pipe(map((response) => ({
            role: this.normalizeAuthRole(response),
            users: this.normalizeAuthUsers(response.users),
            permissions: this.normalizeAuthPermissionRules(response.permissions, { kind: 'role', guid: roleGuid })
        })), catchError((error) => this.rethrowFriendlyError(error)));
    }
    saveAuthRoleAccess(roleGuid, userGuids, permissions) {
        return this.chill.getAuthRole(roleGuid).pipe(map((response) => this.buildSetAuthRoleRequest(response, undefined, () => this.normalizeGuidList(userGuids), () => permissions.map((permission) => this.toAuthPermissionRuleItem(permission)))), switchMap((payload) => this.chill.setAuthRole(payload)), map((response) => ({
            role: this.normalizeAuthRole(response),
            users: this.normalizeAuthUsers(response.users),
            permissions: this.normalizeAuthPermissionRules(response.permissions, { kind: 'role', guid: roleGuid })
        })), catchError((error) => this.rethrowFriendlyError(error)));
    }
    createAuthRole(request) {
        return this.chill.setAuthRole(this.buildSetAuthRoleRequest(null, request)).pipe(map((response) => this.normalizeAuthRole(response)), catchError((error) => this.rethrowFriendlyError(error)));
    }
    updateAuthRole(roleGuid, request) {
        return this.chill.getAuthRole(roleGuid).pipe(map((response) => this.buildSetAuthRoleRequest(response, request)), switchMap((payload) => this.chill.setAuthRole(payload)), map((response) => this.normalizeAuthRole(response)), catchError((error) => this.rethrowFriendlyError(error)));
    }
    getAuthPermissionRules(userGuid, roleGuid) {
        if (userGuid?.trim()) {
            return this.chill.getAuthUser(userGuid).pipe(map((response) => this.normalizeAuthPermissionRules(response.permissions, { kind: 'user', guid: userGuid })), catchError((error) => this.rethrowFriendlyError(error)));
        }
        if (roleGuid?.trim()) {
            return this.chill.getAuthRole(roleGuid).pipe(map((response) => this.normalizeAuthPermissionRules(response.permissions, { kind: 'role', guid: roleGuid })), catchError((error) => this.rethrowFriendlyError(error)));
        }
        return this.chill.getAuthPermissions().pipe(map((response) => this.normalizeAuthPermissionRules(response.permissions)), catchError((error) => this.rethrowFriendlyError(error)));
    }
    createAuthPermissionRule(request) {
        const userGuid = request.userGuid?.trim();
        if (userGuid) {
            return this.chill.getAuthUser(userGuid).pipe(map((response) => this.buildSetAuthUserRequest(response, undefined, undefined, (permissions) => [...permissions, this.toAuthPermissionRuleItem(request)])), switchMap((payload) => this.chill.setAuthUser(payload)), map((response) => this.getLatestAuthPermissionRule(response.permissions, { kind: 'user', guid: userGuid })), catchError((error) => this.rethrowFriendlyError(error)));
        }
        const roleGuid = request.roleGuid?.trim();
        if (roleGuid) {
            return this.chill.getAuthRole(roleGuid).pipe(map((response) => this.buildSetAuthRoleRequest(response, undefined, undefined, (permissions) => [...permissions, this.toAuthPermissionRuleItem(request)])), switchMap((payload) => this.chill.setAuthRole(payload)), map((response) => this.getLatestAuthPermissionRule(response.permissions, { kind: 'role', guid: roleGuid })), catchError((error) => this.rethrowFriendlyError(error)));
        }
        return throwError(() => new Error('Auth permission management requires either userGuid or roleGuid.'));
    }
    deleteAuthPermissionRule(ruleGuid) {
        const owner = this.permissionRuleOwners.get(ruleGuid.trim());
        if (!owner) {
            return throwError(() => new Error('Auth permission rule owner is unknown. Reload permissions before deleting a rule.'));
        }
        if (owner.kind === 'user') {
            return this.chill.getAuthUser(owner.guid).pipe(map((response) => this.buildSetAuthUserRequest(response, undefined, undefined, (permissions) => permissions.filter((permission) => this.readJsonString(permission, 'Guid') !== ruleGuid.trim()))), switchMap((payload) => this.chill.setAuthUser(payload)), map(() => {
                this.permissionRuleOwners.delete(ruleGuid.trim());
                return void 0;
            }), catchError((error) => this.rethrowFriendlyError(error)));
        }
        return this.chill.getAuthRole(owner.guid).pipe(map((response) => this.buildSetAuthRoleRequest(response, undefined, undefined, (permissions) => permissions.filter((permission) => this.readJsonString(permission, 'Guid') !== ruleGuid.trim()))), switchMap((payload) => this.chill.setAuthRole(payload)), map(() => {
            this.permissionRuleOwners.delete(ruleGuid.trim());
            return void 0;
        }), catchError((error) => this.rethrowFriendlyError(error)));
    }
    query(request) {
        return this.chill.query(request).pipe(map((response) => response), catchError((error) => this.rethrowFriendlyError(error)));
    }
    lookup(request) {
        const client = this.chill;
        if (typeof client.lookup === 'function') {
            return client.lookup(request).pipe(map((response) => response), catchError((error) => this.rethrowFriendlyError(error)));
        }
        const rawClient = client.getRawClient?.();
        if (typeof rawClient?.lookup === 'function') {
            return from(rawClient.lookup(request)).pipe(map((response) => response), catchError((error) => this.rethrowFriendlyError(error)));
        }
        return throwError(() => new Error(this.T('2D0D795E-ABEA-4507-AB12-F2BE1E2FA8E8', 'The current ChillSharp client does not expose lookup().', 'Il client ChillSharp corrente non espone lookup().')));
    }
    autocomplete(request) {
        return this.chill.autocomplete(request).pipe(map((response) => response), catchError((error) => this.rethrowFriendlyError(error)));
    }
    find(request) {
        return this.chill.find(request).pipe(map((response) => response), catchError((error) => this.rethrowFriendlyError(error)));
    }
    validate(request) {
        return this.chill.validate(request).pipe(map((response) => (response ?? [])), catchError((error) => this.rethrowFriendlyError(error)));
    }
    create(request) {
        return this.chill.create(request).pipe(map((response) => response), catchError((error) => this.rethrowFriendlyError(error)));
    }
    update(request) {
        return this.chill.update(request).pipe(map((response) => response), catchError((error) => this.rethrowFriendlyError(error)));
    }
    delete(request) {
        return this.chill.delete(request).pipe(map(() => void 0), catchError((error) => this.rethrowFriendlyError(error)));
    }
    chunk(operations) {
        return this.chill.chunk(operations).pipe(map((response) => response), catchError((error) => this.rethrowFriendlyError(error)));
    }
    uploadAttachment(targetEntity, file, options = {}) {
        return this.chill.uploadAttachment(targetEntity, file, options).pipe(map((response) => response), catchError((error) => this.rethrowFriendlyError(error)));
    }
    downloadAttachment(attachmentOrGuid) {
        return this.chill.downloadAttachment(attachmentOrGuid).pipe(catchError((error) => this.rethrowFriendlyError(error)));
    }
    getMenu(parentGuid) {
        return this.chill.getMenu(parentGuid).pipe(map((response) => (response ?? []).map((item) => this.normalizeMenuItem(item))), catchError((error) => this.rethrowFriendlyError(error)));
    }
    getEntityOptions(chillType) {
        return this.chill.getEntityOptions(chillType).pipe(map((response) => this.normalizeEntityOptions(response)), catchError((error) => this.rethrowFriendlyError(error)));
    }
    setEntityOptions(entityOptions) {
        return this.chill.setEntityOptions(this.toEntityOptionsDto(entityOptions)).pipe(map((response) => this.normalizeEntityOptions(response)), catchError((error) => this.rethrowFriendlyError(error)));
    }
    setMenu(menuItem) {
        return this.chill.setMenu(this.toMenuDto(menuItem)).pipe(map((response) => this.normalizeMenuItem(response)), catchError((error) => this.rethrowFriendlyError(error)));
    }
    deleteMenu(menuItemGuid) {
        return this.chill.deleteMenu(menuItemGuid).pipe(map(() => void 0), catchError((error) => this.rethrowFriendlyError(error)));
    }
    toJsonValue(schema, propertyName, value) {
        const property = schema?.properties.find((candidate) => candidate.name === propertyName);
        return this.serializePropertyValue(property, value);
    }
    formatDisplayNumber(value) {
        const numberFormat = this.readNumberFormatConfig();
        if (numberFormat.kind === 'locale') {
            return new Intl.NumberFormat(numberFormat.locale).format(value);
        }
        return this.formatNumberWithPattern(value, numberFormat);
    }
    parseDisplayInteger(value) {
        const parsedValue = this.parseLocalizedNumber(value);
        return parsedValue !== null && Number.isInteger(parsedValue)
            ? parsedValue
            : null;
    }
    parseDisplayDecimal(value) {
        return this.parseLocalizedNumber(value);
    }
    readDisplayNumber(value) {
        if (typeof value === 'number' && Number.isFinite(value)) {
            return value;
        }
        if (typeof value === 'string' && value.trim()) {
            return this.parseLocalizedNumber(value);
        }
        return null;
    }
    formatDisplayDate(value) {
        const normalizedValue = value.trim();
        const match = normalizedValue.match(/^(\d{4})-(\d{2})-(\d{2})(?:$|[T\s])/);
        if (!match) {
            return normalizedValue;
        }
        const year = Number(match[1]);
        const month = Number(match[2]);
        const day = Number(match[3]);
        return this.isValidDateParts(year, month, day)
            ? this.formatDateParts(year, month, day)
            : normalizedValue;
    }
    parseDisplayDate(value) {
        const normalizedValue = value.trim();
        if (!normalizedValue) {
            return null;
        }
        const leadingIsoDateMatch = normalizedValue.match(/^(\d{4})-(\d{2})-(\d{2})(?:$|[T\s])/);
        if (leadingIsoDateMatch) {
            const year = Number(leadingIsoDateMatch[1]);
            const month = Number(leadingIsoDateMatch[2]);
            const day = Number(leadingIsoDateMatch[3]);
            return this.isValidDateParts(year, month, day)
                ? this.toIsoDate(year, month, day)
                : null;
        }
        const parts = this.parseDisplayDateParts(normalizedValue);
        if (parts) {
            return this.toIsoDate(parts.year, parts.month, parts.day);
        }
        const parsed = new Date(normalizedValue);
        if (Number.isNaN(parsed.getTime())) {
            return null;
        }
        return this.toIsoDate(parsed.getFullYear(), parsed.getMonth() + 1, parsed.getDate());
    }
    formatDisplayTime(value) {
        const normalizedValue = this.parseDisplayTime(value);
        if (!normalizedValue) {
            return value.trim();
        }
        const match = normalizedValue.match(/^(\d{2}):(\d{2})(?::(\d{2})(\.\d{1,7})?)?$/);
        if (!match) {
            return normalizedValue;
        }
        const seconds = match[3] && match[3] !== '00' ? `:${match[3]}` : '';
        const fraction = seconds ? (match[4] ?? '') : '';
        return `${match[1]}:${match[2]}${seconds}${fraction}`;
    }
    parseDisplayTime(value) {
        const normalizedValue = value.trim();
        if (!normalizedValue) {
            return null;
        }
        const directMatch = normalizedValue.match(/^(\d{1,2}):(\d{2})(?::(\d{2})(\.\d{1,7})?)?$/);
        if (directMatch) {
            return this.normalizeTimeParts(directMatch[1], directMatch[2], directMatch[3], directMatch[4]);
        }
        const isoMatch = normalizedValue.match(/^\d{4}-\d{2}-\d{2}[T\s](\d{1,2}):(\d{2})(?::(\d{2})(\.\d{1,7})?)?(?:Z|[+-]\d{2}:\d{2})?$/);
        if (isoMatch) {
            return this.normalizeTimeParts(isoMatch[1], isoMatch[2], isoMatch[3], isoMatch[4]);
        }
        return null;
    }
    formatDisplayDateTime(value) {
        const normalizedValue = value.trim();
        if (!normalizedValue) {
            return '';
        }
        const parsed = new Date(normalizedValue);
        if (Number.isNaN(parsed.getTime())) {
            return normalizedValue;
        }
        const parts = this.readZonedDateTimeParts(parsed, this.currentTimeZone());
        const formattedDate = this.formatDateParts(parts.year, parts.month, parts.day);
        const seconds = parts.second !== 0 ? `:${`${parts.second}`.padStart(2, '0')}` : '';
        return `${formattedDate} ${`${parts.hour}`.padStart(2, '0')}:${`${parts.minute}`.padStart(2, '0')}${seconds}`;
    }
    parseDisplayDateTime(value) {
        const normalizedValue = value.trim();
        if (!normalizedValue) {
            return null;
        }
        const directMatch = normalizedValue.match(/^(\d{4})-(\d{2})-(\d{2})[T\s](\d{1,2}):(\d{2})(?::(\d{2})(\.\d{1,7})?)?(Z|[+-]\d{2}:\d{2})?$/);
        if (directMatch) {
            const [, yearText, monthText, dayText, hourText, minuteText, secondText, fractionText, offsetText] = directMatch;
            const year = Number(yearText);
            const month = Number(monthText);
            const day = Number(dayText);
            const hour = Number(hourText);
            const minute = Number(minuteText);
            const second = secondText ? Number(secondText) : 0;
            if (!this.isValidDateParts(year, month, day) || hour > 23 || minute > 59 || second > 59) {
                return null;
            }
            if (offsetText) {
                return `${yearText}-${monthText}-${dayText}T${`${hour}`.padStart(2, '0')}:${minuteText}:${`${second}`.padStart(2, '0')}${fractionText ?? ''}${offsetText}`;
            }
            return this.toZonedIsoDateTime(year, month, day, hour, minute, second, fractionText ?? '');
        }
        const splitMatch = normalizedValue.match(/^(.*?)[T\s]+(\d{1,2}):(\d{2})(?::(\d{2})(\.\d{1,7})?)?$/);
        if (splitMatch) {
            const dateParts = this.parseDisplayDateParts(splitMatch[1]);
            if (!dateParts) {
                return null;
            }
            const hour = Number(splitMatch[2]);
            const minute = Number(splitMatch[3]);
            const second = splitMatch[4] ? Number(splitMatch[4]) : 0;
            if (hour > 23 || minute > 59 || second > 59) {
                return null;
            }
            return this.toZonedIsoDateTime(dateParts.year, dateParts.month, dateParts.day, hour, minute, second, splitMatch[5] ?? '');
        }
        const parsed = new Date(normalizedValue);
        if (Number.isNaN(parsed.getTime())) {
            return null;
        }
        const parts = this.readZonedDateTimeParts(parsed, this.currentTimeZone());
        return this.toZonedIsoDateTime(parts.year, parts.month, parts.day, parts.hour, parts.minute, parts.second, '');
    }
    prepareForm(schema, source, options) {
        const controls = Object.fromEntries((schema.properties ?? []).map((property) => [
            property.name,
            new FormControl(this.readPreparedFormValue(source, property), { nonNullable: true })
        ]));
        const form = new FormGroup(controls);
        for (const property of schema.properties ?? []) {
            const control = controls[property.name];
            const asyncValidators = options?.createControlAsyncValidators?.({
                schema,
                property,
                source,
                getForm: () => form
            });
            if (!control || !asyncValidators) {
                continue;
            }
            control.setAsyncValidators(asyncValidators);
        }
        return form;
    }
    register(request) {
        return this.chill.registerAuthAccount(this.toRegisterAuthIdentityRequest(request)).pipe(map((response) => this.toTokenResponse(response)), switchMap((response) => from(this.handleAuthenticatedResponse(response, false)).pipe(map(() => response))), catchError((error) => this.rethrowFriendlyError(error)));
    }
    login(request) {
        return this.chill.loginAuthAccount(this.toLoginAuthIdentityRequest(request)).pipe(map((response) => this.toTokenResponse(response)), switchMap((response) => from(this.handleAuthenticatedResponse(response, true)).pipe(map(() => response))), catchError((error) => this.rethrowFriendlyError(error)));
    }
    refreshSession() {
        return this.chill.refreshAuthAccount().pipe(map((response) => this.toTokenResponse(response)), switchMap((response) => from(this.handleAuthenticatedResponse(response, false)).pipe(map(() => response))), catchError((error) => this.rethrowFriendlyError(error)));
    }
    requestPasswordReset(request) {
        return this.chill.requestAuthPasswordReset(this.toRequestPasswordResetRequest(request)).pipe(map((response) => response), catchError((error) => this.rethrowFriendlyError(error)));
    }
    confirmPasswordReset(request) {
        return this.chill.resetAuthPassword(this.toResetPasswordRequest(request)).pipe(map((response) => response), catchError((error) => this.rethrowFriendlyError(error)));
    }
    logout() {
        this.clearSession();
    }
    formatError(error) {
        if (typeof error === 'string' && error.trim()) {
            return error;
        }
        if (error instanceof Error && error.message.trim()) {
            return error.message;
        }
        return this.T('48D1CE91-0230-4D35-90D0-A776D804B0A8', 'Unexpected error while calling ChillSharp.', 'Errore imprevisto durante la chiamata a ChillSharp.');
    }
    persistSession(response) {
        const accessToken = response.AccessToken?.trim() ?? '';
        if (!accessToken) {
            return;
        }
        const session = {
            accessToken,
            accessTokenExpiresUtc: response.AccessTokenExpiresUtc ?? '',
            refreshToken: response.RefreshToken ?? '',
            refreshTokenExpiresUtc: response.RefreshTokenExpiresUtc ?? '',
            userId: response.UserId ?? '',
            userName: response.UserName ?? ''
        };
        localStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify(session));
        this.sessionState.set(session);
        this.syncClientSession(session);
    }
    clearSession() {
        localStorage.removeItem(SESSION_STORAGE_KEY);
        this.sessionState.set(null);
        this.currentUserGuidState.set('');
        this.syncClientSession(null);
    }
    async handleAuthenticatedResponse(response, promptForTimeZoneMismatch) {
        this.persistSession(response);
        const userGuid = await this.resolveCurrentUserGuid();
        if (!userGuid) {
            return;
        }
        await this.loadCurrentUserPreferences(userGuid, {
            promptForTimeZoneMismatch,
            clearSessionOnNotFound: false
        });
    }
    async loadCurrentUserPreferences(userGuid, options) {
        try {
            const user = await firstValueFrom(this.getAuthUserDetails(userGuid));
            this.currentUserGuidState.set(user.guid?.trim() ?? userGuid.trim());
            this.persistUserPreferences(this.toStoredUserPreferences(user));
            if (options.promptForTimeZoneMismatch) {
                await this.promptForTimeZoneAlignment(userGuid, user);
            }
            return user;
        }
        catch (error) {
            if (options.clearSessionOnNotFound && this.isNotFoundError(error)) {
                console.info('[ChillService] Current user was not found while loading preferences. Clearing stale session.', {
                    userGuid
                });
                this.clearSession();
                return null;
            }
            console.warn('[ChillService] Unable to load current user preferences', error);
            return null;
        }
    }
    async resolveCurrentUserGuid() {
        const session = this.sessionState();
        const normalizedUserId = session?.userId?.trim() ?? '';
        const normalizedUserName = session?.userName?.trim().toLowerCase() ?? '';
        if (!normalizedUserId && !normalizedUserName) {
            return '';
        }
        try {
            const users = await firstValueFrom(this.getAuthUsers());
            const matchedUser = users.find((user) => user.guid.trim() === normalizedUserId
                || user.userName.trim().toLowerCase() === normalizedUserName);
            if (matchedUser?.guid.trim()) {
                return matchedUser.guid.trim();
            }
        }
        catch (error) {
            console.warn('[ChillService] Unable to resolve current user guid from auth user list', error);
        }
        return normalizedUserId;
    }
    readStoredSession() {
        const raw = localStorage.getItem(SESSION_STORAGE_KEY);
        if (!raw) {
            return null;
        }
        try {
            const session = JSON.parse(raw);
            if (!session.accessToken) {
                return null;
            }
            return {
                accessToken: session.accessToken,
                accessTokenExpiresUtc: session.accessTokenExpiresUtc ?? '',
                refreshToken: session.refreshToken ?? '',
                refreshTokenExpiresUtc: session.refreshTokenExpiresUtc ?? '',
                userId: session.userId ?? '',
                userName: session.userName ?? ''
            };
        }
        catch {
            localStorage.removeItem(SESSION_STORAGE_KEY);
            return null;
        }
    }
    toTokenResponse(response) {
        return {
            AccessToken: this.readJsonString(response, 'AccessToken'),
            AccessTokenIssuedUtc: this.readJsonString(response, 'AccessTokenIssuedUtc'),
            AccessTokenExpiresUtc: this.readJsonString(response, 'AccessTokenExpiresUtc'),
            RefreshToken: this.readJsonString(response, 'RefreshToken'),
            RefreshTokenExpiresUtc: this.readJsonString(response, 'RefreshTokenExpiresUtc'),
            UserId: this.readJsonString(response, 'UserId'),
            UserName: this.readJsonString(response, 'UserName')
        };
    }
    syncClientSession(session) {
        const client = this.chill.getRawClient();
        if (typeof client.applyAuthToken !== 'function') {
            return;
        }
        client.applyAuthToken({
            AccessToken: session?.accessToken ?? '',
            AccessTokenExpiresUtc: session?.accessTokenExpiresUtc ?? '',
            RefreshToken: session?.refreshToken ?? '',
            RefreshTokenExpiresUtc: session?.refreshTokenExpiresUtc ?? '',
            UserName: session?.userName ?? ''
        }, true);
    }
    readJsonString(payload, key) {
        const directValue = payload[key];
        if (typeof directValue === 'string' && directValue.trim()) {
            return directValue.trim();
        }
        const camelKey = key.length > 1
            ? `${key[0].toLowerCase()}${key.slice(1)}`
            : key.toLowerCase();
        const camelValue = payload[camelKey];
        if (typeof camelValue === 'string' && camelValue.trim()) {
            return camelValue.trim();
        }
        const matchedKey = Object.keys(payload).find((candidate) => candidate.toLowerCase() === key.toLowerCase());
        const matchedValue = matchedKey ? payload[matchedKey] : undefined;
        return typeof matchedValue === 'string' && matchedValue.trim()
            ? matchedValue.trim()
            : undefined;
    }
    readJsonBoolean(payload, key) {
        const directValue = payload[key];
        if (typeof directValue === 'boolean') {
            return directValue;
        }
        const matchedKey = Object.keys(payload).find((candidate) => candidate.toLowerCase() === key.toLowerCase());
        const matchedValue = matchedKey ? payload[matchedKey] : undefined;
        return matchedValue === true;
    }
    readJsonNumber(payload, key) {
        const directValue = payload[key];
        if (typeof directValue === 'number') {
            return directValue;
        }
        const matchedKey = Object.keys(payload).find((candidate) => candidate.toLowerCase() === key.toLowerCase());
        const matchedValue = matchedKey ? payload[matchedKey] : undefined;
        return typeof matchedValue === 'number' ? matchedValue : 0;
    }
    readPermissionEffect(payload) {
        const value = this.readJsonNumber(payload, 'Effect');
        return value === PermissionEffect.Deny
            ? PermissionEffect.Deny
            : PermissionEffect.Allow;
    }
    readPermissionAction(payload) {
        const value = this.readJsonNumber(payload, 'Action');
        switch (value) {
            case PermissionAction.FullControl:
            case PermissionAction.Query:
            case PermissionAction.Create:
            case PermissionAction.Update:
            case PermissionAction.Delete:
            case PermissionAction.See:
            case PermissionAction.Modify:
                return value;
            default:
                return PermissionAction.Query;
        }
    }
    readPermissionScope(payload) {
        const value = this.readJsonNumber(payload, 'Scope');
        switch (value) {
            case PermissionScope.Module:
            case PermissionScope.Entity:
            case PermissionScope.Property:
                return value;
            default:
                return PermissionScope.Module;
        }
    }
    isJsonObject(value) {
        return !!value && typeof value === 'object' && !Array.isArray(value);
    }
    readPreparedFormValue(source, property) {
        const propertyName = property.name.trim();
        if (!source || !propertyName) {
            return this.emptySerializedValue(property.propertyType ?? CHILL_PROPERTY_TYPE$2.Unknown);
        }
        const properties = source.properties;
        if (properties && propertyName in properties) {
            return this.serializePropertyValue(property, properties[propertyName]);
        }
        if (propertyName in source) {
            return this.serializePropertyValue(property, source[propertyName]);
        }
        const pascalCaseName = `${propertyName[0]?.toUpperCase() ?? ''}${propertyName.slice(1)}`;
        return this.serializePropertyValue(property, source[pascalCaseName]);
    }
    serializePropertyValue(property, value) {
        const propertyType = property?.propertyType ?? CHILL_PROPERTY_TYPE$2.Unknown;
        if (typeof value === 'boolean') {
            return value;
        }
        if (typeof value === 'number') {
            return value;
        }
        if (this.isJsonObject(value)) {
            return value;
        }
        if (Array.isArray(value)) {
            return value;
        }
        const normalized = typeof value === 'string'
            ? value.trim()
            : '';
        if (!normalized) {
            return this.emptySerializedValue(propertyType);
        }
        switch (propertyType) {
            case CHILL_PROPERTY_TYPE$2.Guid:
                return normalized;
            case CHILL_PROPERTY_TYPE$2.Integer:
                return this.parseIntegerValue(normalized);
            case CHILL_PROPERTY_TYPE$2.Decimal:
                return this.parseDecimalValue(normalized);
            case CHILL_PROPERTY_TYPE$2.Date:
                return this.parseDateValue(normalized);
            case CHILL_PROPERTY_TYPE$2.Time:
                return this.parseTimeValue(normalized);
            case CHILL_PROPERTY_TYPE$2.Duration:
                return normalized;
            case CHILL_PROPERTY_TYPE$2.DateTime:
                return this.parseDateTimeValue(normalized);
            case CHILL_PROPERTY_TYPE$2.Boolean:
                return this.parseBooleanValue(normalized);
            case CHILL_PROPERTY_TYPE$2.String:
            case CHILL_PROPERTY_TYPE$2.Text:
            case CHILL_PROPERTY_TYPE$2.Select:
            case CHILL_PROPERTY_TYPE$2.Json:
                return normalized;
            case CHILL_PROPERTY_TYPE$2.ChillEntity:
            case CHILL_PROPERTY_TYPE$2.ChillQuery:
                return normalized;
            case CHILL_PROPERTY_TYPE$2.ChillEntityCollection:
                return normalized;
            case CHILL_PROPERTY_TYPE$2.Unknown:
            default:
                return normalized;
        }
    }
    emptySerializedValue(propertyType) {
        switch (propertyType) {
            case CHILL_PROPERTY_TYPE$2.String:
            case CHILL_PROPERTY_TYPE$2.Text:
            case CHILL_PROPERTY_TYPE$2.Select:
            case CHILL_PROPERTY_TYPE$2.Json:
            case CHILL_PROPERTY_TYPE$2.Time:
            case CHILL_PROPERTY_TYPE$2.Duration:
            case CHILL_PROPERTY_TYPE$2.Unknown:
                return '';
            default:
                return null;
        }
    }
    parseIntegerValue(value) {
        return this.parseDisplayInteger(value);
    }
    parseDecimalValue(value) {
        return this.parseDisplayDecimal(value);
    }
    parseBooleanValue(value) {
        const normalized = value.toLowerCase();
        if (normalized === 'true' || normalized === '1') {
            return true;
        }
        if (normalized === 'false' || normalized === '0') {
            return false;
        }
        return null;
    }
    parseDateValue(value) {
        return this.parseDisplayDate(value);
    }
    parseTimeValue(value) {
        return this.parseDisplayTime(value);
    }
    parseDateTimeValue(value) {
        return this.parseDisplayDateTime(value);
    }
    resolveCultureName(cultureName) {
        const normalizedCultureName = cultureName?.trim();
        return normalizedCultureName || this.currentCultureName();
    }
    defaultDateFormatForCulture(cultureName) {
        return cultureName.trim().toLowerCase() === 'en-us'
            ? 'MM/dd/yyyy'
            : 'dd/MM/yyyy';
    }
    formatDateParts(year, month, day) {
        return this.currentDateFormat().replace(/yyyy|yy|YYYY|YY|dd|DD|MM/g, (token) => {
            switch (token) {
                case 'yyyy':
                case 'YYYY':
                    return `${year}`.padStart(4, '0');
                case 'yy':
                case 'YY':
                    return `${year % 100}`.padStart(2, '0');
                case 'dd':
                case 'DD':
                    return `${day}`.padStart(2, '0');
                case 'MM':
                    return `${month}`.padStart(2, '0');
                default:
                    return token;
            }
        });
    }
    parseDisplayDateParts(value) {
        const normalizedValue = value.trim();
        const format = this.currentDateFormat();
        const tokenRegex = /(dd|MM|yyyy|yy|DD|YYYY|YY)/g;
        const tokens = format.match(tokenRegex) ?? [];
        if (tokens.length !== 3) {
            return null;
        }
        const separators = format.split(tokenRegex).filter((_, index) => index % 2 === 0);
        const escapedSeparators = separators.map((separator) => separator.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'));
        const capturePattern = tokens
            .map((token, index) => `${escapedSeparators[index] ?? ''}${this.normalizeDateFormatToken(token) === 'yyyy' ? '(\\d{4})' : this.normalizeDateFormatToken(token) === 'yy' ? '(\\d{2})' : '(\\d{1,2})'}`)
            .join('') + (escapedSeparators[separators.length - 1] ?? '');
        const match = normalizedValue.match(new RegExp(`^${capturePattern}$`));
        if (!match) {
            return null;
        }
        const values = match.slice(1);
        const parsedValues = Object.fromEntries(tokens.map((token, index) => [this.normalizeDateFormatToken(token), values[index]]));
        const yearToken = parsedValues['yyyy'] ?? parsedValues['yy'] ?? '';
        const year = (parsedValues['yyyy']
            ? Number(yearToken)
            : 2000 + Number(yearToken));
        const month = Number(parsedValues['MM'] ?? '');
        const day = Number(parsedValues['dd'] ?? '');
        return this.isValidDateParts(year, month, day)
            ? { year, month, day }
            : null;
    }
    normalizeDateFormatToken(token) {
        switch (token) {
            case 'DD':
                return 'dd';
            case 'YYYY':
                return 'yyyy';
            case 'YY':
                return 'yy';
            default:
                return token;
        }
    }
    toIsoDate(year, month, day) {
        return `${year}-${`${month}`.padStart(2, '0')}-${`${day}`.padStart(2, '0')}`;
    }
    isValidDateParts(year, month, day) {
        if (month < 1 || month > 12 || day < 1 || day > 31) {
            return false;
        }
        const candidate = new Date(year, month - 1, day);
        return candidate.getFullYear() === year
            && candidate.getMonth() === month - 1
            && candidate.getDate() === day;
    }
    parseLocalizedNumber(value) {
        const normalizedValue = value.trim();
        if (!normalizedValue) {
            return null;
        }
        const numberFormat = this.readNumberFormatConfig();
        const formatter = numberFormat.kind === 'locale'
            ? new Intl.NumberFormat(numberFormat.locale)
            : null;
        const parts = formatter?.formatToParts(12345.6) ?? [];
        const groupSeparator = numberFormat.kind === 'pattern'
            ? numberFormat.groupSeparator
            : (parts.find((part) => part.type === 'group')?.value ?? ',');
        const decimalSeparator = numberFormat.kind === 'pattern'
            ? numberFormat.decimalSeparator
            : (parts.find((part) => part.type === 'decimal')?.value ?? '.');
        const signParts = formatter?.formatToParts(-1) ?? [];
        const minusSign = signParts.find((part) => part.type === 'minusSign')?.value ?? '-';
        let sanitizedValue = normalizedValue
            .replaceAll(String.fromCharCode(160), '')
            .replaceAll(' ', '');
        if (groupSeparator) {
            sanitizedValue = sanitizedValue.replaceAll(groupSeparator, '');
        }
        sanitizedValue = sanitizedValue
            .replaceAll(decimalSeparator, '.')
            .replaceAll(minusSign, '-');
        const parsedValue = Number(sanitizedValue);
        return Number.isFinite(parsedValue)
            ? parsedValue
            : null;
    }
    readNumberFormatConfig() {
        const configuredFormat = this.currentNumberFormat().trim();
        if (this.isSupportedLocale(configuredFormat)) {
            return {
                kind: 'locale',
                locale: configuredFormat
            };
        }
        return this.parseNumberFormatPattern(configuredFormat);
    }
    isSupportedLocale(value) {
        if (!value) {
            return false;
        }
        try {
            return Intl.NumberFormat.supportedLocalesOf([value]).length > 0;
        }
        catch {
            return false;
        }
    }
    parseNumberFormatPattern(value) {
        const normalizedValue = value.trim();
        const lastDot = normalizedValue.lastIndexOf('.');
        const lastComma = normalizedValue.lastIndexOf(',');
        const decimalIndex = Math.max(lastDot, lastComma);
        const decimalSeparator = decimalIndex >= 0 ? normalizedValue[decimalIndex] : '.';
        const integerPart = decimalIndex >= 0 ? normalizedValue.slice(0, decimalIndex) : normalizedValue;
        const fractionPart = decimalIndex >= 0 ? normalizedValue.slice(decimalIndex + 1).replace(/[^\d]/g, '') : '';
        const groupSeparatorMatch = integerPart.match(/[^\d]/);
        return {
            kind: 'pattern',
            groupSeparator: groupSeparatorMatch?.[0] ?? '',
            decimalSeparator,
            fractionDigits: fractionPart.length
        };
    }
    formatNumberWithPattern(value, pattern) {
        const sign = value < 0 ? '-' : '';
        const absoluteValue = Math.abs(value);
        const fixedValue = pattern.fractionDigits > 0
            ? absoluteValue.toFixed(pattern.fractionDigits)
            : Math.round(absoluteValue).toString();
        const [integerPart, fractionPart = ''] = fixedValue.split('.');
        const groupedInteger = pattern.groupSeparator
            ? integerPart.replace(/\B(?=(\d{3})+(?!\d))/g, pattern.groupSeparator)
            : integerPart;
        const hasNonZeroFraction = fractionPart.replace(/0/g, '').length > 0;
        return pattern.fractionDigits > 0 && hasNonZeroFraction
            ? `${sign}${groupedInteger}${pattern.decimalSeparator}${fractionPart}`
            : `${sign}${groupedInteger}`;
    }
    toZonedIsoDateTime(year, month, day, hour, minute, second, fraction) {
        const baseUtc = Date.UTC(year, month - 1, day, hour, minute, second);
        let candidate = new Date(baseUtc);
        for (let attempt = 0; attempt < 3; attempt += 1) {
            const offsetMinutes = this.readTimeZoneOffsetMinutes(candidate, this.currentTimeZone());
            const nextCandidate = new Date(baseUtc - offsetMinutes * 60_000);
            if (nextCandidate.getTime() === candidate.getTime()) {
                break;
            }
            candidate = nextCandidate;
        }
        if (Number.isNaN(candidate.getTime())) {
            return null;
        }
        const offsetMinutes = this.readTimeZoneOffsetMinutes(candidate, this.currentTimeZone());
        return `${this.toIsoDate(year, month, day)}T${`${hour}`.padStart(2, '0')}:${`${minute}`.padStart(2, '0')}:${`${second}`.padStart(2, '0')}${fraction || ''}${this.formatOffsetMinutes(offsetMinutes)}`;
    }
    normalizeTimeParts(hourText, minuteText, secondText, fractionText) {
        const hour = Number(hourText);
        const minute = Number(minuteText);
        const second = secondText ? Number(secondText) : null;
        if (hour < 0 || hour > 23 || minute < 0 || minute > 59 || (second !== null && (second < 0 || second > 59))) {
            return null;
        }
        const normalizedHour = `${hour}`.padStart(2, '0');
        const normalizedMinute = `${minute}`.padStart(2, '0');
        if (second === null) {
            return `${normalizedHour}:${normalizedMinute}`;
        }
        return `${normalizedHour}:${normalizedMinute}:${`${second}`.padStart(2, '0')}${fractionText ?? ''}`;
    }
    formatOffsetMinutes(offsetMinutes) {
        const sign = offsetMinutes >= 0 ? '+' : '-';
        const absoluteMinutes = Math.abs(offsetMinutes);
        const hours = Math.floor(absoluteMinutes / 60);
        const minutes = absoluteMinutes % 60;
        return `${sign}${`${hours}`.padStart(2, '0')}:${`${minutes}`.padStart(2, '0')}`;
    }
    readZonedDateTimeParts(date, timeZone) {
        const formatter = new Intl.DateTimeFormat('en-CA', {
            timeZone,
            year: 'numeric',
            month: '2-digit',
            day: '2-digit',
            hour: '2-digit',
            minute: '2-digit',
            second: '2-digit',
            hourCycle: 'h23'
        });
        const parts = formatter.formatToParts(date);
        return {
            year: Number(parts.find((part) => part.type === 'year')?.value ?? '0'),
            month: Number(parts.find((part) => part.type === 'month')?.value ?? '0'),
            day: Number(parts.find((part) => part.type === 'day')?.value ?? '0'),
            hour: Number(parts.find((part) => part.type === 'hour')?.value ?? '0'),
            minute: Number(parts.find((part) => part.type === 'minute')?.value ?? '0'),
            second: Number(parts.find((part) => part.type === 'second')?.value ?? '0')
        };
    }
    readTimeZoneOffsetMinutes(date, timeZone) {
        const formatter = new Intl.DateTimeFormat('en-US', {
            timeZone,
            timeZoneName: 'shortOffset',
            hour: '2-digit',
            minute: '2-digit'
        });
        const offsetText = formatter.formatToParts(date).find((part) => part.type === 'timeZoneName')?.value ?? 'GMT';
        if (offsetText === 'GMT' || offsetText === 'UTC') {
            return 0;
        }
        const match = offsetText.match(/GMT([+-])(\d{1,2})(?::?(\d{2}))?/i);
        if (!match) {
            return 0;
        }
        const sign = match[1] === '-' ? -1 : 1;
        const hours = Number(match[2]);
        const minutes = Number(match[3] ?? '0');
        return sign * (hours * 60 + minutes);
    }
    normalizeEntityChangeNotification(change) {
        return {
            chillType: change.chillType?.trim() ?? '',
            guid: change.guid?.trim() ?? '',
            action: change.action
        };
    }
    normalizeAuthUsers(response) {
        return Array.isArray(response)
            ? response.filter((item) => this.isJsonObject(item)).map((item) => this.normalizeAuthUser(item))
            : [];
    }
    buildSetAuthUserRequest(response, overrides, mutateRoleGuids, mutatePermissions) {
        const roleGuids = response.roles
            .map((role) => role.guid?.trim() ?? '')
            .filter((guid) => guid.length > 0);
        const permissions = response.permissions.map((permission) => this.toAuthPermissionRuleItem(permission));
        return {
            guid: response.guid,
            externalId: overrides?.externalId ?? response.externalId,
            userName: overrides?.userName ?? response.userName,
            displayName: overrides?.displayName ?? response.displayName,
            displayCultureName: overrides?.displayCultureName ?? response.displayCultureName,
            displayTimeZone: overrides?.displayTimeZone ?? response.displayTimeZone,
            displayDateFormat: overrides?.displayDateFormat ?? response.displayDateFormat,
            displayNumberFormat: overrides?.displayNumberFormat ?? response.displayNumberFormat,
            isActive: overrides?.isActive ?? response.isActive,
            canManagePermissions: overrides?.canManagePermissions ?? response.canManagePermissions,
            canManageSchema: overrides?.canManageSchema ?? response.canManageSchema,
            menuHierarchy: overrides?.menuHierarchy ?? response.menuHierarchy ?? '',
            roleGuids: mutateRoleGuids ? mutateRoleGuids(roleGuids) : roleGuids,
            permissions: mutatePermissions ? mutatePermissions(permissions) : permissions
        };
    }
    readStoredUserPreferences() {
        const rawPreferences = globalThis.localStorage?.getItem(USER_PREFERENCES_STORAGE_KEY);
        if (!rawPreferences) {
            return this.createEmptyUserPreferences();
        }
        try {
            const parsed = JSON.parse(rawPreferences);
            return {
                displayCultureName: parsed.displayCultureName?.trim() ?? '',
                displayTimeZone: parsed.displayTimeZone?.trim() ?? '',
                displayDateFormat: parsed.displayDateFormat?.trim() ?? '',
                displayNumberFormat: parsed.displayNumberFormat?.trim() ?? ''
            };
        }
        catch {
            globalThis.localStorage?.removeItem(USER_PREFERENCES_STORAGE_KEY);
            return this.createEmptyUserPreferences();
        }
    }
    createEmptyUserPreferences() {
        return {
            displayCultureName: '',
            displayTimeZone: '',
            displayDateFormat: '',
            displayNumberFormat: ''
        };
    }
    persistUserPreferences(preferences) {
        const previousPreferences = this.userPreferencesState();
        const previousCultureName = this.userPreferencesState().displayCultureName.trim().toLowerCase();
        const nextCultureName = preferences.displayCultureName.trim().toLowerCase();
        globalThis.localStorage?.setItem(USER_PREFERENCES_STORAGE_KEY, JSON.stringify(preferences));
        this.userPreferencesState.set(preferences);
        this.logUserPreferencesUpdate(previousPreferences, preferences);
        if (previousCultureName !== nextCultureName) {
            this.textCache.clear();
            this.pendingTextRequests.clear();
            this.inFlightTextRequests.clear();
            this.pendingTextResolvers.clear();
            this.textVersion.update((current) => current + 1);
        }
    }
    toStoredUserPreferences(user) {
        return {
            displayCultureName: this.readJsonString(user, 'DisplayCultureName') ?? user.displayCultureName ?? '',
            displayTimeZone: this.readJsonString(user, 'DisplayTimeZone') ?? user.displayTimeZone ?? '',
            displayDateFormat: this.readJsonString(user, 'DisplayDateFormat') ?? user.displayDateFormat ?? '',
            displayNumberFormat: this.readJsonString(user, 'DisplayNumberFormat') ?? user.displayNumberFormat ?? ''
        };
    }
    async promptForTimeZoneAlignment(userGuid, user) {
        const browserTimeZone = this.readBrowserTimeZone();
        const userTimeZone = (this.readJsonString(user, 'DisplayTimeZone') ?? user.displayTimeZone ?? '').trim();
        if (!browserTimeZone || !userTimeZone || browserTimeZone === userTimeZone || this.isTimeZoneAlignmentPromptOpen) {
            return;
        }
        this.isTimeZoneAlignmentPromptOpen = true;
        try {
            const shouldAlign = await this.dialog.confirmYesNo(this.T('3A9D83B1-B1D0-48A1-B917-340496692645', 'Align time zone', 'Allinea fuso orario'), this.T('B8D2AC57-314D-4B0B-B6C6-ED4D6422163F', `Your browser uses ${browserTimeZone}, but your profile is set to ${userTimeZone}. Do you want to align your profile time zone?`, `Il browser usa ${browserTimeZone}, ma il profilo e impostato su ${userTimeZone}. Vuoi allineare il fuso orario del profilo?`));
            if (!shouldAlign) {
                return;
            }
            const updatedUser = await firstValueFrom(this.updateUserProfile(userGuid, {
                displayName: user.displayName ?? '',
                displayCultureName: user.displayCultureName ?? '',
                displayTimeZone: browserTimeZone,
                displayDateFormat: user.displayDateFormat ?? '',
                displayNumberFormat: user.displayNumberFormat ?? ''
            }));
            this.persistUserPreferences(this.toStoredUserPreferences(updatedUser));
        }
        finally {
            this.isTimeZoneAlignmentPromptOpen = false;
        }
    }
    readBrowserTimeZone() {
        return Intl.DateTimeFormat().resolvedOptions().timeZone || 'UTC';
    }
    isCurrentUser(userGuid) {
        const normalizedUserGuid = userGuid.trim();
        if (!normalizedUserGuid) {
            return false;
        }
        const resolvedCurrentUserGuid = this.currentUserGuidState().trim();
        if (resolvedCurrentUserGuid) {
            return normalizedUserGuid === resolvedCurrentUserGuid;
        }
        return normalizedUserGuid === (this.sessionState()?.userId?.trim() ?? '');
    }
    toRegisterAuthIdentityRequest(request) {
        return {
            userName: request.UserName,
            email: request.Email?.trim() || null,
            password: request.Password,
            displayName: request.DisplayName,
            displayCultureName: request.DisplayCultureName,
            displayTimeZone: request.DisplayTimeZone,
            createChillAuthUser: request.CreateChillAuthUser
        };
    }
    toLoginAuthIdentityRequest(request) {
        return {
            userNameOrEmail: request.UserNameOrEmail,
            password: request.Password
        };
    }
    toRequestPasswordResetRequest(request) {
        return {
            userNameOrEmail: request.UserNameOrEmail
        };
    }
    toResetPasswordRequest(request) {
        return {
            userId: request.UserId,
            resetToken: request.ResetToken,
            newPassword: request.NewPassword
        };
    }
    buildSetAuthRoleRequest(response, overrides, mutateUserGuids, mutatePermissions) {
        const userGuids = response?.users
            .map((user) => user.guid?.trim() ?? '')
            .filter((guid) => guid.length > 0) ?? [];
        const permissions = response?.permissions.map((permission) => this.toAuthPermissionRuleItem(permission)) ?? [];
        return {
            guid: response?.guid ?? null,
            name: overrides?.name ?? response?.name ?? '',
            description: overrides?.description ?? response?.description ?? '',
            isActive: overrides?.isActive ?? response?.isActive ?? true,
            menuHierarchy: response?.menuHierarchy ?? '',
            userGuids: mutateUserGuids ? mutateUserGuids(userGuids) : userGuids,
            permissions: mutatePermissions ? mutatePermissions(permissions) : permissions
        };
    }
    normalizeMenuItem(response) {
        return {
            guid: response.guid?.trim() ?? '',
            positionNo: Number.isFinite(response.positionNo) ? response.positionNo : 0,
            title: response.title?.trim() ?? '',
            description: response.description?.trim() || null,
            parent: response.parent ? {
                guid: response.parent.guid?.trim() ?? '',
                positionNo: Number.isFinite(response.parent.positionNo) ? response.parent.positionNo : 0,
                title: response.parent.title?.trim() ?? '',
                description: response.parent.description?.trim() || null,
                parent: null,
                componentName: response.parent.componentName?.trim() ?? '',
                componentConfigurationJson: response.parent.componentConfigurationJson?.trim() || null,
                menuHierarchy: response.parent.menuHierarchy?.trim() ?? ''
            } : null,
            componentName: response.componentName?.trim() ?? '',
            componentConfigurationJson: response.componentConfigurationJson?.trim() || null,
            menuHierarchy: response.menuHierarchy?.trim() ?? ''
        };
    }
    toMenuDto(menuItem) {
        return {
            guid: menuItem.guid?.trim() ?? '',
            positionNo: Number.isFinite(menuItem.positionNo) ? menuItem.positionNo : 0,
            title: menuItem.title?.trim() ?? '',
            description: menuItem.description?.trim() || null,
            parent: menuItem.parent ? {
                guid: menuItem.parent.guid?.trim() ?? '',
                positionNo: Number.isFinite(menuItem.parent.positionNo) ? menuItem.parent.positionNo : 0,
                title: menuItem.parent.title?.trim() ?? '',
                description: menuItem.parent.description?.trim() || null,
                parent: null,
                componentName: menuItem.parent.componentName?.trim() ?? '',
                componentConfigurationJson: menuItem.parent.componentConfigurationJson?.trim() || null,
                menuHierarchy: menuItem.parent.menuHierarchy?.trim() ?? ''
            } : null,
            componentName: menuItem.componentName?.trim() ?? '',
            componentConfigurationJson: menuItem.componentConfigurationJson?.trim() || null,
            menuHierarchy: menuItem.menuHierarchy?.trim() ?? ''
        };
    }
    normalizeEntityOptions(response) {
        const responseObject = this.isJsonObject(response) ? response : {};
        return {
            chillType: response.chillType?.trim() ?? '',
            checksumEnabled: !!response.checksumEnabled,
            handleAttachments: !!response.handleAttachments,
            labelFormatString: response.labelFormatString?.trim() || null,
            shortLabelFormatString: response.shortLabelFormatString?.trim() || null,
            fullTextContentFormatString: response.fullTextContentFormatString?.trim() || null,
            changeLogEnabled: !!response.changeLogEnabled,
            enableMCP: this.readJsonBoolean(responseObject, 'EnableMCP'),
            mcpDescription: this.readJsonString(responseObject, 'MCPDescription') ?? null
        };
    }
    toEntityOptionsDto(entityOptions) {
        return {
            chillType: entityOptions.chillType?.trim() ?? '',
            checksumEnabled: !!entityOptions.checksumEnabled,
            handleAttachments: !!entityOptions.handleAttachments,
            labelFormatString: entityOptions.labelFormatString?.trim() || null,
            shortLabelFormatString: entityOptions.shortLabelFormatString?.trim() || null,
            fullTextContentFormatString: entityOptions.fullTextContentFormatString?.trim() || null,
            changeLogEnabled: !!entityOptions.changeLogEnabled,
            enableMCP: !!entityOptions.enableMCP,
            mcpDescription: entityOptions.mcpDescription?.trim() || null
        };
    }
    normalizeSchema(response) {
        if (!response || !this.isJsonObject(response)) {
            return null;
        }
        return {
            ...response,
            chillType: this.readJsonString(response, 'ChillType') ?? '',
            chillViewCode: this.readJsonString(response, 'ChillViewCode') ?? '',
            displayName: this.readJsonString(response, 'DisplayName') ?? '',
            handleAttachments: this.readJsonBoolean(response, 'HandleAttachments'),
            enableMCP: this.readJsonBoolean(response, 'EnableMCP'),
            mcpDescription: this.readJsonString(response, 'MCPDescription') ?? null,
            queryRelatedChillType: this.readJsonString(response, 'QueryRelatedChillType') ?? undefined,
            metadata: this.normalizeMetadataRecord(response['metadata'] ?? response['Metadata']),
            relations: this.normalizeSchemaRelations(response['relations'] ?? response['Relations']),
            properties: this.normalizeSchemaProperties(response['properties'] ?? response['Properties'])
        };
    }
    normalizeSchemaRelations(value) {
        if (!Array.isArray(value)) {
            return [];
        }
        return value
            .filter((relation) => this.isJsonObject(relation))
            .map((relation) => ({
            ...relation,
            chillType: this.readJsonString(relation, 'ChillType') ?? '',
            chillQuery: this.readJsonString(relation, 'ChillQuery') ?? '',
            fixedValues: this.normalizeJsonRecord(relation['fixedValues'] ?? relation['FixedValues']),
            fixedQueryValues: this.normalizeJsonRecord(relation['fixedQueryValues'] ?? relation['FixedQueryValues']),
            relationLabel: this.normalizeSchemaRelationLabel(relation['relationLabel'] ?? relation['RelationLabel'])
        }));
    }
    normalizeSchemaRelationLabel(value) {
        if (!this.isJsonObject(value)) {
            return null;
        }
        return {
            labelGuid: this.readJsonString(value, 'LabelGuid') ?? null,
            primaryDefaultText: this.readJsonString(value, 'PrimaryDefaultText') ?? '',
            secondaryDefaultText: this.readJsonString(value, 'SecondaryDefaultText') ?? ''
        };
    }
    normalizeSchemaProperties(value) {
        if (!Array.isArray(value)) {
            return [];
        }
        return value
            .filter((property) => this.isJsonObject(property))
            .map((property) => ({
            ...property,
            name: this.readJsonString(property, 'Name') ?? '',
            displayName: this.readJsonString(property, 'DisplayName') ?? property.name ?? '',
            propertyType: this.readJsonNumber(property, 'PropertyType') ?? property.propertyType ?? CHILL_PROPERTY_TYPE$2.Unknown,
            simplePropertyType: this.readJsonString(property, 'SimplePropertyType') ?? property.simplePropertyType ?? '',
            mcpDescription: this.readJsonString(property, 'MCPDescription') ?? property.mcpDescription ?? '',
            isNullable: this.readJsonBoolean(property, 'IsNullable'),
            isReadOnly: this.readJsonBoolean(property, 'IsReadOnly'),
            chillType: this.readJsonString(property, 'ChillType') ?? property.chillType ?? null,
            referenceChillType: this.readJsonString(property, 'ReferenceChillType') ?? property.referenceChillType ?? null,
            referenceChillTypeQuery: this.readJsonString(property, 'ReferenceChillTypeQuery') ?? property.referenceChillTypeQuery ?? null,
            metadata: this.normalizeMetadataRecord(property['metadata'] ?? property['Metadata'])
        }));
    }
    toSchemaRelationDtos(relations) {
        if (!Array.isArray(relations)) {
            return [];
        }
        return relations.map((relation) => ({
            chillType: relation.chillType?.trim() ?? '',
            chillQuery: relation.chillQuery?.trim() ?? '',
            fixedValues: this.serializeStringRecord(relation.fixedValues),
            fixedQueryValues: this.serializeStringRecord(relation.fixedQueryValues),
            relationLabel: {
                labelGuid: relation.relationLabel?.labelGuid?.trim() ?? null,
                primaryDefaultText: relation.relationLabel?.primaryDefaultText?.trim() ?? '',
                secondaryDefaultText: relation.relationLabel?.secondaryDefaultText?.trim() ?? ''
            }
        }));
    }
    normalizeMetadataRecord(value) {
        if (!this.isJsonObject(value)) {
            return {};
        }
        return Object.fromEntries(Object.entries(value).map(([key, entryValue]) => {
            if (typeof entryValue === 'string') {
                return [key, entryValue];
            }
            if (typeof entryValue === 'number' || typeof entryValue === 'boolean') {
                return [key, String(entryValue)];
            }
            if (entryValue === null) {
                return [key, ''];
            }
            return [key, entryValue];
        }));
    }
    normalizeJsonRecord(value) {
        if (!this.isJsonObject(value)) {
            return {};
        }
        return Object.fromEntries(Object.entries(value).map(([key, entryValue]) => [key, entryValue]));
    }
    toAuthPermissionRuleItem(response) {
        const source = response;
        return {
            guid: this.readJsonString(source, 'Guid') ?? null,
            effect: this.readPermissionEffect(source),
            action: this.readPermissionAction(source),
            scope: this.readPermissionScope(source),
            module: this.readJsonString(source, 'Module') ?? '',
            entityName: this.readJsonString(source, 'EntityName') ?? null,
            propertyName: this.readJsonString(source, 'PropertyName') ?? null,
            appliesToAllProperties: this.readJsonBoolean(source, 'AppliesToAllProperties'),
            description: this.readJsonString(source, 'Description') ?? ''
        };
    }
    getLatestAuthPermissionRule(response, owner) {
        const rules = this.normalizeAuthPermissionRules(response, owner);
        if (rules.length === 0) {
            throw new Error('Auth permission rule was not returned by the server.');
        }
        return rules[rules.length - 1];
    }
    normalizeGuidList(values) {
        return [...new Set(values
                .map((value) => value.trim())
                .filter((value) => value.length > 0))];
    }
    normalizeStringList(values) {
        return Array.isArray(values)
            ? [...new Set(values
                    .filter((value) => typeof value === 'string')
                    .map((value) => value.trim())
                    .filter((value) => value.length > 0))]
            : [];
    }
    normalizeAuthUser(response) {
        return {
            guid: this.readJsonString(response, 'Guid') ?? '',
            externalId: this.readJsonString(response, 'ExternalId') ?? '',
            userName: this.readJsonString(response, 'UserName') ?? '',
            displayName: this.readJsonString(response, 'DisplayName') ?? '',
            displayCultureName: this.readJsonString(response, 'DisplayCultureName') ?? '',
            displayTimeZone: this.readJsonString(response, 'DisplayTimeZone') ?? '',
            displayDateFormat: this.readJsonString(response, 'DisplayDateFormat') ?? '',
            displayNumberFormat: this.readJsonString(response, 'DisplayNumberFormat') ?? '',
            isActive: this.readJsonBoolean(response, 'IsActive'),
            canManagePermissions: this.readJsonBoolean(response, 'CanManagePermissions'),
            canManageSchema: this.readJsonBoolean(response, 'CanManageSchema'),
            menuHierarchy: this.readJsonString(response, 'MenuHierarchy') ?? ''
        };
    }
    normalizeAuthRoles(response) {
        return Array.isArray(response)
            ? response.filter((item) => this.isJsonObject(item)).map((item) => this.normalizeAuthRole(item))
            : [];
    }
    normalizeAuthRole(response) {
        return {
            guid: this.readJsonString(response, 'Guid') ?? '',
            name: this.readJsonString(response, 'Name') ?? '',
            description: this.readJsonString(response, 'Description') ?? '',
            isActive: this.readJsonBoolean(response, 'IsActive')
        };
    }
    normalizeAuthPermissionRules(response, owner) {
        return Array.isArray(response)
            ? response
                .filter((item) => this.isJsonObject(item))
                .map((item) => this.normalizeAuthPermissionRule(item, owner))
            : [];
    }
    normalizeAuthPermissionRule(response, owner) {
        const rule = {
            guid: this.readJsonString(response, 'Guid') ?? '',
            userGuid: this.readJsonString(response, 'UserGuid') ?? (owner?.kind === 'user' ? owner.guid : ''),
            roleGuid: this.readJsonString(response, 'RoleGuid') ?? (owner?.kind === 'role' ? owner.guid : ''),
            effect: this.readPermissionEffect(response),
            action: this.readPermissionAction(response),
            scope: this.readPermissionScope(response),
            module: this.readJsonString(response, 'Module') ?? '',
            entityName: this.readJsonString(response, 'EntityName') ?? '',
            propertyName: this.readJsonString(response, 'PropertyName') ?? '',
            appliesToAllProperties: this.readJsonBoolean(response, 'AppliesToAllProperties'),
            description: this.readJsonString(response, 'Description') ?? '',
            createdUtc: this.readJsonString(response, 'CreatedUtc') ?? ''
        };
        const ownerKind = owner?.kind ?? (rule.userGuid ? 'user' : (rule.roleGuid ? 'role' : null));
        const ownerGuid = owner?.guid ?? rule.userGuid ?? rule.roleGuid ?? '';
        if (rule.guid && ownerKind && ownerGuid) {
            this.permissionRuleOwners.set(rule.guid, {
                kind: ownerKind,
                guid: ownerGuid
            });
        }
        return rule;
    }
    enqueueTextRequest(key, primaryDefaultText, secondaryDefaultText, fallbackText) {
        if (this.textCache.has(key) || this.pendingTextRequests.has(key) || this.inFlightTextRequests.has(key)) {
            return;
        }
        this.pendingTextRequests.set(key, {
            request: {
                labelGuid: key,
                cultureName: this.currentCultureName(),
                primaryCultureName: CHILL_PRIMARY_TEXT_CULTURE,
                primaryDefaultText: primaryDefaultText ?? '',
                secondaryCultureName: CHILL_SECONDARY_TEXT_CULTURE,
                secondaryDefaultText: secondaryDefaultText ?? ''
            },
            fallbackText
        });
        this.scheduleTextQueueFlush();
    }
    scheduleTextQueueFlush() {
        if (this.textQueueHandle !== null) {
            return;
        }
        this.textQueueHandle = globalThis.setTimeout(() => {
            this.textQueueHandle = null;
            void this.flushTextQueue();
        }, TEXT_QUEUE_DELAY_MS);
    }
    async flushTextQueue() {
        const entries = Array.from(this.pendingTextRequests.entries());
        if (entries.length === 0) {
            return;
        }
        this.pendingTextRequests.clear();
        entries.forEach(([key]) => this.inFlightTextRequests.add(key));
        try {
            const responses = await firstValueFrom(this.chill.getTexts(entries.map(([, entry]) => entry.request)));
            entries.forEach(([key, entry], index) => {
                const translatedText = this.readTextResponseValue(responses[index], entry.fallbackText);
                this.textCache.set(key, translatedText);
                this.inFlightTextRequests.delete(key);
                this.resolvePendingTextRequest(key, translatedText);
            });
        }
        catch (error) {
            this.logDetailedError('getTexts()', error);
            entries.forEach(([key, entry]) => {
                this.textCache.set(key, entry.fallbackText);
                this.inFlightTextRequests.delete(key);
                this.resolvePendingTextRequest(key, entry.fallbackText);
            });
        }
        this.textVersion.update((value) => value + 1);
    }
    resolvePendingTextRequest(key, value) {
        const resolvers = this.pendingTextResolvers.get(key);
        if (!resolvers) {
            return;
        }
        this.pendingTextResolvers.delete(key);
        resolvers.forEach((resolve) => resolve(value));
    }
    readTextResponseValue(response, fallbackText) {
        const value = response?.['Value'];
        return typeof value === 'string' && value.trim() ? value.trim() : fallbackText;
    }
    normalizeLabelGuid(labelGuid) {
        return labelGuid.trim().toUpperCase();
    }
    selectDefaultText(primaryDefaultText, secondaryDefaultText) {
        const primaryText = primaryDefaultText.trim();
        const secondaryText = secondaryDefaultText.trim();
        const cultureName = this.currentCultureName();
        if (this.culturesMatch(cultureName, CHILL_SECONDARY_TEXT_CULTURE) && secondaryText) {
            return secondaryText;
        }
        if (this.culturesMatch(cultureName, CHILL_PRIMARY_TEXT_CULTURE) && primaryText) {
            return primaryText;
        }
        return primaryText || secondaryText;
    }
    culturesMatch(left, right) {
        return left.trim().toLowerCase() === right.trim().toLowerCase();
    }
    logUserPreferencesUpdate(previous, next) {
        console.log('[ChillService] User preferences updated', {
            previous,
            next,
            changed: {
                displayCultureName: previous.displayCultureName !== next.displayCultureName,
                displayTimeZone: previous.displayTimeZone !== next.displayTimeZone,
                displayDateFormat: previous.displayDateFormat !== next.displayDateFormat,
                displayNumberFormat: previous.displayNumberFormat !== next.displayNumberFormat
            }
        });
    }
    logStartupDiagnostics() {
        console.log('[ChillService] Startup', {
            baseUrl: CHILL_BASE_URL,
            culture: this.currentCultureName(),
            hasStoredSession: this.sessionState() !== null
        });
        try {
            const version = this.version();
            if (version === this.T('1EB1A234-D374-48B1-9E14-C9A7BAE1C31D', 'Client version is unavailable on the current ChillSharp instance.', 'La versione del client non è disponibile nell\'istanza corrente di ChillSharp.')) {
                console.warn('[ChillService] Client version unavailable', {
                    reason: version
                });
            }
            else {
                console.log('[ChillService] Client version', version);
            }
        }
        catch (error) {
            this.logDetailedError('version()', error);
        }
        this.test().subscribe({
            next: (response) => {
                console.log('[ChillService] test() success', {
                    response: response.trim()
                });
            },
            error: (error) => {
                this.logDetailedError('test()', error);
            }
        });
    }
    buildApiUrl(relativeUrl) {
        const normalizedBaseUrl = CHILL_BASE_URL.trim().replace(/\/+$/, '');
        const chillSuffix = '/chill';
        const apiBaseUrl = normalizedBaseUrl.toLowerCase().endsWith(chillSuffix)
            ? normalizedBaseUrl.slice(0, -chillSuffix.length)
            : normalizedBaseUrl;
        return `${apiBaseUrl}/${relativeUrl.replace(/^\/+/, '')}`;
    }
    serializeMetadataRecord(metadata) {
        if (!metadata) {
            return {};
        }
        return Object.fromEntries(Object.entries(metadata).flatMap(([key, value]) => {
            if (value === undefined) {
                return [];
            }
            if (typeof value === 'string') {
                return [[key, value]];
            }
            if (typeof value === 'number' || typeof value === 'boolean') {
                return [[key, String(value)]];
            }
            if (value === null) {
                return [[key, '']];
            }
            try {
                return [[key, JSON.stringify(value)]];
            }
            catch {
                return [[key, '']];
            }
        }));
    }
    serializeStringRecord(record) {
        if (!record) {
            return {};
        }
        return Object.fromEntries(Object.entries(record).flatMap(([key, value]) => {
            if (value === undefined) {
                return [];
            }
            if (typeof value === 'string') {
                return [[key, value]];
            }
            if (typeof value === 'number' || typeof value === 'boolean') {
                return [[key, String(value)]];
            }
            if (value === null) {
                return [[key, '']];
            }
            try {
                return [[key, JSON.stringify(value)]];
            }
            catch {
                return [[key, '']];
            }
        }));
    }
    rethrowFriendlyError(error) {
        if (error instanceof ChillSharpClientError) {
            const message = this.readChillErrorMessage(error);
            return throwError(() => new Error(message));
        }
        return throwError(() => error);
    }
    isNotFoundError(error) {
        if (error instanceof ChillSharpClientError) {
            return error.statusCode === 404;
        }
        if (error instanceof Error) {
            return error.message.trim().toLowerCase() === 'not found';
        }
        return false;
    }
    readChillErrorMessage(error) {
        const responseText = error.responseText?.trim();
        if (!responseText) {
            return error.message;
        }
        try {
            const parsed = JSON.parse(responseText);
            if (parsed.detail?.trim()) {
                return parsed.detail.trim();
            }
            const validationErrors = Object.values(parsed.errors ?? {})
                .flat()
                .filter((message) => message.trim().length > 0);
            if (validationErrors.length > 0) {
                return validationErrors.join(' ');
            }
            if (parsed.title?.trim()) {
                return parsed.title.trim();
            }
        }
        catch {
            return responseText;
        }
        return responseText;
    }
    logDetailedError(context, error) {
        if (error instanceof ChillSharpClientError) {
            const clientError = error;
            console.error(`[ChillService] ${context} failed`, {
                name: clientError.name,
                message: clientError.message,
                statusCode: clientError.statusCode,
                responseText: clientError.responseText,
                cause: clientError.cause,
                stack: clientError.stack
            });
            return;
        }
        if (error instanceof Error) {
            console.error(`[ChillService] ${context} failed`, {
                name: error.name,
                message: error.message,
                cause: error.cause,
                stack: error.stack
            });
            return;
        }
        console.error(`[ChillService] ${context} failed`, error);
    }
    static { this.ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: ChillService, deps: [], target: i0.ɵɵFactoryTarget.Injectable }); }
    static { this.ɵprov = i0.ɵɵngDeclareInjectable({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: ChillService, providedIn: 'root' }); }
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: ChillService, decorators: [{
            type: Injectable,
            args: [{
                    providedIn: 'root'
                }]
        }], ctorParameters: () => [] });

class WorkspaceLayoutService {
    constructor() {
        this.layoutEditingEnabledState = signal(this.readStoredLayoutEditingState());
        this.isLayoutEditingEnabled = this.layoutEditingEnabledState.asReadonly();
        effect(() => {
            globalThis.localStorage?.setItem(WORKSPACE_LAYOUT_EDITING_STORAGE_KEY, this.layoutEditingEnabledState() ? 'true' : 'false');
        });
    }
    toggleLayoutEditingEnabled() {
        this.layoutEditingEnabledState.update((enabled) => !enabled);
    }
    readStoredLayoutEditingState() {
        return globalThis.localStorage?.getItem(WORKSPACE_LAYOUT_EDITING_STORAGE_KEY)?.trim().toLowerCase() === 'true';
    }
    static { this.ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: WorkspaceLayoutService, deps: [], target: i0.ɵɵFactoryTarget.Injectable }); }
    static { this.ɵprov = i0.ɵɵngDeclareInjectable({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: WorkspaceLayoutService, providedIn: 'root' }); }
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: WorkspaceLayoutService, decorators: [{
            type: Injectable,
            args: [{
                    providedIn: 'root'
                }]
        }], ctorParameters: () => [] });

class ChillI18nLabelComponent {
    constructor() {
        this.chill = inject(ChillService);
        this.layout = inject(WorkspaceLayoutService);
        this.labelGuid = input.required();
        this.primaryDefaultText = input.required();
        this.secondaryDefaultText = input.required();
        this.editable = input(true);
        this.isEditing = signal(false);
        this.isSaving = signal(false);
        this.draftText = signal('');
        this.inputWidth = signal(`min(100%, ${ChillI18nLabelComponent.MIN_INPUT_WIDTH_CH}ch)`);
        this.errorMessage = signal('');
        this.text = computed(() => this.chill.T(this.labelGuid(), this.primaryDefaultText(), this.secondaryDefaultText()));
        this.editEnabled = computed(() => this.editable() && this.chill.isAuthenticated() && this.layout.isLayoutEditingEnabled());
        this.canSave = computed(() => this.draftText().trim().length > 0 && this.draftText().trim() !== this.text().trim());
        this.editAriaLabel = computed(() => this.chill.T('F7FD0AA5-3E6E-491C-857A-5C6C5C7119D5', 'Edit label', 'Modifica etichetta'));
        this.saveAriaLabel = computed(() => this.chill.T('1A77383B-6A11-489A-B527-0CD15A9DBE84', 'Save label', 'Salva etichetta'));
        this.cancelAriaLabel = computed(() => this.chill.T('D51111F2-5230-416E-9957-4E91D6F7C527', 'Cancel label edit', 'Annulla modifica etichetta'));
    }
    static { this.MIN_INPUT_WIDTH_CH = 24; }
    startEditing() {
        if (!this.editEnabled()) {
            return;
        }
        const text = this.text();
        this.draftText.set(text);
        this.inputWidth.set(this.buildInputWidth(text));
        this.errorMessage.set('');
        this.isEditing.set(true);
    }
    cancel() {
        if (this.isSaving()) {
            return;
        }
        this.isEditing.set(false);
        this.errorMessage.set('');
        this.draftText.set(this.text());
    }
    save() {
        if (!this.canSave() || this.isSaving()) {
            return;
        }
        this.isSaving.set(true);
        this.errorMessage.set('');
        this.chill.setText(this.labelGuid(), this.draftText().trim()).subscribe({
            next: (value) => {
                this.draftText.set(value);
                this.isSaving.set(false);
                this.isEditing.set(false);
            },
            error: (error) => {
                this.errorMessage.set(this.chill.formatError(error));
                this.isSaving.set(false);
            }
        });
    }
    buildInputWidth(text) {
        const widthCh = Math.max(text.trim().length + 2, ChillI18nLabelComponent.MIN_INPUT_WIDTH_CH);
        return `min(100%, ${widthCh}ch)`;
        //return `min(${widthCh}ch)`;
    }
    static { this.ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: ChillI18nLabelComponent, deps: [], target: i0.ɵɵFactoryTarget.Component }); }
    static { this.ɵcmp = i0.ɵɵngDeclareComponent({ minVersion: "17.0.0", version: "19.2.21", type: ChillI18nLabelComponent, isStandalone: true, selector: "app-chill-i18n-label", inputs: { labelGuid: { classPropertyName: "labelGuid", publicName: "labelGuid", isSignal: true, isRequired: true, transformFunction: null }, primaryDefaultText: { classPropertyName: "primaryDefaultText", publicName: "primaryDefaultText", isSignal: true, isRequired: true, transformFunction: null }, secondaryDefaultText: { classPropertyName: "secondaryDefaultText", publicName: "secondaryDefaultText", isSignal: true, isRequired: true, transformFunction: null }, editable: { classPropertyName: "editable", publicName: "editable", isSignal: true, isRequired: false, transformFunction: null } }, ngImport: i0, template: `
    <span class="i18n-label" [class.edit-enabled]="editEnabled()">
      @if (isEditing()) {
        <input
          type="text"
          class="i18n-label__input"
          [style.width]="inputWidth()"
          [ngModel]="draftText()"
          (ngModelChange)="draftText.set($event)"
          [disabled]="isSaving()"
          [attr.aria-label]="editAriaLabel()"
          (keydown.enter)="save()"
          (keydown.escape)="cancel()" />

        <button
          type="button"
          class="i18n-label__action"
          [class.confirm]="canSave()"
          [class.cancel]="!canSave()"
          (click)="canSave() ? save() : cancel()"
          [disabled]="isSaving()"
          [attr.aria-label]="canSave() ? saveAriaLabel() : cancelAriaLabel()">
          <span class="material-symbol-icon">{{ canSave() ? 'check' : 'close' }}</span>
        </button>
      } @else {
        <span class="i18n-label__text">{{ text() }}</span>

        @if (editEnabled()) {
          <button
            type="button"
            class="i18n-label__action edit"
            (click)="startEditing()"
            [attr.aria-label]="editAriaLabel()">
            <span class="material-symbol-icon">edit</span>
          </button>
        }
      }
    </span>

    @if (errorMessage()) {
      <small class="i18n-label__error">{{ errorMessage() }}</small>
    }
  `, isInline: true, styles: [":host{display:inline-grid;gap:.25rem}.i18n-label{display:inline-flex;align-items:center;gap:.35rem;min-width:0}.i18n-label__text{min-width:0}.i18n-label__input{min-width:0;width:min(24rem,100%);padding:.15rem .35rem;border:1px solid var(--border-color);border-radius:.45rem;background:var(--surface-0);color:var(--text-main);font:inherit}.i18n-label__action{width:1.8rem;height:1.8rem;display:inline-grid;place-items:center;padding:0;border:1px solid var(--border-color);border-radius:.45rem;background:var(--surface-0);color:var(--text-main);cursor:pointer;font:inherit;line-height:1}.i18n-label__action.confirm{color:var(--success)}.i18n-label__action.cancel{color:var(--text-muted)}.i18n-label__action:disabled{cursor:progress;opacity:.6}.i18n-label__action .material-symbol-icon{font-size:1rem}.i18n-label__error{color:var(--danger);max-width:100%;max-height:8rem;overflow-x:hidden;overflow-y:auto;overflow-wrap:anywhere;word-break:break-word}:root[data-theme=dark] .i18n-label__input,:root[data-theme=dark] .i18n-label__action{background:#09131a94}\n"], dependencies: [{ kind: "ngmodule", type: CommonModule }, { kind: "ngmodule", type: FormsModule }, { kind: "directive", type: i1.DefaultValueAccessor, selector: "input:not([type=checkbox])[formControlName],textarea[formControlName],input:not([type=checkbox])[formControl],textarea[formControl],input:not([type=checkbox])[ngModel],textarea[ngModel],[ngDefaultControl]" }, { kind: "directive", type: i1.NgControlStatus, selector: "[formControlName],[ngModel],[formControl]" }, { kind: "directive", type: i1.NgModel, selector: "[ngModel]:not([formControlName]):not([formControl])", inputs: ["name", "disabled", "ngModel", "ngModelOptions"], outputs: ["ngModelChange"], exportAs: ["ngModel"] }] }); }
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: ChillI18nLabelComponent, decorators: [{
            type: Component,
            args: [{ selector: 'app-chill-i18n-label', standalone: true, imports: [CommonModule, FormsModule], template: `
    <span class="i18n-label" [class.edit-enabled]="editEnabled()">
      @if (isEditing()) {
        <input
          type="text"
          class="i18n-label__input"
          [style.width]="inputWidth()"
          [ngModel]="draftText()"
          (ngModelChange)="draftText.set($event)"
          [disabled]="isSaving()"
          [attr.aria-label]="editAriaLabel()"
          (keydown.enter)="save()"
          (keydown.escape)="cancel()" />

        <button
          type="button"
          class="i18n-label__action"
          [class.confirm]="canSave()"
          [class.cancel]="!canSave()"
          (click)="canSave() ? save() : cancel()"
          [disabled]="isSaving()"
          [attr.aria-label]="canSave() ? saveAriaLabel() : cancelAriaLabel()">
          <span class="material-symbol-icon">{{ canSave() ? 'check' : 'close' }}</span>
        </button>
      } @else {
        <span class="i18n-label__text">{{ text() }}</span>

        @if (editEnabled()) {
          <button
            type="button"
            class="i18n-label__action edit"
            (click)="startEditing()"
            [attr.aria-label]="editAriaLabel()">
            <span class="material-symbol-icon">edit</span>
          </button>
        }
      }
    </span>

    @if (errorMessage()) {
      <small class="i18n-label__error">{{ errorMessage() }}</small>
    }
  `, styles: [":host{display:inline-grid;gap:.25rem}.i18n-label{display:inline-flex;align-items:center;gap:.35rem;min-width:0}.i18n-label__text{min-width:0}.i18n-label__input{min-width:0;width:min(24rem,100%);padding:.15rem .35rem;border:1px solid var(--border-color);border-radius:.45rem;background:var(--surface-0);color:var(--text-main);font:inherit}.i18n-label__action{width:1.8rem;height:1.8rem;display:inline-grid;place-items:center;padding:0;border:1px solid var(--border-color);border-radius:.45rem;background:var(--surface-0);color:var(--text-main);cursor:pointer;font:inherit;line-height:1}.i18n-label__action.confirm{color:var(--success)}.i18n-label__action.cancel{color:var(--text-muted)}.i18n-label__action:disabled{cursor:progress;opacity:.6}.i18n-label__action .material-symbol-icon{font-size:1rem}.i18n-label__error{color:var(--danger);max-width:100%;max-height:8rem;overflow-x:hidden;overflow-y:auto;overflow-wrap:anywhere;word-break:break-word}:root[data-theme=dark] .i18n-label__input,:root[data-theme=dark] .i18n-label__action{background:#09131a94}\n"] }]
        }] });

class AuthShellComponent {
    constructor() {
        this.chill = inject(ChillService);
    }
    static { this.ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: AuthShellComponent, deps: [], target: i0.ɵɵFactoryTarget.Component }); }
    static { this.ɵcmp = i0.ɵɵngDeclareComponent({ minVersion: "14.0.0", version: "19.2.21", type: AuthShellComponent, isStandalone: true, selector: "app-auth-shell", ngImport: i0, template: `
    <section class="auth-layout">
      <div class="auth-layout__hero">
        <p class="auth-layout__eyebrow">
          <app-chill-i18n-label
            [labelGuid]="'D8E9F1A4-3E8A-47A7-BE9C-1C702F81C6B0'"
            [primaryDefaultText]="'ChillSharp UI'"
            [secondaryDefaultText]="'ChillSharp UI'" />
        </p>
        <h1>
          <app-chill-i18n-label
            [labelGuid]="'38C43178-A697-4DE7-BA27-1989DD0E9B69'"
            [primaryDefaultText]="'Identity that stays out of the way.'"
            [secondaryDefaultText]="'Identita che resta fuori dal percorso.'" />
        </h1>
        <p>
          <app-chill-i18n-label
            [labelGuid]="'9B329C57-797A-43DF-930B-583C44A21D57'"
            [primaryDefaultText]="'Separate auth pages now lead into a dedicated workspace built to host user tasks without mixing concerns.'"
            [secondaryDefaultText]="'Le pagine di autenticazione ora conducono a un workspace dedicato, pensato per ospitare le attivita utente senza mescolare le responsabilita.'" />
        </p>
      </div>

      <div class="auth-layout__content">
        <router-outlet />
      </div>
    </section>
  `, isInline: true, dependencies: [{ kind: "directive", type: RouterOutlet, selector: "router-outlet", inputs: ["name", "routerOutletData"], outputs: ["activate", "deactivate", "attach", "detach"], exportAs: ["outlet"] }, { kind: "component", type: ChillI18nLabelComponent, selector: "app-chill-i18n-label", inputs: ["labelGuid", "primaryDefaultText", "secondaryDefaultText", "editable"] }] }); }
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: AuthShellComponent, decorators: [{
            type: Component,
            args: [{
                    selector: 'app-auth-shell',
                    standalone: true,
                    imports: [RouterOutlet, ChillI18nLabelComponent],
                    template: `
    <section class="auth-layout">
      <div class="auth-layout__hero">
        <p class="auth-layout__eyebrow">
          <app-chill-i18n-label
            [labelGuid]="'D8E9F1A4-3E8A-47A7-BE9C-1C702F81C6B0'"
            [primaryDefaultText]="'ChillSharp UI'"
            [secondaryDefaultText]="'ChillSharp UI'" />
        </p>
        <h1>
          <app-chill-i18n-label
            [labelGuid]="'38C43178-A697-4DE7-BA27-1989DD0E9B69'"
            [primaryDefaultText]="'Identity that stays out of the way.'"
            [secondaryDefaultText]="'Identita che resta fuori dal percorso.'" />
        </h1>
        <p>
          <app-chill-i18n-label
            [labelGuid]="'9B329C57-797A-43DF-930B-583C44A21D57'"
            [primaryDefaultText]="'Separate auth pages now lead into a dedicated workspace built to host user tasks without mixing concerns.'"
            [secondaryDefaultText]="'Le pagine di autenticazione ora conducono a un workspace dedicato, pensato per ospitare le attivita utente senza mescolare le responsabilita.'" />
        </p>
      </div>

      <div class="auth-layout__content">
        <router-outlet />
      </div>
    </section>
  `
                }]
        }] });

class ChillI18nButtonLabelComponent {
    constructor() {
        this.chill = inject(ChillService);
        this.layout = inject(WorkspaceLayoutService);
        this.labelGuid = input.required();
        this.primaryDefaultText = input.required();
        this.secondaryDefaultText = input.required();
        this.editable = input(true);
        this.isEditing = signal(false);
        this.isSaving = signal(false);
        this.draftText = signal('');
        this.errorMessage = signal('');
        this.text = computed(() => this.chill.T(this.labelGuid(), this.primaryDefaultText(), this.secondaryDefaultText()));
        this.editEnabled = computed(() => this.editable() && this.chill.isAuthenticated() && this.layout.isLayoutEditingEnabled());
        this.canSave = computed(() => this.draftText().trim().length > 0 && this.draftText().trim() !== this.text().trim());
        this.editAriaLabel = computed(() => this.chill.T('344781FB-FD8F-4127-A599-4EC92E466B19', 'Edit button label', 'Modifica etichetta pulsante'));
        this.saveAriaLabel = computed(() => this.chill.T('3B2D7D1E-AEA2-4412-9AE3-228BB2252D49', 'Save button label', 'Salva etichetta pulsante'));
    }
    startEditing(event) {
        this.swallow(event);
        if (!this.editEnabled()) {
            return;
        }
        this.draftText.set(this.text());
        this.errorMessage.set('');
        this.isEditing.set(true);
    }
    cancel(event) {
        this.swallow(event);
        if (this.isSaving()) {
            return;
        }
        this.isEditing.set(false);
        this.errorMessage.set('');
        this.draftText.set(this.text());
    }
    save(event) {
        this.swallow(event);
        if (!this.canSave() || this.isSaving()) {
            return;
        }
        this.isSaving.set(true);
        this.errorMessage.set('');
        this.chill.setText(this.labelGuid(), this.draftText().trim()).subscribe({
            next: (value) => {
                this.draftText.set(value);
                this.isSaving.set(false);
                this.isEditing.set(false);
            },
            error: (error) => {
                this.errorMessage.set(this.chill.formatError(error));
                this.isSaving.set(false);
            }
        });
    }
    swallow(event) {
        event?.preventDefault();
        event?.stopPropagation();
    }
    static { this.ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: ChillI18nButtonLabelComponent, deps: [], target: i0.ɵɵFactoryTarget.Component }); }
    static { this.ɵcmp = i0.ɵɵngDeclareComponent({ minVersion: "17.0.0", version: "19.2.21", type: ChillI18nButtonLabelComponent, isStandalone: true, selector: "app-chill-i18n-button-label", inputs: { labelGuid: { classPropertyName: "labelGuid", publicName: "labelGuid", isSignal: true, isRequired: true, transformFunction: null }, primaryDefaultText: { classPropertyName: "primaryDefaultText", publicName: "primaryDefaultText", isSignal: true, isRequired: true, transformFunction: null }, secondaryDefaultText: { classPropertyName: "secondaryDefaultText", publicName: "secondaryDefaultText", isSignal: true, isRequired: true, transformFunction: null }, editable: { classPropertyName: "editable", publicName: "editable", isSignal: true, isRequired: false, transformFunction: null } }, ngImport: i0, template: `
    <span class="i18n-button-label">
      @if (isEditing()) {
        <input
          type="text"
          class="i18n-button-label__input"
          [ngModel]="draftText()"
          (ngModelChange)="draftText.set($event)"
          [disabled]="isSaving()"
          [attr.aria-label]="editAriaLabel()"
          (click)="swallow($event)"
          (mousedown)="swallow($event)"
          (keydown.enter)="save($event)"
          (keydown.escape)="cancel($event)" />

        <span
          class="i18n-button-label__action confirm"
          role="button"
          tabindex="0"
          [attr.aria-disabled]="isSaving() || !canSave()"
          [attr.aria-label]="saveAriaLabel()"
          (click)="save($event)"
          (mousedown)="swallow($event)"
          (keydown.enter)="save($event)"
          (keydown.space)="save($event)">
          ✓
        </span>
      } @else {
        <span class="i18n-button-label__text">{{ text() }}</span>

        @if (editEnabled()) {
          <span
            class="i18n-button-label__action edit"
            role="button"
            tabindex="0"
            [attr.aria-label]="editAriaLabel()"
            (click)="startEditing($event)"
            (mousedown)="swallow($event)"
            (keydown.enter)="startEditing($event)"
            (keydown.space)="startEditing($event)">
            ✎
          </span>
        }
      }
    </span>

    @if (errorMessage()) {
      <small class="i18n-button-label__error">{{ errorMessage() }}</small>
    }
  `, isInline: true, styles: [":host{display:inline-grid;gap:.2rem;max-width:100%}.i18n-button-label{display:inline-flex;align-items:center;justify-content:center;gap:.35rem;max-width:100%}.i18n-button-label__text{min-width:0}.i18n-button-label__input{min-width:0;width:min(18rem,100%);padding:.1rem .3rem;border:1px solid color-mix(in srgb,currentColor 28%,var(--border-color));border-radius:.4rem;background:var(--surface-0);color:var(--text-main);font:inherit}.i18n-button-label__action{width:1.35rem;height:1.35rem;display:inline-grid;place-items:center;border:1px solid color-mix(in srgb,currentColor 28%,var(--border-color));border-radius:.35rem;background:color-mix(in srgb,var(--surface-0) 92%,transparent);color:inherit;cursor:pointer;line-height:1;font-size:.78em}.i18n-button-label__action.confirm{color:var(--success)}.i18n-button-label__action[aria-disabled=true]{cursor:progress;opacity:.6}.i18n-button-label__error{color:var(--danger);text-align:left;max-width:100%;max-height:8rem;overflow-x:hidden;overflow-y:auto;overflow-wrap:anywhere;word-break:break-word}:root[data-theme=dark] .i18n-button-label__input,:root[data-theme=dark] .i18n-button-label__action{background:#09131a94}\n"], dependencies: [{ kind: "ngmodule", type: CommonModule }, { kind: "ngmodule", type: FormsModule }, { kind: "directive", type: i1.DefaultValueAccessor, selector: "input:not([type=checkbox])[formControlName],textarea[formControlName],input:not([type=checkbox])[formControl],textarea[formControl],input:not([type=checkbox])[ngModel],textarea[ngModel],[ngDefaultControl]" }, { kind: "directive", type: i1.NgControlStatus, selector: "[formControlName],[ngModel],[formControl]" }, { kind: "directive", type: i1.NgModel, selector: "[ngModel]:not([formControlName]):not([formControl])", inputs: ["name", "disabled", "ngModel", "ngModelOptions"], outputs: ["ngModelChange"], exportAs: ["ngModel"] }] }); }
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: ChillI18nButtonLabelComponent, decorators: [{
            type: Component,
            args: [{ selector: 'app-chill-i18n-button-label', standalone: true, imports: [CommonModule, FormsModule], template: `
    <span class="i18n-button-label">
      @if (isEditing()) {
        <input
          type="text"
          class="i18n-button-label__input"
          [ngModel]="draftText()"
          (ngModelChange)="draftText.set($event)"
          [disabled]="isSaving()"
          [attr.aria-label]="editAriaLabel()"
          (click)="swallow($event)"
          (mousedown)="swallow($event)"
          (keydown.enter)="save($event)"
          (keydown.escape)="cancel($event)" />

        <span
          class="i18n-button-label__action confirm"
          role="button"
          tabindex="0"
          [attr.aria-disabled]="isSaving() || !canSave()"
          [attr.aria-label]="saveAriaLabel()"
          (click)="save($event)"
          (mousedown)="swallow($event)"
          (keydown.enter)="save($event)"
          (keydown.space)="save($event)">
          ✓
        </span>
      } @else {
        <span class="i18n-button-label__text">{{ text() }}</span>

        @if (editEnabled()) {
          <span
            class="i18n-button-label__action edit"
            role="button"
            tabindex="0"
            [attr.aria-label]="editAriaLabel()"
            (click)="startEditing($event)"
            (mousedown)="swallow($event)"
            (keydown.enter)="startEditing($event)"
            (keydown.space)="startEditing($event)">
            ✎
          </span>
        }
      }
    </span>

    @if (errorMessage()) {
      <small class="i18n-button-label__error">{{ errorMessage() }}</small>
    }
  `, styles: [":host{display:inline-grid;gap:.2rem;max-width:100%}.i18n-button-label{display:inline-flex;align-items:center;justify-content:center;gap:.35rem;max-width:100%}.i18n-button-label__text{min-width:0}.i18n-button-label__input{min-width:0;width:min(18rem,100%);padding:.1rem .3rem;border:1px solid color-mix(in srgb,currentColor 28%,var(--border-color));border-radius:.4rem;background:var(--surface-0);color:var(--text-main);font:inherit}.i18n-button-label__action{width:1.35rem;height:1.35rem;display:inline-grid;place-items:center;border:1px solid color-mix(in srgb,currentColor 28%,var(--border-color));border-radius:.35rem;background:color-mix(in srgb,var(--surface-0) 92%,transparent);color:inherit;cursor:pointer;line-height:1;font-size:.78em}.i18n-button-label__action.confirm{color:var(--success)}.i18n-button-label__action[aria-disabled=true]{cursor:progress;opacity:.6}.i18n-button-label__error{color:var(--danger);text-align:left;max-width:100%;max-height:8rem;overflow-x:hidden;overflow-y:auto;overflow-wrap:anywhere;word-break:break-word}:root[data-theme=dark] .i18n-button-label__input,:root[data-theme=dark] .i18n-button-label__action{background:#09131a94}\n"] }]
        }] });

const NOTICE_TRANSITION_CLASS = 'notice-transition-leave-clone';
const NOTICE_TRANSITION_MS = 220;
const NOTICE_TRANSITION_EASING = 'cubic-bezier(0.2, 0, 0, 1)';
const NOTICE_TRANSITION_STYLE = [
    `height ${NOTICE_TRANSITION_MS}ms ${NOTICE_TRANSITION_EASING}`,
    `margin-block-start ${NOTICE_TRANSITION_MS}ms ${NOTICE_TRANSITION_EASING}`,
    `margin-block-end ${NOTICE_TRANSITION_MS}ms ${NOTICE_TRANSITION_EASING}`,
    `opacity ${NOTICE_TRANSITION_MS}ms ease`
].join(', ');
class NoticeTransitionDirective {
    constructor() {
        this.elementRef = inject(ElementRef);
        this.enterFrame = null;
        this.enterCleanupTimer = null;
        this.enterCleanup = null;
    }
    ngAfterViewInit() {
        const element = this.elementRef.nativeElement;
        if (this.shouldSkipAnimation(element)) {
            return;
        }
        this.animateEnter(element);
    }
    ngOnDestroy() {
        const element = this.elementRef.nativeElement;
        this.cancelEnterAnimation();
        if (this.shouldSkipAnimation(element)) {
            return;
        }
        this.animateLeaveClone(element);
    }
    animateEnter(element) {
        const computedStyle = getComputedStyle(element);
        const height = element.getBoundingClientRect().height;
        if (height <= 0) {
            return;
        }
        const originalStyles = {
            height: element.style.height,
            marginBlockStart: element.style.marginBlockStart,
            marginBlockEnd: element.style.marginBlockEnd,
            opacity: element.style.opacity,
            overflow: element.style.overflow,
            transition: element.style.transition,
            willChange: element.style.willChange
        };
        const finalMarginBlockStart = computedStyle.marginBlockStart;
        const finalMarginBlockEnd = computedStyle.marginBlockEnd;
        element.style.height = '0px';
        element.style.marginBlockStart = '0px';
        element.style.marginBlockEnd = '0px';
        element.style.opacity = '0';
        element.style.overflow = 'hidden';
        element.style.transition = 'none';
        element.style.willChange = 'height, margin, opacity';
        const finish = () => {
            if (this.enterCleanupTimer !== null) {
                window.clearTimeout(this.enterCleanupTimer);
                this.enterCleanupTimer = null;
            }
            element.removeEventListener('transitionend', handleTransitionEnd);
            this.enterCleanup = null;
            if (!element.isConnected) {
                return;
            }
            element.style.height = originalStyles.height;
            element.style.marginBlockStart = originalStyles.marginBlockStart;
            element.style.marginBlockEnd = originalStyles.marginBlockEnd;
            element.style.opacity = originalStyles.opacity;
            element.style.overflow = originalStyles.overflow;
            element.style.transition = originalStyles.transition;
            element.style.willChange = originalStyles.willChange;
        };
        const handleTransitionEnd = (event) => {
            if (event.target === element && event.propertyName === 'height') {
                finish();
            }
        };
        this.enterCleanup = finish;
        this.enterFrame = window.requestAnimationFrame(() => {
            this.enterFrame = null;
            if (!element.isConnected) {
                finish();
                return;
            }
            element.addEventListener('transitionend', handleTransitionEnd);
            element.style.transition = NOTICE_TRANSITION_STYLE;
            element.style.height = `${height}px`;
            element.style.marginBlockStart = finalMarginBlockStart;
            element.style.marginBlockEnd = finalMarginBlockEnd;
            element.style.opacity = '1';
            this.enterCleanupTimer = window.setTimeout(finish, NOTICE_TRANSITION_MS + 120);
        });
    }
    animateLeaveClone(element) {
        const parent = element.parentNode;
        if (!parent) {
            return;
        }
        const height = element.getBoundingClientRect().height;
        if (height <= 0) {
            return;
        }
        const computedStyle = getComputedStyle(element);
        const clone = element.cloneNode(true);
        clone.classList.add(NOTICE_TRANSITION_CLASS);
        clone.setAttribute('aria-hidden', 'true');
        clone.style.height = `${height}px`;
        clone.style.marginBlockStart = computedStyle.marginBlockStart;
        clone.style.marginBlockEnd = computedStyle.marginBlockEnd;
        clone.style.opacity = computedStyle.opacity || '1';
        clone.style.overflow = 'hidden';
        clone.style.pointerEvents = 'none';
        clone.style.transition = 'none';
        clone.style.willChange = 'height, margin, opacity';
        parent.insertBefore(clone, element.nextSibling);
        const removeClone = () => {
            clone.removeEventListener('transitionend', handleTransitionEnd);
            clone.remove();
        };
        const handleTransitionEnd = (event) => {
            if (event.target === clone && event.propertyName === 'height') {
                removeClone();
            }
        };
        window.requestAnimationFrame(() => {
            if (!clone.isConnected) {
                return;
            }
            clone.addEventListener('transitionend', handleTransitionEnd);
            clone.style.transition = NOTICE_TRANSITION_STYLE;
            clone.style.height = '0px';
            clone.style.marginBlockStart = '0px';
            clone.style.marginBlockEnd = '0px';
            clone.style.opacity = '0';
            window.setTimeout(removeClone, NOTICE_TRANSITION_MS + 120);
        });
    }
    cancelEnterAnimation() {
        if (this.enterFrame !== null) {
            window.cancelAnimationFrame(this.enterFrame);
            this.enterFrame = null;
        }
        if (this.enterCleanupTimer !== null) {
            window.clearTimeout(this.enterCleanupTimer);
            this.enterCleanupTimer = null;
        }
        this.enterCleanup?.();
        this.enterCleanup = null;
    }
    shouldSkipAnimation(element) {
        return typeof window === 'undefined'
            || element.classList.contains(NOTICE_TRANSITION_CLASS)
            || window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    }
    static { this.ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: NoticeTransitionDirective, deps: [], target: i0.ɵɵFactoryTarget.Directive }); }
    static { this.ɵdir = i0.ɵɵngDeclareDirective({ minVersion: "14.0.0", version: "19.2.21", type: NoticeTransitionDirective, isStandalone: true, selector: ".notice", ngImport: i0 }); }
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: NoticeTransitionDirective, decorators: [{
            type: Directive,
            args: [{
                    selector: '.notice',
                    standalone: true
                }]
        }] });

class WorkspaceToolbarService {
    constructor() {
        this.buttonScopesState = signal({});
    }
    buttons(scope = 'workspace') {
        return this.buttonScopesState()[scope] ?? [];
    }
    setButtons(buttons, scope = 'workspace') {
        this.buttonScopesState.update((current) => ({
            ...current,
            [scope]: [...buttons]
        }));
    }
    clearButtons(scope = 'workspace') {
        this.buttonScopesState.update((current) => {
            if (!(scope in current)) {
                return current;
            }
            const { [scope]: _removedScope, ...rest } = current;
            return rest;
        });
    }
    static { this.ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: WorkspaceToolbarService, deps: [], target: i0.ɵɵFactoryTarget.Injectable }); }
    static { this.ɵprov = i0.ɵɵngDeclareInjectable({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: WorkspaceToolbarService, providedIn: 'root' }); }
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: WorkspaceToolbarService, decorators: [{
            type: Injectable,
            args: [{
                    providedIn: 'root'
                }]
        }] });

class AuthSearchSelectComponent {
    constructor() {
        this.options = input([]);
        this.selectedId = input('');
        this.placeholder = input('Search and select');
        this.emptyMessage = input('Start typing to search.');
        this.noResultsMessage = input('No matches found.');
        this.clearAriaLabel = input('Clear selection');
        this.selectionChange = output();
        this.searchTerm = signal('');
        this.isOpen = signal(false);
        this.selectedOption = computed(() => this.options().find((option) => option.id === this.selectedId()) ?? null);
        this.filteredOptions = computed(() => {
            const query = this.searchTerm().trim().toLowerCase();
            if (!query) {
                return this.options();
            }
            return this.options().filter((option) => {
                const haystack = [
                    option.label,
                    option.description,
                    option.keywords
                ].join(' ').toLowerCase();
                return haystack.includes(query);
            });
        });
    }
    updateSearchTerm(value) {
        this.searchTerm.set(value);
        this.isOpen.set(true);
    }
    openResults() {
        this.isOpen.set(true);
    }
    closeResultsSoon() {
        window.setTimeout(() => {
            this.isOpen.set(false);
        }, 120);
    }
    selectOption(id) {
        this.searchTerm.set('');
        this.isOpen.set(false);
        this.selectionChange.emit(id);
    }
    clearSelection() {
        this.searchTerm.set('');
        this.isOpen.set(false);
        this.selectionChange.emit('');
    }
    static { this.ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: AuthSearchSelectComponent, deps: [], target: i0.ɵɵFactoryTarget.Component }); }
    static { this.ɵcmp = i0.ɵɵngDeclareComponent({ minVersion: "17.0.0", version: "19.2.21", type: AuthSearchSelectComponent, isStandalone: true, selector: "app-auth-search-select", inputs: { options: { classPropertyName: "options", publicName: "options", isSignal: true, isRequired: false, transformFunction: null }, selectedId: { classPropertyName: "selectedId", publicName: "selectedId", isSignal: true, isRequired: false, transformFunction: null }, placeholder: { classPropertyName: "placeholder", publicName: "placeholder", isSignal: true, isRequired: false, transformFunction: null }, emptyMessage: { classPropertyName: "emptyMessage", publicName: "emptyMessage", isSignal: true, isRequired: false, transformFunction: null }, noResultsMessage: { classPropertyName: "noResultsMessage", publicName: "noResultsMessage", isSignal: true, isRequired: false, transformFunction: null }, clearAriaLabel: { classPropertyName: "clearAriaLabel", publicName: "clearAriaLabel", isSignal: true, isRequired: false, transformFunction: null } }, outputs: { selectionChange: "selectionChange" }, ngImport: i0, template: `
    <div class="auth-search-select">
      @if (selectedOption(); as option) {
        <div class="auth-search-select__selected" [title]="option.label">
          <span class="auth-search-select__selected-label">{{ option.label }}</span>
          @if (option.description) {
            <span class="auth-search-select__selected-description">{{ option.description }}</span>
          }
          <button
            type="button"
            class="auth-search-select__clear"
            (click)="clearSelection()"
            [attr.aria-label]="clearAriaLabel()">
            X
          </button>
        </div>
      } @else {
        <div class="auth-search-select__lookup">
          <input
            type="text"
            [ngModel]="searchTerm()"
            (ngModelChange)="updateSearchTerm($event)"
            (focus)="openResults()"
            (blur)="closeResultsSoon()"
            [placeholder]="placeholder()" />

          @if (isOpen()) {
            <div class="auth-search-select__results" role="listbox">
              @for (option of filteredOptions(); track option.id) {
                <button
                  type="button"
                  class="auth-search-select__result"
                  (mousedown)="$event.preventDefault()"
                  (click)="selectOption(option.id)">
                  <strong>{{ option.label }}</strong>
                  @if (option.description) {
                    <small>{{ option.description }}</small>
                  }
                </button>
              } @empty {
                <div class="auth-search-select__empty">
                  {{ searchTerm().trim() ? noResultsMessage() : emptyMessage() }}
                </div>
              }
            </div>
          }
        </div>
      }
    </div>
  `, isInline: true, styles: [":host{display:block;min-width:0}.auth-search-select,.auth-search-select__lookup{position:relative}.auth-search-select__lookup input,.auth-search-select__selected{width:100%;min-height:3rem;border-radius:.85rem;border:1px solid var(--border-color);background:var(--surface-0);color:var(--text-main);font:inherit}.auth-search-select__lookup input{padding:.85rem 1rem}.auth-search-select__selected{display:flex;align-items:center;gap:.75rem;padding:.45rem .5rem .45rem 1rem}.auth-search-select__selected-label,.auth-search-select__selected-description{min-width:0;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}.auth-search-select__selected-label{font-weight:700;flex:1 1 auto}.auth-search-select__selected-description{color:var(--text-muted);flex:0 1 auto}.auth-search-select__clear{width:2rem;height:2rem;border:0;border-radius:999px;background:var(--accent-soft);color:var(--text-main);cursor:pointer;font:inherit;font-weight:700;flex:0 0 auto}.auth-search-select__results{position:absolute;left:0;right:0;top:calc(100% + .35rem);z-index:10;display:grid;gap:.35rem;max-height:18rem;padding:.45rem;overflow:auto;border-radius:.9rem;border:1px solid var(--border-color);background:var(--surface-0);box-shadow:var(--shadow)}.auth-search-select__result,.auth-search-select__empty{display:grid;gap:.2rem;padding:.7rem .8rem;border-radius:.75rem}.auth-search-select__result{border:0;background:transparent;color:var(--text-main);cursor:pointer;font:inherit;text-align:left}.auth-search-select__result:hover{background:var(--accent-soft)}.auth-search-select__result small,.auth-search-select__empty{color:var(--text-muted)}\n"], dependencies: [{ kind: "ngmodule", type: CommonModule }, { kind: "ngmodule", type: FormsModule }, { kind: "directive", type: i1.DefaultValueAccessor, selector: "input:not([type=checkbox])[formControlName],textarea[formControlName],input:not([type=checkbox])[formControl],textarea[formControl],input:not([type=checkbox])[ngModel],textarea[ngModel],[ngDefaultControl]" }, { kind: "directive", type: i1.NgControlStatus, selector: "[formControlName],[ngModel],[formControl]" }, { kind: "directive", type: i1.NgModel, selector: "[ngModel]:not([formControlName]):not([formControl])", inputs: ["name", "disabled", "ngModel", "ngModelOptions"], outputs: ["ngModelChange"], exportAs: ["ngModel"] }] }); }
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: AuthSearchSelectComponent, decorators: [{
            type: Component,
            args: [{ selector: 'app-auth-search-select', standalone: true, imports: [CommonModule, FormsModule], template: `
    <div class="auth-search-select">
      @if (selectedOption(); as option) {
        <div class="auth-search-select__selected" [title]="option.label">
          <span class="auth-search-select__selected-label">{{ option.label }}</span>
          @if (option.description) {
            <span class="auth-search-select__selected-description">{{ option.description }}</span>
          }
          <button
            type="button"
            class="auth-search-select__clear"
            (click)="clearSelection()"
            [attr.aria-label]="clearAriaLabel()">
            X
          </button>
        </div>
      } @else {
        <div class="auth-search-select__lookup">
          <input
            type="text"
            [ngModel]="searchTerm()"
            (ngModelChange)="updateSearchTerm($event)"
            (focus)="openResults()"
            (blur)="closeResultsSoon()"
            [placeholder]="placeholder()" />

          @if (isOpen()) {
            <div class="auth-search-select__results" role="listbox">
              @for (option of filteredOptions(); track option.id) {
                <button
                  type="button"
                  class="auth-search-select__result"
                  (mousedown)="$event.preventDefault()"
                  (click)="selectOption(option.id)">
                  <strong>{{ option.label }}</strong>
                  @if (option.description) {
                    <small>{{ option.description }}</small>
                  }
                </button>
              } @empty {
                <div class="auth-search-select__empty">
                  {{ searchTerm().trim() ? noResultsMessage() : emptyMessage() }}
                </div>
              }
            </div>
          }
        </div>
      }
    </div>
  `, styles: [":host{display:block;min-width:0}.auth-search-select,.auth-search-select__lookup{position:relative}.auth-search-select__lookup input,.auth-search-select__selected{width:100%;min-height:3rem;border-radius:.85rem;border:1px solid var(--border-color);background:var(--surface-0);color:var(--text-main);font:inherit}.auth-search-select__lookup input{padding:.85rem 1rem}.auth-search-select__selected{display:flex;align-items:center;gap:.75rem;padding:.45rem .5rem .45rem 1rem}.auth-search-select__selected-label,.auth-search-select__selected-description{min-width:0;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}.auth-search-select__selected-label{font-weight:700;flex:1 1 auto}.auth-search-select__selected-description{color:var(--text-muted);flex:0 1 auto}.auth-search-select__clear{width:2rem;height:2rem;border:0;border-radius:999px;background:var(--accent-soft);color:var(--text-main);cursor:pointer;font:inherit;font-weight:700;flex:0 0 auto}.auth-search-select__results{position:absolute;left:0;right:0;top:calc(100% + .35rem);z-index:10;display:grid;gap:.35rem;max-height:18rem;padding:.45rem;overflow:auto;border-radius:.9rem;border:1px solid var(--border-color);background:var(--surface-0);box-shadow:var(--shadow)}.auth-search-select__result,.auth-search-select__empty{display:grid;gap:.2rem;padding:.7rem .8rem;border-radius:.75rem}.auth-search-select__result{border:0;background:transparent;color:var(--text-main);cursor:pointer;font:inherit;text-align:left}.auth-search-select__result:hover{background:var(--accent-soft)}.auth-search-select__result small,.auth-search-select__empty{color:var(--text-muted)}\n"] }]
        }] });

const ALL_PROPERTIES_VALUE = '__all__';
class PermissionEditorComponent {
    constructor() {
        this.chill = inject(ChillService);
        this.rows = input([]);
        this.rowsChange = output();
        this.lookupErrorMessage = signal('');
        this.moduleOptions = signal([]);
        this.entityOptionsByKey = signal({});
        this.propertyOptionsByKey = signal({});
        this.editingRowIds = signal([]);
        this.permissionEffectOptions = [
            { value: PermissionEffect.Allow, label: 'Allow' },
            { value: PermissionEffect.Deny, label: 'Deny' }
        ];
        this.permissionActionOptions = [
            { value: PermissionAction.FullControl, label: 'FullControl' },
            { value: PermissionAction.Query, label: 'Query' },
            { value: PermissionAction.Create, label: 'Create' },
            { value: PermissionAction.Update, label: 'Update' },
            { value: PermissionAction.Delete, label: 'Delete' },
            { value: PermissionAction.See, label: 'See' },
            { value: PermissionAction.Modify, label: 'Modify' }
        ];
        this.permissionScopeOptions = [
            { value: PermissionScope.Module, label: 'Module' },
            { value: PermissionScope.Entity, label: 'Entity' },
            { value: PermissionScope.Property, label: 'Property' }
        ];
        this.loadModuleOptions();
        effect(() => {
            const rows = this.rows();
            this.ensureOptionsForRows(rows);
            this.syncEditingRows(rows);
        });
    }
    addPermissionRule() {
        const localId = `new-${crypto.randomUUID()}`;
        this.editingRowIds.update((ids) => [...ids, localId]);
        this.rowsChange.emit([
            ...this.rows(),
            {
                localId,
                guid: '',
                effect: PermissionEffect.Allow,
                action: PermissionAction.Query,
                scope: PermissionScope.Module,
                module: '',
                entityName: '',
                propertyName: '',
                appliesToAllProperties: false,
                description: ''
            }
        ]);
    }
    startEditingRow(rowId) {
        this.editingRowIds.update((ids) => ids.includes(rowId) ? ids : [...ids, rowId]);
    }
    stopEditingRow(rowId) {
        this.editingRowIds.update((ids) => ids.filter((id) => id !== rowId));
    }
    isEditingRow(rowId) {
        return this.editingRowIds().includes(rowId);
    }
    updatePermissionRow(rowId, key, value) {
        this.rowsChange.emit(this.rows().map((row) => {
            if (row.localId !== rowId) {
                return row;
            }
            const updatedRow = { ...row, [key]: value };
            if (key === 'module') {
                updatedRow.entityName = '';
                updatedRow.propertyName = '';
                updatedRow.appliesToAllProperties = false;
                this.ensureEntityOptions(updatedRow.module, updatedRow.action);
            }
            else if (key === 'action') {
                updatedRow.entityName = '';
                updatedRow.propertyName = '';
                updatedRow.appliesToAllProperties = false;
                this.ensureEntityOptions(updatedRow.module, updatedRow.action);
            }
            else if (key === 'entityName') {
                updatedRow.propertyName = '';
                updatedRow.appliesToAllProperties = false;
                this.ensurePropertyOptions(updatedRow.module, updatedRow.action, updatedRow.entityName);
            }
            else if (key === 'scope' && value === PermissionScope.Module) {
                updatedRow.entityName = '';
                updatedRow.propertyName = '';
                updatedRow.appliesToAllProperties = false;
            }
            else if (key === 'scope' && value === PermissionScope.Entity) {
                updatedRow.propertyName = '';
                updatedRow.appliesToAllProperties = false;
            }
            else if (key === 'appliesToAllProperties') {
                updatedRow.appliesToAllProperties = !!value;
                updatedRow.propertyName = updatedRow.appliesToAllProperties ? '' : updatedRow.propertyName;
            }
            return updatedRow;
        }));
    }
    updatePropertySelection(rowId, value) {
        this.rowsChange.emit(this.rows().map((row) => {
            if (row.localId !== rowId) {
                return row;
            }
            if (value === ALL_PROPERTIES_VALUE) {
                return {
                    ...row,
                    propertyName: '',
                    appliesToAllProperties: true
                };
            }
            return {
                ...row,
                propertyName: value,
                appliesToAllProperties: false
            };
        }));
    }
    removePermissionRow(rowId) {
        this.stopEditingRow(rowId);
        this.rowsChange.emit(this.rows().filter((row) => row.localId !== rowId));
    }
    effectLabel(value) {
        return this.permissionEffectOptions.find((option) => option.value === value)?.label ?? String(value);
    }
    actionLabel(value) {
        return this.permissionActionOptions.find((option) => option.value === value)?.label ?? String(value);
    }
    scopeLabel(value) {
        return this.permissionScopeOptions.find((option) => option.value === value)?.label ?? String(value);
    }
    targetLabel(row) {
        if (row.scope === PermissionScope.Module) {
            return row.module?.trim() || this.chill.T('DB687669-CA05-4786-83C7-E5537D38FBCB', 'Any module', 'Qualsiasi modulo');
        }
        if (row.scope === PermissionScope.Entity) {
            return row.entityName?.trim() || this.chill.T('A69C0B93-F1E2-41BE-8586-1493B10A4033', 'Any entity', 'Qualsiasi entita');
        }
        if (row.appliesToAllProperties) {
            return `${row.entityName?.trim() || this.chill.T('4B45CB93-F918-4892-A208-9A46282DEBBC', 'Entity', 'Entita')}.*`;
        }
        return row.propertyName?.trim()
            || this.chill.T('16A9A1A4-B1B6-49D7-AE2C-927E96D05172', 'Any property', 'Qualsiasi proprieta');
    }
    descriptionLabel(row) {
        return row.description?.trim()
            || this.chill.T('57D5BF09-012A-43D4-84BF-2BA33DDECA31', 'No description', 'Nessuna descrizione');
    }
    entityOptionsFor(row) {
        return this.entityOptionsByKey()[this.entityOptionsKey(row.module, row.action)] ?? [];
    }
    propertyOptionsFor(row) {
        return this.propertyOptionsByKey()[this.propertyOptionsKey(row.module, row.action, row.entityName)] ?? [];
    }
    propertySelectValueFor(row) {
        return row.appliesToAllProperties ? ALL_PROPERTIES_VALUE : (row.propertyName ?? '');
    }
    loadModuleOptions() {
        this.chill.getModuleList().subscribe({
            next: (modules) => {
                this.moduleOptions.set(modules);
            },
            error: (error) => {
                this.lookupErrorMessage.set(this.chill.formatError(error));
            }
        });
    }
    ensureOptionsForRows(rows) {
        rows.forEach((row) => {
            this.ensureEntityOptions(row.module, row.action);
            this.ensurePropertyOptions(row.module, row.action, row.entityName);
        });
    }
    syncEditingRows(rows) {
        const activeIds = new Set(rows.map((row) => row.localId));
        this.editingRowIds.update((ids) => ids.filter((id) => activeIds.has(id)));
    }
    ensureEntityOptions(module, action) {
        const normalizedModule = module?.trim() ?? '';
        if (!normalizedModule) {
            return;
        }
        const key = this.entityOptionsKey(normalizedModule, action);
        if (this.entityOptionsByKey()[key]) {
            return;
        }
        const source = action === PermissionAction.Query
            ? this.chill.getQueryList(normalizedModule)
            : this.chill.getEntityList(normalizedModule);
        source.subscribe({
            next: (entities) => {
                this.entityOptionsByKey.update((current) => ({
                    ...current,
                    [key]: entities
                }));
            },
            error: (error) => {
                this.lookupErrorMessage.set(this.chill.formatError(error));
            }
        });
    }
    ensurePropertyOptions(module, action, entityName) {
        const normalizedModule = module?.trim() ?? '';
        const normalizedEntityName = entityName?.trim() ?? '';
        if (!normalizedModule || !normalizedEntityName) {
            return;
        }
        const key = this.propertyOptionsKey(normalizedModule, action, normalizedEntityName);
        if (this.propertyOptionsByKey()[key]) {
            return;
        }
        this.chill.getPropertyList(normalizedEntityName).subscribe({
            next: (properties) => {
                this.propertyOptionsByKey.update((current) => ({
                    ...current,
                    [key]: properties
                }));
            },
            error: (error) => {
                this.lookupErrorMessage.set(this.chill.formatError(error));
            }
        });
    }
    entityOptionsKey(module, action) {
        return `${action}|${module?.trim() ?? ''}`;
    }
    propertyOptionsKey(module, action, entityName) {
        return `${action}|${module?.trim() ?? ''}|${entityName?.trim() ?? ''}`;
    }
    static { this.ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: PermissionEditorComponent, deps: [], target: i0.ɵɵFactoryTarget.Component }); }
    static { this.ɵcmp = i0.ɵɵngDeclareComponent({ minVersion: "17.0.0", version: "19.2.21", type: PermissionEditorComponent, isStandalone: true, selector: "app-permission-editor", inputs: { rows: { classPropertyName: "rows", publicName: "rows", isSignal: true, isRequired: false, transformFunction: null } }, outputs: { rowsChange: "rowsChange" }, ngImport: i0, template: "@if (lookupErrorMessage()) {\n  <div class=\"notice error\">{{ lookupErrorMessage() }}</div>\n}\n\n<section class=\"box-card\">\n  <header class=\"card-header\">\n    <h3><app-chill-i18n-label [labelGuid]=\"'64D855B5-1A89-4D6B-BD59-B201182BDBEF'\" [primaryDefaultText]=\"'Permission rules'\" [secondaryDefaultText]=\"'Regole permesso'\" /></h3>\n    <p><app-chill-i18n-label [labelGuid]=\"'C04B7E13-FB43-4E9B-B9DA-DB4E6E49590A'\" [primaryDefaultText]=\"'Configure module, entity, and property permission rules.'\" [secondaryDefaultText]=\"'Configura regole permesso per modulo, entit\u00E0 e propriet\u00E0.'\" /></p>\n  </header>\n\n  <div class=\"permission-list\">\n    @for (rule of rows(); track rule.localId) {\n      <article class=\"permission-card\">\n        @if (isEditingRow(rule.localId)) {\n          <div class=\"form-grid\">\n            <label class=\"field\">\n              <span><app-chill-i18n-label [labelGuid]=\"'678D4A79-804B-4D8F-B986-B08F4E3EC22F'\" [primaryDefaultText]=\"'Effect'\" [secondaryDefaultText]=\"'Effetto'\" /></span>\n              <select [ngModel]=\"rule.effect\" (ngModelChange)=\"updatePermissionRow(rule.localId, 'effect', +$event)\">\n                @for (option of permissionEffectOptions; track option.value) {\n                  <option [value]=\"option.value\">{{ option.label }}</option>\n                }\n              </select>\n            </label>\n\n            <label class=\"field\">\n              <span><app-chill-i18n-label [labelGuid]=\"'7E4DDC4C-D4E7-4427-BC9A-08AEF926A0FE'\" [primaryDefaultText]=\"'Action'\" [secondaryDefaultText]=\"'Azione'\" /></span>\n              <select [ngModel]=\"rule.action\" (ngModelChange)=\"updatePermissionRow(rule.localId, 'action', +$event)\">\n                @for (option of permissionActionOptions; track option.value) {\n                  <option [value]=\"option.value\">{{ option.label }}</option>\n                }\n              </select>\n            </label>\n\n            <label class=\"field\">\n              <span><app-chill-i18n-label [labelGuid]=\"'88724B00-D84F-44C8-AFC9-BC6C94A8D5E6'\" [primaryDefaultText]=\"'Scope'\" [secondaryDefaultText]=\"'Ambito'\" /></span>\n              <select [ngModel]=\"rule.scope\" (ngModelChange)=\"updatePermissionRow(rule.localId, 'scope', +$event)\">\n                @for (option of permissionScopeOptions; track option.value) {\n                  <option [value]=\"option.value\">{{ option.label }}</option>\n                }\n              </select>\n            </label>\n\n            <label class=\"field\">\n              <span><app-chill-i18n-label [labelGuid]=\"'2E4F99D0-8747-4A72-8394-C53F144D6CB2'\" [primaryDefaultText]=\"'Module'\" [secondaryDefaultText]=\"'Modulo'\" /></span>\n              <select [ngModel]=\"rule.module\" (ngModelChange)=\"updatePermissionRow(rule.localId, 'module', $event)\">\n                <option value=\"\">{{ chill.T('5D5D7655-697B-442C-BB16-67250D755916', 'Select module', 'Seleziona modulo') }}</option>\n                @for (module of moduleOptions(); track module) {\n                  <option [value]=\"module\">{{ module }}</option>\n                }\n              </select>\n            </label>\n\n            <label class=\"field\">\n              <span><app-chill-i18n-label [labelGuid]=\"'22268D8E-642A-45A4-A923-D5BC560C59EA'\" [primaryDefaultText]=\"'Entity name'\" [secondaryDefaultText]=\"'Nome entit\u00E0'\" /></span>\n              <select\n                [ngModel]=\"rule.entityName\"\n                [disabled]=\"rule.scope === 1 || !rule.module\"\n                (ngModelChange)=\"updatePermissionRow(rule.localId, 'entityName', $event)\">\n                <option value=\"\">\n                  {{ rule.action === 1\n                    ? chill.T('3CC838AF-33F8-45DF-A5EB-6BB6DD0A7C6B', 'Select query', 'Seleziona query')\n                    : chill.T('BA36F4A9-7F62-4E0A-A79A-0F24311E24E2', 'Select entity', 'Seleziona entit\u00E0') }}\n                </option>\n                @for (entity of entityOptionsFor(rule); track entity) {\n                  <option [value]=\"entity\">{{ entity }}</option>\n                }\n              </select>\n            </label>\n\n            <label class=\"field\">\n              <span><app-chill-i18n-label [labelGuid]=\"'B947A33E-97FD-49A0-B59B-D2D77838DA25'\" [primaryDefaultText]=\"'Property name'\" [secondaryDefaultText]=\"'Nome propriet\u00E0'\" /></span>\n              <select\n                [ngModel]=\"propertySelectValueFor(rule)\"\n                [disabled]=\"rule.scope !== 3 || !rule.entityName\"\n                (ngModelChange)=\"updatePropertySelection(rule.localId, $event)\">\n                <option value=\"\">{{ chill.T('40B20240-3BC0-43C2-8F2A-AF9525206458', 'Select property', 'Seleziona propriet\u00E0') }}</option>\n                <option value=\"__all__\">{{ chill.T('20D5D424-D258-4F52-8D4A-5C3E1F6892E1', 'ALL | Tutte', 'ALL | Tutte') }}</option>\n                @for (property of propertyOptionsFor(rule); track property) {\n                  <option [value]=\"property\">{{ property }}</option>\n                }\n              </select>\n            </label>\n\n            <label class=\"field full-width\">\n              <span><app-chill-i18n-label [labelGuid]=\"'97A7BFE7-22A7-4665-B0D8-C75506A8F794'\" [primaryDefaultText]=\"'Description'\" [secondaryDefaultText]=\"'Descrizione'\" /></span>\n              <textarea rows=\"2\" [ngModel]=\"rule.description\" (ngModelChange)=\"updatePermissionRow(rule.localId, 'description', $event)\"></textarea>\n            </label>\n          </div>\n\n          <div class=\"permission-actions\">\n            <label class=\"toggle\">\n              <input type=\"checkbox\" [ngModel]=\"rule.appliesToAllProperties\" [disabled]=\"rule.scope !== 3\" (ngModelChange)=\"updatePermissionRow(rule.localId, 'appliesToAllProperties', $event)\" />\n              <span><app-chill-i18n-label [labelGuid]=\"'2B72F81F-D445-4F56-B86D-98D7C24A3B4B'\" [primaryDefaultText]=\"'Apply to all properties'\" [secondaryDefaultText]=\"'Applica a tutte le propriet\u00E0'\" /></span>\n            </label>\n\n            <div class=\"action-row\">\n              <button type=\"button\" class=\"secondary\" (click)=\"stopEditingRow(rule.localId)\">\n                <app-chill-i18n-button-label [labelGuid]=\"'D860F9DB-C5B7-4C29-A60B-D0A7C8321CB4'\" [primaryDefaultText]=\"'Done'\" [secondaryDefaultText]=\"'Chiudi'\" />\n              </button>\n\n              <button type=\"button\" class=\"secondary danger\" (click)=\"removePermissionRow(rule.localId)\">\n                <app-chill-i18n-button-label [labelGuid]=\"'704B4EC7-C971-48C7-9439-E08C2F590992'\" [primaryDefaultText]=\"'Delete'\" [secondaryDefaultText]=\"'Elimina'\" />\n              </button>\n            </div>\n          </div>\n        } @else {\n          <div class=\"permission-summary\">\n            <div class=\"summary-main\">\n              <strong>{{ effectLabel(rule.effect) }} {{ actionLabel(rule.action) }}</strong>\n              <span>{{ scopeLabel(rule.scope) }}: {{ targetLabel(rule) }}</span>\n              <small>{{ descriptionLabel(rule) }}</small>\n            </div>\n\n            <div class=\"summary-meta\">\n              <span class=\"summary-chip\">{{ rule.module || chill.T('0D6742DB-C4D4-4B68-ABCD-5F7B168499BF', 'No module', 'Nessun modulo') }}</span>\n              @if (rule.entityName) {\n                <span class=\"summary-chip\">{{ rule.entityName }}</span>\n              }\n              @if (rule.scope === 3 && rule.appliesToAllProperties) {\n                <span class=\"summary-chip\">{{ chill.T('13D7F183-7FB4-42F7-B6CF-DA43269331EA', 'All properties', 'Tutte le propriet\u00E0') }}</span>\n              }\n            </div>\n          </div>\n\n          <div class=\"permission-actions compact-actions\">\n            <button type=\"button\" class=\"secondary\" (click)=\"startEditingRow(rule.localId)\">\n              <app-chill-i18n-button-label [labelGuid]=\"'314E7191-5C3A-4A96-8D01-AC4C17FF757F'\" [primaryDefaultText]=\"'Edit'\" [secondaryDefaultText]=\"'Modifica'\" />\n            </button>\n\n            <button type=\"button\" class=\"secondary danger\" (click)=\"removePermissionRow(rule.localId)\">\n              <app-chill-i18n-button-label [labelGuid]=\"'704B4EC7-C971-48C7-9439-E08C2F590992'\" [primaryDefaultText]=\"'Delete'\" [secondaryDefaultText]=\"'Elimina'\" />\n            </button>\n          </div>\n        }\n      </article>\n    } @empty {\n      <div class=\"empty-state\">{{ chill.T('1801F875-1D00-4D0B-BE62-C72471A645B2', 'No permission rules for the selected target.', 'Nessuna regola permesso per la destinazione selezionata.') }}</div>\n    }\n  </div>\n\n  <button type=\"button\" class=\"secondary\" (click)=\"addPermissionRule()\">\n    <app-chill-i18n-button-label [labelGuid]=\"'B4F053A6-0B3E-43A3-9B7D-5124EF2A3952'\" [primaryDefaultText]=\"'Create permission rule'\" [secondaryDefaultText]=\"'Crea regola permesso'\" />\n  </button>\n</section>\n", styles: [":host{display:block}.permission-editor,.entity-header,.entity-toolbar,.editor-layout{display:grid;gap:1rem}.entity-toolbar{grid-template-columns:minmax(0,1fr) auto;align-items:start}.entity-toolbar__lookup{min-width:0}.entity-toolbar__actions{display:flex;gap:.75rem;flex-wrap:wrap;justify-content:end}.editor-layout{gap:1.25rem;grid-template-columns:minmax(16rem,22rem) minmax(0,1fr);align-items:start}.sidebar-card,.box-card,.empty-panel,.create-card{display:grid;gap:1rem;padding:1rem 1.25rem;border-radius:1rem;border:1px solid var(--border-color);background:var(--surface-2);color:var(--text-main)}.box-card{background:var(--surface-3);box-shadow:var(--shadow-soft)}.content-column{display:grid;gap:1rem}.selection-header,.card-header,.permission-actions,.check-item{display:flex;gap:1rem}.selection-header,.permission-actions{justify-content:space-between;align-items:start}.card-header,.check-item{flex-direction:column}.card-header h2,.card-header h3,.card-header p,.selection-header h2,.selection-header p{margin:0}.section-kicker{margin:0 0 .35rem;color:var(--accent);font-size:.78rem;font-weight:700;letter-spacing:.14em;text-transform:uppercase}.search-field,.field{display:grid;gap:.4rem}.search-field input,.field input,.field textarea,.field select{width:100%;padding:.8rem .95rem;border-radius:.8rem;border:1px solid var(--border-color);background:var(--surface-0);color:var(--text-main);font:inherit}.selector-list,.permission-list,.check-list,.box-grid{display:grid;gap:.8rem}.selector-list,.check-list{max-height:30rem;overflow:auto}.selector-item,.permission-card,.check-item{padding:.85rem .95rem;border-radius:.85rem;border:1px solid var(--border-color);background:color-mix(in srgb,var(--surface-0) 78%,var(--surface-2))}.selector-item{display:grid;gap:.2rem;text-align:left;color:var(--text-main)}.selector-item.active{border-color:var(--accent);box-shadow:inset 0 0 0 1px var(--accent)}.permission-card{display:grid;gap:.9rem}.selector-item small,.check-item small,.card-header p,.selection-header p{color:var(--text-muted)}.box-grid{grid-template-columns:repeat(2,minmax(0,1fr))}.form-grid{display:grid;gap:.85rem;grid-template-columns:repeat(2,minmax(0,1fr))}.permission-summary,.summary-main,.summary-meta{display:grid}.permission-summary{gap:.65rem}.summary-main{gap:.2rem}.summary-main strong,.summary-main span,.summary-main small{margin:0}.summary-main span,.summary-main small{color:var(--text-muted)}.summary-meta{gap:.45rem;grid-template-columns:repeat(auto-fit,minmax(8rem,max-content))}.summary-chip{display:inline-flex;align-items:center;min-height:2rem;padding:.2rem .7rem;border-radius:999px;background:var(--accent-soft);border:1px solid color-mix(in srgb,var(--accent) 22%,var(--border-color));color:var(--text-main);font-size:.88rem}.field.full-width{grid-column:1/-1}.toggle{display:flex;gap:.7rem;align-items:center}.action-row{display:flex;gap:.65rem;flex-wrap:wrap;justify-content:end}.compact-actions{justify-content:end}.check-item{display:flex;flex-direction:row;align-items:start}.empty-state,.empty-panel{color:var(--text-muted)}.notice{padding:1rem 1.25rem;border-radius:1rem;background:var(--surface-2);border:1px solid var(--border-color);color:var(--text-main)}.notice.error{color:#8b1e3f;border-color:#8b1e3f33;background:#fff0f4eb}.notice.success{color:#175b3a;border-color:#175b3a33;background:#eefff6eb}button{padding:.8rem 1rem;border-radius:.8rem;border:0;background:var(--accent);color:#fff;font:inherit;font-weight:700}button.secondary{background:var(--surface-3);color:var(--text-main)}button.danger{background:#8b1e3f}button:disabled{opacity:.55}@media(max-width:1100px){.entity-toolbar,.editor-layout,.box-grid{grid-template-columns:1fr}.entity-toolbar__actions{justify-content:start}}@media(max-width:720px){.form-grid{grid-template-columns:1fr}.selection-header,.permission-actions{flex-direction:column}.action-row{justify-content:start}}\n"], dependencies: [{ kind: "ngmodule", type: CommonModule }, { kind: "ngmodule", type: FormsModule }, { kind: "directive", type: i1.NgSelectOption, selector: "option", inputs: ["ngValue", "value"] }, { kind: "directive", type: i1.ɵNgSelectMultipleOption, selector: "option", inputs: ["ngValue", "value"] }, { kind: "directive", type: i1.DefaultValueAccessor, selector: "input:not([type=checkbox])[formControlName],textarea[formControlName],input:not([type=checkbox])[formControl],textarea[formControl],input:not([type=checkbox])[ngModel],textarea[ngModel],[ngDefaultControl]" }, { kind: "directive", type: i1.CheckboxControlValueAccessor, selector: "input[type=checkbox][formControlName],input[type=checkbox][formControl],input[type=checkbox][ngModel]" }, { kind: "directive", type: i1.SelectControlValueAccessor, selector: "select:not([multiple])[formControlName],select:not([multiple])[formControl],select:not([multiple])[ngModel]", inputs: ["compareWith"] }, { kind: "directive", type: i1.NgControlStatus, selector: "[formControlName],[ngModel],[formControl]" }, { kind: "directive", type: i1.NgModel, selector: "[ngModel]:not([formControlName]):not([formControl])", inputs: ["name", "disabled", "ngModel", "ngModelOptions"], outputs: ["ngModelChange"], exportAs: ["ngModel"] }, { kind: "component", type: ChillI18nLabelComponent, selector: "app-chill-i18n-label", inputs: ["labelGuid", "primaryDefaultText", "secondaryDefaultText", "editable"] }, { kind: "component", type: ChillI18nButtonLabelComponent, selector: "app-chill-i18n-button-label", inputs: ["labelGuid", "primaryDefaultText", "secondaryDefaultText", "editable"] }, { kind: "directive", type: NoticeTransitionDirective, selector: ".notice" }] }); }
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: PermissionEditorComponent, decorators: [{
            type: Component,
            args: [{ selector: 'app-permission-editor', standalone: true, imports: [CommonModule, FormsModule, ChillI18nLabelComponent, ChillI18nButtonLabelComponent, NoticeTransitionDirective], template: "@if (lookupErrorMessage()) {\n  <div class=\"notice error\">{{ lookupErrorMessage() }}</div>\n}\n\n<section class=\"box-card\">\n  <header class=\"card-header\">\n    <h3><app-chill-i18n-label [labelGuid]=\"'64D855B5-1A89-4D6B-BD59-B201182BDBEF'\" [primaryDefaultText]=\"'Permission rules'\" [secondaryDefaultText]=\"'Regole permesso'\" /></h3>\n    <p><app-chill-i18n-label [labelGuid]=\"'C04B7E13-FB43-4E9B-B9DA-DB4E6E49590A'\" [primaryDefaultText]=\"'Configure module, entity, and property permission rules.'\" [secondaryDefaultText]=\"'Configura regole permesso per modulo, entit\u00E0 e propriet\u00E0.'\" /></p>\n  </header>\n\n  <div class=\"permission-list\">\n    @for (rule of rows(); track rule.localId) {\n      <article class=\"permission-card\">\n        @if (isEditingRow(rule.localId)) {\n          <div class=\"form-grid\">\n            <label class=\"field\">\n              <span><app-chill-i18n-label [labelGuid]=\"'678D4A79-804B-4D8F-B986-B08F4E3EC22F'\" [primaryDefaultText]=\"'Effect'\" [secondaryDefaultText]=\"'Effetto'\" /></span>\n              <select [ngModel]=\"rule.effect\" (ngModelChange)=\"updatePermissionRow(rule.localId, 'effect', +$event)\">\n                @for (option of permissionEffectOptions; track option.value) {\n                  <option [value]=\"option.value\">{{ option.label }}</option>\n                }\n              </select>\n            </label>\n\n            <label class=\"field\">\n              <span><app-chill-i18n-label [labelGuid]=\"'7E4DDC4C-D4E7-4427-BC9A-08AEF926A0FE'\" [primaryDefaultText]=\"'Action'\" [secondaryDefaultText]=\"'Azione'\" /></span>\n              <select [ngModel]=\"rule.action\" (ngModelChange)=\"updatePermissionRow(rule.localId, 'action', +$event)\">\n                @for (option of permissionActionOptions; track option.value) {\n                  <option [value]=\"option.value\">{{ option.label }}</option>\n                }\n              </select>\n            </label>\n\n            <label class=\"field\">\n              <span><app-chill-i18n-label [labelGuid]=\"'88724B00-D84F-44C8-AFC9-BC6C94A8D5E6'\" [primaryDefaultText]=\"'Scope'\" [secondaryDefaultText]=\"'Ambito'\" /></span>\n              <select [ngModel]=\"rule.scope\" (ngModelChange)=\"updatePermissionRow(rule.localId, 'scope', +$event)\">\n                @for (option of permissionScopeOptions; track option.value) {\n                  <option [value]=\"option.value\">{{ option.label }}</option>\n                }\n              </select>\n            </label>\n\n            <label class=\"field\">\n              <span><app-chill-i18n-label [labelGuid]=\"'2E4F99D0-8747-4A72-8394-C53F144D6CB2'\" [primaryDefaultText]=\"'Module'\" [secondaryDefaultText]=\"'Modulo'\" /></span>\n              <select [ngModel]=\"rule.module\" (ngModelChange)=\"updatePermissionRow(rule.localId, 'module', $event)\">\n                <option value=\"\">{{ chill.T('5D5D7655-697B-442C-BB16-67250D755916', 'Select module', 'Seleziona modulo') }}</option>\n                @for (module of moduleOptions(); track module) {\n                  <option [value]=\"module\">{{ module }}</option>\n                }\n              </select>\n            </label>\n\n            <label class=\"field\">\n              <span><app-chill-i18n-label [labelGuid]=\"'22268D8E-642A-45A4-A923-D5BC560C59EA'\" [primaryDefaultText]=\"'Entity name'\" [secondaryDefaultText]=\"'Nome entit\u00E0'\" /></span>\n              <select\n                [ngModel]=\"rule.entityName\"\n                [disabled]=\"rule.scope === 1 || !rule.module\"\n                (ngModelChange)=\"updatePermissionRow(rule.localId, 'entityName', $event)\">\n                <option value=\"\">\n                  {{ rule.action === 1\n                    ? chill.T('3CC838AF-33F8-45DF-A5EB-6BB6DD0A7C6B', 'Select query', 'Seleziona query')\n                    : chill.T('BA36F4A9-7F62-4E0A-A79A-0F24311E24E2', 'Select entity', 'Seleziona entit\u00E0') }}\n                </option>\n                @for (entity of entityOptionsFor(rule); track entity) {\n                  <option [value]=\"entity\">{{ entity }}</option>\n                }\n              </select>\n            </label>\n\n            <label class=\"field\">\n              <span><app-chill-i18n-label [labelGuid]=\"'B947A33E-97FD-49A0-B59B-D2D77838DA25'\" [primaryDefaultText]=\"'Property name'\" [secondaryDefaultText]=\"'Nome propriet\u00E0'\" /></span>\n              <select\n                [ngModel]=\"propertySelectValueFor(rule)\"\n                [disabled]=\"rule.scope !== 3 || !rule.entityName\"\n                (ngModelChange)=\"updatePropertySelection(rule.localId, $event)\">\n                <option value=\"\">{{ chill.T('40B20240-3BC0-43C2-8F2A-AF9525206458', 'Select property', 'Seleziona propriet\u00E0') }}</option>\n                <option value=\"__all__\">{{ chill.T('20D5D424-D258-4F52-8D4A-5C3E1F6892E1', 'ALL | Tutte', 'ALL | Tutte') }}</option>\n                @for (property of propertyOptionsFor(rule); track property) {\n                  <option [value]=\"property\">{{ property }}</option>\n                }\n              </select>\n            </label>\n\n            <label class=\"field full-width\">\n              <span><app-chill-i18n-label [labelGuid]=\"'97A7BFE7-22A7-4665-B0D8-C75506A8F794'\" [primaryDefaultText]=\"'Description'\" [secondaryDefaultText]=\"'Descrizione'\" /></span>\n              <textarea rows=\"2\" [ngModel]=\"rule.description\" (ngModelChange)=\"updatePermissionRow(rule.localId, 'description', $event)\"></textarea>\n            </label>\n          </div>\n\n          <div class=\"permission-actions\">\n            <label class=\"toggle\">\n              <input type=\"checkbox\" [ngModel]=\"rule.appliesToAllProperties\" [disabled]=\"rule.scope !== 3\" (ngModelChange)=\"updatePermissionRow(rule.localId, 'appliesToAllProperties', $event)\" />\n              <span><app-chill-i18n-label [labelGuid]=\"'2B72F81F-D445-4F56-B86D-98D7C24A3B4B'\" [primaryDefaultText]=\"'Apply to all properties'\" [secondaryDefaultText]=\"'Applica a tutte le propriet\u00E0'\" /></span>\n            </label>\n\n            <div class=\"action-row\">\n              <button type=\"button\" class=\"secondary\" (click)=\"stopEditingRow(rule.localId)\">\n                <app-chill-i18n-button-label [labelGuid]=\"'D860F9DB-C5B7-4C29-A60B-D0A7C8321CB4'\" [primaryDefaultText]=\"'Done'\" [secondaryDefaultText]=\"'Chiudi'\" />\n              </button>\n\n              <button type=\"button\" class=\"secondary danger\" (click)=\"removePermissionRow(rule.localId)\">\n                <app-chill-i18n-button-label [labelGuid]=\"'704B4EC7-C971-48C7-9439-E08C2F590992'\" [primaryDefaultText]=\"'Delete'\" [secondaryDefaultText]=\"'Elimina'\" />\n              </button>\n            </div>\n          </div>\n        } @else {\n          <div class=\"permission-summary\">\n            <div class=\"summary-main\">\n              <strong>{{ effectLabel(rule.effect) }} {{ actionLabel(rule.action) }}</strong>\n              <span>{{ scopeLabel(rule.scope) }}: {{ targetLabel(rule) }}</span>\n              <small>{{ descriptionLabel(rule) }}</small>\n            </div>\n\n            <div class=\"summary-meta\">\n              <span class=\"summary-chip\">{{ rule.module || chill.T('0D6742DB-C4D4-4B68-ABCD-5F7B168499BF', 'No module', 'Nessun modulo') }}</span>\n              @if (rule.entityName) {\n                <span class=\"summary-chip\">{{ rule.entityName }}</span>\n              }\n              @if (rule.scope === 3 && rule.appliesToAllProperties) {\n                <span class=\"summary-chip\">{{ chill.T('13D7F183-7FB4-42F7-B6CF-DA43269331EA', 'All properties', 'Tutte le propriet\u00E0') }}</span>\n              }\n            </div>\n          </div>\n\n          <div class=\"permission-actions compact-actions\">\n            <button type=\"button\" class=\"secondary\" (click)=\"startEditingRow(rule.localId)\">\n              <app-chill-i18n-button-label [labelGuid]=\"'314E7191-5C3A-4A96-8D01-AC4C17FF757F'\" [primaryDefaultText]=\"'Edit'\" [secondaryDefaultText]=\"'Modifica'\" />\n            </button>\n\n            <button type=\"button\" class=\"secondary danger\" (click)=\"removePermissionRow(rule.localId)\">\n              <app-chill-i18n-button-label [labelGuid]=\"'704B4EC7-C971-48C7-9439-E08C2F590992'\" [primaryDefaultText]=\"'Delete'\" [secondaryDefaultText]=\"'Elimina'\" />\n            </button>\n          </div>\n        }\n      </article>\n    } @empty {\n      <div class=\"empty-state\">{{ chill.T('1801F875-1D00-4D0B-BE62-C72471A645B2', 'No permission rules for the selected target.', 'Nessuna regola permesso per la destinazione selezionata.') }}</div>\n    }\n  </div>\n\n  <button type=\"button\" class=\"secondary\" (click)=\"addPermissionRule()\">\n    <app-chill-i18n-button-label [labelGuid]=\"'B4F053A6-0B3E-43A3-9B7D-5124EF2A3952'\" [primaryDefaultText]=\"'Create permission rule'\" [secondaryDefaultText]=\"'Crea regola permesso'\" />\n  </button>\n</section>\n", styles: [":host{display:block}.permission-editor,.entity-header,.entity-toolbar,.editor-layout{display:grid;gap:1rem}.entity-toolbar{grid-template-columns:minmax(0,1fr) auto;align-items:start}.entity-toolbar__lookup{min-width:0}.entity-toolbar__actions{display:flex;gap:.75rem;flex-wrap:wrap;justify-content:end}.editor-layout{gap:1.25rem;grid-template-columns:minmax(16rem,22rem) minmax(0,1fr);align-items:start}.sidebar-card,.box-card,.empty-panel,.create-card{display:grid;gap:1rem;padding:1rem 1.25rem;border-radius:1rem;border:1px solid var(--border-color);background:var(--surface-2);color:var(--text-main)}.box-card{background:var(--surface-3);box-shadow:var(--shadow-soft)}.content-column{display:grid;gap:1rem}.selection-header,.card-header,.permission-actions,.check-item{display:flex;gap:1rem}.selection-header,.permission-actions{justify-content:space-between;align-items:start}.card-header,.check-item{flex-direction:column}.card-header h2,.card-header h3,.card-header p,.selection-header h2,.selection-header p{margin:0}.section-kicker{margin:0 0 .35rem;color:var(--accent);font-size:.78rem;font-weight:700;letter-spacing:.14em;text-transform:uppercase}.search-field,.field{display:grid;gap:.4rem}.search-field input,.field input,.field textarea,.field select{width:100%;padding:.8rem .95rem;border-radius:.8rem;border:1px solid var(--border-color);background:var(--surface-0);color:var(--text-main);font:inherit}.selector-list,.permission-list,.check-list,.box-grid{display:grid;gap:.8rem}.selector-list,.check-list{max-height:30rem;overflow:auto}.selector-item,.permission-card,.check-item{padding:.85rem .95rem;border-radius:.85rem;border:1px solid var(--border-color);background:color-mix(in srgb,var(--surface-0) 78%,var(--surface-2))}.selector-item{display:grid;gap:.2rem;text-align:left;color:var(--text-main)}.selector-item.active{border-color:var(--accent);box-shadow:inset 0 0 0 1px var(--accent)}.permission-card{display:grid;gap:.9rem}.selector-item small,.check-item small,.card-header p,.selection-header p{color:var(--text-muted)}.box-grid{grid-template-columns:repeat(2,minmax(0,1fr))}.form-grid{display:grid;gap:.85rem;grid-template-columns:repeat(2,minmax(0,1fr))}.permission-summary,.summary-main,.summary-meta{display:grid}.permission-summary{gap:.65rem}.summary-main{gap:.2rem}.summary-main strong,.summary-main span,.summary-main small{margin:0}.summary-main span,.summary-main small{color:var(--text-muted)}.summary-meta{gap:.45rem;grid-template-columns:repeat(auto-fit,minmax(8rem,max-content))}.summary-chip{display:inline-flex;align-items:center;min-height:2rem;padding:.2rem .7rem;border-radius:999px;background:var(--accent-soft);border:1px solid color-mix(in srgb,var(--accent) 22%,var(--border-color));color:var(--text-main);font-size:.88rem}.field.full-width{grid-column:1/-1}.toggle{display:flex;gap:.7rem;align-items:center}.action-row{display:flex;gap:.65rem;flex-wrap:wrap;justify-content:end}.compact-actions{justify-content:end}.check-item{display:flex;flex-direction:row;align-items:start}.empty-state,.empty-panel{color:var(--text-muted)}.notice{padding:1rem 1.25rem;border-radius:1rem;background:var(--surface-2);border:1px solid var(--border-color);color:var(--text-main)}.notice.error{color:#8b1e3f;border-color:#8b1e3f33;background:#fff0f4eb}.notice.success{color:#175b3a;border-color:#175b3a33;background:#eefff6eb}button{padding:.8rem 1rem;border-radius:.8rem;border:0;background:var(--accent);color:#fff;font:inherit;font-weight:700}button.secondary{background:var(--surface-3);color:var(--text-main)}button.danger{background:#8b1e3f}button:disabled{opacity:.55}@media(max-width:1100px){.entity-toolbar,.editor-layout,.box-grid{grid-template-columns:1fr}.entity-toolbar__actions{justify-content:start}}@media(max-width:720px){.form-grid{grid-template-columns:1fr}.selection-header,.permission-actions{flex-direction:column}.action-row{justify-content:start}}\n"] }]
        }], ctorParameters: () => [] });

class RolePermissionComponent {
    constructor() {
        this.chill = inject(ChillService);
        this.dialog = inject(WorkspaceDialogService);
        this.users = input([]);
        this.roles = input([]);
        this.roleCreated = output();
        this.roleUpdated = output();
        this.selectedRoleGuid = signal('');
        this.isLoadingDetails = signal(false);
        this.isSaving = signal(false);
        this.errorMessage = signal('');
        this.successMessage = signal('');
        this.selectedUserGuids = signal([]);
        this.permissionRows = signal([]);
        this.originalSnapshot = signal('');
        this.selectionVersion = signal(0);
        this.roleOptions = computed(() => this.roles().map((role) => ({
            id: role.guid,
            label: role.name,
            description: role.description,
            keywords: [role.name, role.description, role.guid].join(' ')
        })));
        this.selectedRole = computed(() => this.roles().find((role) => role.guid === this.selectedRoleGuid()) ?? null);
        this.hasSelection = computed(() => !!this.selectedRole());
        this.hasChanges = computed(() => {
            if (!this.selectedRole()) {
                return false;
            }
            return this.serializeSnapshot() !== this.originalSnapshot();
        });
        this.saveDisabled = computed(() => this.isSaving() || this.isLoadingDetails() || !this.selectedRole() || !this.hasChanges());
        effect(() => {
            const roles = this.roles();
            const selectedRoleGuid = this.selectedRoleGuid();
            if (roles.length === 0) {
                this.clearSelectionState();
                return;
            }
            if (selectedRoleGuid && !roles.some((role) => role.guid === selectedRoleGuid)) {
                this.clearSelectionState();
            }
        });
    }
    selectRole(roleGuid) {
        if (!roleGuid) {
            this.clearSelectionState();
            return;
        }
        if (this.selectedRoleGuid() === roleGuid) {
            return;
        }
        this.selectedRoleGuid.set(roleGuid);
        this.errorMessage.set('');
        this.successMessage.set('');
        this.loadSelectedRole(roleGuid);
    }
    toggleUser(userGuid, checked) {
        const current = new Set(this.selectedUserGuids());
        if (checked) {
            current.add(userGuid);
        }
        else {
            current.delete(userGuid);
        }
        this.selectedUserGuids.set([...current]);
    }
    updatePermissionRows(rows) {
        this.permissionRows.set(rows);
    }
    save() {
        const role = this.selectedRole();
        if (!role || this.saveDisabled()) {
            return;
        }
        this.isSaving.set(true);
        this.errorMessage.set('');
        this.successMessage.set('');
        this.chill.saveAuthRoleAccess(role.guid, this.selectedUserGuids(), this.permissionRows().map((row) => this.toPermissionPayload(row))).subscribe({
            next: (response) => {
                this.selectedUserGuids.set(response.users.map((user) => user.guid));
                this.permissionRows.set(response.permissions.map((permission) => this.toPermissionRow(permission)));
                this.originalSnapshot.set(this.serializeSnapshot());
                this.isSaving.set(false);
                this.successMessage.set(this.chill.T('F1F539A5-DB3A-4B32-B68B-9A2084AA0B6E', 'Role permissions updated.', 'Permessi ruolo aggiornati.'));
            },
            error: (error) => {
                this.isSaving.set(false);
                this.errorMessage.set(this.chill.formatError(error));
            }
        });
    }
    userLabel(user) {
        return user.displayName?.trim() || user.userName?.trim() || user.externalId?.trim() || user.guid;
    }
    async openCreateRoleDialog() {
        const { AuthRoleDialogComponent } = await Promise.resolve().then(function () { return authRoleDialog_component; });
        const result = await this.dialog.openDialog({
            title: this.chill.T('0B47EAA4-33BC-4D1C-B8C6-F75D3A5C8864', 'Create role', 'Crea ruolo'),
            component: AuthRoleDialogComponent,
            okLabel: await this.chill.TAsync('61E5DBBB-413A-449B-BE0E-B4A991FA1E39', 'Create', 'Crea')
        });
        if (result.status !== 'confirmed' || !result.value) {
            return;
        }
        this.roleCreated.emit(result.value);
        this.selectRole(result.value.guid);
        this.successMessage.set(this.chill.T('175A80C9-2A43-419F-A835-463E4A0A7BAA', 'Role created.', 'Ruolo creato.'));
    }
    async openEditRoleDialog() {
        const role = this.selectedRole();
        if (!role) {
            return;
        }
        const { AuthRoleDialogComponent } = await Promise.resolve().then(function () { return authRoleDialog_component; });
        const result = await this.dialog.openDialog({
            title: this.chill.T('49DE3A27-3C6C-4E9F-9F07-6B1FAE3DC3E4', 'Edit role', 'Modifica ruolo'),
            component: AuthRoleDialogComponent,
            okLabel: await this.chill.TAsync('62953302-B951-4FD1-BD08-4B7649A91BAF', 'Save', 'Salva'),
            inputs: {
                roleGuid: role.guid
            }
        });
        if (result.status !== 'confirmed' || !result.value) {
            return;
        }
        this.roleUpdated.emit(result.value);
        this.selectRole(result.value.guid);
        this.successMessage.set(this.chill.T('4D95B0C0-73A2-4B35-9D06-A4F9133B768E', 'Role details updated.', 'Dettagli ruolo aggiornati.'));
    }
    loadSelectedRole(roleGuid) {
        this.isLoadingDetails.set(true);
        this.permissionRows.set([]);
        this.selectedUserGuids.set([]);
        this.chill.getAuthRoleAccess(roleGuid).subscribe({
            next: (response) => {
                this.selectionVersion.update((value) => value + 1);
                this.selectedUserGuids.set(response.users.map((user) => user.guid));
                this.permissionRows.set(response.permissions.map((permission) => this.toPermissionRow(permission)));
                this.originalSnapshot.set(this.serializeSnapshot());
                this.isLoadingDetails.set(false);
            },
            error: (error) => {
                this.isLoadingDetails.set(false);
                this.errorMessage.set(this.chill.formatError(error));
            }
        });
    }
    toPermissionRow(permission) {
        return {
            localId: permission.guid || `existing-${this.selectionVersion()}-${crypto.randomUUID()}`,
            guid: permission.guid,
            effect: permission.effect,
            action: permission.action,
            scope: permission.scope,
            module: permission.module,
            entityName: permission.entityName ?? '',
            propertyName: permission.propertyName ?? '',
            appliesToAllProperties: permission.appliesToAllProperties,
            description: permission.description
        };
    }
    toPermissionPayload(row) {
        const propertyName = row.appliesToAllProperties
            ? undefined
            : row.propertyName?.trim() || undefined;
        return {
            guid: row.guid?.trim() || undefined,
            effect: row.effect,
            action: row.action,
            scope: row.scope,
            module: row.module.trim(),
            entityName: row.entityName?.trim() || '',
            propertyName,
            appliesToAllProperties: row.appliesToAllProperties,
            description: row.description.trim()
        };
    }
    serializeSnapshot() {
        return JSON.stringify({
            userGuids: [...this.selectedUserGuids()].sort(),
            permissions: this.permissionRows().map((row) => this.toPermissionPayload(row))
        });
    }
    clearSelectionState() {
        this.selectedRoleGuid.set('');
        this.selectedUserGuids.set([]);
        this.permissionRows.set([]);
        this.originalSnapshot.set('');
        this.isLoadingDetails.set(false);
    }
    static { this.ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: RolePermissionComponent, deps: [], target: i0.ɵɵFactoryTarget.Component }); }
    static { this.ɵcmp = i0.ɵɵngDeclareComponent({ minVersion: "17.0.0", version: "19.2.21", type: RolePermissionComponent, isStandalone: true, selector: "app-role-permission", inputs: { users: { classPropertyName: "users", publicName: "users", isSignal: true, isRequired: false, transformFunction: null }, roles: { classPropertyName: "roles", publicName: "roles", isSignal: true, isRequired: false, transformFunction: null } }, outputs: { roleCreated: "roleCreated", roleUpdated: "roleUpdated" }, ngImport: i0, template: "<section class=\"permission-editor\">\n  @if (errorMessage()) {\n    <div class=\"notice error\">{{ errorMessage() }}</div>\n  }\n\n  @if (successMessage()) {\n    <div class=\"notice success\">{{ successMessage() }}</div>\n  }\n\n  <header class=\"entity-header\">\n    <div class=\"card-header\">\n      <h2><app-chill-i18n-label [labelGuid]=\"'B3CC2B2C-8B89-4D4B-B5A0-88CDE26F61A6'\" [primaryDefaultText]=\"'Roles'\" [secondaryDefaultText]=\"'Ruoli'\" /></h2>\n      <p><app-chill-i18n-label [labelGuid]=\"'FF8CDA0A-D049-41AD-82E7-8A24F0E41357'\" [primaryDefaultText]=\"'Search and select a role to edit direct permissions and assigned users.'\" [secondaryDefaultText]=\"'Cerca e seleziona un ruolo per modificare permessi diretti e utenti assegnati.'\" /></p>\n    </div>\n\n    <div class=\"entity-toolbar\">\n      <app-auth-search-select\n        class=\"entity-toolbar__lookup\"\n        [options]=\"roleOptions()\"\n        [selectedId]=\"selectedRoleGuid()\"\n        [placeholder]=\"chill.T('D7906DD5-5C74-4CD1-B2D7-8D72510E1C22', 'Search and select a role', 'Cerca e seleziona un ruolo')\"\n        [emptyMessage]=\"chill.T('D93069E7-524D-44CD-B9F8-6FF6E0215D8F', 'No roles are available.', 'Nessun ruolo disponibile.')\"\n        [noResultsMessage]=\"chill.T('830FDB91-5135-4A95-BE1A-7E5B2198CB6F', 'No roles match the current search.', 'Nessun ruolo corrisponde alla ricerca corrente.')\"\n        [clearAriaLabel]=\"chill.T('34015BA4-E0CA-460E-B82B-A4E2D4D8A184', 'Clear', 'Pulisci')\"\n        (selectionChange)=\"selectRole($event)\" />\n\n      <div class=\"entity-toolbar__actions\">\n        @if (hasSelection()) {\n          <button type=\"button\" class=\"secondary\" (click)=\"openEditRoleDialog()\">\n            <app-chill-i18n-button-label [labelGuid]=\"'314E7191-5C3A-4A96-8D01-AC4C17FF757F'\" [primaryDefaultText]=\"'Edit'\" [secondaryDefaultText]=\"'Modifica'\" />\n          </button>\n        }\n\n        <button type=\"button\" (click)=\"openCreateRoleDialog()\">\n          <app-chill-i18n-button-label [labelGuid]=\"'23A5536E-8A94-4469-977C-D3BB57E5E621'\" [primaryDefaultText]=\"'Add'\" [secondaryDefaultText]=\"'Aggiungi'\" />\n        </button>\n      </div>\n    </div>\n  </header>\n\n  <section class=\"content-column\">\n    @if (isLoadingDetails()) {\n      <div class=\"notice\">{{ chill.T('EDE17330-7FF3-4455-B5B3-15539A9DBD19', 'Loading role permissions...', 'Caricamento permessi ruolo...') }}</div>\n    } @else if (!hasSelection()) {\n      <div class=\"empty-panel\">{{ chill.T('636340D8-A41D-4CE3-B0C7-9E618C475A97', 'Select a role to edit access.', 'Seleziona un ruolo per modificare l\\'accesso.') }}</div>\n    } @else {\n      <header class=\"selection-header\">\n        <div>\n          <p class=\"section-kicker\"><app-chill-i18n-label [labelGuid]=\"'13D94C61-7FBC-4C9D-A3C1-3B2422F5EFC1'\" [primaryDefaultText]=\"'Selected role'\" [secondaryDefaultText]=\"'Ruolo selezionato'\" /></p>\n          <h2>{{ selectedRole()!.name }}</h2>\n          <p>{{ selectedRole()!.description }}</p>\n        </div>\n\n        <button type=\"button\" [disabled]=\"saveDisabled()\" (click)=\"save()\">\n          @if (isSaving()) {\n            <app-chill-i18n-button-label [labelGuid]=\"'32E1A7C8-B09A-4A6B-8EA5-3C5AF84070B2'\" [primaryDefaultText]=\"'Saving...'\" [secondaryDefaultText]=\"'Salvataggio...'\" />\n          } @else {\n            <app-chill-i18n-button-label [labelGuid]=\"'E7E6D035-15D8-4F70-AE2A-23AD04F6B27E'\" [primaryDefaultText]=\"'Save role access'\" [secondaryDefaultText]=\"'Salva accesso ruolo'\" />\n          }\n        </button>\n      </header>\n\n      <div class=\"box-grid\">\n        <app-permission-editor [rows]=\"permissionRows()\" (rowsChange)=\"updatePermissionRows($event)\" />\n\n        <section class=\"box-card\">\n          <header class=\"card-header\">\n            <h3><app-chill-i18n-label [labelGuid]=\"'30CF51EC-A909-446D-97C1-2E9D980606A4'\" [primaryDefaultText]=\"'Assigned users'\" [secondaryDefaultText]=\"'Utenti assegnati'\" /></h3>\n            <p><app-chill-i18n-label [labelGuid]=\"'AEEEB4D8-470D-49B3-A3E4-2B0A3B0764D7'\" [primaryDefaultText]=\"'Choose which users belong to the selected role.'\" [secondaryDefaultText]=\"'Scegli quali utenti appartengono al ruolo selezionato.'\" /></p>\n          </header>\n\n          <div class=\"check-list\">\n            @for (user of users(); track user.guid) {\n              <label class=\"check-item\">\n                <input type=\"checkbox\" [checked]=\"selectedUserGuids().includes(user.guid)\" (change)=\"toggleUser(user.guid, $any($event.target).checked)\" />\n                <span>\n                  <strong>{{ userLabel(user) }}</strong>\n                  <small>{{ user.userName }}</small>\n                </span>\n              </label>\n            } @empty {\n              <div class=\"empty-state\">{{ chill.T('F7E403F5-F8B4-4106-9258-071CB7EF5770', 'No users are available.', 'Nessun utente disponibile.') }}</div>\n            }\n          </div>\n        </section>\n      </div>\n    }\n  </section>\n</section>\n", styles: [":host{display:block}.permission-editor,.entity-header,.entity-toolbar,.editor-layout{display:grid;gap:1rem}.entity-toolbar{grid-template-columns:minmax(0,1fr) auto;align-items:start}.entity-toolbar__lookup{min-width:0}.entity-toolbar__actions{display:flex;gap:.75rem;flex-wrap:wrap;justify-content:end}.editor-layout{gap:1.25rem;grid-template-columns:minmax(16rem,22rem) minmax(0,1fr);align-items:start}.sidebar-card,.box-card,.empty-panel,.create-card{display:grid;gap:1rem;padding:1rem 1.25rem;border-radius:1rem;border:1px solid var(--border-color);background:var(--surface-2);color:var(--text-main)}.box-card{background:var(--surface-3);box-shadow:var(--shadow-soft)}.content-column{display:grid;gap:1rem}.selection-header,.card-header,.permission-actions,.check-item{display:flex;gap:1rem}.selection-header,.permission-actions{justify-content:space-between;align-items:start}.card-header,.check-item{flex-direction:column}.card-header h2,.card-header h3,.card-header p,.selection-header h2,.selection-header p{margin:0}.section-kicker{margin:0 0 .35rem;color:var(--accent);font-size:.78rem;font-weight:700;letter-spacing:.14em;text-transform:uppercase}.search-field,.field{display:grid;gap:.4rem}.search-field input,.field input,.field textarea,.field select{width:100%;padding:.8rem .95rem;border-radius:.8rem;border:1px solid var(--border-color);background:var(--surface-0);color:var(--text-main);font:inherit}.selector-list,.permission-list,.check-list,.box-grid{display:grid;gap:.8rem}.selector-list,.check-list{max-height:30rem;overflow:auto}.selector-item,.permission-card,.check-item{padding:.85rem .95rem;border-radius:.85rem;border:1px solid var(--border-color);background:color-mix(in srgb,var(--surface-0) 78%,var(--surface-2))}.selector-item{display:grid;gap:.2rem;text-align:left;color:var(--text-main)}.selector-item.active{border-color:var(--accent);box-shadow:inset 0 0 0 1px var(--accent)}.permission-card{display:grid;gap:.9rem}.selector-item small,.check-item small,.card-header p,.selection-header p{color:var(--text-muted)}.box-grid{grid-template-columns:repeat(2,minmax(0,1fr))}.form-grid{display:grid;gap:.85rem;grid-template-columns:repeat(2,minmax(0,1fr))}.permission-summary,.summary-main,.summary-meta{display:grid}.permission-summary{gap:.65rem}.summary-main{gap:.2rem}.summary-main strong,.summary-main span,.summary-main small{margin:0}.summary-main span,.summary-main small{color:var(--text-muted)}.summary-meta{gap:.45rem;grid-template-columns:repeat(auto-fit,minmax(8rem,max-content))}.summary-chip{display:inline-flex;align-items:center;min-height:2rem;padding:.2rem .7rem;border-radius:999px;background:var(--accent-soft);border:1px solid color-mix(in srgb,var(--accent) 22%,var(--border-color));color:var(--text-main);font-size:.88rem}.field.full-width{grid-column:1/-1}.toggle{display:flex;gap:.7rem;align-items:center}.action-row{display:flex;gap:.65rem;flex-wrap:wrap;justify-content:end}.compact-actions{justify-content:end}.check-item{display:flex;flex-direction:row;align-items:start}.empty-state,.empty-panel{color:var(--text-muted)}.notice{padding:1rem 1.25rem;border-radius:1rem;background:var(--surface-2);border:1px solid var(--border-color);color:var(--text-main)}.notice.error{color:#8b1e3f;border-color:#8b1e3f33;background:#fff0f4eb}.notice.success{color:#175b3a;border-color:#175b3a33;background:#eefff6eb}button{padding:.8rem 1rem;border-radius:.8rem;border:0;background:var(--accent);color:#fff;font:inherit;font-weight:700}button.secondary{background:var(--surface-3);color:var(--text-main)}button.danger{background:#8b1e3f}button:disabled{opacity:.55}@media(max-width:1100px){.entity-toolbar,.editor-layout,.box-grid{grid-template-columns:1fr}.entity-toolbar__actions{justify-content:start}}@media(max-width:720px){.form-grid{grid-template-columns:1fr}.selection-header,.permission-actions{flex-direction:column}.action-row{justify-content:start}}\n"], dependencies: [{ kind: "ngmodule", type: CommonModule }, { kind: "component", type: PermissionEditorComponent, selector: "app-permission-editor", inputs: ["rows"], outputs: ["rowsChange"] }, { kind: "component", type: ChillI18nLabelComponent, selector: "app-chill-i18n-label", inputs: ["labelGuid", "primaryDefaultText", "secondaryDefaultText", "editable"] }, { kind: "component", type: ChillI18nButtonLabelComponent, selector: "app-chill-i18n-button-label", inputs: ["labelGuid", "primaryDefaultText", "secondaryDefaultText", "editable"] }, { kind: "component", type: AuthSearchSelectComponent, selector: "app-auth-search-select", inputs: ["options", "selectedId", "placeholder", "emptyMessage", "noResultsMessage", "clearAriaLabel"], outputs: ["selectionChange"] }, { kind: "directive", type: NoticeTransitionDirective, selector: ".notice" }] }); }
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: RolePermissionComponent, decorators: [{
            type: Component,
            args: [{ selector: 'app-role-permission', standalone: true, imports: [CommonModule, PermissionEditorComponent, ChillI18nLabelComponent, ChillI18nButtonLabelComponent, AuthSearchSelectComponent, NoticeTransitionDirective], template: "<section class=\"permission-editor\">\n  @if (errorMessage()) {\n    <div class=\"notice error\">{{ errorMessage() }}</div>\n  }\n\n  @if (successMessage()) {\n    <div class=\"notice success\">{{ successMessage() }}</div>\n  }\n\n  <header class=\"entity-header\">\n    <div class=\"card-header\">\n      <h2><app-chill-i18n-label [labelGuid]=\"'B3CC2B2C-8B89-4D4B-B5A0-88CDE26F61A6'\" [primaryDefaultText]=\"'Roles'\" [secondaryDefaultText]=\"'Ruoli'\" /></h2>\n      <p><app-chill-i18n-label [labelGuid]=\"'FF8CDA0A-D049-41AD-82E7-8A24F0E41357'\" [primaryDefaultText]=\"'Search and select a role to edit direct permissions and assigned users.'\" [secondaryDefaultText]=\"'Cerca e seleziona un ruolo per modificare permessi diretti e utenti assegnati.'\" /></p>\n    </div>\n\n    <div class=\"entity-toolbar\">\n      <app-auth-search-select\n        class=\"entity-toolbar__lookup\"\n        [options]=\"roleOptions()\"\n        [selectedId]=\"selectedRoleGuid()\"\n        [placeholder]=\"chill.T('D7906DD5-5C74-4CD1-B2D7-8D72510E1C22', 'Search and select a role', 'Cerca e seleziona un ruolo')\"\n        [emptyMessage]=\"chill.T('D93069E7-524D-44CD-B9F8-6FF6E0215D8F', 'No roles are available.', 'Nessun ruolo disponibile.')\"\n        [noResultsMessage]=\"chill.T('830FDB91-5135-4A95-BE1A-7E5B2198CB6F', 'No roles match the current search.', 'Nessun ruolo corrisponde alla ricerca corrente.')\"\n        [clearAriaLabel]=\"chill.T('34015BA4-E0CA-460E-B82B-A4E2D4D8A184', 'Clear', 'Pulisci')\"\n        (selectionChange)=\"selectRole($event)\" />\n\n      <div class=\"entity-toolbar__actions\">\n        @if (hasSelection()) {\n          <button type=\"button\" class=\"secondary\" (click)=\"openEditRoleDialog()\">\n            <app-chill-i18n-button-label [labelGuid]=\"'314E7191-5C3A-4A96-8D01-AC4C17FF757F'\" [primaryDefaultText]=\"'Edit'\" [secondaryDefaultText]=\"'Modifica'\" />\n          </button>\n        }\n\n        <button type=\"button\" (click)=\"openCreateRoleDialog()\">\n          <app-chill-i18n-button-label [labelGuid]=\"'23A5536E-8A94-4469-977C-D3BB57E5E621'\" [primaryDefaultText]=\"'Add'\" [secondaryDefaultText]=\"'Aggiungi'\" />\n        </button>\n      </div>\n    </div>\n  </header>\n\n  <section class=\"content-column\">\n    @if (isLoadingDetails()) {\n      <div class=\"notice\">{{ chill.T('EDE17330-7FF3-4455-B5B3-15539A9DBD19', 'Loading role permissions...', 'Caricamento permessi ruolo...') }}</div>\n    } @else if (!hasSelection()) {\n      <div class=\"empty-panel\">{{ chill.T('636340D8-A41D-4CE3-B0C7-9E618C475A97', 'Select a role to edit access.', 'Seleziona un ruolo per modificare l\\'accesso.') }}</div>\n    } @else {\n      <header class=\"selection-header\">\n        <div>\n          <p class=\"section-kicker\"><app-chill-i18n-label [labelGuid]=\"'13D94C61-7FBC-4C9D-A3C1-3B2422F5EFC1'\" [primaryDefaultText]=\"'Selected role'\" [secondaryDefaultText]=\"'Ruolo selezionato'\" /></p>\n          <h2>{{ selectedRole()!.name }}</h2>\n          <p>{{ selectedRole()!.description }}</p>\n        </div>\n\n        <button type=\"button\" [disabled]=\"saveDisabled()\" (click)=\"save()\">\n          @if (isSaving()) {\n            <app-chill-i18n-button-label [labelGuid]=\"'32E1A7C8-B09A-4A6B-8EA5-3C5AF84070B2'\" [primaryDefaultText]=\"'Saving...'\" [secondaryDefaultText]=\"'Salvataggio...'\" />\n          } @else {\n            <app-chill-i18n-button-label [labelGuid]=\"'E7E6D035-15D8-4F70-AE2A-23AD04F6B27E'\" [primaryDefaultText]=\"'Save role access'\" [secondaryDefaultText]=\"'Salva accesso ruolo'\" />\n          }\n        </button>\n      </header>\n\n      <div class=\"box-grid\">\n        <app-permission-editor [rows]=\"permissionRows()\" (rowsChange)=\"updatePermissionRows($event)\" />\n\n        <section class=\"box-card\">\n          <header class=\"card-header\">\n            <h3><app-chill-i18n-label [labelGuid]=\"'30CF51EC-A909-446D-97C1-2E9D980606A4'\" [primaryDefaultText]=\"'Assigned users'\" [secondaryDefaultText]=\"'Utenti assegnati'\" /></h3>\n            <p><app-chill-i18n-label [labelGuid]=\"'AEEEB4D8-470D-49B3-A3E4-2B0A3B0764D7'\" [primaryDefaultText]=\"'Choose which users belong to the selected role.'\" [secondaryDefaultText]=\"'Scegli quali utenti appartengono al ruolo selezionato.'\" /></p>\n          </header>\n\n          <div class=\"check-list\">\n            @for (user of users(); track user.guid) {\n              <label class=\"check-item\">\n                <input type=\"checkbox\" [checked]=\"selectedUserGuids().includes(user.guid)\" (change)=\"toggleUser(user.guid, $any($event.target).checked)\" />\n                <span>\n                  <strong>{{ userLabel(user) }}</strong>\n                  <small>{{ user.userName }}</small>\n                </span>\n              </label>\n            } @empty {\n              <div class=\"empty-state\">{{ chill.T('F7E403F5-F8B4-4106-9258-071CB7EF5770', 'No users are available.', 'Nessun utente disponibile.') }}</div>\n            }\n          </div>\n        </section>\n      </div>\n    }\n  </section>\n</section>\n", styles: [":host{display:block}.permission-editor,.entity-header,.entity-toolbar,.editor-layout{display:grid;gap:1rem}.entity-toolbar{grid-template-columns:minmax(0,1fr) auto;align-items:start}.entity-toolbar__lookup{min-width:0}.entity-toolbar__actions{display:flex;gap:.75rem;flex-wrap:wrap;justify-content:end}.editor-layout{gap:1.25rem;grid-template-columns:minmax(16rem,22rem) minmax(0,1fr);align-items:start}.sidebar-card,.box-card,.empty-panel,.create-card{display:grid;gap:1rem;padding:1rem 1.25rem;border-radius:1rem;border:1px solid var(--border-color);background:var(--surface-2);color:var(--text-main)}.box-card{background:var(--surface-3);box-shadow:var(--shadow-soft)}.content-column{display:grid;gap:1rem}.selection-header,.card-header,.permission-actions,.check-item{display:flex;gap:1rem}.selection-header,.permission-actions{justify-content:space-between;align-items:start}.card-header,.check-item{flex-direction:column}.card-header h2,.card-header h3,.card-header p,.selection-header h2,.selection-header p{margin:0}.section-kicker{margin:0 0 .35rem;color:var(--accent);font-size:.78rem;font-weight:700;letter-spacing:.14em;text-transform:uppercase}.search-field,.field{display:grid;gap:.4rem}.search-field input,.field input,.field textarea,.field select{width:100%;padding:.8rem .95rem;border-radius:.8rem;border:1px solid var(--border-color);background:var(--surface-0);color:var(--text-main);font:inherit}.selector-list,.permission-list,.check-list,.box-grid{display:grid;gap:.8rem}.selector-list,.check-list{max-height:30rem;overflow:auto}.selector-item,.permission-card,.check-item{padding:.85rem .95rem;border-radius:.85rem;border:1px solid var(--border-color);background:color-mix(in srgb,var(--surface-0) 78%,var(--surface-2))}.selector-item{display:grid;gap:.2rem;text-align:left;color:var(--text-main)}.selector-item.active{border-color:var(--accent);box-shadow:inset 0 0 0 1px var(--accent)}.permission-card{display:grid;gap:.9rem}.selector-item small,.check-item small,.card-header p,.selection-header p{color:var(--text-muted)}.box-grid{grid-template-columns:repeat(2,minmax(0,1fr))}.form-grid{display:grid;gap:.85rem;grid-template-columns:repeat(2,minmax(0,1fr))}.permission-summary,.summary-main,.summary-meta{display:grid}.permission-summary{gap:.65rem}.summary-main{gap:.2rem}.summary-main strong,.summary-main span,.summary-main small{margin:0}.summary-main span,.summary-main small{color:var(--text-muted)}.summary-meta{gap:.45rem;grid-template-columns:repeat(auto-fit,minmax(8rem,max-content))}.summary-chip{display:inline-flex;align-items:center;min-height:2rem;padding:.2rem .7rem;border-radius:999px;background:var(--accent-soft);border:1px solid color-mix(in srgb,var(--accent) 22%,var(--border-color));color:var(--text-main);font-size:.88rem}.field.full-width{grid-column:1/-1}.toggle{display:flex;gap:.7rem;align-items:center}.action-row{display:flex;gap:.65rem;flex-wrap:wrap;justify-content:end}.compact-actions{justify-content:end}.check-item{display:flex;flex-direction:row;align-items:start}.empty-state,.empty-panel{color:var(--text-muted)}.notice{padding:1rem 1.25rem;border-radius:1rem;background:var(--surface-2);border:1px solid var(--border-color);color:var(--text-main)}.notice.error{color:#8b1e3f;border-color:#8b1e3f33;background:#fff0f4eb}.notice.success{color:#175b3a;border-color:#175b3a33;background:#eefff6eb}button{padding:.8rem 1rem;border-radius:.8rem;border:0;background:var(--accent);color:#fff;font:inherit;font-weight:700}button.secondary{background:var(--surface-3);color:var(--text-main)}button.danger{background:#8b1e3f}button:disabled{opacity:.55}@media(max-width:1100px){.entity-toolbar,.editor-layout,.box-grid{grid-template-columns:1fr}.entity-toolbar__actions{justify-content:start}}@media(max-width:720px){.form-grid{grid-template-columns:1fr}.selection-header,.permission-actions{flex-direction:column}.action-row{justify-content:start}}\n"] }]
        }], ctorParameters: () => [] });

class UserPermissionComponent {
    constructor() {
        this.chill = inject(ChillService);
        this.dialog = inject(WorkspaceDialogService);
        this.users = input([]);
        this.roles = input([]);
        this.userCreated = output();
        this.userUpdated = output();
        this.selectedUserGuid = signal('');
        this.isLoadingDetails = signal(false);
        this.isSaving = signal(false);
        this.errorMessage = signal('');
        this.successMessage = signal('');
        this.selectedRoleGuids = signal([]);
        this.permissionRows = signal([]);
        this.originalSnapshot = signal('');
        this.selectionVersion = signal(0);
        this.userOptions = computed(() => this.users().map((user) => ({
            id: user.guid,
            label: this.userLabel(user),
            description: user.userName,
            keywords: [user.displayName, user.userName, user.externalId, user.guid].join(' ')
        })));
        this.selectedUser = computed(() => this.users().find((user) => user.guid === this.selectedUserGuid()) ?? null);
        this.hasSelection = computed(() => !!this.selectedUser());
        this.hasChanges = computed(() => {
            if (!this.selectedUser()) {
                return false;
            }
            return this.serializeSnapshot() !== this.originalSnapshot();
        });
        this.saveDisabled = computed(() => this.isSaving() || this.isLoadingDetails() || !this.selectedUser() || !this.hasChanges());
        effect(() => {
            const users = this.users();
            const selectedUserGuid = this.selectedUserGuid();
            if (users.length === 0) {
                this.clearSelectionState();
                return;
            }
            if (selectedUserGuid && !users.some((user) => user.guid === selectedUserGuid)) {
                this.clearSelectionState();
            }
        });
    }
    selectUser(userGuid) {
        if (!userGuid) {
            this.clearSelectionState();
            return;
        }
        if (this.selectedUserGuid() === userGuid) {
            return;
        }
        this.selectedUserGuid.set(userGuid);
        this.errorMessage.set('');
        this.successMessage.set('');
        this.loadSelectedUser(userGuid);
    }
    toggleRole(roleGuid, checked) {
        const current = new Set(this.selectedRoleGuids());
        if (checked) {
            current.add(roleGuid);
        }
        else {
            current.delete(roleGuid);
        }
        this.selectedRoleGuids.set([...current]);
    }
    updatePermissionRows(rows) {
        this.permissionRows.set(rows);
    }
    save() {
        const user = this.selectedUser();
        if (!user || this.saveDisabled()) {
            return;
        }
        this.isSaving.set(true);
        this.errorMessage.set('');
        this.successMessage.set('');
        this.chill.saveAuthUserAccess(user.guid, this.selectedRoleGuids(), this.permissionRows().map((row) => this.toPermissionPayload(row))).subscribe({
            next: (response) => {
                this.selectedRoleGuids.set(response.roles.map((role) => role.guid));
                this.permissionRows.set(response.permissions.map((permission) => this.toPermissionRow(permission)));
                this.originalSnapshot.set(this.serializeSnapshot());
                this.isSaving.set(false);
                this.successMessage.set(this.chill.T('7F2F0CE1-88D9-4EF7-B7EA-2A729986AB27', 'User permissions updated.', 'Permessi utente aggiornati.'));
            },
            error: (error) => {
                this.isSaving.set(false);
                this.errorMessage.set(this.chill.formatError(error));
            }
        });
    }
    userLabel(user) {
        return user.displayName?.trim() || user.userName?.trim() || user.externalId?.trim() || user.guid;
    }
    async openCreateUserDialog() {
        const { AuthUserDialogComponent } = await Promise.resolve().then(function () { return authUserDialog_component; });
        const result = await this.dialog.openDialog({
            title: this.chill.T('9E2BFF8D-BF6C-4C8D-BE6A-972425BA63DB', 'New user', 'Nuovo utente'),
            component: AuthUserDialogComponent,
            okLabel: await this.chill.TAsync('61E5DBBB-413A-449B-BE0E-B4A991FA1E39', 'Create', 'Crea')
        });
        if (result.status !== 'confirmed' || !result.value) {
            return;
        }
        this.userCreated.emit(result.value);
        this.selectUser(result.value.guid);
        this.successMessage.set(this.chill.T('A92C6256-EA89-4D6D-84F7-CF2423AF93D2', 'User created.', 'Utente creato.'));
    }
    async openEditUserDialog() {
        const user = this.selectedUser();
        if (!user) {
            return;
        }
        const { AuthUserDialogComponent } = await Promise.resolve().then(function () { return authUserDialog_component; });
        const result = await this.dialog.openDialog({
            title: this.chill.T('C082531D-0F50-49D4-B677-C752D1A4DAA4', 'Edit user', 'Modifica utente'),
            component: AuthUserDialogComponent,
            okLabel: await this.chill.TAsync('62953302-B951-4FD1-BD08-4B7649A91BAF', 'Save', 'Salva'),
            inputs: {
                userGuid: user.guid
            }
        });
        if (result.status !== 'confirmed' || !result.value) {
            return;
        }
        this.userUpdated.emit(result.value);
        this.selectUser(result.value.guid);
        this.successMessage.set(this.chill.T('5D2A2B57-7E48-417D-A886-AB5610A35A17', 'User details updated.', 'Dettagli utente aggiornati.'));
    }
    loadSelectedUser(userGuid) {
        this.isLoadingDetails.set(true);
        this.permissionRows.set([]);
        this.selectedRoleGuids.set([]);
        this.chill.getAuthUserAccess(userGuid).subscribe({
            next: (response) => {
                this.selectionVersion.update((value) => value + 1);
                this.selectedRoleGuids.set(response.roles.map((role) => role.guid));
                this.permissionRows.set(response.permissions.map((permission) => this.toPermissionRow(permission)));
                this.originalSnapshot.set(this.serializeSnapshot());
                this.isLoadingDetails.set(false);
            },
            error: (error) => {
                this.isLoadingDetails.set(false);
                this.errorMessage.set(this.chill.formatError(error));
            }
        });
    }
    toPermissionRow(permission) {
        return {
            localId: permission.guid || `existing-${this.selectionVersion()}-${crypto.randomUUID()}`,
            guid: permission.guid,
            effect: permission.effect,
            action: permission.action,
            scope: permission.scope,
            module: permission.module,
            entityName: permission.entityName ?? '',
            propertyName: permission.propertyName ?? '',
            appliesToAllProperties: permission.appliesToAllProperties,
            description: permission.description
        };
    }
    toPermissionPayload(row) {
        const propertyName = row.appliesToAllProperties
            ? undefined
            : row.propertyName?.trim() || undefined;
        return {
            guid: row.guid?.trim() || undefined,
            effect: row.effect,
            action: row.action,
            scope: row.scope,
            module: row.module.trim(),
            entityName: row.entityName?.trim() || '',
            propertyName,
            appliesToAllProperties: row.appliesToAllProperties,
            description: row.description.trim()
        };
    }
    serializeSnapshot() {
        return JSON.stringify({
            roleGuids: [...this.selectedRoleGuids()].sort(),
            permissions: this.permissionRows().map((row) => this.toPermissionPayload(row))
        });
    }
    clearSelectionState() {
        this.selectedUserGuid.set('');
        this.selectedRoleGuids.set([]);
        this.permissionRows.set([]);
        this.originalSnapshot.set('');
        this.isLoadingDetails.set(false);
    }
    static { this.ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: UserPermissionComponent, deps: [], target: i0.ɵɵFactoryTarget.Component }); }
    static { this.ɵcmp = i0.ɵɵngDeclareComponent({ minVersion: "17.0.0", version: "19.2.21", type: UserPermissionComponent, isStandalone: true, selector: "app-user-permission", inputs: { users: { classPropertyName: "users", publicName: "users", isSignal: true, isRequired: false, transformFunction: null }, roles: { classPropertyName: "roles", publicName: "roles", isSignal: true, isRequired: false, transformFunction: null } }, outputs: { userCreated: "userCreated", userUpdated: "userUpdated" }, ngImport: i0, template: "<section class=\"permission-editor\">\n  @if (errorMessage()) {\n    <div class=\"notice error\">{{ errorMessage() }}</div>\n  }\n\n  @if (successMessage()) {\n    <div class=\"notice success\">{{ successMessage() }}</div>\n  }\n\n  <header class=\"entity-header\">\n    <div class=\"card-header\">\n      <h2><app-chill-i18n-label [labelGuid]=\"'8455C9A5-BAC8-457C-B726-F79DA6D758DF'\" [primaryDefaultText]=\"'Users'\" [secondaryDefaultText]=\"'Utenti'\" /></h2>\n      <p><app-chill-i18n-label [labelGuid]=\"'B8DAEDE3-34D6-4DDC-879D-70A32294DFF7'\" [primaryDefaultText]=\"'Search and select a user to edit direct permissions and assigned roles.'\" [secondaryDefaultText]=\"'Cerca e seleziona un utente per modificare permessi diretti e ruoli assegnati.'\" /></p>\n    </div>\n\n    <div class=\"entity-toolbar\">\n      <app-auth-search-select\n        class=\"entity-toolbar__lookup\"\n        [options]=\"userOptions()\"\n        [selectedId]=\"selectedUserGuid()\"\n        [placeholder]=\"chill.T('F513F0F0-ACD9-47D6-8251-892B4AA3A21E', 'Search and select a user', 'Cerca e seleziona un utente')\"\n        [emptyMessage]=\"chill.T('64B21C60-B790-4E38-8E08-B8C2D8D8868A', 'No users are available.', 'Nessun utente disponibile.')\"\n        [noResultsMessage]=\"chill.T('14C74A0F-C4A8-4CAE-8F23-9344EA450053', 'No users match the current search.', 'Nessun utente corrisponde alla ricerca corrente.')\"\n        [clearAriaLabel]=\"chill.T('34015BA4-E0CA-460E-B82B-A4E2D4D8A184', 'Clear', 'Pulisci')\"\n        (selectionChange)=\"selectUser($event)\" />\n\n      <div class=\"entity-toolbar__actions\">\n        @if (hasSelection()) {\n          <button type=\"button\" class=\"secondary\" (click)=\"openEditUserDialog()\">\n            <app-chill-i18n-button-label [labelGuid]=\"'314E7191-5C3A-4A96-8D01-AC4C17FF757F'\" [primaryDefaultText]=\"'Edit'\" [secondaryDefaultText]=\"'Modifica'\" />\n          </button>\n        }\n\n        <button type=\"button\" (click)=\"openCreateUserDialog()\">\n          <app-chill-i18n-button-label [labelGuid]=\"'23A5536E-8A94-4469-977C-D3BB57E5E621'\" [primaryDefaultText]=\"'Add'\" [secondaryDefaultText]=\"'Aggiungi'\" />\n        </button>\n      </div>\n    </div>\n  </header>\n\n  <section class=\"content-column\">\n    @if (isLoadingDetails()) {\n      <div class=\"notice\">{{ chill.T('B6A1E50E-D1A0-4A47-AC66-25801770A5DD', 'Loading user permissions...', 'Caricamento permessi utente...') }}</div>\n    } @else if (!hasSelection()) {\n      <div class=\"empty-panel\">{{ chill.T('6C31D890-EA80-4180-8C0A-8B369E3D8CDD', 'Select a user to edit access.', 'Seleziona un utente per modificare l\\'accesso.') }}</div>\n    } @else {\n      <header class=\"selection-header\">\n        <div>\n          <p class=\"section-kicker\"><app-chill-i18n-label [labelGuid]=\"'EE9FBF12-1D92-477C-BEF5-0A2787DE93D0'\" [primaryDefaultText]=\"'Selected user'\" [secondaryDefaultText]=\"'Utente selezionato'\" /></p>\n          <h2>{{ userLabel(selectedUser()!) }}</h2>\n          <p>{{ selectedUser()!.userName }}</p>\n        </div>\n\n        <button type=\"button\" [disabled]=\"saveDisabled()\" (click)=\"save()\">\n          @if (isSaving()) {\n            <app-chill-i18n-button-label [labelGuid]=\"'32E1A7C8-B09A-4A6B-8EA5-3C5AF84070B2'\" [primaryDefaultText]=\"'Saving...'\" [secondaryDefaultText]=\"'Salvataggio...'\" />\n          } @else {\n            <app-chill-i18n-button-label [labelGuid]=\"'D9938C0B-5D36-4702-BB53-E46D6154D2FD'\" [primaryDefaultText]=\"'Save user access'\" [secondaryDefaultText]=\"'Salva accesso utente'\" />\n          }\n        </button>\n      </header>\n\n      <div class=\"box-grid\">\n        <app-permission-editor [rows]=\"permissionRows()\" (rowsChange)=\"updatePermissionRows($event)\" />\n\n        <section class=\"box-card\">\n          <header class=\"card-header\">\n            <h3><app-chill-i18n-label [labelGuid]=\"'AB54AD2A-2A88-4AF0-8387-39AA4A08F9F6'\" [primaryDefaultText]=\"'Assigned roles'\" [secondaryDefaultText]=\"'Ruoli assegnati'\" /></h3>\n            <p><app-chill-i18n-label [labelGuid]=\"'5A697E78-7AE6-4098-95E5-7E3E934D1B55'\" [primaryDefaultText]=\"'Choose which roles are assigned to the selected user.'\" [secondaryDefaultText]='\"Scegli quali ruoli sono assegnati all&apos;utente selezionato.\"' /></p>\n          </header>\n\n          <div class=\"check-list\">\n            @for (role of roles(); track role.guid) {\n              <label class=\"check-item\">\n                <input type=\"checkbox\" [checked]=\"selectedRoleGuids().includes(role.guid)\" (change)=\"toggleRole(role.guid, $any($event.target).checked)\" />\n                <span>\n                  <strong>{{ role.name }}</strong>\n                  <small>{{ role.description }}</small>\n                </span>\n              </label>\n            } @empty {\n              <div class=\"empty-state\">{{ chill.T('A15A13D4-F9BA-4D8A-B2F6-1F531B82FECC', 'No roles are available.', 'Nessun ruolo disponibile.') }}</div>\n            }\n          </div>\n        </section>\n      </div>\n    }\n  </section>\n</section>\n", styles: [":host{display:block}.permission-editor,.entity-header,.entity-toolbar,.editor-layout{display:grid;gap:1rem}.entity-toolbar{grid-template-columns:minmax(0,1fr) auto;align-items:start}.entity-toolbar__lookup{min-width:0}.entity-toolbar__actions{display:flex;gap:.75rem;flex-wrap:wrap;justify-content:end}.editor-layout{gap:1.25rem;grid-template-columns:minmax(16rem,22rem) minmax(0,1fr);align-items:start}.sidebar-card,.box-card,.empty-panel,.create-card{display:grid;gap:1rem;padding:1rem 1.25rem;border-radius:1rem;border:1px solid var(--border-color);background:var(--surface-2);color:var(--text-main)}.box-card{background:var(--surface-3);box-shadow:var(--shadow-soft)}.content-column{display:grid;gap:1rem}.selection-header,.card-header,.permission-actions,.check-item{display:flex;gap:1rem}.selection-header,.permission-actions{justify-content:space-between;align-items:start}.card-header,.check-item{flex-direction:column}.card-header h2,.card-header h3,.card-header p,.selection-header h2,.selection-header p{margin:0}.section-kicker{margin:0 0 .35rem;color:var(--accent);font-size:.78rem;font-weight:700;letter-spacing:.14em;text-transform:uppercase}.search-field,.field{display:grid;gap:.4rem}.search-field input,.field input,.field textarea,.field select{width:100%;padding:.8rem .95rem;border-radius:.8rem;border:1px solid var(--border-color);background:var(--surface-0);color:var(--text-main);font:inherit}.selector-list,.permission-list,.check-list,.box-grid{display:grid;gap:.8rem}.selector-list,.check-list{max-height:30rem;overflow:auto}.selector-item,.permission-card,.check-item{padding:.85rem .95rem;border-radius:.85rem;border:1px solid var(--border-color);background:color-mix(in srgb,var(--surface-0) 78%,var(--surface-2))}.selector-item{display:grid;gap:.2rem;text-align:left;color:var(--text-main)}.selector-item.active{border-color:var(--accent);box-shadow:inset 0 0 0 1px var(--accent)}.permission-card{display:grid;gap:.9rem}.selector-item small,.check-item small,.card-header p,.selection-header p{color:var(--text-muted)}.box-grid{grid-template-columns:repeat(2,minmax(0,1fr))}.form-grid{display:grid;gap:.85rem;grid-template-columns:repeat(2,minmax(0,1fr))}.permission-summary,.summary-main,.summary-meta{display:grid}.permission-summary{gap:.65rem}.summary-main{gap:.2rem}.summary-main strong,.summary-main span,.summary-main small{margin:0}.summary-main span,.summary-main small{color:var(--text-muted)}.summary-meta{gap:.45rem;grid-template-columns:repeat(auto-fit,minmax(8rem,max-content))}.summary-chip{display:inline-flex;align-items:center;min-height:2rem;padding:.2rem .7rem;border-radius:999px;background:var(--accent-soft);border:1px solid color-mix(in srgb,var(--accent) 22%,var(--border-color));color:var(--text-main);font-size:.88rem}.field.full-width{grid-column:1/-1}.toggle{display:flex;gap:.7rem;align-items:center}.action-row{display:flex;gap:.65rem;flex-wrap:wrap;justify-content:end}.compact-actions{justify-content:end}.check-item{display:flex;flex-direction:row;align-items:start}.empty-state,.empty-panel{color:var(--text-muted)}.notice{padding:1rem 1.25rem;border-radius:1rem;background:var(--surface-2);border:1px solid var(--border-color);color:var(--text-main)}.notice.error{color:#8b1e3f;border-color:#8b1e3f33;background:#fff0f4eb}.notice.success{color:#175b3a;border-color:#175b3a33;background:#eefff6eb}button{padding:.8rem 1rem;border-radius:.8rem;border:0;background:var(--accent);color:#fff;font:inherit;font-weight:700}button.secondary{background:var(--surface-3);color:var(--text-main)}button.danger{background:#8b1e3f}button:disabled{opacity:.55}@media(max-width:1100px){.entity-toolbar,.editor-layout,.box-grid{grid-template-columns:1fr}.entity-toolbar__actions{justify-content:start}}@media(max-width:720px){.form-grid{grid-template-columns:1fr}.selection-header,.permission-actions{flex-direction:column}.action-row{justify-content:start}}\n"], dependencies: [{ kind: "ngmodule", type: CommonModule }, { kind: "component", type: PermissionEditorComponent, selector: "app-permission-editor", inputs: ["rows"], outputs: ["rowsChange"] }, { kind: "component", type: ChillI18nLabelComponent, selector: "app-chill-i18n-label", inputs: ["labelGuid", "primaryDefaultText", "secondaryDefaultText", "editable"] }, { kind: "component", type: ChillI18nButtonLabelComponent, selector: "app-chill-i18n-button-label", inputs: ["labelGuid", "primaryDefaultText", "secondaryDefaultText", "editable"] }, { kind: "component", type: AuthSearchSelectComponent, selector: "app-auth-search-select", inputs: ["options", "selectedId", "placeholder", "emptyMessage", "noResultsMessage", "clearAriaLabel"], outputs: ["selectionChange"] }, { kind: "directive", type: NoticeTransitionDirective, selector: ".notice" }] }); }
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: UserPermissionComponent, decorators: [{
            type: Component,
            args: [{ selector: 'app-user-permission', standalone: true, imports: [CommonModule, PermissionEditorComponent, ChillI18nLabelComponent, ChillI18nButtonLabelComponent, AuthSearchSelectComponent, NoticeTransitionDirective], template: "<section class=\"permission-editor\">\n  @if (errorMessage()) {\n    <div class=\"notice error\">{{ errorMessage() }}</div>\n  }\n\n  @if (successMessage()) {\n    <div class=\"notice success\">{{ successMessage() }}</div>\n  }\n\n  <header class=\"entity-header\">\n    <div class=\"card-header\">\n      <h2><app-chill-i18n-label [labelGuid]=\"'8455C9A5-BAC8-457C-B726-F79DA6D758DF'\" [primaryDefaultText]=\"'Users'\" [secondaryDefaultText]=\"'Utenti'\" /></h2>\n      <p><app-chill-i18n-label [labelGuid]=\"'B8DAEDE3-34D6-4DDC-879D-70A32294DFF7'\" [primaryDefaultText]=\"'Search and select a user to edit direct permissions and assigned roles.'\" [secondaryDefaultText]=\"'Cerca e seleziona un utente per modificare permessi diretti e ruoli assegnati.'\" /></p>\n    </div>\n\n    <div class=\"entity-toolbar\">\n      <app-auth-search-select\n        class=\"entity-toolbar__lookup\"\n        [options]=\"userOptions()\"\n        [selectedId]=\"selectedUserGuid()\"\n        [placeholder]=\"chill.T('F513F0F0-ACD9-47D6-8251-892B4AA3A21E', 'Search and select a user', 'Cerca e seleziona un utente')\"\n        [emptyMessage]=\"chill.T('64B21C60-B790-4E38-8E08-B8C2D8D8868A', 'No users are available.', 'Nessun utente disponibile.')\"\n        [noResultsMessage]=\"chill.T('14C74A0F-C4A8-4CAE-8F23-9344EA450053', 'No users match the current search.', 'Nessun utente corrisponde alla ricerca corrente.')\"\n        [clearAriaLabel]=\"chill.T('34015BA4-E0CA-460E-B82B-A4E2D4D8A184', 'Clear', 'Pulisci')\"\n        (selectionChange)=\"selectUser($event)\" />\n\n      <div class=\"entity-toolbar__actions\">\n        @if (hasSelection()) {\n          <button type=\"button\" class=\"secondary\" (click)=\"openEditUserDialog()\">\n            <app-chill-i18n-button-label [labelGuid]=\"'314E7191-5C3A-4A96-8D01-AC4C17FF757F'\" [primaryDefaultText]=\"'Edit'\" [secondaryDefaultText]=\"'Modifica'\" />\n          </button>\n        }\n\n        <button type=\"button\" (click)=\"openCreateUserDialog()\">\n          <app-chill-i18n-button-label [labelGuid]=\"'23A5536E-8A94-4469-977C-D3BB57E5E621'\" [primaryDefaultText]=\"'Add'\" [secondaryDefaultText]=\"'Aggiungi'\" />\n        </button>\n      </div>\n    </div>\n  </header>\n\n  <section class=\"content-column\">\n    @if (isLoadingDetails()) {\n      <div class=\"notice\">{{ chill.T('B6A1E50E-D1A0-4A47-AC66-25801770A5DD', 'Loading user permissions...', 'Caricamento permessi utente...') }}</div>\n    } @else if (!hasSelection()) {\n      <div class=\"empty-panel\">{{ chill.T('6C31D890-EA80-4180-8C0A-8B369E3D8CDD', 'Select a user to edit access.', 'Seleziona un utente per modificare l\\'accesso.') }}</div>\n    } @else {\n      <header class=\"selection-header\">\n        <div>\n          <p class=\"section-kicker\"><app-chill-i18n-label [labelGuid]=\"'EE9FBF12-1D92-477C-BEF5-0A2787DE93D0'\" [primaryDefaultText]=\"'Selected user'\" [secondaryDefaultText]=\"'Utente selezionato'\" /></p>\n          <h2>{{ userLabel(selectedUser()!) }}</h2>\n          <p>{{ selectedUser()!.userName }}</p>\n        </div>\n\n        <button type=\"button\" [disabled]=\"saveDisabled()\" (click)=\"save()\">\n          @if (isSaving()) {\n            <app-chill-i18n-button-label [labelGuid]=\"'32E1A7C8-B09A-4A6B-8EA5-3C5AF84070B2'\" [primaryDefaultText]=\"'Saving...'\" [secondaryDefaultText]=\"'Salvataggio...'\" />\n          } @else {\n            <app-chill-i18n-button-label [labelGuid]=\"'D9938C0B-5D36-4702-BB53-E46D6154D2FD'\" [primaryDefaultText]=\"'Save user access'\" [secondaryDefaultText]=\"'Salva accesso utente'\" />\n          }\n        </button>\n      </header>\n\n      <div class=\"box-grid\">\n        <app-permission-editor [rows]=\"permissionRows()\" (rowsChange)=\"updatePermissionRows($event)\" />\n\n        <section class=\"box-card\">\n          <header class=\"card-header\">\n            <h3><app-chill-i18n-label [labelGuid]=\"'AB54AD2A-2A88-4AF0-8387-39AA4A08F9F6'\" [primaryDefaultText]=\"'Assigned roles'\" [secondaryDefaultText]=\"'Ruoli assegnati'\" /></h3>\n            <p><app-chill-i18n-label [labelGuid]=\"'5A697E78-7AE6-4098-95E5-7E3E934D1B55'\" [primaryDefaultText]=\"'Choose which roles are assigned to the selected user.'\" [secondaryDefaultText]='\"Scegli quali ruoli sono assegnati all&apos;utente selezionato.\"' /></p>\n          </header>\n\n          <div class=\"check-list\">\n            @for (role of roles(); track role.guid) {\n              <label class=\"check-item\">\n                <input type=\"checkbox\" [checked]=\"selectedRoleGuids().includes(role.guid)\" (change)=\"toggleRole(role.guid, $any($event.target).checked)\" />\n                <span>\n                  <strong>{{ role.name }}</strong>\n                  <small>{{ role.description }}</small>\n                </span>\n              </label>\n            } @empty {\n              <div class=\"empty-state\">{{ chill.T('A15A13D4-F9BA-4D8A-B2F6-1F531B82FECC', 'No roles are available.', 'Nessun ruolo disponibile.') }}</div>\n            }\n          </div>\n        </section>\n      </div>\n    }\n  </section>\n</section>\n", styles: [":host{display:block}.permission-editor,.entity-header,.entity-toolbar,.editor-layout{display:grid;gap:1rem}.entity-toolbar{grid-template-columns:minmax(0,1fr) auto;align-items:start}.entity-toolbar__lookup{min-width:0}.entity-toolbar__actions{display:flex;gap:.75rem;flex-wrap:wrap;justify-content:end}.editor-layout{gap:1.25rem;grid-template-columns:minmax(16rem,22rem) minmax(0,1fr);align-items:start}.sidebar-card,.box-card,.empty-panel,.create-card{display:grid;gap:1rem;padding:1rem 1.25rem;border-radius:1rem;border:1px solid var(--border-color);background:var(--surface-2);color:var(--text-main)}.box-card{background:var(--surface-3);box-shadow:var(--shadow-soft)}.content-column{display:grid;gap:1rem}.selection-header,.card-header,.permission-actions,.check-item{display:flex;gap:1rem}.selection-header,.permission-actions{justify-content:space-between;align-items:start}.card-header,.check-item{flex-direction:column}.card-header h2,.card-header h3,.card-header p,.selection-header h2,.selection-header p{margin:0}.section-kicker{margin:0 0 .35rem;color:var(--accent);font-size:.78rem;font-weight:700;letter-spacing:.14em;text-transform:uppercase}.search-field,.field{display:grid;gap:.4rem}.search-field input,.field input,.field textarea,.field select{width:100%;padding:.8rem .95rem;border-radius:.8rem;border:1px solid var(--border-color);background:var(--surface-0);color:var(--text-main);font:inherit}.selector-list,.permission-list,.check-list,.box-grid{display:grid;gap:.8rem}.selector-list,.check-list{max-height:30rem;overflow:auto}.selector-item,.permission-card,.check-item{padding:.85rem .95rem;border-radius:.85rem;border:1px solid var(--border-color);background:color-mix(in srgb,var(--surface-0) 78%,var(--surface-2))}.selector-item{display:grid;gap:.2rem;text-align:left;color:var(--text-main)}.selector-item.active{border-color:var(--accent);box-shadow:inset 0 0 0 1px var(--accent)}.permission-card{display:grid;gap:.9rem}.selector-item small,.check-item small,.card-header p,.selection-header p{color:var(--text-muted)}.box-grid{grid-template-columns:repeat(2,minmax(0,1fr))}.form-grid{display:grid;gap:.85rem;grid-template-columns:repeat(2,minmax(0,1fr))}.permission-summary,.summary-main,.summary-meta{display:grid}.permission-summary{gap:.65rem}.summary-main{gap:.2rem}.summary-main strong,.summary-main span,.summary-main small{margin:0}.summary-main span,.summary-main small{color:var(--text-muted)}.summary-meta{gap:.45rem;grid-template-columns:repeat(auto-fit,minmax(8rem,max-content))}.summary-chip{display:inline-flex;align-items:center;min-height:2rem;padding:.2rem .7rem;border-radius:999px;background:var(--accent-soft);border:1px solid color-mix(in srgb,var(--accent) 22%,var(--border-color));color:var(--text-main);font-size:.88rem}.field.full-width{grid-column:1/-1}.toggle{display:flex;gap:.7rem;align-items:center}.action-row{display:flex;gap:.65rem;flex-wrap:wrap;justify-content:end}.compact-actions{justify-content:end}.check-item{display:flex;flex-direction:row;align-items:start}.empty-state,.empty-panel{color:var(--text-muted)}.notice{padding:1rem 1.25rem;border-radius:1rem;background:var(--surface-2);border:1px solid var(--border-color);color:var(--text-main)}.notice.error{color:#8b1e3f;border-color:#8b1e3f33;background:#fff0f4eb}.notice.success{color:#175b3a;border-color:#175b3a33;background:#eefff6eb}button{padding:.8rem 1rem;border-radius:.8rem;border:0;background:var(--accent);color:#fff;font:inherit;font-weight:700}button.secondary{background:var(--surface-3);color:var(--text-main)}button.danger{background:#8b1e3f}button:disabled{opacity:.55}@media(max-width:1100px){.entity-toolbar,.editor-layout,.box-grid{grid-template-columns:1fr}.entity-toolbar__actions{justify-content:start}}@media(max-width:720px){.form-grid{grid-template-columns:1fr}.selection-header,.permission-actions{flex-direction:column}.action-row{justify-content:start}}\n"] }]
        }], ctorParameters: () => [] });

class PermissionsPageComponent {
    static getComponentConfigurationJsonExample() {
        return {};
    }
    constructor() {
        this.chill = inject(ChillService);
        this.toolbar = inject(WorkspaceToolbarService);
        this.visible = input(true);
        this.toolbarScope = input('workspace');
        this.isLoading = signal(true);
        this.errorMessage = signal('');
        this.activeSection = signal('users');
        this.users = signal([]);
        this.roles = signal([]);
        this.canManagePermissions = signal(false);
        this.currentUser = computed(() => {
            const session = this.chill.session();
            const normalizedUserName = session?.userName?.trim().toLowerCase() ?? '';
            const normalizedUserId = session?.userId?.trim() ?? '';
            return this.users().find((user) => user.guid === normalizedUserId
                || user.userName.trim().toLowerCase() === normalizedUserName) ?? null;
        });
        effect(() => {
            const toolbarScope = this.toolbarScope();
            if (!this.visible() || !this.canManagePermissions()) {
                this.toolbar.clearButtons(toolbarScope);
                return;
            }
            const activeSection = this.activeSection();
            this.toolbar.setButtons([
                {
                    id: 'permissions-users',
                    labelGuid: '8455C9A5-BAC8-457C-B726-F79DA6D758DF',
                    primaryDefaultText: 'Users',
                    secondaryDefaultText: 'Utenti',
                    ariaLabel: this.chill.T('8455C9A5-BAC8-457C-B726-F79DA6D758DF', 'Users', 'Utenti'),
                    action: () => this.setActiveSection('users'),
                    disabled: activeSection === 'users'
                },
                {
                    id: 'permissions-roles',
                    labelGuid: 'B3CC2B2C-8B89-4D4B-B5A0-88CDE26F61A6',
                    primaryDefaultText: 'Roles',
                    secondaryDefaultText: 'Ruoli',
                    ariaLabel: this.chill.T('B3CC2B2C-8B89-4D4B-B5A0-88CDE26F61A6', 'Roles', 'Ruoli'),
                    action: () => this.setActiveSection('roles'),
                    disabled: activeSection === 'roles'
                }
            ], toolbarScope);
        });
    }
    ngOnInit() {
        this.loadPage();
    }
    ngOnDestroy() {
        this.toolbar.clearButtons(this.toolbarScope());
    }
    setActiveSection(section) {
        this.activeSection.set(section);
    }
    handleUserCreated(user) {
        this.users.set(this.upsertUser(user));
        this.activeSection.set('users');
    }
    handleUserUpdated(user) {
        this.users.set(this.upsertUser(user));
    }
    handleRoleCreated(role) {
        this.roles.set(this.upsertRole(role));
        this.activeSection.set('roles');
    }
    handleRoleUpdated(role) {
        this.roles.set(this.upsertRole(role));
    }
    loadPage() {
        this.isLoading.set(true);
        this.errorMessage.set('');
        this.chill.getAuthUsers().subscribe({
            next: (users) => {
                const sortedUsers = [...users].sort((left, right) => this.userLabel(left).localeCompare(this.userLabel(right)));
                this.users.set(sortedUsers);
                const currentUser = this.resolveCurrentUser(sortedUsers);
                const canManagePermissions = currentUser?.canManagePermissions === true;
                this.canManagePermissions.set(canManagePermissions);
                if (!canManagePermissions) {
                    this.roles.set([]);
                    this.isLoading.set(false);
                    return;
                }
                this.chill.getAuthRoles().subscribe({
                    next: (roles) => {
                        const sortedRoles = [...roles].sort((left, right) => left.name.localeCompare(right.name));
                        this.roles.set(sortedRoles);
                        this.isLoading.set(false);
                    },
                    error: (error) => {
                        this.roles.set([]);
                        this.isLoading.set(false);
                        this.errorMessage.set(this.chill.formatError(error));
                    }
                });
            },
            error: (error) => {
                this.users.set([]);
                this.roles.set([]);
                this.canManagePermissions.set(false);
                this.isLoading.set(false);
                this.errorMessage.set(this.chill.formatError(error));
            }
        });
    }
    resolveCurrentUser(users) {
        const session = this.chill.session();
        const normalizedUserName = session?.userName?.trim().toLowerCase() ?? '';
        const normalizedUserId = session?.userId?.trim() ?? '';
        return users.find((user) => user.guid === normalizedUserId
            || user.userName.trim().toLowerCase() === normalizedUserName) ?? null;
    }
    userLabel(user) {
        return user.displayName?.trim() || user.userName?.trim() || user.externalId?.trim() || user.guid;
    }
    upsertUser(user) {
        const users = this.users().filter((entry) => entry.guid !== user.guid);
        return [...users, user].sort((left, right) => this.userLabel(left).localeCompare(this.userLabel(right)));
    }
    upsertRole(role) {
        const roles = this.roles().filter((entry) => entry.guid !== role.guid);
        return [...roles, role].sort((left, right) => left.name.localeCompare(right.name));
    }
    static { this.ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: PermissionsPageComponent, deps: [], target: i0.ɵɵFactoryTarget.Component }); }
    static { this.ɵcmp = i0.ɵɵngDeclareComponent({ minVersion: "17.0.0", version: "19.2.21", type: PermissionsPageComponent, isStandalone: true, selector: "app-permissions-page", inputs: { visible: { classPropertyName: "visible", publicName: "visible", isSignal: true, isRequired: false, transformFunction: null }, toolbarScope: { classPropertyName: "toolbarScope", publicName: "toolbarScope", isSignal: true, isRequired: false, transformFunction: null } }, ngImport: i0, template: "<section class=\"permissions-page\">\n  <header class=\"hero\">\n    <p class=\"eyebrow\"><app-chill-i18n-label [labelGuid]=\"'68ADFBAB-4D1A-42A5-AB39-F099FB870A7C'\" [primaryDefaultText]=\"'Security'\" [secondaryDefaultText]=\"'Sicurezza'\" /></p>\n    <h1><app-chill-i18n-label [labelGuid]=\"'830A6D96-0332-4B08-8EC7-B850702B4337'\" [primaryDefaultText]=\"'Permissions'\" [secondaryDefaultText]=\"'Permessi'\" /></h1>\n    <p class=\"lede\"><app-chill-i18n-label [labelGuid]=\"'B8F5A363-9E5F-4CAA-9296-54FBF31E1C98'\" [primaryDefaultText]=\"'Manage user roles, permission rules, and the special ability to manage permissions.'\" [secondaryDefaultText]='\"Gestisci ruoli utente, regole permesso e l&apos;abilitazione speciale per gestire i permessi.\"' /></p>\n  </header>\n\n  @if (errorMessage()) {\n    <div class=\"notice error\">{{ errorMessage() }}</div>\n  }\n\n  @if (isLoading()) {\n    <div class=\"notice\">{{ chill.T('9559C65E-F793-4A7E-BBE3-52F9400A3444', 'Loading users, roles, and permission rules...', 'Caricamento utenti, ruoli e regole permesso...') }}</div>\n  } @else if (!canManagePermissions()) {\n    <section class=\"empty-state\">\n      <h2><app-chill-i18n-label [labelGuid]=\"'5DEBC3A2-0A07-4B40-B9CF-16C4E3AFA8C1'\" [primaryDefaultText]=\"'Insufficient permissions'\" [secondaryDefaultText]=\"'Permessi insufficienti'\" /></h2>\n      <p><app-chill-i18n-label [labelGuid]=\"'0C03BF4E-57B4-47DB-8E71-0B5B3D165CB5'\" [primaryDefaultText]=\"'You have insufficient permission to handle permissions.'\" [secondaryDefaultText]=\"'Non disponi dei permessi sufficienti per gestire i permessi.'\" /></p>\n    </section>\n  } @else {\n    @if (activeSection() === 'users') {\n      <app-user-permission\n        [users]=\"users()\"\n        [roles]=\"roles()\"\n        (userCreated)=\"handleUserCreated($event)\"\n        (userUpdated)=\"handleUserUpdated($event)\" />\n    } @else {\n      <app-role-permission\n        [users]=\"users()\"\n        [roles]=\"roles()\"\n        (roleCreated)=\"handleRoleCreated($event)\"\n        (roleUpdated)=\"handleRoleUpdated($event)\" />\n    }\n  }\n</section>\n", styles: [":host{display:block;height:100%;min-height:0}.permissions-page{display:grid;height:100%;min-height:0;gap:1.5rem;padding:1.5rem .5rem;overflow-y:auto}.hero{display:grid;gap:.6rem}.hero h1,.hero p{margin:0}.eyebrow{color:var(--accent);font-size:.78rem;font-weight:700;letter-spacing:.18em;text-transform:uppercase}.lede{max-width:64rem;color:var(--text-muted)}.notice,.empty-state{padding:1rem 1.25rem;border-radius:1rem;border:1px solid var(--border-color);background:var(--surface-2);color:var(--text-main);font:inherit}.empty-state{display:grid;gap:.6rem;max-width:42rem}.empty-state h2,.empty-state p{margin:0}.notice.error{color:#8b1e3f;border-color:#8b1e3f33;background:#fff0f4eb}@media(max-width:720px){.permissions-page{padding:1rem .25rem}}\n"], dependencies: [{ kind: "ngmodule", type: CommonModule }, { kind: "component", type: UserPermissionComponent, selector: "app-user-permission", inputs: ["users", "roles"], outputs: ["userCreated", "userUpdated"] }, { kind: "component", type: RolePermissionComponent, selector: "app-role-permission", inputs: ["users", "roles"], outputs: ["roleCreated", "roleUpdated"] }, { kind: "component", type: ChillI18nLabelComponent, selector: "app-chill-i18n-label", inputs: ["labelGuid", "primaryDefaultText", "secondaryDefaultText", "editable"] }, { kind: "directive", type: NoticeTransitionDirective, selector: ".notice" }] }); }
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: PermissionsPageComponent, decorators: [{
            type: Component,
            args: [{ selector: 'app-permissions-page', standalone: true, imports: [CommonModule, UserPermissionComponent, RolePermissionComponent, ChillI18nLabelComponent, NoticeTransitionDirective], template: "<section class=\"permissions-page\">\n  <header class=\"hero\">\n    <p class=\"eyebrow\"><app-chill-i18n-label [labelGuid]=\"'68ADFBAB-4D1A-42A5-AB39-F099FB870A7C'\" [primaryDefaultText]=\"'Security'\" [secondaryDefaultText]=\"'Sicurezza'\" /></p>\n    <h1><app-chill-i18n-label [labelGuid]=\"'830A6D96-0332-4B08-8EC7-B850702B4337'\" [primaryDefaultText]=\"'Permissions'\" [secondaryDefaultText]=\"'Permessi'\" /></h1>\n    <p class=\"lede\"><app-chill-i18n-label [labelGuid]=\"'B8F5A363-9E5F-4CAA-9296-54FBF31E1C98'\" [primaryDefaultText]=\"'Manage user roles, permission rules, and the special ability to manage permissions.'\" [secondaryDefaultText]='\"Gestisci ruoli utente, regole permesso e l&apos;abilitazione speciale per gestire i permessi.\"' /></p>\n  </header>\n\n  @if (errorMessage()) {\n    <div class=\"notice error\">{{ errorMessage() }}</div>\n  }\n\n  @if (isLoading()) {\n    <div class=\"notice\">{{ chill.T('9559C65E-F793-4A7E-BBE3-52F9400A3444', 'Loading users, roles, and permission rules...', 'Caricamento utenti, ruoli e regole permesso...') }}</div>\n  } @else if (!canManagePermissions()) {\n    <section class=\"empty-state\">\n      <h2><app-chill-i18n-label [labelGuid]=\"'5DEBC3A2-0A07-4B40-B9CF-16C4E3AFA8C1'\" [primaryDefaultText]=\"'Insufficient permissions'\" [secondaryDefaultText]=\"'Permessi insufficienti'\" /></h2>\n      <p><app-chill-i18n-label [labelGuid]=\"'0C03BF4E-57B4-47DB-8E71-0B5B3D165CB5'\" [primaryDefaultText]=\"'You have insufficient permission to handle permissions.'\" [secondaryDefaultText]=\"'Non disponi dei permessi sufficienti per gestire i permessi.'\" /></p>\n    </section>\n  } @else {\n    @if (activeSection() === 'users') {\n      <app-user-permission\n        [users]=\"users()\"\n        [roles]=\"roles()\"\n        (userCreated)=\"handleUserCreated($event)\"\n        (userUpdated)=\"handleUserUpdated($event)\" />\n    } @else {\n      <app-role-permission\n        [users]=\"users()\"\n        [roles]=\"roles()\"\n        (roleCreated)=\"handleRoleCreated($event)\"\n        (roleUpdated)=\"handleRoleUpdated($event)\" />\n    }\n  }\n</section>\n", styles: [":host{display:block;height:100%;min-height:0}.permissions-page{display:grid;height:100%;min-height:0;gap:1.5rem;padding:1.5rem .5rem;overflow-y:auto}.hero{display:grid;gap:.6rem}.hero h1,.hero p{margin:0}.eyebrow{color:var(--accent);font-size:.78rem;font-weight:700;letter-spacing:.18em;text-transform:uppercase}.lede{max-width:64rem;color:var(--text-muted)}.notice,.empty-state{padding:1rem 1.25rem;border-radius:1rem;border:1px solid var(--border-color);background:var(--surface-2);color:var(--text-main);font:inherit}.empty-state{display:grid;gap:.6rem;max-width:42rem}.empty-state h2,.empty-state p{margin:0}.notice.error{color:#8b1e3f;border-color:#8b1e3f33;background:#fff0f4eb}@media(max-width:720px){.permissions-page{padding:1rem .25rem}}\n"] }]
        }], ctorParameters: () => [] });

const CHILL_PROPERTY_TYPE$1 = {
    Unknown: 0,
    Guid: 1,
    Integer: 10,
    Decimal: 20,
    Date: 30,
    Time: 40,
    DateTime: 50,
    Duration: 60,
    Boolean: 70,
    String: 80,
    Text: 81,
    Select: 90,
    Json: 99,
    ChillEntity: 1000,
    ChillEntityCollection: 1010,
    ChillQuery: 1100
};
const CHILL_PROPERTY_TYPE_OPTIONS = [
    { value: CHILL_PROPERTY_TYPE$1.Guid, label: 'Guid' },
    { value: CHILL_PROPERTY_TYPE$1.Integer, label: 'Integer' },
    { value: CHILL_PROPERTY_TYPE$1.Decimal, label: 'Decimal' },
    { value: CHILL_PROPERTY_TYPE$1.Date, label: 'Date' },
    { value: CHILL_PROPERTY_TYPE$1.Time, label: 'Time' },
    { value: CHILL_PROPERTY_TYPE$1.DateTime, label: 'DateTime' },
    { value: CHILL_PROPERTY_TYPE$1.Duration, label: 'Duration' },
    { value: CHILL_PROPERTY_TYPE$1.Boolean, label: 'Boolean' },
    { value: CHILL_PROPERTY_TYPE$1.String, label: 'String' },
    { value: CHILL_PROPERTY_TYPE$1.Text, label: 'Text' },
    { value: CHILL_PROPERTY_TYPE$1.Select, label: 'Select' },
    { value: CHILL_PROPERTY_TYPE$1.Json, label: 'Json' },
    { value: CHILL_PROPERTY_TYPE$1.ChillEntity, label: 'ChillEntity' },
    { value: CHILL_PROPERTY_TYPE$1.ChillEntityCollection, label: 'ChillEntityCollection' },
    { value: CHILL_PROPERTY_TYPE$1.ChillQuery, label: 'ChillQuery' }
];
function canChangeChillPropertyType(currentType, nextType) {
    if ((currentType ?? CHILL_PROPERTY_TYPE$1.Unknown) === nextType) {
        return true;
    }
    return currentType === CHILL_PROPERTY_TYPE$1.String
        && (nextType === CHILL_PROPERTY_TYPE$1.Text || nextType === CHILL_PROPERTY_TYPE$1.Json);
}
function chillSimplePropertyType(propertyType) {
    switch (propertyType) {
        case CHILL_PROPERTY_TYPE$1.Guid:
            return 'guid';
        case CHILL_PROPERTY_TYPE$1.Integer:
            return 'int';
        case CHILL_PROPERTY_TYPE$1.Decimal:
            return 'decimal';
        case CHILL_PROPERTY_TYPE$1.Date:
            return 'date';
        case CHILL_PROPERTY_TYPE$1.Time:
            return 'time';
        case CHILL_PROPERTY_TYPE$1.DateTime:
            return 'datetime';
        case CHILL_PROPERTY_TYPE$1.Duration:
            return 'duration';
        case CHILL_PROPERTY_TYPE$1.Boolean:
            return 'bool';
        case CHILL_PROPERTY_TYPE$1.String:
            return 'string';
        case CHILL_PROPERTY_TYPE$1.Text:
            return 'text';
        case CHILL_PROPERTY_TYPE$1.Select:
            return 'string';
        case CHILL_PROPERTY_TYPE$1.Json:
            return 'json';
        case CHILL_PROPERTY_TYPE$1.ChillEntity:
            return 'chill-entity';
        case CHILL_PROPERTY_TYPE$1.ChillEntityCollection:
            return 'chill-entity-collection';
        case CHILL_PROPERTY_TYPE$1.ChillQuery:
            return 'chill-query';
        default:
            return '';
    }
}

class ChillJsonInputComponent {
    constructor() {
        this.value = input('');
        this.placeholder = input('');
        this.invalid = input(false);
        this.disabled = input(false);
        this.language = input('json');
        this.minHeight = input('4rem');
        this.maxHeight = input('50vh');
        this.mobileFullHeight = input(false);
        this.valueChange = output();
        this.blur = output();
        this.zone = inject(NgZone);
        this.monaco = null;
        this.editor = null;
        this.model = null;
        this.resizeObserver = null;
        this.themeObserver = null;
        this.suppressValueEmit = false;
    }
    async ngAfterViewInit() {
        const host = this.editorHost?.nativeElement;
        if (!host) {
            return;
        }
        this.monaco = await import('monaco-editor/esm/vs/editor/editor.api');
        this.model = this.monaco.editor.createModel(this.value(), this.language());
        this.editor = this.monaco.editor.create(host, {
            model: this.model,
            language: this.language(),
            automaticLayout: true,
            minimap: { enabled: false },
            scrollBeyondLastLine: false,
            tabSize: 2,
            insertSpaces: true,
            formatOnPaste: this.language() === 'json',
            formatOnType: this.language() === 'json',
            wordWrap: 'on',
            lineNumbersMinChars: 3,
            padding: { top: 12, bottom: 12 },
            roundedSelection: false,
            scrollbar: {
                verticalScrollbarSize: 10,
                horizontalScrollbarSize: 10
            },
            overviewRulerLanes: 0,
            fontSize: 13,
            readOnly: this.disabled(),
            ariaLabel: this.placeholder() || this.defaultAriaLabel()
        });
        this.applyTheme();
        this.editor.onDidChangeModelContent(() => {
            if (this.suppressValueEmit || !this.editor) {
                return;
            }
            const nextValue = this.editor.getValue();
            this.zone.run(() => this.valueChange.emit(nextValue));
        });
        this.editor.onDidBlurEditorText(() => {
            this.zone.run(() => this.blur.emit());
        });
        this.resizeObserver = new ResizeObserver(() => {
            this.editor?.layout();
        });
        this.resizeObserver.observe(host);
        this.themeObserver = new MutationObserver(() => {
            this.applyTheme();
        });
        this.themeObserver.observe(document.documentElement, {
            attributes: true,
            attributeFilter: ['data-theme']
        });
    }
    ngOnChanges(changes) {
        if (changes['value'] && this.editor) {
            const nextValue = this.value();
            if (nextValue !== this.editor.getValue()) {
                this.suppressValueEmit = true;
                this.editor.setValue(nextValue);
                this.suppressValueEmit = false;
            }
        }
        if (changes['disabled'] && this.editor) {
            this.editor.updateOptions({ readOnly: this.disabled() });
        }
        if (changes['placeholder'] && this.editor) {
            this.editor.updateOptions({
                ariaLabel: this.placeholder() || this.defaultAriaLabel()
            });
        }
        if (changes['language'] && this.monaco && this.model && this.editor) {
            const language = this.language();
            this.monaco.editor.setModelLanguage(this.model, language);
            this.editor.updateOptions({
                formatOnPaste: language === 'json',
                formatOnType: language === 'json',
                ariaLabel: this.placeholder() || this.defaultAriaLabel()
            });
        }
    }
    ngOnDestroy() {
        this.themeObserver?.disconnect();
        this.resizeObserver?.disconnect();
        this.editor?.dispose();
        this.model?.dispose();
    }
    applyTheme() {
        const isDarkTheme = document.documentElement.dataset['theme'] === 'dark';
        this.monaco?.editor.setTheme(isDarkTheme ? 'vs-dark' : 'vs');
    }
    editorStyle() {
        return {
            minHeight: this.minHeight(),
            maxHeight: this.maxHeight(),
            height: `clamp(${this.minHeight()}, 34vh, ${this.maxHeight()})`
        };
    }
    defaultAriaLabel() {
        return this.language() === 'json' ? 'JSON editor' : 'Text editor';
    }
    static { this.ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: ChillJsonInputComponent, deps: [], target: i0.ɵɵFactoryTarget.Component }); }
    static { this.ɵcmp = i0.ɵɵngDeclareComponent({ minVersion: "17.1.0", version: "19.2.21", type: ChillJsonInputComponent, isStandalone: true, selector: "app-chill-json-input", inputs: { value: { classPropertyName: "value", publicName: "value", isSignal: true, isRequired: false, transformFunction: null }, placeholder: { classPropertyName: "placeholder", publicName: "placeholder", isSignal: true, isRequired: false, transformFunction: null }, invalid: { classPropertyName: "invalid", publicName: "invalid", isSignal: true, isRequired: false, transformFunction: null }, disabled: { classPropertyName: "disabled", publicName: "disabled", isSignal: true, isRequired: false, transformFunction: null }, language: { classPropertyName: "language", publicName: "language", isSignal: true, isRequired: false, transformFunction: null }, minHeight: { classPropertyName: "minHeight", publicName: "minHeight", isSignal: true, isRequired: false, transformFunction: null }, maxHeight: { classPropertyName: "maxHeight", publicName: "maxHeight", isSignal: true, isRequired: false, transformFunction: null }, mobileFullHeight: { classPropertyName: "mobileFullHeight", publicName: "mobileFullHeight", isSignal: true, isRequired: false, transformFunction: null } }, outputs: { valueChange: "valueChange", blur: "blur" }, host: { properties: { "class.is-mobile-full-height": "mobileFullHeight()" } }, viewQueries: [{ propertyName: "editorHost", first: true, predicate: ["editorHost"], descendants: true, static: true }], usesOnChanges: true, ngImport: i0, template: `
    <div class="json-editor" [class.is-invalid]="invalid()">
      <div #editorHost class="json-editor__host" [ngStyle]="editorStyle()"></div>
    </div>
  `, isInline: true, styles: [":host{display:block}.json-editor{border:1px solid color-mix(in srgb,var(--accent) 26%,var(--border-color));border-radius:.7rem;overflow:hidden;background:linear-gradient(180deg,color-mix(in srgb,var(--accent-soft) 20%,transparent),transparent 30%),linear-gradient(180deg,color-mix(in srgb,var(--surface-0) 96%,rgba(2,16,25,.14)),color-mix(in srgb,var(--surface-1) 92%,rgba(1,10,18,.18)));box-shadow:inset 0 0 0 1px color-mix(in srgb,var(--accent) 8%,transparent),0 0 .7rem color-mix(in srgb,var(--accent) 6%,transparent)}.json-editor.is-invalid{border-color:color-mix(in srgb,var(--danger) 70%,var(--border-color));box-shadow:inset 0 0 0 1px color-mix(in srgb,var(--danger) 18%,transparent),0 0 .7rem color-mix(in srgb,var(--danger) 8%,transparent)}.json-editor__host{width:100%}:root[data-theme=dark] .json-editor{background:#09131a94}@media(max-width:720px){:host(.is-mobile-full-height),:host(.is-mobile-full-height) .json-editor,:host(.is-mobile-full-height) .json-editor__host{height:100%;min-height:0}:host(.is-mobile-full-height) .json-editor__host{max-height:none!important;height:100%!important}}\n"], dependencies: [{ kind: "ngmodule", type: CommonModule }, { kind: "directive", type: i1$1.NgStyle, selector: "[ngStyle]", inputs: ["ngStyle"] }] }); }
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: ChillJsonInputComponent, decorators: [{
            type: Component,
            args: [{ selector: 'app-chill-json-input', standalone: true, imports: [CommonModule], host: {
                        '[class.is-mobile-full-height]': 'mobileFullHeight()'
                    }, template: `
    <div class="json-editor" [class.is-invalid]="invalid()">
      <div #editorHost class="json-editor__host" [ngStyle]="editorStyle()"></div>
    </div>
  `, styles: [":host{display:block}.json-editor{border:1px solid color-mix(in srgb,var(--accent) 26%,var(--border-color));border-radius:.7rem;overflow:hidden;background:linear-gradient(180deg,color-mix(in srgb,var(--accent-soft) 20%,transparent),transparent 30%),linear-gradient(180deg,color-mix(in srgb,var(--surface-0) 96%,rgba(2,16,25,.14)),color-mix(in srgb,var(--surface-1) 92%,rgba(1,10,18,.18)));box-shadow:inset 0 0 0 1px color-mix(in srgb,var(--accent) 8%,transparent),0 0 .7rem color-mix(in srgb,var(--accent) 6%,transparent)}.json-editor.is-invalid{border-color:color-mix(in srgb,var(--danger) 70%,var(--border-color));box-shadow:inset 0 0 0 1px color-mix(in srgb,var(--danger) 18%,transparent),0 0 .7rem color-mix(in srgb,var(--danger) 8%,transparent)}.json-editor__host{width:100%}:root[data-theme=dark] .json-editor{background:#09131a94}@media(max-width:720px){:host(.is-mobile-full-height),:host(.is-mobile-full-height) .json-editor,:host(.is-mobile-full-height) .json-editor__host{height:100%;min-height:0}:host(.is-mobile-full-height) .json-editor__host{max-height:none!important;height:100%!important}}\n"] }]
        }], propDecorators: { editorHost: [{
                type: ViewChild,
                args: ['editorHost', { static: true }]
            }] } });

class ChillPolymorphicInputComponent {
    // #endregion
    // #region Component Lifecycle
    /**
     * Rebuilds local field, error, and lookup state from the current form/schema pair and re-emits aggregate state.
     */
    constructor() {
        // #region Service Injections
        this.chill = inject(ChillService);
        this.dialog = inject(WorkspaceDialogService);
        // #endregion
        // #region Inputs
        this.form = input(null);
        this.schema = input(null);
        this.propertyNames = input(null);
        this.readonlyPropertyNames = input(null);
        this.externalErrors = input(null);
        this.showLabels = input(true);
        // #endregion
        // #region Outputs
        this.valueChange = output();
        this.validityChange = output();
        this.fieldBlur = output();
        this.lookupDialogOpenChange = output();
        this.editorDialogOpenChange = output();
        // #endregion
        // #region State
        this.fieldValues = signal({});
        this.draftTextValues = signal({});
        this.errors = signal({});
        this.lookups = signal({});
        this.lookupDialogSelectionState = signal({});
        this.editorDialogSelectionState = signal({});
        this.lookupOverlayPositions = [
            {
                originX: 'start',
                originY: 'bottom',
                overlayX: 'start',
                overlayY: 'top'
            },
            {
                originX: 'end',
                originY: 'bottom',
                overlayX: 'end',
                overlayY: 'top'
            },
            {
                originX: 'start',
                originY: 'top',
                overlayX: 'start',
                overlayY: 'bottom'
            },
            {
                originX: 'end',
                originY: 'top',
                overlayX: 'end',
                overlayY: 'bottom'
            }
        ];
        this.lookupSearchTimers = new Map();
        this.lookupRequestSequence = new Map();
        this.controlSubscriptions = new Subscription();
        this.isDestroyed = false;
        // #endregion
        // #region Computed Properties
        this.properties = computed(() => {
            const allowedPropertyNames = this.propertyNames();
            const allowedSet = allowedPropertyNames ? new Set(allowedPropertyNames) : null;
            return (this.schema()?.properties ?? []).filter((property) => {
                if (this.shouldSkipProperty(property)) {
                    return false;
                }
                return allowedSet ? allowedSet.has(property.name) : true;
            });
        });
        this.resolvedErrors = computed(() => {
            const next = {
                ...this.errors()
            };
            for (const property of this.properties()) {
                const controlError = this.readControlValidationMessage(property.name);
                if (controlError) {
                    next[property.name] = controlError;
                }
            }
            const externalErrors = this.externalErrors() ?? {};
            const propertyNameMap = new Map(this.properties()
                .map((property) => property.name.trim())
                .filter((propertyName) => propertyName.length > 0)
                .map((propertyName) => [propertyName.toLowerCase(), propertyName]));
            for (const [fieldName, message] of Object.entries(externalErrors)) {
                const normalizedMessage = message.trim();
                if (normalizedMessage) {
                    const resolvedFieldName = propertyNameMap.get(fieldName.trim().toLowerCase()) ?? fieldName;
                    next[resolvedFieldName] = normalizedMessage;
                }
            }
            return next;
        });
        this.isValid = computed(() => this.properties().every((property) => !this.resolvedErrors()[property.name]));
        this.readonlyPropertyNameSet = computed(() => new Set((this.readonlyPropertyNames() ?? [])
            .map((propertyName) => propertyName.trim().toLowerCase())
            .filter((propertyName) => propertyName.length > 0)));
        effect(() => {
            const properties = this.properties();
            const form = this.form();
            const fields = this.readFormValues(properties);
            const errors = this.validateAllFields(properties, fields);
            const lookups = this.createLookupState(properties, fields);
            this.controlSubscriptions.unsubscribe();
            this.controlSubscriptions = new Subscription();
            for (const property of properties) {
                const control = this.control(property.name);
                if (!control) {
                    continue;
                }
                this.controlSubscriptions.add(control.valueChanges.subscribe((value) => {
                    this.fieldValues.update((current) => ({
                        ...current,
                        [property.name]: value
                    }));
                    this.draftTextValues.update((current) => {
                        if (!(property.name in current)) {
                            return current;
                        }
                        const { [property.name]: _, ...rest } = current;
                        return rest;
                    });
                    this.syncLookupState(property, value);
                    if (this.shouldValidateOnChange(property)) {
                        this.validateField(property);
                    }
                }));
            }
            if (!form) {
                this.fieldValues.set({});
                this.draftTextValues.set({});
                this.errors.set({});
                this.lookups.set({});
                return;
            }
            this.fieldValues.update((current) => this.areRecordsEqual(current, fields) ? current : fields);
            this.draftTextValues.set({});
            this.errors.update((current) => this.areStringRecordsEqual(current, errors) ? current : errors);
            this.lookups.update((current) => this.areLookupStatesEqual(current, lookups) ? current : lookups);
        });
        effect(() => {
            this.valueChange.emit(this.fieldValues());
            this.validityChange.emit(this.isValid());
        });
    }
    /**
     * Clears control subscriptions and pending lookup timers when the component is destroyed.
     */
    ngOnDestroy() {
        this.isDestroyed = true;
        this.controlSubscriptions.unsubscribe();
        for (const timer of this.lookupSearchTimers.values()) {
            clearTimeout(timer);
        }
        this.lookupSearchTimers.clear();
    }
    // #endregion
    // #region Public Methods
    /**
     * Identifies boolean fields so the template can render a checkbox instead of a text input.
     */
    isCheckbox(property) {
        return property.propertyType === CHILL_PROPERTY_TYPE$1.Boolean;
    }
    /**
     * Uses type and metadata hints to decide when a string field should render as multiline input.
     */
    isTextarea(property) {
        return property.customFormat?.toLowerCase() === 'textarea'
            || this.metadataString(property, 'multiline').toLowerCase() === 'true';
    }
    /**
     * Identifies static select fields backed by metadata option tuples.
     */
    isSelect(property) {
        return property.propertyType === CHILL_PROPERTY_TYPE$1.Select;
    }
    /**
     * Flags JSON-string fields so the template can render the Monaco editor.
     */
    isJsonEditor(property) {
        return property.propertyType === CHILL_PROPERTY_TYPE$1.Json
            || property.propertyType === CHILL_PROPERTY_TYPE$1.Text;
    }
    editorLanguage(property) {
        return property.propertyType === CHILL_PROPERTY_TYPE$1.Json ? 'json' : 'plaintext';
    }
    /**
     * Returns true when the caller marked the property as read only.
     */
    isPropertyReadOnly(propertyName) {
        return this.readonlyPropertyNameSet().has(propertyName.trim().toLowerCase());
    }
    /**
     * Checks whether a property uses single-value lookup behavior.
     */
    isLookup(property) {
        return property.propertyType === CHILL_PROPERTY_TYPE$1.ChillEntity
            || property.propertyType === CHILL_PROPERTY_TYPE$1.ChillQuery;
    }
    /**
     * Checks whether a property uses multi-value lookup behavior.
     */
    isLookupCollection(property) {
        return property.propertyType === CHILL_PROPERTY_TYPE$1.ChillEntityCollection;
    }
    /**
     * Flags date-only and date-time fields so they can render localized display text instead of raw storage values.
     */
    isCultureDateInput(property) {
        return property.propertyType === CHILL_PROPERTY_TYPE$1.Date
            || property.propertyType === CHILL_PROPERTY_TYPE$1.DateTime;
    }
    isFormattedTextInput(property) {
        return property.propertyType === CHILL_PROPERTY_TYPE$1.Date
            || property.propertyType === CHILL_PROPERTY_TYPE$1.DateTime
            || property.propertyType === CHILL_PROPERTY_TYPE$1.Time
            || property.propertyType === CHILL_PROPERTY_TYPE$1.Duration
            || property.propertyType === CHILL_PROPERTY_TYPE$1.Integer
            || property.propertyType === CHILL_PROPERTY_TYPE$1.Decimal;
    }
    /**
     * Resolves the native input type for scalar fields.
     */
    inputType(property) {
        return property.propertyType === CHILL_PROPERTY_TYPE$1.Integer
            || property.propertyType === CHILL_PROPERTY_TYPE$1.Decimal
            ? 'number'
            : 'text';
    }
    /**
     * Resolves the numeric step value from metadata or property type defaults.
     */
    inputStep(property) {
        const metadataStep = this.metadataString(property, 'step');
        if (metadataStep) {
            return metadataStep;
        }
        if (property.propertyType === CHILL_PROPERTY_TYPE$1.Integer) {
            return '1';
        }
        if (property.propertyType === CHILL_PROPERTY_TYPE$1.Decimal) {
            return 'any';
        }
        return null;
    }
    /**
     * Uses metadata placeholder first, otherwise mirrors the field label when labels are visually hidden.
     */
    placeholder(property) {
        const explicitPlaceholder = this.metadataString(property, 'placeholder');
        if (explicitPlaceholder) {
            return explicitPlaceholder;
        }
        return this.showLabels()
            ? ''
            : property.displayName?.trim() || property.name;
    }
    /**
     * Converts string and numeric field values into the text representation expected by native inputs.
     */
    textValue(propertyName) {
        const draftValue = this.draftTextValues()[propertyName];
        if (typeof draftValue === 'string') {
            return draftValue;
        }
        const value = this.fieldValues()[propertyName];
        const property = this.properties().find((candidate) => candidate.name === propertyName);
        if (property) {
            if (property.propertyType === CHILL_PROPERTY_TYPE$1.Integer || property.propertyType === CHILL_PROPERTY_TYPE$1.Decimal) {
                if (typeof value === 'number') {
                    return this.chill.formatDisplayNumber(value);
                }
                return typeof value === 'string' ? value : '';
            }
            if (typeof value === 'string') {
                if (property.propertyType === CHILL_PROPERTY_TYPE$1.Date) {
                    return this.chill.formatDisplayDate(value);
                }
                if (property.propertyType === CHILL_PROPERTY_TYPE$1.Time) {
                    return this.chill.formatDisplayTime(value);
                }
                if (property.propertyType === CHILL_PROPERTY_TYPE$1.DateTime) {
                    return this.chill.formatDisplayDateTime(value);
                }
            }
        }
        return typeof value === 'string' || typeof value === 'number' ? String(value) : '';
    }
    /**
     * Reads a field value as a boolean for checkbox binding.
     */
    booleanValue(propertyName) {
        return this.fieldValues()[propertyName] === true;
    }
    /**
     * Returns the current lookup search term for a property.
     */
    lookupTerm(propertyName) {
        return this.lookups()[propertyName]?.term ?? '';
    }
    /**
     * Exposes the current lookup result list for dropdown rendering.
     */
    lookupResults(propertyName) {
        return this.lookups()[propertyName]?.results ?? [];
    }
    /**
     * Returns the current lookup error message for a property.
     */
    lookupError(propertyName) {
        return this.lookups()[propertyName]?.error ?? '';
    }
    /**
     * Returns whether a lookup search is currently running for a property.
     */
    lookupIsSearching(propertyName) {
        return this.lookups()[propertyName]?.isSearching ?? false;
    }
    /**
     * Measures the visible input slot so the detached overlay keeps the same width as the field.
     */
    lookupOverlayWidth(origin) {
        if (!origin) {
            return 0;
        }
        return Math.ceil(origin.getBoundingClientRect().width);
    }
    /**
     * Checks whether the dialog-based lookup picker can be opened for a property.
     */
    canOpenLookupDialog(property) {
        return (property.propertyType === CHILL_PROPERTY_TYPE$1.ChillEntity
            || property.propertyType === CHILL_PROPERTY_TYPE$1.ChillEntityCollection) && !!property.referenceChillType?.trim();
    }
    /**
     * Joins the selected labels of a lookup collection into the compact summary shown in the input.
     */
    lookupCollectionSummary(propertyName) {
        return this.collectionLookupLabels(propertyName).join(', ');
    }
    /**
     * Returns the selected collection lookup entities in storage order.
     */
    selectedLookupCollectionEntities(propertyName) {
        const value = this.fieldValues()[propertyName];
        return Array.isArray(value)
            ? value.filter((item) => this.isJsonObject(item))
            : [];
    }
    /**
     * Returns the selected single-lookup entity when one is currently stored in the field.
     */
    selectedLookupEntity(propertyName) {
        const value = this.fieldValues()[propertyName];
        return this.isJsonObject(value) ? value : null;
    }
    /**
     * Returns whether the field currently holds a selected single lookup entity.
     */
    hasSelectedLookupEntity(propertyName) {
        return this.selectedLookupEntity(propertyName) !== null;
    }
    /**
     * Returns the full label shown inside the selected single-value lookup pill.
     */
    selectedLookupLabel(propertyName) {
        return this.lookups()[propertyName]?.selectedLabel ?? '';
    }
    /**
     * Returns the compact lookup label used when the selected pill becomes narrow.
     */
    selectedLookupShortLabel(propertyName) {
        return this.lookups()[propertyName]?.selectedShortLabel ?? '';
    }
    /**
     * Extracts non-empty labels from the current lookup collection value.
     */
    collectionLookupLabels(propertyName) {
        const value = this.fieldValues()[propertyName];
        return Array.isArray(value)
            ? value.filter((item) => this.isJsonObject(item)).map((item) => this.lookupLabel(item)).filter((item) => item.length > 0)
            : [];
    }
    /**
     * Returns the merged validation message coming from local validation, form errors, or external errors.
     */
    validationMessage(propertyName) {
        return this.resolvedErrors()[propertyName] ?? '';
    }
    /**
     * Trims and type-normalizes free-text input on blur, then revalidates before notifying the parent.
     */
    normalizeTextOnBlur(property) {
        const draftValue = this.draftTextValues()[property.name];
        const currentValue = typeof draftValue === 'string'
            ? draftValue
            : this.fieldValues()[property.name];
        if (typeof currentValue !== 'string') {
            this.notifyFieldBlur(property.name);
            return;
        }
        const normalizedValue = this.normalizeBlurValue(property, currentValue);
        if (normalizedValue === null) {
            this.setLocalError(property.name, this.getValidationMessage(property, currentValue));
            return;
        }
        this.setFieldValue(property.name, normalizedValue);
        this.clearDraftTextValue(property.name);
        this.validateField(property);
        this.notifyFieldBlur(property.name);
    }
    /**
     * Tracks raw typing for date and date-time inputs until blur normalization rewrites the value in culture format.
     */
    updateTextInput(propertyName, value) {
        const property = this.properties().find((candidate) => candidate.name === propertyName);
        if (property && this.shouldCommitTextOnBlur(property)) {
            this.draftTextValues.update((current) => ({
                ...current,
                [propertyName]: value
            }));
            this.clearLocalError(propertyName);
            return;
        }
        this.setFieldValue(propertyName, value);
    }
    /**
     * Stores the Monaco JSON editor content as a raw string inside the form control.
     */
    updateJsonInput(propertyName, value) {
        this.setFieldValue(propertyName, value);
    }
    async openEditorDialog(property) {
        const { ChillTextEditorDialogComponent } = await Promise.resolve().then(function () { return chillTextEditorDialog_component; });
        this.beginEditorDialogSelection(property.name);
        this.editorDialogOpenChange.emit(true);
        try {
            const result = await this.dialog.openDialog({
                title: property.displayName?.trim() || property.name,
                component: ChillTextEditorDialogComponent,
                panelClass: 'workspace-dialog--mobile-full-height',
                okLabel: this.chill.T('62953302-B951-4FD1-BD08-4B7649A91BAF', 'Save', 'Salva'),
                inputs: {
                    value: this.textValue(property.name),
                    language: this.editorLanguage(property),
                    placeholder: this.placeholder(property),
                    disabled: this.isPropertyReadOnly(property.name)
                }
            });
            if (result.status !== 'confirmed' || typeof result.value !== 'string') {
                return;
            }
            this.updateJsonInput(property.name, result.value);
            this.validateField(property);
            this.notifyFieldBlur(property.name);
        }
        finally {
            this.endEditorDialogSelection(property.name);
            this.editorDialogOpenChange.emit(false);
        }
    }
    beginEditorDialogSelection(propertyName) {
        this.editorDialogSelectionState.update((current) => ({
            ...current,
            [propertyName]: true
        }));
    }
    /**
     * Reads the current scalar value for a metadata-backed select field.
     */
    selectValue(propertyName) {
        const value = this.fieldValues()[propertyName];
        return typeof value === 'string' || typeof value === 'number'
            ? String(value)
            : '';
    }
    /**
     * Stores the selected option value as the field value and forwards blur semantics to the parent.
     */
    updateSelectValue(property, value) {
        this.setFieldValue(property.name, value);
        this.validateField(property);
        this.notifyFieldBlur(property.name);
    }
    /**
     * Returns normalized `[value, text]` tuples from property metadata for native select rendering.
     */
    selectOptions(property) {
        const rawOptions = property.metadata?.['options'];
        if (!Array.isArray(rawOptions)) {
            return [];
        }
        return rawOptions.flatMap((option) => {
            if (!Array.isArray(option) || option.length < 2) {
                return [];
            }
            const value = option[0];
            const text = option[1];
            if ((typeof value !== 'string' && typeof value !== 'number') || (typeof text !== 'string' && typeof text !== 'number')) {
                return [];
            }
            return [[String(value), String(text)]];
        });
    }
    /**
     * Updates the typed lookup text, clears stale selection metadata, and starts debounced search when applicable.
     */
    updateLookupTerm(property, value) {
        const previousLookup = this.lookups()[property.name] ?? this.createEmptyLookupState();
        this.lookups.update((current) => ({
            ...current,
            [property.name]: {
                ...previousLookup,
                term: value,
                error: '',
                selectedGuid: this.matchesLookupLabel(previousLookup.term, value) ? previousLookup.selectedGuid : ''
            }
        }));
        if (!value.trim()) {
            this.cancelLookupSearch(property.name);
            if (this.isLookup(property)) {
                this.setFieldValue(property.name, null);
            }
            this.lookups.update((current) => ({
                ...current,
                [property.name]: {
                    ...(current[property.name] ?? this.createEmptyLookupState()),
                    results: []
                }
            }));
            this.validateField(property);
            return;
        }
        this.scheduleLookupSearch(property, value);
    }
    /**
     * Reopens lookup suggestions on focus when the field already has searchable text but no visible results.
     */
    handleLookupFocus(property) {
        const lookup = this.lookups()[property.name] ?? this.createEmptyLookupState();
        if (lookup.term.trim() && lookup.results.length === 0) {
            this.scheduleLookupSearch(property, lookup.term);
        }
    }
    /**
     * Emits blur immediately and clears the popup list after a short delay so click selection can still complete.
     */
    handleLookupBlur(propertyName) {
        if (this.lookupDialogSelectionState()[propertyName]) {
            return;
        }
        this.notifyFieldBlur(propertyName);
        window.setTimeout(() => this.closeLookupResults(propertyName), 120);
    }
    /**
     * Forwards blur for controls that do not need blur-time value normalization.
     */
    emitFieldBlur(propertyName) {
        if (this.editorDialogSelectionState()[propertyName]) {
            return;
        }
        this.notifyFieldBlur(propertyName);
    }
    endEditorDialogSelection(propertyName) {
        this.editorDialogSelectionState.update((current) => {
            if (!(propertyName in current)) {
                return current;
            }
            const { [propertyName]: _, ...rest } = current;
            return rest;
        });
    }
    beginLookupDialogSelection(propertyName) {
        const wasAnyLookupDialogOpen = this.isAnyLookupDialogOpen();
        this.lookupDialogSelectionState.update((current) => ({
            ...current,
            [propertyName]: true
        }));
        if (!wasAnyLookupDialogOpen) {
            this.lookupDialogOpenChange.emit(true);
        }
    }
    /**
     * Opens the CRUD picker dialog for entity lookups and maps the confirmed selection back into the field.
     */
    async openLookupDialog(property) {
        this.beginLookupDialogSelection(property.name);
        const entityChillType = this.resolveLookupEntityChillType(property);
        const queryChillType = this.resolveLookupQueryChillType(property, entityChillType);
        if (!entityChillType && !queryChillType) {
            this.setLookupError(property.name, this.chill.T('7E0D5F0F-CDA4-4F49-8E02-A7E0E854B65A', 'Lookup schema is unavailable.', 'Lo schema di ricerca non è disponibile.'));
            this.endLookupDialogSelection(property.name);
            return;
        }
        const currentValue = this.fieldValues()[property.name];
        const selectedEntity = this.isJsonObject(currentValue) ? currentValue : null;
        const selectedEntities = Array.isArray(currentValue)
            ? currentValue.filter((item) => this.isJsonObject(item))
            : [];
        const { CrudTaskComponent } = await Promise.resolve().then(function () { return crudTask_component; });
        const result = await this.dialog.openDialog({
            title: property.displayName?.trim() || property.name,
            component: CrudTaskComponent,
            inputs: {
                componentConfiguration: {
                    chillType: entityChillType,
                    chillQuery: queryChillType || null,
                    viewCode: this.resolveLookupDialogViewCode()
                },
                taskTitle: property.displayName?.trim() || property.name,
                selectionEnabled: true,
                multipleSelection: this.isLookupCollection(property),
                initialSelectedEntity: selectedEntity,
                initialSelectedEntities: selectedEntities,
                toolbarScope: 'dialog'
            }
        });
        try {
            if (this.isDestroyed) {
                return;
            }
            if (result.status !== 'confirmed' || !result.value) {
                return;
            }
            if (Array.isArray(result.value)) {
                this.selectLookupResults(property, result.value);
            }
            else {
                this.selectLookupResult(property, result.value);
            }
            this.validateField(property);
            this.notifyFieldBlur(property.name);
        }
        finally {
            this.closeLookupResults(property.name);
            this.endLookupDialogSelection(property.name);
        }
    }
    /**
     * Stores a single lookup object, updates its display term, and marks the matching selected Guid.
     */
    selectLookupResult(property, result) {
        if (this.isLookupCollection(property)) {
            this.appendLookupCollectionResult(property, result);
            return;
        }
        this.setFieldValue(property.name, result);
        const selectedGuid = this.lookupGuid(result);
        const selectedLabel = this.lookupLabel(result);
        const selectedShortLabel = this.lookupShortLabel(result);
        this.lookups.update((current) => ({
            ...current,
            [property.name]: {
                ...(current[property.name] ?? this.createEmptyLookupState()),
                term: selectedLabel,
                isSearching: false,
                error: '',
                results: [],
                selectedGuid,
                selectedLabel,
                selectedShortLabel
            }
        }));
        this.validateField(property);
    }
    /**
     * Stores multiple lookup objects and rebuilds the collection summary shown in the input.
     */
    selectLookupResults(property, results) {
        const nextResults = this.isLookupCollection(property)
            ? this.mergeLookupCollectionResults(this.selectedLookupCollectionEntities(property.name), results)
            : results;
        this.setFieldValue(property.name, nextResults);
        this.lookups.update((current) => ({
            ...current,
            [property.name]: {
                ...(current[property.name] ?? this.createEmptyLookupState()),
                term: '',
                isSearching: false,
                error: '',
                results: [],
                selectedGuid: ''
            }
        }));
        this.validateField(property);
    }
    /**
     * Removes the current lookup value and resets the transient search state for that field.
     */
    clearLookup(property) {
        this.setFieldValue(property.name, this.isLookupCollection(property) ? [] : null);
        this.lookups.update((current) => ({
            ...current,
            [property.name]: this.createEmptyLookupState()
        }));
        this.validateField(property);
    }
    endLookupDialogSelection(propertyName) {
        this.lookupDialogSelectionState.update((current) => {
            if (!(propertyName in current)) {
                return current;
            }
            const { [propertyName]: _removed, ...rest } = current;
            return rest;
        });
        if (!this.isAnyLookupDialogOpen() && !this.isDestroyed) {
            this.lookupDialogOpenChange.emit(false);
        }
    }
    isAnyLookupDialogOpen() {
        return Object.keys(this.lookupDialogSelectionState()).length > 0;
    }
    /**
     * Resolves the first usable lookup label from common server payload field names.
     */
    lookupLabel(result) {
        const label = result['Label']
            ?? result['label']
            ?? result['DisplayName']
            ?? result['displayName']
            ?? result['Name']
            ?? result['name']
            ?? result['Guid']
            ?? result['guid'];
        if (typeof label === 'string' && label.trim()) {
            return label.trim();
        }
        if (typeof label === 'number' || typeof label === 'boolean') {
            return String(label);
        }
        return '';
    }
    /**
     * Resolves a short lookup label from common compact-name fields before falling back to the full label.
     */
    lookupShortLabel(result) {
        const shortLabel = result['ShortLabel']
            ?? result['shortLabel']
            ?? result['ShortName']
            ?? result['shortName']
            ?? result['Code'];
        if (typeof shortLabel === 'string' && shortLabel.trim()) {
            return shortLabel.trim();
        }
        if (typeof shortLabel === 'number' || typeof shortLabel === 'boolean') {
            return String(shortLabel);
        }
        return this.lookupLabel(result);
    }
    /**
     * Extracts the lookup Guid using either `Guid` or `guid`.
     */
    lookupGuid(result) {
        const guid = result['Guid'] ?? result['guid'];
        if (typeof guid === 'string' && guid.trim()) {
            return guid.trim();
        }
        return '';
    }
    /**
     * Matches a rendered lookup option against the currently selected single-value lookup Guid.
     */
    isLookupResultSelected(propertyName, result) {
        const selectedGuid = this.lookups()[propertyName]?.selectedGuid ?? '';
        const resultGuid = this.lookupGuid(result);
        if (!resultGuid) {
            return false;
        }
        if (selectedGuid && resultGuid === selectedGuid) {
            return true;
        }
        return this.selectedLookupCollectionEntities(propertyName).some((item) => this.lookupGuid(item) === resultGuid);
    }
    /**
     * Removes one selected entity from a lookup collection while preserving the remaining selection order.
     */
    removeLookupCollectionEntity(property, entity) {
        if (!this.isLookupCollection(property)) {
            return;
        }
        const entityGuid = this.lookupGuid(entity);
        const currentEntities = this.selectedLookupCollectionEntities(property.name);
        const nextEntities = entityGuid
            ? currentEntities.filter((item) => this.lookupGuid(item) !== entityGuid)
            : currentEntities.filter((item) => item !== entity);
        this.setFieldValue(property.name, nextEntities);
        this.validateField(property);
    }
    /**
     * Returns the Angular control for a schema property when the prepared form is available.
     */
    control(propertyName) {
        return this.form()?.controls[propertyName] ?? null;
    }
    // #endregion
    // #region Helper Methods
    /**
     * Executes a lookup query and ignores late responses from older requests so only the newest search wins.
     */
    searchLookup(property, rawSearchTerm) {
        const lookup = this.lookups()[property.name] ?? this.createEmptyLookupState();
        const searchTerm = rawSearchTerm.trim();
        const targetChillType = this.resolveLookupEntityChillType(property);
        const requestSequence = (this.lookupRequestSequence.get(property.name) ?? 0) + 1;
        this.lookupRequestSequence.set(property.name, requestSequence);
        if (!targetChillType) {
            this.setLookupError(property.name, this.chill.T('7E0D5F0F-CDA4-4F49-8E02-A7E0E854B65A', 'Lookup schema is unavailable.', 'Lo schema di ricerca non è disponibile.'));
            return;
        }
        if (!searchTerm) {
            this.setLookupError(property.name, this.chill.T('8B0ED598-819C-42E5-B41A-439F7066EEA9', 'Enter a search value first.', 'Inserisci prima un valore di ricerca.'));
            return;
        }
        this.lookups.update((current) => ({
            ...current,
            [property.name]: {
                ...lookup,
                isSearching: true,
                error: '',
                results: [],
                term: rawSearchTerm
            }
        }));
        this.chill.lookup({
            ChillType: targetChillType,
            Properties: {
                FullTextSearch: searchTerm
            },
            ResultProperties: [
                { Name: 'guid' },
                { Name: 'label' },
                { Name: 'shortLabel' },
                { Name: 'displayName' },
                { Name: 'name' },
                { Name: 'code' }
            ]
        }).subscribe({
            next: (response) => {
                if (this.lookupRequestSequence.get(property.name) !== requestSequence) {
                    return;
                }
                this.lookups.update((current) => ({
                    ...current,
                    [property.name]: {
                        ...(current[property.name] ?? this.createEmptyLookupState()),
                        term: rawSearchTerm,
                        isSearching: false,
                        error: '',
                        results: this.extractLookupResults(response)
                    }
                }));
            },
            error: (error) => {
                if (this.lookupRequestSequence.get(property.name) !== requestSequence) {
                    return;
                }
                this.setLookupError(property.name, this.chill.formatError(error), false);
            }
        });
    }
    /**
     * Reads the current form values for the rendered properties and fills missing values with editor defaults.
     */
    readFormValues(properties) {
        const nextState = {};
        for (const property of properties) {
            const control = this.control(property.name);
            nextState[property.name] = this.normalizeFieldValue(property, control?.value);
        }
        return nextState;
    }
    /**
     * Builds the initial lookup UI state from the already-selected form values.
     */
    createLookupState(properties, fields) {
        const nextState = {};
        for (const property of properties) {
            if (!this.isLookup(property) && !this.isLookupCollection(property)) {
                continue;
            }
            const value = fields[property.name];
            const selectedLabel = this.isJsonObject(value) ? this.lookupLabel(value) : '';
            const selectedShortLabel = this.isJsonObject(value) ? this.lookupShortLabel(value) : '';
            nextState[property.name] = {
                term: this.isLookupCollection(property) ? '' : selectedLabel,
                isSearching: false,
                error: '',
                results: [],
                selectedGuid: this.isJsonObject(value) ? this.lookupGuid(value) : '',
                selectedLabel,
                selectedShortLabel
            };
        }
        return nextState;
    }
    /**
     * Maps undefined form values to the empty value shape expected by the rendered control.
     */
    normalizeFieldValue(property, value) {
        if (value === undefined) {
            return this.isLookupCollection(property) ? [] : '';
        }
        if (property.propertyType === CHILL_PROPERTY_TYPE$1.Json) {
            return typeof value === 'string'
                ? value
                : '';
        }
        return value;
    }
    /**
     * Validates every rendered property and returns only the fields that currently have local errors.
     */
    validateAllFields(properties, fields) {
        const nextErrors = {};
        for (const property of properties) {
            const error = this.getValidationMessage(property, fields[property.name]);
            if (error) {
                nextErrors[property.name] = error;
            }
        }
        return nextErrors;
    }
    /**
     * Revalidates one field and adds or removes its local error entry.
     */
    validateField(property) {
        const message = this.getValidationMessage(property, this.fieldValues()[property.name]);
        this.setLocalError(property.name, message);
    }
    setLocalError(propertyName, message) {
        this.errors.update((current) => {
            if (!message) {
                const { [propertyName]: _, ...rest } = current;
                return rest;
            }
            return {
                ...current,
                [propertyName]: message
            };
        });
    }
    clearLocalError(propertyName) {
        this.errors.update((current) => {
            if (!(propertyName in current)) {
                return current;
            }
            const { [propertyName]: _, ...rest } = current;
            return rest;
        });
    }
    clearDraftTextValue(propertyName) {
        this.draftTextValues.update((current) => {
            if (!(propertyName in current)) {
                return current;
            }
            const { [propertyName]: _, ...rest } = current;
            return rest;
        });
    }
    shouldValidateOnChange(property) {
        return !this.shouldCommitTextOnBlur(property);
    }
    shouldCommitTextOnBlur(property) {
        return property.propertyType === CHILL_PROPERTY_TYPE$1.Date
            || property.propertyType === CHILL_PROPERTY_TYPE$1.Time
            || property.propertyType === CHILL_PROPERTY_TYPE$1.DateTime;
    }
    /**
     * Reads server validation stored on the Angular control so backend errors participate in the merged output.
     */
    readControlValidationMessage(propertyName) {
        const errors = this.control(propertyName)?.errors;
        if (!errors) {
            return '';
        }
        const serverValidation = errors['serverValidation'];
        return typeof serverValidation === 'string'
            ? serverValidation.trim()
            : '';
    }
    /**
     * Routes validation through type-specific rules after handling required and empty-value cases.
     */
    getValidationMessage(property, value) {
        if (this.isEmptyValue(value)) {
            return this.isRequired(property)
                ? this.chill.T('7E64BA1D-8E3B-450D-B03B-A6E2E7B6EC9A', 'This field is required.', 'Questo campo è obbligatorio.')
                : '';
        }
        const propertyType = property.propertyType ?? CHILL_PROPERTY_TYPE$1.Unknown;
        switch (propertyType) {
            case CHILL_PROPERTY_TYPE$1.Guid:
                return this.validateGuid(value);
            case CHILL_PROPERTY_TYPE$1.Integer:
                return this.validateInteger(value, property);
            case CHILL_PROPERTY_TYPE$1.Decimal:
                return this.validateDecimal(value, property);
            case CHILL_PROPERTY_TYPE$1.Date:
                return this.validateDate(value);
            case CHILL_PROPERTY_TYPE$1.Time:
                return this.validateTime(value);
            case CHILL_PROPERTY_TYPE$1.DateTime:
                return this.validateDateTime(value);
            case CHILL_PROPERTY_TYPE$1.Duration:
                return this.validateDuration(value);
            case CHILL_PROPERTY_TYPE$1.Boolean:
                return typeof value === 'boolean' ? '' : this.chill.T('4EB9E8DC-FA6A-45A0-9C95-5814C44144F0', 'Invalid boolean value.', 'Valore booleano non valido.');
            case CHILL_PROPERTY_TYPE$1.String:
            case CHILL_PROPERTY_TYPE$1.Text:
            case CHILL_PROPERTY_TYPE$1.Select:
                return this.validateString(value, property);
            case CHILL_PROPERTY_TYPE$1.Json:
                return this.validateJson(value);
            case CHILL_PROPERTY_TYPE$1.ChillEntity:
            case CHILL_PROPERTY_TYPE$1.ChillQuery:
                return this.isJsonObject(value)
                    ? ''
                    : this.chill.T('5302E408-0D83-4857-8C81-17DCA0DDAF44', 'Select a value from the lookup results.', 'Seleziona un valore dai risultati di ricerca.');
            case CHILL_PROPERTY_TYPE$1.ChillEntityCollection:
                return Array.isArray(value) && value.every((item) => this.isJsonObject(item))
                    ? ''
                    : this.chill.T('5302E408-0D83-4857-8C81-17DCA0DDAF44', 'Select a value from the lookup results.', 'Seleziona un valore dai risultati di ricerca.');
            default:
                return '';
        }
    }
    /**
     * Validates Guid input against the standard GUID format.
     */
    validateGuid(value) {
        if (typeof value !== 'string') {
            return this.chill.T('514D6255-1A59-4D42-95B4-8BB5CFC7A04A', 'Invalid Guid value.', 'Valore Guid non valido.');
        }
        return /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(value.trim())
            ? ''
            : this.chill.T('514D6255-1A59-4D42-95B4-8BB5CFC7A04A', 'Invalid Guid value.', 'Valore Guid non valido.');
    }
    /**
     * Validates integer input and applies configured numeric range rules.
     */
    validateInteger(value, property) {
        const numericValue = this.readNumber(value);
        if (numericValue === null || !Number.isInteger(numericValue)) {
            return this.chill.T('6574C416-B4C8-47D7-9936-A7AE1A0FC437', 'Enter a valid integer.', 'Inserisci un numero intero valido.');
        }
        return this.validateNumericRange(numericValue, property);
    }
    /**
     * Validates decimal input and applies configured numeric range rules.
     */
    validateDecimal(value, property) {
        const numericValue = this.readNumber(value);
        if (numericValue === null) {
            return this.chill.T('4AE9D1D9-D3C5-42DB-BE55-0F322481A87B', 'Enter a valid decimal number.', 'Inserisci un numero decimale valido.');
        }
        return this.validateNumericRange(numericValue, property);
    }
    /**
     * Applies shared min/max metadata checks after numeric parsing has already succeeded.
     */
    validateNumericRange(value, property) {
        const min = this.readMetadataNumber(property, 'min');
        if (min !== null && value < min) {
            return this.chill.T('52D0C7D3-D9DF-47F0-8752-A095BC307331', `Value must be greater than or equal to ${min}.`, `Il valore deve essere maggiore o uguale a ${min}.`);
        }
        const max = this.readMetadataNumber(property, 'max');
        if (max !== null && value > max) {
            return this.chill.T('DDF46D7D-4A1F-4510-9F8D-F77B3D96CF90', `Value must be less than or equal to ${max}.`, `Il valore deve essere minore o uguale a ${max}.`);
        }
        return '';
    }
    /**
     * Validates a date string.
     */
    validateDate(value) {
        if (typeof value !== 'string') {
            return this.chill.T('8EC86C1D-B626-40FB-BEA8-1FE80B66E51F', 'Enter a valid date.', 'Inserisci una data valida.');
        }
        return this.chill.parseDisplayDate(value.trim()) === null
            ? this.chill.T('8EC86C1D-B626-40FB-BEA8-1FE80B66E51F', 'Enter a valid date.', 'Inserisci una data valida.')
            : '';
    }
    /**
     * Validates a time string.
     */
    validateTime(value) {
        if (typeof value !== 'string') {
            return this.chill.T('6E14B3A1-498E-4B11-A8C9-E16189E60AFD', 'Enter a valid time.', 'Inserisci un orario valido.');
        }
        return this.chill.parseDisplayTime(value.trim()) !== null
            ? ''
            : this.chill.T('6E14B3A1-498E-4B11-A8C9-E16189E60AFD', 'Enter a valid time.', 'Inserisci un orario valido.');
    }
    /**
     * Reuses the date-time parser so validation and blur-time normalization accept the same formats.
     */
    validateDateTime(value) {
        if (typeof value !== 'string') {
            return this.chill.T('B08EAAE2-7AA8-45C6-A531-0A37A4DE65F5', 'Enter a valid date and time.', 'Inserisci una data e ora valida.');
        }
        return this.chill.parseDisplayDateTime(value.trim()) === null
            ? this.chill.T('B08EAAE2-7AA8-45C6-A531-0A37A4DE65F5', 'Enter a valid date and time.', 'Inserisci una data e ora valida.')
            : '';
    }
    /**
     * Reuses the duration parser so validation and blur-time normalization stay aligned.
     */
    validateDuration(value) {
        if (typeof value !== 'string') {
            return this.chill.T('3DF867D5-F007-4D15-9579-0F6B6C7BA0EE', 'Enter a valid duration.', 'Inserisci una durata valida.');
        }
        return this.parseDurationDisplayValue(value.trim()) !== null
            ? ''
            : this.chill.T('3DF867D5-F007-4D15-9579-0F6B6C7BA0EE', 'Enter a valid duration.', 'Inserisci una durata valida.');
    }
    /**
     * Validates string values against length and regex metadata rules.
     */
    validateString(value, property) {
        if (typeof value !== 'string') {
            return this.chill.T('FAF662FD-D4D3-46C2-B052-8AA086B72ED2', 'Enter a valid text value.', 'Inserisci un valore testuale valido.');
        }
        const trimmedValue = value.trim();
        if (this.isSelect(property)) {
            const availableValues = this.selectOptions(property).map(([optionValue]) => optionValue);
            if (availableValues.length > 0 && !availableValues.includes(trimmedValue)) {
                return this.chill.T('79AC66D6-7C13-4F0E-8F13-E5236FD09D6C', 'Select a valid option.', 'Seleziona un valore valido.');
            }
        }
        const minLength = this.readMetadataNumber(property, 'minLength');
        if (minLength !== null && trimmedValue.length < minLength) {
            return this.chill.T('CEC26B81-3B54-4B8A-A2C0-8136A7AA61A4', `Value must contain at least ${minLength} characters.`, `Il valore deve contenere almeno ${minLength} caratteri.`);
        }
        const maxLength = this.readMetadataNumber(property, 'maxLength');
        if (maxLength !== null && trimmedValue.length > maxLength) {
            return this.chill.T('A0382F9C-5F39-42BF-9B33-1B92ACDA25A1', `Value must contain at most ${maxLength} characters.`, `Il valore deve contenere al massimo ${maxLength} caratteri.`);
        }
        const pattern = this.metadataString(property, 'pattern');
        if (pattern && !(new RegExp(pattern).test(trimmedValue))) {
            return this.chill.T('D05267DD-7A9E-4099-B69B-D44B0EB23189', 'Value does not match the required format.', 'Il valore non rispetta il formato richiesto.');
        }
        return '';
    }
    /**
     * Validates that the field contains a JSON document while keeping the stored form value as text.
     */
    validateJson(value) {
        if (typeof value !== 'string') {
            return this.chill.T('1D1760FE-3D90-4107-BD6B-D20D7927F5F3', 'Enter a valid JSON value.', 'Inserisci un valore JSON valido.');
        }
        try {
            JSON.parse(value);
            return '';
        }
        catch {
            return this.chill.T('1D1760FE-3D90-4107-BD6B-D20D7927F5F3', 'Enter a valid JSON value.', 'Inserisci un valore JSON valido.');
        }
    }
    /**
     * Converts the raw text entered by the user into the normalized typed value stored in the form.
     */
    normalizeBlurValue(property, value) {
        const trimmedValue = value.trim();
        if (!trimmedValue) {
            return '';
        }
        switch (property.propertyType) {
            case CHILL_PROPERTY_TYPE$1.Integer: {
                return this.chill.parseDisplayInteger(trimmedValue);
            }
            case CHILL_PROPERTY_TYPE$1.Decimal: {
                return this.chill.parseDisplayDecimal(trimmedValue);
            }
            case CHILL_PROPERTY_TYPE$1.Date:
                return this.chill.parseDisplayDate(trimmedValue);
            case CHILL_PROPERTY_TYPE$1.Time:
                return this.chill.parseDisplayTime(trimmedValue);
            case CHILL_PROPERTY_TYPE$1.DateTime:
                return this.chill.parseDisplayDateTime(trimmedValue);
            case CHILL_PROPERTY_TYPE$1.Duration:
                return this.parseDurationDisplayValue(trimmedValue);
            case CHILL_PROPERTY_TYPE$1.Json:
                return value;
            default:
                return trimmedValue;
        }
    }
    /**
     * Parses a user-entered date into the normalized storage format.
     */
    parseDateDisplayValue(value) {
        if (/^\d{4}-\d{2}-\d{2}$/.test(value)) {
            return value;
        }
        const parts = this.parseCultureDateParts(value);
        if (parts) {
            return `${parts.year}-${`${parts.month}`.padStart(2, '0')}-${`${parts.day}`.padStart(2, '0')}`;
        }
        const parsed = new Date(value);
        if (Number.isNaN(parsed.getTime())) {
            return null;
        }
        const year = parsed.getFullYear();
        const month = `${parsed.getMonth() + 1}`.padStart(2, '0');
        const day = `${parsed.getDate()}`.padStart(2, '0');
        return `${year}-${month}-${day}`;
    }
    /**
     * Parses a user-entered time into the normalized storage format.
     */
    parseTimeDisplayValue(value) {
        const match = value.match(/^(\d{1,2}):(\d{1,2})(?::(\d{1,2})(\.\d{1,7})?)?$/);
        if (!match) {
            return null;
        }
        const hours = Number(match[1]);
        const minutes = Number(match[2]);
        const seconds = match[3] ? Number(match[3]) : null;
        const fractional = match[4] ?? '';
        if (hours < 0 || hours > 23 || minutes < 0 || minutes > 59 || (seconds !== null && (seconds < 0 || seconds > 59))) {
            return null;
        }
        const normalizedHours = `${hours}`.padStart(2, '0');
        const normalizedMinutes = `${minutes}`.padStart(2, '0');
        if (seconds === null) {
            return `${normalizedHours}:${normalizedMinutes}`;
        }
        return `${normalizedHours}:${normalizedMinutes}:${`${seconds}`.padStart(2, '0')}${fractional}`;
    }
    /**
     * Accepts ISO-like date-time text first, then falls back to `Date` parsing for looser user input.
     */
    parseDateTimeDisplayValue(value) {
        const directMatch = value.match(/^(\d{4})-(\d{2})-(\d{2})[T\s](\d{1,2}):(\d{2})(?::(\d{2})(\.\d{1,7})?)?(Z|[+-]\d{2}:\d{2})?$/);
        if (directMatch) {
            const [, yearText, monthText, dayText, hourText, minuteText, secondText, fractionText, offsetText] = directMatch;
            const year = Number(yearText);
            const month = Number(monthText);
            const day = Number(dayText);
            const hour = Number(hourText);
            const minute = Number(minuteText);
            const second = secondText ? Number(secondText) : 0;
            if (!this.isValidDateParts(year, month, day) || hour > 23 || minute > 59 || second > 59) {
                return null;
            }
            const normalizedDate = `${yearText}-${monthText}-${dayText}`;
            const normalizedTime = `${`${hour}`.padStart(2, '0')}:${minuteText}:${`${second}`.padStart(2, '0')}`;
            return `${normalizedDate}T${normalizedTime}${fractionText ?? ''}${offsetText ?? ''}`;
        }
        const cultureMatch = value.match(/^(\d{1,4})[\/.-](\d{1,2})[\/.-](\d{1,4})(?:[T\s]+(\d{1,2}):(\d{2})(?::(\d{2})(\.\d{1,7})?)?)?$/);
        if (cultureMatch) {
            const dateParts = this.parseCultureDateParts(`${cultureMatch[1]}/${cultureMatch[2]}/${cultureMatch[3]}`);
            if (!dateParts) {
                return null;
            }
            const hour = cultureMatch[4] ? Number(cultureMatch[4]) : 0;
            const minute = cultureMatch[5] ? Number(cultureMatch[5]) : 0;
            const second = cultureMatch[6] ? Number(cultureMatch[6]) : 0;
            if (hour > 23 || minute > 59 || second > 59) {
                return null;
            }
            const normalizedDate = `${dateParts.year}-${`${dateParts.month}`.padStart(2, '0')}-${`${dateParts.day}`.padStart(2, '0')}`;
            return `${normalizedDate}T${`${hour}`.padStart(2, '0')}:${`${minute}`.padStart(2, '0')}:${`${second}`.padStart(2, '0')}${cultureMatch[7] ?? ''}`;
        }
        const parsed = new Date(value);
        if (Number.isNaN(parsed.getTime())) {
            return null;
        }
        const year = parsed.getFullYear();
        const month = `${parsed.getMonth() + 1}`.padStart(2, '0');
        const day = `${parsed.getDate()}`.padStart(2, '0');
        const hour = `${parsed.getHours()}`.padStart(2, '0');
        const minute = `${parsed.getMinutes()}`.padStart(2, '0');
        const second = `${parsed.getSeconds()}`.padStart(2, '0');
        return `${year}-${month}-${day}T${hour}:${minute}:${second}`;
    }
    /**
     * Accepts both ISO durations and `d.hh:mm[:ss]`-style values and normalizes them for storage.
     */
    parseDurationDisplayValue(value) {
        if (/^P(?!$)(\d+D)?(T(\d+H)?(\d+M)?(\d+S)?)?$/i.test(value)) {
            return value.toUpperCase();
        }
        const match = value.match(/^(?:(\d+)\.)?(\d{1,2}):(\d{1,2})(?::(\d{1,2})(\.\d{1,7})?)?$/);
        if (!match) {
            return null;
        }
        const days = match[1] ? Number(match[1]) : null;
        const hours = Number(match[2]);
        const minutes = Number(match[3]);
        const seconds = match[4] ? Number(match[4]) : null;
        const fractional = match[5] ?? '';
        if ((days !== null && days < 0) || hours < 0 || hours > 23 || minutes < 0 || minutes > 59 || (seconds !== null && (seconds < 0 || seconds > 59))) {
            return null;
        }
        const normalizedHours = `${hours}`.padStart(2, '0');
        const normalizedMinutes = `${minutes}`.padStart(2, '0');
        const dayPrefix = days !== null ? `${days}.` : '';
        if (seconds === null) {
            return `${dayPrefix}${normalizedHours}:${normalizedMinutes}`;
        }
        return `${dayPrefix}${normalizedHours}:${normalizedMinutes}:${`${seconds}`.padStart(2, '0')}${fractional}`;
    }
    /**
     * Validates year, month, and day parts before composing a normalized date.
     */
    isValidDateParts(year, month, day) {
        if (month < 1 || month > 12 || day < 1 || day > 31) {
            return false;
        }
        const candidate = new Date(year, month - 1, day);
        return candidate.getFullYear() === year
            && candidate.getMonth() === month - 1
            && candidate.getDate() === day;
    }
    /**
     * Parses culture-aware short date input using the configured Chill UI culture.
     */
    parseCultureDateParts(value) {
        const normalizedValue = value.trim();
        const separatorMatch = normalizedValue.match(/^(\d{1,4})[\/.-](\d{1,2})[\/.-](\d{1,4})$/);
        if (!separatorMatch) {
            return null;
        }
        const left = Number(separatorMatch[1]);
        const middle = Number(separatorMatch[2]);
        const right = Number(separatorMatch[3]);
        if (!Number.isInteger(left) || !Number.isInteger(middle) || !Number.isInteger(right)) {
            return null;
        }
        if (separatorMatch[1].length === 4) {
            return this.isValidDateParts(left, middle, right)
                ? { year: left, month: middle, day: right }
                : null;
        }
        const culture = this.chill.currentCultureName().toLowerCase();
        const isMonthFirstCulture = culture === 'en-us';
        const month = isMonthFirstCulture ? left : middle;
        const day = isMonthFirstCulture ? middle : left;
        const year = right < 100 ? 2000 + right : right;
        return this.isValidDateParts(year, month, day)
            ? { year, month, day }
            : null;
    }
    /**
     * Resolves the entity type targeted by a lookup property from explicit schema fields or metadata fallbacks.
     */
    resolveLookupEntityChillType(property) {
        return property.referenceChillType?.trim()
            || property.referenceChillTypeQuery?.trim()
            || this.metadataString(property, 'ChillEntityTypeName')
            || this.metadataString(property, 'chillEntityTypeName')
            || this.metadataString(property, 'referenceChillTypeQuery')
            || this.metadataString(property, 'ReferenceChillTypeQuery')
            || '';
    }
    /**
     * Chooses the query schema used by the ellipsis picker, preferring explicit schema hints over inferred defaults.
     */
    resolveLookupQueryChillType(property, entityChillType) {
        const explicitQueryType = property.referenceChillTypeQuery?.trim()
            || this.metadataString(property, 'referenceChillTypeQuery')
            || this.metadataString(property, 'ReferenceChillTypeQuery')
            || '';
        if (explicitQueryType) {
            return explicitQueryType;
        }
        if (!entityChillType) {
            return '';
        }
        // Some schemas expose only the entity Chill type and use that same type for querying.
        if (property.referenceChillType?.trim() || this.metadataString(property, 'ChillEntityTypeName') || this.metadataString(property, 'chillEntityTypeName')) {
            return entityChillType;
        }
        const entityTypeName = entityChillType.split('.').pop()?.trim() ?? '';
        if (!entityTypeName) {
            return '';
        }
        const candidates = [
            `${entityChillType}Query`,
            `Model.Query.${entityTypeName}Query`,
            `Model.General.${entityTypeName}Query`
        ];
        return candidates.find((candidate) => candidate.trim().length > 0) ?? '';
    }
    /**
     * Derives the dialog-specific view code from the caller schema view code.
     */
    resolveLookupDialogViewCode() {
        const currentViewCode = this.schema()?.chillViewCode?.trim() ?? '';
        if (!currentViewCode || currentViewCode.toLowerCase() === 'default') {
            return 'dialog';
        }
        return `${currentViewCode}.dialog`;
    }
    /**
     * Formats normalized storage dates into the user culture short-date representation.
     */
    formatDateDisplayValue(value) {
        const normalizedValue = value.trim();
        const match = normalizedValue.match(/^(\d{4})-(\d{2})-(\d{2})$/);
        if (!match) {
            return normalizedValue;
        }
        const year = Number(match[1]);
        const month = Number(match[2]);
        const day = Number(match[3]);
        if (!this.isValidDateParts(year, month, day)) {
            return normalizedValue;
        }
        return new Intl.DateTimeFormat(this.chill.currentCultureName(), {
            year: 'numeric',
            month: '2-digit',
            day: '2-digit'
        }).format(new Date(year, month - 1, day));
    }
    /**
     * Formats normalized storage date-times into the user culture date order while preserving the typed time.
     */
    formatDateTimeDisplayValue(value) {
        const normalizedValue = value.trim();
        const match = normalizedValue.match(/^(\d{4}-\d{2}-\d{2})T(\d{2}:\d{2})(?::(\d{2})(\.\d{1,7})?)?(Z|[+-]\d{2}:\d{2})?$/);
        if (!match) {
            return normalizedValue;
        }
        const formattedDate = this.formatDateDisplayValue(match[1]);
        const seconds = match[3] && match[3] !== '00' ? `:${match[3]}` : '';
        const fraction = match[4] ?? '';
        const offset = match[5] ?? '';
        return `${formattedDate} ${match[2]}${seconds}${fraction}${offset}`;
    }
    /**
     * Formats normalized storage time values as `HH:MM`, keeping seconds only when they are non-zero.
     */
    formatTimeDisplayValue(value) {
        return this.chill.formatDisplayTime(value);
    }
    /**
     * Reads numeric validation metadata such as min, max, or length constraints.
     */
    readMetadataNumber(property, key) {
        const rawValue = this.metadataString(property, key);
        if (!rawValue) {
            return null;
        }
        const parsedValue = Number(rawValue);
        return Number.isFinite(parsedValue)
            ? parsedValue
            : null;
    }
    /**
     * Checks whether a property is marked as required in metadata.
     */
    isRequired(property) {
        const rawRequired = this.metadataString(property, 'required').toLowerCase();
        return rawRequired === 'true'
            || rawRequired === '1'
            || rawRequired === 'required';
    }
    /**
     * Reads string metadata defensively so structured metadata values do not break string-only callers.
     */
    metadataString(property, key) {
        const value = property.metadata?.[key];
        return typeof value === 'string'
            ? value.trim()
            : typeof value === 'number'
                ? String(value)
                : '';
    }
    /**
     * Treats nullish values, blank strings, and empty arrays as empty for required validation.
     */
    isEmptyValue(value) {
        if (value === undefined || value === null) {
            return true;
        }
        if (typeof value === 'string') {
            return value.trim().length === 0;
        }
        if (Array.isArray(value)) {
            return value.length === 0;
        }
        return false;
    }
    /**
     * Converts numeric strings and finite numbers into a comparable numeric value.
     */
    readNumber(value) {
        return this.chill.readDisplayNumber(value);
    }
    /**
     * Excludes unsupported schema properties from rendering.
     */
    shouldSkipProperty(property) {
        return property.propertyType === CHILL_PROPERTY_TYPE$1.Unknown;
    }
    /**
     * Searches common API wrapper properties until it finds an array of lookup objects.
     */
    extractLookupResults(response) {
        const candidates = [
            response,
            response['Results'],
            response['results'],
            response['Entities'],
            response['entities'],
            response['Items'],
            response['items'],
            response['Value'],
            response['value'],
            response['Data'],
            response['data']
        ];
        for (const candidate of candidates) {
            if (!Array.isArray(candidate)) {
                continue;
            }
            const results = candidate.filter((item) => this.isJsonObject(item));
            if (results.length > 0) {
                return results;
            }
        }
        return [];
    }
    /**
     * Writes a lookup error and optionally keeps the last result list visible for recovery.
     */
    setLookupError(propertyName, message, preserveResults = true) {
        this.lookups.update((current) => ({
            ...current,
            [propertyName]: {
                ...(current[propertyName] ?? this.createEmptyLookupState()),
                isSearching: false,
                error: message,
                results: preserveResults ? (current[propertyName]?.results ?? []) : []
            }
        }));
    }
    /**
     * Creates the default empty lookup state object.
     */
    createEmptyLookupState() {
        return {
            term: '',
            isSearching: false,
            error: '',
            results: [],
            selectedGuid: '',
            selectedLabel: '',
            selectedShortLabel: ''
        };
    }
    /**
     * Debounces lookup requests so rapid typing collapses into a single backend query.
     */
    scheduleLookupSearch(property, term) {
        this.cancelLookupSearch(property.name);
        this.lookupSearchTimers.set(property.name, setTimeout(() => {
            this.lookupSearchTimers.delete(property.name);
            this.searchLookup(property, term);
        }, 250));
    }
    /**
     * Cancels any pending debounced lookup search for a property.
     */
    cancelLookupSearch(propertyName) {
        const timer = this.lookupSearchTimers.get(propertyName);
        if (timer) {
            clearTimeout(timer);
            this.lookupSearchTimers.delete(propertyName);
        }
    }
    /**
     * Compares two lookup labels in a case-insensitive, trimmed form.
     */
    matchesLookupLabel(left, right) {
        return left.trim().toLowerCase() === right.trim().toLowerCase();
    }
    /**
     * Emits only the blurred field and its latest cached value to match the parent component contract.
     */
    notifyFieldBlur(propertyName) {
        if (this.isDestroyed) {
            return;
        }
        this.fieldBlur.emit({
            [propertyName]: this.fieldValues()[propertyName]
        });
    }
    /**
     * Keeps the Angular control and the local signal cache synchronized when the component updates a field itself.
     */
    setFieldValue(propertyName, value) {
        const control = this.control(propertyName);
        const valueChanged = !Object.is(control?.value ?? null, value ?? null);
        control?.setValue(value);
        if (control && valueChanged) {
            control.markAsDirty();
            control.markAsTouched();
        }
        this.fieldValues.update((current) => ({
            ...current,
            [propertyName]: value
        }));
    }
    /**
     * Rebuilds lookup display text when the underlying form value changes outside the lookup UI handlers.
     */
    syncLookupState(property, value) {
        if (!this.isLookup(property) && !this.isLookupCollection(property)) {
            return;
        }
        this.lookups.update((current) => ({
            ...current,
            [property.name]: {
                ...(current[property.name] ?? this.createEmptyLookupState()),
                term: this.isLookupCollection(property)
                    ? (current[property.name]?.term ?? '')
                    : this.isJsonObject(value) ? this.lookupLabel(value) : (current[property.name]?.term ?? ''),
                selectedGuid: this.isJsonObject(value) ? this.lookupGuid(value) : '',
                selectedLabel: this.isJsonObject(value) ? this.lookupLabel(value) : '',
                selectedShortLabel: this.isJsonObject(value) ? this.lookupShortLabel(value) : ''
            }
        }));
    }
    /**
     * Builds a comma-separated summary from a lookup collection value.
     */
    lookupCollectionSummaryFromValue(value) {
        return Array.isArray(value)
            ? value.filter((item) => this.isJsonObject(item)).map((item) => this.lookupLabel(item)).filter((item) => item.length > 0).join(', ')
            : '';
    }
    /**
     * Appends a newly selected lookup entity to a collection and resets the live search slot.
     */
    appendLookupCollectionResult(property, result) {
        const nextResults = this.mergeLookupCollectionResults(this.selectedLookupCollectionEntities(property.name), [result]);
        this.setFieldValue(property.name, nextResults);
        this.lookups.update((current) => ({
            ...current,
            [property.name]: {
                ...(current[property.name] ?? this.createEmptyLookupState()),
                term: '',
                isSearching: false,
                error: '',
                results: [],
                selectedGuid: ''
            }
        }));
        this.validateField(property);
    }
    /**
     * Merges collection lookup selections without duplicating the same entity Guid.
     */
    mergeLookupCollectionResults(existing, incoming) {
        const merged = [...existing];
        const seenGuids = new Set(existing.map((item) => this.lookupGuid(item)).filter((guid) => guid.length > 0));
        for (const item of incoming) {
            const guid = this.lookupGuid(item);
            if (guid) {
                if (seenGuids.has(guid)) {
                    continue;
                }
                seenGuids.add(guid);
                merged.push(item);
                continue;
            }
            if (!merged.includes(item)) {
                merged.push(item);
            }
        }
        return merged;
    }
    /**
     * Checks whether a JSON value is a non-array object.
     */
    isJsonObject(value) {
        return !!value && typeof value === 'object' && !Array.isArray(value);
    }
    /**
     * Avoids resetting field signals when the computed field map is unchanged.
     */
    areRecordsEqual(left, right) {
        const leftKeys = Object.keys(left);
        const rightKeys = Object.keys(right);
        if (leftKeys.length !== rightKeys.length) {
            return false;
        }
        return leftKeys.every((key) => Object.is(left[key], right[key]));
    }
    /**
     * Avoids rewriting error state when the same field messages are already stored.
     */
    areStringRecordsEqual(left, right) {
        const leftKeys = Object.keys(left);
        const rightKeys = Object.keys(right);
        if (leftKeys.length !== rightKeys.length) {
            return false;
        }
        return leftKeys.every((key) => left[key] === right[key]);
    }
    /**
     * Prevents lookup signal churn by comparing the full lookup state map before writing it.
     */
    areLookupStatesEqual(left, right) {
        const leftKeys = Object.keys(left);
        const rightKeys = Object.keys(right);
        if (leftKeys.length !== rightKeys.length) {
            return false;
        }
        return leftKeys.every((key) => this.areLookupStateEntriesEqual(left[key], right[key]));
    }
    /**
     * Compares the user-visible parts of two lookup entries, including result ordering.
     */
    areLookupStateEntriesEqual(left, right) {
        if (!left || !right) {
            return left === right;
        }
        return left.term === right.term
            && left.isSearching === right.isSearching
            && left.error === right.error
            && left.selectedGuid === right.selectedGuid
            && left.selectedLabel === right.selectedLabel
            && left.selectedShortLabel === right.selectedShortLabel
            && left.results.length === right.results.length
            && left.results.every((item, index) => item === right.results[index]);
    }
    closeLookupResults(propertyName) {
        this.lookups.update((current) => {
            const lookup = current[propertyName];
            if (!lookup) {
                return current;
            }
            return {
                ...current,
                [propertyName]: {
                    ...lookup,
                    results: []
                }
            };
        });
    }
    static { this.ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: ChillPolymorphicInputComponent, deps: [], target: i0.ɵɵFactoryTarget.Component }); }
    static { this.ɵcmp = i0.ɵɵngDeclareComponent({ minVersion: "17.0.0", version: "19.2.21", type: ChillPolymorphicInputComponent, isStandalone: true, selector: "app-chill-polymorphic-input", inputs: { form: { classPropertyName: "form", publicName: "form", isSignal: true, isRequired: false, transformFunction: null }, schema: { classPropertyName: "schema", publicName: "schema", isSignal: true, isRequired: false, transformFunction: null }, propertyNames: { classPropertyName: "propertyNames", publicName: "propertyNames", isSignal: true, isRequired: false, transformFunction: null }, readonlyPropertyNames: { classPropertyName: "readonlyPropertyNames", publicName: "readonlyPropertyNames", isSignal: true, isRequired: false, transformFunction: null }, externalErrors: { classPropertyName: "externalErrors", publicName: "externalErrors", isSignal: true, isRequired: false, transformFunction: null }, showLabels: { classPropertyName: "showLabels", publicName: "showLabels", isSignal: true, isRequired: false, transformFunction: null } }, outputs: { valueChange: "valueChange", validityChange: "validityChange", fieldBlur: "fieldBlur", lookupDialogOpenChange: "lookupDialogOpenChange", editorDialogOpenChange: "editorDialogOpenChange" }, ngImport: i0, template: "<div class=\"polymorphic-fields\">\n  @for (property of properties(); track property.name) {\n    <label class=\"field\">\n      @if (showLabels()) {\n        <span>{{ property.displayName || property.name }}</span>\n      }\n\n      @if (isCheckbox(property)) {\n        @if (control(property.name); as fieldControl) {\n          <input\n            type=\"checkbox\"\n            [formControl]=\"fieldControl\"\n            [disabled]=\"isPropertyReadOnly(property.name)\"\n            (blur)=\"emitFieldBlur(property.name)\"\n            [name]=\"property.name\" />\n        }\n      } @else if (isLookupCollection(property)) {\n        <div class=\"lookup\">\n          @for (entity of selectedLookupCollectionEntities(property.name); track lookupGuid(entity) || $index) {\n            <div class=\"lookup-selected\" [title]=\"lookupLabel(entity)\">\n              <span class=\"lookup-selected__label lookup-selected__label--full\">\n                {{ lookupLabel(entity) || chill.T('B93CA88B-01FE-44B6-9C2F-C9878A7B324B', 'Select value', 'Seleziona valore') }}\n              </span>\n              <span class=\"lookup-selected__label lookup-selected__label--short\">\n                {{ lookupShortLabel(entity) || lookupLabel(entity) || chill.T('B93CA88B-01FE-44B6-9C2F-C9878A7B324B', 'Select value', 'Seleziona valore') }}\n              </span>\n              <button\n                type=\"button\"\n                class=\"lookup-selected__clear\"\n                (click)=\"removeLookupCollectionEntity(property, entity)\"\n                [disabled]=\"isPropertyReadOnly(property.name)\"\n                [attr.aria-label]=\"chill.T('34015BA4-E0CA-460E-B82B-A4E2D4D8A184', 'Clear', 'Pulisci')\">\n                X\n              </button>\n            </div>\n          }\n\n          <div class=\"lookup-bar\">\n            <div class=\"lookup-input-slot\" cdkOverlayOrigin #lookupOrigin=\"cdkOverlayOrigin\" #lookupOriginElement>\n              <input\n                type=\"text\"\n                [ngModel]=\"lookupTerm(property.name)\"\n                (ngModelChange)=\"updateLookupTerm(property, $event)\"\n                (focus)=\"handleLookupFocus(property)\"\n                (blur)=\"handleLookupBlur(property.name)\"\n                [disabled]=\"isPropertyReadOnly(property.name)\"\n                [name]=\"property.name\"\n                [placeholder]=\"placeholder(property) || chill.T('FA7B8E01-658C-4D63-B53F-D476CD697892', 'Search entity', 'Cerca entita')\" />\n            </div>\n            @if (canOpenLookupDialog(property)) {\n              <button\n                type=\"button\"\n                class=\"lookup-dialog\"\n                (mousedown)=\"beginLookupDialogSelection(property.name)\"\n                (click)=\"openLookupDialog(property)\"\n                [disabled]=\"isPropertyReadOnly(property.name)\"\n                [attr.aria-label]=\"chill.T('B6D48459-73D4-4234-977E-8D79E510A20D', 'Open entity picker', 'Apri selettore entita')\">\n                ...\n              </button>\n            }\n            <button type=\"button\" class=\"lookup-clear\" (click)=\"clearLookup(property)\" [disabled]=\"isPropertyReadOnly(property.name)\">\n              {{ chill.T('34015BA4-E0CA-460E-B82B-A4E2D4D8A184', 'Clear', 'Pulisci') }}\n            </button>\n          </div>\n\n          <ng-template\n            cdkConnectedOverlay\n            [cdkConnectedOverlayOrigin]=\"lookupOrigin\"\n            [cdkConnectedOverlayOpen]=\"lookupResults(property.name).length > 0\"\n            [cdkConnectedOverlayPositions]=\"lookupOverlayPositions\"\n            [cdkConnectedOverlayPush]=\"true\"\n            [cdkConnectedOverlayFlexibleDimensions]=\"true\"\n            [cdkConnectedOverlayViewportMargin]=\"8\"\n            [cdkConnectedOverlayOffsetY]=\"6\">\n            <div\n              class=\"lookup-results\"\n              role=\"listbox\"\n              [style.--lookup-overlay-width.px]=\"lookupOverlayWidth(lookupOriginElement)\">\n              @for (result of lookupResults(property.name); track $index) {\n                <button\n                  type=\"button\"\n                  class=\"lookup-result\"\n                  [class.is-selected]=\"isLookupResultSelected(property.name, result)\"\n                  (mousedown)=\"$event.preventDefault()\"\n                  (click)=\"selectLookupResult(property, result)\">\n                  {{ lookupLabel(result) || chill.T('B93CA88B-01FE-44B6-9C2F-C9878A7B324B', 'Select value', 'Seleziona valore') }}\n                </button>\n              }\n            </div>\n          </ng-template>\n\n          @if (lookupError(property.name)) {\n            <small class=\"field-error\">{{ lookupError(property.name) }}</small>\n          }\n\n          @if (lookupIsSearching(property.name)) {\n            <small class=\"lookup-status\">\n              {{ chill.T('ABAF5996-C2BB-4F85-BE0F-CC75883A648B', 'Searching...', 'Ricerca in corso...') }}\n            </small>\n          }\n        </div>\n      } @else if (isLookup(property)) {\n        <div class=\"lookup\">\n          @if (property.propertyType === 1000 && hasSelectedLookupEntity(property.name)) {\n            <div class=\"lookup-selected\" [title]=\"selectedLookupLabel(property.name)\">\n              <span class=\"lookup-selected__label lookup-selected__label--full\">\n                {{ selectedLookupLabel(property.name) || chill.T('B93CA88B-01FE-44B6-9C2F-C9878A7B324B', 'Select value', 'Seleziona valore') }}\n              </span>\n              <span class=\"lookup-selected__label lookup-selected__label--short\">\n                {{ selectedLookupShortLabel(property.name) || selectedLookupLabel(property.name) || chill.T('B93CA88B-01FE-44B6-9C2F-C9878A7B324B', 'Select value', 'Seleziona valore') }}\n              </span>\n              <button\n                type=\"button\"\n                class=\"lookup-selected__clear\"\n                (click)=\"clearLookup(property)\"\n                [disabled]=\"isPropertyReadOnly(property.name)\"\n                [attr.aria-label]=\"chill.T('34015BA4-E0CA-460E-B82B-A4E2D4D8A184', 'Clear', 'Pulisci')\">\n                X\n              </button>\n            </div>\n          } @else {\n            <div class=\"lookup-bar\">\n              <div class=\"lookup-input-slot\" cdkOverlayOrigin #lookupOrigin=\"cdkOverlayOrigin\" #lookupOriginElement>\n                <input\n                  type=\"text\"\n                  [ngModel]=\"lookupTerm(property.name)\"\n                  (ngModelChange)=\"updateLookupTerm(property, $event)\"\n                  (focus)=\"handleLookupFocus(property)\"\n                  (blur)=\"handleLookupBlur(property.name)\"\n                  [disabled]=\"isPropertyReadOnly(property.name)\"\n                  [name]=\"property.name\"\n                  [placeholder]=\"placeholder(property) || chill.T('FA7B8E01-658C-4D63-B53F-D476CD697892', 'Search entity', 'Cerca entita')\" />\n              </div>\n              @if (canOpenLookupDialog(property)) {\n                <button\n                  type=\"button\"\n                  class=\"lookup-dialog\"\n                  (mousedown)=\"beginLookupDialogSelection(property.name)\"\n                  (click)=\"openLookupDialog(property)\"\n                  [disabled]=\"isPropertyReadOnly(property.name)\"\n                  [attr.aria-label]=\"chill.T('B6D48459-73D4-4234-977E-8D79E510A20D', 'Open entity picker', 'Apri selettore entita')\">\n                  ...\n                </button>\n              }\n            </div>\n\n            <ng-template\n              cdkConnectedOverlay\n              [cdkConnectedOverlayOrigin]=\"lookupOrigin\"\n              [cdkConnectedOverlayOpen]=\"lookupResults(property.name).length > 0\"\n              [cdkConnectedOverlayPositions]=\"lookupOverlayPositions\"\n              [cdkConnectedOverlayPush]=\"true\"\n              [cdkConnectedOverlayFlexibleDimensions]=\"true\"\n              [cdkConnectedOverlayViewportMargin]=\"8\"\n              [cdkConnectedOverlayOffsetY]=\"6\">\n              <div\n                class=\"lookup-results\"\n                role=\"listbox\"\n                [style.--lookup-overlay-width.px]=\"lookupOverlayWidth(lookupOriginElement)\">\n                @for (result of lookupResults(property.name); track $index) {\n                  <button\n                    type=\"button\"\n                    class=\"lookup-result\"\n                    [class.is-selected]=\"isLookupResultSelected(property.name, result)\"\n                    (mousedown)=\"$event.preventDefault()\"\n                    (click)=\"selectLookupResult(property, result)\">\n                    {{ lookupLabel(result) || chill.T('B93CA88B-01FE-44B6-9C2F-C9878A7B324B', 'Select value', 'Seleziona valore') }}\n                  </button>\n                }\n              </div>\n            </ng-template>\n          }\n\n          @if (lookupIsSearching(property.name)) {\n            <small class=\"lookup-status\">\n              {{ chill.T('ABAF5996-C2BB-4F85-BE0F-CC75883A648B', 'Searching...', 'Ricerca in corso...') }}\n            </small>\n          }\n\n          @if (lookupError(property.name)) {\n            <small class=\"field-error\">{{ lookupError(property.name) }}</small>\n          }\n        </div>\n      } @else if (isTextarea(property)) {\n        @if (control(property.name); as fieldControl) {\n          <textarea\n            rows=\"4\"\n            [formControl]=\"fieldControl\"\n            [disabled]=\"isPropertyReadOnly(property.name)\"\n            (blur)=\"emitFieldBlur(property.name)\"\n            [name]=\"property.name\"\n            [placeholder]=\"placeholder(property)\"></textarea>\n        }\n      } @else if (isJsonEditor(property)) {\n        <div class=\"editor-field\">\n          <app-chill-json-input\n            [value]=\"textValue(property.name)\"\n            [language]=\"editorLanguage(property)\"\n            [placeholder]=\"placeholder(property)\"\n            [invalid]=\"!!validationMessage(property.name)\"\n            [disabled]=\"isPropertyReadOnly(property.name) || (control(property.name)?.disabled ?? false)\"\n            (valueChange)=\"updateJsonInput(property.name, $event)\"\n            (blur)=\"emitFieldBlur(property.name)\"></app-chill-json-input>\n          <button\n            type=\"button\"\n            class=\"editor-dialog-button\"\n            (mousedown)=\"beginEditorDialogSelection(property.name)\"\n            (click)=\"openEditorDialog(property)\"\n            [attr.aria-label]=\"chill.T('EC935816-DCBC-4AE7-BA13-36D66A7D7EBD', 'Open editor dialog', 'Apri editor in dialog')\">\n            <span class=\"material-symbol-icon\" aria-hidden=\"true\">open_in_full</span>\n          </button>\n        </div>\n      } @else if (isSelect(property)) {\n        <select\n          [ngModel]=\"selectValue(property.name)\"\n          (ngModelChange)=\"updateSelectValue(property, $event)\"\n          [disabled]=\"isPropertyReadOnly(property.name)\"\n          [name]=\"property.name\">\n          @for (option of selectOptions(property); track option[0] + ':' + option[1]) {\n            <option [value]=\"option[0]\">{{ option[1] }}</option>\n          }\n        </select>\n      } @else {\n        @if (control(property.name); as fieldControl) {\n          @if (isFormattedTextInput(property)) {\n            <input\n              type=\"text\"\n              [ngModel]=\"textValue(property.name)\"\n              (ngModelChange)=\"updateTextInput(property.name, $event)\"\n              (blur)=\"normalizeTextOnBlur(property)\"\n              [disabled]=\"isPropertyReadOnly(property.name)\"\n              [name]=\"property.name\"\n              [placeholder]=\"placeholder(property)\" />\n          } @else {\n            <input\n              [type]=\"inputType(property)\"\n              [step]=\"inputStep(property)\"\n              [formControl]=\"fieldControl\"\n              (blur)=\"normalizeTextOnBlur(property)\"\n              [name]=\"property.name\"\n              [placeholder]=\"placeholder(property)\" />\n          }\n        }\n      }\n\n      @if (validationMessage(property.name)) {\n        <small class=\"field-error\">{{ validationMessage(property.name) }}</small>\n      }\n    </label>\n  }\n</div>\n", styles: [":host{display:block}.polymorphic-fields{display:grid;gap:.85rem}.field{display:grid;gap:.45rem}.field span{font-weight:600;color:var(--text-main)}.field input,.field select,.field textarea{width:100%;padding:.75rem .85rem;border-radius:.45rem;border:1px solid color-mix(in srgb,var(--accent) 26%,var(--border-color));background:linear-gradient(180deg,color-mix(in srgb,var(--accent-soft) 32%,transparent),transparent 30%),linear-gradient(180deg,color-mix(in srgb,var(--surface-0) 96%,rgba(2,16,25,.14)),color-mix(in srgb,var(--surface-1) 92%,rgba(1,10,18,.18)));color:var(--text-main);box-shadow:inset 0 0 0 1px color-mix(in srgb,var(--accent) 8%,transparent),0 0 .7rem color-mix(in srgb,var(--accent) 6%,transparent)}.field input[type=checkbox]{width:1rem;height:1rem;padding:0}.field textarea{resize:vertical}.field app-chill-json-input{width:100%}.editor-field{position:relative;min-width:0}.editor-dialog-button{position:absolute;top:.35rem;right:.35rem;z-index:1;display:inline-grid;place-items:center;width:2rem;height:2rem;padding:0;border:1px solid color-mix(in srgb,var(--accent) 26%,var(--border-color));border-radius:.4rem;background:color-mix(in srgb,var(--surface-0) 92%,transparent);color:var(--text-main);box-shadow:var(--shadow-soft);cursor:pointer}.editor-dialog-button .material-symbol-icon{font-size:1.05rem}.field-error{color:var(--danger);font-size:.85rem}.lookup{display:grid;gap:.55rem;position:relative;container-type:inline-size}.lookup-bar{display:grid;grid-template-columns:minmax(0,1fr) auto auto;gap:.5rem;align-items:start}.lookup-input-slot{min-width:0}.lookup-dialog,.lookup-clear,.lookup-result{min-height:2.5rem;padding:.65rem .85rem;border:1px solid color-mix(in srgb,var(--accent) 26%,var(--border-color));border-radius:.45rem;background:linear-gradient(180deg,color-mix(in srgb,var(--accent-soft) 32%,transparent),transparent 30%),linear-gradient(180deg,color-mix(in srgb,var(--surface-0) 96%,rgba(2,16,25,.14)),color-mix(in srgb,var(--surface-1) 92%,rgba(1,10,18,.18)));color:var(--text-main);box-shadow:inset 0 0 0 1px color-mix(in srgb,var(--accent) 8%,transparent),0 0 .7rem color-mix(in srgb,var(--accent) 6%,transparent);cursor:pointer}.lookup-selected{display:grid;grid-template-columns:minmax(0,1fr) auto;align-items:center;gap:.65rem;width:100%;min-height:2.75rem;padding:.5rem .6rem .5rem .85rem;border:1px solid color-mix(in srgb,var(--accent) 26%,var(--border-color));border-radius:999px;background:linear-gradient(180deg,color-mix(in srgb,var(--accent-soft) 32%,transparent),transparent 30%),linear-gradient(180deg,color-mix(in srgb,var(--surface-0) 96%,rgba(2,16,25,.14)),color-mix(in srgb,var(--surface-1) 92%,rgba(1,10,18,.18)));color:var(--text-main);box-shadow:inset 0 0 0 1px color-mix(in srgb,var(--accent) 8%,transparent),0 0 .7rem color-mix(in srgb,var(--accent) 6%,transparent)}.lookup-selected__label{min-width:0;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;font-weight:500}.lookup-selected__label--short{display:none}.lookup-selected__clear{width:1.9rem;height:1.9rem;border:0;border-radius:999px;background:color-mix(in srgb,var(--accent-soft) 62%,var(--surface-0));color:var(--text-main);cursor:pointer;font-size:.9rem;line-height:1}:root[data-theme=dark] .field input,:root[data-theme=dark] .field select,:root[data-theme=dark] .field textarea,:root[data-theme=dark] .editor-dialog-button,:root[data-theme=dark] .lookup-dialog,:root[data-theme=dark] .lookup-clear,:root[data-theme=dark] .lookup-result,:root[data-theme=dark] .lookup-selected{background:#09131a94}.lookup-status{color:var(--text-muted);font-size:.85rem}.lookup-results{display:grid;gap:.45rem;width:var(--lookup-overlay-width, 100%);min-width:14rem;max-width:calc(100vw - 16px);max-height:400px;overflow-y:auto;padding:.35rem;border:1px solid color-mix(in srgb,var(--accent) 20%,var(--border-color));border-radius:.7rem;background:color-mix(in srgb,var(--surface-0) 96%,rgba(2,16,25,.14));box-shadow:var(--shadow)}.lookup-tags{display:flex;flex-wrap:wrap;gap:.45rem}.lookup-tag{display:inline-flex;align-items:center;min-height:2rem;padding:.35rem .7rem;border:1px solid color-mix(in srgb,var(--accent) 22%,var(--border-color));border-radius:999px;background:color-mix(in srgb,var(--accent-soft) 42%,var(--surface-0));color:var(--text-main);font-size:.85rem}.lookup-result{text-align:left}.lookup-result.is-selected{border-color:color-mix(in srgb,var(--accent) 55%,var(--border-color));background:linear-gradient(180deg,color-mix(in srgb,var(--accent-soft) 70%,transparent),transparent 35%),linear-gradient(180deg,color-mix(in srgb,var(--surface-0) 98%,rgba(2,16,25,.14)),color-mix(in srgb,var(--surface-1) 94%,rgba(1,10,18,.18)))}@media(max-width:720px){.lookup-bar{grid-template-columns:1fr}}@container (max-width: 16rem){.lookup-selected__label--full{display:none}.lookup-selected__label--short{display:inline}}\n"], dependencies: [{ kind: "ngmodule", type: CommonModule }, { kind: "ngmodule", type: FormsModule }, { kind: "directive", type: i1.NgSelectOption, selector: "option", inputs: ["ngValue", "value"] }, { kind: "directive", type: i1.ɵNgSelectMultipleOption, selector: "option", inputs: ["ngValue", "value"] }, { kind: "directive", type: i1.DefaultValueAccessor, selector: "input:not([type=checkbox])[formControlName],textarea[formControlName],input:not([type=checkbox])[formControl],textarea[formControl],input:not([type=checkbox])[ngModel],textarea[ngModel],[ngDefaultControl]" }, { kind: "directive", type: i1.CheckboxControlValueAccessor, selector: "input[type=checkbox][formControlName],input[type=checkbox][formControl],input[type=checkbox][ngModel]" }, { kind: "directive", type: i1.SelectControlValueAccessor, selector: "select:not([multiple])[formControlName],select:not([multiple])[formControl],select:not([multiple])[ngModel]", inputs: ["compareWith"] }, { kind: "directive", type: i1.NgControlStatus, selector: "[formControlName],[ngModel],[formControl]" }, { kind: "directive", type: i1.NgModel, selector: "[ngModel]:not([formControlName]):not([formControl])", inputs: ["name", "disabled", "ngModel", "ngModelOptions"], outputs: ["ngModelChange"], exportAs: ["ngModel"] }, { kind: "ngmodule", type: ReactiveFormsModule }, { kind: "directive", type: i1.FormControlDirective, selector: "[formControl]", inputs: ["formControl", "disabled", "ngModel"], outputs: ["ngModelChange"], exportAs: ["ngForm"] }, { kind: "ngmodule", type: OverlayModule }, { kind: "directive", type: i2.CdkConnectedOverlay, selector: "[cdk-connected-overlay], [connected-overlay], [cdkConnectedOverlay]", inputs: ["cdkConnectedOverlayOrigin", "cdkConnectedOverlayPositions", "cdkConnectedOverlayPositionStrategy", "cdkConnectedOverlayOffsetX", "cdkConnectedOverlayOffsetY", "cdkConnectedOverlayWidth", "cdkConnectedOverlayHeight", "cdkConnectedOverlayMinWidth", "cdkConnectedOverlayMinHeight", "cdkConnectedOverlayBackdropClass", "cdkConnectedOverlayPanelClass", "cdkConnectedOverlayViewportMargin", "cdkConnectedOverlayScrollStrategy", "cdkConnectedOverlayOpen", "cdkConnectedOverlayDisableClose", "cdkConnectedOverlayTransformOriginOn", "cdkConnectedOverlayHasBackdrop", "cdkConnectedOverlayLockPosition", "cdkConnectedOverlayFlexibleDimensions", "cdkConnectedOverlayGrowAfterOpen", "cdkConnectedOverlayPush", "cdkConnectedOverlayDisposeOnNavigation"], outputs: ["backdropClick", "positionChange", "attach", "detach", "overlayKeydown", "overlayOutsideClick"], exportAs: ["cdkConnectedOverlay"] }, { kind: "directive", type: i2.CdkOverlayOrigin, selector: "[cdk-overlay-origin], [overlay-origin], [cdkOverlayOrigin]", exportAs: ["cdkOverlayOrigin"] }, { kind: "component", type: ChillJsonInputComponent, selector: "app-chill-json-input", inputs: ["value", "placeholder", "invalid", "disabled", "language", "minHeight", "maxHeight", "mobileFullHeight"], outputs: ["valueChange", "blur"] }] }); }
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: ChillPolymorphicInputComponent, decorators: [{
            type: Component,
            args: [{ selector: 'app-chill-polymorphic-input', standalone: true, imports: [CommonModule, FormsModule, ReactiveFormsModule, OverlayModule, ChillJsonInputComponent], template: "<div class=\"polymorphic-fields\">\n  @for (property of properties(); track property.name) {\n    <label class=\"field\">\n      @if (showLabels()) {\n        <span>{{ property.displayName || property.name }}</span>\n      }\n\n      @if (isCheckbox(property)) {\n        @if (control(property.name); as fieldControl) {\n          <input\n            type=\"checkbox\"\n            [formControl]=\"fieldControl\"\n            [disabled]=\"isPropertyReadOnly(property.name)\"\n            (blur)=\"emitFieldBlur(property.name)\"\n            [name]=\"property.name\" />\n        }\n      } @else if (isLookupCollection(property)) {\n        <div class=\"lookup\">\n          @for (entity of selectedLookupCollectionEntities(property.name); track lookupGuid(entity) || $index) {\n            <div class=\"lookup-selected\" [title]=\"lookupLabel(entity)\">\n              <span class=\"lookup-selected__label lookup-selected__label--full\">\n                {{ lookupLabel(entity) || chill.T('B93CA88B-01FE-44B6-9C2F-C9878A7B324B', 'Select value', 'Seleziona valore') }}\n              </span>\n              <span class=\"lookup-selected__label lookup-selected__label--short\">\n                {{ lookupShortLabel(entity) || lookupLabel(entity) || chill.T('B93CA88B-01FE-44B6-9C2F-C9878A7B324B', 'Select value', 'Seleziona valore') }}\n              </span>\n              <button\n                type=\"button\"\n                class=\"lookup-selected__clear\"\n                (click)=\"removeLookupCollectionEntity(property, entity)\"\n                [disabled]=\"isPropertyReadOnly(property.name)\"\n                [attr.aria-label]=\"chill.T('34015BA4-E0CA-460E-B82B-A4E2D4D8A184', 'Clear', 'Pulisci')\">\n                X\n              </button>\n            </div>\n          }\n\n          <div class=\"lookup-bar\">\n            <div class=\"lookup-input-slot\" cdkOverlayOrigin #lookupOrigin=\"cdkOverlayOrigin\" #lookupOriginElement>\n              <input\n                type=\"text\"\n                [ngModel]=\"lookupTerm(property.name)\"\n                (ngModelChange)=\"updateLookupTerm(property, $event)\"\n                (focus)=\"handleLookupFocus(property)\"\n                (blur)=\"handleLookupBlur(property.name)\"\n                [disabled]=\"isPropertyReadOnly(property.name)\"\n                [name]=\"property.name\"\n                [placeholder]=\"placeholder(property) || chill.T('FA7B8E01-658C-4D63-B53F-D476CD697892', 'Search entity', 'Cerca entita')\" />\n            </div>\n            @if (canOpenLookupDialog(property)) {\n              <button\n                type=\"button\"\n                class=\"lookup-dialog\"\n                (mousedown)=\"beginLookupDialogSelection(property.name)\"\n                (click)=\"openLookupDialog(property)\"\n                [disabled]=\"isPropertyReadOnly(property.name)\"\n                [attr.aria-label]=\"chill.T('B6D48459-73D4-4234-977E-8D79E510A20D', 'Open entity picker', 'Apri selettore entita')\">\n                ...\n              </button>\n            }\n            <button type=\"button\" class=\"lookup-clear\" (click)=\"clearLookup(property)\" [disabled]=\"isPropertyReadOnly(property.name)\">\n              {{ chill.T('34015BA4-E0CA-460E-B82B-A4E2D4D8A184', 'Clear', 'Pulisci') }}\n            </button>\n          </div>\n\n          <ng-template\n            cdkConnectedOverlay\n            [cdkConnectedOverlayOrigin]=\"lookupOrigin\"\n            [cdkConnectedOverlayOpen]=\"lookupResults(property.name).length > 0\"\n            [cdkConnectedOverlayPositions]=\"lookupOverlayPositions\"\n            [cdkConnectedOverlayPush]=\"true\"\n            [cdkConnectedOverlayFlexibleDimensions]=\"true\"\n            [cdkConnectedOverlayViewportMargin]=\"8\"\n            [cdkConnectedOverlayOffsetY]=\"6\">\n            <div\n              class=\"lookup-results\"\n              role=\"listbox\"\n              [style.--lookup-overlay-width.px]=\"lookupOverlayWidth(lookupOriginElement)\">\n              @for (result of lookupResults(property.name); track $index) {\n                <button\n                  type=\"button\"\n                  class=\"lookup-result\"\n                  [class.is-selected]=\"isLookupResultSelected(property.name, result)\"\n                  (mousedown)=\"$event.preventDefault()\"\n                  (click)=\"selectLookupResult(property, result)\">\n                  {{ lookupLabel(result) || chill.T('B93CA88B-01FE-44B6-9C2F-C9878A7B324B', 'Select value', 'Seleziona valore') }}\n                </button>\n              }\n            </div>\n          </ng-template>\n\n          @if (lookupError(property.name)) {\n            <small class=\"field-error\">{{ lookupError(property.name) }}</small>\n          }\n\n          @if (lookupIsSearching(property.name)) {\n            <small class=\"lookup-status\">\n              {{ chill.T('ABAF5996-C2BB-4F85-BE0F-CC75883A648B', 'Searching...', 'Ricerca in corso...') }}\n            </small>\n          }\n        </div>\n      } @else if (isLookup(property)) {\n        <div class=\"lookup\">\n          @if (property.propertyType === 1000 && hasSelectedLookupEntity(property.name)) {\n            <div class=\"lookup-selected\" [title]=\"selectedLookupLabel(property.name)\">\n              <span class=\"lookup-selected__label lookup-selected__label--full\">\n                {{ selectedLookupLabel(property.name) || chill.T('B93CA88B-01FE-44B6-9C2F-C9878A7B324B', 'Select value', 'Seleziona valore') }}\n              </span>\n              <span class=\"lookup-selected__label lookup-selected__label--short\">\n                {{ selectedLookupShortLabel(property.name) || selectedLookupLabel(property.name) || chill.T('B93CA88B-01FE-44B6-9C2F-C9878A7B324B', 'Select value', 'Seleziona valore') }}\n              </span>\n              <button\n                type=\"button\"\n                class=\"lookup-selected__clear\"\n                (click)=\"clearLookup(property)\"\n                [disabled]=\"isPropertyReadOnly(property.name)\"\n                [attr.aria-label]=\"chill.T('34015BA4-E0CA-460E-B82B-A4E2D4D8A184', 'Clear', 'Pulisci')\">\n                X\n              </button>\n            </div>\n          } @else {\n            <div class=\"lookup-bar\">\n              <div class=\"lookup-input-slot\" cdkOverlayOrigin #lookupOrigin=\"cdkOverlayOrigin\" #lookupOriginElement>\n                <input\n                  type=\"text\"\n                  [ngModel]=\"lookupTerm(property.name)\"\n                  (ngModelChange)=\"updateLookupTerm(property, $event)\"\n                  (focus)=\"handleLookupFocus(property)\"\n                  (blur)=\"handleLookupBlur(property.name)\"\n                  [disabled]=\"isPropertyReadOnly(property.name)\"\n                  [name]=\"property.name\"\n                  [placeholder]=\"placeholder(property) || chill.T('FA7B8E01-658C-4D63-B53F-D476CD697892', 'Search entity', 'Cerca entita')\" />\n              </div>\n              @if (canOpenLookupDialog(property)) {\n                <button\n                  type=\"button\"\n                  class=\"lookup-dialog\"\n                  (mousedown)=\"beginLookupDialogSelection(property.name)\"\n                  (click)=\"openLookupDialog(property)\"\n                  [disabled]=\"isPropertyReadOnly(property.name)\"\n                  [attr.aria-label]=\"chill.T('B6D48459-73D4-4234-977E-8D79E510A20D', 'Open entity picker', 'Apri selettore entita')\">\n                  ...\n                </button>\n              }\n            </div>\n\n            <ng-template\n              cdkConnectedOverlay\n              [cdkConnectedOverlayOrigin]=\"lookupOrigin\"\n              [cdkConnectedOverlayOpen]=\"lookupResults(property.name).length > 0\"\n              [cdkConnectedOverlayPositions]=\"lookupOverlayPositions\"\n              [cdkConnectedOverlayPush]=\"true\"\n              [cdkConnectedOverlayFlexibleDimensions]=\"true\"\n              [cdkConnectedOverlayViewportMargin]=\"8\"\n              [cdkConnectedOverlayOffsetY]=\"6\">\n              <div\n                class=\"lookup-results\"\n                role=\"listbox\"\n                [style.--lookup-overlay-width.px]=\"lookupOverlayWidth(lookupOriginElement)\">\n                @for (result of lookupResults(property.name); track $index) {\n                  <button\n                    type=\"button\"\n                    class=\"lookup-result\"\n                    [class.is-selected]=\"isLookupResultSelected(property.name, result)\"\n                    (mousedown)=\"$event.preventDefault()\"\n                    (click)=\"selectLookupResult(property, result)\">\n                    {{ lookupLabel(result) || chill.T('B93CA88B-01FE-44B6-9C2F-C9878A7B324B', 'Select value', 'Seleziona valore') }}\n                  </button>\n                }\n              </div>\n            </ng-template>\n          }\n\n          @if (lookupIsSearching(property.name)) {\n            <small class=\"lookup-status\">\n              {{ chill.T('ABAF5996-C2BB-4F85-BE0F-CC75883A648B', 'Searching...', 'Ricerca in corso...') }}\n            </small>\n          }\n\n          @if (lookupError(property.name)) {\n            <small class=\"field-error\">{{ lookupError(property.name) }}</small>\n          }\n        </div>\n      } @else if (isTextarea(property)) {\n        @if (control(property.name); as fieldControl) {\n          <textarea\n            rows=\"4\"\n            [formControl]=\"fieldControl\"\n            [disabled]=\"isPropertyReadOnly(property.name)\"\n            (blur)=\"emitFieldBlur(property.name)\"\n            [name]=\"property.name\"\n            [placeholder]=\"placeholder(property)\"></textarea>\n        }\n      } @else if (isJsonEditor(property)) {\n        <div class=\"editor-field\">\n          <app-chill-json-input\n            [value]=\"textValue(property.name)\"\n            [language]=\"editorLanguage(property)\"\n            [placeholder]=\"placeholder(property)\"\n            [invalid]=\"!!validationMessage(property.name)\"\n            [disabled]=\"isPropertyReadOnly(property.name) || (control(property.name)?.disabled ?? false)\"\n            (valueChange)=\"updateJsonInput(property.name, $event)\"\n            (blur)=\"emitFieldBlur(property.name)\"></app-chill-json-input>\n          <button\n            type=\"button\"\n            class=\"editor-dialog-button\"\n            (mousedown)=\"beginEditorDialogSelection(property.name)\"\n            (click)=\"openEditorDialog(property)\"\n            [attr.aria-label]=\"chill.T('EC935816-DCBC-4AE7-BA13-36D66A7D7EBD', 'Open editor dialog', 'Apri editor in dialog')\">\n            <span class=\"material-symbol-icon\" aria-hidden=\"true\">open_in_full</span>\n          </button>\n        </div>\n      } @else if (isSelect(property)) {\n        <select\n          [ngModel]=\"selectValue(property.name)\"\n          (ngModelChange)=\"updateSelectValue(property, $event)\"\n          [disabled]=\"isPropertyReadOnly(property.name)\"\n          [name]=\"property.name\">\n          @for (option of selectOptions(property); track option[0] + ':' + option[1]) {\n            <option [value]=\"option[0]\">{{ option[1] }}</option>\n          }\n        </select>\n      } @else {\n        @if (control(property.name); as fieldControl) {\n          @if (isFormattedTextInput(property)) {\n            <input\n              type=\"text\"\n              [ngModel]=\"textValue(property.name)\"\n              (ngModelChange)=\"updateTextInput(property.name, $event)\"\n              (blur)=\"normalizeTextOnBlur(property)\"\n              [disabled]=\"isPropertyReadOnly(property.name)\"\n              [name]=\"property.name\"\n              [placeholder]=\"placeholder(property)\" />\n          } @else {\n            <input\n              [type]=\"inputType(property)\"\n              [step]=\"inputStep(property)\"\n              [formControl]=\"fieldControl\"\n              (blur)=\"normalizeTextOnBlur(property)\"\n              [name]=\"property.name\"\n              [placeholder]=\"placeholder(property)\" />\n          }\n        }\n      }\n\n      @if (validationMessage(property.name)) {\n        <small class=\"field-error\">{{ validationMessage(property.name) }}</small>\n      }\n    </label>\n  }\n</div>\n", styles: [":host{display:block}.polymorphic-fields{display:grid;gap:.85rem}.field{display:grid;gap:.45rem}.field span{font-weight:600;color:var(--text-main)}.field input,.field select,.field textarea{width:100%;padding:.75rem .85rem;border-radius:.45rem;border:1px solid color-mix(in srgb,var(--accent) 26%,var(--border-color));background:linear-gradient(180deg,color-mix(in srgb,var(--accent-soft) 32%,transparent),transparent 30%),linear-gradient(180deg,color-mix(in srgb,var(--surface-0) 96%,rgba(2,16,25,.14)),color-mix(in srgb,var(--surface-1) 92%,rgba(1,10,18,.18)));color:var(--text-main);box-shadow:inset 0 0 0 1px color-mix(in srgb,var(--accent) 8%,transparent),0 0 .7rem color-mix(in srgb,var(--accent) 6%,transparent)}.field input[type=checkbox]{width:1rem;height:1rem;padding:0}.field textarea{resize:vertical}.field app-chill-json-input{width:100%}.editor-field{position:relative;min-width:0}.editor-dialog-button{position:absolute;top:.35rem;right:.35rem;z-index:1;display:inline-grid;place-items:center;width:2rem;height:2rem;padding:0;border:1px solid color-mix(in srgb,var(--accent) 26%,var(--border-color));border-radius:.4rem;background:color-mix(in srgb,var(--surface-0) 92%,transparent);color:var(--text-main);box-shadow:var(--shadow-soft);cursor:pointer}.editor-dialog-button .material-symbol-icon{font-size:1.05rem}.field-error{color:var(--danger);font-size:.85rem}.lookup{display:grid;gap:.55rem;position:relative;container-type:inline-size}.lookup-bar{display:grid;grid-template-columns:minmax(0,1fr) auto auto;gap:.5rem;align-items:start}.lookup-input-slot{min-width:0}.lookup-dialog,.lookup-clear,.lookup-result{min-height:2.5rem;padding:.65rem .85rem;border:1px solid color-mix(in srgb,var(--accent) 26%,var(--border-color));border-radius:.45rem;background:linear-gradient(180deg,color-mix(in srgb,var(--accent-soft) 32%,transparent),transparent 30%),linear-gradient(180deg,color-mix(in srgb,var(--surface-0) 96%,rgba(2,16,25,.14)),color-mix(in srgb,var(--surface-1) 92%,rgba(1,10,18,.18)));color:var(--text-main);box-shadow:inset 0 0 0 1px color-mix(in srgb,var(--accent) 8%,transparent),0 0 .7rem color-mix(in srgb,var(--accent) 6%,transparent);cursor:pointer}.lookup-selected{display:grid;grid-template-columns:minmax(0,1fr) auto;align-items:center;gap:.65rem;width:100%;min-height:2.75rem;padding:.5rem .6rem .5rem .85rem;border:1px solid color-mix(in srgb,var(--accent) 26%,var(--border-color));border-radius:999px;background:linear-gradient(180deg,color-mix(in srgb,var(--accent-soft) 32%,transparent),transparent 30%),linear-gradient(180deg,color-mix(in srgb,var(--surface-0) 96%,rgba(2,16,25,.14)),color-mix(in srgb,var(--surface-1) 92%,rgba(1,10,18,.18)));color:var(--text-main);box-shadow:inset 0 0 0 1px color-mix(in srgb,var(--accent) 8%,transparent),0 0 .7rem color-mix(in srgb,var(--accent) 6%,transparent)}.lookup-selected__label{min-width:0;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;font-weight:500}.lookup-selected__label--short{display:none}.lookup-selected__clear{width:1.9rem;height:1.9rem;border:0;border-radius:999px;background:color-mix(in srgb,var(--accent-soft) 62%,var(--surface-0));color:var(--text-main);cursor:pointer;font-size:.9rem;line-height:1}:root[data-theme=dark] .field input,:root[data-theme=dark] .field select,:root[data-theme=dark] .field textarea,:root[data-theme=dark] .editor-dialog-button,:root[data-theme=dark] .lookup-dialog,:root[data-theme=dark] .lookup-clear,:root[data-theme=dark] .lookup-result,:root[data-theme=dark] .lookup-selected{background:#09131a94}.lookup-status{color:var(--text-muted);font-size:.85rem}.lookup-results{display:grid;gap:.45rem;width:var(--lookup-overlay-width, 100%);min-width:14rem;max-width:calc(100vw - 16px);max-height:400px;overflow-y:auto;padding:.35rem;border:1px solid color-mix(in srgb,var(--accent) 20%,var(--border-color));border-radius:.7rem;background:color-mix(in srgb,var(--surface-0) 96%,rgba(2,16,25,.14));box-shadow:var(--shadow)}.lookup-tags{display:flex;flex-wrap:wrap;gap:.45rem}.lookup-tag{display:inline-flex;align-items:center;min-height:2rem;padding:.35rem .7rem;border:1px solid color-mix(in srgb,var(--accent) 22%,var(--border-color));border-radius:999px;background:color-mix(in srgb,var(--accent-soft) 42%,var(--surface-0));color:var(--text-main);font-size:.85rem}.lookup-result{text-align:left}.lookup-result.is-selected{border-color:color-mix(in srgb,var(--accent) 55%,var(--border-color));background:linear-gradient(180deg,color-mix(in srgb,var(--accent-soft) 70%,transparent),transparent 35%),linear-gradient(180deg,color-mix(in srgb,var(--surface-0) 98%,rgba(2,16,25,.14)),color-mix(in srgb,var(--surface-1) 94%,rgba(1,10,18,.18)))}@media(max-width:720px){.lookup-bar{grid-template-columns:1fr}}@container (max-width: 16rem){.lookup-selected__label--full{display:none}.lookup-selected__label--short{display:inline}}\n"] }]
        }], ctorParameters: () => [] });

const FORM_LAYOUT_METADATA_KEY = 'chill-form-component';
const DEFAULT_FORM_COLUMN_COUNT = 2;
const EMPTY_LAYOUT_ITEM_PREFIX = '__empty__';
class ChillFormComponent {
    constructor() {
        this.columnOptions = [1, 2, 3, 4, 5, 6];
        this.propertyTypeOptions = CHILL_PROPERTY_TYPE_OPTIONS;
        this.host = inject(ElementRef);
        this.chill = inject(ChillService);
        this.layout = inject(WorkspaceLayoutService);
        this.dialog = inject(WorkspaceDialogService, { optional: true });
        this.schema = input(null);
        this.entity = input(null);
        this.query = input(null);
        this.submitLabel = input(this.chill.T('22282CD9-6B51-4B50-87BE-36E3790D4B8D', 'Submit', 'Invia'));
        this.submitLabelGuid = input(null);
        this.submitPrimaryDefaultText = input(null);
        this.submitSecondaryDefaultText = input(null);
        this.showSchemaHeader = input(true);
        this.renderSubmitInsideForm = input(true);
        this.onSubmit = input(null);
        this.closeDialogOnSubmit = input(false);
        this.submitError = input(null);
        this.dismissSubmitError = input(null);
        this.readonlyPropertyNames = input(null);
        this.formSubmit = output();
        this.schemaUpdated = output();
        this.form = signal(null);
        this.propertyValidity = signal({});
        this.serverFieldErrors = signal({});
        this.genericValidationErrors = signal([]);
        this.isAutocompleting = signal(false);
        this.isSubmitting = signal(false);
        this.internalSubmitError = signal('');
        this.isEditMode = signal(false);
        this.isSavingLayout = signal(false);
        this.isRefreshingSchema = signal(false);
        this.layoutError = signal('');
        this.dragPropertyName = signal('');
        this.schemaRefreshTick = signal(0);
        this.layoutState = signal({
            columnCount: DEFAULT_FORM_COLUMN_COUNT,
            items: []
        });
        this.formValueSubscription = new Subscription();
        this.lastFormValue = {};
        this.lastFormResetSignature = '';
        this.autocompleteRequestSequence = 0;
        this.pendingAutocompletePromise = null;
        this.mode = computed(() => this.query() ? 'query' : 'entity');
        this.source = computed(() => this.query() ?? this.entity());
        this.hasCustomSubmitHandler = computed(() => !!this.onSubmit());
        this.properties = computed(() => {
            this.schemaRefreshTick();
            return this.schema()?.properties ?? [];
        });
        this.layoutItems = computed(() => {
            const propertyMap = new Map(this.properties().map((property) => [property.name, property]));
            const layout = this.layoutState();
            const resolvedItems = layout.items
                .map((item) => {
                const span = Math.min(Math.max(item.span, 1), layout.columnCount);
                if (item.kind === 'empty') {
                    return {
                        id: item.id,
                        kind: 'empty',
                        span,
                        hidden: item.hidden
                    };
                }
                const propertyName = item.name?.trim() ?? '';
                const property = propertyMap.get(propertyName);
                if (!property) {
                    return null;
                }
                return {
                    id: item.id,
                    kind: 'property',
                    property,
                    span,
                    hidden: item.hidden
                };
            })
                .filter((item) => item !== null);
            const knownPropertyNames = new Set(resolvedItems
                .filter((item) => item.kind === 'property')
                .map((item) => item.property.name));
            const missingProperties = this.properties()
                .filter((property) => !knownPropertyNames.has(property.name))
                .map((property) => ({
                id: property.name,
                kind: 'property',
                property,
                span: 1,
                hidden: false
            }));
            return [...resolvedItems, ...missingProperties];
        });
        this.canSubmit = computed(() => {
            const form = this.form();
            if (this.isEditMode()) {
                return false;
            }
            if (this.isSubmitting()) {
                return false;
            }
            if (form?.pending || form?.invalid) {
                return false;
            }
            return !this.hasInvalidPropertyState();
        });
        this.resolvedSubmitError = computed(() => {
            const internalSubmitError = this.internalSubmitError().trim();
            if (internalSubmitError) {
                return internalSubmitError;
            }
            const submitError = this.submitError();
            if (typeof submitError === 'function') {
                return submitError().trim();
            }
            return typeof submitError === 'string'
                ? submitError.trim()
                : '';
        });
        this.genericValidationMessage = computed(() => this.genericValidationErrors().join(' ').trim());
        this.readonlyPropertyNameSet = computed(() => new Set((this.readonlyPropertyNames() ?? [])
            .map((propertyName) => propertyName.trim().toLowerCase())
            .filter((propertyName) => propertyName.length > 0)));
        effect(() => {
            const schema = this.schema();
            const source = this.source();
            const nextLayoutState = this.readLayoutState(schema);
            const formResetSignature = this.buildFormResetSignature(schema, source);
            const shouldResetForm = formResetSignature !== this.lastFormResetSignature;
            this.layoutState.set(nextLayoutState);
            this.layoutError.set('');
            this.isEditMode.set(false);
            if (!shouldResetForm) {
                return;
            }
            this.lastFormResetSignature = formResetSignature;
            const nextForm = schema
                ? this.chill.prepareForm(schema, source)
                : null;
            this.form.set(nextForm);
            this.propertyValidity.set(this.createInitialPropertyValidity(schema));
            this.serverFieldErrors.set({});
            this.genericValidationErrors.set([]);
            this.internalSubmitError.set('');
            this.isAutocompleting.set(false);
            this.isSubmitting.set(false);
            this.syncFormValueSubscription(nextForm);
        });
        effect(() => {
            if (!this.layout.isLayoutEditingEnabled()) {
                this.isEditMode.set(false);
            }
        });
        effect(() => {
            const form = this.form();
            const readonlyPropertyNameSet = this.readonlyPropertyNameSet();
            if (!form) {
                return;
            }
            for (const property of this.properties()) {
                const control = form.controls[property.name];
                if (!control) {
                    continue;
                }
                const isReadonly = readonlyPropertyNameSet.has(property.name.trim().toLowerCase());
                if (isReadonly && control.enabled) {
                    control.disable({ emitEvent: false });
                }
                else if (!isReadonly && control.disabled) {
                    control.enable({ emitEvent: false });
                }
            }
        });
    }
    async submit() {
        const form = this.form();
        if (!form || this.isEditMode() || this.isSubmitting()) {
            return;
        }
        this.internalSubmitError.set('');
        this.isSubmitting.set(true);
        try {
            if (this.shouldAutocompleteOnBlur()) {
                await this.flushPendingAutocomplete();
            }
            if (form.pending || form.invalid || this.hasInvalidPropertyState()) {
                return;
            }
            if (this.shouldValidateOnSubmit()) {
                const isValid = await this.validateCurrentPayload();
                if (!isValid) {
                    return;
                }
            }
            if (form.pending || form.invalid || this.hasInvalidPropertyState()) {
                return;
            }
            const payload = this.mode() === 'query'
                ? this.buildQueryPayload()
                : this.buildEntityPayload();
            const event = {
                kind: this.mode(),
                value: payload
            };
            this.formSubmit.emit(event);
            const customSubmit = this.onSubmit();
            if (customSubmit) {
                await customSubmit(event);
                if (this.closeDialogOnSubmit()) {
                    this.dialog?.confirm();
                }
            }
            else {
                await this.submitDefault(event);
            }
        }
        catch (error) {
            this.internalSubmitError.set(this.chill.formatError(error));
        }
        finally {
            this.isSubmitting.set(false);
        }
    }
    toggleEditMode() {
        if (!this.layout.isLayoutEditingEnabled()) {
            return;
        }
        if (!this.isEditMode()) {
            this.layoutState.update((current) => this.completeLayoutState(current));
            this.isEditMode.set(true);
            this.layoutError.set('');
            return;
        }
        this.saveLayout();
    }
    updateFields(value) {
        const incomingFieldNames = Object.keys(value)
            .map((fieldName) => fieldName.trim())
            .filter((fieldName) => fieldName.length > 0);
        const form = this.form();
        if (!form) {
            return;
        }
        for (const [fieldName, fieldValue] of Object.entries(value)) {
            const control = form.controls[fieldName];
            if (!control || Object.is(control.value, fieldValue)) {
                continue;
            }
            control.setValue(fieldValue);
        }
        if (incomingFieldNames.length > 0) {
            this.clearServerValidationForFields(incomingFieldNames);
        }
    }
    updatePropertyValidity(propertyName, isValid) {
        this.propertyValidity.update((current) => {
            if (current[propertyName] === isValid) {
                return current;
            }
            return {
                ...current,
                [propertyName]: isValid
            };
        });
    }
    updateColumnCount(value) {
        const parsedValue = typeof value === 'number' ? value : Number(value);
        const columnCount = Number.isFinite(parsedValue)
            ? Math.max(1, Math.floor(parsedValue))
            : DEFAULT_FORM_COLUMN_COUNT;
        this.layoutState.update((current) => ({
            columnCount,
            items: current.items.map((item) => ({
                ...item,
                span: Math.min(item.span, columnCount)
            }))
        }));
    }
    addEmptyCell() {
        this.layoutState.update((current) => ({
            ...current,
            items: [
                ...current.items,
                {
                    id: this.createEmptyLayoutItemId(current.items),
                    kind: 'empty',
                    span: 1,
                    hidden: false
                }
            ]
        }));
    }
    updatePropertyHidden(itemId, hidden) {
        this.layoutState.update((current) => ({
            ...current,
            items: this.updateLayoutItemHidden(current.items, itemId, hidden)
        }));
    }
    isPropertyTypeOptionDisabled(property, propertyType) {
        return !canChangeChillPropertyType(property.propertyType, propertyType);
    }
    updatePropertyType(property, value) {
        const schema = this.schema();
        const parsed = typeof value === 'number' ? value : Number(value);
        if (!schema || !Number.isFinite(parsed) || !canChangeChillPropertyType(property.propertyType, parsed)) {
            return;
        }
        if ((property.propertyType ?? CHILL_PROPERTY_TYPE$1.Unknown) === parsed) {
            return;
        }
        this.savePropertySchema(schema, property.name, {
            ...property,
            propertyType: parsed,
            simplePropertyType: chillSimplePropertyType(parsed)
        });
    }
    increaseSpan(itemId) {
        this.layoutState.update((current) => ({
            ...current,
            items: current.items.map((item) => item.id === itemId
                ? { ...item, span: Math.min(item.span + 1, current.columnCount) }
                : item)
        }));
    }
    decreaseSpan(itemId) {
        this.layoutState.update((current) => ({
            ...current,
            items: current.items.map((item) => item.id === itemId
                ? { ...item, span: Math.max(1, item.span - 1) }
                : item)
        }));
    }
    resetLayout() {
        this.layoutState.update((current) => ({
            columnCount: current.columnCount,
            items: (this.schema()?.properties ?? []).map((property) => ({
                id: property.name,
                kind: 'property',
                name: property.name,
                span: 1,
                hidden: false
            }))
        }));
    }
    refreshSchemaFromModel() {
        const schema = this.schema();
        const chillType = schema?.chillType?.trim() ?? '';
        const chillViewCode = schema?.chillViewCode?.trim() || 'default';
        if (!schema || !chillType || this.isRefreshingSchema()) {
            return;
        }
        this.isRefreshingSchema.set(true);
        this.isSavingLayout.set(true);
        this.layoutError.set('');
        this.chill.getSchema(chillType, chillViewCode, undefined, true).subscribe({
            next: (updatedSchema) => {
                if (!updatedSchema) {
                    this.layoutError.set(this.chill.T('A6A6949E-F0D4-42F5-A8AE-E15B1B174084', 'The result schema is unavailable.', 'Lo schema dei risultati non è disponibile.'));
                    return;
                }
                this.applyUpdatedSchema(schema, updatedSchema);
            },
            error: (error) => {
                this.layoutError.set(this.chill.formatError(error));
                this.isRefreshingSchema.set(false);
                this.isSavingLayout.set(false);
            },
            complete: () => {
                this.isRefreshingSchema.set(false);
                this.isSavingLayout.set(false);
            }
        });
    }
    beginDrag(event, itemId) {
        if (!this.isEditMode()) {
            return;
        }
        if (this.isCellToolbarControl(event.target)) {
            event.preventDefault();
            return;
        }
        this.dragPropertyName.set(itemId);
    }
    allowDrop(event) {
        if (!this.isEditMode()) {
            return;
        }
        event.preventDefault();
    }
    dropProperty(targetItemId) {
        const sourceItemId = this.dragPropertyName();
        if (!sourceItemId || sourceItemId === targetItemId) {
            this.dragPropertyName.set('');
            return;
        }
        this.layoutState.update((current) => {
            const nextItems = [...current.items];
            const sourceIndex = nextItems.findIndex((item) => item.id === sourceItemId);
            const targetIndex = nextItems.findIndex((item) => item.id === targetItemId);
            if (sourceIndex < 0 || targetIndex < 0) {
                return current;
            }
            const [movedItem] = nextItems.splice(sourceIndex, 1);
            const insertionIndex = sourceIndex < targetIndex
                ? targetIndex - 1
                : targetIndex;
            nextItems.splice(insertionIndex, 0, movedItem);
            return {
                ...current,
                items: nextItems
            };
        });
        this.dragPropertyName.set('');
    }
    endDrag() {
        this.dragPropertyName.set('');
    }
    gridTemplateColumns() {
        return `repeat(${this.layoutState().columnCount}, minmax(0, 1fr))`;
    }
    trackByProperty(index, item) {
        return item.id || `${index}`;
    }
    canDialogSubmit() {
        return this.canSubmit();
    }
    clearSubmitError() {
        this.internalSubmitError.set('');
        this.dismissSubmitError()?.();
    }
    handlePropertyBlur(value) {
        const protectedFieldNames = Object.keys(value)
            .map((fieldName) => fieldName.trim())
            .filter((fieldName) => fieldName.length > 0);
        this.updateFields(value);
        if (!this.shouldAutocompleteOnBlur()) {
            return;
        }
        queueMicrotask(() => {
            void this.runAutocomplete(protectedFieldNames);
        });
    }
    openPropertySettings(property) {
        const schema = this.schema();
        if (!schema || !this.dialog) {
            return;
        }
        void (async () => {
            const { SchemaPropertyDialogComponent } = await Promise.resolve().then(function () { return schemaPropertyDialog_component; });
            const result = await this.dialog.openDialog({
                title: property.displayName?.trim() || property.name,
                component: SchemaPropertyDialogComponent,
                okLabel: this.chill.T('62953302-B951-4FD1-BD08-4B7649A91BAF', 'Save', 'Salva'),
                inputs: {
                    schema,
                    property
                }
            });
            if (result.status !== 'confirmed' || !result.value) {
                return;
            }
            this.savePropertySchema(schema, property.name, result.value);
        })();
    }
    ngOnDestroy() {
        this.formValueSubscription.unsubscribe();
    }
    buildEntityPayload() {
        const entity = this.entity();
        const schema = this.schema();
        const sanitizedEntity = entity && schema
            ? this.stripSchemaPropertiesFromRoot(entity, schema)
            : entity;
        return {
            ...(sanitizedEntity ?? {}),
            properties: this.buildPropertiesObject(this.form())
        };
    }
    buildQueryPayload() {
        const query = this.query();
        return {
            ...(query ?? {}),
            properties: this.buildPropertiesObject(this.form())
        };
    }
    buildPropertiesObject(formOverride) {
        const properties = {};
        const schema = this.schema();
        const formValue = formOverride?.getRawValue() ?? {};
        for (const property of this.properties()) {
            const rawValue = formValue[property.name];
            properties[property.name] = this.chill.toJsonValue(schema, property.name, rawValue);
        }
        return properties;
    }
    syncFormValueSubscription(form) {
        this.formValueSubscription.unsubscribe();
        this.formValueSubscription = new Subscription();
        this.lastFormValue = form?.getRawValue() ?? {};
        if (!form) {
            return;
        }
        this.formValueSubscription = form.valueChanges.subscribe((value) => {
            const nextValue = value;
            const changedFieldNames = this.readChangedFieldNames(this.lastFormValue, nextValue);
            this.lastFormValue = { ...nextValue };
            if (changedFieldNames.length > 0) {
                this.clearServerValidationForFields(changedFieldNames);
            }
        });
    }
    async runAutocomplete(protectedFieldNames = []) {
        const schema = this.schema();
        if (!schema || this.isEditMode() || !this.shouldAutocompleteOnBlur()) {
            return;
        }
        const requestSequence = ++this.autocompleteRequestSequence;
        const request = this.buildCurrentPayload();
        this.isAutocompleting.set(true);
        const pendingRequest = (async () => {
            const response = await firstValueFrom(this.chill.autocomplete(request));
            if (requestSequence !== this.autocompleteRequestSequence) {
                return;
            }
            const autocompletedFields = this.extractAutocompleteFields(schema, response);
            if (Object.keys(autocompletedFields).length > 0) {
                this.applyAutocompleteFields(autocompletedFields, protectedFieldNames);
            }
        })()
            .catch(() => undefined)
            .finally(() => {
            if (requestSequence === this.autocompleteRequestSequence) {
                this.isAutocompleting.set(false);
            }
            if (this.pendingAutocompletePromise === pendingRequest) {
                this.pendingAutocompletePromise = null;
            }
        });
        this.pendingAutocompletePromise = pendingRequest;
        await pendingRequest;
    }
    applyAutocompleteFields(value, protectedFieldNames = []) {
        const form = this.form();
        if (!form) {
            return;
        }
        const focusedPropertyName = this.readFocusedPropertyName();
        const protectedFieldNameSet = new Set(protectedFieldNames
            .map((fieldName) => fieldName.trim())
            .filter((fieldName) => fieldName.length > 0));
        const nextValues = {};
        for (const [fieldName, fieldValue] of Object.entries(value)) {
            const control = form.controls[fieldName];
            if (!control) {
                continue;
            }
            const shouldProtectFocusedField = fieldName === focusedPropertyName
                && control.dirty
                && control.value !== null;
            const shouldProtectBlurredField = protectedFieldNameSet.has(fieldName)
                && control.dirty
                && control.value !== null;
            if (shouldProtectFocusedField || shouldProtectBlurredField) {
                continue;
            }
            nextValues[fieldName] = fieldValue;
        }
        if (Object.keys(nextValues).length > 0) {
            this.updateFields(nextValues);
        }
    }
    readFocusedPropertyName() {
        const activeElement = globalThis.document?.activeElement;
        if (!(activeElement instanceof HTMLElement)) {
            return '';
        }
        if (!this.host.nativeElement.contains(activeElement)) {
            return '';
        }
        return activeElement.getAttribute('name')?.trim() ?? '';
    }
    isCellToolbarControl(target) {
        return target instanceof HTMLElement
            && !!target.closest('input, button, select, textarea');
    }
    async flushPendingAutocomplete() {
        this.blurFocusedControl();
        await new Promise((resolve) => queueMicrotask(resolve));
        while (this.pendingAutocompletePromise) {
            await this.pendingAutocompletePromise;
            await new Promise((resolve) => queueMicrotask(resolve));
        }
    }
    blurFocusedControl() {
        const activeElement = globalThis.document?.activeElement;
        if (!(activeElement instanceof HTMLElement)) {
            return;
        }
        if (!this.host.nativeElement.contains(activeElement)) {
            return;
        }
        activeElement.blur();
    }
    async submitDefault(event) {
        if (event.kind !== 'entity') {
            return;
        }
        const schema = this.schema();
        if (!schema) {
            return;
        }
        const entity = this.normalizeEntityForSubmit(event.value, schema);
        const isNewEntity = this.readEntityIsNew(entity);
        const request = isNewEntity
            ? this.chill.create(entity)
            : this.chill.update(entity);
        const savedEntity = await firstValueFrom(request);
        this.dialog?.confirm(savedEntity);
    }
    normalizeEntityForSubmit(entity, schema) {
        const sanitizedSourceEntity = this.entity()
            ? this.stripSchemaPropertiesFromRoot(this.entity(), schema)
            : null;
        const sanitizedEntity = this.stripSchemaPropertiesFromRoot(entity, schema);
        return {
            ...(sanitizedSourceEntity ?? {}),
            ...sanitizedEntity,
            chillType: this.readEntityChillType(entity, schema),
            properties: this.buildPropertiesObject(this.form())
        };
    }
    stripSchemaPropertiesFromRoot(entity, schema) {
        const nextEntity = { ...entity };
        const protectedNames = new Set(['guid', 'Guid', 'chillType', 'ChillType', 'chillState', 'ChillState', 'properties', 'Properties']);
        for (const property of schema.properties ?? []) {
            const propertyName = property.name?.trim();
            if (!propertyName || protectedNames.has(propertyName)) {
                continue;
            }
            delete nextEntity[propertyName];
            const pascalCaseName = propertyName.length > 0
                ? `${propertyName[0].toUpperCase()}${propertyName.slice(1)}`
                : propertyName;
            if (!protectedNames.has(pascalCaseName)) {
                delete nextEntity[pascalCaseName];
            }
        }
        return nextEntity;
    }
    readEntityChillType(entity, schema) {
        const directChillType = typeof entity['chillType'] === 'string'
            ? entity['chillType'].trim()
            : '';
        if (directChillType) {
            return directChillType;
        }
        const sourceEntity = this.entity();
        const sourceChillType = sourceEntity && typeof sourceEntity['chillType'] === 'string'
            ? sourceEntity['chillType'].trim()
            : '';
        if (sourceChillType) {
            return sourceChillType;
        }
        return schema.chillType?.trim() ?? '';
    }
    readEntityIsNew(entity) {
        const chillState = entity['chillState'];
        return !!chillState
            && typeof chillState === 'object'
            && !Array.isArray(chillState)
            && chillState['isNew'] === true;
    }
    shouldValidateOnSubmit() {
        return this.mode() === 'entity';
    }
    shouldAutocompleteOnBlur() {
        return this.mode() === 'entity';
    }
    readResponsePropertyValue(source, propertyName) {
        const directProperties = source['properties'];
        if (directProperties && typeof directProperties === 'object' && !Array.isArray(directProperties) && propertyName in directProperties) {
            return directProperties[propertyName];
        }
        const pascalProperties = source['Properties'];
        if (pascalProperties && typeof pascalProperties === 'object' && !Array.isArray(pascalProperties) && propertyName in pascalProperties) {
            return pascalProperties[propertyName];
        }
        if (propertyName in source) {
            return source[propertyName];
        }
        const pascalPropertyName = propertyName.length > 0
            ? `${propertyName[0].toUpperCase()}${propertyName.slice(1)}`
            : propertyName;
        return pascalPropertyName in source
            ? source[pascalPropertyName]
            : undefined;
    }
    validateCurrentPayload() {
        return this.validateEntityPayload();
    }
    async validateEntityPayload() {
        const schema = this.schema();
        if (!schema) {
            return true;
        }
        try {
            const errors = await firstValueFrom(this.chill.validate(this.buildCurrentPayload()));
            const { fieldErrors, genericErrors } = this.partitionValidationErrors(errors, schema);
            this.serverFieldErrors.set(fieldErrors);
            this.genericValidationErrors.set(genericErrors);
            return Object.keys(fieldErrors).length === 0 && genericErrors.length === 0;
        }
        catch (error) {
            this.genericValidationErrors.set([this.chill.formatError(error)]);
            return false;
        }
    }
    partitionValidationErrors(errors, schema) {
        const fieldNameMap = new Map((schema.properties ?? [])
            .map((property) => property.name.trim())
            .filter((propertyName) => propertyName.length > 0)
            .map((propertyName) => [propertyName.toLowerCase(), propertyName]));
        const fieldErrors = {};
        const genericErrors = [];
        for (const error of errors) {
            const fieldName = typeof error.fieldName === 'string' ? error.fieldName.trim() : '';
            const message = typeof error.message === 'string' ? error.message.trim() : '';
            if (!message) {
                continue;
            }
            const resolvedFieldName = fieldName ? fieldNameMap.get(fieldName.toLowerCase()) : undefined;
            if (resolvedFieldName) {
                fieldErrors[resolvedFieldName] = fieldErrors[resolvedFieldName]
                    ? `${fieldErrors[resolvedFieldName]} ${message}`
                    : message;
                continue;
            }
            genericErrors.push(message);
        }
        return { fieldErrors, genericErrors };
    }
    buildCurrentPayload() {
        return this.mode() === 'query'
            ? this.buildQueryPayload()
            : this.buildEntityPayload();
    }
    hasInvalidPropertyState() {
        return this.layoutItems()
            .filter((item) => item.kind === 'property' && !item.hidden)
            .some((item) => this.propertyValidity()[item.property.name] === false);
    }
    extractAutocompleteFields(schema, response) {
        const nextFields = {};
        for (const property of schema.properties ?? []) {
            const autocompletedValue = this.readResponsePropertyValue(response, property.name);
            if (autocompletedValue !== undefined) {
                nextFields[property.name] = autocompletedValue;
            }
        }
        return nextFields;
    }
    saveLayout() {
        const schema = this.schema();
        if (!schema) {
            this.isEditMode.set(false);
            return;
        }
        const metadata = this.readSchemaMetadata(schema);
        const layoutState = this.layoutState();
        const serializedLayout = JSON.stringify(layoutState);
        metadata[FORM_LAYOUT_METADATA_KEY] = serializedLayout;
        const updatedSchema = {
            ...schema,
            metadata
        };
        this.isSavingLayout.set(true);
        this.layoutError.set('');
        this.chill.setSchema(updatedSchema).subscribe({
            next: (savedSchema) => {
                const effectiveSchema = savedSchema ?? updatedSchema;
                const targetSchema = this.schema();
                if (targetSchema) {
                    targetSchema.metadata = this.readSchemaMetadata(effectiveSchema);
                    delete targetSchema['Metadata'];
                }
                this.layoutState.set(this.readLayoutState(effectiveSchema));
                this.isSavingLayout.set(false);
                this.isEditMode.set(false);
            },
            error: (error) => {
                this.layoutError.set(this.chill.formatError(error));
                this.isSavingLayout.set(false);
            }
        });
    }
    savePropertySchema(schema, originalPropertyName, property) {
        const metadata = this.readSchemaMetadata(schema);
        metadata[FORM_LAYOUT_METADATA_KEY] = JSON.stringify(this.layoutState());
        const updatedSchema = {
            ...schema,
            properties: (schema.properties ?? []).map((candidate) => candidate.name === originalPropertyName
                ? property
                : candidate),
            metadata
        };
        this.isSavingLayout.set(true);
        this.layoutError.set('');
        this.chill.setSchema(updatedSchema).subscribe({
            next: (savedSchema) => {
                const effectiveSchema = savedSchema ?? updatedSchema;
                const targetSchema = this.schema();
                if (targetSchema) {
                    this.applyUpdatedSchema(targetSchema, effectiveSchema);
                }
                this.isSavingLayout.set(false);
            },
            error: (error) => {
                this.layoutError.set(this.chill.formatError(error));
                this.isSavingLayout.set(false);
            }
        });
    }
    applyUpdatedSchema(targetSchema, updatedSchema) {
        targetSchema.metadata = this.readSchemaMetadata(updatedSchema);
        targetSchema.properties = [...(updatedSchema.properties ?? [])];
        targetSchema.displayName = updatedSchema.displayName ?? targetSchema.displayName;
        targetSchema.handleAttachments = updatedSchema.handleAttachments;
        targetSchema.enableMCP = updatedSchema.enableMCP;
        targetSchema.mcpDescription = updatedSchema.mcpDescription ?? null;
        targetSchema.queryRelatedChillType = updatedSchema.queryRelatedChillType;
        delete targetSchema['Metadata'];
        delete targetSchema['Properties'];
        const currentFormValue = this.form()?.getRawValue() ?? {};
        const nextForm = this.chill.prepareForm(targetSchema, this.source());
        for (const [fieldName, fieldValue] of Object.entries(currentFormValue)) {
            const control = nextForm.controls[fieldName];
            if (control) {
                control.setValue(fieldValue, { emitEvent: false });
            }
        }
        this.lastFormResetSignature = this.buildFormResetSignature(targetSchema, this.source());
        this.form.set(nextForm);
        this.syncFormValueSubscription(nextForm);
        this.propertyValidity.set(this.createInitialPropertyValidity(targetSchema));
        this.layoutState.set(this.readLayoutState(targetSchema));
        this.schemaRefreshTick.update((current) => current + 1);
        this.schemaUpdated.emit(targetSchema);
    }
    createDefaultLayout(schema) {
        return {
            columnCount: DEFAULT_FORM_COLUMN_COUNT,
            items: (schema?.properties ?? []).map((property) => ({
                id: property.name,
                kind: 'property',
                name: property.name,
                span: 1,
                hidden: false
            }))
        };
    }
    createInitialPropertyValidity(schema) {
        return Object.fromEntries((schema?.properties ?? []).map((property) => [property.name, true]));
    }
    buildFormResetSignature(schema, source) {
        return JSON.stringify({
            schema: schema
                ? {
                    chillType: schema.chillType ?? '',
                    chillViewCode: schema.chillViewCode ?? '',
                    displayName: schema.displayName ?? '',
                    handleAttachments: schema.handleAttachments === true,
                    queryRelatedChillType: schema.queryRelatedChillType ?? '',
                    properties: schema.properties ?? []
                }
                : null,
            source
        });
    }
    readLayoutState(schema) {
        const defaultLayout = this.createDefaultLayout(schema);
        const metadata = this.readSchemaMetadata(schema);
        const rawLayoutValue = metadata[FORM_LAYOUT_METADATA_KEY];
        const rawLayout = typeof rawLayoutValue === 'string' ? rawLayoutValue.trim() : '';
        if (!rawLayout) {
            return defaultLayout;
        }
        try {
            const parsedLayout = JSON.parse(rawLayout);
            const columnCount = typeof parsedLayout.columnCount === 'number' && Number.isFinite(parsedLayout.columnCount)
                ? Math.max(1, Math.floor(parsedLayout.columnCount))
                : defaultLayout.columnCount;
            const rawItems = Array.isArray(parsedLayout.items)
                ? parsedLayout.items
                : (Array.isArray(parsedLayout.properties)
                    ? parsedLayout.properties
                    : []);
            const savedItems = rawItems
                .map((item, index) => this.normalizePersistedLayoutItem(item, index))
                .filter((item) => item !== null);
            const defaultPropertyNames = new Set(defaultLayout.items.flatMap((item) => item.kind === 'property' && item.name ? [item.name] : []));
            const orderedPropertyNames = [
                ...savedItems
                    .flatMap((item) => item.kind === 'property' && item.name && defaultPropertyNames.has(item.name) ? [item.name] : []),
                ...defaultLayout.items
                    .flatMap((item) => item.kind === 'property' && item.name ? [item.name] : [])
                    .filter((name) => !savedItems.some((item) => item.kind === 'property' && item.name === name))
            ];
            const restoredItems = savedItems
                .filter((item) => item.kind === 'empty' || (item.kind === 'property' && item.name && defaultPropertyNames.has(item.name)))
                .map((item) => ({
                ...item,
                span: Math.min(item.span, columnCount)
            }));
            const missingItems = orderedPropertyNames
                .filter((name) => !restoredItems.some((item) => item.kind === 'property' && item.name === name))
                .map((name) => ({
                id: name,
                kind: 'property',
                name,
                span: 1,
                hidden: false
            }));
            const restoredLayout = {
                columnCount,
                items: [...restoredItems, ...missingItems]
            };
            return restoredLayout;
        }
        catch {
            return defaultLayout;
        }
    }
    normalizePersistedLayoutItem(value, index) {
        if (!value || typeof value !== 'object' || Array.isArray(value)) {
            return null;
        }
        const candidate = value;
        const span = typeof candidate.span === 'number' && Number.isFinite(candidate.span)
            ? Math.max(1, Math.floor(candidate.span))
            : 1;
        const hidden = candidate.hidden === true;
        const kind = candidate.kind === 'empty'
            ? 'empty'
            : 'property';
        if (kind === 'empty') {
            const id = typeof candidate.id === 'string' && candidate.id.trim()
                ? candidate.id.trim()
                : `${EMPTY_LAYOUT_ITEM_PREFIX}${index + 1}`;
            return {
                id,
                kind,
                span,
                hidden
            };
        }
        const name = typeof candidate.name === 'string'
            ? candidate.name.trim()
            : '';
        if (!name) {
            return null;
        }
        return {
            id: typeof candidate.id === 'string' && candidate.id.trim()
                ? candidate.id.trim()
                : name,
            kind,
            name,
            span,
            hidden
        };
    }
    updateLayoutItemHidden(items, itemId, hidden) {
        const normalizedItemId = itemId.trim();
        if (!normalizedItemId) {
            return items;
        }
        let didUpdate = false;
        const nextItems = items.map((item) => {
            if (item.id !== normalizedItemId && item.name !== normalizedItemId) {
                return item;
            }
            didUpdate = true;
            return {
                ...item,
                hidden
            };
        });
        if (didUpdate) {
            return nextItems;
        }
        const property = this.properties().find((candidate) => candidate.name === normalizedItemId);
        if (!property) {
            return items;
        }
        return [
            ...items,
            {
                id: property.name,
                kind: 'property',
                name: property.name,
                span: 1,
                hidden
            }
        ];
    }
    completeLayoutState(layoutState) {
        const knownPropertyNames = new Set(layoutState.items
            .flatMap((item) => item.kind === 'property' && item.name ? [item.name] : []));
        const missingItems = this.properties()
            .filter((property) => !knownPropertyNames.has(property.name))
            .map((property) => ({
            id: property.name,
            kind: 'property',
            name: property.name,
            span: 1,
            hidden: false
        }));
        return missingItems.length === 0
            ? layoutState
            : {
                ...layoutState,
                items: [
                    ...layoutState.items,
                    ...missingItems
                ]
            };
    }
    createEmptyLayoutItemId(items) {
        let index = 1;
        while (items.some((item) => item.id === `${EMPTY_LAYOUT_ITEM_PREFIX}${index}`)) {
            index += 1;
        }
        return `${EMPTY_LAYOUT_ITEM_PREFIX}${index}`;
    }
    readSchemaMetadata(schema) {
        if (!schema) {
            return {};
        }
        const camelMetadata = schema.metadata;
        if (camelMetadata) {
            return { ...camelMetadata };
        }
        const pascalMetadata = schema['Metadata'];
        if (pascalMetadata && typeof pascalMetadata === 'object' && !Array.isArray(pascalMetadata)) {
            return Object.fromEntries(Object.entries(pascalMetadata).map(([key, value]) => [key, typeof value === 'string' ? value : String(value ?? '')]));
        }
        return {};
    }
    clearServerValidationForFields(fieldNames) {
        if (fieldNames.length === 0) {
            return;
        }
        const normalizedFieldNames = new Set(fieldNames.map((fieldName) => fieldName.trim().toLowerCase()).filter((fieldName) => fieldName.length > 0));
        this.serverFieldErrors.update((current) => {
            let changed = false;
            const nextEntries = Object.entries(current).filter(([fieldName]) => {
                const shouldKeep = !normalizedFieldNames.has(fieldName.trim().toLowerCase());
                if (!shouldKeep) {
                    changed = true;
                }
                return shouldKeep;
            });
            return changed ? Object.fromEntries(nextEntries) : current;
        });
        this.genericValidationErrors.set([]);
    }
    areRecordsEqual(left, right) {
        const leftKeys = Object.keys(left);
        const rightKeys = Object.keys(right);
        if (leftKeys.length !== rightKeys.length) {
            return false;
        }
        return leftKeys.every((key) => Object.is(left[key], right[key]));
    }
    readChangedFieldNames(previousValue, nextValue) {
        const fieldNames = new Set([
            ...Object.keys(previousValue),
            ...Object.keys(nextValue)
        ]);
        return [...fieldNames].filter((fieldName) => !Object.is(previousValue[fieldName], nextValue[fieldName]));
    }
    static { this.ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: ChillFormComponent, deps: [], target: i0.ɵɵFactoryTarget.Component }); }
    static { this.ɵcmp = i0.ɵɵngDeclareComponent({ minVersion: "17.0.0", version: "19.2.21", type: ChillFormComponent, isStandalone: true, selector: "app-chill-form", inputs: { schema: { classPropertyName: "schema", publicName: "schema", isSignal: true, isRequired: false, transformFunction: null }, entity: { classPropertyName: "entity", publicName: "entity", isSignal: true, isRequired: false, transformFunction: null }, query: { classPropertyName: "query", publicName: "query", isSignal: true, isRequired: false, transformFunction: null }, submitLabel: { classPropertyName: "submitLabel", publicName: "submitLabel", isSignal: true, isRequired: false, transformFunction: null }, submitLabelGuid: { classPropertyName: "submitLabelGuid", publicName: "submitLabelGuid", isSignal: true, isRequired: false, transformFunction: null }, submitPrimaryDefaultText: { classPropertyName: "submitPrimaryDefaultText", publicName: "submitPrimaryDefaultText", isSignal: true, isRequired: false, transformFunction: null }, submitSecondaryDefaultText: { classPropertyName: "submitSecondaryDefaultText", publicName: "submitSecondaryDefaultText", isSignal: true, isRequired: false, transformFunction: null }, showSchemaHeader: { classPropertyName: "showSchemaHeader", publicName: "showSchemaHeader", isSignal: true, isRequired: false, transformFunction: null }, renderSubmitInsideForm: { classPropertyName: "renderSubmitInsideForm", publicName: "renderSubmitInsideForm", isSignal: true, isRequired: false, transformFunction: null }, onSubmit: { classPropertyName: "onSubmit", publicName: "onSubmit", isSignal: true, isRequired: false, transformFunction: null }, closeDialogOnSubmit: { classPropertyName: "closeDialogOnSubmit", publicName: "closeDialogOnSubmit", isSignal: true, isRequired: false, transformFunction: null }, submitError: { classPropertyName: "submitError", publicName: "submitError", isSignal: true, isRequired: false, transformFunction: null }, dismissSubmitError: { classPropertyName: "dismissSubmitError", publicName: "dismissSubmitError", isSignal: true, isRequired: false, transformFunction: null }, readonlyPropertyNames: { classPropertyName: "readonlyPropertyNames", publicName: "readonlyPropertyNames", isSignal: true, isRequired: false, transformFunction: null } }, outputs: { formSubmit: "formSubmit", schemaUpdated: "schemaUpdated" }, ngImport: i0, template: "<section class=\"chill-form-shell\">\n  @if (showSchemaHeader() && schema()?.displayName) {\n    <header class=\"form-header\">\n      <p class=\"form-kicker\"><app-chill-i18n-label [labelGuid]=\"'2DDE962B-086C-47B1-8A48-B16F0E34C0A3'\" [primaryDefaultText]=\"'Chill schema'\" [secondaryDefaultText]=\"'Schema Chill'\" /></p>\n      <h2>{{ schema()?.displayName }}</h2>\n    </header>\n  }\n\n  @if (layout.isLayoutEditingEnabled()) {\n    <div class=\"form-actions\">\n      <button type=\"button\" class=\"layout-button\" (click)=\"toggleEditMode()\" [disabled]=\"isSavingLayout()\">\n        @if (isSavingLayout()) {\n          <app-chill-i18n-button-label [labelGuid]=\"'F9BFE458-EA0D-4E27-A8A9-C7EE0C02F9FB'\" [primaryDefaultText]=\"'Saving layout...'\" [secondaryDefaultText]=\"'Salvataggio layout...'\" />\n        } @else if (isEditMode()) {\n          <app-chill-i18n-button-label [labelGuid]=\"'D7EA89E2-4AF2-455A-8FA9-33540E61D7C5'\" [primaryDefaultText]=\"'Done'\" [secondaryDefaultText]=\"'Fine'\" />\n        } @else {\n          <app-chill-i18n-button-label [labelGuid]=\"'872638CF-0346-4351-A53A-62A6B78B94FE'\" [primaryDefaultText]=\"'Edit mode'\" [secondaryDefaultText]=\"'Modalita modifica'\" />\n        }\n      </button>\n\n      @if (isEditMode()) {\n        <label class=\"column-count\">\n          <span><app-chill-i18n-label [labelGuid]=\"'665D0BFB-D1BB-4D53-B58A-C578CA559A0B'\" [primaryDefaultText]=\"'Columns'\" [secondaryDefaultText]=\"'Colonne'\" /></span>\n          <select\n            [ngModel]=\"layoutState().columnCount\"\n            (ngModelChange)=\"updateColumnCount($event)\"\n            name=\"form-column-count\">\n            @for (columnCount of columnOptions; track columnCount) {\n              <option [ngValue]=\"columnCount\">{{ columnCount }}</option>\n            }\n          </select>\n        </label>\n\n        <button type=\"button\" class=\"layout-button secondary\" (click)=\"addEmptyCell()\">\n          <app-chill-i18n-button-label [labelGuid]=\"'7B8C7557-FB39-4C6C-B70F-D70F87EC7517'\" [primaryDefaultText]=\"'Add empty cell'\" [secondaryDefaultText]=\"'Aggiungi cella vuota'\" />\n        </button>\n\n        <button type=\"button\" class=\"layout-button secondary\" (click)=\"resetLayout()\">\n          <app-chill-i18n-button-label [labelGuid]=\"'408CE646-58A0-4657-8192-D1C79BFE7F91'\" [primaryDefaultText]=\"'Reset'\" [secondaryDefaultText]=\"'Reset'\" />\n        </button>\n\n        <button type=\"button\" class=\"layout-button secondary\" (click)=\"refreshSchemaFromModel()\" [disabled]=\"isSavingLayout()\">\n          @if (isRefreshingSchema()) {\n            <app-chill-i18n-button-label [labelGuid]=\"'8628775B-B831-44F2-8A38-909F40E2F7B3'\" [primaryDefaultText]=\"'Updating schema...'\" [secondaryDefaultText]=\"'Aggiornamento schema...'\" />\n          } @else {\n            <app-chill-i18n-button-label [labelGuid]=\"'62953302-B951-4FD1-BD08-4B7649A91BAF'\" [primaryDefaultText]=\"'Update'\" [secondaryDefaultText]=\"'Aggiorna'\" />\n          }\n        </button>\n      }\n    </div>\n  }\n\n  @if (layoutError()) {\n    <div class=\"empty-state error-state\">{{ layoutError() }}</div>\n  }\n\n  @if (resolvedSubmitError()) {\n    <div class=\"notice error notice-dismissible\">\n      <span>{{ resolvedSubmitError() }}</span>\n      <button type=\"button\" class=\"notice-close\" (click)=\"clearSubmitError()\" [attr.aria-label]=\"chill.T('0C4C06EF-A105-468F-B1E2-AEA8EB96A4DC', 'Dismiss error', 'Chiudi errore')\">x</button>\n    </div>\n  }\n\n  @if (genericValidationMessage()) {\n    <div class=\"notice error\">\n      <span>{{ genericValidationMessage() }}</span>\n    </div>\n  }\n\n  @if (properties().length === 0) {\n    <div class=\"empty-state\">{{ chill.T('3D7ABF07-6E0C-4F99-A0C8-C936C44322C6', 'No schema properties available.', 'Nessuna propriet\u00E0 di schema disponibile.') }}</div>\n  } @else {\n    <form class=\"chill-form\" (ngSubmit)=\"submit()\">\n      <div class=\"form-grid\" [style.grid-template-columns]=\"gridTemplateColumns()\">\n        @for (item of layoutItems(); track trackByProperty($index, item)) {\n          <section\n            class=\"form-cell\"\n            [class.empty-cell]=\"item.kind === 'empty' || (item.kind === 'property' && item.hidden && !isEditMode())\"\n            [class.edit-mode]=\"isEditMode()\"\n            [style.grid-column]=\"'span ' + item.span\"\n            [draggable]=\"isEditMode()\"\n            (dragstart)=\"beginDrag($event, item.id)\"\n            (dragover)=\"allowDrop($event)\"\n            (drop)=\"dropProperty(item.id)\"\n            (dragend)=\"endDrag()\">\n            @if (isEditMode()) {\n              <div\n                class=\"cell-toolbar\"\n                (click)=\"$event.stopPropagation()\">\n                <div class=\"cell-toolbar__title-row\">\n                  <span>\n                    @if (item.kind === 'property') {\n                      {{ item.property.displayName || item.property.name }}\n                    } @else {\n                      {{ chill.T('278389E7-9C0F-4849-B7C8-B904CC0E4AAA', 'Empty cell', 'Cella vuota') }}\n                    }\n                  </span>\n                  @if (item.kind === 'property') {\n                    <input\n                      type=\"checkbox\"\n                      class=\"field-visibility-checkbox\"\n                      [checked]=\"!item.hidden\"\n                      (mousedown)=\"$event.stopPropagation()\"\n                      (click)=\"$event.stopPropagation()\"\n                      (change)=\"updatePropertyHidden(item.property.name, !$any($event.target).checked)\"\n                      [name]=\"'hidden-' + item.property.name\"\n                      [attr.aria-label]=\"chill.T('D72C8236-361A-4053-86B8-6F5A9D6983F6', 'Show field', 'Mostra campo')\" />\n                  }\n                </div>\n                <div class=\"cell-toolbar__settings-row\">\n                  @if (item.kind === 'property') {\n                    <button type=\"button\" class=\"cell-toolbar__icon-button\" (click)=\"openPropertySettings(item.property)\" [attr.aria-label]=\"chill.T('40E9838A-70F2-4E89-BE38-BBE44027D253', 'Edit property settings', 'Modifica impostazioni propriet\u00E0')\">\n                      <span class=\"material-symbol-icon\">tune</span>\n                    </button>\n                    <select\n                      class=\"property-type-select\"\n                      [ngModel]=\"item.property.propertyType\"\n                      (ngModelChange)=\"updatePropertyType(item.property, $event)\"\n                      [name]=\"'propertyType-' + item.property.name\"\n                      [attr.aria-label]=\"chill.T('A1803D67-C40D-41AF-BFD2-8F9B0E34C48B', 'Property type', 'Tipo propriet\u00E0')\">\n                      @for (option of propertyTypeOptions; track option.value) {\n                        <option [ngValue]=\"option.value\" [disabled]=\"isPropertyTypeOptionDisabled(item.property, option.value)\">{{ option.label }}</option>\n                      }\n                    </select>\n                  } @else {\n                    <span class=\"cell-toolbar__icon-spacer\"></span>\n                    <span class=\"cell-toolbar__select-spacer\"></span>\n                  }\n                  <div class=\"span-controls\">\n                    <button type=\"button\" class=\"cell-toolbar__icon-button\" (click)=\"decreaseSpan(item.id)\" [attr.aria-label]=\"chill.T('DB73501A-618F-48C7-A6BC-613B44729887', 'Decrease field span', 'Riduci larghezza campo')\">\n                      <span class=\"material-symbol-icon\" aria-hidden=\"true\">remove</span>\n                    </button>\n                    <span class=\"span-controls__value\">{{ item.span }}</span>\n                    <button type=\"button\" class=\"cell-toolbar__icon-button\" (click)=\"increaseSpan(item.id)\" [attr.aria-label]=\"chill.T('63E1E823-9015-4551-A43B-4B672E988B4A', 'Increase field span', 'Aumenta larghezza campo')\">\n                      <span class=\"material-symbol-icon\" aria-hidden=\"true\">add</span>\n                    </button>\n                  </div>\n                </div>\n              </div>\n            }\n            @else \n            {\n              @if (item.kind === 'property' && !item.hidden) {\n                <app-chill-polymorphic-input\n                  [form]=\"form()\"\n                  [schema]=\"schema()\"\n                  [propertyNames]=\"[item.property.name]\"\n                  [readonlyPropertyNames]=\"readonlyPropertyNames()\"\n                  [externalErrors]=\"serverFieldErrors()\"\n                  (validityChange)=\"updatePropertyValidity(item.property.name, $event)\"\n                  (fieldBlur)=\"handlePropertyBlur($event)\" />\n              } @else {\n                <div class=\"empty-cell-body\"></div>\n              }\n            }\n          </section>\n        }\n      </div>\n\n      @if (!isEditMode() && renderSubmitInsideForm() && !hasCustomSubmitHandler()) {\n        <button type=\"submit\" [disabled]=\"!canSubmit()\">\n          @if (submitLabelGuid() && submitPrimaryDefaultText() && submitSecondaryDefaultText()) {\n            <app-chill-i18n-button-label\n              [labelGuid]=\"submitLabelGuid()!\"\n              [primaryDefaultText]=\"submitPrimaryDefaultText()!\"\n              [secondaryDefaultText]=\"submitSecondaryDefaultText()!\" />\n          } @else {\n            {{ submitLabel() }}\n          }\n        </button>\n      }\n    </form>\n  }\n</section>\n", styles: [":host{display:block}.chill-form-shell{--tron-border: color-mix(in srgb, var(--accent) 24%, var(--border-color));--tron-border-strong: color-mix(in srgb, var(--accent) 42%, var(--border-color));--tron-glow: color-mix(in srgb, var(--accent) 14%, transparent);--tron-panel: linear-gradient(180deg, color-mix(in srgb, var(--surface-2) 94%, rgba(8, 18, 26, .18)), color-mix(in srgb, var(--surface-1) 96%, rgba(7, 14, 22, .22)));display:grid;gap:.85rem}.form-actions{display:flex;flex-wrap:wrap;gap:.65rem;align-items:center}.layout-button{min-height:2.5rem;padding:.65rem .9rem;border:1px solid var(--tron-border);border-radius:.4rem;background:var(--surface-0);color:var(--text-main);box-shadow:inset 0 0 0 1px color-mix(in srgb,var(--accent) 8%,transparent),0 0 .85rem var(--tron-glow);font-weight:700;cursor:pointer}.layout-button.secondary{background:color-mix(in srgb,var(--surface-0) 92%,var(--accent-soft))}.column-count{display:flex;gap:.5rem;align-items:center;color:var(--text-main);font-weight:600}.column-count select{width:5rem;padding:.55rem .7rem;border-radius:.4rem;border:1px solid var(--tron-border);background:var(--surface-0);color:var(--text-main);box-shadow:inset 0 0 0 1px color-mix(in srgb,var(--accent) 8%,transparent),0 0 .65rem var(--tron-glow);appearance:none}.form-header h2,.form-header p{margin:0}.form-kicker{color:var(--accent);font-size:.75rem;font-weight:700;letter-spacing:.18em;text-transform:uppercase}.chill-form{display:grid;gap:.85rem;position:relative;overflow:hidden}.chill-form:before{content:\"\";position:absolute;inset:0;pointer-events:none;background:linear-gradient(90deg,transparent 0,transparent calc(100% - 1px),color-mix(in srgb,var(--accent) 10%,transparent) calc(100% - 1px),color-mix(in srgb,var(--accent) 10%,transparent) 100%),linear-gradient(transparent 0,transparent calc(100% - 1px),color-mix(in srgb,var(--accent) 8%,transparent) calc(100% - 1px),color-mix(in srgb,var(--accent) 8%,transparent) 100%);background-size:2.75rem 2.75rem;opacity:.24}.form-grid{display:grid;gap:.85rem}.form-cell{display:grid;gap:.65rem;padding:.8rem;border-radius:.55rem;border:1px solid var(--tron-border);background:linear-gradient(180deg,color-mix(in srgb,var(--accent-soft) 44%,transparent),transparent 34%),linear-gradient(180deg,color-mix(in srgb,var(--surface-1) 94%,rgba(0,11,20,.2)),color-mix(in srgb,var(--surface-0) 90%,rgba(1,16,25,.22)));box-shadow:inset 0 0 0 1px color-mix(in srgb,var(--accent) 8%,transparent),0 0 .9rem color-mix(in srgb,var(--accent) 8%,transparent)}.form-cell.edit-mode{border-color:var(--tron-border-strong);-webkit-user-select:none;user-select:none}.form-cell.empty-cell:not(.edit-mode){display:none}.form-cell.empty-cell.edit-mode{min-height:4.5rem}.cell-toolbar{display:grid;gap:.4rem;min-width:0;color:var(--text-main);font-weight:600}.cell-toolbar__title-row,.cell-toolbar__settings-row{min-width:0}.cell-toolbar__title-row{display:grid;grid-template-columns:minmax(0,1fr) auto;align-items:center;gap:.45rem}.cell-toolbar__title-row span{min-width:0;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}.field-visibility-checkbox{width:1rem;height:1rem;margin:0;accent-color:var(--accent)}.cell-toolbar__settings-row{display:grid;grid-template-columns:2rem minmax(7.25rem,1fr) minmax(6.35rem,auto);align-items:center;gap:.45rem;width:100%}.cell-toolbar__icon-button,.cell-toolbar__icon-spacer{width:2rem;min-width:2rem;height:2rem}.cell-toolbar__select-spacer{display:block;min-width:7.25rem;height:2rem}.cell-toolbar__icon-button{display:inline-grid;place-items:center;padding:0;border:1px solid var(--tron-border);border-radius:.4rem;background:var(--surface-0);color:var(--text-main);box-shadow:inset 0 0 0 1px color-mix(in srgb,var(--accent) 8%,transparent);cursor:pointer}.span-controls{display:inline-flex;align-items:center;justify-self:end;gap:.15rem;min-width:6.35rem}.span-controls__value{min-width:1.75rem;color:var(--text-muted);font-size:.8rem;font-weight:700;text-align:center}.property-type-select{min-width:7.25rem;min-height:2rem;padding:.35rem .55rem;border:1px solid var(--tron-border);border-radius:.4rem;background:var(--surface-0);color:var(--text-main);font:inherit;font-size:.78rem}.empty-cell-body{min-height:3rem;border-radius:.45rem}.empty-cell-body.edit-mode{border:1px dashed var(--tron-border-strong);background:linear-gradient(180deg,color-mix(in srgb,var(--accent-soft) 68%,transparent),transparent),linear-gradient(90deg,transparent 0,transparent calc(100% - 1px),color-mix(in srgb,var(--accent) 18%,transparent) calc(100% - 1px),color-mix(in srgb,var(--accent) 18%,transparent) 100%),linear-gradient(transparent 0,transparent calc(100% - 1px),color-mix(in srgb,var(--accent) 14%,transparent) calc(100% - 1px),color-mix(in srgb,var(--accent) 14%,transparent) 100%);background-size:auto,1.1rem 1.1rem,1.1rem 1.1rem}button[type=submit]{justify-self:start;min-height:2.7rem;padding:.7rem 1rem;border:0;border-radius:.45rem;background:linear-gradient(135deg,color-mix(in srgb,var(--accent) 92%,#61f8f1),color-mix(in srgb,var(--accent-strong) 78%,#0fbcff));color:#07131a;font-weight:700;box-shadow:0 0 1rem color-mix(in srgb,var(--accent) 22%,transparent),0 0 1.8rem color-mix(in srgb,var(--accent) 10%,transparent);cursor:pointer}.empty-state{padding:.85rem 1rem;border-radius:.55rem;background:linear-gradient(180deg,color-mix(in srgb,var(--accent-soft) 42%,transparent),transparent 34%),var(--surface-2);border:1px dashed var(--tron-border);color:var(--text-muted);max-width:100%;max-height:10rem;overflow-x:hidden;overflow-y:auto;overflow-wrap:anywhere;word-break:break-word}.error-state{color:var(--danger)}:root[data-theme=dark] .layout-button,:root[data-theme=dark] .column-count select,:root[data-theme=dark] .cell-toolbar__icon-button,:root[data-theme=dark] .property-type-select{background:#09131a94}@media(max-width:720px){.form-grid{grid-template-columns:minmax(0,1fr)!important}.form-cell{grid-column:auto!important}}\n"], dependencies: [{ kind: "ngmodule", type: CommonModule }, { kind: "ngmodule", type: FormsModule }, { kind: "directive", type: i1.ɵNgNoValidate, selector: "form:not([ngNoForm]):not([ngNativeValidate])" }, { kind: "directive", type: i1.NgSelectOption, selector: "option", inputs: ["ngValue", "value"] }, { kind: "directive", type: i1.ɵNgSelectMultipleOption, selector: "option", inputs: ["ngValue", "value"] }, { kind: "directive", type: i1.SelectControlValueAccessor, selector: "select:not([multiple])[formControlName],select:not([multiple])[formControl],select:not([multiple])[ngModel]", inputs: ["compareWith"] }, { kind: "directive", type: i1.NgControlStatus, selector: "[formControlName],[ngModel],[formControl]" }, { kind: "directive", type: i1.NgControlStatusGroup, selector: "[formGroupName],[formArrayName],[ngModelGroup],[formGroup],form:not([ngNoForm]),[ngForm]" }, { kind: "directive", type: i1.NgModel, selector: "[ngModel]:not([formControlName]):not([formControl])", inputs: ["name", "disabled", "ngModel", "ngModelOptions"], outputs: ["ngModelChange"], exportAs: ["ngModel"] }, { kind: "directive", type: i1.NgForm, selector: "form:not([ngNoForm]):not([formGroup]),ng-form,[ngForm]", inputs: ["ngFormOptions"], outputs: ["ngSubmit"], exportAs: ["ngForm"] }, { kind: "ngmodule", type: ReactiveFormsModule }, { kind: "component", type: ChillPolymorphicInputComponent, selector: "app-chill-polymorphic-input", inputs: ["form", "schema", "propertyNames", "readonlyPropertyNames", "externalErrors", "showLabels"], outputs: ["valueChange", "validityChange", "fieldBlur", "lookupDialogOpenChange", "editorDialogOpenChange"] }, { kind: "component", type: ChillI18nLabelComponent, selector: "app-chill-i18n-label", inputs: ["labelGuid", "primaryDefaultText", "secondaryDefaultText", "editable"] }, { kind: "component", type: ChillI18nButtonLabelComponent, selector: "app-chill-i18n-button-label", inputs: ["labelGuid", "primaryDefaultText", "secondaryDefaultText", "editable"] }, { kind: "directive", type: NoticeTransitionDirective, selector: ".notice" }] }); }
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: ChillFormComponent, decorators: [{
            type: Component,
            args: [{ selector: 'app-chill-form', standalone: true, imports: [CommonModule, FormsModule, ReactiveFormsModule, ChillPolymorphicInputComponent, ChillI18nLabelComponent, ChillI18nButtonLabelComponent, NoticeTransitionDirective], template: "<section class=\"chill-form-shell\">\n  @if (showSchemaHeader() && schema()?.displayName) {\n    <header class=\"form-header\">\n      <p class=\"form-kicker\"><app-chill-i18n-label [labelGuid]=\"'2DDE962B-086C-47B1-8A48-B16F0E34C0A3'\" [primaryDefaultText]=\"'Chill schema'\" [secondaryDefaultText]=\"'Schema Chill'\" /></p>\n      <h2>{{ schema()?.displayName }}</h2>\n    </header>\n  }\n\n  @if (layout.isLayoutEditingEnabled()) {\n    <div class=\"form-actions\">\n      <button type=\"button\" class=\"layout-button\" (click)=\"toggleEditMode()\" [disabled]=\"isSavingLayout()\">\n        @if (isSavingLayout()) {\n          <app-chill-i18n-button-label [labelGuid]=\"'F9BFE458-EA0D-4E27-A8A9-C7EE0C02F9FB'\" [primaryDefaultText]=\"'Saving layout...'\" [secondaryDefaultText]=\"'Salvataggio layout...'\" />\n        } @else if (isEditMode()) {\n          <app-chill-i18n-button-label [labelGuid]=\"'D7EA89E2-4AF2-455A-8FA9-33540E61D7C5'\" [primaryDefaultText]=\"'Done'\" [secondaryDefaultText]=\"'Fine'\" />\n        } @else {\n          <app-chill-i18n-button-label [labelGuid]=\"'872638CF-0346-4351-A53A-62A6B78B94FE'\" [primaryDefaultText]=\"'Edit mode'\" [secondaryDefaultText]=\"'Modalita modifica'\" />\n        }\n      </button>\n\n      @if (isEditMode()) {\n        <label class=\"column-count\">\n          <span><app-chill-i18n-label [labelGuid]=\"'665D0BFB-D1BB-4D53-B58A-C578CA559A0B'\" [primaryDefaultText]=\"'Columns'\" [secondaryDefaultText]=\"'Colonne'\" /></span>\n          <select\n            [ngModel]=\"layoutState().columnCount\"\n            (ngModelChange)=\"updateColumnCount($event)\"\n            name=\"form-column-count\">\n            @for (columnCount of columnOptions; track columnCount) {\n              <option [ngValue]=\"columnCount\">{{ columnCount }}</option>\n            }\n          </select>\n        </label>\n\n        <button type=\"button\" class=\"layout-button secondary\" (click)=\"addEmptyCell()\">\n          <app-chill-i18n-button-label [labelGuid]=\"'7B8C7557-FB39-4C6C-B70F-D70F87EC7517'\" [primaryDefaultText]=\"'Add empty cell'\" [secondaryDefaultText]=\"'Aggiungi cella vuota'\" />\n        </button>\n\n        <button type=\"button\" class=\"layout-button secondary\" (click)=\"resetLayout()\">\n          <app-chill-i18n-button-label [labelGuid]=\"'408CE646-58A0-4657-8192-D1C79BFE7F91'\" [primaryDefaultText]=\"'Reset'\" [secondaryDefaultText]=\"'Reset'\" />\n        </button>\n\n        <button type=\"button\" class=\"layout-button secondary\" (click)=\"refreshSchemaFromModel()\" [disabled]=\"isSavingLayout()\">\n          @if (isRefreshingSchema()) {\n            <app-chill-i18n-button-label [labelGuid]=\"'8628775B-B831-44F2-8A38-909F40E2F7B3'\" [primaryDefaultText]=\"'Updating schema...'\" [secondaryDefaultText]=\"'Aggiornamento schema...'\" />\n          } @else {\n            <app-chill-i18n-button-label [labelGuid]=\"'62953302-B951-4FD1-BD08-4B7649A91BAF'\" [primaryDefaultText]=\"'Update'\" [secondaryDefaultText]=\"'Aggiorna'\" />\n          }\n        </button>\n      }\n    </div>\n  }\n\n  @if (layoutError()) {\n    <div class=\"empty-state error-state\">{{ layoutError() }}</div>\n  }\n\n  @if (resolvedSubmitError()) {\n    <div class=\"notice error notice-dismissible\">\n      <span>{{ resolvedSubmitError() }}</span>\n      <button type=\"button\" class=\"notice-close\" (click)=\"clearSubmitError()\" [attr.aria-label]=\"chill.T('0C4C06EF-A105-468F-B1E2-AEA8EB96A4DC', 'Dismiss error', 'Chiudi errore')\">x</button>\n    </div>\n  }\n\n  @if (genericValidationMessage()) {\n    <div class=\"notice error\">\n      <span>{{ genericValidationMessage() }}</span>\n    </div>\n  }\n\n  @if (properties().length === 0) {\n    <div class=\"empty-state\">{{ chill.T('3D7ABF07-6E0C-4F99-A0C8-C936C44322C6', 'No schema properties available.', 'Nessuna propriet\u00E0 di schema disponibile.') }}</div>\n  } @else {\n    <form class=\"chill-form\" (ngSubmit)=\"submit()\">\n      <div class=\"form-grid\" [style.grid-template-columns]=\"gridTemplateColumns()\">\n        @for (item of layoutItems(); track trackByProperty($index, item)) {\n          <section\n            class=\"form-cell\"\n            [class.empty-cell]=\"item.kind === 'empty' || (item.kind === 'property' && item.hidden && !isEditMode())\"\n            [class.edit-mode]=\"isEditMode()\"\n            [style.grid-column]=\"'span ' + item.span\"\n            [draggable]=\"isEditMode()\"\n            (dragstart)=\"beginDrag($event, item.id)\"\n            (dragover)=\"allowDrop($event)\"\n            (drop)=\"dropProperty(item.id)\"\n            (dragend)=\"endDrag()\">\n            @if (isEditMode()) {\n              <div\n                class=\"cell-toolbar\"\n                (click)=\"$event.stopPropagation()\">\n                <div class=\"cell-toolbar__title-row\">\n                  <span>\n                    @if (item.kind === 'property') {\n                      {{ item.property.displayName || item.property.name }}\n                    } @else {\n                      {{ chill.T('278389E7-9C0F-4849-B7C8-B904CC0E4AAA', 'Empty cell', 'Cella vuota') }}\n                    }\n                  </span>\n                  @if (item.kind === 'property') {\n                    <input\n                      type=\"checkbox\"\n                      class=\"field-visibility-checkbox\"\n                      [checked]=\"!item.hidden\"\n                      (mousedown)=\"$event.stopPropagation()\"\n                      (click)=\"$event.stopPropagation()\"\n                      (change)=\"updatePropertyHidden(item.property.name, !$any($event.target).checked)\"\n                      [name]=\"'hidden-' + item.property.name\"\n                      [attr.aria-label]=\"chill.T('D72C8236-361A-4053-86B8-6F5A9D6983F6', 'Show field', 'Mostra campo')\" />\n                  }\n                </div>\n                <div class=\"cell-toolbar__settings-row\">\n                  @if (item.kind === 'property') {\n                    <button type=\"button\" class=\"cell-toolbar__icon-button\" (click)=\"openPropertySettings(item.property)\" [attr.aria-label]=\"chill.T('40E9838A-70F2-4E89-BE38-BBE44027D253', 'Edit property settings', 'Modifica impostazioni propriet\u00E0')\">\n                      <span class=\"material-symbol-icon\">tune</span>\n                    </button>\n                    <select\n                      class=\"property-type-select\"\n                      [ngModel]=\"item.property.propertyType\"\n                      (ngModelChange)=\"updatePropertyType(item.property, $event)\"\n                      [name]=\"'propertyType-' + item.property.name\"\n                      [attr.aria-label]=\"chill.T('A1803D67-C40D-41AF-BFD2-8F9B0E34C48B', 'Property type', 'Tipo propriet\u00E0')\">\n                      @for (option of propertyTypeOptions; track option.value) {\n                        <option [ngValue]=\"option.value\" [disabled]=\"isPropertyTypeOptionDisabled(item.property, option.value)\">{{ option.label }}</option>\n                      }\n                    </select>\n                  } @else {\n                    <span class=\"cell-toolbar__icon-spacer\"></span>\n                    <span class=\"cell-toolbar__select-spacer\"></span>\n                  }\n                  <div class=\"span-controls\">\n                    <button type=\"button\" class=\"cell-toolbar__icon-button\" (click)=\"decreaseSpan(item.id)\" [attr.aria-label]=\"chill.T('DB73501A-618F-48C7-A6BC-613B44729887', 'Decrease field span', 'Riduci larghezza campo')\">\n                      <span class=\"material-symbol-icon\" aria-hidden=\"true\">remove</span>\n                    </button>\n                    <span class=\"span-controls__value\">{{ item.span }}</span>\n                    <button type=\"button\" class=\"cell-toolbar__icon-button\" (click)=\"increaseSpan(item.id)\" [attr.aria-label]=\"chill.T('63E1E823-9015-4551-A43B-4B672E988B4A', 'Increase field span', 'Aumenta larghezza campo')\">\n                      <span class=\"material-symbol-icon\" aria-hidden=\"true\">add</span>\n                    </button>\n                  </div>\n                </div>\n              </div>\n            }\n            @else \n            {\n              @if (item.kind === 'property' && !item.hidden) {\n                <app-chill-polymorphic-input\n                  [form]=\"form()\"\n                  [schema]=\"schema()\"\n                  [propertyNames]=\"[item.property.name]\"\n                  [readonlyPropertyNames]=\"readonlyPropertyNames()\"\n                  [externalErrors]=\"serverFieldErrors()\"\n                  (validityChange)=\"updatePropertyValidity(item.property.name, $event)\"\n                  (fieldBlur)=\"handlePropertyBlur($event)\" />\n              } @else {\n                <div class=\"empty-cell-body\"></div>\n              }\n            }\n          </section>\n        }\n      </div>\n\n      @if (!isEditMode() && renderSubmitInsideForm() && !hasCustomSubmitHandler()) {\n        <button type=\"submit\" [disabled]=\"!canSubmit()\">\n          @if (submitLabelGuid() && submitPrimaryDefaultText() && submitSecondaryDefaultText()) {\n            <app-chill-i18n-button-label\n              [labelGuid]=\"submitLabelGuid()!\"\n              [primaryDefaultText]=\"submitPrimaryDefaultText()!\"\n              [secondaryDefaultText]=\"submitSecondaryDefaultText()!\" />\n          } @else {\n            {{ submitLabel() }}\n          }\n        </button>\n      }\n    </form>\n  }\n</section>\n", styles: [":host{display:block}.chill-form-shell{--tron-border: color-mix(in srgb, var(--accent) 24%, var(--border-color));--tron-border-strong: color-mix(in srgb, var(--accent) 42%, var(--border-color));--tron-glow: color-mix(in srgb, var(--accent) 14%, transparent);--tron-panel: linear-gradient(180deg, color-mix(in srgb, var(--surface-2) 94%, rgba(8, 18, 26, .18)), color-mix(in srgb, var(--surface-1) 96%, rgba(7, 14, 22, .22)));display:grid;gap:.85rem}.form-actions{display:flex;flex-wrap:wrap;gap:.65rem;align-items:center}.layout-button{min-height:2.5rem;padding:.65rem .9rem;border:1px solid var(--tron-border);border-radius:.4rem;background:var(--surface-0);color:var(--text-main);box-shadow:inset 0 0 0 1px color-mix(in srgb,var(--accent) 8%,transparent),0 0 .85rem var(--tron-glow);font-weight:700;cursor:pointer}.layout-button.secondary{background:color-mix(in srgb,var(--surface-0) 92%,var(--accent-soft))}.column-count{display:flex;gap:.5rem;align-items:center;color:var(--text-main);font-weight:600}.column-count select{width:5rem;padding:.55rem .7rem;border-radius:.4rem;border:1px solid var(--tron-border);background:var(--surface-0);color:var(--text-main);box-shadow:inset 0 0 0 1px color-mix(in srgb,var(--accent) 8%,transparent),0 0 .65rem var(--tron-glow);appearance:none}.form-header h2,.form-header p{margin:0}.form-kicker{color:var(--accent);font-size:.75rem;font-weight:700;letter-spacing:.18em;text-transform:uppercase}.chill-form{display:grid;gap:.85rem;position:relative;overflow:hidden}.chill-form:before{content:\"\";position:absolute;inset:0;pointer-events:none;background:linear-gradient(90deg,transparent 0,transparent calc(100% - 1px),color-mix(in srgb,var(--accent) 10%,transparent) calc(100% - 1px),color-mix(in srgb,var(--accent) 10%,transparent) 100%),linear-gradient(transparent 0,transparent calc(100% - 1px),color-mix(in srgb,var(--accent) 8%,transparent) calc(100% - 1px),color-mix(in srgb,var(--accent) 8%,transparent) 100%);background-size:2.75rem 2.75rem;opacity:.24}.form-grid{display:grid;gap:.85rem}.form-cell{display:grid;gap:.65rem;padding:.8rem;border-radius:.55rem;border:1px solid var(--tron-border);background:linear-gradient(180deg,color-mix(in srgb,var(--accent-soft) 44%,transparent),transparent 34%),linear-gradient(180deg,color-mix(in srgb,var(--surface-1) 94%,rgba(0,11,20,.2)),color-mix(in srgb,var(--surface-0) 90%,rgba(1,16,25,.22)));box-shadow:inset 0 0 0 1px color-mix(in srgb,var(--accent) 8%,transparent),0 0 .9rem color-mix(in srgb,var(--accent) 8%,transparent)}.form-cell.edit-mode{border-color:var(--tron-border-strong);-webkit-user-select:none;user-select:none}.form-cell.empty-cell:not(.edit-mode){display:none}.form-cell.empty-cell.edit-mode{min-height:4.5rem}.cell-toolbar{display:grid;gap:.4rem;min-width:0;color:var(--text-main);font-weight:600}.cell-toolbar__title-row,.cell-toolbar__settings-row{min-width:0}.cell-toolbar__title-row{display:grid;grid-template-columns:minmax(0,1fr) auto;align-items:center;gap:.45rem}.cell-toolbar__title-row span{min-width:0;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}.field-visibility-checkbox{width:1rem;height:1rem;margin:0;accent-color:var(--accent)}.cell-toolbar__settings-row{display:grid;grid-template-columns:2rem minmax(7.25rem,1fr) minmax(6.35rem,auto);align-items:center;gap:.45rem;width:100%}.cell-toolbar__icon-button,.cell-toolbar__icon-spacer{width:2rem;min-width:2rem;height:2rem}.cell-toolbar__select-spacer{display:block;min-width:7.25rem;height:2rem}.cell-toolbar__icon-button{display:inline-grid;place-items:center;padding:0;border:1px solid var(--tron-border);border-radius:.4rem;background:var(--surface-0);color:var(--text-main);box-shadow:inset 0 0 0 1px color-mix(in srgb,var(--accent) 8%,transparent);cursor:pointer}.span-controls{display:inline-flex;align-items:center;justify-self:end;gap:.15rem;min-width:6.35rem}.span-controls__value{min-width:1.75rem;color:var(--text-muted);font-size:.8rem;font-weight:700;text-align:center}.property-type-select{min-width:7.25rem;min-height:2rem;padding:.35rem .55rem;border:1px solid var(--tron-border);border-radius:.4rem;background:var(--surface-0);color:var(--text-main);font:inherit;font-size:.78rem}.empty-cell-body{min-height:3rem;border-radius:.45rem}.empty-cell-body.edit-mode{border:1px dashed var(--tron-border-strong);background:linear-gradient(180deg,color-mix(in srgb,var(--accent-soft) 68%,transparent),transparent),linear-gradient(90deg,transparent 0,transparent calc(100% - 1px),color-mix(in srgb,var(--accent) 18%,transparent) calc(100% - 1px),color-mix(in srgb,var(--accent) 18%,transparent) 100%),linear-gradient(transparent 0,transparent calc(100% - 1px),color-mix(in srgb,var(--accent) 14%,transparent) calc(100% - 1px),color-mix(in srgb,var(--accent) 14%,transparent) 100%);background-size:auto,1.1rem 1.1rem,1.1rem 1.1rem}button[type=submit]{justify-self:start;min-height:2.7rem;padding:.7rem 1rem;border:0;border-radius:.45rem;background:linear-gradient(135deg,color-mix(in srgb,var(--accent) 92%,#61f8f1),color-mix(in srgb,var(--accent-strong) 78%,#0fbcff));color:#07131a;font-weight:700;box-shadow:0 0 1rem color-mix(in srgb,var(--accent) 22%,transparent),0 0 1.8rem color-mix(in srgb,var(--accent) 10%,transparent);cursor:pointer}.empty-state{padding:.85rem 1rem;border-radius:.55rem;background:linear-gradient(180deg,color-mix(in srgb,var(--accent-soft) 42%,transparent),transparent 34%),var(--surface-2);border:1px dashed var(--tron-border);color:var(--text-muted);max-width:100%;max-height:10rem;overflow-x:hidden;overflow-y:auto;overflow-wrap:anywhere;word-break:break-word}.error-state{color:var(--danger)}:root[data-theme=dark] .layout-button,:root[data-theme=dark] .column-count select,:root[data-theme=dark] .cell-toolbar__icon-button,:root[data-theme=dark] .property-type-select{background:#09131a94}@media(max-width:720px){.form-grid{grid-template-columns:minmax(0,1fr)!important}.form-cell{grid-column:auto!important}}\n"] }]
        }], ctorParameters: () => [] });

const CHILL_PROPERTY_TYPE = {
    Unknown: 0,
    Guid: 1,
    Integer: 10,
    Decimal: 20,
    Date: 30,
    Time: 40,
    DateTime: 50,
    Duration: 60,
    Boolean: 70,
    String: 80,
    Text: 81,
    ChillEntity: 1000,
    ChillEntityCollection: 1010,
    ChillQuery: 1100
};
class ChillPolymorphicOutputComponent {
    constructor() {
        // #region Service Injections
        this.chill = inject(ChillService);
        this.host = inject(ElementRef);
        // #endregion
        // #region Inputs
        this.source = input(null);
        this.schema = input(null);
        this.propertyName = input.required();
        // #endregion
        // #region State
        this.hostWidth = signal(null);
        this.resizeObserver = null;
        // #endregion
        // #region Computed Properties
        this.property = computed(() => this.schema()?.properties.find((candidate) => candidate.name === this.propertyName()) ?? null);
        this.value = computed(() => this.readPropertyValue(this.source(), this.propertyName()));
        this.spacedDisplayParts = computed(() => this.buildSpacedDisplayParts(this.value(), this.property()));
        this.displayText = computed(() => this.formatValue(this.value(), this.property(), false));
        this.titleText = computed(() => this.formatValue(this.value(), this.property(), true));
    }
    // #endregion
    // #region Component Lifecycle
    /**
     * Tracks the rendered width so object labels can switch to their short form in tight cells.
     */
    ngOnInit() {
        if (typeof ResizeObserver === 'undefined') {
            return;
        }
        this.resizeObserver = new ResizeObserver((entries) => {
            const entry = entries[0];
            this.hostWidth.set(entry ? entry.contentRect.width : null);
        });
        this.resizeObserver.observe(this.host.nativeElement);
    }
    /**
     * Disconnects the resize observer when the component is destroyed.
     */
    ngOnDestroy() {
        this.resizeObserver?.disconnect();
    }
    // #endregion
    // #region Helper Methods
    /**
     * Reads a property from the entity bag first, then falls back to top-level camelCase/PascalCase fields.
     */
    readPropertyValue(source, propertyName) {
        if (!source) {
            return undefined;
        }
        const properties = source.properties
            ?? (this.isJsonObject(source['Properties']) ? source['Properties'] : undefined);
        if (properties && propertyName in properties) {
            return properties[propertyName];
        }
        return source[propertyName] ?? source[this.toPascalCase(propertyName)];
    }
    /**
     * Formats scalars, dates, arrays, and entity-like objects using the schema type and display context.
     */
    formatValue(value, property, preferFullLabel) {
        if (value === undefined || value === null) {
            return '';
        }
        if (Array.isArray(value)) {
            return value
                .map((item) => this.formatValue(item, property, preferFullLabel))
                .filter((item) => item.length > 0)
                .join(', ');
        }
        const propertyType = property?.propertyType ?? CHILL_PROPERTY_TYPE.Unknown;
        switch (propertyType) {
            case CHILL_PROPERTY_TYPE.Integer:
            case CHILL_PROPERTY_TYPE.Decimal:
                return this.formatNumber(value);
            case CHILL_PROPERTY_TYPE.Boolean:
                return value === true
                    ? this.chill.T('1A29951D-C442-4187-B0AA-F80454DEB09D', 'Yes', 'Si')
                    : this.chill.T('8A65EBA6-81BD-4733-87D5-4CFE3F5C2D3F', 'No', 'No');
            case CHILL_PROPERTY_TYPE.Date:
                return this.formatDate(value);
            case CHILL_PROPERTY_TYPE.Time:
                return this.formatTime(value);
            case CHILL_PROPERTY_TYPE.DateTime:
                return this.formatDateTime(value);
            case CHILL_PROPERTY_TYPE.ChillEntity:
            case CHILL_PROPERTY_TYPE.ChillQuery:
                return this.formatObjectValue(value, preferFullLabel);
            default:
                if (typeof value === 'string' || typeof value === 'number' || typeof value === 'boolean') {
                    return String(value);
                }
                return this.formatObjectValue(value, preferFullLabel);
        }
    }
    /**
     * Formats valid date strings with the local date formatter and otherwise preserves the raw value.
     */
    formatDate(value) {
        if (typeof value !== 'string' || !value.trim()) {
            return '';
        }
        return this.chill.formatDisplayDate(value);
    }
    /**
     * Formats valid date-time strings with the local formatter and otherwise preserves the raw value.
     */
    formatDateTime(value) {
        if (typeof value !== 'string' || !value.trim()) {
            return '';
        }
        return this.chill.formatDisplayDateTime(value);
    }
    buildSpacedDisplayParts(value, property) {
        if (!property || !this.shouldRenderAsSpacedParts(property)) {
            return null;
        }
        if (value === undefined || value === null) {
            return null;
        }
        const formattedValue = this.formatValue(value, property, false);
        if (!formattedValue) {
            return null;
        }
        const parts = formattedValue.trim().split(/\s+/);
        if (parts.length < 1) {
            return null;
        }
        return parts;
    }
    formatTime(value) {
        if (typeof value !== 'string' || !value.trim()) {
            return '';
        }
        return this.chill.formatDisplayTime(value);
    }
    formatNumber(value) {
        if (typeof value === 'number' && Number.isFinite(value)) {
            return this.chill.formatDisplayNumber(value);
        }
        if (typeof value === 'string' && value.trim()) {
            const parsedValue = this.chill.parseDisplayDecimal(value);
            return parsedValue === null
                ? value
                : this.chill.formatDisplayNumber(parsedValue);
        }
        return '';
    }
    /**
     * Chooses the most useful label from an object payload and optionally prefers `ShortLabel` in narrow cells.
     */
    formatObjectValue(value, preferFullLabel) {
        if (!this.isJsonObject(value)) {
            return typeof value === 'string' || typeof value === 'number' || typeof value === 'boolean'
                ? String(value)
                : '';
        }
        const label = this.readObjectText(value, ['Label', 'label']);
        const shortLabel = this.readObjectText(value, ['ShortLabel', 'shortLabel']);
        const displayName = this.readObjectText(value, ['DisplayName', 'displayName']);
        const name = this.readObjectText(value, ['Name', 'name']);
        const guid = this.readObjectText(value, ['Guid', 'guid']);
        const shouldUseShortLabel = !preferFullLabel
            && !!shortLabel
            && this.shouldPreferShortLabel();
        const resolvedLabel = shouldUseShortLabel
            ? shortLabel
            : label || shortLabel || displayName || name || guid;
        return resolvedLabel ?? '';
    }
    /**
     * Converts a property name to PascalCase to match server payloads that expose both casing styles.
     */
    toPascalCase(value) {
        return value.length > 0
            ? `${value[0].toUpperCase()}${value.slice(1)}`
            : value;
    }
    /**
     * Checks whether a JSON value is a non-array object.
     */
    isJsonObject(value) {
        return !!value && typeof value === 'object' && !Array.isArray(value);
    }
    /**
     * Returns the first non-empty string, number, or boolean found among the candidate keys.
     */
    readObjectText(value, keys) {
        for (const key of keys) {
            const candidate = value[key];
            if (typeof candidate === 'string' && candidate.trim()) {
                return candidate.trim();
            }
            if (typeof candidate === 'number' || typeof candidate === 'boolean') {
                return String(candidate);
            }
        }
        return null;
    }
    /**
     * Treats cells narrower than 140px as compact enough to prefer short labels.
     */
    shouldPreferShortLabel() {
        const hostWidth = this.hostWidth();
        return hostWidth !== null && hostWidth < 140;
    }
    shouldRenderAsSpacedParts(property) {
        return property.propertyType === CHILL_PROPERTY_TYPE.Date
            || property.propertyType === CHILL_PROPERTY_TYPE.Time
            || property.propertyType === CHILL_PROPERTY_TYPE.DateTime;
    }
    static { this.ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: ChillPolymorphicOutputComponent, deps: [], target: i0.ɵɵFactoryTarget.Component }); }
    static { this.ɵcmp = i0.ɵɵngDeclareComponent({ minVersion: "17.0.0", version: "19.2.21", type: ChillPolymorphicOutputComponent, isStandalone: true, selector: "app-chill-polymorphic-output", inputs: { source: { classPropertyName: "source", publicName: "source", isSignal: true, isRequired: false, transformFunction: null }, schema: { classPropertyName: "schema", publicName: "schema", isSignal: true, isRequired: false, transformFunction: null }, propertyName: { classPropertyName: "propertyName", publicName: "propertyName", isSignal: true, isRequired: true, transformFunction: null } }, ngImport: i0, template: `
    <span class="polymorphic-output" [title]="titleText()">
      @if (spacedDisplayParts(); as parts) {
        <span class="polymorphic-output__spaced-parts">
          @for (part of parts; track $index; let isFirst = $first) {
            @if (!isFirst) {
              <span class="polymorphic-output__spaced-parts-separator"> </span>
            }
            <span class="polymorphic-output__spaced-parts-part">{{ part }}</span>
          }
        </span>
      } @else {
        {{ displayText() }}
      }
    </span>
  `, isInline: true, styles: [":host{display:block;min-width:0}.polymorphic-output{display:block;min-width:0;overflow-wrap:anywhere}.polymorphic-output__spaced-parts{display:inline}.polymorphic-output__spaced-parts-part{white-space:nowrap}\n"], dependencies: [{ kind: "ngmodule", type: CommonModule }] }); }
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: ChillPolymorphicOutputComponent, decorators: [{
            type: Component,
            args: [{ selector: 'app-chill-polymorphic-output', standalone: true, imports: [CommonModule], template: `
    <span class="polymorphic-output" [title]="titleText()">
      @if (spacedDisplayParts(); as parts) {
        <span class="polymorphic-output__spaced-parts">
          @for (part of parts; track $index; let isFirst = $first) {
            @if (!isFirst) {
              <span class="polymorphic-output__spaced-parts-separator"> </span>
            }
            <span class="polymorphic-output__spaced-parts-part">{{ part }}</span>
          }
        </span>
      } @else {
        {{ displayText() }}
      }
    </span>
  `, styles: [":host{display:block;min-width:0}.polymorphic-output{display:block;min-width:0;overflow-wrap:anywhere}.polymorphic-output__spaced-parts{display:inline}.polymorphic-output__spaced-parts-part{white-space:nowrap}\n"] }]
        }] });

const TABLE_LAYOUT_METADATA_KEY = 'chill-table-component';
const TABLE_COLUMN_WIDTH_METADATA_KEY = 'widthProportion';
const TABLE_COLUMN_WIDTH_OPTIONS = [0.25, 0.5, 1, 2, 3, 4, 5];
const ROW_REFRESH_FLASH_MS = 900;
class ChillTableComponent {
    // #endregion
    // #region Component Lifecycle
    /**
     * Wires reactive state for layout persistence, live entity updates, validation-driven focus, and inline edit completion.
     */
    constructor() {
        // #region Service Injections
        this.chill = inject(ChillService);
        this.dialog = inject(WorkspaceDialogService, { optional: true });
        this.layout = inject(WorkspaceLayoutService);
        // #endregion
        // #region Inputs
        this.schema = input(null);
        this.entities = input([]);
        this.selectionColumn = input(null);
        this.rowAction = input(null);
        this.rowActions = input(null);
        this.enableInlineEditing = input(false);
        this.readonlyPropertyNames = input(null);
        this.validationFocus = input(null);
        this.showSchemaHeader = input(true);
        this.ordering = input(null);
        this.enableFullTextSearch = input(false);
        this.fullTextSearch = input('');
        this.showMobileTaskClose = input(false);
        // #endregion
        this.columnWidthOptions = TABLE_COLUMN_WIDTH_OPTIONS;
        this.propertyTypeOptions = CHILL_PROPERTY_TYPE_OPTIONS;
        // #region Outputs
        this.cellEditCommit = output();
        this.sortChange = output();
        this.fullTextSearchChange = output();
        this.schemaUpdated = output();
        this.mobileTaskClose = output();
        // #endregion
        // #region State References
        this.fullTextSearchInput = viewChild('fullTextSearchInput');
        this.isEditLayoutMode = signal(false);
        this.isSavingLayout = signal(false);
        this.isRefreshingSchema = signal(false);
        this.layoutError = signal('');
        this.dragColumnName = signal('');
        this.layoutState = signal([]);
        this.activeCellEdit = signal(null);
        this.activeRowActionMenu = signal(null);
        this.displayedEntities = signal([]);
        this.schemaRefreshTick = signal(0);
        this.isFullTextSearchOpen = signal(false);
        this.fullTextSearchText = signal('');
        this.rowRefreshFlashKeys = signal(new Set());
        this.entityNotificationSubscriptions = new Map();
        this.rowRefreshFlashTimers = new Map();
        this.subscribedNotificationChillType = '';
        // #endregion
        // #region Computed Properties
        /**
         * Merges schema properties with persisted layout preferences and preserves the saved column order.
         */
        this.columns = computed(() => {
            this.schemaRefreshTick();
            const schema = this.schema();
            const properties = schema?.properties ?? [];
            const propertyMap = new Map(properties.map((property) => [property.name, property]));
            const savedLayout = this.layoutState();
            const orderedNames = [
                ...savedLayout.map((item) => item.name).filter((name) => propertyMap.has(name)),
                ...properties.map((property) => property.name).filter((name) => !savedLayout.some((item) => item.name === name))
            ];
            return orderedNames
                .map((name) => {
                const property = propertyMap.get(name);
                if (!property) {
                    return null;
                }
                const layout = savedLayout.find((item) => item.name === name);
                return {
                    ...property,
                    displayName: layout?.displayName?.trim() || property.displayName || property.name,
                    hidden: layout?.hidden ?? false,
                    widthProportion: layout?.widthProportion ?? this.readPropertyWidthProportion(property)
                };
            })
                .filter((column) => column !== null);
        });
        /**
         * Filters the resolved column list down to visible columns.
         */
        this.visibleColumns = computed(() => this.columns().filter((column) => !column.hidden));
        this.tableMinimumWidth = computed(() => {
            const dataColumnCount = this.visibleColumns().length;
            const pinnedColumnWidthRem = this.pinnedColumnWidthRem();
            const minimumWidthRem = Math.max(36, dataColumnCount * 12 + pinnedColumnWidthRem);
            return `max(100%, ${minimumWidthRem}rem)`;
        });
        this.mobileTableMinimumWidth = computed(() => {
            const dataColumnCount = this.visibleColumns().length;
            const pinnedColumnWidthRem = this.pinnedColumnWidthRem();
            const dataColumnWidthVw = Math.max(1, dataColumnCount) * 50;
            if (pinnedColumnWidthRem <= 0) {
                return `max(100%, ${dataColumnWidthVw}vw)`;
            }
            return `max(100%, calc(${dataColumnWidthVw}vw + ${pinnedColumnWidthRem}rem))`;
        });
        /**
         * Filters the resolved column list down to hidden columns.
         */
        this.hiddenColumns = computed(() => this.columns().filter((column) => column.hidden));
        /**
         * Hides row selection while the user is editing layout metadata.
         */
        this.hasSelectionColumn = computed(() => !!this.selectionColumn() && !this.isEditLayoutMode());
        /**
         * Normalizes the single-action and multi-action inputs into one action list.
         */
        this.resolvedRowActions = computed(() => {
            const rowActions = this.rowActions();
            if (rowActions && rowActions.length > 0) {
                return rowActions;
            }
            const rowAction = this.rowAction();
            return rowAction ? [rowAction] : [];
        });
        /**
         * Hides row actions while the user is editing layout metadata.
         */
        this.hasActionColumn = computed(() => this.resolvedRowActions().length > 0 && !this.isEditLayoutMode());
        this.readonlyPropertyNameSet = computed(() => new Set((this.readonlyPropertyNames() ?? [])
            .map((propertyName) => propertyName.trim().toLowerCase())
            .filter((propertyName) => propertyName.length > 0)));
        effect(() => {
            this.layoutState.set(this.readLayoutState(this.schema()));
            this.layoutError.set('');
            this.isEditLayoutMode.set(false);
        });
        effect(() => {
            const searchText = this.fullTextSearch();
            this.fullTextSearchText.set(searchText);
            if (searchText.trim().length > 0) {
                this.isFullTextSearchOpen.set(true);
            }
        });
        effect(() => {
            const entities = this.entities();
            this.displayedEntities.set(entities);
        });
        effect(() => {
            this.syncEntityNotificationSubscriptions(this.schema(), this.displayedEntities());
        });
        effect(() => {
            if (!this.layout.isLayoutEditingEnabled()) {
                this.isEditLayoutMode.set(false);
            }
        });
        effect(() => {
            const validationFocus = this.validationFocus();
            if (!validationFocus || !this.enableInlineEditing()) {
                return;
            }
            const activeCellEdit = this.activeCellEdit();
            if (activeCellEdit
                && activeCellEdit.entityKey === validationFocus.entityKey
                && activeCellEdit.propertyName === validationFocus.propertyName) {
                return;
            }
            const targetEntity = this.displayedEntities().find((entity) => this.trackByEntity(0, entity) === validationFocus.entityKey);
            const targetColumn = this.visibleColumns().find((column) => column.name === validationFocus.propertyName);
            if (!targetEntity || !targetColumn || this.isDeletedRow(targetEntity)) {
                return;
            }
            this.activateCellEdit(targetEntity, targetColumn);
        });
        effect(() => {
            const activeCellEdit = this.activeCellEdit();
            if (!activeCellEdit?.isCommitting) {
                return;
            }
            const latestEntity = this.displayedEntities().find((entity) => this.trackByEntity(0, entity) === activeCellEdit.entityKey);
            if (!latestEntity) {
                this.activeCellEdit.set(null);
                return;
            }
            if (this.rowHasValidationErrors(latestEntity)) {
                this.activeCellEdit.set({
                    ...activeCellEdit,
                    entity: latestEntity,
                    isCommitting: false
                });
                return;
            }
            const latestValue = this.readPropertyValue(latestEntity, activeCellEdit.propertyName) ?? null;
            const editedValue = activeCellEdit.form.controls[activeCellEdit.propertyName]?.value ?? null;
            if (this.areJsonValuesEqual(latestValue, editedValue)) {
                this.activeCellEdit.set(null);
            }
        });
    }
    /**
     * Releases per-entity live update subscriptions when the table is destroyed.
     */
    ngOnDestroy() {
        this.clearEntityNotificationSubscriptions();
        this.clearRowRefreshFlashTimers();
    }
    // #endregion
    // #region Public Methods
    /**
     * Builds a stable row key, preferring Guid-like identifiers before falling back to labels or index.
     */
    trackByEntity(index, entity) {
        return this.readEntityText(entity, 'guid')
            ?? this.readEntityText(entity, 'Guid')
            ?? this.readEntityText(entity, 'label')
            ?? this.readEntityText(entity, 'Label')
            ?? `${index}`;
    }
    /**
     * Enters layout-edit mode immediately, or persists the current layout when toggled off.
     */
    toggleEditLayoutMode() {
        if (!this.layout.isLayoutEditingEnabled()) {
            return;
        }
        if (!this.isEditLayoutMode()) {
            this.isEditLayoutMode.set(true);
            this.layoutError.set('');
            return;
        }
        this.saveLayout();
    }
    /**
     * Applies an in-memory display-name override for the selected column.
     */
    updateColumnDisplayName(columnName, value) {
        this.layoutState.update((current) => current.map((item) => item.name === columnName
            ? { ...item, displayName: value }
            : item));
    }
    /**
     * Marks a column as visible or hidden inside the pending layout state.
     */
    updateColumnHidden(columnName, hidden) {
        this.layoutState.update((current) => current.map((item) => item.name === columnName
            ? { ...item, hidden }
            : item));
    }
    toggleFullTextSearch() {
        if (!this.enableFullTextSearch()) {
            return;
        }
        if (!this.isFullTextSearchOpen()) {
            this.isFullTextSearchOpen.set(true);
            setTimeout(() => this.fullTextSearchInput()?.nativeElement.focus());
            return;
        }
        this.submitFullTextSearch();
    }
    updateFullTextSearchText(value) {
        this.fullTextSearchText.set(value);
    }
    refreshSchemaFromModel() {
        const schema = this.schema();
        const chillType = schema?.chillType?.trim() ?? '';
        const chillViewCode = schema?.chillViewCode?.trim() || 'default';
        if (!schema || !chillType || this.isRefreshingSchema()) {
            return;
        }
        this.isRefreshingSchema.set(true);
        this.isSavingLayout.set(true);
        this.layoutError.set('');
        this.chill.getSchema(chillType, chillViewCode, undefined, true).subscribe({
            next: (updatedSchema) => {
                if (!updatedSchema) {
                    this.layoutError.set(this.chill.T('A6A6949E-F0D4-42F5-A8AE-E15B1B174084', 'The result schema is unavailable.', 'Lo schema dei risultati non è disponibile.'));
                    return;
                }
                this.applyUpdatedSchema(schema, updatedSchema);
            },
            error: (error) => {
                this.layoutError.set(this.chill.formatError(error));
                this.isRefreshingSchema.set(false);
                this.isSavingLayout.set(false);
            },
            complete: () => {
                this.isRefreshingSchema.set(false);
                this.isSavingLayout.set(false);
            }
        });
    }
    submitFullTextSearch() {
        if (!this.enableFullTextSearch()) {
            return;
        }
        const normalizedText = this.fullTextSearchText().trim();
        this.fullTextSearchText.set(normalizedText);
        this.fullTextSearchChange.emit(normalizedText);
    }
    resetFullTextSearch() {
        if (!this.enableFullTextSearch()) {
            return;
        }
        this.fullTextSearchText.set('');
        this.fullTextSearchChange.emit('');
        this.isFullTextSearchOpen.set(false);
    }
    closeMobileTask() {
        if (!this.showMobileTaskClose()) {
            return;
        }
        this.mobileTaskClose.emit();
    }
    updateColumnWidthProportion(columnName, direction) {
        this.layoutState.update((current) => current.map((item) => {
            if (item.name !== columnName) {
                return item;
            }
            const currentIndex = this.findColumnWidthOptionIndex(item.widthProportion);
            const nextIndex = Math.max(0, Math.min(this.columnWidthOptions.length - 1, currentIndex + direction));
            return {
                ...item,
                widthProportion: this.columnWidthOptions[nextIndex]
            };
        }));
    }
    canDecreaseColumnWidth(column) {
        return this.findColumnWidthOptionIndex(column.widthProportion) > 0;
    }
    canIncreaseColumnWidth(column) {
        return this.findColumnWidthOptionIndex(column.widthProportion) < this.columnWidthOptions.length - 1;
    }
    columnWidthLabel(column) {
        return `${this.normalizeColumnWidthProportion(column.widthProportion)}x`;
    }
    columnWidthPercent(column) {
        const visibleWidthTotal = this.visibleColumns()
            .reduce((total, item) => total + this.normalizeColumnWidthProportion(item.widthProportion), 0);
        if (visibleWidthTotal <= 0) {
            return 0;
        }
        return this.normalizeColumnWidthProportion(column.widthProportion) / visibleWidthTotal * 100;
    }
    openPropertySettings(property) {
        const schema = this.schema();
        if (!schema || !this.dialog) {
            return;
        }
        void (async () => {
            const { SchemaPropertyDialogComponent } = await Promise.resolve().then(function () { return schemaPropertyDialog_component; });
            const result = await this.dialog.openDialog({
                title: property.displayName?.trim() || property.name,
                component: SchemaPropertyDialogComponent,
                okLabel: this.chill.T('62953302-B951-4FD1-BD08-4B7649A91BAF', 'Save', 'Salva'),
                inputs: {
                    schema,
                    property
                }
            });
            if (result.status !== 'confirmed' || !result.value) {
                return;
            }
            this.savePropertySchema(schema, property.name, result.value);
        })();
    }
    /**
     * Moves a hidden column back into the visible portion of the saved layout ordering.
     */
    revealColumn(columnName) {
        const normalizedColumnName = columnName.trim();
        if (!normalizedColumnName) {
            return;
        }
        this.layoutState.update((current) => {
            const targetIndex = current.findIndex((item) => item.name === normalizedColumnName);
            if (targetIndex < 0) {
                return current;
            }
            const next = [...current];
            const [target] = next.splice(targetIndex, 1);
            const insertIndex = next.findIndex((item) => item.hidden);
            next.splice(insertIndex >= 0 ? insertIndex : next.length, 0, { ...target, hidden: false });
            return next;
        });
    }
    /**
     * Records which column is being dragged during layout editing.
     */
    beginDrag(event, columnName) {
        if (!this.isEditLayoutMode()) {
            return;
        }
        if (this.isColumnEditorControl(event.target)) {
            event.preventDefault();
            return;
        }
        this.dragColumnName.set(columnName);
    }
    /**
     * Enables the column drop target only while layout editing is active.
     */
    allowDrop(event) {
        if (!this.isEditLayoutMode()) {
            return;
        }
        event.preventDefault();
    }
    /**
     * Reorders the pending layout by moving the dragged column onto the target position.
     */
    dropColumn(targetColumnName) {
        const sourceColumnName = this.dragColumnName();
        if (!sourceColumnName || sourceColumnName === targetColumnName) {
            this.dragColumnName.set('');
            return;
        }
        this.layoutState.update((current) => {
            const next = [...current];
            const sourceIndex = next.findIndex((item) => item.name === sourceColumnName);
            const targetIndex = next.findIndex((item) => item.name === targetColumnName);
            if (sourceIndex < 0 || targetIndex < 0) {
                return current;
            }
            const [moved] = next.splice(sourceIndex, 1);
            next.splice(targetIndex, 0, moved);
            return next;
        });
        this.dragColumnName.set('');
    }
    /**
     * Clears the active drag marker after drag completes or is cancelled.
     */
    endDrag() {
        this.dragColumnName.set('');
    }
    /**
     * Invokes the configured row action with the current entity.
     */
    runRowAction(action, entity, menu) {
        void menu;
        this.closeRowActionMenu();
        action.handler(entity);
    }
    /**
     * Toggles the floating row-action menu anchored to the trigger button.
     */
    toggleRowActionMenu(event, entity) {
        event.preventDefault();
        event.stopPropagation();
        const trigger = event.currentTarget;
        if (!(trigger instanceof HTMLElement)) {
            return;
        }
        const entityKey = this.trackByEntity(0, entity);
        const currentMenu = this.activeRowActionMenu();
        if (currentMenu?.entityKey === entityKey) {
            this.closeRowActionMenu();
            return;
        }
        this.activeRowActionMenu.set(this.buildRowActionMenuState(entityKey, trigger));
    }
    /**
     * Returns true when the floating row-action menu belongs to the provided entity.
     */
    isRowActionMenuOpen(entity) {
        return this.activeRowActionMenu()?.entityKey === this.trackByEntity(0, entity);
    }
    /**
     * Closes the floating row-action menu.
     */
    closeRowActionMenu() {
        this.activeRowActionMenu.set(null);
    }
    /**
     * Exposes the computed fixed-position style for the active row-action menu.
     */
    rowActionMenuStyle() {
        const menu = this.activeRowActionMenu();
        if (!menu) {
            return null;
        }
        return {
            top: `${menu.top}px`,
            left: `${menu.left}px`
        };
    }
    /**
     * Maps a few common semantic action names to icons and otherwise returns the provided icon verbatim.
     */
    rowActionIcon(action) {
        const icon = action.icon?.trim();
        if (!icon) {
            return 'edit';
        }
        if (action.iconClass === 'material-symbol-icon') {
            return icon;
        }
        switch (icon.toLowerCase()) {
            case 'pencil':
            case 'edit':
                return 'edit';
            case 'bin':
            case 'delete':
            case 'trash':
                return 'delete';
            default:
                return icon;
        }
    }
    /**
     * Applies Material Symbols automatically for common semantic row actions.
     */
    rowActionIconClass(action) {
        if (action.iconClass === 'material-symbol-icon') {
            return 'material-symbol-icon';
        }
        const icon = action.icon?.trim().toLowerCase();
        if (!icon || icon === 'pencil' || icon === 'edit' || icon === 'bin' || icon === 'delete' || icon === 'trash') {
            return 'material-symbol-icon';
        }
        return action.iconClass?.trim() ?? '';
    }
    /**
     * Derives a readable row-action label when the host does not provide one.
     */
    rowActionLabel(action) {
        if (action.labelGuid?.trim() && action.primaryDefaultText?.trim() && action.secondaryDefaultText?.trim()) {
            return this.chill.T(action.labelGuid.trim(), action.primaryDefaultText.trim(), action.secondaryDefaultText.trim());
        }
        if (action.label?.trim()) {
            return action.label.trim();
        }
        if (action.ariaLabel?.trim()) {
            return action.ariaLabel.trim();
        }
        switch (this.rowActionIcon(action).trim().toLowerCase()) {
            case 'edit':
                return this.chill.T('E64B6037-B83A-406A-B5D6-CB5AA6E42FC6', 'Edit row', 'Modifica riga');
            case 'delete':
                return this.chill.T('04290FEE-910B-4A1B-B83D-A3AC0427BAAB', 'Delete row', 'Elimina riga');
            default:
                return this.chill.T('6455D4FC-D267-4AA1-83C9-749D511838CB', 'Row action', 'Azione riga');
        }
    }
    toggleColumnSort(column) {
        if (this.isEditLayoutMode() || !this.canSortColumn(column)) {
            return;
        }
        const propertyName = column.name.trim();
        if (!propertyName) {
            return;
        }
        const activeOrdering = this.readActiveOrdering();
        if (!activeOrdering || activeOrdering.propertyName !== propertyName) {
            this.sortChange.emit({
                propertyName,
                direction: 'ASC'
            });
            return;
        }
        if (activeOrdering.direction === 'ASC') {
            this.sortChange.emit({
                propertyName,
                direction: 'DESC'
            });
            return;
        }
        this.sortChange.emit({
            propertyName,
            direction: null
        });
    }
    sortDirectionFor(column) {
        const activeOrdering = this.readActiveOrdering();
        return activeOrdering?.propertyName === column.name.trim()
            ? (activeOrdering.direction === 'DESC' ? 'DESC' : 'ASC')
            : null;
    }
    isColumnEditing(column) {
        return this.activeCellEdit()?.propertyName === column.name;
    }
    isColumnReadOnly(column) {
        return this.readonlyPropertyNameSet().has(column.name.trim().toLowerCase());
    }
    canSortColumn(column) {
        return (column.propertyType ?? CHILL_PROPERTY_TYPE$1.Unknown) !== CHILL_PROPERTY_TYPE$1.ChillEntityCollection;
    }
    isPropertyTypeOptionDisabled(property, propertyType) {
        return !canChangeChillPropertyType(property.propertyType, propertyType);
    }
    updatePropertyType(property, value) {
        const schema = this.schema();
        const parsed = typeof value === 'number' ? value : Number(value);
        if (!schema || !Number.isFinite(parsed) || !canChangeChillPropertyType(property.propertyType, parsed)) {
            return;
        }
        if ((property.propertyType ?? CHILL_PROPERTY_TYPE$1.Unknown) === parsed) {
            return;
        }
        this.savePropertySchema(schema, property.name, {
            ...property,
            propertyType: parsed,
            simplePropertyType: chillSimplePropertyType(parsed)
        });
    }
    readActiveOrdering() {
        const ordering = this.ordering();
        return ordering?.propertyName?.trim()
            ? {
                propertyName: ordering.propertyName.trim(),
                direction: ordering.direction === 'DESC' ? 'DESC' : 'ASC'
            }
            : null;
    }
    isColumnEditorControl(target) {
        return target instanceof HTMLElement
            && !!target.closest('input, button, select, textarea');
    }
    /**
     * Forwards row selection changes to the hosting selection controller.
     */
    toggleRowSelection(entity, selected) {
        this.selectionColumn()?.toggle(entity, selected);
    }
    /**
     * Reads the current selection state from the hosting selection controller.
     */
    isRowSelected(entity) {
        return this.selectionColumn()?.isSelected(entity) ?? false;
    }
    /**
     * Delegates row-selection disabled state to the host when provided.
     */
    isRowSelectionDisabled(entity) {
        return this.selectionColumn()?.disabled?.(entity) ?? false;
    }
    /**
     * Evaluates whether a row action should be disabled for the current entity.
     */
    isRowActionDisabled(action, entity) {
        return action.disabled?.(entity) ?? false;
    }
    handleDocumentClick() {
        this.closeRowActionMenu();
    }
    handleWindowResize() {
        this.closeRowActionMenu();
    }
    handleWindowScroll() {
        this.closeRowActionMenu();
    }
    /**
     * Treats non-pristine CRUD states as pending so the row can render transient styling.
     */
    isPendingRow(entity) {
        const state = this.readCrudState(entity);
        return state === 'draft' || state === 'dirty' || state === 'saving' || state === 'error' || state === 'deleted';
    }
    /**
     * Uses the normalized CRUD status to identify deleted rows.
     */
    isDeletedRow(entity) {
        return this.readCrudState(entity) === 'deleted';
    }
    isRefreshFlashRow(entity) {
        return this.rowRefreshFlashKeys().has(this.trackByEntity(0, entity));
    }
    /**
     * Creates a single-property edit session for the chosen cell using a fresh schema-driven form.
     */
    activateCellEdit(entity, column) {
        if (!this.enableInlineEditing() || this.isEditLayoutMode() || this.isDeletedRow(entity) || this.isColumnReadOnly(column)) {
            return;
        }
        const schema = this.schema();
        if (!schema) {
            return;
        }
        const propertyName = column.name;
        this.activeCellEdit.set({
            entityKey: this.trackByEntity(0, entity),
            propertyName,
            entity,
            form: this.chill.prepareForm(schema, entity),
            originalValue: this.readPropertyValue(entity, propertyName) ?? null,
            isValid: true,
            isLookupDialogOpen: false,
            isCommitting: false
        });
    }
    /**
     * Matches the requested cell against the current inline edit session.
     */
    isCellEditing(entity, column) {
        const activeCellEdit = this.activeCellEdit();
        return !!activeCellEdit
            && activeCellEdit.entityKey === this.trackByEntity(0, entity)
            && activeCellEdit.propertyName === column.name;
    }
    /**
     * Clears the committing flag when the active editor changes its tracked property value.
     */
    handleCellValueChange(value) {
        const activeCellEdit = this.activeCellEdit();
        if (!activeCellEdit) {
            return;
        }
        if (!(activeCellEdit.propertyName in value)) {
            return;
        }
        this.activeCellEdit.set({
            ...activeCellEdit,
            isCommitting: false
        });
    }
    /**
     * Keeps the active edit session aligned with the child editor validity state.
     */
    handleCellValidityChange(isValid) {
        const activeCellEdit = this.activeCellEdit();
        if (!activeCellEdit) {
            return;
        }
        this.activeCellEdit.set({
            ...activeCellEdit,
            isValid,
            isCommitting: false
        });
    }
    /**
     * Keeps inline editing alive while a lookup picker dialog owns the focus outside the table cell.
     */
    handleLookupDialogOpenChange(isOpen) {
        const activeCellEdit = this.activeCellEdit();
        if (!activeCellEdit) {
            return;
        }
        this.activeCellEdit.set({
            ...activeCellEdit,
            isLookupDialogOpen: isOpen
        });
        if (isOpen) {
            return;
        }
        const activeControl = activeCellEdit.form.controls[activeCellEdit.propertyName];
        if (activeControl?.dirty) {
            this.commitCellEdit();
        }
    }
    /**
     * Commits the edit only when focus leaves the entire editor, not when it moves within the editor.
     */
    handleCellFocusOut(event) {
        const currentTarget = event.currentTarget;
        if (!(currentTarget instanceof HTMLElement)) {
            return;
        }
        const relatedTarget = event.relatedTarget;
        if (relatedTarget instanceof Node && currentTarget.contains(relatedTarget)) {
            return;
        }
        if (this.activeCellEdit()?.isLookupDialogOpen) {
            return;
        }
        this.commitCellEdit();
    }
    /**
     * Supports Enter-to-commit and Escape-to-cancel without letting the event leak to the row.
     */
    handleCellEditorKeydown(event) {
        if (this.isMonacoEditorEventTarget(event.target)) {
            return;
        }
        if (event.key === 'Enter') {
            event.preventDefault();
            event.stopPropagation();
            this.commitCellEdit();
            return;
        }
        if (event.key === 'Escape') {
            event.preventDefault();
            event.stopPropagation();
            this.cancelCellEdit();
        }
    }
    /**
     * Drops the current inline edit session without emitting a commit.
     */
    cancelCellEdit() {
        this.activeCellEdit.set(null);
    }
    /**
     * Emits a cell commit only for valid edits whose value actually changed from the original snapshot.
     */
    commitCellEdit() {
        const activeCellEdit = this.activeCellEdit();
        if (!activeCellEdit) {
            return;
        }
        const value = activeCellEdit.form.controls[activeCellEdit.propertyName]?.value ?? null;
        if (!activeCellEdit.isValid || this.areJsonValuesEqual(activeCellEdit.originalValue, value)) {
            this.activeCellEdit.set(null);
            return;
        }
        this.activeCellEdit.set({
            ...activeCellEdit,
            isCommitting: true
        });
        this.cellEditCommit.emit({
            entity: activeCellEdit.entity,
            propertyName: activeCellEdit.propertyName,
            value,
            dirtyProperties: this.readDirtyControlNames(activeCellEdit.form)
        });
    }
    /**
     * Extracts per-field validation errors from the row chill state in a template-friendly shape.
     */
    rowFieldErrors(entity) {
        const crudState = this.readChillState(entity);
        if (!crudState || typeof crudState !== 'object' || Array.isArray(crudState)) {
            return {};
        }
        const validationErrors = crudState['validationErrors'];
        if (!validationErrors || typeof validationErrors !== 'object' || Array.isArray(validationErrors)) {
            return {};
        }
        const nextErrors = {};
        for (const [fieldName, value] of Object.entries(validationErrors)) {
            if (typeof value === 'string' && value.trim().length > 0) {
                nextErrors[fieldName] = value;
            }
        }
        return nextErrors;
    }
    /**
     * Detects either field-level or generic validation errors stored in the row chill state.
     */
    rowHasValidationErrors(entity) {
        if (Object.keys(this.rowFieldErrors(entity)).length > 0) {
            return true;
        }
        const crudState = this.readChillState(entity);
        if (!crudState || typeof crudState !== 'object' || Array.isArray(crudState)) {
            return false;
        }
        const genericErrors = crudState['genericErrors'];
        return Array.isArray(genericErrors)
            && genericErrors.some((message) => typeof message === 'string' && message.trim().length > 0);
    }
    // #endregion
    // #region Helper Methods
    /**
     * Keeps live entity subscriptions aligned with the current schema type and visible entity set.
     */
    syncEntityNotificationSubscriptions(schema, entities) {
        const chillType = schema?.chillType?.trim() ?? '';
        if (!chillType) {
            this.clearEntityNotificationSubscriptions();
            return;
        }
        if (this.subscribedNotificationChillType && this.subscribedNotificationChillType !== chillType) {
            this.clearEntityNotificationSubscriptions();
        }
        this.subscribedNotificationChillType = chillType;
        const targetGuids = new Set(entities
            .map((entity) => this.readEntityGuid(entity))
            .filter((guid) => guid.length > 0));
        for (const [guid, subscription] of this.entityNotificationSubscriptions.entries()) {
            if (targetGuids.has(guid)) {
                continue;
            }
            subscription.unsubscribe();
            this.entityNotificationSubscriptions.delete(guid);
        }
        for (const guid of targetGuids) {
            if (this.entityNotificationSubscriptions.has(guid)) {
                continue;
            }
            const subscription = this.chill.watchEntityChanges(chillType, guid).subscribe({
                next: (changes) => {
                    void this.handleEntityNotifications(chillType, changes);
                },
                error: () => {
                    const currentSubscription = this.entityNotificationSubscriptions.get(guid);
                    currentSubscription?.unsubscribe();
                    this.entityNotificationSubscriptions.delete(guid);
                }
            });
            this.entityNotificationSubscriptions.set(guid, subscription);
        }
    }
    /**
     * Unsubscribes from all live entity notifications and clears the associated bookkeeping.
     */
    clearEntityNotificationSubscriptions() {
        for (const subscription of this.entityNotificationSubscriptions.values()) {
            subscription.unsubscribe();
        }
        this.entityNotificationSubscriptions.clear();
        this.subscribedNotificationChillType = '';
    }
    clearRowRefreshFlashTimers() {
        for (const timer of this.rowRefreshFlashTimers.values()) {
            clearTimeout(timer);
        }
        this.rowRefreshFlashTimers.clear();
        this.rowRefreshFlashKeys.set(new Set());
    }
    pinnedColumnWidthRem() {
        return (this.hasSelectionColumn() ? 3 : 0) + (this.hasActionColumn() ? 3 : 0);
    }
    /**
     * Refreshes locally displayed rows only for remote update notifications that contain a Guid.
     */
    async handleEntityNotifications(chillType, changes) {
        for (const change of changes) {
            if (change.action !== 'UPDATED') {
                continue;
            }
            const guid = change.guid?.trim();
            if (!guid) {
                continue;
            }
            await this.refreshDisplayedEntity(chillType, guid);
        }
    }
    /**
     * Reloads a row from the server, merges remote changes into non-dirty fields, and warns on conflicts.
     */
    async refreshDisplayedEntity(chillType, guid) {
        const schema = this.schema();
        if (!schema) {
            return;
        }
        const currentEntity = this.displayedEntities().find((entity) => this.sameEntityGuid(entity, guid));
        if (!currentEntity || this.isNewEntity(currentEntity) || this.isDeletedRow(currentEntity)) {
            return;
        }
        if (this.shouldIgnoreEntityNotification(currentEntity)) {
            return;
        }
        try {
            const latestEntityResponse = await firstValueFrom(this.chill.find(this.buildRefreshFindRequest(chillType, guid, schema)));
            if (!latestEntityResponse) {
                return;
            }
            const latestEntity = this.normalizeServerEntity(this.prepareEntityForSchema(latestEntityResponse, schema));
            const currentState = this.readCrudStateObject(currentEntity);
            if (currentState.status === 'pristine') {
                this.replaceDisplayedEntity(latestEntity, currentEntity);
                this.flashRefreshedRow(currentEntity);
                return;
            }
            const dirtyProperties = new Set(currentState.dirtyProperties ?? []);
            const nextProperties = {
                ...(currentEntity.properties ?? {})
            };
            const conflictingProperties = [];
            for (const property of schema.properties ?? []) {
                const propertyName = property.name;
                const remoteValue = latestEntity.properties?.[propertyName];
                const localValue = currentEntity.properties?.[propertyName];
                if (dirtyProperties.has(propertyName)) {
                    if (!this.areJsonValuesEqual(remoteValue, localValue)) {
                        conflictingProperties.push(propertyName);
                    }
                    continue;
                }
                nextProperties[propertyName] = remoteValue ?? null;
            }
            const mergedEntity = this.withCrudState({
                ...currentEntity,
                ...latestEntity,
                properties: nextProperties
            }, {
                ...currentState
            });
            this.replaceDisplayedEntity(mergedEntity, currentEntity);
            if (conflictingProperties.length > 0) {
                await this.dialog?.confirmOk(this.chill.T('43B7D65E-61B6-4D20-9BEE-EA9E8467AA12', 'Entity updated remotely', 'Entita aggiornata da remoto'), this.chill.T('490D8729-8B0B-4D25-9661-F763FEC35C42', `Remote updates also changed dirty properties: ${conflictingProperties.join(', ')}`, `L'aggiornamento remoto ha modificato anche proprieta dirty: ${conflictingProperties.join(', ')}`));
            }
        }
        catch {
            return;
        }
    }
    /**
     * Persists the current column layout into schema metadata and updates local state with the saved result.
     */
    saveLayout() {
        const schema = this.schema();
        if (!schema) {
            this.isEditLayoutMode.set(false);
            return;
        }
        const normalizedLayoutState = this.normalizeLayoutForSave(this.layoutState());
        const metadata = this.readSchemaMetadata(schema);
        metadata[TABLE_LAYOUT_METADATA_KEY] = JSON.stringify({
            columns: normalizedLayoutState.map((column) => ({
                name: column.name,
                displayName: column.displayName,
                hidden: column.hidden
            }))
        });
        const updatedSchema = {
            ...schema,
            properties: this.applyPropertyWidthProportions(schema, normalizedLayoutState),
            metadata
        };
        this.isSavingLayout.set(true);
        this.layoutError.set('');
        this.chill.setSchema(updatedSchema).subscribe({
            next: (savedSchema) => {
                const effectiveSchema = savedSchema ?? updatedSchema;
                const targetSchema = this.schema();
                if (targetSchema) {
                    targetSchema.metadata = this.readSchemaMetadata(effectiveSchema);
                    targetSchema.properties = [...(effectiveSchema.properties ?? [])];
                    delete targetSchema['Metadata'];
                    delete targetSchema['Properties'];
                }
                this.layoutState.set(normalizedLayoutState);
                this.layoutState.set(this.readLayoutState(effectiveSchema));
                this.isSavingLayout.set(false);
                this.isEditLayoutMode.set(false);
            },
            error: (error) => {
                this.layoutError.set(this.chill.formatError(error));
                this.isSavingLayout.set(false);
            }
        });
    }
    /**
     * Reads persisted column layout from schema metadata and falls back to schema order when unavailable.
     */
    readLayoutState(schema) {
        const properties = schema?.properties ?? [];
        const defaultLayout = properties.map((property) => ({
            name: property.name,
            displayName: property.displayName || property.name,
            hidden: false,
            widthProportion: this.readPropertyWidthProportion(property)
        }));
        const metadata = this.readSchemaMetadata(schema);
        const rawLayoutValue = metadata[TABLE_LAYOUT_METADATA_KEY];
        const rawLayout = typeof rawLayoutValue === 'string' ? rawLayoutValue.trim() : '';
        if (!rawLayout) {
            return defaultLayout;
        }
        try {
            const parsedLayout = JSON.parse(rawLayout);
            const savedColumns = Array.isArray(parsedLayout.columns)
                ? parsedLayout.columns
                    .filter((item) => typeof item?.name === 'string')
                    .map((item) => ({
                    name: item.name.trim(),
                    displayName: typeof item.displayName === 'string' ? item.displayName : '',
                    hidden: item.hidden === true
                }))
                    .filter((item) => item.name.length > 0)
                : [];
            const restoredLayout = defaultLayout.map((column) => {
                const savedColumn = savedColumns.find((item) => item.name === column.name);
                return savedColumn
                    ? { ...column, ...savedColumn }
                    : column;
            })
                .sort((left, right) => {
                const leftIndex = savedColumns.findIndex((item) => item.name === left.name);
                const rightIndex = savedColumns.findIndex((item) => item.name === right.name);
                const resolvedLeftIndex = leftIndex >= 0 ? leftIndex : Number.MAX_SAFE_INTEGER;
                const resolvedRightIndex = rightIndex >= 0 ? rightIndex : Number.MAX_SAFE_INTEGER;
                return resolvedLeftIndex - resolvedRightIndex;
            });
            return restoredLayout;
        }
        catch {
            return defaultLayout;
        }
    }
    isMonacoEditorEventTarget(target) {
        return target instanceof HTMLElement
            && !!target.closest('.editor-field, .monaco-editor');
    }
    /**
     * Stores visible columns before hidden ones so the persisted layout can be rendered directly.
     */
    normalizeLayoutForSave(layoutState) {
        const visibleColumns = layoutState.filter((column) => !column.hidden);
        const hiddenColumns = layoutState.filter((column) => column.hidden);
        return [...visibleColumns, ...hiddenColumns].map((column) => ({
            ...column,
            widthProportion: this.normalizeColumnWidthProportion(column.widthProportion)
        }));
    }
    applyPropertyWidthProportions(schema, layoutState) {
        const widthsByPropertyName = new Map(layoutState.map((column) => [
            column.name,
            this.normalizeColumnWidthProportion(column.widthProportion)
        ]));
        return (schema.properties ?? []).map((property) => {
            const widthProportion = widthsByPropertyName.get(property.name);
            if (!widthProportion) {
                return property;
            }
            return {
                ...property,
                metadata: {
                    ...(property.metadata ?? {}),
                    [TABLE_COLUMN_WIDTH_METADATA_KEY]: widthProportion
                }
            };
        });
    }
    readPropertyWidthProportion(property) {
        return this.normalizeColumnWidthProportion(property.metadata?.[TABLE_COLUMN_WIDTH_METADATA_KEY]);
    }
    normalizeColumnWidthProportion(value) {
        const numericValue = typeof value === 'number'
            ? value
            : typeof value === 'string'
                ? Number.parseFloat(value)
                : 1;
        if (!Number.isFinite(numericValue)) {
            return 1;
        }
        return this.columnWidthOptions.reduce((closest, option) => Math.abs(option - numericValue) < Math.abs(closest - numericValue) ? option : closest, 1);
    }
    findColumnWidthOptionIndex(value) {
        const normalizedValue = this.normalizeColumnWidthProportion(value);
        const index = this.columnWidthOptions.findIndex((option) => option === normalizedValue);
        return index >= 0 ? index : this.columnWidthOptions.indexOf(1);
    }
    /**
     * Normalizes schema metadata from camelCase or legacy payload shapes into a mutable string map.
     */
    readSchemaMetadata(schema) {
        if (!schema) {
            return {};
        }
        const camelMetadata = schema.metadata;
        if (camelMetadata) {
            return { ...camelMetadata };
        }
        const pascalMetadata = schema['Metadata'];
        if (pascalMetadata && typeof pascalMetadata === 'object' && !Array.isArray(pascalMetadata)) {
            return Object.fromEntries(Object.entries(pascalMetadata).map(([key, value]) => [key, typeof value === 'string' ? value : String(value ?? '')]));
        }
        return {};
    }
    /**
     * Reads a property from the entity bag first, then from direct camelCase or PascalCase fields.
     */
    readPropertyValue(entity, propertyName) {
        const properties = entity.properties
            ?? (this.isJsonObjectRecord(entity['Properties']) ? entity['Properties'] : undefined);
        if (properties && propertyName in properties) {
            return properties[propertyName];
        }
        return entity[propertyName] ?? entity[this.toPascalCase(propertyName)];
    }
    /**
     * Converts primitive entity properties into trimmed text for keys such as Guid or Label.
     */
    readEntityText(entity, key) {
        const value = entity[key];
        if (typeof value === 'string' && value.trim()) {
            return value.trim();
        }
        if (typeof value === 'number' || typeof value === 'boolean') {
            return String(value);
        }
        return null;
    }
    /**
     * Returns the normalized lowercase CRUD status used by row rendering logic.
     */
    readCrudState(entity) {
        const status = this.readCrudStateObject(entity).status;
        return typeof status === 'string'
            ? status.trim().toLowerCase()
            : '';
    }
    /**
     * Type guard for JSON object records.
     */
    isJsonObjectRecord(value) {
        return !!value && typeof value === 'object' && !Array.isArray(value);
    }
    /**
     * Returns the raw chill state payload attached to the entity.
     */
    readChillState(entity) {
        return entity['chillState'];
    }
    /**
     * Normalizes chill state into a predictable CRUD model with defaults for new and deleting rows.
     */
    readCrudStateObject(entity) {
        const currentState = this.readChillState(entity);
        const isNew = this.readChillStateBoolean(entity, 'isNew');
        const isDeleting = this.readChillStateBoolean(entity, 'isDeleting');
        if (currentState && typeof currentState === 'object' && !Array.isArray(currentState)) {
            const typedState = currentState;
            const resolvedIsNew = typedState['isNew'] === true || isNew;
            const status = typeof typedState['status'] === 'string'
                ? typedState['status']
                : (resolvedIsNew ? 'draft' : isDeleting ? 'deleted' : 'pristine');
            return {
                ...typedState,
                isNew: resolvedIsNew,
                status,
                dirtyProperties: Array.isArray(typedState['dirtyProperties'])
                    ? typedState['dirtyProperties'].filter((propertyName) => typeof propertyName === 'string' && propertyName.trim().length > 0)
                    : null
            };
        }
        return {
            isNew,
            status: isNew ? 'draft' : isDeleting ? 'deleted' : 'pristine',
            dirtyProperties: isNew ? [] : null
        };
    }
    /**
     * Merges a CRUD-state patch onto the entity while keeping derived `isNew` and `isDeleting` flags consistent.
     */
    withCrudState(entity, state) {
        const nextState = this.sanitizeCrudState({
            ...this.readCrudStateObject(entity),
            ...state
        });
        return {
            ...entity,
            chillState: {
                ...(this.readChillState(entity) && typeof this.readChillState(entity) === 'object' && !Array.isArray(this.readChillState(entity))
                    ? this.readChillState(entity)
                    : {}),
                ...nextState,
                isNew: nextState['isNew'] === true,
                isDeleting: nextState['status'] === 'deleted'
            }
        };
    }
    /**
     * Removes undefined entries before persisting CRUD state back onto the entity payload.
     */
    sanitizeCrudState(state) {
        return Object.fromEntries(Object.entries(state).filter(([, value]) => value !== undefined));
    }
    /**
     * Resets a freshly loaded server entity back to a pristine local CRUD state.
     */
    normalizeServerEntity(entity) {
        return this.withCrudState(entity, {
            isNew: false,
            status: 'pristine',
            dirtyProperties: null,
            validationErrors: null,
            genericErrors: null,
            errorMessage: null,
            ignoreNotificationsUntil: null
        });
    }
    savePropertySchema(schema, originalPropertyName, property) {
        const normalizedLayoutState = this.normalizeLayoutForSave(this.layoutState());
        const metadata = this.readSchemaMetadata(schema);
        metadata[TABLE_LAYOUT_METADATA_KEY] = JSON.stringify({
            columns: normalizedLayoutState.map((column) => ({
                name: column.name,
                displayName: column.displayName,
                hidden: column.hidden
            }))
        });
        const schemaWithUpdatedProperty = {
            ...schema,
            properties: (schema.properties ?? []).map((candidate) => candidate.name === originalPropertyName
                ? property
                : candidate),
            metadata
        };
        const updatedSchema = {
            ...schemaWithUpdatedProperty,
            properties: this.applyPropertyWidthProportions(schemaWithUpdatedProperty, normalizedLayoutState)
        };
        this.isSavingLayout.set(true);
        this.layoutError.set('');
        this.chill.setSchema(updatedSchema).subscribe({
            next: (savedSchema) => {
                const effectiveSchema = savedSchema ?? updatedSchema;
                const targetSchema = this.schema();
                if (targetSchema) {
                    targetSchema.metadata = this.readSchemaMetadata(effectiveSchema);
                    targetSchema.properties = [...(effectiveSchema.properties ?? [])];
                    delete targetSchema['Metadata'];
                    delete targetSchema['Properties'];
                }
                this.activeCellEdit.set(null);
                this.layoutState.set(this.readLayoutState(effectiveSchema));
                this.schemaRefreshTick.update((current) => current + 1);
                this.isSavingLayout.set(false);
            },
            error: (error) => {
                this.layoutError.set(this.chill.formatError(error));
                this.isSavingLayout.set(false);
            }
        });
    }
    applyUpdatedSchema(targetSchema, updatedSchema) {
        targetSchema.metadata = this.readSchemaMetadata(updatedSchema);
        targetSchema.properties = [...(updatedSchema.properties ?? [])];
        targetSchema.displayName = updatedSchema.displayName ?? targetSchema.displayName;
        targetSchema.handleAttachments = updatedSchema.handleAttachments;
        targetSchema.enableMCP = updatedSchema.enableMCP;
        targetSchema.mcpDescription = updatedSchema.mcpDescription ?? null;
        targetSchema.queryRelatedChillType = updatedSchema.queryRelatedChillType;
        delete targetSchema['Metadata'];
        delete targetSchema['Properties'];
        this.activeCellEdit.set(null);
        this.layoutState.set(this.readLayoutState(targetSchema));
        this.schemaRefreshTick.update((current) => current + 1);
        this.schemaUpdated.emit(targetSchema);
    }
    /**
     * Skips live refreshes for a short window after a local save so the row keeps the just-returned server copy.
     */
    shouldIgnoreEntityNotification(entity) {
        const ignoreUntil = this.readCrudStateObject(entity)['ignoreNotificationsUntil'];
        return typeof ignoreUntil === 'number' && Number.isFinite(ignoreUntil) && ignoreUntil > Date.now();
    }
    buildRefreshFindRequest(chillType, guid, schema) {
        return {
            chillType,
            guid,
            properties: this.buildTablePropertyRequest(schema)
        };
    }
    buildTablePropertyRequest(schema) {
        const tablePropertyNames = this.columns()
            .map((column) => column.name?.trim() ?? '')
            .filter((propertyName) => propertyName.length > 0);
        const fallbackPropertyNames = (schema.properties ?? [])
            .map((property) => property.name?.trim() ?? '')
            .filter((propertyName) => propertyName.length > 0);
        const propertyNames = tablePropertyNames.length > 0
            ? tablePropertyNames
            : fallbackPropertyNames;
        return Object.fromEntries([...new Set(propertyNames)].map((propertyName) => [propertyName, null]));
    }
    /**
     * Ensures a server entity exposes every schema property through the `properties` bag expected by the table.
     */
    prepareEntityForSchema(entity, schema) {
        const nextProperties = {
            ...(entity.properties ?? {})
        };
        for (const property of schema.properties ?? []) {
            if (property.name in nextProperties) {
                continue;
            }
            nextProperties[property.name] = this.readPropertyValue(entity, property.name) ?? null;
        }
        return {
            ...entity,
            chillType: this.readStringValue(entity['chillType']) || schema.chillType?.trim() || '',
            properties: nextProperties
        };
    }
    /**
     * Replaces a row in the displayed collection and pushes fresh values into any active editor for that row.
     */
    replaceDisplayedEntity(nextEntity, previousEntity) {
        const previousEntityKey = this.trackByEntity(0, previousEntity);
        this.displayedEntities.update((current) => current.map((entity) => this.trackByEntity(0, entity) === previousEntityKey ? nextEntity : entity));
        const activeCellEdit = this.activeCellEdit();
        if (!activeCellEdit || activeCellEdit.entityKey !== previousEntityKey) {
            return;
        }
        for (const [propertyName, control] of Object.entries(activeCellEdit.form.controls)) {
            const nextValue = this.readPropertyValue(nextEntity, propertyName) ?? null;
            if (this.areJsonValuesEqual(control.value, nextValue)) {
                continue;
            }
            control.setValue(nextValue);
        }
        this.activeCellEdit.set({
            ...activeCellEdit,
            entity: nextEntity
        });
    }
    flashRefreshedRow(entity) {
        const entityKey = this.trackByEntity(0, entity);
        if (!entityKey) {
            return;
        }
        const currentTimer = this.rowRefreshFlashTimers.get(entityKey);
        if (currentTimer) {
            clearTimeout(currentTimer);
        }
        this.rowRefreshFlashKeys.update((current) => new Set([...current, entityKey]));
        const timer = setTimeout(() => {
            this.rowRefreshFlashTimers.delete(entityKey);
            this.rowRefreshFlashKeys.update((current) => {
                const next = new Set(current);
                next.delete(entityKey);
                return next;
            });
        }, ROW_REFRESH_FLASH_MS);
        this.rowRefreshFlashTimers.set(entityKey, timer);
    }
    /**
     * Uses normalized CRUD state to detect client-side draft rows.
     */
    isNewEntity(entity) {
        return this.readCrudStateObject(entity).isNew === true;
    }
    /**
     * Collects the property names whose form controls are currently dirty.
     */
    readDirtyControlNames(form) {
        return Object.entries(form.controls)
            .filter(([, control]) => control.dirty)
            .map(([propertyName]) => propertyName.trim())
            .filter((propertyName) => propertyName.length > 0);
    }
    /**
     * Reads the row Guid using either camelCase or PascalCase server field names.
     */
    readEntityGuid(entity) {
        return this.readEntityText(entity, 'guid')
            ?? this.readEntityText(entity, 'Guid')
            ?? '';
    }
    /**
     * Normalizes a JSON value into trimmed text when it is already a string.
     */
    readStringValue(value) {
        return typeof value === 'string'
            ? value.trim()
            : '';
    }
    /**
     * Reads a boolean flag from the raw chill state object.
     */
    readChillStateBoolean(entity, propertyName) {
        const chillState = this.readChillState(entity);
        if (!chillState || typeof chillState !== 'object' || Array.isArray(chillState)) {
            return false;
        }
        return chillState[propertyName] === true;
    }
    /**
     * Compares an entity Guid with an incoming Guid after trimming the incoming value.
     */
    sameEntityGuid(entity, guid) {
        return this.readEntityGuid(entity) === guid.trim();
    }
    /**
     * Converts a property name to PascalCase for payloads that expose both casing styles.
     */
    toPascalCase(value) {
        return value.length > 0
            ? `${value[0].toUpperCase()}${value.slice(1)}`
            : value;
    }
    /**
     * Uses JSON serialization as a pragmatic deep-equality check for editor values and server payloads.
     */
    areJsonValuesEqual(left, right) {
        return JSON.stringify(left ?? null) === JSON.stringify(right ?? null);
    }
    /**
     * Computes a viewport-clamped fixed position for the row-action menu.
     */
    buildRowActionMenuState(entityKey, trigger) {
        const rect = trigger.getBoundingClientRect();
        const menuWidth = 176;
        const menuHeight = 112;
        const viewportPadding = 8;
        const preferredLeft = rect.right - menuWidth;
        const maxLeft = window.innerWidth - menuWidth - viewportPadding;
        const left = Math.max(viewportPadding, Math.min(preferredLeft, maxLeft));
        const fitsBelow = rect.bottom + 6 + menuHeight <= window.innerHeight - viewportPadding;
        const top = fitsBelow
            ? rect.bottom + 6
            : Math.max(viewportPadding, rect.top - menuHeight - 6);
        return { entityKey, top, left };
    }
    static { this.ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: ChillTableComponent, deps: [], target: i0.ɵɵFactoryTarget.Component }); }
    static { this.ɵcmp = i0.ɵɵngDeclareComponent({ minVersion: "17.0.0", version: "19.2.21", type: ChillTableComponent, isStandalone: true, selector: "app-chill-table", inputs: { schema: { classPropertyName: "schema", publicName: "schema", isSignal: true, isRequired: false, transformFunction: null }, entities: { classPropertyName: "entities", publicName: "entities", isSignal: true, isRequired: false, transformFunction: null }, selectionColumn: { classPropertyName: "selectionColumn", publicName: "selectionColumn", isSignal: true, isRequired: false, transformFunction: null }, rowAction: { classPropertyName: "rowAction", publicName: "rowAction", isSignal: true, isRequired: false, transformFunction: null }, rowActions: { classPropertyName: "rowActions", publicName: "rowActions", isSignal: true, isRequired: false, transformFunction: null }, enableInlineEditing: { classPropertyName: "enableInlineEditing", publicName: "enableInlineEditing", isSignal: true, isRequired: false, transformFunction: null }, readonlyPropertyNames: { classPropertyName: "readonlyPropertyNames", publicName: "readonlyPropertyNames", isSignal: true, isRequired: false, transformFunction: null }, validationFocus: { classPropertyName: "validationFocus", publicName: "validationFocus", isSignal: true, isRequired: false, transformFunction: null }, showSchemaHeader: { classPropertyName: "showSchemaHeader", publicName: "showSchemaHeader", isSignal: true, isRequired: false, transformFunction: null }, ordering: { classPropertyName: "ordering", publicName: "ordering", isSignal: true, isRequired: false, transformFunction: null }, enableFullTextSearch: { classPropertyName: "enableFullTextSearch", publicName: "enableFullTextSearch", isSignal: true, isRequired: false, transformFunction: null }, fullTextSearch: { classPropertyName: "fullTextSearch", publicName: "fullTextSearch", isSignal: true, isRequired: false, transformFunction: null }, showMobileTaskClose: { classPropertyName: "showMobileTaskClose", publicName: "showMobileTaskClose", isSignal: true, isRequired: false, transformFunction: null } }, outputs: { cellEditCommit: "cellEditCommit", sortChange: "sortChange", fullTextSearchChange: "fullTextSearchChange", schemaUpdated: "schemaUpdated", mobileTaskClose: "mobileTaskClose" }, host: { listeners: { "document:click": "handleDocumentClick()", "window:resize": "handleWindowResize()", "window:scroll": "handleWindowScroll()" } }, viewQueries: [{ propertyName: "fullTextSearchInput", first: true, predicate: ["fullTextSearchInput"], descendants: true, isSignal: true }], ngImport: i0, template: "<section class=\"chill-table-shell\">\n  @if (showSchemaHeader() && schema()?.displayName) {\n    <header class=\"table-header\">\n      <div class=\"table-header__title-row\">\n        <div class=\"table-header__title\">\n          <p class=\"table-kicker\"><app-chill-i18n-label [labelGuid]=\"'2DDE962B-086C-47B1-8A48-B16F0E34C0A3'\" [primaryDefaultText]=\"'Chill schema'\" [secondaryDefaultText]=\"'Schema Chill'\" /></p>\n          <h2>{{ schema()?.displayName }}</h2>\n        </div>\n\n        <div class=\"table-header__mobile-controls\">\n          @if (enableFullTextSearch()) {\n            <button\n              type=\"button\"\n              class=\"table-search__icon-button table-search__icon-button--mobile\"\n              (click)=\"toggleFullTextSearch()\"\n              [attr.aria-label]=\"chill.T('54E62296-DB94-4973-A2AE-49902D5E25E5', 'Full text search', 'Ricerca full text')\">\n              <span class=\"material-symbol-icon\" aria-hidden=\"true\">search</span>\n            </button>\n          }\n\n          @if (showMobileTaskClose()) {\n            <button\n              type=\"button\"\n              class=\"table-header__task-close\"\n              (click)=\"closeMobileTask()\"\n              [attr.aria-label]=\"chill.T('D2566993-8138-408A-9153-904454528781', 'Close task', 'Chiudi attivita')\">\n              <span class=\"material-symbol-icon\" aria-hidden=\"true\">close</span>\n            </button>\n          }\n        </div>\n      </div>\n\n      @if (enableFullTextSearch()) {\n        <div class=\"table-search\" [class.is-open]=\"isFullTextSearchOpen()\">\n          @if (isFullTextSearchOpen()) {\n            <input\n              #fullTextSearchInput\n              type=\"search\"\n              class=\"table-search__input\"\n              [ngModel]=\"fullTextSearchText()\"\n              (ngModelChange)=\"updateFullTextSearchText($event)\"\n              (keydown.enter)=\"submitFullTextSearch()\"\n              (keydown.escape)=\"resetFullTextSearch()\"\n              [placeholder]=\"chill.T('D513421E-1C00-425E-A89B-E736A440474F', 'Search', 'Cerca')\"\n              [attr.aria-label]=\"chill.T('54E62296-DB94-4973-A2AE-49902D5E25E5', 'Full text search', 'Ricerca full text')\" />\n            <button\n              type=\"button\"\n              class=\"table-search__icon-button\"\n              (click)=\"resetFullTextSearch()\"\n              [attr.aria-label]=\"chill.T('34015BA4-E0CA-460E-B82B-A4E2D4D8A184', 'Clear', 'Pulisci')\">\n              <span class=\"material-symbol-icon\" aria-hidden=\"true\">close</span>\n            </button>\n          } @else {\n            <button\n              type=\"button\"\n              class=\"table-search__icon-button\"\n              (click)=\"toggleFullTextSearch()\"\n              [attr.aria-label]=\"chill.T('54E62296-DB94-4973-A2AE-49902D5E25E5', 'Full text search', 'Ricerca full text')\">\n              <span class=\"material-symbol-icon\" aria-hidden=\"true\">search</span>\n            </button>\n          }\n        </div>\n      }\n    </header>\n  }\n\n  @if (layout.isLayoutEditingEnabled()) {\n    <div class=\"table-actions\">\n      @if (isEditLayoutMode() && hiddenColumns().length > 0) {\n        <label class=\"hidden-column-picker\">\n          <span>{{ chill.T('3758A6E6-85C1-481D-BF57-A531E79661B8', 'Show column', 'Mostra colonna') }}</span>\n          <select #hiddenColumnSelect (change)=\"revealColumn(hiddenColumnSelect.value); hiddenColumnSelect.value = ''\">\n            <option value=\"\">{{ chill.T('D1A15B17-B3C2-49A8-90AA-BAE81ECA8D69', 'Select hidden column', 'Seleziona colonna nascosta') }}</option>\n            @for (column of hiddenColumns(); track column.name) {\n              <option [value]=\"column.name\">{{ column.displayName || column.name }}</option>\n            }\n          </select>\n        </label>\n      }\n      <button type=\"button\" class=\"layout-button\" (click)=\"toggleEditLayoutMode()\" [disabled]=\"isSavingLayout()\">\n        @if (isSavingLayout()) {\n          <app-chill-i18n-button-label [labelGuid]=\"'B46C82D8-443E-45DE-8D49-C270656B511E'\" [primaryDefaultText]=\"'Saving layout...'\" [secondaryDefaultText]=\"'Salvataggio layout...'\" />\n        } @else if (isEditLayoutMode()) {\n          <app-chill-i18n-button-label [labelGuid]=\"'7C681F13-4245-447F-B66A-D9A3A500D322'\" [primaryDefaultText]=\"'Done'\" [secondaryDefaultText]=\"'Fine'\" />\n        } @else {\n          <app-chill-i18n-button-label [labelGuid]=\"'9D0B43AC-47B4-4C63-98C7-DB0118C9C0CF'\" [primaryDefaultText]=\"'Edit layout mode'\" [secondaryDefaultText]=\"'Modalita modifica layout'\" />\n        }\n      </button>\n      @if (isEditLayoutMode()) {\n        <button type=\"button\" class=\"layout-button\" (click)=\"refreshSchemaFromModel()\" [disabled]=\"isSavingLayout()\">\n          @if (isRefreshingSchema()) {\n            <app-chill-i18n-button-label [labelGuid]=\"'8628775B-B831-44F2-8A38-909F40E2F7B3'\" [primaryDefaultText]=\"'Updating schema...'\" [secondaryDefaultText]=\"'Aggiornamento schema...'\" />\n          } @else {\n            <app-chill-i18n-button-label [labelGuid]=\"'62953302-B951-4FD1-BD08-4B7649A91BAF'\" [primaryDefaultText]=\"'Update'\" [secondaryDefaultText]=\"'Aggiorna'\" />\n          }\n        </button>\n      }\n    </div>\n  }\n\n  @if (layoutError()) {\n    <div class=\"empty-state error-state\">{{ layoutError() }}</div>\n  }\n\n  @if (visibleColumns().length === 0) {\n    <div class=\"empty-state\">{{ chill.T('3D7ABF07-6E0C-4F99-A0C8-C936C44322C6', 'No schema properties available.', 'Nessuna propriet\u00E0 di schema disponibile.') }}</div>\n  } @else {\n    <div class=\"table-wrap\" [class.is-edit-layout-mode]=\"isEditLayoutMode()\">\n      <table\n        class=\"chill-table\"\n        [class.has-selection-column]=\"hasSelectionColumn()\"\n        [class.is-edit-layout-mode]=\"isEditLayoutMode()\"\n        [class.has-inline-cell-edit]=\"activeCellEdit() !== null\"\n        [style.--chill-table-min-width]=\"tableMinimumWidth()\"\n        [style.--chill-table-mobile-min-width]=\"mobileTableMinimumWidth()\">\n        <colgroup>\n          @if (hasSelectionColumn()) {\n            <col class=\"selection-column\" />\n          }\n          @if (hasActionColumn()) {\n            <col class=\"action-column\" />\n          }\n          @for (column of visibleColumns(); track column.name) {\n            <col class=\"data-column\" [style.width.%]=\"isColumnEditing(column) ? null : columnWidthPercent(column)\" />\n          }\n        </colgroup>\n        <thead>\n          <tr>\n            @if (hasSelectionColumn()) {\n              <th scope=\"col\" class=\"selection-column\"></th>\n            }\n            @if (hasActionColumn()) {\n              <th scope=\"col\" class=\"action-column\">#</th>\n            }\n            @for (column of visibleColumns(); track column.name) {\n              <th\n                scope=\"col\"\n                [draggable]=\"isEditLayoutMode()\"\n                (dragstart)=\"beginDrag($event, column.name)\"\n                (dragover)=\"allowDrop($event)\"\n                (drop)=\"dropColumn(column.name)\"\n                (dragend)=\"endDrag()\"\n                [class.is-sortable]=\"!isEditLayoutMode() && canSortColumn(column)\"\n                [class.is-sorted]=\"sortDirectionFor(column) !== null\"\n                (click)=\"!isEditLayoutMode() && canSortColumn(column) && toggleColumnSort(column)\">\n                @if (isEditLayoutMode()) {\n                  <div class=\"column-editor\" (click)=\"$event.stopPropagation()\">\n                    <div class=\"column-editor__title-row\">\n                      <input\n                        type=\"text\"\n                        class=\"column-name-input\"\n                        [ngModel]=\"column.displayName\"\n                        (ngModelChange)=\"updateColumnDisplayName(column.name, $event)\"\n                        [name]=\"'displayName-' + column.name\" />\n                      <input\n                        type=\"checkbox\"\n                        [checked]=\"!column.hidden\"\n                        (change)=\"updateColumnHidden(column.name, !$any($event.target).checked)\"\n                        [name]=\"'hidden-' + column.name\"\n                        [attr.aria-label]=\"chill.T('C6610992-9D61-4EBA-8D13-92F34521AB64', 'Show column', 'Mostra colonna')\" />\n                    </div>\n                    <div class=\"column-editor__settings-row\">\n                      <button\n                        type=\"button\"\n                        class=\"column-icon-button\"\n                        (click)=\"openPropertySettings(column)\"\n                        [attr.aria-label]=\"chill.T('40E9838A-70F2-4E89-BE38-BBE44027D253', 'Edit property settings', 'Modifica impostazioni propriet\u00E0')\">\n                        <span class=\"material-symbol-icon\">tune</span>\n                      </button>\n                      <select\n                        class=\"property-type-select\"\n                        [ngModel]=\"column.propertyType\"\n                        (ngModelChange)=\"updatePropertyType(column, $event)\"\n                        [name]=\"'propertyType-' + column.name\"\n                        [attr.aria-label]=\"chill.T('A1803D67-C40D-41AF-BFD2-8F9B0E34C48B', 'Property type', 'Tipo propriet\u00E0')\">\n                        @for (option of propertyTypeOptions; track option.value) {\n                          <option [ngValue]=\"option.value\" [disabled]=\"isPropertyTypeOptionDisabled(column, option.value)\">{{ option.label }}</option>\n                        }\n                      </select>\n                      <div class=\"column-width-controls\" [attr.aria-label]=\"chill.T('53796BE9-AF5C-487D-9B26-468C1B75FE54', 'Column width proportion', 'Proporzione larghezza colonna')\">\n                        <button\n                          type=\"button\"\n                          class=\"column-icon-button\"\n                          (click)=\"updateColumnWidthProportion(column.name, -1)\"\n                          [disabled]=\"!canDecreaseColumnWidth(column)\"\n                          [attr.aria-label]=\"chill.T('8C3198E8-F1CA-4B1B-A91B-A02B48FF9219', 'Decrease column width', 'Riduci larghezza colonna')\">\n                          <span class=\"material-symbol-icon\" aria-hidden=\"true\">remove</span>\n                        </button>\n                        <span class=\"column-width-value\">{{ columnWidthLabel(column) }}</span>\n                        <button\n                          type=\"button\"\n                          class=\"column-icon-button\"\n                          (click)=\"updateColumnWidthProportion(column.name, 1)\"\n                          [disabled]=\"!canIncreaseColumnWidth(column)\"\n                          [attr.aria-label]=\"chill.T('30EA851D-40AB-4AD2-A4C8-D2E77C5162BB', 'Increase column width', 'Aumenta larghezza colonna')\">\n                          <span class=\"material-symbol-icon\" aria-hidden=\"true\">add</span>\n                        </button>\n                      </div>\n                    </div>\n                  </div>\n                } @else {\n                  <span>{{ column.displayName || column.name }}</span>\n                  @if (canSortColumn(column)) {\n                    <span class=\"column-sort-indicator\" aria-hidden=\"true\">\n                      @switch (sortDirectionFor(column)) {\n                        @case ('ASC') { \u2191 }\n                        @case ('DESC') { \u2193 }\n                        @default { \u2195 }\n                      }\n                    </span>\n                  }\n                }\n              </th>\n            }\n          </tr>\n        </thead>\n\n        <tbody>\n          @if (displayedEntities().length === 0) {\n            <tr>\n              <td [attr.colspan]=\"visibleColumns().length + (hasActionColumn() ? 1 : 0) + (hasSelectionColumn() ? 1 : 0)\" class=\"empty-row\">{{ chill.T('D43AB4B7-3FD0-486C-88FF-7214FB45A1CA', 'No entities to display.', 'Nessuna entit\u00E0 da visualizzare.') }}</td>\n            </tr>\n          } @else {\n            @for (entity of displayedEntities(); track trackByEntity($index, entity)) {\n              <tr\n                [class.pending-row]=\"isPendingRow(entity)\"\n                [class.deleted-row]=\"isDeletedRow(entity)\"\n                [class.refreshed-row]=\"isRefreshFlashRow(entity)\">\n                @if (hasSelectionColumn()) {\n                  <td class=\"selection-column\">\n                    <input\n                      type=\"checkbox\"\n                      class=\"row-selection-checkbox\"\n                      [checked]=\"isRowSelected(entity)\"\n                      [disabled]=\"isRowSelectionDisabled(entity)\"\n                      [attr.aria-label]=\"selectionColumn()?.ariaLabel || chill.T('2EE7A0D9-CDE2-4F72-9BE1-B86A91D4B208', 'Select row', 'Seleziona riga')\"\n                      (change)=\"toggleRowSelection(entity, $any($event.target).checked)\" />\n                  </td>\n                }\n                @if (hasActionColumn()) {\n                  <td class=\"action-column\" [class.menu-open]=\"isRowActionMenuOpen(entity)\">\n                    <div class=\"row-action-menu\">\n                      <button\n                        type=\"button\"\n                        class=\"row-action-menu__trigger\"\n                        (click)=\"toggleRowActionMenu($event, entity)\"\n                        [attr.aria-label]=\"chill.T('7143A8CE-9D46-4509-8D26-AC954C88F277', 'Open row actions', 'Apri azioni riga')\">\n                        <span class=\"material-symbol-icon\">more_horiz</span>\n                      </button>\n\n                      @if (isRowActionMenuOpen(entity)) {\n                        <div class=\"row-action-menu__panel\" [ngStyle]=\"rowActionMenuStyle()\" (click)=\"$event.stopPropagation()\">\n                        @for (action of resolvedRowActions(); track $index) {\n                          <button\n                            type=\"button\"\n                            class=\"row-action-menu__item\"\n                            (click)=\"runRowAction(action, entity)\"\n                            [disabled]=\"isRowActionDisabled(action, entity)\"\n                            [attr.aria-label]=\"rowActionLabel(action)\">\n                            <span\n                              class=\"row-action-button__icon\"\n                              [class.material-symbol-icon]=\"rowActionIconClass(action) === 'material-symbol-icon'\">{{ rowActionIcon(action) }}</span>\n                            <span class=\"row-action-menu__label\">\n                              @if (action.labelGuid && action.primaryDefaultText && action.secondaryDefaultText) {\n                                <app-chill-i18n-label\n                                  [labelGuid]=\"action.labelGuid\"\n                                  [primaryDefaultText]=\"action.primaryDefaultText\"\n                                  [secondaryDefaultText]=\"action.secondaryDefaultText\"\n                                  [editable]=\"false\" />\n                              } @else {\n                                {{ rowActionLabel(action) }}\n                              }\n                            </span>\n                          </button>\n                        }\n                        </div>\n                      }\n                    </div>\n                  </td>\n                }\n                @for (column of visibleColumns(); track column.name) {\n                  <td\n                    class=\"data-cell\"\n                    [style.width.%]=\"isColumnEditing(column) ? null : columnWidthPercent(column)\"\n                    [class.is-editing]=\"isCellEditing(entity, column)\"\n                    (dblclick)=\"activateCellEdit(entity, column)\"\n                    (focusout)=\"handleCellFocusOut($event)\">\n                    @if (isCellEditing(entity, column)) {\n                      <div class=\"data-cell__editor\" (keydown)=\"handleCellEditorKeydown($event)\">\n                        <app-chill-polymorphic-input\n                          [form]=\"activeCellEdit()?.form ?? null\"\n                          [schema]=\"schema()\"\n                          [propertyNames]=\"[column.name]\"\n                          [readonlyPropertyNames]=\"readonlyPropertyNames()\"\n                          [showLabels]=\"false\"\n                          [externalErrors]=\"rowFieldErrors(entity)\"\n                          (valueChange)=\"handleCellValueChange($event)\"\n                          (validityChange)=\"handleCellValidityChange($event)\"\n                          (lookupDialogOpenChange)=\"handleLookupDialogOpenChange($event)\"\n                          (editorDialogOpenChange)=\"handleLookupDialogOpenChange($event)\" />\n                      </div>\n                    } @else {\n                      <app-chill-polymorphic-output\n                        [source]=\"entity\"\n                        [schema]=\"schema()\"\n                        [propertyName]=\"column.name\" />\n                    }\n                  </td>\n                }\n              </tr>\n            }\n          }\n        </tbody>\n      </table>\n    </div>\n  }\n</section>\n", styles: [":host{display:block;min-width:0;max-width:100%}.chill-table-shell{display:grid;gap:1rem;min-width:0;max-width:100%}.table-actions{display:flex;align-items:center;gap:.75rem;justify-content:flex-end;flex-wrap:wrap}.layout-button{min-height:2.75rem;padding:.75rem 1rem;border:1px solid var(--border-color);border-radius:999px;background:var(--surface-0);color:var(--text-main);cursor:pointer}.table-header{display:flex;align-items:flex-start;justify-content:space-between;gap:1rem}.table-header__title-row{min-width:0;display:flex;align-items:flex-start;justify-content:space-between;gap:.75rem}.table-header__title{min-width:0}.table-header__mobile-controls{display:none;align-items:center;gap:.5rem;flex:0 0 auto}.table-header__task-close{width:2.5rem;min-width:2.5rem;height:2.5rem;display:none;place-items:center;padding:0;border:1px solid var(--border-color);border-radius:999px;background:var(--surface-0);color:var(--text-main);cursor:pointer}.table-header h2,.table-header p{margin:0}.table-kicker{color:var(--accent);font-size:.75rem;font-weight:700;letter-spacing:.16em;text-transform:uppercase}.table-search{display:inline-flex;align-items:center;justify-content:flex-end;gap:.4rem;min-height:2.5rem;flex:0 1 22rem}.table-search__input{min-width:10rem;width:min(18rem,52vw);min-height:2.5rem;padding:.55rem .8rem;border:1px solid var(--border-color);border-radius:999px;background:var(--surface-0);color:var(--text-main);font:inherit}.table-search__input:focus{outline:2px solid color-mix(in srgb,var(--accent) 42%,transparent);outline-offset:2px}.table-search__icon-button{width:2.5rem;min-width:2.5rem;height:2.5rem;display:inline-grid;place-items:center;padding:0;border:1px solid var(--border-color);border-radius:999px;background:var(--surface-0);color:var(--text-main);cursor:pointer}.table-search__icon-button:disabled{cursor:default;opacity:.45}.table-search__icon-button--mobile{display:none}.table-wrap{position:relative;min-width:0;max-width:100%;overflow-x:auto;overflow-y:visible;overscroll-behavior-x:contain;border-radius:.5rem;border:1px solid var(--border-color);background:var(--surface-2);box-shadow:var(--shadow)}.table-wrap.is-edit-layout-mode{overflow-x:auto}.chill-table{width:100%;border-collapse:collapse;min-width:var(--chill-table-min-width, 36rem);table-layout:fixed}.chill-table.is-edit-layout-mode,.chill-table.has-inline-cell-edit{width:max-content;min-width:var(--chill-table-min-width, 100%);table-layout:auto}.chill-table th{padding:.5rem .4rem;text-align:left;vertical-align:middle;border-bottom:1px solid var(--border-color);border-left:1px solid var(--border-color);border-right:1px solid var(--border-color)}.chill-table td{padding:.3rem .4rem;text-align:left;vertical-align:middle;border-bottom:1px solid var(--border-color)}.chill-table th.action-column,.chill-table td.action-column,.chill-table th.selection-column,.chill-table td.selection-column{position:sticky;left:0;z-index:1;width:1%;white-space:nowrap;overflow:visible}.chill-table th.action-column,.chill-table td.action-column,col.action-column{min-width:3rem}.chill-table.has-selection-column th.action-column,.chill-table.has-selection-column td.action-column{left:3rem}.chill-table th{color:var(--accent-strong);background:var(--accent-soft);font-size:.85rem;font-weight:700;letter-spacing:.02em;white-space:nowrap;overflow:hidden}.chill-table.is-edit-layout-mode th{overflow:visible}.chill-table th.action-column{z-index:3}.chill-table th.selection-column{z-index:4}.chill-table td.action-column{z-index:2}.chill-table td.action-column.menu-open{z-index:25}.chill-table th.is-sortable{cursor:pointer}.chill-table th.is-sorted{background:color-mix(in srgb,var(--accent) 22%,var(--accent-soft));color:var(--text-main);box-shadow:inset 0 -2px 0 var(--accent)}.chill-table td{color:var(--text-main);max-width:20rem;overflow-wrap:anywhere;background:var(--surface-2)}.chill-table.is-edit-layout-mode td.data-cell{width:auto!important}.chill-table tbody tr.pending-row>td:first-child{box-shadow:inset .32rem 0 0 var(--accent)}.chill-table tbody tr.deleted-row{opacity:.5}.chill-table tbody tr.refreshed-row>td{animation:chill-table-row-refresh-flash .9s ease-out}@keyframes chill-table-row-refresh-flash{0%{background:color-mix(in srgb,var(--accent) 26%,var(--surface-2))}to{background:var(--surface-2)}}@media(prefers-reduced-motion:reduce){.chill-table tbody tr.refreshed-row>td{animation:none;background:color-mix(in srgb,var(--accent) 14%,var(--surface-2))}}.data-cell{cursor:default}.data-cell.is-editing{padding:.45rem .75rem;max-width:none;white-space:nowrap}.data-cell__editor{width:100%;min-width:14rem;max-width:none}.data-cell__editor :where(.polymorphic-fields){display:block}.data-cell__editor :where(.field){gap:.35rem}.data-cell__editor :where(.field>span){display:none}.column-editor{display:grid;gap:.4rem;width:100%;min-width:100%;max-width:100%;overflow:hidden}.chill-table.is-edit-layout-mode .column-editor{width:max-content;min-width:100%;max-width:none;overflow:visible}.column-editor__title-row,.column-editor__settings-row{min-width:0}.column-editor__title-row{display:grid;grid-template-columns:minmax(0,1fr) auto;align-items:center;gap:.45rem}.column-editor__settings-row{display:grid;grid-template-columns:2rem minmax(7.25rem,1fr) minmax(6.35rem,auto);align-items:center;gap:.45rem;width:100%}.column-sort-indicator{margin-left:.35rem;opacity:.8}.column-icon-button{width:2rem;min-width:2rem;height:2rem;display:inline-grid;place-items:center;padding:0;border:1px solid color-mix(in srgb,var(--accent) 24%,var(--border-color));border-radius:999px;background:color-mix(in srgb,var(--surface-0) 94%,transparent);color:inherit;cursor:pointer}.column-icon-button:disabled{cursor:default;opacity:.45}.column-width-controls{display:inline-flex;align-items:center;justify-self:end;gap:.15rem;min-width:6.35rem;flex:0 0 auto}.column-width-value{min-width:1.75rem;text-align:center;color:var(--text-muted);font-size:.7rem;font-weight:700}.column-name-input{min-width:0;width:100%;max-width:100%;padding:0;border:0;margin:0;background:transparent;color:inherit;font:inherit;outline:none;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}.property-type-select{min-width:7.25rem;min-height:2rem;padding:.35rem .55rem;border:1px solid color-mix(in srgb,var(--accent) 24%,var(--border-color));border-radius:.4rem;background:var(--surface-0);color:var(--text-main);font:inherit;font-size:.78rem}.column-editor input[type=checkbox]{margin:0;accent-color:var(--accent)}.hidden-column-picker{display:flex;align-items:center;gap:.5rem;color:var(--text-main);font-size:.85rem;font-weight:600}.hidden-column-picker select{min-height:2.5rem;min-width:12rem;padding:.55rem .8rem;border:1px solid var(--border-color);border-radius:999px;background:var(--surface-0);color:var(--text-main);font:inherit}.row-action-button,.row-action-menu__trigger,.row-action-menu__item{border:1px solid var(--border-color);background:var(--surface-0);color:var(--text-main);font:inherit}.row-action-menu{position:relative}.row-action-menu__trigger{width:2.25rem;min-height:2.25rem;display:inline-grid;place-items:center;padding:0;border-radius:999px;background:var(--surface-0);color:var(--text-main);cursor:pointer}.row-action-menu__panel{position:fixed;z-index:40;min-width:10rem;display:grid;gap:.25rem;padding:.4rem;border:1px solid var(--border-color);border-radius:.85rem;background:var(--surface-3);box-shadow:var(--shadow-soft)}.row-action-menu__item{min-height:2.15rem;display:inline-flex;align-items:center;gap:.55rem;padding:.45rem .7rem;border-radius:.7rem;cursor:pointer;text-align:left}.row-action-menu__label{white-space:nowrap}.row-action-button__icon{display:inline-flex;align-items:center;justify-content:center;min-width:1.1rem;line-height:1}.row-action-button__icon.material-symbol-icon{font-size:1.15rem}.selection-column{width:3rem;min-width:3rem;text-align:center}.row-selection-checkbox{width:1rem;height:1rem;margin:0;accent-color:var(--accent)}.row-action-button:disabled,.row-action-menu__item:disabled{cursor:progress;opacity:.65}.chill-table tbody tr:last-child td{border-bottom:0}.empty-state,.empty-row{color:var(--text-muted)}.error-state{color:var(--danger)}.empty-state{padding:1rem 1.25rem;border-radius:1rem;background:var(--surface-2);border:1px dashed var(--border-color)}:root[data-theme=dark] .layout-button{background:#09131a94}.empty-row{text-align:center;padding:1.5rem}@media(max-width:720px){.table-header{align-items:stretch;flex-direction:column}.table-header__title-row{width:100%}.table-header__mobile-controls{display:flex}.table-header__task-close{display:inline-grid;flex:0 0 auto}.table-search{width:100%;flex:none}.table-search:not(.is-open){display:none}.table-search__input{width:100%;min-width:0}.table-search__icon-button--mobile{display:inline-grid}.chill-table{min-width:var(--chill-table-mobile-min-width, var(--chill-table-min-width, 100%));table-layout:auto}.chill-table th:not(.action-column):not(.selection-column),.chill-table td.data-cell,col.data-column{min-width:20vw;max-width:70vw}}\n"], dependencies: [{ kind: "ngmodule", type: CommonModule }, { kind: "directive", type: i1$1.NgStyle, selector: "[ngStyle]", inputs: ["ngStyle"] }, { kind: "ngmodule", type: FormsModule }, { kind: "directive", type: i1.NgSelectOption, selector: "option", inputs: ["ngValue", "value"] }, { kind: "directive", type: i1.ɵNgSelectMultipleOption, selector: "option", inputs: ["ngValue", "value"] }, { kind: "directive", type: i1.DefaultValueAccessor, selector: "input:not([type=checkbox])[formControlName],textarea[formControlName],input:not([type=checkbox])[formControl],textarea[formControl],input:not([type=checkbox])[ngModel],textarea[ngModel],[ngDefaultControl]" }, { kind: "directive", type: i1.SelectControlValueAccessor, selector: "select:not([multiple])[formControlName],select:not([multiple])[formControl],select:not([multiple])[ngModel]", inputs: ["compareWith"] }, { kind: "directive", type: i1.NgControlStatus, selector: "[formControlName],[ngModel],[formControl]" }, { kind: "directive", type: i1.NgModel, selector: "[ngModel]:not([formControlName]):not([formControl])", inputs: ["name", "disabled", "ngModel", "ngModelOptions"], outputs: ["ngModelChange"], exportAs: ["ngModel"] }, { kind: "component", type: ChillI18nLabelComponent, selector: "app-chill-i18n-label", inputs: ["labelGuid", "primaryDefaultText", "secondaryDefaultText", "editable"] }, { kind: "component", type: ChillI18nButtonLabelComponent, selector: "app-chill-i18n-button-label", inputs: ["labelGuid", "primaryDefaultText", "secondaryDefaultText", "editable"] }, { kind: "component", type: ChillPolymorphicOutputComponent, selector: "app-chill-polymorphic-output", inputs: ["source", "schema", "propertyName"] }, { kind: "component", type: ChillPolymorphicInputComponent, selector: "app-chill-polymorphic-input", inputs: ["form", "schema", "propertyNames", "readonlyPropertyNames", "externalErrors", "showLabels"], outputs: ["valueChange", "validityChange", "fieldBlur", "lookupDialogOpenChange", "editorDialogOpenChange"] }] }); }
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: ChillTableComponent, decorators: [{
            type: Component,
            args: [{ selector: 'app-chill-table', standalone: true, imports: [CommonModule, FormsModule, ChillI18nLabelComponent, ChillI18nButtonLabelComponent, ChillPolymorphicOutputComponent, ChillPolymorphicInputComponent], template: "<section class=\"chill-table-shell\">\n  @if (showSchemaHeader() && schema()?.displayName) {\n    <header class=\"table-header\">\n      <div class=\"table-header__title-row\">\n        <div class=\"table-header__title\">\n          <p class=\"table-kicker\"><app-chill-i18n-label [labelGuid]=\"'2DDE962B-086C-47B1-8A48-B16F0E34C0A3'\" [primaryDefaultText]=\"'Chill schema'\" [secondaryDefaultText]=\"'Schema Chill'\" /></p>\n          <h2>{{ schema()?.displayName }}</h2>\n        </div>\n\n        <div class=\"table-header__mobile-controls\">\n          @if (enableFullTextSearch()) {\n            <button\n              type=\"button\"\n              class=\"table-search__icon-button table-search__icon-button--mobile\"\n              (click)=\"toggleFullTextSearch()\"\n              [attr.aria-label]=\"chill.T('54E62296-DB94-4973-A2AE-49902D5E25E5', 'Full text search', 'Ricerca full text')\">\n              <span class=\"material-symbol-icon\" aria-hidden=\"true\">search</span>\n            </button>\n          }\n\n          @if (showMobileTaskClose()) {\n            <button\n              type=\"button\"\n              class=\"table-header__task-close\"\n              (click)=\"closeMobileTask()\"\n              [attr.aria-label]=\"chill.T('D2566993-8138-408A-9153-904454528781', 'Close task', 'Chiudi attivita')\">\n              <span class=\"material-symbol-icon\" aria-hidden=\"true\">close</span>\n            </button>\n          }\n        </div>\n      </div>\n\n      @if (enableFullTextSearch()) {\n        <div class=\"table-search\" [class.is-open]=\"isFullTextSearchOpen()\">\n          @if (isFullTextSearchOpen()) {\n            <input\n              #fullTextSearchInput\n              type=\"search\"\n              class=\"table-search__input\"\n              [ngModel]=\"fullTextSearchText()\"\n              (ngModelChange)=\"updateFullTextSearchText($event)\"\n              (keydown.enter)=\"submitFullTextSearch()\"\n              (keydown.escape)=\"resetFullTextSearch()\"\n              [placeholder]=\"chill.T('D513421E-1C00-425E-A89B-E736A440474F', 'Search', 'Cerca')\"\n              [attr.aria-label]=\"chill.T('54E62296-DB94-4973-A2AE-49902D5E25E5', 'Full text search', 'Ricerca full text')\" />\n            <button\n              type=\"button\"\n              class=\"table-search__icon-button\"\n              (click)=\"resetFullTextSearch()\"\n              [attr.aria-label]=\"chill.T('34015BA4-E0CA-460E-B82B-A4E2D4D8A184', 'Clear', 'Pulisci')\">\n              <span class=\"material-symbol-icon\" aria-hidden=\"true\">close</span>\n            </button>\n          } @else {\n            <button\n              type=\"button\"\n              class=\"table-search__icon-button\"\n              (click)=\"toggleFullTextSearch()\"\n              [attr.aria-label]=\"chill.T('54E62296-DB94-4973-A2AE-49902D5E25E5', 'Full text search', 'Ricerca full text')\">\n              <span class=\"material-symbol-icon\" aria-hidden=\"true\">search</span>\n            </button>\n          }\n        </div>\n      }\n    </header>\n  }\n\n  @if (layout.isLayoutEditingEnabled()) {\n    <div class=\"table-actions\">\n      @if (isEditLayoutMode() && hiddenColumns().length > 0) {\n        <label class=\"hidden-column-picker\">\n          <span>{{ chill.T('3758A6E6-85C1-481D-BF57-A531E79661B8', 'Show column', 'Mostra colonna') }}</span>\n          <select #hiddenColumnSelect (change)=\"revealColumn(hiddenColumnSelect.value); hiddenColumnSelect.value = ''\">\n            <option value=\"\">{{ chill.T('D1A15B17-B3C2-49A8-90AA-BAE81ECA8D69', 'Select hidden column', 'Seleziona colonna nascosta') }}</option>\n            @for (column of hiddenColumns(); track column.name) {\n              <option [value]=\"column.name\">{{ column.displayName || column.name }}</option>\n            }\n          </select>\n        </label>\n      }\n      <button type=\"button\" class=\"layout-button\" (click)=\"toggleEditLayoutMode()\" [disabled]=\"isSavingLayout()\">\n        @if (isSavingLayout()) {\n          <app-chill-i18n-button-label [labelGuid]=\"'B46C82D8-443E-45DE-8D49-C270656B511E'\" [primaryDefaultText]=\"'Saving layout...'\" [secondaryDefaultText]=\"'Salvataggio layout...'\" />\n        } @else if (isEditLayoutMode()) {\n          <app-chill-i18n-button-label [labelGuid]=\"'7C681F13-4245-447F-B66A-D9A3A500D322'\" [primaryDefaultText]=\"'Done'\" [secondaryDefaultText]=\"'Fine'\" />\n        } @else {\n          <app-chill-i18n-button-label [labelGuid]=\"'9D0B43AC-47B4-4C63-98C7-DB0118C9C0CF'\" [primaryDefaultText]=\"'Edit layout mode'\" [secondaryDefaultText]=\"'Modalita modifica layout'\" />\n        }\n      </button>\n      @if (isEditLayoutMode()) {\n        <button type=\"button\" class=\"layout-button\" (click)=\"refreshSchemaFromModel()\" [disabled]=\"isSavingLayout()\">\n          @if (isRefreshingSchema()) {\n            <app-chill-i18n-button-label [labelGuid]=\"'8628775B-B831-44F2-8A38-909F40E2F7B3'\" [primaryDefaultText]=\"'Updating schema...'\" [secondaryDefaultText]=\"'Aggiornamento schema...'\" />\n          } @else {\n            <app-chill-i18n-button-label [labelGuid]=\"'62953302-B951-4FD1-BD08-4B7649A91BAF'\" [primaryDefaultText]=\"'Update'\" [secondaryDefaultText]=\"'Aggiorna'\" />\n          }\n        </button>\n      }\n    </div>\n  }\n\n  @if (layoutError()) {\n    <div class=\"empty-state error-state\">{{ layoutError() }}</div>\n  }\n\n  @if (visibleColumns().length === 0) {\n    <div class=\"empty-state\">{{ chill.T('3D7ABF07-6E0C-4F99-A0C8-C936C44322C6', 'No schema properties available.', 'Nessuna propriet\u00E0 di schema disponibile.') }}</div>\n  } @else {\n    <div class=\"table-wrap\" [class.is-edit-layout-mode]=\"isEditLayoutMode()\">\n      <table\n        class=\"chill-table\"\n        [class.has-selection-column]=\"hasSelectionColumn()\"\n        [class.is-edit-layout-mode]=\"isEditLayoutMode()\"\n        [class.has-inline-cell-edit]=\"activeCellEdit() !== null\"\n        [style.--chill-table-min-width]=\"tableMinimumWidth()\"\n        [style.--chill-table-mobile-min-width]=\"mobileTableMinimumWidth()\">\n        <colgroup>\n          @if (hasSelectionColumn()) {\n            <col class=\"selection-column\" />\n          }\n          @if (hasActionColumn()) {\n            <col class=\"action-column\" />\n          }\n          @for (column of visibleColumns(); track column.name) {\n            <col class=\"data-column\" [style.width.%]=\"isColumnEditing(column) ? null : columnWidthPercent(column)\" />\n          }\n        </colgroup>\n        <thead>\n          <tr>\n            @if (hasSelectionColumn()) {\n              <th scope=\"col\" class=\"selection-column\"></th>\n            }\n            @if (hasActionColumn()) {\n              <th scope=\"col\" class=\"action-column\">#</th>\n            }\n            @for (column of visibleColumns(); track column.name) {\n              <th\n                scope=\"col\"\n                [draggable]=\"isEditLayoutMode()\"\n                (dragstart)=\"beginDrag($event, column.name)\"\n                (dragover)=\"allowDrop($event)\"\n                (drop)=\"dropColumn(column.name)\"\n                (dragend)=\"endDrag()\"\n                [class.is-sortable]=\"!isEditLayoutMode() && canSortColumn(column)\"\n                [class.is-sorted]=\"sortDirectionFor(column) !== null\"\n                (click)=\"!isEditLayoutMode() && canSortColumn(column) && toggleColumnSort(column)\">\n                @if (isEditLayoutMode()) {\n                  <div class=\"column-editor\" (click)=\"$event.stopPropagation()\">\n                    <div class=\"column-editor__title-row\">\n                      <input\n                        type=\"text\"\n                        class=\"column-name-input\"\n                        [ngModel]=\"column.displayName\"\n                        (ngModelChange)=\"updateColumnDisplayName(column.name, $event)\"\n                        [name]=\"'displayName-' + column.name\" />\n                      <input\n                        type=\"checkbox\"\n                        [checked]=\"!column.hidden\"\n                        (change)=\"updateColumnHidden(column.name, !$any($event.target).checked)\"\n                        [name]=\"'hidden-' + column.name\"\n                        [attr.aria-label]=\"chill.T('C6610992-9D61-4EBA-8D13-92F34521AB64', 'Show column', 'Mostra colonna')\" />\n                    </div>\n                    <div class=\"column-editor__settings-row\">\n                      <button\n                        type=\"button\"\n                        class=\"column-icon-button\"\n                        (click)=\"openPropertySettings(column)\"\n                        [attr.aria-label]=\"chill.T('40E9838A-70F2-4E89-BE38-BBE44027D253', 'Edit property settings', 'Modifica impostazioni propriet\u00E0')\">\n                        <span class=\"material-symbol-icon\">tune</span>\n                      </button>\n                      <select\n                        class=\"property-type-select\"\n                        [ngModel]=\"column.propertyType\"\n                        (ngModelChange)=\"updatePropertyType(column, $event)\"\n                        [name]=\"'propertyType-' + column.name\"\n                        [attr.aria-label]=\"chill.T('A1803D67-C40D-41AF-BFD2-8F9B0E34C48B', 'Property type', 'Tipo propriet\u00E0')\">\n                        @for (option of propertyTypeOptions; track option.value) {\n                          <option [ngValue]=\"option.value\" [disabled]=\"isPropertyTypeOptionDisabled(column, option.value)\">{{ option.label }}</option>\n                        }\n                      </select>\n                      <div class=\"column-width-controls\" [attr.aria-label]=\"chill.T('53796BE9-AF5C-487D-9B26-468C1B75FE54', 'Column width proportion', 'Proporzione larghezza colonna')\">\n                        <button\n                          type=\"button\"\n                          class=\"column-icon-button\"\n                          (click)=\"updateColumnWidthProportion(column.name, -1)\"\n                          [disabled]=\"!canDecreaseColumnWidth(column)\"\n                          [attr.aria-label]=\"chill.T('8C3198E8-F1CA-4B1B-A91B-A02B48FF9219', 'Decrease column width', 'Riduci larghezza colonna')\">\n                          <span class=\"material-symbol-icon\" aria-hidden=\"true\">remove</span>\n                        </button>\n                        <span class=\"column-width-value\">{{ columnWidthLabel(column) }}</span>\n                        <button\n                          type=\"button\"\n                          class=\"column-icon-button\"\n                          (click)=\"updateColumnWidthProportion(column.name, 1)\"\n                          [disabled]=\"!canIncreaseColumnWidth(column)\"\n                          [attr.aria-label]=\"chill.T('30EA851D-40AB-4AD2-A4C8-D2E77C5162BB', 'Increase column width', 'Aumenta larghezza colonna')\">\n                          <span class=\"material-symbol-icon\" aria-hidden=\"true\">add</span>\n                        </button>\n                      </div>\n                    </div>\n                  </div>\n                } @else {\n                  <span>{{ column.displayName || column.name }}</span>\n                  @if (canSortColumn(column)) {\n                    <span class=\"column-sort-indicator\" aria-hidden=\"true\">\n                      @switch (sortDirectionFor(column)) {\n                        @case ('ASC') { \u2191 }\n                        @case ('DESC') { \u2193 }\n                        @default { \u2195 }\n                      }\n                    </span>\n                  }\n                }\n              </th>\n            }\n          </tr>\n        </thead>\n\n        <tbody>\n          @if (displayedEntities().length === 0) {\n            <tr>\n              <td [attr.colspan]=\"visibleColumns().length + (hasActionColumn() ? 1 : 0) + (hasSelectionColumn() ? 1 : 0)\" class=\"empty-row\">{{ chill.T('D43AB4B7-3FD0-486C-88FF-7214FB45A1CA', 'No entities to display.', 'Nessuna entit\u00E0 da visualizzare.') }}</td>\n            </tr>\n          } @else {\n            @for (entity of displayedEntities(); track trackByEntity($index, entity)) {\n              <tr\n                [class.pending-row]=\"isPendingRow(entity)\"\n                [class.deleted-row]=\"isDeletedRow(entity)\"\n                [class.refreshed-row]=\"isRefreshFlashRow(entity)\">\n                @if (hasSelectionColumn()) {\n                  <td class=\"selection-column\">\n                    <input\n                      type=\"checkbox\"\n                      class=\"row-selection-checkbox\"\n                      [checked]=\"isRowSelected(entity)\"\n                      [disabled]=\"isRowSelectionDisabled(entity)\"\n                      [attr.aria-label]=\"selectionColumn()?.ariaLabel || chill.T('2EE7A0D9-CDE2-4F72-9BE1-B86A91D4B208', 'Select row', 'Seleziona riga')\"\n                      (change)=\"toggleRowSelection(entity, $any($event.target).checked)\" />\n                  </td>\n                }\n                @if (hasActionColumn()) {\n                  <td class=\"action-column\" [class.menu-open]=\"isRowActionMenuOpen(entity)\">\n                    <div class=\"row-action-menu\">\n                      <button\n                        type=\"button\"\n                        class=\"row-action-menu__trigger\"\n                        (click)=\"toggleRowActionMenu($event, entity)\"\n                        [attr.aria-label]=\"chill.T('7143A8CE-9D46-4509-8D26-AC954C88F277', 'Open row actions', 'Apri azioni riga')\">\n                        <span class=\"material-symbol-icon\">more_horiz</span>\n                      </button>\n\n                      @if (isRowActionMenuOpen(entity)) {\n                        <div class=\"row-action-menu__panel\" [ngStyle]=\"rowActionMenuStyle()\" (click)=\"$event.stopPropagation()\">\n                        @for (action of resolvedRowActions(); track $index) {\n                          <button\n                            type=\"button\"\n                            class=\"row-action-menu__item\"\n                            (click)=\"runRowAction(action, entity)\"\n                            [disabled]=\"isRowActionDisabled(action, entity)\"\n                            [attr.aria-label]=\"rowActionLabel(action)\">\n                            <span\n                              class=\"row-action-button__icon\"\n                              [class.material-symbol-icon]=\"rowActionIconClass(action) === 'material-symbol-icon'\">{{ rowActionIcon(action) }}</span>\n                            <span class=\"row-action-menu__label\">\n                              @if (action.labelGuid && action.primaryDefaultText && action.secondaryDefaultText) {\n                                <app-chill-i18n-label\n                                  [labelGuid]=\"action.labelGuid\"\n                                  [primaryDefaultText]=\"action.primaryDefaultText\"\n                                  [secondaryDefaultText]=\"action.secondaryDefaultText\"\n                                  [editable]=\"false\" />\n                              } @else {\n                                {{ rowActionLabel(action) }}\n                              }\n                            </span>\n                          </button>\n                        }\n                        </div>\n                      }\n                    </div>\n                  </td>\n                }\n                @for (column of visibleColumns(); track column.name) {\n                  <td\n                    class=\"data-cell\"\n                    [style.width.%]=\"isColumnEditing(column) ? null : columnWidthPercent(column)\"\n                    [class.is-editing]=\"isCellEditing(entity, column)\"\n                    (dblclick)=\"activateCellEdit(entity, column)\"\n                    (focusout)=\"handleCellFocusOut($event)\">\n                    @if (isCellEditing(entity, column)) {\n                      <div class=\"data-cell__editor\" (keydown)=\"handleCellEditorKeydown($event)\">\n                        <app-chill-polymorphic-input\n                          [form]=\"activeCellEdit()?.form ?? null\"\n                          [schema]=\"schema()\"\n                          [propertyNames]=\"[column.name]\"\n                          [readonlyPropertyNames]=\"readonlyPropertyNames()\"\n                          [showLabels]=\"false\"\n                          [externalErrors]=\"rowFieldErrors(entity)\"\n                          (valueChange)=\"handleCellValueChange($event)\"\n                          (validityChange)=\"handleCellValidityChange($event)\"\n                          (lookupDialogOpenChange)=\"handleLookupDialogOpenChange($event)\"\n                          (editorDialogOpenChange)=\"handleLookupDialogOpenChange($event)\" />\n                      </div>\n                    } @else {\n                      <app-chill-polymorphic-output\n                        [source]=\"entity\"\n                        [schema]=\"schema()\"\n                        [propertyName]=\"column.name\" />\n                    }\n                  </td>\n                }\n              </tr>\n            }\n          }\n        </tbody>\n      </table>\n    </div>\n  }\n</section>\n", styles: [":host{display:block;min-width:0;max-width:100%}.chill-table-shell{display:grid;gap:1rem;min-width:0;max-width:100%}.table-actions{display:flex;align-items:center;gap:.75rem;justify-content:flex-end;flex-wrap:wrap}.layout-button{min-height:2.75rem;padding:.75rem 1rem;border:1px solid var(--border-color);border-radius:999px;background:var(--surface-0);color:var(--text-main);cursor:pointer}.table-header{display:flex;align-items:flex-start;justify-content:space-between;gap:1rem}.table-header__title-row{min-width:0;display:flex;align-items:flex-start;justify-content:space-between;gap:.75rem}.table-header__title{min-width:0}.table-header__mobile-controls{display:none;align-items:center;gap:.5rem;flex:0 0 auto}.table-header__task-close{width:2.5rem;min-width:2.5rem;height:2.5rem;display:none;place-items:center;padding:0;border:1px solid var(--border-color);border-radius:999px;background:var(--surface-0);color:var(--text-main);cursor:pointer}.table-header h2,.table-header p{margin:0}.table-kicker{color:var(--accent);font-size:.75rem;font-weight:700;letter-spacing:.16em;text-transform:uppercase}.table-search{display:inline-flex;align-items:center;justify-content:flex-end;gap:.4rem;min-height:2.5rem;flex:0 1 22rem}.table-search__input{min-width:10rem;width:min(18rem,52vw);min-height:2.5rem;padding:.55rem .8rem;border:1px solid var(--border-color);border-radius:999px;background:var(--surface-0);color:var(--text-main);font:inherit}.table-search__input:focus{outline:2px solid color-mix(in srgb,var(--accent) 42%,transparent);outline-offset:2px}.table-search__icon-button{width:2.5rem;min-width:2.5rem;height:2.5rem;display:inline-grid;place-items:center;padding:0;border:1px solid var(--border-color);border-radius:999px;background:var(--surface-0);color:var(--text-main);cursor:pointer}.table-search__icon-button:disabled{cursor:default;opacity:.45}.table-search__icon-button--mobile{display:none}.table-wrap{position:relative;min-width:0;max-width:100%;overflow-x:auto;overflow-y:visible;overscroll-behavior-x:contain;border-radius:.5rem;border:1px solid var(--border-color);background:var(--surface-2);box-shadow:var(--shadow)}.table-wrap.is-edit-layout-mode{overflow-x:auto}.chill-table{width:100%;border-collapse:collapse;min-width:var(--chill-table-min-width, 36rem);table-layout:fixed}.chill-table.is-edit-layout-mode,.chill-table.has-inline-cell-edit{width:max-content;min-width:var(--chill-table-min-width, 100%);table-layout:auto}.chill-table th{padding:.5rem .4rem;text-align:left;vertical-align:middle;border-bottom:1px solid var(--border-color);border-left:1px solid var(--border-color);border-right:1px solid var(--border-color)}.chill-table td{padding:.3rem .4rem;text-align:left;vertical-align:middle;border-bottom:1px solid var(--border-color)}.chill-table th.action-column,.chill-table td.action-column,.chill-table th.selection-column,.chill-table td.selection-column{position:sticky;left:0;z-index:1;width:1%;white-space:nowrap;overflow:visible}.chill-table th.action-column,.chill-table td.action-column,col.action-column{min-width:3rem}.chill-table.has-selection-column th.action-column,.chill-table.has-selection-column td.action-column{left:3rem}.chill-table th{color:var(--accent-strong);background:var(--accent-soft);font-size:.85rem;font-weight:700;letter-spacing:.02em;white-space:nowrap;overflow:hidden}.chill-table.is-edit-layout-mode th{overflow:visible}.chill-table th.action-column{z-index:3}.chill-table th.selection-column{z-index:4}.chill-table td.action-column{z-index:2}.chill-table td.action-column.menu-open{z-index:25}.chill-table th.is-sortable{cursor:pointer}.chill-table th.is-sorted{background:color-mix(in srgb,var(--accent) 22%,var(--accent-soft));color:var(--text-main);box-shadow:inset 0 -2px 0 var(--accent)}.chill-table td{color:var(--text-main);max-width:20rem;overflow-wrap:anywhere;background:var(--surface-2)}.chill-table.is-edit-layout-mode td.data-cell{width:auto!important}.chill-table tbody tr.pending-row>td:first-child{box-shadow:inset .32rem 0 0 var(--accent)}.chill-table tbody tr.deleted-row{opacity:.5}.chill-table tbody tr.refreshed-row>td{animation:chill-table-row-refresh-flash .9s ease-out}@keyframes chill-table-row-refresh-flash{0%{background:color-mix(in srgb,var(--accent) 26%,var(--surface-2))}to{background:var(--surface-2)}}@media(prefers-reduced-motion:reduce){.chill-table tbody tr.refreshed-row>td{animation:none;background:color-mix(in srgb,var(--accent) 14%,var(--surface-2))}}.data-cell{cursor:default}.data-cell.is-editing{padding:.45rem .75rem;max-width:none;white-space:nowrap}.data-cell__editor{width:100%;min-width:14rem;max-width:none}.data-cell__editor :where(.polymorphic-fields){display:block}.data-cell__editor :where(.field){gap:.35rem}.data-cell__editor :where(.field>span){display:none}.column-editor{display:grid;gap:.4rem;width:100%;min-width:100%;max-width:100%;overflow:hidden}.chill-table.is-edit-layout-mode .column-editor{width:max-content;min-width:100%;max-width:none;overflow:visible}.column-editor__title-row,.column-editor__settings-row{min-width:0}.column-editor__title-row{display:grid;grid-template-columns:minmax(0,1fr) auto;align-items:center;gap:.45rem}.column-editor__settings-row{display:grid;grid-template-columns:2rem minmax(7.25rem,1fr) minmax(6.35rem,auto);align-items:center;gap:.45rem;width:100%}.column-sort-indicator{margin-left:.35rem;opacity:.8}.column-icon-button{width:2rem;min-width:2rem;height:2rem;display:inline-grid;place-items:center;padding:0;border:1px solid color-mix(in srgb,var(--accent) 24%,var(--border-color));border-radius:999px;background:color-mix(in srgb,var(--surface-0) 94%,transparent);color:inherit;cursor:pointer}.column-icon-button:disabled{cursor:default;opacity:.45}.column-width-controls{display:inline-flex;align-items:center;justify-self:end;gap:.15rem;min-width:6.35rem;flex:0 0 auto}.column-width-value{min-width:1.75rem;text-align:center;color:var(--text-muted);font-size:.7rem;font-weight:700}.column-name-input{min-width:0;width:100%;max-width:100%;padding:0;border:0;margin:0;background:transparent;color:inherit;font:inherit;outline:none;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}.property-type-select{min-width:7.25rem;min-height:2rem;padding:.35rem .55rem;border:1px solid color-mix(in srgb,var(--accent) 24%,var(--border-color));border-radius:.4rem;background:var(--surface-0);color:var(--text-main);font:inherit;font-size:.78rem}.column-editor input[type=checkbox]{margin:0;accent-color:var(--accent)}.hidden-column-picker{display:flex;align-items:center;gap:.5rem;color:var(--text-main);font-size:.85rem;font-weight:600}.hidden-column-picker select{min-height:2.5rem;min-width:12rem;padding:.55rem .8rem;border:1px solid var(--border-color);border-radius:999px;background:var(--surface-0);color:var(--text-main);font:inherit}.row-action-button,.row-action-menu__trigger,.row-action-menu__item{border:1px solid var(--border-color);background:var(--surface-0);color:var(--text-main);font:inherit}.row-action-menu{position:relative}.row-action-menu__trigger{width:2.25rem;min-height:2.25rem;display:inline-grid;place-items:center;padding:0;border-radius:999px;background:var(--surface-0);color:var(--text-main);cursor:pointer}.row-action-menu__panel{position:fixed;z-index:40;min-width:10rem;display:grid;gap:.25rem;padding:.4rem;border:1px solid var(--border-color);border-radius:.85rem;background:var(--surface-3);box-shadow:var(--shadow-soft)}.row-action-menu__item{min-height:2.15rem;display:inline-flex;align-items:center;gap:.55rem;padding:.45rem .7rem;border-radius:.7rem;cursor:pointer;text-align:left}.row-action-menu__label{white-space:nowrap}.row-action-button__icon{display:inline-flex;align-items:center;justify-content:center;min-width:1.1rem;line-height:1}.row-action-button__icon.material-symbol-icon{font-size:1.15rem}.selection-column{width:3rem;min-width:3rem;text-align:center}.row-selection-checkbox{width:1rem;height:1rem;margin:0;accent-color:var(--accent)}.row-action-button:disabled,.row-action-menu__item:disabled{cursor:progress;opacity:.65}.chill-table tbody tr:last-child td{border-bottom:0}.empty-state,.empty-row{color:var(--text-muted)}.error-state{color:var(--danger)}.empty-state{padding:1rem 1.25rem;border-radius:1rem;background:var(--surface-2);border:1px dashed var(--border-color)}:root[data-theme=dark] .layout-button{background:#09131a94}.empty-row{text-align:center;padding:1.5rem}@media(max-width:720px){.table-header{align-items:stretch;flex-direction:column}.table-header__title-row{width:100%}.table-header__mobile-controls{display:flex}.table-header__task-close{display:inline-grid;flex:0 0 auto}.table-search{width:100%;flex:none}.table-search:not(.is-open){display:none}.table-search__input{width:100%;min-width:0}.table-search__icon-button--mobile{display:inline-grid}.chill-table{min-width:var(--chill-table-mobile-min-width, var(--chill-table-min-width, 100%));table-layout:auto}.chill-table th:not(.action-column):not(.selection-column),.chill-table td.data-cell,col.data-column{min-width:20vw;max-width:70vw}}\n"] }]
        }], ctorParameters: () => [], propDecorators: { handleDocumentClick: [{
                type: HostListener,
                args: ['document:click']
            }], handleWindowResize: [{
                type: HostListener,
                args: ['window:resize']
            }], handleWindowScroll: [{
                type: HostListener,
                args: ['window:scroll']
            }] } });

const DEFAULT_VIEW_CODE = 'default';
const DEFAULT_PAGE_SIZE = 20;
const SERVER_PAGE_WINDOW_SIZE = 4;
const ENTITY_NOTIFICATION_IGNORE_WINDOW_MS = 1000;
const ATTACHMENT_ENTITY_CHILL_TYPE = 'ChillSharp.Attachment.Model.Attachment';
const ATTACHMENT_QUERY_CHILL_TYPE = 'ChillSharp.Attachment.Model.AttachmentQuery';
class CrudPageComponentConfiguration {
    constructor() {
        this.chillType = '';
        this.disableAdd = false;
        this.disableCreate = false;
        this.disableEdit = false;
        this.disableInlineEdit = false;
        this.disableDelete = false;
    }
}
class CrudPageComponent {
    constructor() {
        //#region Service injection
        this.chill = inject(ChillService);
        this.dialog = inject(WorkspaceDialogService);
        this.workspace = inject(WorkspaceService);
        //#endregion
        //#region Component inputs
        this.selectionEnabled = input(false);
        this.multipleSelection = input(false);
        this.initialSelectedEntity = input(null);
        this.initialSelectedEntities = input([]);
        this.showTableHeader = input(true);
        this.showMobileTaskClose = input(false);
        this.componentConfiguration = input(null);
        //#endregion
        //#region Component state
        this.isLoadingSchemaList = signal(true);
        this.isLoadingSchema = signal(false);
        this.isSearching = signal(false);
        this.isSaving = signal(false);
        this.errorMessage = signal('');
        this.querySchemas = signal([]);
        this.selectedQueryType = signal('');
        this.querySchema = signal(null);
        this.resultSchema = signal(null);
        this.queryModel = signal(null);
        this.results = signal([]);
        this.selectedEntityKeys = signal([]);
        this.selectedViewCode = signal(DEFAULT_VIEW_CODE);
        this.serverWindowStartPage = signal(1);
        this.hasMoreServerPages = signal(false);
        this.normalizedConfiguration = computed(() => this.normalizeComponentConfiguration(this.componentConfiguration()));
        this.readonlyQueryPropertyNames = computed(() => [
            ...Object.keys(this.defaultQueryValues()),
            ...Object.keys(this.fixedQueryValues())
        ].filter((propertyName, index, values) => values.findIndex((value) => value === propertyName) === index));
        this.readonlyEntityPropertyNames = computed(() => Object.keys(this.fixedEntityValues()));
        this.currentPage = signal(1);
        this.pageSize = DEFAULT_PAGE_SIZE;
        this.validationErrorMessage = computed(() => {
            const messages = this.results()
                .flatMap((entity) => {
                const crudState = this.readChillStateObject(entity);
                if (!crudState || typeof crudState !== 'object' || Array.isArray(crudState)) {
                    return [];
                }
                const genericErrors = crudState['genericErrors'];
                return Array.isArray(genericErrors)
                    ? genericErrors.filter((message) => typeof message === 'string' && message.trim().length > 0)
                    : [];
            });
            return [...new Set(messages)].join(' ').trim();
        });
        this.validationFocus = computed(() => {
            for (const entity of this.pagedResults()) {
                const crudState = this.readChillStateObject(entity);
                const fieldNames = Object.keys(crudState.validationErrors ?? {});
                if (fieldNames.length > 0) {
                    return {
                        entityKey: this.readEntityKey(entity),
                        propertyName: fieldNames[0]
                    };
                }
            }
            return null;
        });
        this.currentFullTextSearch = computed(() => {
            const value = this.queryModel()?.properties?.['FullTextSearch'];
            return typeof value === 'string' ? value : '';
        });
        this.pagedResults = computed(() => {
            const start = (this.currentPage() - this.serverWindowStartPage()) * this.pageSize;
            return this.results().slice(start, start + this.pageSize);
        });
        this.rowActions = computed(() => [
            ...(this.isEditDisabled() ? [] : [{
                    icon: 'edit',
                    iconClass: 'material-symbol-icon',
                    ariaLabel: this.chill.T('E64B6037-B83A-406A-B5D6-CB5AA6E42FC6', 'Edit row', 'Modifica riga'),
                    disabled: (entity) => this.isSaving() || this.isDeletedEntity(entity),
                    handler: (entity) => this.openEntityDialog(entity)
                }]),
            ...this.createAttachmentDownloadRowActions(),
            ...(this.isDeleteDisabled() ? [] : [{
                    icon: 'delete',
                    iconClass: 'material-symbol-icon',
                    ariaLabel: this.chill.T('704B4EC7-C971-48C7-9439-E08C2F590992', 'Delete row', 'Elimina riga'),
                    disabled: (entity) => this.isSaving() || this.isDeletedEntity(entity),
                    handler: (entity) => this.markEntityDeleted(entity)
                }]),
            ...this.createAttachmentRowActions(),
            ...this.createRelationRowActions()
        ]);
        this.activeRowActions = computed(() => this.selectionEnabled() ? null : this.rowActions());
        this.selectionColumn = computed(() => this.selectionEnabled()
            ? {
                ariaLabel: this.chill.T('2EE7A0D9-CDE2-4F72-9BE1-B86A91D4B208', 'Select row', 'Seleziona riga'),
                isSelected: (entity) => this.isEntitySelected(entity),
                toggle: (entity, selected) => this.toggleSelectedEntity(entity, selected),
                disabled: () => this.isSaving()
            }
            : null);
    }
    //#endregion
    // #region Public Methods
    /**
     * Initializes the component by setting up initial state and loading query schemas.
     */
    ngOnInit() {
        this.selectedViewCode.set(this.normalizeViewCode(this.normalizedConfiguration().viewCode));
        this.selectedEntityKeys.set(this.readInitialSelectedEntityKeys());
        this.loadQuerySchemas();
    }
    /**
     * Determines if the selection can be confirmed based on the selection mode and selected entities.
     */
    canConfirmSelection() {
        return this.multipleSelection()
            ? this.selectedEntities().length > 0
            : !!this.selectedEntity();
    }
    /**
     * Returns the dialog result based on the selection mode.
     */
    dialogResult() {
        if (this.multipleSelection()) {
            return this.selectedEntities().map((entity) => this.cloneEntity(entity));
        }
        const entity = this.selectedEntity();
        return entity ? this.cloneEntity(entity) : null;
    }
    /**
     * Selects a query schema and loads the corresponding result schema.
     */
    selectQuerySchema(chillType) {
        const normalizedType = chillType.trim();
        this.selectedQueryType.set(normalizedType);
        this.errorMessage.set('');
        this.clearResultWindow();
        if (!normalizedType) {
            this.querySchema.set(null);
            this.resultSchema.set(null);
            this.queryModel.set(null);
            return;
        }
        this.loadSelectedSchema(normalizedType);
    }
    /**
     * Performs a search using the provided query form event.
     */
    search(event) {
        if (event.kind !== 'query') {
            return;
        }
        const query = this.normalizeQuery(event.value);
        this.queryModel.set(query);
        this.executeQuery(query, true, 1);
    }
    applyOrdering(event) {
        const currentQuery = this.queryModel();
        if (!currentQuery) {
            return;
        }
        const nextQuery = this.normalizeQuery({
            ...currentQuery,
            ordering: event.direction
                ? {
                    propertyName: event.propertyName,
                    direction: event.direction
                }
                : null
        });
        this.queryModel.set(nextQuery);
        this.executeQuery(nextQuery, true, 1);
    }
    applyFullTextSearch(value) {
        const currentQuery = this.queryModel();
        if (!currentQuery) {
            return;
        }
        const fullTextSearch = value.trim();
        const nextProperties = {
            ...(currentQuery.properties ?? {})
        };
        if (fullTextSearch.length > 0) {
            nextProperties['FullTextSearch'] = fullTextSearch;
        }
        else {
            delete nextProperties['FullTextSearch'];
        }
        const nextQuery = this.normalizeQuery({
            ...currentQuery,
            properties: nextProperties
        });
        this.queryModel.set(nextQuery);
        this.executeQuery(nextQuery, true, 1);
    }
    handleResultSchemaUpdated(schema) {
        this.resultSchema.set({
            ...schema,
            metadata: schema.metadata ? { ...schema.metadata } : undefined,
            properties: [...(schema.properties ?? [])]
        });
    }
    closeActiveTask() {
        const activeTaskId = this.workspace.activeTask()?.id;
        if (!activeTaskId) {
            return;
        }
        void this.workspace.closeTask(activeTaskId);
    }
    /**
     * Opens a search dialog for the current query schema.
     */
    openSearchDialog() {
        const schema = this.querySchema();
        if (!schema) {
            return;
        }
        void this.dialog.openDialog({
            title: this.chill.T('44972777-6760-4F48-BE39-B504E4467150', 'Search', 'Cerca'),
            component: ChillFormComponent,
            okLabel: this.chill.T('D513421E-1C00-425E-A89B-E736A440474F', 'Search', 'Cerca'),
            inputs: {
                schema,
                query: this.queryModel(),
                readonlyPropertyNames: this.readonlyQueryPropertyNames(),
                submitLabelGuid: 'D513421E-1C00-425E-A89B-E736A440474F',
                submitPrimaryDefaultText: 'Search',
                submitSecondaryDefaultText: 'Cerca',
                submitLabel: this.chill.T('D513421E-1C00-425E-A89B-E736A440474F', 'Search', 'Cerca'),
                showSchemaHeader: false,
                renderSubmitInsideForm: false,
                onSubmit: (event) => this.search(event),
                closeDialogOnSubmit: true
            }
        });
    }
    /**
     * Checks if the search dialog can be opened.
     */
    canOpenSearchDialog() {
        return !!this.querySchema() && !this.isLoadingSchema();
    }
    /**
     * Checks if a new entity can be added.
     */
    canAddEntity() {
        return !!this.resultSchema() && !this.isSaving() && !this.isLoadingSchema();
    }
    isAddDisabled() {
        return this.normalizedConfiguration().disableAdd === true || this.normalizedConfiguration().disableCreate === true;
    }
    isEditDisabled() {
        return this.normalizedConfiguration().disableEdit === true;
    }
    isInlineEditDisabled() {
        return this.normalizedConfiguration().disableInlineEdit === true;
    }
    isDeleteDisabled() {
        return this.normalizedConfiguration().disableDelete === true;
    }
    isAttachmentCrud() {
        return this.resultSchema()?.chillType?.trim() === ATTACHMENT_ENTITY_CHILL_TYPE;
    }
    canOpenAttachmentUploadDialog() {
        const target = this.readAttachmentTargetInfo();
        return this.isAttachmentCrud() && !!target.attachToChillType && !!target.attachToGuid;
    }
    /**
     * Checks if there are any pending entities that need to be saved.
     */
    hasPendingEntities() {
        return this.results().some((entity) => this.isPendingEntity(entity));
    }
    /**
     * Saves all pending entities by validating and committing them.
     */
    async savePendingEntities() {
        const schema = this.resultSchema();
        const pendingEntities = this.pendingEntities();
        if (!schema || pendingEntities.length === 0 || this.isSaving()) {
            return;
        }
        const entitiesToValidate = pendingEntities.filter((entity) => !this.isDeletedEntity(entity));
        const isValidationSuccessful = await this.validatePendingEntities(entitiesToValidate, schema);
        if (!isValidationSuccessful) {
            return;
        }
        this.isSaving.set(true);
        this.errorMessage.set('');
        this.updatePendingStatuses(pendingEntities, {
            status: 'saving'
        });
        const removableDraftKeys = new Set(pendingEntities
            .filter((entity) => this.isDraftEntity(entity) && this.isDeletedEntity(entity))
            .map((entity) => this.readEntityKey(entity))
            .filter((entityKey) => entityKey.length > 0));
        const chunkOperations = this.buildChunkOperations(pendingEntities.filter((entity) => !removableDraftKeys.has(this.readEntityKey(entity))), schema);
        try {
            if (chunkOperations.length > 0) {
                await firstValueFrom(this.chill.chunk(chunkOperations));
            }
            const successfulEntityKeys = new Set(pendingEntities
                .map((entity) => this.readEntityKey(entity))
                .filter((entityKey) => entityKey.length > 0));
            this.results.update((current) => current.filter((entity) => !successfulEntityKeys.has(this.readEntityKey(entity))));
            this.errorMessage.set('');
            this.refreshResults();
        }
        catch (error) {
            const errorMessage = this.chill.formatError(error);
            const failedEntityKeys = new Set(pendingEntities
                .map((entity) => this.readEntityKey(entity))
                .filter((entityKey) => entityKey.length > 0));
            this.results.update((current) => current.map((entity) => failedEntityKeys.has(this.readEntityKey(entity))
                ? this.withCrudState(entity, {
                    status: this.isDeletedEntity(entity) ? 'deleted' : 'error',
                    errorMessage
                })
                : entity));
            this.errorMessage.set(errorMessage);
        }
        finally {
            this.isSaving.set(false);
        }
    }
    /**
     * Checks if navigation to the previous page is possible.
     */
    canGoToPreviousPage() {
        return this.currentPage() > 1;
    }
    /**
     * Checks if navigation to the next page is possible.
     */
    canGoToNextPage() {
        return this.hasLoadedPage(this.currentPage() + 1) || this.hasMoreServerPages();
    }
    /**
     * Navigates to the previous page.
     */
    goToPreviousPage() {
        if (!this.canGoToPreviousPage()) {
            return;
        }
        const previousPage = this.currentPage() - 1;
        if (this.hasLoadedPage(previousPage)) {
            this.currentPage.set(previousPage);
            return;
        }
        const query = this.queryModel();
        if (!query) {
            return;
        }
        this.executeQuery(this.normalizeQuery(query), true, previousPage);
    }
    /**
     * Navigates to the next page.
     */
    goToNextPage() {
        if (!this.canGoToNextPage()) {
            return;
        }
        const nextPage = this.currentPage() + 1;
        if (this.hasLoadedPage(nextPage)) {
            this.currentPage.set(nextPage);
            return;
        }
        const query = this.queryModel();
        if (!query) {
            return;
        }
        this.executeQuery(this.normalizeQuery(query), true, nextPage);
    }
    /**
     * Returns the label for the current page.
     */
    pageLabel() {
        const pageText = this.chill.T('A28A7E16-5B47-4B5D-A5CF-54BDEFF43073', 'Page', 'Pagina');
        return `${pageText} ${this.currentPage()}`;
    }
    /**
     * Clears the error message.
     */
    clearErrorMessage() {
        this.errorMessage.set('');
    }
    /**
     * Opens a dialog for editing or adding an entity.
     */
    openEntityDialog(entity) {
        const schema = this.resultSchema();
        if (!schema) {
            return;
        }
        const isDraft = this.isNewEntity(entity);
        void (async () => {
            const result = await this.dialog.openDialog({
                title: isDraft
                    ? this.chill.T('23A5536E-8A94-4469-977C-D3BB57E5E621', 'Add', 'Aggiungi')
                    : this.chill.T('E64B6037-B83A-406A-B5D6-CB5AA6E42FC6', 'Edit', 'Modifica'),
                component: ChillFormComponent,
                okLabel: isDraft
                    ? this.chill.T('D7EA89E2-4AF2-455A-8FA9-33540E61D7C5', 'Done', 'Fine')
                    : this.chill.T('62953302-B951-4FD1-BD08-4B7649A91BAF', 'Update', 'Aggiorna'),
                inputs: {
                    schema,
                    entity: this.prepareDialogEntity(entity, schema),
                    readonlyPropertyNames: this.readonlyEntityPropertyNames(),
                    submitLabelGuid: isDraft ? 'D7EA89E2-4AF2-455A-8FA9-33540E61D7C5' : '62953302-B951-4FD1-BD08-4B7649A91BAF',
                    submitPrimaryDefaultText: isDraft ? 'Done' : 'Update',
                    submitSecondaryDefaultText: isDraft ? 'Fine' : 'Aggiorna',
                    submitLabel: isDraft
                        ? this.chill.T('D7EA89E2-4AF2-455A-8FA9-33540E61D7C5', 'Done', 'Fine')
                        : this.chill.T('62953302-B951-4FD1-BD08-4B7649A91BAF', 'Update', 'Aggiorna'),
                    showSchemaHeader: false,
                    renderSubmitInsideForm: false,
                    closeDialogOnSubmit: false
                }
            });
            if (result.status !== 'confirmed') {
                return;
            }
            const savedEntity = result.value;
            if (!savedEntity) {
                if (isDraft) {
                    this.removeIsNewEntity(entity);
                }
                else {
                    this.refreshResults();
                }
                return;
            }
            const nextEntity = this.prepareSavedDialogEntity(savedEntity, schema);
            this.replaceEntity(nextEntity, this.findEntityByKey(entity) ?? entity);
        })();
    }
    loadQuerySchemas() {
        this.isLoadingSchemaList.set(true);
        this.errorMessage.set('');
        this.chill.getSchemaList().subscribe({
            next: (schemaList) => {
                const querySchemas = schemaList
                    .filter((item) => this.isQuerySchema(item))
                    .sort((left, right) => this.schemaLabel(left).localeCompare(this.schemaLabel(right)));
                this.querySchemas.set(querySchemas);
                this.isLoadingSchemaList.set(false);
                if (querySchemas.length === 0) {
                    this.errorMessage.set(this.chill.T('9A6E134E-44BF-4FF4-97DF-EE3041286395', 'No query schemas are available.', 'Nessuno schema di query disponibile.'));
                    return;
                }
                const configuredQueryType = this.configuredQueryChillType();
                const configuredResultType = this.configuredResultChillType();
                const initialSchema = querySchemas.find((schema) => schema.chillType?.trim() === configuredQueryType)
                    ?? querySchemas.find((schema) => schema.relatedChillType?.trim() === configuredResultType)
                    ?? null;
                if (!initialSchema) {
                    const backupQuerySchema = this.createBackupQuerySchema(configuredResultType, this.selectedViewCode());
                    if (!backupQuerySchema) {
                        this.errorMessage.set(this.chill.T('5C237896-63A2-4E59-809A-12598DC24882', 'No query schemas are available.', 'Nessuno schema di query disponibile.'));
                        return;
                    }
                    this.selectedQueryType.set(backupQuerySchema.chillType?.trim() ?? '');
                    this.querySchema.set(backupQuerySchema);
                    this.queryModel.set(this.createQueryModel(backupQuerySchema));
                    this.clearResultWindow();
                    void this.loadResultSchema(configuredResultType, backupQuerySchema.chillViewCode?.trim() || this.selectedViewCode());
                    return;
                }
                this.selectQuerySchema(initialSchema.chillType?.trim() ?? '');
            },
            error: (error) => {
                this.querySchemas.set([]);
                this.errorMessage.set(this.chill.formatError(error));
                this.isLoadingSchemaList.set(false);
            }
        });
    }
    loadSelectedSchema(chillType) {
        this.isLoadingSchema.set(true);
        const viewCode = this.selectedViewCode();
        this.chill.getSchema(chillType, viewCode).subscribe({
            next: (schema) => {
                if (!schema) {
                    this.querySchema.set(null);
                    this.resultSchema.set(null);
                    this.queryModel.set(null);
                    this.clearResultWindow();
                    this.errorMessage.set(this.chill.T('80085620-C926-4F8C-820D-672EE1E7B4AF', 'The selected query schema is unavailable.', 'Lo schema di query selezionato non è disponibile.'));
                    this.isLoadingSchema.set(false);
                    return;
                }
                this.querySchema.set(schema);
                this.queryModel.set(this.createQueryModel(schema));
                void this.loadResultSchema(this.configuredResultChillType() || schema.queryRelatedChillType?.trim() || '', schema.chillViewCode?.trim() || viewCode);
            },
            error: (error) => {
                this.querySchema.set(null);
                this.resultSchema.set(null);
                this.queryModel.set(null);
                this.clearResultWindow();
                this.errorMessage.set(this.chill.formatError(error));
                this.isLoadingSchema.set(false);
            }
        });
    }
    /**
     * Adds a new draft entity to the results.
     */
    add() {
        const schema = this.resultSchema();
        if (!schema) {
            return;
        }
        this.errorMessage.set('');
        const isNew = true;
        const draftEntity = {
            guid: crypto.randomUUID(),
            chillState: {
                isNew: isNew,
                isDeleting: false,
                status: 'draft',
                dirtyProperties: []
            },
            chillType: schema.chillType?.trim() ?? '',
            properties: {
                ...this.defaultEntityValues(),
                ...this.fixedEntityValues()
            }
        };
        this.results.update((current) => [...current, this.prepareEntityForSchema(draftEntity, schema, isNew)]);
        if (this.selectionEnabled()) {
            this.toggleSelectedEntity(draftEntity, true);
        }
    }
    /**
     * Handles inline cell edit commits from the table.
     */
    async handleInlineCellEdit(event) {
        const schema = this.resultSchema();
        if (!schema) {
            return;
        }
        const updatedEntity = this.mergeEntityProperty(event.entity, event.propertyName, event.value, schema);
        if (this.isNewEntity(event.entity)) {
            const nextEntity = this.withCrudState(updatedEntity, {
                status: 'draft',
                isNew: true,
                dirtyProperties: this.normalizeDirtyProperties(event.dirtyProperties),
                validationErrors: null,
                genericErrors: null
            });
            this.replaceEntity(nextEntity, event.entity);
            await this.autocompleteAndValidateEntity(nextEntity);
            return;
        }
        this.errorMessage.set('');
        const nextEntity = this.withCrudState(updatedEntity, {
            status: 'dirty',
            dirtyProperties: this.normalizeDirtyProperties(event.dirtyProperties),
            validationErrors: null,
            genericErrors: null
        });
        this.replaceEntity(nextEntity, event.entity);
        await this.autocompleteAndValidateEntity(nextEntity);
    }
    // #endregion
    // #region Helper Methods
    async loadResultSchema(relatedChillType, chillViewCode) {
        if (!relatedChillType) {
            this.resultSchema.set(null);
            this.errorMessage.set(this.chill.T('C187D4C0-DB14-476E-9A40-F6D086C2D7A5', 'The selected query schema does not define QueryRelatedChillType.', 'Lo schema di query selezionato non definisce QueryRelatedChillType.'));
            this.isLoadingSchema.set(false);
            return;
        }
        this.chill.getSchema(relatedChillType, chillViewCode).subscribe({
            next: (schema) => {
                this.resultSchema.set(schema);
                if (!schema) {
                    this.errorMessage.set(this.chill.T('A6A6949E-F0D4-42F5-A8AE-E15B1B174084', 'The result schema is unavailable.', 'Lo schema dei risultati non è disponibile.'));
                }
                if (schema && this.queryModel()) {
                    this.isLoadingSchema.set(false);
                    this.refreshResults();
                    return;
                }
                this.isLoadingSchema.set(false);
            },
            error: (error) => {
                this.resultSchema.set(null);
                this.errorMessage.set(this.chill.formatError(error));
                this.isLoadingSchema.set(false);
            }
        });
    }
    markEntityDeleted(entity) {
        this.errorMessage.set('');
        this.results.update((current) => current.map((candidate) => this.readEntityKey(candidate) === this.readEntityKey(entity)
            ? this.withCrudState(candidate, {
                status: 'deleted'
            })
            : candidate));
    }
    createQueryModel(schema) {
        return this.normalizeQuery({
            chillType: this.configuredQueryChillType() || schema.chillType?.trim() || this.selectedQueryType(),
            properties: {
                ...this.defaultQueryValues(),
                ...this.fixedQueryValues()
            }
        });
    }
    normalizeQuery(query) {
        const resultSchema = this.resultSchema();
        const ordering = query.ordering;
        return {
            ...query,
            chillType: query.chillType?.trim() || this.configuredQueryChillType() || this.querySchema()?.chillType?.trim() || this.selectedQueryType(),
            properties: {
                ...(query.properties ?? {}),
                ...this.defaultQueryValues(),
                ...this.fixedQueryValues()
            },
            ordering: ordering?.propertyName?.trim()
                ? {
                    propertyName: ordering.propertyName.trim(),
                    direction: ordering.direction === 'DESC' ? 'DESC' : 'ASC'
                }
                : null,
            pagination: this.buildPaginationForPage(this.currentPage()),
            resultProperties: resultSchema?.properties?.map((property) => ({ PropertyName: property.name })) ?? []
        };
    }
    createBackupQuerySchema(entityChillType, viewCode) {
        const normalizedEntityChillType = entityChillType.trim();
        if (!normalizedEntityChillType) {
            return null;
        }
        return {
            chillType: normalizedEntityChillType,
            chillViewCode: this.normalizeViewCode(viewCode),
            displayName: normalizedEntityChillType,
            queryRelatedChillType: normalizedEntityChillType,
            metadata: {},
            properties: [
                {
                    name: 'Guid',
                    displayName: 'Guid',
                    propertyType: CHILL_PROPERTY_TYPE$1.Guid,
                    isNullable: true,
                    metadata: {}
                },
                {
                    name: 'FullTextSearch',
                    displayName: 'FullTextSearch',
                    propertyType: CHILL_PROPERTY_TYPE$1.String,
                    isNullable: true,
                    metadata: {}
                }
            ]
        };
    }
    normalizeCreateEntity(entity, schema) {
        const preparedEntity = this.prepareEntityForSchema(entity, schema);
        const entityChillType = this.readStringValue(preparedEntity['chillType']);
        const sanitizedEntity = this.stripSchemaPropertiesFromRoot(preparedEntity, schema);
        const { chillState: _chillState, ...normalizedEntity } = sanitizedEntity;
        return {
            ...normalizedEntity,
            chillType: entityChillType || schema.chillType?.trim() || this.querySchema()?.queryRelatedChillType?.trim() || '',
            properties: {
                ...(preparedEntity.properties ?? {})
            }
        };
    }
    stripSchemaPropertiesFromRoot(entity, schema) {
        const nextEntity = { ...entity };
        const protectedNames = new Set(['guid', 'Guid', 'chillType', 'ChillType', 'chillState', 'ChillState', 'properties', 'Properties']);
        for (const property of schema.properties ?? []) {
            const propertyName = property.name?.trim();
            if (!propertyName || protectedNames.has(propertyName)) {
                continue;
            }
            delete nextEntity[propertyName];
            const pascalCaseName = propertyName.length > 0
                ? `${propertyName[0].toUpperCase()}${propertyName.slice(1)}`
                : propertyName;
            if (!protectedNames.has(pascalCaseName)) {
                delete nextEntity[pascalCaseName];
            }
        }
        return nextEntity;
    }
    buildChunkOperations(entities, schema) {
        if (entities.length === 0) {
            return [];
        }
        const operations = [{
                Index: 0,
                Verb: 'transaction'
            }];
        entities.forEach((entity, index) => {
            const normalizedEntity = this.normalizeCreateEntity(entity, schema);
            const verb = this.isDeletedEntity(entity)
                ? 'delete'
                : this.isNewEntity(entity)
                    ? 'create'
                    : 'update';
            operations.push({
                Index: index + 1,
                Verb: verb,
                Entity: normalizedEntity
            });
        });
        operations.push({
            Index: operations.length,
            Verb: 'commit'
        });
        return operations;
    }
    refreshResults() {
        const query = this.queryModel();
        if (!query) {
            return;
        }
        this.executeQuery(this.normalizeQuery(query), false, this.currentPage());
    }
    clearResultWindow() {
        this.results.set([]);
        this.currentPage.set(1);
        this.serverWindowStartPage.set(1);
        this.hasMoreServerPages.set(false);
    }
    executeQuery(query, preservePendingEntitiesOnError, targetPage) {
        const normalizedTargetPage = Math.max(1, targetPage);
        const windowStartPage = this.calculateServerWindowStartPage(normalizedTargetPage);
        const pagedQuery = {
            ...query,
            pagination: this.buildPaginationForPage(normalizedTargetPage)
        };
        this.isSearching.set(true);
        this.errorMessage.set('');
        this.chill.query(pagedQuery).subscribe({
            next: (response) => {
                const serverEntities = this.extractEntities(response);
                if (serverEntities.length === 0 && normalizedTargetPage > 1) {
                    this.hasMoreServerPages.set(false);
                    this.isSearching.set(false);
                    return;
                }
                this.results.set(this.mergeWithDraftEntities(serverEntities));
                this.serverWindowStartPage.set(windowStartPage);
                this.currentPage.set(normalizedTargetPage);
                this.hasMoreServerPages.set(serverEntities.length >= this.serverWindowEntityCount());
                this.isSearching.set(false);
            },
            error: (error) => {
                if (preservePendingEntitiesOnError) {
                    this.results.set(this.pendingEntities());
                }
                this.errorMessage.set(this.chill.formatError(error));
                this.currentPage.set(normalizedTargetPage);
                this.serverWindowStartPage.set(windowStartPage);
                this.hasMoreServerPages.set(false);
                this.isSearching.set(false);
            }
        });
    }
    buildPaginationForPage(page) {
        const windowStartPage = this.calculateServerWindowStartPage(page);
        return {
            Page: Math.floor((windowStartPage - 1) / SERVER_PAGE_WINDOW_SIZE) + 1,
            PageResults: this.serverWindowEntityCount()
        };
    }
    calculateServerWindowStartPage(page) {
        return Math.floor((Math.max(1, page) - 1) / SERVER_PAGE_WINDOW_SIZE) * SERVER_PAGE_WINDOW_SIZE + 1;
    }
    serverWindowEntityCount() {
        return this.pageSize * SERVER_PAGE_WINDOW_SIZE;
    }
    hasLoadedPage(page) {
        if (page < this.serverWindowStartPage()) {
            return false;
        }
        const start = (page - this.serverWindowStartPage()) * this.pageSize;
        return start >= 0 && start < this.results().length;
    }
    removeIsNewEntity(entity) {
        const isNew = this.isNewEntity(entity);
        if (!isNew) {
            return;
        }
        this.results.update((current) => current.filter((candidate) => this.isNewEntity(candidate) && candidate.guid !== entity.guid));
    }
    isDraftEntity(entity) {
        return this.isNewEntity(entity);
    }
    pendingEntities() {
        return this.results().filter((entity) => this.isPendingEntity(entity));
    }
    mergeWithDraftEntities(serverEntities) {
        const pendingEntities = this.pendingEntities();
        const persistedPendingEntityMap = new Map(pendingEntities
            .filter((entity) => !this.isDraftEntity(entity))
            .map((entity) => [this.readEntityKey(entity), entity]));
        return [
            ...serverEntities.map((entity) => persistedPendingEntityMap.get(this.readEntityKey(entity)) ?? entity),
            ...pendingEntities.filter((entity) => this.isDraftEntity(entity))
        ];
    }
    updatePendingStatuses(entities, state) {
        const entityKeys = new Set(entities.map((entity) => this.readEntityKey(entity)).filter((entityKey) => entityKey.length > 0));
        this.results.update((current) => current.map((entity) => entityKeys.has(this.readEntityKey(entity))
            ? this.withCrudState(entity, state)
            : entity));
    }
    isPendingEntity(entity) {
        const status = this.readCrudStatus(entity);
        return status === 'draft' || status === 'dirty' || status === 'deleted' || status === 'error';
    }
    isDeletedEntity(entity) {
        return this.readCrudStatus(entity) === 'deleted';
    }
    readEntityKey(entity) {
        return this.readStringValue(entity['guid'])
            || this.readStringValue(entity['Guid']);
    }
    async openAttachmentUploadDialog() {
        const target = this.readAttachmentTargetInfo();
        if (!target.attachToChillType || !target.attachToGuid) {
            return;
        }
        const { AttachmentUploadDialogComponent } = await Promise.resolve().then(function () { return attachmentUploadDialog_component; });
        const result = await this.dialog.openDialog({
            title: this.chill.T('D31B58D6-32F2-443B-AD18-7BFA76AF2FB6', 'Add attachment', 'Aggiungi allegato'),
            component: AttachmentUploadDialogComponent,
            okLabel: this.chill.T('D31B58D6-32F2-443B-AD18-7BFA76AF2FB6', 'Add attachment', 'Aggiungi allegato'),
            inputs: {
                attachToChillType: target.attachToChillType,
                attachToGuid: target.attachToGuid
            }
        });
        if (result.status === 'confirmed') {
            this.refreshResults();
        }
    }
    readEntityChillType(entity) {
        return this.readStringValue(entity['chillType'])
            || this.readStringValue(entity['ChillType'])
            || this.resultSchema()?.chillType?.trim()
            || '';
    }
    withCrudState(entity, state) {
        const nextState = this.sanitizeCrudState({
            ...this.readChillStateObject(entity),
            ...state
        });
        return {
            ...entity,
            chillState: {
                ...(this.readChillStateObject(entity) ?? {}),
                ...nextState,
                isNew: nextState.isNew,
                isDeleting: nextState.status === 'deleted'
            }
        };
    }
    cloneEntity(entity) {
        return {
            ...entity,
            properties: {
                ...(entity.properties ?? {})
            },
            ...(entity.chillState
                ? {
                    chillState: { ...entity.chillState }
                }
                : {}),
            chillState: this.sanitizeCrudState({
                ...this.readChillStateObject(entity),
                isDeleting: this.readChillStateObject(entity).status === 'deleted'
            })
        };
    }
    readCrudStatus(entity) {
        const status = this.readChillStateObject(entity).status;
        return status === 'pristine' || status === 'draft' || status === 'dirty' || status === 'saving' || status === 'deleted' || status === 'error'
            ? status
            : '';
    }
    mergeEntityProperty(entity, propertyName, value, schema) {
        const currentProperties = {
            ...(entity.properties ?? {})
        };
        return {
            ...entity,
            properties: {
                ...currentProperties,
                [propertyName]: this.chill.toJsonValue(schema, propertyName, value)
            }
        };
    }
    async autocompleteAndValidateEntity(entity) {
        const schema = this.resultSchema();
        if (!schema) {
            return;
        }
        let currentEntity = entity;
        try {
            const autocompletedEntity = await firstValueFrom(this.chill.autocomplete(this.normalizeCreateEntity(entity, schema)));
            const autocompletedFields = this.extractValidationEntityFields(autocompletedEntity, schema);
            if (Object.keys(autocompletedFields).length > 0) {
                currentEntity = this.withCrudState({
                    ...currentEntity,
                    properties: {
                        ...(currentEntity.properties ?? {}),
                        ...autocompletedFields
                    }
                }, this.readChillStateObject(currentEntity));
                this.replaceEntity(currentEntity, entity);
            }
        }
        catch {
            currentEntity = this.findEntityByKey(currentEntity) ?? currentEntity;
        }
        const updatedEntity = this.withCrudState(currentEntity, {
            ...this.readChillStateObject(currentEntity),
            validationErrors: null,
            genericErrors: null
        });
        this.replaceEntity(updatedEntity, currentEntity);
    }
    async validatePendingEntities(entities, schema) {
        if (entities.length === 0) {
            return true;
        }
        let hasErrors = false;
        let firstInvalidIndex = -1;
        for (const [index, entity] of entities.entries()) {
            try {
                const validationErrors = await firstValueFrom(this.chill.validate(this.normalizeCreateEntity(entity, schema)));
                const partitionedErrors = this.partitionValidationErrors(validationErrors, schema);
                const hasEntityErrors = Object.keys(partitionedErrors.fieldErrors).length > 0 || partitionedErrors.genericErrors.length > 0;
                if (hasEntityErrors) {
                    hasErrors = true;
                    if (firstInvalidIndex < 0) {
                        firstInvalidIndex = index;
                    }
                }
                const nextEntity = this.withCrudState(entity, {
                    ...this.readChillStateObject(entity),
                    validationErrors: Object.keys(partitionedErrors.fieldErrors).length > 0 ? partitionedErrors.fieldErrors : null,
                    genericErrors: partitionedErrors.genericErrors.length > 0 ? partitionedErrors.genericErrors : null
                });
                this.replaceEntity(nextEntity, entity);
            }
            catch (error) {
                hasErrors = true;
                if (firstInvalidIndex < 0) {
                    firstInvalidIndex = index;
                }
                const nextEntity = this.withCrudState(entity, {
                    ...this.readChillStateObject(entity),
                    genericErrors: [this.chill.formatError(error)],
                    validationErrors: null
                });
                this.replaceEntity(nextEntity, entity);
            }
        }
        if (firstInvalidIndex >= 0) {
            const firstInvalidEntity = entities[firstInvalidIndex];
            const absoluteIndex = this.results().findIndex((entity) => this.readEntityKey(entity) === this.readEntityKey(firstInvalidEntity));
            if (absoluteIndex >= 0) {
                this.currentPage.set(this.serverWindowStartPage() + Math.floor(absoluteIndex / this.pageSize));
            }
        }
        return !hasErrors;
    }
    replaceEntity(nextEntity, previousEntity) {
        const previousEntityKey = this.readEntityKey(previousEntity);
        this.results.update((current) => current.map((entity) => this.readEntityKey(entity) === previousEntityKey ? nextEntity : entity));
    }
    findEntityByKey(entity) {
        const entityKey = this.readEntityKey(entity);
        return this.results().find((candidate) => this.readEntityKey(candidate) === entityKey) ?? null;
    }
    // private readChillStateObject(entity: ChillEntity): ChillState {
    //   const currentState = this.readChillStateValue(entity);
    //   const chillState = this.readChillStateObject(entity);
    //   const isNew = chillState?.['isNew'] === true;
    //   const isDeleting = chillState?.['isDeleting'] === true;
    //   if (currentState && typeof currentState === 'object' && !Array.isArray(currentState)) {
    //     const typedState = currentState as ChillState;
    //     const resolvedIsNew = typedState.isNew === true || isNew;
    //     const resolvedStatus = typedState.status === 'pristine' || typedState.status === 'draft' || typedState.status === 'dirty' || typedState.status === 'saving' || typedState.status === 'deleted' || typedState.status === 'error'
    //       ? typedState.status
    //       : (resolvedIsNew ? 'draft' : isDeleting ? 'deleted' : 'pristine');
    //     return {
    //       isNew: resolvedIsNew,
    //       status: resolvedStatus,
    //       errorMessage: typedState.errorMessage ?? null,
    //       validationErrors: typedState.validationErrors ?? null,
    //       genericErrors: typedState.genericErrors ?? null,
    //       dirtyProperties: Array.isArray(typedState.dirtyProperties)
    //         ? typedState.dirtyProperties.filter((propertyName): propertyName is string => typeof propertyName === 'string' && propertyName.trim().length > 0)
    //         : null
    //     };
    //   }
    //   return {
    //     isNew,
    //     status: isNew ? 'draft' : isDeleting ? 'deleted' : 'pristine',
    //     dirtyProperties: isNew ? [] : null
    //   };
    // }
    isNewEntity(entity) {
        return this.readChillStateObject(entity)?.isNew ?? false;
    }
    prepareEntityForSchema(entity, schema, isNew = false) {
        const clonedEntity = this.cloneEntity(entity);
        const nextProperties = {
            ...(isNew ? this.defaultEntityValues() : {}),
            ...(isNew ? this.fixedEntityValues() : {}),
            ...(clonedEntity.properties ?? {})
        };
        for (const property of schema.properties ?? []) {
            if (property.name in nextProperties) {
                continue;
            }
            nextProperties[property.name] = this.readEntityPropertyValue(clonedEntity, property.name) ?? null;
            if (!property.isNullable && nextProperties[property.name] === null) {
                if (property.propertyType == CHILL_PROPERTY_TYPE$1.Boolean)
                    nextProperties[property.name] = false;
                else if (property.propertyType == CHILL_PROPERTY_TYPE$1.Integer)
                    nextProperties[property.name] = 0;
                else if (property.propertyType == CHILL_PROPERTY_TYPE$1.Decimal)
                    nextProperties[property.name] = 0;
                else if (property.propertyType == CHILL_PROPERTY_TYPE$1.String)
                    nextProperties[property.name] = '';
            }
        }
        return {
            ...clonedEntity,
            chillType: this.readStringValue(clonedEntity['chillType']) || schema.chillType?.trim() || this.querySchema()?.queryRelatedChillType?.trim() || '',
            properties: nextProperties
        };
    }
    readEntityPropertyValue(entity, propertyName) {
        const properties = entity.properties;
        if (properties && propertyName in properties) {
            return properties[propertyName];
        }
        if (propertyName in entity) {
            return entity[propertyName];
        }
        const pascalCaseName = propertyName.length > 0
            ? `${propertyName[0].toUpperCase()}${propertyName.slice(1)}`
            : propertyName;
        return entity[pascalCaseName];
    }
    sanitizeCrudState(state) {
        return Object.fromEntries(Object.entries(state).filter(([, value]) => value !== undefined));
    }
    normalizeServerEntity(entity) {
        return this.withCrudState(entity, {
            isNew: false,
            status: 'pristine',
            dirtyProperties: null,
            validationErrors: null,
            genericErrors: null,
            errorMessage: null,
            ignoreNotificationsUntil: null
        });
    }
    prepareDialogEntity(entity, schema) {
        const preparedEntity = this.prepareEntityForSchema(entity, schema);
        if (this.isNewEntity(entity)) {
            return this.withCrudState(preparedEntity, {
                ...this.readChillStateObject(preparedEntity),
                status: 'draft',
                dirtyProperties: this.normalizeDirtyProperties(this.readChillStateObject(preparedEntity).dirtyProperties),
                validationErrors: null,
                genericErrors: null,
                errorMessage: null,
                ignoreNotificationsUntil: null
            });
        }
        return this.withCrudState(preparedEntity, {
            ...this.readChillStateObject(preparedEntity),
            status: 'pristine',
            dirtyProperties: null,
            validationErrors: null,
            genericErrors: null,
            errorMessage: null,
            ignoreNotificationsUntil: null
        });
    }
    prepareSavedDialogEntity(entity, schema) {
        return this.withCrudState(this.normalizeServerEntity(this.prepareEntityForSchema(entity, schema)), {
            ignoreNotificationsUntil: Date.now() + ENTITY_NOTIFICATION_IGNORE_WINDOW_MS
        });
    }
    readChillStateObject(entity) {
        const chillState = entity['chillState'];
        return chillState && typeof chillState === 'object' && !Array.isArray(chillState)
            ? chillState
            : {};
    }
    normalizeDirtyProperties(propertyNames) {
        const normalizedPropertyNames = (propertyNames ?? [])
            .map((propertyName) => propertyName.trim())
            .filter((propertyName) => propertyName.length > 0);
        return normalizedPropertyNames.length > 0
            ? [...new Set(normalizedPropertyNames)]
            : null;
    }
    extractValidationEntityFields(source, schema) {
        const nextFields = {};
        for (const property of schema.properties ?? []) {
            const fieldName = property.name;
            const propertiesValue = source['properties'];
            if (propertiesValue && typeof propertiesValue === 'object' && !Array.isArray(propertiesValue) && fieldName in propertiesValue) {
                nextFields[fieldName] = propertiesValue[fieldName];
                continue;
            }
            const pascalPropertiesValue = source['Properties'];
            if (pascalPropertiesValue && typeof pascalPropertiesValue === 'object' && !Array.isArray(pascalPropertiesValue) && fieldName in pascalPropertiesValue) {
                nextFields[fieldName] = pascalPropertiesValue[fieldName];
            }
        }
        return nextFields;
    }
    partitionValidationErrors(errors, schema) {
        const propertyNameMap = new Map((schema.properties ?? [])
            .map((property) => property.name.trim())
            .filter((propertyName) => propertyName.length > 0)
            .map((propertyName) => [propertyName.toLowerCase(), propertyName]));
        const fieldErrors = {};
        const genericErrors = [];
        for (const error of errors) {
            const fieldName = typeof error.fieldName === 'string' ? error.fieldName.trim() : '';
            const message = typeof error.message === 'string' ? error.message.trim() : '';
            if (!message) {
                continue;
            }
            const resolvedFieldName = fieldName ? propertyNameMap.get(fieldName.toLowerCase()) : undefined;
            if (resolvedFieldName) {
                fieldErrors[resolvedFieldName] = fieldErrors[resolvedFieldName]
                    ? `${fieldErrors[resolvedFieldName]} ${message}`
                    : message;
                continue;
            }
            genericErrors.push(message);
        }
        return { fieldErrors, genericErrors };
    }
    extractEntities(response) {
        const candidates = [
            response,
            response['results'],
            response['entities'],
            response['items'],
            response['value'],
            response['data']
        ];
        for (const candidate of candidates) {
            const entities = this.toEntityArray(candidate);
            if (entities.length > 0) {
                return entities.map((entity) => this.normalizeServerEntity(entity));
            }
        }
        return [];
    }
    toEntityArray(value) {
        if (!Array.isArray(value)) {
            return [];
        }
        return value.filter((item) => this.isJsonObject(item));
    }
    isJsonObject(value) {
        return !!value && typeof value === 'object' && !Array.isArray(value);
    }
    readStringValue(value) {
        return typeof value === 'string'
            ? value.trim()
            : '';
    }
    configuredResultChillType() {
        return this.normalizedConfiguration().chillType?.trim() || '';
    }
    configuredQueryChillType() {
        return this.normalizedConfiguration().chillQuery?.trim() || '';
    }
    defaultEntityValues() {
        return this.resolveConfigRecord(this.normalizedConfiguration().defaultValues);
    }
    fixedEntityValues() {
        return this.resolveConfigRecord(this.normalizedConfiguration().fixedValues);
    }
    defaultQueryValues() {
        return this.resolveConfigRecord(this.normalizedConfiguration().defaultQueryValues);
    }
    fixedQueryValues() {
        return this.resolveConfigRecord(this.normalizedConfiguration().fixedQueryValues);
    }
    relations() {
        return this.normalizedConfiguration().relations ?? [];
    }
    normalizeComponentConfiguration(configuration) {
        const normalizedConfiguration = new CrudPageComponentConfiguration();
        if (!configuration) {
            return normalizedConfiguration;
        }
        normalizedConfiguration.chillType = this.readConfigString(configuration['chillType']);
        normalizedConfiguration.chillQuery = this.readConfigString(configuration['chillQuery']) || null;
        normalizedConfiguration.viewCode = this.readConfigString(configuration['viewCode']) || null;
        normalizedConfiguration.disableAdd = this.readConfigBoolean(configuration['disableAdd']);
        normalizedConfiguration.disableCreate = this.readConfigBoolean(configuration['disableCreate']);
        normalizedConfiguration.disableEdit = this.readConfigBoolean(configuration['disableEdit']);
        normalizedConfiguration.disableInlineEdit = this.readConfigBoolean(configuration['disableInlineEdit']);
        normalizedConfiguration.disableDelete = this.readConfigBoolean(configuration['disableDelete']);
        normalizedConfiguration.relationLabel = this.readRelationLabel(configuration['relationLabel']);
        normalizedConfiguration.defaultValues = this.readConfigRecord(configuration['defaultValues']);
        normalizedConfiguration.fixedValues = this.readConfigRecord(configuration['fixedValues']);
        normalizedConfiguration.fixedQueryValues = this.readConfigRecord(configuration['fixedQueryValues']);
        normalizedConfiguration.defaultQueryValues = this.readConfigRecord(configuration['defaultQueryValues']);
        normalizedConfiguration.relations = this.readRelationConfigurations(configuration.relations);
        return normalizedConfiguration;
    }
    readConfigString(value) {
        return typeof value === 'string'
            ? value.trim()
            : '';
    }
    readConfigBoolean(value) {
        return value === true;
    }
    readConfigRecord(value) {
        if (!value || typeof value !== 'object' || Array.isArray(value)) {
            return {};
        }
        return Object.fromEntries(Object.entries(value)
            .map(([key, entryValue]) => [key.trim(), entryValue])
            .filter(([key]) => key.length > 0));
    }
    readRelationLabel(value) {
        if (typeof value === 'string') {
            const normalizedValue = value.trim();
            return normalizedValue ? normalizedValue : null;
        }
        if (!value || typeof value !== 'object' || Array.isArray(value)) {
            return null;
        }
        const source = value;
        const labelGuid = this.readConfigString(source['labelGuid'] ?? source['guid']);
        const primaryDefaultText = this.readConfigString(source['primaryDefaultText'] ?? source['primaryText']);
        const secondaryDefaultText = this.readConfigString(source['secondaryDefaultText'] ?? source['secondaryText']);
        if (!labelGuid || !primaryDefaultText || !secondaryDefaultText) {
            return null;
        }
        return {
            labelGuid,
            primaryDefaultText,
            secondaryDefaultText
        };
    }
    readRelationConfigurations(value) {
        if (!Array.isArray(value)) {
            return [];
        }
        return value
            .map((entry) => this.normalizeComponentConfiguration(this.isJsonObject(entry)
            ? entry
            : null))
            .filter((entry) => entry.chillType.trim().length > 0);
    }
    createRelationRowActions() {
        return this.relations().map((relation, index) => {
            const relationLabel = relation.relationLabel;
            const plainLabel = this.relationActionLabel(relation, index);
            return {
                icon: 'account_tree',
                iconClass: 'material-symbol-icon',
                ...(typeof relationLabel === 'string'
                    ? { label: relationLabel.trim() }
                    : relationLabel
                        ? {
                            labelGuid: relationLabel.labelGuid,
                            primaryDefaultText: relationLabel.primaryDefaultText,
                            secondaryDefaultText: relationLabel.secondaryDefaultText
                        }
                        : {}),
                ariaLabel: plainLabel,
                disabled: (entity) => this.isSaving() || this.isDeletedEntity(entity),
                handler: (entity) => this.openRelation(entity, relation)
            };
        });
    }
    createAttachmentRowActions() {
        if (this.resultSchema()?.handleAttachments !== true || this.isAttachmentCrud()) {
            return [];
        }
        return [{
                icon: 'attach_file',
                iconClass: 'material-symbol-icon',
                labelGuid: '7A673274-5786-4E58-BA33-65D06BF6B9F3',
                primaryDefaultText: 'Attachments',
                secondaryDefaultText: 'Allegati',
                ariaLabel: this.chill.T('7A673274-5786-4E58-BA33-65D06BF6B9F3', 'Attachments', 'Allegati'),
                disabled: (entity) => this.isSaving()
                    || this.isDeletedEntity(entity)
                    || this.isNewEntity(entity)
                    || !this.canOpenAttachmentCrud(entity),
                handler: (entity) => this.openAttachmentCrud(entity)
            }];
    }
    createAttachmentDownloadRowActions() {
        if (!this.isAttachmentCrud()) {
            return [];
        }
        return [{
                icon: 'download',
                iconClass: 'material-symbol-icon',
                labelGuid: '3E0E4E83-A2FE-4C08-B389-0C3B26A0CCFF',
                primaryDefaultText: 'Download',
                secondaryDefaultText: 'Scarica',
                ariaLabel: this.chill.T('3E0E4E83-A2FE-4C08-B389-0C3B26A0CCFF', 'Download', 'Scarica'),
                disabled: (entity) => this.isSaving() || this.isDeletedEntity(entity) || !this.readEntityKey(entity),
                handler: (entity) => void this.downloadAttachment(entity)
            }];
    }
    openRelation(entity, relation) {
        const resolvedRelation = this.resolveRelationConfiguration(relation, entity);
        const chillType = resolvedRelation.chillType.trim();
        if (!chillType) {
            return;
        }
        this.workspace.openCrudTask({
            chillType,
            queryChillType: resolvedRelation.chillQuery,
            viewCode: resolvedRelation.viewCode,
            displayName: this.resolveRelationLabel(resolvedRelation) || undefined,
            componentConfiguration: resolvedRelation
        });
    }
    canOpenAttachmentCrud(entity) {
        return !!this.readEntityKey(entity) && !!this.readEntityChillType(entity);
    }
    openAttachmentCrud(entity) {
        const attachToGuid = this.readEntityKey(entity);
        const attachToChillType = this.readEntityChillType(entity);
        if (!attachToGuid || !attachToChillType) {
            return;
        }
        const attachmentConfiguration = {
            chillType: ATTACHMENT_ENTITY_CHILL_TYPE,
            chillQuery: ATTACHMENT_QUERY_CHILL_TYPE,
            disableAdd: false,
            disableCreate: true,
            disableEdit: false,
            disableInlineEdit: false,
            disableDelete: false,
            fixedQueryValues: {
                AttachToChillType: attachToChillType,
                AttachToGuid: attachToGuid
            },
            defaultQueryValues: {
                AttachToChillType: attachToChillType,
                AttachToGuid: attachToGuid
            },
            defaultValues: {
                AttachToChillType: attachToChillType,
                AttachToGuid: attachToGuid
            }
        };
        this.workspace.openCrudTask({
            chillType: ATTACHMENT_ENTITY_CHILL_TYPE,
            queryChillType: ATTACHMENT_QUERY_CHILL_TYPE,
            displayName: this.chill.T('7A673274-5786-4E58-BA33-65D06BF6B9F3', 'Attachments', 'Allegati'),
            componentConfiguration: attachmentConfiguration
        });
    }
    resolveRelationConfiguration(configuration, entity) {
        return {
            chillType: configuration.chillType,
            chillQuery: configuration.chillQuery ?? null,
            viewCode: configuration.viewCode ?? null,
            disableAdd: configuration.disableAdd === true,
            disableCreate: configuration.disableCreate === true,
            disableEdit: configuration.disableEdit === true,
            disableInlineEdit: configuration.disableInlineEdit === true,
            disableDelete: configuration.disableDelete === true,
            relationLabel: configuration.relationLabel ?? null,
            defaultValues: {
                ...this.resolveConfigRecord(configuration.defaultValues, entity)
            },
            fixedValues: {
                ...this.resolveConfigRecord(configuration.fixedValues, entity)
            },
            fixedQueryValues: {
                ...this.resolveConfigRecord(configuration.fixedQueryValues, entity)
            },
            defaultQueryValues: {
                ...this.resolveConfigRecord(configuration.defaultQueryValues, entity)
            },
            relations: (configuration.relations ?? []).map((relation) => this.resolveRelationConfiguration(relation, entity))
        };
    }
    relationActionLabel(relation, index) {
        const relationLabel = this.resolveRelationLabel(relation);
        if (relationLabel) {
            return relationLabel;
        }
        const chillType = relation.chillType.trim();
        return chillType
            ? this.chill.T(`crud-relation-${index + 1}`, `Open related ${chillType}`, `Apri collegata ${chillType}`)
            : this.chill.T(`crud-relation-${index + 1}`, 'Open related CRUD', 'Apri CRUD collegata');
    }
    resolveRelationLabel(relation) {
        const relationLabel = relation.relationLabel;
        if (typeof relationLabel === 'string') {
            return relationLabel.trim();
        }
        if (relationLabel && typeof relationLabel === 'object') {
            return this.chill.T(relationLabel.labelGuid, relationLabel.primaryDefaultText, relationLabel.secondaryDefaultText);
        }
        return '';
    }
    resolveConfigRecord(value, entity) {
        if (!value) {
            return {};
        }
        return Object.fromEntries(Object.entries(value).map(([key, entryValue]) => [key, this.resolveConfigValue(entryValue, entity)]));
    }
    readAttachmentTargetInfo() {
        const fixedQueryValues = this.fixedQueryValues();
        const defaultQueryValues = this.defaultQueryValues();
        const queryProperties = this.queryModel()?.properties ?? {};
        const attachToChillType = this.readStringValue(fixedQueryValues['AttachToChillType']
            ?? defaultQueryValues['AttachToChillType']
            ?? queryProperties['AttachToChillType']
            ?? queryProperties['attachToChillType']);
        const attachToGuid = this.readStringValue(fixedQueryValues['AttachToGuid']
            ?? defaultQueryValues['AttachToGuid']
            ?? queryProperties['AttachToGuid']
            ?? queryProperties['attachToGuid']);
        return {
            attachToChillType,
            attachToGuid
        };
    }
    resolveConfigValue(value, entity) {
        if (typeof value !== 'string') {
            return value;
        }
        const placeholderMatch = /^@\{(.+)\}$/.exec(value.trim());
        if (!placeholderMatch) {
            return value;
        }
        const token = placeholderMatch[1].trim();
        if (!token) {
            return value;
        }
        if (!entity) {
            return value;
        }
        if (token.toLowerCase() === 'mock') {
            return this.createEntityMock(entity);
        }
        return this.readEntityPropertyValue(entity, token) ?? null;
    }
    async downloadAttachment(entity) {
        const attachmentGuid = this.readEntityKey(entity);
        if (!attachmentGuid) {
            return;
        }
        try {
            const blob = await firstValueFrom(this.chill.downloadAttachment(attachmentGuid));
            const downloadUrl = URL.createObjectURL(blob);
            const anchor = document.createElement('a');
            anchor.href = downloadUrl;
            anchor.download = this.readAttachmentFileName(entity);
            anchor.style.display = 'none';
            document.body.appendChild(anchor);
            anchor.click();
            anchor.remove();
            URL.revokeObjectURL(downloadUrl);
        }
        catch (error) {
            this.errorMessage.set(this.chill.formatError(error));
        }
    }
    readAttachmentFileName(entity) {
        const originalFilename = this.readStringValue(this.readEntityPropertyValue(entity, 'OriginalFilename'));
        if (originalFilename) {
            return originalFilename;
        }
        const title = this.readStringValue(this.readEntityPropertyValue(entity, 'Title'));
        return title || `${this.readEntityKey(entity) || 'attachment'}.bin`;
    }
    createEntityMock(entity) {
        const clonedEntity = this.cloneEntity(entity);
        const guid = this.readEntityKey(clonedEntity);
        const chillType = this.readStringValue(clonedEntity['chillType']);
        const label = this.readStringValue(clonedEntity['label']);
        return {
            ...(guid ? { guid } : {}),
            ...(chillType ? { chillType } : {}),
            ...(label ? { label } : {}),
            properties: {
                ...(clonedEntity.properties ?? {})
            }
        };
    }
    isQuerySchema(item) {
        const type = item.type?.trim().toLowerCase() ?? '';
        const name = item.name?.trim().toLowerCase() ?? '';
        const chillType = item.chillType?.trim().toLowerCase() ?? '';
        return type === 'query'
            || name.endsWith('query')
            || chillType.includes('.query.')
            || chillType.endsWith('.query');
    }
    schemaLabel(item) {
        return item.displayName?.trim() || item.name?.trim() || item.chillType?.trim() || '';
    }
    normalizeViewCode(value) {
        const normalizedValue = value?.trim();
        return normalizedValue ? normalizedValue : DEFAULT_VIEW_CODE;
    }
    isEntitySelected(entity) {
        const entityKey = this.readEntityKey(entity);
        return !!entityKey && this.selectedEntityKeys().includes(entityKey);
    }
    toggleSelectedEntity(entity, selected) {
        const entityKey = this.readEntityKey(entity);
        if (!entityKey) {
            return;
        }
        this.selectedEntityKeys.update((current) => {
            if (this.multipleSelection()) {
                if (selected) {
                    return current.includes(entityKey) ? current : [...current, entityKey];
                }
                return current.filter((value) => value !== entityKey);
            }
            return selected ? [entityKey] : [];
        });
    }
    selectedEntity() {
        return this.selectedEntities()[0] ?? null;
    }
    selectedEntities() {
        const selectedEntityKeys = this.selectedEntityKeys();
        if (selectedEntityKeys.length === 0) {
            return [];
        }
        const selectedEntityMap = new Map();
        for (const entity of this.results()) {
            const entityKey = this.readEntityKey(entity);
            if (entityKey && selectedEntityKeys.includes(entityKey)) {
                selectedEntityMap.set(entityKey, entity);
            }
        }
        for (const entity of this.readInitialSelectedEntities()) {
            const entityKey = this.readEntityKey(entity);
            if (entityKey && selectedEntityKeys.includes(entityKey) && !selectedEntityMap.has(entityKey)) {
                selectedEntityMap.set(entityKey, entity);
            }
        }
        return selectedEntityKeys
            .map((entityKey) => selectedEntityMap.get(entityKey) ?? null)
            .filter((entity) => entity !== null);
    }
    readInitialSelectedEntityKeys() {
        const selectedKeys = this.readInitialSelectedEntities()
            .map((entity) => this.readEntityKey(entity))
            .filter((entityKey) => entityKey.length > 0);
        return [...new Set(selectedKeys)];
    }
    readInitialSelectedEntities() {
        if (this.multipleSelection()) {
            return this.initialSelectedEntities();
        }
        const initialSelectedEntity = this.initialSelectedEntity();
        return initialSelectedEntity ? [initialSelectedEntity] : [];
    }
    static { this.ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: CrudPageComponent, deps: [], target: i0.ɵɵFactoryTarget.Component }); }
    static { this.ɵcmp = i0.ɵɵngDeclareComponent({ minVersion: "17.0.0", version: "19.2.21", type: CrudPageComponent, isStandalone: true, selector: "app-crud-page", inputs: { selectionEnabled: { classPropertyName: "selectionEnabled", publicName: "selectionEnabled", isSignal: true, isRequired: false, transformFunction: null }, multipleSelection: { classPropertyName: "multipleSelection", publicName: "multipleSelection", isSignal: true, isRequired: false, transformFunction: null }, initialSelectedEntity: { classPropertyName: "initialSelectedEntity", publicName: "initialSelectedEntity", isSignal: true, isRequired: false, transformFunction: null }, initialSelectedEntities: { classPropertyName: "initialSelectedEntities", publicName: "initialSelectedEntities", isSignal: true, isRequired: false, transformFunction: null }, showTableHeader: { classPropertyName: "showTableHeader", publicName: "showTableHeader", isSignal: true, isRequired: false, transformFunction: null }, showMobileTaskClose: { classPropertyName: "showMobileTaskClose", publicName: "showMobileTaskClose", isSignal: true, isRequired: false, transformFunction: null }, componentConfiguration: { classPropertyName: "componentConfiguration", publicName: "componentConfiguration", isSignal: true, isRequired: false, transformFunction: null } }, ngImport: i0, template: "<section class=\"crud-page\">\n  @if (errorMessage()) {\n    <div class=\"notice error notice-dismissible\">\n      <span>{{ errorMessage() }}</span>\n      <button type=\"button\" class=\"notice-close\" (click)=\"clearErrorMessage()\" [attr.aria-label]=\"chill.T('0C4C06EF-A105-468F-B1E2-AEA8EB96A4DC', 'Dismiss error', 'Chiudi errore')\">x</button>\n    </div>\n  }\n\n  @if (validationErrorMessage()) {\n    <div class=\"notice error\">\n      <span>{{ validationErrorMessage() }}</span>\n    </div>\n  }\n\n  @if (isLoadingSchemaList()) {\n    <div class=\"notice\"><app-chill-i18n-label [labelGuid]=\"'77C1E6C4-D380-40C8-9BA8-31A611A9DA15'\" [primaryDefaultText]=\"'Loading query schemas...'\" [secondaryDefaultText]=\"'Caricamento degli schemi di query...'\" /></div>\n  } @else if (isLoadingSchema()) {\n    <div class=\"notice\"><app-chill-i18n-label [labelGuid]=\"'FE514A29-58AB-4842-ABDB-D348D05385E8'\" [primaryDefaultText]=\"'Loading selected schema...'\" [secondaryDefaultText]=\"'Caricamento dello schema selezionato...'\" /></div>\n  } @else if (querySchema()) {\n    <section class=\"panel\">\n      <section class=\"content\">\n        @if (isSearching()) {\n          <div class=\"notice\">{{ chill.T('6734CE10-8F49-4E20-8ADB-C6AEF407399E', 'Running query...', 'Esecuzione query in corso...') }}</div>\n        }\n\n        <app-chill-table\n          [schema]=\"resultSchema()\"\n          [entities]=\"pagedResults()\"\n          [selectionColumn]=\"selectionColumn()\"\n          [rowActions]=\"activeRowActions()\"\n          [ordering]=\"queryModel()?.ordering ?? null\"\n          [enableInlineEditing]=\"!isInlineEditDisabled()\"\n          [readonlyPropertyNames]=\"readonlyEntityPropertyNames()\"\n          [showSchemaHeader]=\"showTableHeader()\"\n          [showMobileTaskClose]=\"showMobileTaskClose()\"\n          [enableFullTextSearch]=\"canOpenSearchDialog()\"\n          [fullTextSearch]=\"currentFullTextSearch()\"\n          [validationFocus]=\"validationFocus()\"\n          (cellEditCommit)=\"handleInlineCellEdit($event)\"\n          (fullTextSearchChange)=\"applyFullTextSearch($event)\"\n          (mobileTaskClose)=\"closeActiveTask()\"\n          (schemaUpdated)=\"handleResultSchemaUpdated($event)\"\n          (sortChange)=\"applyOrdering($event)\" />\n\n        @if (canGoToPreviousPage() || canGoToNextPage()) {\n          <footer class=\"pagination\">\n            <button type=\"button\" (click)=\"goToPreviousPage()\" [disabled]=\"!canGoToPreviousPage()\">\n              {{ chill.T('15FA9D58-C1F9-4A70-8B67-C2D4232D6B89', 'Previous', 'Precedente') }}\n            </button>\n            <p>{{ pageLabel() }}</p>\n            <button type=\"button\" (click)=\"goToNextPage()\" [disabled]=\"!canGoToNextPage()\">\n              {{ chill.T('F42E54FE-F561-4251-B284-28B9230D70F7', 'Next', 'Successiva') }}\n            </button>\n          </footer>\n        }\n      </section>\n    </section>\n  }\n</section>\n", styles: [":host{display:block;height:100%;min-height:0}.crud-page{display:grid;height:100%;min-height:0;gap:1.5rem;padding:1.5rem .5rem;align-content:start;align-items:start;overflow-y:auto}.panel,.content{display:grid;gap:1rem}.pagination{display:flex;align-items:center;justify-content:center;gap:1rem;padding:.5rem 0 0}.pagination p{margin:0;color:var(--text-muted);font-weight:600}.pagination button{min-height:2.75rem;padding:.65rem 1rem;border:1px solid var(--border-color);border-radius:999px;background:var(--surface-0);color:var(--text-main);cursor:pointer}.pagination button:disabled{cursor:not-allowed;opacity:.55}.notice{margin-top:0;padding:1rem 1.25rem;border-radius:1rem;background:var(--surface-2);border:1px solid var(--border-color);color:var(--text-main);max-width:100%;max-height:5rem;overflow-x:hidden;overflow-y:auto;overflow-wrap:anywhere;word-break:break-word}.notice.error{color:var(--danger);border-color:color-mix(in srgb,var(--danger) 22%,transparent);background:var(--danger-bg)}@media(max-width:960px){.crud-page{padding:1rem .25rem;scrollbar-width:none;-ms-overflow-style:none}.crud-page::-webkit-scrollbar{width:0;height:0;display:none}}\n"], dependencies: [{ kind: "ngmodule", type: CommonModule }, { kind: "ngmodule", type: FormsModule }, { kind: "component", type: ChillTableComponent, selector: "app-chill-table", inputs: ["schema", "entities", "selectionColumn", "rowAction", "rowActions", "enableInlineEditing", "readonlyPropertyNames", "validationFocus", "showSchemaHeader", "ordering", "enableFullTextSearch", "fullTextSearch", "showMobileTaskClose"], outputs: ["cellEditCommit", "sortChange", "fullTextSearchChange", "schemaUpdated", "mobileTaskClose"] }, { kind: "component", type: ChillI18nLabelComponent, selector: "app-chill-i18n-label", inputs: ["labelGuid", "primaryDefaultText", "secondaryDefaultText", "editable"] }, { kind: "directive", type: NoticeTransitionDirective, selector: ".notice" }] }); }
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: CrudPageComponent, decorators: [{
            type: Component,
            args: [{ selector: 'app-crud-page', standalone: true, imports: [CommonModule, FormsModule, ChillTableComponent, ChillI18nLabelComponent, NoticeTransitionDirective], template: "<section class=\"crud-page\">\n  @if (errorMessage()) {\n    <div class=\"notice error notice-dismissible\">\n      <span>{{ errorMessage() }}</span>\n      <button type=\"button\" class=\"notice-close\" (click)=\"clearErrorMessage()\" [attr.aria-label]=\"chill.T('0C4C06EF-A105-468F-B1E2-AEA8EB96A4DC', 'Dismiss error', 'Chiudi errore')\">x</button>\n    </div>\n  }\n\n  @if (validationErrorMessage()) {\n    <div class=\"notice error\">\n      <span>{{ validationErrorMessage() }}</span>\n    </div>\n  }\n\n  @if (isLoadingSchemaList()) {\n    <div class=\"notice\"><app-chill-i18n-label [labelGuid]=\"'77C1E6C4-D380-40C8-9BA8-31A611A9DA15'\" [primaryDefaultText]=\"'Loading query schemas...'\" [secondaryDefaultText]=\"'Caricamento degli schemi di query...'\" /></div>\n  } @else if (isLoadingSchema()) {\n    <div class=\"notice\"><app-chill-i18n-label [labelGuid]=\"'FE514A29-58AB-4842-ABDB-D348D05385E8'\" [primaryDefaultText]=\"'Loading selected schema...'\" [secondaryDefaultText]=\"'Caricamento dello schema selezionato...'\" /></div>\n  } @else if (querySchema()) {\n    <section class=\"panel\">\n      <section class=\"content\">\n        @if (isSearching()) {\n          <div class=\"notice\">{{ chill.T('6734CE10-8F49-4E20-8ADB-C6AEF407399E', 'Running query...', 'Esecuzione query in corso...') }}</div>\n        }\n\n        <app-chill-table\n          [schema]=\"resultSchema()\"\n          [entities]=\"pagedResults()\"\n          [selectionColumn]=\"selectionColumn()\"\n          [rowActions]=\"activeRowActions()\"\n          [ordering]=\"queryModel()?.ordering ?? null\"\n          [enableInlineEditing]=\"!isInlineEditDisabled()\"\n          [readonlyPropertyNames]=\"readonlyEntityPropertyNames()\"\n          [showSchemaHeader]=\"showTableHeader()\"\n          [showMobileTaskClose]=\"showMobileTaskClose()\"\n          [enableFullTextSearch]=\"canOpenSearchDialog()\"\n          [fullTextSearch]=\"currentFullTextSearch()\"\n          [validationFocus]=\"validationFocus()\"\n          (cellEditCommit)=\"handleInlineCellEdit($event)\"\n          (fullTextSearchChange)=\"applyFullTextSearch($event)\"\n          (mobileTaskClose)=\"closeActiveTask()\"\n          (schemaUpdated)=\"handleResultSchemaUpdated($event)\"\n          (sortChange)=\"applyOrdering($event)\" />\n\n        @if (canGoToPreviousPage() || canGoToNextPage()) {\n          <footer class=\"pagination\">\n            <button type=\"button\" (click)=\"goToPreviousPage()\" [disabled]=\"!canGoToPreviousPage()\">\n              {{ chill.T('15FA9D58-C1F9-4A70-8B67-C2D4232D6B89', 'Previous', 'Precedente') }}\n            </button>\n            <p>{{ pageLabel() }}</p>\n            <button type=\"button\" (click)=\"goToNextPage()\" [disabled]=\"!canGoToNextPage()\">\n              {{ chill.T('F42E54FE-F561-4251-B284-28B9230D70F7', 'Next', 'Successiva') }}\n            </button>\n          </footer>\n        }\n      </section>\n    </section>\n  }\n</section>\n", styles: [":host{display:block;height:100%;min-height:0}.crud-page{display:grid;height:100%;min-height:0;gap:1.5rem;padding:1.5rem .5rem;align-content:start;align-items:start;overflow-y:auto}.panel,.content{display:grid;gap:1rem}.pagination{display:flex;align-items:center;justify-content:center;gap:1rem;padding:.5rem 0 0}.pagination p{margin:0;color:var(--text-muted);font-weight:600}.pagination button{min-height:2.75rem;padding:.65rem 1rem;border:1px solid var(--border-color);border-radius:999px;background:var(--surface-0);color:var(--text-main);cursor:pointer}.pagination button:disabled{cursor:not-allowed;opacity:.55}.notice{margin-top:0;padding:1rem 1.25rem;border-radius:1rem;background:var(--surface-2);border:1px solid var(--border-color);color:var(--text-main);max-width:100%;max-height:5rem;overflow-x:hidden;overflow-y:auto;overflow-wrap:anywhere;word-break:break-word}.notice.error{color:var(--danger);border-color:color-mix(in srgb,var(--danger) 22%,transparent);background:var(--danger-bg)}@media(max-width:960px){.crud-page{padding:1rem .25rem;scrollbar-width:none;-ms-overflow-style:none}.crud-page::-webkit-scrollbar{width:0;height:0;display:none}}\n"] }]
        }] });

class CrudTaskComponent {
    static getComponentConfigurationJsonExample() {
        return {
            chillType: '',
            chillQuery: null,
            viewCode: 'default',
            disableAdd: false,
            disableCreate: false,
            disableEdit: false,
            disableInlineEdit: false,
            disableDelete: false,
            relationLabel: {
                labelGuid: "",
                primaryDefaultText: "",
                secondaryDefaultText: ""
            },
            defaultValues: {},
            fixedQueryValues: {},
            defaultQueryValues: {},
            relations: []
        };
    }
    resolvedComponentConfiguration() {
        const configuration = this.componentConfiguration();
        if (!configuration) {
            return null;
        }
        return configuration;
    }
    constructor() {
        this.chill = inject(ChillService);
        this.dialog = inject(WorkspaceDialogService, { optional: true });
        this.toolbar = inject(WorkspaceToolbarService);
        this.selectionEnabled = input(false);
        this.multipleSelection = input(false);
        this.initialSelectedEntity = input(null);
        this.initialSelectedEntities = input([]);
        this.componentConfiguration = input(null);
        this.taskTitle = input('');
        this.taskDescription = input('');
        this.toolbarScope = input('workspace');
        this.visible = input(true);
        this.page = viewChild(CrudPageComponent);
        effect(() => {
            const page = this.page();
            const toolbarScope = this.toolbarScope();
            if (!page || !this.visible()) {
                this.toolbar.clearButtons(toolbarScope);
                return;
            }
            this.toolbar.setButtons([
                {
                    id: 'crud-search',
                    labelGuid: 'D513421E-1C00-425E-A89B-E736A440474F',
                    primaryDefaultText: 'Search',
                    secondaryDefaultText: 'Cerca',
                    ariaLabel: this.chill.T('44972777-6760-4F48-BE39-B504E4467150', 'Search', 'Cerca'),
                    icon: 'filter_alt',
                    iconClass: 'material-symbol-icon',
                    action: () => page.openSearchDialog(),
                    disabled: !page.canOpenSearchDialog()
                },
                ...(page.isAttachmentCrud() ? [{
                        id: 'crud-add-attachment',
                        labelGuid: 'D31B58D6-32F2-443B-AD18-7BFA76AF2FB6',
                        primaryDefaultText: 'Add attachment',
                        secondaryDefaultText: 'Aggiungi allegato',
                        ariaLabel: this.chill.T('D31B58D6-32F2-443B-AD18-7BFA76AF2FB6', 'Add attachment', 'Aggiungi allegato'),
                        icon: 'upload_file',
                        iconClass: 'material-symbol-icon',
                        action: () => void page.openAttachmentUploadDialog(),
                        disabled: !page.canOpenAttachmentUploadDialog() || page.isSaving()
                    }] : []),
                ...(page.isAddDisabled() ? [] : [{
                        id: 'crud-add',
                        labelGuid: '23A5536E-8A94-4469-977C-D3BB57E5E621',
                        primaryDefaultText: 'Add',
                        secondaryDefaultText: 'Aggiungi',
                        ariaLabel: this.chill.T('23A5536E-8A94-4469-977C-D3BB57E5E621', 'Add', 'Aggiungi'),
                        icon: 'add',
                        iconClass: 'material-symbol-icon',
                        action: () => page.add(),
                        disabled: !page.canAddEntity() || page.isSaving()
                    }]),
                {
                    id: 'crud-save-draft',
                    labelGuid: 'B8076F7C-34A3-4C28-B4FC-F7D673C0D088',
                    primaryDefaultText: 'Save',
                    secondaryDefaultText: 'Salva',
                    ariaLabel: this.chill.T('B8076F7C-34A3-4C28-B4FC-F7D673C0D088', 'Save', 'Salva'),
                    icon: 'save',
                    iconClass: 'material-symbol-icon',
                    accent: page.hasPendingEntities(),
                    action: () => void page.savePendingEntities(),
                    disabled: !page.hasPendingEntities() || page.isSaving()
                }
            ], toolbarScope);
        });
    }
    submit() {
        if (this.selectionEnabled()) {
            this.dialog?.confirm(this.page()?.dialogResult() ?? null);
            return;
        }
        this.dialog?.confirm();
    }
    canDialogSubmit() {
        return this.selectionEnabled()
            ? (this.page()?.canConfirmSelection() ?? false)
            : true;
    }
    isAllSaved() {
        const page = this.page();
        if (!page) {
            return true;
        }
        return !page.isSaving() && !page.hasPendingEntities();
    }
    ngOnDestroy() {
        this.toolbar.clearButtons(this.toolbarScope());
    }
    static { this.ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: CrudTaskComponent, deps: [], target: i0.ɵɵFactoryTarget.Component }); }
    static { this.ɵcmp = i0.ɵɵngDeclareComponent({ minVersion: "17.2.0", version: "19.2.21", type: CrudTaskComponent, isStandalone: true, selector: "app-crud-task", inputs: { selectionEnabled: { classPropertyName: "selectionEnabled", publicName: "selectionEnabled", isSignal: true, isRequired: false, transformFunction: null }, multipleSelection: { classPropertyName: "multipleSelection", publicName: "multipleSelection", isSignal: true, isRequired: false, transformFunction: null }, initialSelectedEntity: { classPropertyName: "initialSelectedEntity", publicName: "initialSelectedEntity", isSignal: true, isRequired: false, transformFunction: null }, initialSelectedEntities: { classPropertyName: "initialSelectedEntities", publicName: "initialSelectedEntities", isSignal: true, isRequired: false, transformFunction: null }, componentConfiguration: { classPropertyName: "componentConfiguration", publicName: "componentConfiguration", isSignal: true, isRequired: false, transformFunction: null }, taskTitle: { classPropertyName: "taskTitle", publicName: "taskTitle", isSignal: true, isRequired: false, transformFunction: null }, taskDescription: { classPropertyName: "taskDescription", publicName: "taskDescription", isSignal: true, isRequired: false, transformFunction: null }, toolbarScope: { classPropertyName: "toolbarScope", publicName: "toolbarScope", isSignal: true, isRequired: false, transformFunction: null }, visible: { classPropertyName: "visible", publicName: "visible", isSignal: true, isRequired: false, transformFunction: null } }, viewQueries: [{ propertyName: "page", first: true, predicate: CrudPageComponent, descendants: true, isSignal: true }], ngImport: i0, template: `
    <section class="crud-task">
      <app-crud-page
        [selectionEnabled]="selectionEnabled()"
        [multipleSelection]="multipleSelection()"
        [initialSelectedEntity]="initialSelectedEntity()"
        [initialSelectedEntities]="initialSelectedEntities()"
        [componentConfiguration]="resolvedComponentConfiguration()"
        [showTableHeader]="toolbarScope() !== 'dialog'"
        [showMobileTaskClose]="toolbarScope() !== 'dialog'" />
    </section>
  `, isInline: true, styles: [":host,.crud-task{display:block;height:100%;min-height:0}\n"], dependencies: [{ kind: "component", type: CrudPageComponent, selector: "app-crud-page", inputs: ["selectionEnabled", "multipleSelection", "initialSelectedEntity", "initialSelectedEntities", "showTableHeader", "showMobileTaskClose", "componentConfiguration"] }] }); }
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: CrudTaskComponent, decorators: [{
            type: Component,
            args: [{ selector: 'app-crud-task', standalone: true, imports: [CrudPageComponent], template: `
    <section class="crud-task">
      <app-crud-page
        [selectionEnabled]="selectionEnabled()"
        [multipleSelection]="multipleSelection()"
        [initialSelectedEntity]="initialSelectedEntity()"
        [initialSelectedEntities]="initialSelectedEntities()"
        [componentConfiguration]="resolvedComponentConfiguration()"
        [showTableHeader]="toolbarScope() !== 'dialog'"
        [showMobileTaskClose]="toolbarScope() !== 'dialog'" />
    </section>
  `, styles: [":host,.crud-task{display:block;height:100%;min-height:0}\n"] }]
        }], ctorParameters: () => [] });

var crudTask_component = /*#__PURE__*/Object.freeze({
    __proto__: null,
    CrudTaskComponent: CrudTaskComponent
});

const DEFAULT_SOURCE_INDEX_FILE = 'workspace-tasks.index.json';
class WorkspaceTaskRegistryService {
    constructor() {
        this.document = inject(DOCUMENT);
        this.definitionsState = signal([]);
        this.initializationErrorState = signal('');
        this.remoteEntryLoads = new Map();
        this.remoteComponentLoads = new Map();
        this.initialized = false;
        this.definitions = computed(() => this.definitionsState()
            .map(({ aliases: _aliases, ...definition }) => definition));
        this.initializationError = this.initializationErrorState.asReadonly();
    }
    async initialize() {
        if (this.initialized) {
            return;
        }
        this.initialized = true;
        this.registerBuiltInTasks();
        try {
            await this.loadRemoteTaskSources();
        }
        catch (error) {
            this.initializationErrorState.set(error instanceof Error ? error.message : 'Unable to initialize workspace task sources.');
            console.error(error);
        }
    }
    getTaskDefinition(componentName) {
        const normalizedName = normalizeComponentName(componentName);
        return this.definitionsState().find((definition) => definition.aliases.includes(normalizedName)) ?? null;
    }
    async resolveComponent(componentName) {
        const definition = this.getTaskDefinition(componentName);
        if (!definition) {
            return null;
        }
        return definition.loadComponent();
    }
    registerBuiltInTasks() {
        this.registerDefinition({
            componentName: 'permissions',
            title: 'Permissions',
            description: 'Manage roles, users, and access rules.',
            kind: 'builtin',
            componentConfigurationJsonExample: this.serializeComponentConfigurationJsonExample(PermissionsPageComponent),
            showInQuickLaunch: false,
            loadComponent: async () => PermissionsPageComponent,
            aliases: ['permissions', 'permission', 'permission-page']
        });
        this.registerDefinition({
            componentName: 'crud',
            title: 'CRUD',
            description: 'Inspect schemas and work with entities.',
            kind: 'builtin',
            componentConfigurationJsonExample: this.serializeComponentConfigurationJsonExample(CrudTaskComponent),
            usesTaskConfigurationInputs: true,
            showInQuickLaunch: false,
            loadComponent: async () => CrudTaskComponent,
            aliases: ['crud']
        });
    }
    async loadRemoteTaskSources() {
        const runtimeConfig = readWorkspaceRuntimeConfig();
        const sourceUrls = normalizeSourceUrls(runtimeConfig.workspaceTaskSources ?? []);
        if (sourceUrls.length === 0) {
            return;
        }
        const sourceResults = await Promise.all(sourceUrls.map(async (sourceUrl) => {
            const response = await globalThis.fetch(resolveSourceIndexUrl(sourceUrl));
            if (!response.ok) {
                throw new Error(`Unable to load workspace task index from ${sourceUrl}.`);
            }
            const index = await response.json();
            return { sourceUrl, index };
        }));
        for (const { sourceUrl, index } of sourceResults) {
            for (const task of index.tasks ?? []) {
                this.registerRemoteTask(sourceUrl, task);
            }
        }
    }
    registerRemoteTask(sourceUrl, task) {
        const componentName = task.componentName?.trim();
        if (!componentName) {
            return;
        }
        const remoteEntryUrl = new URL(task.remoteEntry, ensureTrailingSlash(sourceUrl)).toString();
        const remoteName = task.remoteName?.trim();
        const exposedModule = task.exposedModule?.trim();
        if (!remoteName || !exposedModule) {
            return;
        }
        const cacheKey = [
            remoteEntryUrl,
            remoteName,
            exposedModule,
            task.exportedComponentName?.trim() || 'default'
        ].join('::');
        this.registerDefinition({
            componentName,
            title: task.title?.trim() || componentName,
            description: task.description?.trim() || 'External workspace task.',
            kind: 'remote',
            componentConfigurationJsonExample: null,
            showInQuickLaunch: Boolean(task.showInQuickLaunch),
            loadComponent: () => this.loadRemoteComponent(cacheKey, remoteEntryUrl, remoteName, exposedModule, task.exportedComponentName?.trim() || 'default'),
            aliases: [componentName]
        });
    }
    registerDefinition(definition) {
        const normalizedAliases = [...new Set(definition.aliases.map((alias) => normalizeComponentName(alias)).filter(Boolean))];
        if (normalizedAliases.length === 0) {
            return;
        }
        this.definitionsState.update((definitions) => {
            if (definitions.some((current) => normalizedAliases.some((alias) => current.aliases.includes(alias)))) {
                console.warn(`Skipping duplicate workspace task registration for "${definition.componentName}".`);
                return definitions;
            }
            return [...definitions, {
                    ...definition,
                    aliases: normalizedAliases
                }];
        });
    }
    serializeComponentConfigurationJsonExample(component) {
        const example = component.getComponentConfigurationJsonExample?.();
        if (!example || typeof example !== 'object' || Array.isArray(example)) {
            return '{}';
        }
        try {
            return JSON.stringify(example, null, 2);
        }
        catch {
            return '{}';
        }
    }
    loadRemoteComponent(cacheKey, remoteEntryUrl, remoteName, exposedModule, exportedComponentName) {
        const existingLoad = this.remoteComponentLoads.get(cacheKey);
        if (existingLoad) {
            return existingLoad;
        }
        const load = (async () => {
            await this.ensureRemoteEntry(remoteEntryUrl);
            const container = readFederationContainer(remoteName);
            if (!container) {
                throw new Error(`Remote container "${remoteName}" is not available after loading ${remoteEntryUrl}.`);
            }
            await initializeFederationContainer(container);
            const moduleFactory = await container.get(exposedModule);
            const moduleExports = moduleFactory();
            const component = readExportedComponent(moduleExports, exportedComponentName);
            if (!component) {
                throw new Error(`Remote task "${remoteName}/${exposedModule}" did not export component "${exportedComponentName}".`);
            }
            return component;
        })();
        this.remoteComponentLoads.set(cacheKey, load);
        return load;
    }
    ensureRemoteEntry(remoteEntryUrl) {
        const existingLoad = this.remoteEntryLoads.get(remoteEntryUrl);
        if (existingLoad) {
            return existingLoad;
        }
        const load = new Promise((resolve, reject) => {
            const script = this.document.createElement('script');
            script.type = 'text/javascript';
            script.src = remoteEntryUrl;
            script.async = true;
            script.onload = () => resolve();
            script.onerror = () => reject(new Error(`Unable to load remote entry ${remoteEntryUrl}.`));
            this.document.head.appendChild(script);
        });
        this.remoteEntryLoads.set(remoteEntryUrl, load);
        return load;
    }
    static { this.ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: WorkspaceTaskRegistryService, deps: [], target: i0.ɵɵFactoryTarget.Injectable }); }
    static { this.ɵprov = i0.ɵɵngDeclareInjectable({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: WorkspaceTaskRegistryService, providedIn: 'root' }); }
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: WorkspaceTaskRegistryService, decorators: [{
            type: Injectable,
            args: [{
                    providedIn: 'root'
                }]
        }] });
function normalizeComponentName(value) {
    return value.trim().toLowerCase();
}
function normalizeSourceUrls(sourceUrls) {
    return sourceUrls
        .map((sourceUrl) => sourceUrl.trim())
        .filter(Boolean);
}
function resolveSourceIndexUrl(sourceUrl) {
    return sourceUrl.toLowerCase().endsWith('.json')
        ? sourceUrl
        : `${ensureTrailingSlash(sourceUrl)}${DEFAULT_SOURCE_INDEX_FILE}`;
}
function ensureTrailingSlash(value) {
    return value.endsWith('/') ? value : `${value}/`;
}
function readWorkspaceRuntimeConfig() {
    return globalThis.__chillSharpUiRuntimeConfig__ ?? {};
}
function readFederationContainer(remoteName) {
    const container = globalThis[remoteName];
    if (!container || typeof container !== 'object') {
        return null;
    }
    return container;
}
async function initializeFederationContainer(container) {
    if (typeof container.init !== 'function') {
        return;
    }
    const globalScope = globalThis;
    if (typeof globalScope.__webpack_init_sharing__ === 'function') {
        await globalScope.__webpack_init_sharing__('default');
        await container.init(globalScope.__webpack_share_scopes__?.['default'] ?? {});
        return;
    }
    await container.init({});
}
function readExportedComponent(moduleExports, exportedComponentName) {
    if (!moduleExports || typeof moduleExports !== 'object') {
        return null;
    }
    const exportsRecord = moduleExports;
    const namedExport = exportsRecord[exportedComponentName];
    if (isAngularComponentType(namedExport)) {
        return namedExport;
    }
    const defaultExport = exportsRecord['default'];
    return isAngularComponentType(defaultExport) ? defaultExport : null;
}
function isAngularComponentType(value) {
    return typeof value === 'function';
}

const ACTIVE_MENU_ITEM_QUERY_PARAM = 'activeMenuItem';
class WorkspaceService {
    constructor() {
        this.document = inject(DOCUMENT);
        this.router = inject(Router);
        this.chill = inject(ChillService);
        this.dialog = inject(WorkspaceDialogService);
        this.layout = inject(WorkspaceLayoutService);
        this.taskRegistry = inject(WorkspaceTaskRegistryService);
        this.destroyRef = inject(DestroyRef);
        this.drawerOpenState = signal(true);
        this.activeTaskIdState = signal(null);
        this.openTaskInstancesState = signal([]);
        this.taskComponentResolver = null;
        this.storedThemePreference = this.readStoredThemePreference();
        this.hasExplicitThemePreferenceState = signal(this.storedThemePreference !== null);
        this.themeState = signal(this.storedThemePreference ?? this.readSystemThemePreference());
        this.availableTasks = computed(() => this.taskRegistry.definitions());
        this.isDrawerOpen = this.drawerOpenState.asReadonly();
        this.theme = this.themeState.asReadonly();
        this.isLayoutEditingEnabled = this.layout.isLayoutEditingEnabled;
        this.openTasks = this.openTaskInstancesState.asReadonly();
        this.activeTask = computed(() => this.openTaskInstancesState()
            .find((task) => task.id === this.activeTaskIdState()) ?? null);
        effect(() => {
            const theme = this.themeState();
            this.document.documentElement.setAttribute('data-theme', theme);
            this.document.documentElement.style.setProperty('color-scheme', theme === 'dark' ? 'dark' : 'light');
        });
        this.bindSystemThemePreference();
    }
    async activateTaskFromRoute(taskType, queryParams) {
        const activeMenuItemGuid = this.readActiveMenuItemGuid(queryParams);
        if (activeMenuItemGuid) {
            const currentActiveMenuItemGuid = this.activeTask()?.menuItemGuid?.trim() ?? '';
            if (currentActiveMenuItemGuid === activeMenuItemGuid) {
                this.drawerOpenState.set(false);
                return;
            }
            const existingTask = this.findTaskByMenuItemGuid(activeMenuItemGuid);
            if (existingTask) {
                this.activeTaskIdState.set(existingTask.id);
                this.drawerOpenState.set(false);
                return;
            }
            const restoredTask = await this.restoreMenuTaskFromRoute(activeMenuItemGuid);
            if (!restoredTask) {
                if (this.openTaskInstancesState().length === 0) {
                    this.activeTaskIdState.set(null);
                    this.drawerOpenState.set(true);
                }
                else {
                    this.drawerOpenState.set(false);
                }
                return;
            }
            if (this.openTaskInstancesState().length === 0) {
                this.openTaskInstancesState.set([restoredTask]);
            }
            else {
                this.openTaskInstancesState.update((tasks) => [...tasks, restoredTask]);
            }
            this.activeTaskIdState.set(restoredTask.id);
            this.drawerOpenState.set(false);
            return;
        }
        if (!taskType) {
            if (this.openTaskInstancesState().length === 0) {
                this.activeTaskIdState.set(null);
                this.drawerOpenState.set(true);
            }
            return;
        }
        const task = await this.resolveTaskFromRoute(taskType, queryParams);
        if (!task) {
            void this.router.navigateByUrl('/workspace');
            return;
        }
        const activeTask = this.activeTask();
        if (activeTask && this.isSameTaskRoute(activeTask.route, task.route)) {
            return;
        }
        const existingTask = this.findTaskByRoute(task.route);
        if (existingTask) {
            this.activeTaskIdState.set(existingTask.id);
            this.drawerOpenState.set(false);
            return;
        }
        this.openTaskInstance(task, false);
    }
    async openTask(componentName, navigate = true) {
        const taskDefinition = this.getTaskDefinition(componentName);
        if (!taskDefinition) {
            return;
        }
        const task = await this.createStaticTaskInstance(taskDefinition);
        if (!task) {
            return;
        }
        const existingTask = this.findTaskByRoute(task.route);
        if (existingTask) {
            this.activateTask(existingTask.id);
            return;
        }
        this.openTaskInstance(task, navigate);
    }
    async openWorkspaceTask(request) {
        const taskDefinition = this.getTaskDefinition(request.componentName);
        if (!taskDefinition) {
            return;
        }
        const task = await this.createStaticTaskInstance(taskDefinition, request.title, request.description, request.configuration ?? null);
        if (!task) {
            return;
        }
        this.openTaskInstance(task);
    }
    openCrudTask(request) {
        const chillType = request.chillType.trim();
        if (!chillType) {
            return;
        }
        const configuration = this.buildCrudTaskConfiguration(request);
        void this.openWorkspaceTask({
            componentName: 'crud',
            title: request.displayName?.trim() || chillType,
            description: `CRUD task for ${chillType}`,
            configuration
        });
    }
    async openMenuItem(item) {
        const task = await this.createMenuTaskInstance(item);
        if (!task) {
            return;
        }
        this.openTaskInstance(task);
    }
    isMenuItemActive(item) {
        return this.activeTask()?.menuItemGuid === item.guid;
    }
    async activateTask(taskInstanceId) {
        const task = this.openTaskInstancesState().find((candidate) => candidate.id === taskInstanceId) ?? null;
        if (!task) {
            return;
        }
        const currentActiveTaskId = this.activeTaskIdState();
        if (currentActiveTaskId && currentActiveTaskId !== task.id) {
            const canLeaveCurrentTask = await this.confirmTaskCanBeLeft(currentActiveTaskId);
            if (!canLeaveCurrentTask) {
                return;
            }
        }
        this.activeTaskIdState.set(task.id);
        this.drawerOpenState.set(false);
        this.navigateToTask(task);
    }
    async closeTask(taskInstanceId) {
        const canCloseTask = await this.confirmTaskCanBeLeft(taskInstanceId);
        if (!canCloseTask) {
            return;
        }
        let nextActiveTaskId = this.activeTaskIdState();
        this.openTaskInstancesState.update((tasks) => {
            const nextTasks = tasks.filter((task) => task.id !== taskInstanceId);
            if (nextActiveTaskId === taskInstanceId) {
                nextActiveTaskId = nextTasks[nextTasks.length - 1]?.id ?? null;
            }
            return nextTasks;
        });
        this.activeTaskIdState.set(nextActiveTaskId);
        const nextActiveTask = this.openTaskInstancesState().find((task) => task.id === nextActiveTaskId) ?? null;
        if (nextActiveTask) {
            this.navigateToTask(nextActiveTask);
            return;
        }
        this.drawerOpenState.set(true);
        void this.router.navigate(['/workspace']);
    }
    toggleDrawer() {
        if (this.openTaskInstancesState().length === 0) {
            this.drawerOpenState.set(true);
            return;
        }
        this.drawerOpenState.update((isOpen) => !isOpen);
    }
    closeDrawer() {
        if (this.openTaskInstancesState().length === 0) {
            this.drawerOpenState.set(true);
            return;
        }
        this.drawerOpenState.set(false);
    }
    setTheme(theme) {
        this.themeState.set(theme);
        this.hasExplicitThemePreferenceState.set(true);
        globalThis.localStorage?.setItem(WORKSPACE_THEME_STORAGE_KEY, theme);
    }
    toggleLayoutEditingEnabled() {
        this.layout.toggleLayoutEditingEnabled();
    }
    async reset() {
        const canReset = await this.confirmAllTasksCanBeClosed();
        if (!canReset) {
            return false;
        }
        this.activeTaskIdState.set(null);
        this.openTaskInstancesState.set([]);
        this.drawerOpenState.set(true);
        return true;
    }
    registerTaskComponentResolver(resolver) {
        this.taskComponentResolver = resolver;
    }
    canUnloadWorkspace() {
        const activeTaskId = this.activeTaskIdState();
        if (!activeTaskId) {
            return true;
        }
        const component = this.taskComponentResolver?.(activeTaskId) ?? null;
        if (!component?.isAllSaved) {
            return true;
        }
        try {
            return component.isAllSaved() === true;
        }
        catch {
            return false;
        }
    }
    openTaskInstance(task, navigate = true) {
        this.openTaskInstancesState.update((tasks) => tasks.some((candidate) => candidate.id === task.id)
            ? tasks
            : [...tasks, task]);
        this.activeTaskIdState.set(task.id);
        this.drawerOpenState.set(false);
        if (navigate) {
            this.navigateToTask(task);
        }
    }
    navigateToTask(task) {
        void this.router.navigate(['/workspace'], {
            queryParams: this.buildWorkspaceQueryParams(task)
        });
    }
    async resolveTaskFromRoute(taskType, queryParams) {
        const taskDefinition = this.getTaskDefinition(taskType);
        if (!taskDefinition) {
            return null;
        }
        return this.createStaticTaskInstance(taskDefinition, queryParams.get('title'), queryParams.get('description'), this.deserializeConfiguration(queryParams.get('config')));
    }
    async createStaticTaskInstance(taskDefinition, titleOverride, descriptionOverride, configuration) {
        const component = await this.taskRegistry.resolveComponent(taskDefinition.componentName);
        if (!component) {
            return null;
        }
        const taskId = crypto.randomUUID();
        const title = titleOverride?.trim() || taskDefinition.title;
        const description = descriptionOverride?.trim() || taskDefinition.description;
        const normalizedConfiguration = this.normalizeConfiguration(configuration);
        const toolbarScope = `workspace-task-${taskId}`;
        return {
            id: taskId,
            taskType: taskDefinition.componentName,
            title,
            description,
            component,
            toolbarScope,
            menuItemGuid: null,
            inputs: taskDefinition.kind === 'remote' || taskDefinition.usesTaskConfigurationInputs
                ? {
                    componentConfiguration: normalizedConfiguration ?? {},
                    taskTitle: title,
                    taskDescription: description,
                    toolbarScope
                }
                : undefined,
            route: this.createDefaultRoute(taskDefinition.componentName, title, description, normalizedConfiguration)
        };
    }
    async createMenuTaskInstance(item) {
        const componentName = this.normalizeComponentName(item.componentName);
        const taskDefinition = this.getTaskDefinition(componentName);
        if (!taskDefinition) {
            return null;
        }
        const task = await this.createStaticTaskInstance(taskDefinition, item.title, item.description, this.parseMenuConfiguration(item.componentConfigurationJson));
        if (!task) {
            return null;
        }
        return {
            ...task,
            menuItemGuid: item.guid?.trim() || null
        };
    }
    getTaskDefinition(taskType) {
        return this.taskRegistry.getTaskDefinition(taskType);
    }
    findTaskByRoute(route) {
        return this.openTaskInstancesState().find((task) => this.isSameTaskRoute(task.route, route)) ?? null;
    }
    findTaskByMenuItemGuid(menuItemGuid) {
        const normalizedMenuItemGuid = menuItemGuid.trim();
        if (!normalizedMenuItemGuid) {
            return null;
        }
        return this.openTaskInstancesState().find((task) => task.menuItemGuid === normalizedMenuItemGuid) ?? null;
    }
    isSameTaskRoute(left, right) {
        if (left.taskType !== right.taskType) {
            return false;
        }
        const leftQueryParams = left.queryParams ?? {};
        const rightQueryParams = right.queryParams ?? {};
        const leftKeys = Object.keys(leftQueryParams);
        const rightKeys = Object.keys(rightQueryParams);
        if (leftKeys.length !== rightKeys.length) {
            return false;
        }
        return leftKeys.every((key) => leftQueryParams[key] === rightQueryParams[key]);
    }
    bindSystemThemePreference() {
        if (typeof globalThis.matchMedia !== 'function') {
            return;
        }
        const mediaQuery = globalThis.matchMedia('(prefers-color-scheme: dark)');
        const applySystemTheme = () => {
            if (this.hasExplicitThemePreferenceState()) {
                return;
            }
            this.themeState.set(mediaQuery.matches ? 'dark' : 'bright');
        };
        applySystemTheme();
        const handleChange = () => applySystemTheme();
        mediaQuery.addEventListener('change', handleChange);
        this.destroyRef.onDestroy(() => mediaQuery.removeEventListener('change', handleChange));
    }
    readStoredThemePreference() {
        const storedTheme = globalThis.localStorage?.getItem(WORKSPACE_THEME_STORAGE_KEY)?.trim().toLowerCase();
        switch (storedTheme) {
            case 'dark':
            case 'soft':
            case 'cini':
            case 'bright':
                return storedTheme;
            default:
                return null;
        }
    }
    readSystemThemePreference() {
        return typeof globalThis.matchMedia === 'function' && globalThis.matchMedia('(prefers-color-scheme: dark)').matches
            ? 'dark'
            : 'bright';
    }
    createDefaultRoute(componentName, title, description, configuration) {
        const queryParams = {};
        const normalizedTitle = title?.trim() ?? '';
        if (normalizedTitle) {
            queryParams['title'] = normalizedTitle;
        }
        const normalizedDescription = description?.trim() ?? '';
        if (normalizedDescription) {
            queryParams['description'] = normalizedDescription;
        }
        const serializedConfiguration = this.serializeConfiguration(configuration);
        if (serializedConfiguration) {
            queryParams['config'] = serializedConfiguration;
        }
        return {
            taskType: this.normalizeComponentName(componentName),
            queryParams
        };
    }
    buildWorkspaceQueryParams(activeTask) {
        const activeMenuItem = activeTask?.menuItemGuid?.trim() ?? '';
        return activeMenuItem
            ? { [ACTIVE_MENU_ITEM_QUERY_PARAM]: activeMenuItem }
            : {};
    }
    readActiveMenuItemGuid(queryParams) {
        return queryParams.get(ACTIVE_MENU_ITEM_QUERY_PARAM)?.trim() ?? '';
    }
    findActiveTaskIdForMenuItemGuid(activeMenuItemGuid) {
        if (!activeMenuItemGuid) {
            return null;
        }
        return this.findTaskByMenuItemGuid(activeMenuItemGuid)?.id ?? null;
    }
    findTaskIdByMenuItemGuid(tasks, menuItemGuid) {
        if (!menuItemGuid) {
            return null;
        }
        return tasks.find((task) => task.menuItemGuid === menuItemGuid)?.id ?? null;
    }
    async restoreMenuTaskFromRoute(activeMenuItemGuid) {
        const menuItems = await this.loadMenuItemsByGuids([activeMenuItemGuid]);
        const menuItem = menuItems.get(activeMenuItemGuid);
        if (!menuItem) {
            return null;
        }
        return this.createMenuTaskInstance(menuItem);
    }
    async loadMenuItemsByGuids(targetGuids) {
        const remainingGuids = new Set(targetGuids.map((guid) => guid.trim()).filter((guid) => guid.length > 0));
        const resolvedMenuItems = new Map();
        const parentQueue = [null];
        const visitedParents = new Set();
        while (parentQueue.length > 0 && remainingGuids.size > 0) {
            const parentGuid = parentQueue.shift() ?? null;
            const parentKey = parentGuid ?? '__root__';
            if (visitedParents.has(parentKey)) {
                continue;
            }
            visitedParents.add(parentKey);
            let items = [];
            try {
                items = await firstValueFrom(this.chill.getMenu(parentGuid));
            }
            catch {
                continue;
            }
            for (const item of items) {
                const itemGuid = item.guid?.trim() ?? '';
                if (itemGuid) {
                    parentQueue.push(itemGuid);
                }
                if (!itemGuid || !remainingGuids.has(itemGuid)) {
                    continue;
                }
                resolvedMenuItems.set(itemGuid, item);
                remainingGuids.delete(itemGuid);
            }
        }
        return resolvedMenuItems;
    }
    async confirmAllTasksCanBeClosed() {
        const taskIds = this.openTaskInstancesState().map((task) => task.id);
        for (const taskId of taskIds) {
            const canLeaveTask = await this.confirmTaskCanBeLeft(taskId);
            if (!canLeaveTask) {
                return false;
            }
        }
        return true;
    }
    async confirmTaskCanBeLeft(taskId) {
        const component = this.taskComponentResolver?.(taskId) ?? null;
        if (!component?.isAllSaved) {
            return true;
        }
        let isAllSaved = true;
        try {
            isAllSaved = await component.isAllSaved();
        }
        catch {
            isAllSaved = false;
        }
        if (isAllSaved) {
            return true;
        }
        const taskTitle = this.openTaskInstancesState().find((task) => task.id === taskId)?.title
            || this.chill.T('68E40F26-CE4E-4FD1-9D2A-505B495D1608', 'this task', 'questa attivita');
        return this.dialog.confirmYesNo(this.chill.T('C215A6B1-F772-478D-8FB2-8A7A495F694E', 'Unsaved changes', 'Modifiche non salvate'), this.chill.T('05105AA0-B849-4020-8018-03C97CB92605', `There is unsaved or unfinished work in ${taskTitle}. Do you want to leave it anyway?`, `Ci sono modifiche non salvate o attivita non completate in ${taskTitle}. Vuoi comunque uscire?`));
    }
    normalizeComponentName(value) {
        return value.trim().toLowerCase();
    }
    normalizeConfiguration(configuration) {
        if (!configuration || Object.keys(configuration).length === 0) {
            return null;
        }
        return configuration;
    }
    serializeConfiguration(configuration) {
        if (!configuration) {
            return '';
        }
        try {
            return JSON.stringify(configuration);
        }
        catch {
            return '';
        }
    }
    deserializeConfiguration(rawConfiguration) {
        if (!rawConfiguration?.trim()) {
            return null;
        }
        try {
            const parsed = JSON.parse(rawConfiguration);
            return parsed && typeof parsed === 'object' && !Array.isArray(parsed)
                ? parsed
                : null;
        }
        catch {
            return null;
        }
    }
    parseMenuConfiguration(value) {
        return this.deserializeConfiguration(value);
    }
    toWorkspaceTaskConfiguration(configuration) {
        if (!configuration) {
            return null;
        }
        const entries = Object.entries(configuration)
            .filter(([, value]) => value !== undefined && value !== null);
        if (entries.length === 0) {
            return null;
        }
        return Object.fromEntries(entries);
    }
    buildCrudTaskConfiguration(request) {
        const configuration = this.toWorkspaceTaskConfiguration(request.componentConfiguration) ?? {};
        const chillType = request.chillType.trim();
        const viewCode = request.viewCode?.trim() || 'default';
        const queryChillType = request.queryChillType?.trim() || null;
        return {
            ...configuration,
            chillType,
            viewCode,
            ...(queryChillType ? { chillQuery: queryChillType } : {})
        };
    }
    readConfigString(configuration, keys) {
        if (!configuration) {
            return '';
        }
        for (const key of keys) {
            const value = configuration[key];
            if (typeof value === 'string' && value.trim()) {
                return value.trim();
            }
        }
        const normalizedKeys = keys.map((key) => key.toLowerCase());
        for (const [key, value] of Object.entries(configuration)) {
            if (typeof value === 'string' && value.trim() && normalizedKeys.includes(key.toLowerCase())) {
                return value.trim();
            }
        }
        return '';
    }
    static { this.ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: WorkspaceService, deps: [], target: i0.ɵɵFactoryTarget.Injectable }); }
    static { this.ɵprov = i0.ɵɵngDeclareInjectable({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: WorkspaceService, providedIn: 'root' }); }
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: WorkspaceService, decorators: [{
            type: Injectable,
            args: [{
                    providedIn: 'root'
                }]
        }], ctorParameters: () => [] });

class WorkspaceDialogHostComponent {
    constructor() {
        this.chill = inject(ChillService);
        this.dialog = inject(WorkspaceDialogService);
        this.toolbar = inject(WorkspaceToolbarService);
        this.contentHosts = viewChildren('contentHost', { read: ViewContainerRef });
        this.isBusy = signal(false);
        this.errorMessage = signal('');
        this.toolbarButtons = computed(() => this.toolbar.buttons('dialog'));
        this.contentRefs = new Map();
        this.activeDialog = computed(() => this.dialog.activeDialog());
        this.activeDialogId = 0;
        effect(() => {
            const activeDialog = this.activeDialog();
            const activeDialogId = activeDialog?.id ?? 0;
            if (activeDialogId !== this.activeDialogId) {
                this.activeDialogId = activeDialogId;
                this.isBusy.set(false);
                this.errorMessage.set('');
            }
        });
        effect(() => {
            const dialogs = this.dialog.dialogs();
            const hosts = this.contentHosts();
            if (hosts.length < dialogs.length) {
                return;
            }
            const liveDialogIds = new Set(dialogs.map((activeDialog) => activeDialog.id));
            for (const [dialogId, contentRef] of this.contentRefs) {
                if (liveDialogIds.has(dialogId)) {
                    continue;
                }
                contentRef.destroy();
                this.contentRefs.delete(dialogId);
            }
            dialogs.forEach((activeDialog, index) => {
                if (this.contentRefs.has(activeDialog.id)) {
                    return;
                }
                const host = hosts[index];
                if (!host) {
                    return;
                }
                host.clear();
                const contentRef = host.createComponent(activeDialog.component);
                for (const [key, value] of Object.entries(activeDialog.inputs ?? {})) {
                    contentRef.setInput(key, value);
                }
                this.contentRefs.set(activeDialog.id, contentRef);
            });
        });
    }
    isTopDialog(dialogId) {
        return this.activeDialog()?.id === dialogId;
    }
    cancel(dialogId) {
        if (this.isBusy() || (dialogId !== undefined && !this.isTopDialog(dialogId))) {
            return;
        }
        this.dialog.cancel();
    }
    async confirm(dialogId) {
        if (this.isBusy() || (dialogId !== undefined && !this.isTopDialog(dialogId))) {
            return;
        }
        const activeDialog = this.activeDialog();
        if (!activeDialog) {
            return;
        }
        const contentRef = this.contentRefs.get(activeDialog.id) ?? null;
        this.errorMessage.set('');
        try {
            const componentInstance = contentRef?.instance;
            if (this.isDialogSubmitter(componentInstance)) {
                await componentInstance.submit();
                return;
            }
            this.isBusy.set(true);
            const value = this.isDialogTask(componentInstance)
                ? await componentInstance.dialogResult?.()
                : undefined;
            this.dialog.confirm(value);
        }
        catch (error) {
            this.errorMessage.set(this.chill.formatError(error));
            this.isBusy.set(false);
        }
    }
    canConfirm() {
        const activeDialog = this.activeDialog();
        const componentInstance = activeDialog
            ? this.contentRefs.get(activeDialog.id)?.instance
            : null;
        return this.isDialogSubmitter(componentInstance)
            ? (componentInstance.canDialogSubmit?.() ?? true)
            : true;
    }
    isDialogTask(value) {
        return !!value && typeof value === 'object' && 'dialogResult' in value;
    }
    isDialogSubmitter(value) {
        return !!value && typeof value === 'object' && 'submit' in value && typeof value.submit === 'function';
    }
    static { this.ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: WorkspaceDialogHostComponent, deps: [], target: i0.ɵɵFactoryTarget.Component }); }
    static { this.ɵcmp = i0.ɵɵngDeclareComponent({ minVersion: "17.0.0", version: "19.2.21", type: WorkspaceDialogHostComponent, isStandalone: true, selector: "app-workspace-dialog-host", viewQueries: [{ propertyName: "contentHosts", predicate: ["contentHost"], descendants: true, read: ViewContainerRef, isSignal: true }], ngImport: i0, template: `
    @if (dialog.dialogs().length > 0) {
      @for (activeDialog of dialog.dialogs(); track activeDialog.id) {
        @if (isTopDialog(activeDialog.id)) {
          <div class="workspace-dialog-backdrop" (click)="cancel(activeDialog.id)"></div>
        }

        <section
          class="workspace-dialog"
          [ngClass]="activeDialog.panelClass"
          [class.is-background]="!isTopDialog(activeDialog.id)"
          role="dialog"
          [attr.aria-modal]="isTopDialog(activeDialog.id) ? 'true' : null"
          [attr.aria-hidden]="isTopDialog(activeDialog.id) ? null : 'true'"
          [attr.aria-label]="activeDialog.title">
        <header class="workspace-dialog__toolbar">
          <div class="workspace-dialog__title">
            <p>{{ chill.T('4ED0A2E7-CFF1-4593-8861-18B9EBF9F10A', 'Task dialog', 'Dialog attivita') }}</p>
            <h2>{{ activeDialog.title }}</h2>
          </div>

          @if (isTopDialog(activeDialog.id) && toolbarButtons().length > 0) {
            <div class="workspace-dialog__toolbar-actions">
              @for (button of toolbarButtons(); track button.id) {
                <button
                  type="button"
                  class="workspace-dialog__toolbar-button"
                  (click)="button.action()"
                  [disabled]="button.disabled"
                  [attr.aria-label]="button.ariaLabel || button.label || button.primaryDefaultText">
                  @if (button.icon) {
                    <span
                      class="workspace-dialog__toolbar-button-icon"
                      [class.material-symbol-icon]="button.iconClass === 'material-symbol-icon'"
                      aria-hidden="true">{{ button.icon }}</span>
                  }
                  @if (button.labelGuid && button.primaryDefaultText && button.secondaryDefaultText) {
                    <app-chill-i18n-button-label
                      [labelGuid]="button.labelGuid"
                      [primaryDefaultText]="button.primaryDefaultText"
                      [secondaryDefaultText]="button.secondaryDefaultText" />
                  } @else {
                    <span>{{ button.label }}</span>
                  }
                </button>
              }
            </div>
          }

          <button
            type="button"
            class="workspace-dialog__close"
            (click)="cancel(activeDialog.id)"
            [disabled]="isBusy() || !isTopDialog(activeDialog.id)"
            [attr.aria-label]="activeDialog.cancelLabel || chill.T('C10CCB95-A6D7-40F1-ACAD-3A8F318958C2', 'Close dialog', 'Chiudi dialog')">
            x
          </button>
        </header>

        <div class="workspace-dialog__content">
          <ng-template #contentHost />
        </div>

        <footer class="workspace-dialog__bottom-bar">
          @if (errorMessage()) {
            <p class="workspace-dialog__error">{{ errorMessage() }}</p>
          }

          <div class="workspace-dialog__actions">
            @if (activeDialog.showCancelButton !== false) {
              <button type="button" class="workspace-dialog__button secondary" (click)="cancel(activeDialog.id)" [disabled]="isBusy() || !isTopDialog(activeDialog.id)">
                @if (activeDialog.cancelLabel) {
                  {{ activeDialog.cancelLabel }}
                } @else {
                  <app-chill-i18n-button-label [labelGuid]="'4DA4C4BA-0D5B-41B5-B49D-685D7C374D71'" [primaryDefaultText]="'Cancel'" [secondaryDefaultText]="'Annulla'" />
                }
              </button>
            }

            @if (activeDialog.showOkButton !== false) {
              <button type="button" class="workspace-dialog__button primary" (click)="confirm(activeDialog.id)" [disabled]="isBusy() || !isTopDialog(activeDialog.id) || !canConfirm()">
                {{ isBusy()
                  ? chill.T('08325F54-06AE-40E5-93EF-3C49B8E0B965', 'Working...', 'Elaborazione...')
                  : '' }}
                @if (!isBusy()) {
                  @if (activeDialog.okLabel) {
                    {{ activeDialog.okLabel }}
                  } @else {
                    <app-chill-i18n-button-label [labelGuid]="'AF183C6E-44B2-4CB3-97F7-01F0E7D01214'" [primaryDefaultText]="'OK'" [secondaryDefaultText]="'OK'" />
                  }
                }
                @if (isBusy()) {
                  <app-chill-i18n-button-label [labelGuid]="'08325F54-06AE-40E5-93EF-3C49B8E0B965'" [primaryDefaultText]="'Working...'" [secondaryDefaultText]="'Elaborazione...'" />
                }
              </button>
            }
          </div>
        </footer>
        </section>
      }
    }
  `, isInline: true, styles: [":host{position:fixed;inset:0;z-index:40;pointer-events:none}.workspace-dialog-backdrop,.workspace-dialog{pointer-events:auto}.workspace-dialog-backdrop{position:absolute;inset:0;background:#070f1473;backdrop-filter:blur(8px)}.workspace-dialog{position:absolute;top:50%;left:50%;width:min(72rem,calc(100vw - 2rem));max-height:calc(100vh - 2rem);display:grid;grid-template-rows:auto minmax(0,1fr) auto;transform:translate(-50%,-50%);border-radius:1rem;border:1px solid var(--border-color);background:var(--surface-3);box-shadow:var(--shadow);overflow:hidden}.workspace-dialog.is-background{pointer-events:none}.workspace-dialog__toolbar,.workspace-dialog__bottom-bar{display:flex;align-items:center;justify-content:space-between;gap:1rem;padding:1rem 1.25rem;border-bottom:1px solid var(--border-color);background:color-mix(in srgb,var(--surface-2) 88%,transparent)}.workspace-dialog__bottom-bar{border-top:1px solid var(--border-color);border-bottom:0}.workspace-dialog__title p,.workspace-dialog__title h2{margin:0}.workspace-dialog__title p{color:var(--accent);text-transform:uppercase;letter-spacing:.16em;font-size:.72rem;font-weight:700}.workspace-dialog__title h2{margin-top:.25rem;font-size:1.25rem}.workspace-dialog__close,.workspace-dialog__button{border:0;cursor:pointer;font:inherit}.workspace-dialog__toolbar-actions{display:flex;flex:1 1 auto;justify-content:flex-end;flex-wrap:wrap;gap:.65rem}.workspace-dialog__toolbar-button{min-height:2.5rem;padding:.6rem .9rem;border:1px solid var(--border-color);border-radius:999px;background:var(--surface-0);color:var(--text-main);cursor:pointer;font:inherit;font-weight:700;display:inline-flex;align-items:center;gap:.45rem}.workspace-dialog__toolbar-button:disabled{cursor:not-allowed;opacity:.65}.workspace-dialog__toolbar-button-icon{font-size:.8rem}.workspace-dialog__close{width:2.5rem;height:2.5rem;border-radius:.8rem;background:var(--surface-2);color:var(--text-main)}.workspace-dialog__content{min-height:0;overflow:auto;padding:1.25rem}.workspace-dialog__actions{margin-left:auto;display:flex;flex-wrap:wrap;gap:.75rem}.workspace-dialog__button{min-height:2.9rem;padding:.75rem 1.1rem;border-radius:.8rem;font-weight:700}.workspace-dialog__button.secondary{border:1px solid var(--border-color);background:var(--surface-0);color:var(--text-main)}.workspace-dialog__button.primary{background:linear-gradient(135deg,var(--accent),var(--accent-strong));color:var(--surface-0)}.workspace-dialog__error{margin:0;color:var(--danger);max-width:100%;max-height:5rem;overflow-x:hidden;overflow-y:auto;overflow-wrap:anywhere;word-break:break-word}@media(max-width:720px){.workspace-dialog{width:calc(100vw - 1rem);max-height:calc(100vh - 1rem)}.workspace-dialog.workspace-dialog--mobile-full-height{top:0;left:0;width:100vw;height:100vh;max-height:100vh;border-width:0;border-radius:0;transform:none}.workspace-dialog.workspace-dialog--mobile-full-height .workspace-dialog__content{display:grid}.workspace-dialog__toolbar,.workspace-dialog__bottom-bar,.workspace-dialog__content{padding:.9rem}.workspace-dialog__toolbar{flex-wrap:wrap}.workspace-dialog__toolbar-actions{width:100%;justify-content:stretch}.workspace-dialog__bottom-bar{flex-direction:column;align-items:stretch}.workspace-dialog__actions{width:100%}.workspace-dialog__button{flex:1 1 0}.workspace-dialog__toolbar-button{flex:1 1 10rem;justify-content:center}}\n"], dependencies: [{ kind: "ngmodule", type: CommonModule }, { kind: "directive", type: i1$1.NgClass, selector: "[ngClass]", inputs: ["class", "ngClass"] }, { kind: "component", type: ChillI18nButtonLabelComponent, selector: "app-chill-i18n-button-label", inputs: ["labelGuid", "primaryDefaultText", "secondaryDefaultText", "editable"] }] }); }
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: WorkspaceDialogHostComponent, decorators: [{
            type: Component,
            args: [{ selector: 'app-workspace-dialog-host', standalone: true, imports: [CommonModule, ChillI18nButtonLabelComponent], template: `
    @if (dialog.dialogs().length > 0) {
      @for (activeDialog of dialog.dialogs(); track activeDialog.id) {
        @if (isTopDialog(activeDialog.id)) {
          <div class="workspace-dialog-backdrop" (click)="cancel(activeDialog.id)"></div>
        }

        <section
          class="workspace-dialog"
          [ngClass]="activeDialog.panelClass"
          [class.is-background]="!isTopDialog(activeDialog.id)"
          role="dialog"
          [attr.aria-modal]="isTopDialog(activeDialog.id) ? 'true' : null"
          [attr.aria-hidden]="isTopDialog(activeDialog.id) ? null : 'true'"
          [attr.aria-label]="activeDialog.title">
        <header class="workspace-dialog__toolbar">
          <div class="workspace-dialog__title">
            <p>{{ chill.T('4ED0A2E7-CFF1-4593-8861-18B9EBF9F10A', 'Task dialog', 'Dialog attivita') }}</p>
            <h2>{{ activeDialog.title }}</h2>
          </div>

          @if (isTopDialog(activeDialog.id) && toolbarButtons().length > 0) {
            <div class="workspace-dialog__toolbar-actions">
              @for (button of toolbarButtons(); track button.id) {
                <button
                  type="button"
                  class="workspace-dialog__toolbar-button"
                  (click)="button.action()"
                  [disabled]="button.disabled"
                  [attr.aria-label]="button.ariaLabel || button.label || button.primaryDefaultText">
                  @if (button.icon) {
                    <span
                      class="workspace-dialog__toolbar-button-icon"
                      [class.material-symbol-icon]="button.iconClass === 'material-symbol-icon'"
                      aria-hidden="true">{{ button.icon }}</span>
                  }
                  @if (button.labelGuid && button.primaryDefaultText && button.secondaryDefaultText) {
                    <app-chill-i18n-button-label
                      [labelGuid]="button.labelGuid"
                      [primaryDefaultText]="button.primaryDefaultText"
                      [secondaryDefaultText]="button.secondaryDefaultText" />
                  } @else {
                    <span>{{ button.label }}</span>
                  }
                </button>
              }
            </div>
          }

          <button
            type="button"
            class="workspace-dialog__close"
            (click)="cancel(activeDialog.id)"
            [disabled]="isBusy() || !isTopDialog(activeDialog.id)"
            [attr.aria-label]="activeDialog.cancelLabel || chill.T('C10CCB95-A6D7-40F1-ACAD-3A8F318958C2', 'Close dialog', 'Chiudi dialog')">
            x
          </button>
        </header>

        <div class="workspace-dialog__content">
          <ng-template #contentHost />
        </div>

        <footer class="workspace-dialog__bottom-bar">
          @if (errorMessage()) {
            <p class="workspace-dialog__error">{{ errorMessage() }}</p>
          }

          <div class="workspace-dialog__actions">
            @if (activeDialog.showCancelButton !== false) {
              <button type="button" class="workspace-dialog__button secondary" (click)="cancel(activeDialog.id)" [disabled]="isBusy() || !isTopDialog(activeDialog.id)">
                @if (activeDialog.cancelLabel) {
                  {{ activeDialog.cancelLabel }}
                } @else {
                  <app-chill-i18n-button-label [labelGuid]="'4DA4C4BA-0D5B-41B5-B49D-685D7C374D71'" [primaryDefaultText]="'Cancel'" [secondaryDefaultText]="'Annulla'" />
                }
              </button>
            }

            @if (activeDialog.showOkButton !== false) {
              <button type="button" class="workspace-dialog__button primary" (click)="confirm(activeDialog.id)" [disabled]="isBusy() || !isTopDialog(activeDialog.id) || !canConfirm()">
                {{ isBusy()
                  ? chill.T('08325F54-06AE-40E5-93EF-3C49B8E0B965', 'Working...', 'Elaborazione...')
                  : '' }}
                @if (!isBusy()) {
                  @if (activeDialog.okLabel) {
                    {{ activeDialog.okLabel }}
                  } @else {
                    <app-chill-i18n-button-label [labelGuid]="'AF183C6E-44B2-4CB3-97F7-01F0E7D01214'" [primaryDefaultText]="'OK'" [secondaryDefaultText]="'OK'" />
                  }
                }
                @if (isBusy()) {
                  <app-chill-i18n-button-label [labelGuid]="'08325F54-06AE-40E5-93EF-3C49B8E0B965'" [primaryDefaultText]="'Working...'" [secondaryDefaultText]="'Elaborazione...'" />
                }
              </button>
            }
          </div>
        </footer>
        </section>
      }
    }
  `, styles: [":host{position:fixed;inset:0;z-index:40;pointer-events:none}.workspace-dialog-backdrop,.workspace-dialog{pointer-events:auto}.workspace-dialog-backdrop{position:absolute;inset:0;background:#070f1473;backdrop-filter:blur(8px)}.workspace-dialog{position:absolute;top:50%;left:50%;width:min(72rem,calc(100vw - 2rem));max-height:calc(100vh - 2rem);display:grid;grid-template-rows:auto minmax(0,1fr) auto;transform:translate(-50%,-50%);border-radius:1rem;border:1px solid var(--border-color);background:var(--surface-3);box-shadow:var(--shadow);overflow:hidden}.workspace-dialog.is-background{pointer-events:none}.workspace-dialog__toolbar,.workspace-dialog__bottom-bar{display:flex;align-items:center;justify-content:space-between;gap:1rem;padding:1rem 1.25rem;border-bottom:1px solid var(--border-color);background:color-mix(in srgb,var(--surface-2) 88%,transparent)}.workspace-dialog__bottom-bar{border-top:1px solid var(--border-color);border-bottom:0}.workspace-dialog__title p,.workspace-dialog__title h2{margin:0}.workspace-dialog__title p{color:var(--accent);text-transform:uppercase;letter-spacing:.16em;font-size:.72rem;font-weight:700}.workspace-dialog__title h2{margin-top:.25rem;font-size:1.25rem}.workspace-dialog__close,.workspace-dialog__button{border:0;cursor:pointer;font:inherit}.workspace-dialog__toolbar-actions{display:flex;flex:1 1 auto;justify-content:flex-end;flex-wrap:wrap;gap:.65rem}.workspace-dialog__toolbar-button{min-height:2.5rem;padding:.6rem .9rem;border:1px solid var(--border-color);border-radius:999px;background:var(--surface-0);color:var(--text-main);cursor:pointer;font:inherit;font-weight:700;display:inline-flex;align-items:center;gap:.45rem}.workspace-dialog__toolbar-button:disabled{cursor:not-allowed;opacity:.65}.workspace-dialog__toolbar-button-icon{font-size:.8rem}.workspace-dialog__close{width:2.5rem;height:2.5rem;border-radius:.8rem;background:var(--surface-2);color:var(--text-main)}.workspace-dialog__content{min-height:0;overflow:auto;padding:1.25rem}.workspace-dialog__actions{margin-left:auto;display:flex;flex-wrap:wrap;gap:.75rem}.workspace-dialog__button{min-height:2.9rem;padding:.75rem 1.1rem;border-radius:.8rem;font-weight:700}.workspace-dialog__button.secondary{border:1px solid var(--border-color);background:var(--surface-0);color:var(--text-main)}.workspace-dialog__button.primary{background:linear-gradient(135deg,var(--accent),var(--accent-strong));color:var(--surface-0)}.workspace-dialog__error{margin:0;color:var(--danger);max-width:100%;max-height:5rem;overflow-x:hidden;overflow-y:auto;overflow-wrap:anywhere;word-break:break-word}@media(max-width:720px){.workspace-dialog{width:calc(100vw - 1rem);max-height:calc(100vh - 1rem)}.workspace-dialog.workspace-dialog--mobile-full-height{top:0;left:0;width:100vw;height:100vh;max-height:100vh;border-width:0;border-radius:0;transform:none}.workspace-dialog.workspace-dialog--mobile-full-height .workspace-dialog__content{display:grid}.workspace-dialog__toolbar,.workspace-dialog__bottom-bar,.workspace-dialog__content{padding:.9rem}.workspace-dialog__toolbar{flex-wrap:wrap}.workspace-dialog__toolbar-actions{width:100%;justify-content:stretch}.workspace-dialog__bottom-bar{flex-direction:column;align-items:stretch}.workspace-dialog__actions{width:100%}.workspace-dialog__button{flex:1 1 0}.workspace-dialog__toolbar-button{flex:1 1 10rem;justify-content:center}}\n"] }]
        }], ctorParameters: () => [] });

function applySchemaRelationsToCrudConfiguration(baseConfiguration, schema) {
    return {
        ...baseConfiguration,
        relations: buildCrudRelationsFromSchema(schema)
    };
}
function buildCrudRelationsFromSchema(schema) {
    if (!schema || !Array.isArray(schema.relations)) {
        return [];
    }
    return schema.relations
        .map((relation) => mapSchemaRelationToCrudConfiguration(relation))
        .filter((relation) => relation !== null);
}
function mapSchemaRelationToCrudConfiguration(relation) {
    const chillType = normalizeString(relation.chillType);
    if (!chillType) {
        return null;
    }
    const configuration = {
        chillType
    };
    const chillQuery = normalizeString(relation.chillQuery);
    if (chillQuery) {
        configuration['chillQuery'] = chillQuery;
    }
    const fixedValues = normalizeJsonRecord(relation.fixedValues);
    if (Object.keys(fixedValues).length > 0) {
        configuration['fixedValues'] = fixedValues;
    }
    const fixedQueryValues = normalizeJsonRecord(relation.fixedQueryValues);
    if (Object.keys(fixedQueryValues).length > 0) {
        configuration['fixedQueryValues'] = fixedQueryValues;
    }
    const relationLabel = mapRelationLabel(relation.relationLabel);
    if (relationLabel) {
        configuration['relationLabel'] = relationLabel;
    }
    return configuration;
}
function mapRelationLabel(relationLabel) {
    if (!relationLabel) {
        return null;
    }
    const labelGuid = normalizeString(relationLabel.labelGuid);
    const primaryDefaultText = normalizeString(relationLabel.primaryDefaultText);
    const secondaryDefaultText = normalizeString(relationLabel.secondaryDefaultText);
    if (!labelGuid || !primaryDefaultText || !secondaryDefaultText) {
        return null;
    }
    return {
        labelGuid,
        primaryDefaultText,
        secondaryDefaultText
    };
}
function normalizeJsonRecord(value) {
    if (!value || typeof value !== 'object' || Array.isArray(value)) {
        return {};
    }
    return Object.fromEntries(Object.entries(value)
        .map(([key, entryValue]) => [key.trim(), entryValue])
        .filter(([key]) => key.length > 0));
}
function normalizeString(value) {
    return typeof value === 'string'
        ? value.trim()
        : '';
}

class WorkspaceMenuComponent {
    constructor() {
        this.chill = inject(ChillService);
        this.workspace = inject(WorkspaceService);
        this.dialog = inject(WorkspaceDialogService);
        this.layout = inject(WorkspaceLayoutService);
        this.isLoadingSchemas = signal(true);
        this.schemaLoadError = signal('');
        this.crudTypes = signal([]);
        this.entityOptionsTypes = signal([]);
        this.selectedModule = signal('');
        this.selectedChillType = signal('');
        this.selectedEntityOptionsModule = signal('');
        this.selectedEntityOptionsChillType = signal('');
        this.viewCode = signal('default');
        this.isLoadingMenu = signal(true);
        this.menuLoadError = signal('');
        this.menuRoots = signal([]);
        this.draggedMenuItemGuid = signal('');
        this.dragHoverExpandGuid = signal('');
        this.dragHoverExpandTimers = new Map();
        this.quickTasks = computed(() => this.workspace.availableTasks()
            .filter((task) => task.showInQuickLaunch && task.componentName !== 'crud' && task.componentName !== 'permissions'));
        this.moduleOptions = computed(() => [...new Set(this.crudTypes().map((schema) => schema.module))]);
        this.filteredCrudTypes = computed(() => this.crudTypes()
            .filter((schema) => schema.module === this.selectedModule()));
        this.selectedCrudSchema = computed(() => this.filteredCrudTypes()
            .find((schema) => schema.chillType === this.selectedChillType()) ?? null);
        this.entityOptionsModuleOptions = computed(() => [...new Set(this.entityOptionsTypes().map((schema) => schema.module))]);
        this.filteredEntityOptionsTypes = computed(() => this.entityOptionsTypes()
            .filter((schema) => schema.module === this.selectedEntityOptionsModule()));
        this.selectedEntityOptionsSchema = computed(() => this.filteredEntityOptionsTypes()
            .find((schema) => schema.chillType === this.selectedEntityOptionsChillType()) ?? null);
    }
    ngOnInit() {
        this.loadCrudTypes();
        void this.loadRootMenu();
    }
    ngOnDestroy() {
        this.clearAllDragHoverExpandTimers();
    }
    selectModule(module) {
        this.selectedModule.set(module);
        const firstSchema = this.filteredCrudTypes()[0] ?? null;
        this.selectedChillType.set(firstSchema?.chillType ?? '');
    }
    selectEntityOptionsModule(module) {
        this.selectedEntityOptionsModule.set(module);
        const firstSchema = this.filteredEntityOptionsTypes()[0] ?? null;
        this.selectedEntityOptionsChillType.set(firstSchema?.chillType ?? '');
    }
    openCrudTask() {
        const schema = this.selectedCrudSchema();
        if (!schema) {
            return;
        }
        this.workspace.openCrudTask({
            chillType: schema.chillType,
            queryChillType: schema.queryChillType,
            viewCode: this.normalizeViewCode(this.viewCode()),
            displayName: schema.displayName
        });
    }
    async addCrudTaskToMenu() {
        const schema = this.selectedCrudSchema();
        if (!schema) {
            return;
        }
        try {
            const configuration = await this.buildCrudMenuConfiguration(schema);
            const savedItem = await firstValueFrom(this.chill.setMenu({
                guid: crypto.randomUUID(),
                positionNo: this.menuRoots().length + 1,
                title: schema.displayName || schema.chillType,
                description: `CRUD task for ${schema.chillType}`,
                parent: null,
                componentName: 'crud',
                componentConfigurationJson: JSON.stringify(configuration, null, 2),
                menuHierarchy: schema.module
            }));
            await this.refreshMenuBranch(savedItem.parent?.guid ?? null);
        }
        catch (error) {
            this.schemaLoadError.set(this.chill.formatError(error));
        }
    }
    async openEntityOptionsDialog() {
        const schema = this.selectedEntityOptionsSchema();
        if (!schema) {
            return;
        }
        const { EntityOptionsDialogComponent } = await Promise.resolve().then(function () { return entityOptionsDialog_component; });
        await this.dialog.openDialog({
            title: `${schema.displayName || schema.chillType} entity options`,
            component: EntityOptionsDialogComponent,
            inputs: {
                chillType: schema.chillType,
                displayName: schema.displayName
            },
            okLabel: this.chill.T('62953302-B951-4FD1-BD08-4B7649A91BAF', 'Save', 'Salva')
        });
    }
    openQuickTask(componentName) {
        void this.workspace.openTask(componentName);
    }
    normalizeViewCode(value) {
        const normalizedValue = value.trim();
        return normalizedValue ? normalizedValue : 'default';
    }
    toggleNode(node) {
        if (!node.hasChildren && !node.isLoadingChildren) {
            return;
        }
        if (!node.childrenLoaded && !node.isLoadingChildren) {
            void this.loadNodeChildren(node, true);
            return;
        }
        this.menuRoots.update((roots) => this.updateNodeCollection(roots, node.item.guid, (current) => ({
            ...current,
            isExpanded: !current.isExpanded
        })));
    }
    openMenuItem(item) {
        void this.workspace.openMenuItem(item);
    }
    beginMenuDrag(item) {
        if (!this.layout.isLayoutEditingEnabled()) {
            return;
        }
        this.draggedMenuItemGuid.set(item.guid);
    }
    hoverMenuItem(node) {
        if (!this.layout.isLayoutEditingEnabled() || !this.draggedMenuItemGuid() || node.isExpanded || node.isLoadingChildren) {
            return;
        }
        const draggedGuid = this.draggedMenuItemGuid();
        if (!draggedGuid || draggedGuid === node.item.guid) {
            return;
        }
        const sourceContext = this.findNodeContext(draggedGuid, this.menuRoots(), null);
        if (!sourceContext || this.isDescendantOf(sourceContext.node, node.item.guid)) {
            return;
        }
        if (this.dragHoverExpandTimers.has(node.item.guid)) {
            return;
        }
        this.dragHoverExpandGuid.set(node.item.guid);
        const timer = setTimeout(() => {
            this.dragHoverExpandTimers.delete(node.item.guid);
            this.dragHoverExpandGuid.update((current) => current === node.item.guid ? '' : current);
            void this.expandNodeForHover(node);
        }, 1000);
        this.dragHoverExpandTimers.set(node.item.guid, timer);
    }
    leaveMenuItem(node, event) {
        const nextTarget = event.relatedTarget;
        if (nextTarget instanceof Node && event.currentTarget instanceof Node && event.currentTarget.contains(nextTarget)) {
            return;
        }
        this.clearDragHoverExpandTimer(node.item.guid);
    }
    allowMenuDrop(event) {
        if (!this.layout.isLayoutEditingEnabled() || !this.draggedMenuItemGuid()) {
            return;
        }
        event.preventDefault();
    }
    async dropMenuItem(parent, targetIndex) {
        const sourceGuid = this.draggedMenuItemGuid();
        this.draggedMenuItemGuid.set('');
        this.clearAllDragHoverExpandTimers();
        if (!sourceGuid) {
            return;
        }
        const sourceContext = this.findNodeContext(sourceGuid, this.menuRoots(), null);
        if (!sourceContext) {
            return;
        }
        if (parent && (parent.guid === sourceGuid || this.isDescendantOf(sourceContext.node, parent.guid))) {
            return;
        }
        const sourceParentGuid = sourceContext.parent?.item.guid ?? null;
        const targetParentGuid = parent?.guid ?? null;
        const sourceSiblings = [...sourceContext.siblings];
        const targetSiblings = sourceParentGuid === targetParentGuid
            ? sourceSiblings
            : [...(this.findChildCollection(targetParentGuid) ?? [])];
        if (sourceParentGuid !== targetParentGuid && !this.findChildCollection(targetParentGuid)) {
            return;
        }
        const [movedNode] = sourceSiblings.splice(sourceContext.index, 1);
        if (!movedNode) {
            return;
        }
        const normalizedTargetIndex = sourceParentGuid === targetParentGuid && sourceContext.index < targetIndex
            ? targetIndex - 1
            : targetIndex;
        const boundedTargetIndex = Math.max(0, Math.min(normalizedTargetIndex, targetSiblings.length));
        if (sourceParentGuid === targetParentGuid && boundedTargetIndex === sourceContext.index) {
            return;
        }
        const movedParent = parent ? this.toParentReference(parent) : null;
        targetSiblings.splice(boundedTargetIndex, 0, {
            ...movedNode,
            item: {
                ...movedNode.item,
                parent: movedParent
            }
        });
        const itemsToSave = sourceParentGuid === targetParentGuid
            ? this.reindexMenuItems(targetSiblings, movedParent)
            : [
                ...this.reindexMenuItems(sourceSiblings, sourceContext.parent ? this.toParentReference(sourceContext.parent.item) : null),
                ...this.reindexMenuItems(targetSiblings, movedParent)
            ];
        for (const item of itemsToSave) {
            await firstValueFrom(this.chill.setMenu(item));
        }
        await this.loadRootMenu();
    }
    async dropMenuItemAsChild(node, event) {
        event.preventDefault();
        event.stopPropagation();
        await this.dropMenuItem(node.item, node.children.length);
    }
    endMenuDrag() {
        this.draggedMenuItemGuid.set('');
        this.clearAllDragHoverExpandTimers();
    }
    isDropTarget(parent, targetIndex) {
        const draggedGuid = this.draggedMenuItemGuid();
        if (!draggedGuid) {
            return false;
        }
        const sourceContext = this.findNodeContext(draggedGuid, this.menuRoots(), null);
        if (!sourceContext) {
            return false;
        }
        const sourceParentGuid = sourceContext.parent?.item.guid ?? null;
        const targetParentGuid = parent?.guid ?? null;
        const normalizedTargetIndex = sourceParentGuid === targetParentGuid && sourceContext.index < targetIndex
            ? targetIndex - 1
            : targetIndex;
        return sourceParentGuid === targetParentGuid && normalizedTargetIndex === sourceContext.index;
    }
    isMenuTaskActive(item) {
        return this.workspace.isMenuItemActive(item);
    }
    async createMenuItem(parent) {
        await this.editOrCreateMenuItem(null, parent);
    }
    async editMenuItem(item) {
        await this.editOrCreateMenuItem(item, item.parent);
    }
    async deleteMenuItem(item) {
        const confirmed = await this.dialog.confirmYesNo(this.chill.T('601728DD-B38F-4B1D-B3AC-4B4BC2A49D6B', 'Delete menu item', 'Elimina voce di menu'), this.chill.T('0B714FA0-6F35-4C99-8C0C-C5EC2955B5B5', `Delete "${item.title}" from the application menu?`, `Eliminare "${item.title}" dal menu applicazione?`));
        if (!confirmed) {
            return;
        }
        await firstValueFrom(this.chill.deleteMenu(item.guid));
        await this.refreshMenuBranch(item.parent?.guid ?? null);
    }
    loadCrudTypes() {
        this.isLoadingSchemas.set(true);
        this.schemaLoadError.set('');
        this.chill.getSchemaList().subscribe({
            next: (schemaList) => {
                const crudTypes = schemaList
                    .filter((schema) => this.isQuerySchema(schema))
                    .map((schema) => this.toCrudSchemaOption(schema))
                    .sort((left, right) => left.displayName.localeCompare(right.displayName));
                const entityOptionsTypes = schemaList
                    .map((schema) => this.toEntityOptionsSchemaOption(schema))
                    .filter((schema) => schema !== null)
                    .filter((schema, index, options) => index === options.findIndex((option) => option.module === schema.module && option.chillType === schema.chillType))
                    .sort((left, right) => left.displayName.localeCompare(right.displayName));
                this.crudTypes.set(crudTypes);
                this.entityOptionsTypes.set(entityOptionsTypes);
                this.isLoadingSchemas.set(false);
                const firstModule = crudTypes[0]?.module ?? '';
                this.selectedModule.set(firstModule);
                const firstSchema = crudTypes.find((schema) => schema.module === firstModule) ?? null;
                this.selectedChillType.set(firstSchema?.chillType ?? '');
                const firstEntityOptionsModule = entityOptionsTypes[0]?.module ?? '';
                this.selectedEntityOptionsModule.set(firstEntityOptionsModule);
                const firstEntityOptionsSchema = entityOptionsTypes.find((schema) => schema.module === firstEntityOptionsModule) ?? null;
                this.selectedEntityOptionsChillType.set(firstEntityOptionsSchema?.chillType ?? '');
            },
            error: (error) => {
                this.crudTypes.set([]);
                this.entityOptionsTypes.set([]);
                this.schemaLoadError.set(this.chill.formatError(error));
                this.isLoadingSchemas.set(false);
            }
        });
    }
    async loadRootMenu() {
        this.isLoadingMenu.set(true);
        this.menuLoadError.set('');
        try {
            const items = await firstValueFrom(this.chill.getMenu());
            const roots = items.map((item) => this.createNode(item));
            this.menuRoots.set(roots);
            this.isLoadingMenu.set(false);
            await Promise.all(roots.map((node) => this.preloadNodeChildren(node)));
        }
        catch (error) {
            this.menuRoots.set([]);
            this.menuLoadError.set(this.chill.formatError(error));
            this.isLoadingMenu.set(false);
        }
    }
    async preloadNodeChildren(node) {
        if (!node.item.guid.trim()) {
            return;
        }
        this.menuRoots.update((roots) => this.updateNodeCollection(roots, node.item.guid, (current) => ({
            ...current,
            isLoadingChildren: true,
            childrenError: ''
        })));
        try {
            const children = await firstValueFrom(this.chill.getMenu(node.item.guid));
            const childNodes = children.map((item) => this.createNode(item));
            this.menuRoots.update((roots) => this.updateNodeCollection(roots, node.item.guid, (current) => ({
                ...current,
                children: childNodes,
                childrenLoaded: true,
                isLoadingChildren: false,
                hasChildren: childNodes.length > 0
            })));
            await Promise.all(childNodes.map((child) => this.preloadNodeChildren(child)));
        }
        catch (error) {
            this.menuRoots.update((roots) => this.updateNodeCollection(roots, node.item.guid, (current) => ({
                ...current,
                childrenLoaded: false,
                isLoadingChildren: false,
                hasChildren: false,
                childrenError: this.chill.formatError(error)
            })));
        }
    }
    async loadNodeChildren(node, expandAfterLoad) {
        this.menuRoots.update((roots) => this.updateNodeCollection(roots, node.item.guid, (current) => ({
            ...current,
            isLoadingChildren: true,
            childrenError: ''
        })));
        try {
            const children = await firstValueFrom(this.chill.getMenu(node.item.guid));
            const childNodes = children.map((item) => this.createNode(item));
            this.menuRoots.update((roots) => this.updateNodeCollection(roots, node.item.guid, (current) => ({
                ...current,
                children: childNodes,
                childrenLoaded: true,
                isExpanded: expandAfterLoad,
                isLoadingChildren: false,
                hasChildren: childNodes.length > 0
            })));
            await Promise.all(childNodes.map((child) => this.preloadNodeChildren(child)));
        }
        catch (error) {
            this.menuRoots.update((roots) => this.updateNodeCollection(roots, node.item.guid, (current) => ({
                ...current,
                childrenLoaded: false,
                isLoadingChildren: false,
                hasChildren: false,
                childrenError: this.chill.formatError(error)
            })));
        }
    }
    isQuerySchema(item) {
        const type = item.type?.trim().toLowerCase() ?? '';
        const name = item.name?.trim().toLowerCase() ?? '';
        const chillType = item.chillType?.trim().toLowerCase() ?? '';
        return type === 'query'
            || name.endsWith('query')
            || chillType.includes('.query.')
            || chillType.endsWith('.query');
    }
    toCrudSchemaOption(schema) {
        const chillType = schema.relatedChillType?.trim() || schema.chillType?.trim() || '';
        return {
            module: schema.module?.trim() || chillType.split('.')[0] || 'Default',
            chillType,
            queryChillType: schema.chillType?.trim() ?? '',
            displayName: schema.displayName?.trim() || schema.name?.trim() || chillType,
            viewCode: schema.chillViewCode?.trim() || 'default'
        };
    }
    toEntityOptionsSchemaOption(schema) {
        const chillType = schema.chillType?.trim() ?? '';
        if (!chillType) {
            return null;
        }
        return {
            module: schema.module?.trim() || chillType.split('.')[0] || 'Default',
            chillType,
            displayName: schema.displayName?.trim() || schema.name?.trim() || chillType,
            kind: this.isQuerySchema(schema) ? 'Query' : 'Entity'
        };
    }
    createNode(item) {
        return {
            item,
            children: [],
            childrenLoaded: false,
            isExpanded: false,
            isLoadingChildren: false,
            childrenError: '',
            hasChildren: false
        };
    }
    async buildCrudMenuConfiguration(schema) {
        const viewCode = this.normalizeViewCode(this.viewCode());
        const baseConfiguration = {
            chillType: schema.chillType,
            viewCode
        };
        const queryChillType = schema.queryChillType.trim();
        if (queryChillType) {
            baseConfiguration['chillQuery'] = queryChillType;
        }
        const entitySchema = await firstValueFrom(this.chill.getSchema(schema.chillType, viewCode, undefined, true));
        return applySchemaRelationsToCrudConfiguration(baseConfiguration, entitySchema);
    }
    async expandNodeForHover(node) {
        if (node.isExpanded || node.isLoadingChildren) {
            return;
        }
        if (!node.childrenLoaded) {
            await this.loadNodeChildren(node, true);
            return;
        }
        this.menuRoots.update((roots) => this.updateNodeCollection(roots, node.item.guid, (current) => ({
            ...current,
            isExpanded: true
        })));
    }
    findNodeContext(targetGuid, nodes, parent) {
        for (let index = 0; index < nodes.length; index += 1) {
            const node = nodes[index];
            if (node.item.guid === targetGuid) {
                return { node, parent, siblings: nodes, index };
            }
            const childResult = this.findNodeContext(targetGuid, node.children, node);
            if (childResult) {
                return childResult;
            }
        }
        return null;
    }
    findChildCollection(parentGuid) {
        if (!parentGuid) {
            return this.menuRoots();
        }
        return this.findNodeContext(parentGuid, this.menuRoots(), null)?.node.children ?? null;
    }
    isDescendantOf(node, possibleDescendantGuid) {
        for (const child of node.children) {
            if (child.item.guid === possibleDescendantGuid || this.isDescendantOf(child, possibleDescendantGuid)) {
                return true;
            }
        }
        return false;
    }
    reindexMenuItems(nodes, parent) {
        return nodes.map((node, index) => ({
            ...node.item,
            parent,
            positionNo: index + 1
        }));
    }
    toParentReference(item) {
        return {
            guid: item.guid,
            positionNo: item.positionNo,
            title: item.title,
            description: item.description,
            parent: null,
            componentName: item.componentName,
            componentConfigurationJson: item.componentConfigurationJson,
            menuHierarchy: item.menuHierarchy
        };
    }
    updateNodeCollection(nodes, targetGuid, updater) {
        return nodes.map((node) => {
            if (node.item.guid === targetGuid) {
                return updater(node);
            }
            if (node.children.length === 0) {
                return node;
            }
            const nextChildren = this.updateNodeCollection(node.children, targetGuid, updater);
            return nextChildren === node.children
                ? node
                : {
                    ...node,
                    children: nextChildren
                };
        });
    }
    clearDragHoverExpandTimer(nodeGuid) {
        const timer = this.dragHoverExpandTimers.get(nodeGuid);
        if (!timer) {
            return;
        }
        clearTimeout(timer);
        this.dragHoverExpandTimers.delete(nodeGuid);
        this.dragHoverExpandGuid.update((current) => current === nodeGuid ? '' : current);
    }
    clearAllDragHoverExpandTimers() {
        for (const timer of this.dragHoverExpandTimers.values()) {
            clearTimeout(timer);
        }
        this.dragHoverExpandTimers.clear();
        this.dragHoverExpandGuid.set('');
    }
    async editOrCreateMenuItem(item, parent) {
        const { WorkspaceMenuItemDialogComponent } = await Promise.resolve().then(function () { return workspaceMenuItemDialog_component; });
        const result = await this.dialog.openDialog({
            title: item
                ? this.chill.T('35B8C58D-45AB-47D9-BDC0-6A7D3686981E', 'Edit menu item', 'Modifica voce di menu')
                : this.chill.T('4B47BBA2-8823-4629-BE14-B9B374F8C6F1', 'New menu item', 'Nuova voce di menu'),
            component: WorkspaceMenuItemDialogComponent,
            inputs: { item, parent },
            okLabel: this.chill.T('62953302-B951-4FD1-BD08-4B7649A91BAF', 'Save', 'Salva')
        });
        if (result.status !== 'confirmed' || !result.value?.value) {
            return;
        }
        // Set guid if missing
        if (!result.value?.value?.guid || result.value?.value?.guid === "")
            result.value.value.guid = crypto.randomUUID();
        const savedItem = await firstValueFrom(this.chill.setMenu(result.value.value));
        await this.refreshMenuBranch(savedItem.parent?.guid ?? null);
    }
    async refreshMenuBranch(parentGuid) {
        if (!parentGuid) {
            await this.loadRootMenu();
            return;
        }
        try {
            const children = await firstValueFrom(this.chill.getMenu(parentGuid));
            const childNodes = children.map((item) => this.createNode(item));
            this.menuRoots.update((roots) => this.updateNodeCollection(roots, parentGuid, (current) => ({
                ...current,
                children: childNodes,
                childrenLoaded: true,
                isExpanded: true,
                isLoadingChildren: false,
                hasChildren: childNodes.length > 0,
                childrenError: ''
            })));
            await Promise.all(childNodes.map((child) => this.preloadNodeChildren(child)));
        }
        catch (error) {
            this.menuRoots.update((roots) => this.updateNodeCollection(roots, parentGuid, (current) => ({
                ...current,
                childrenError: this.chill.formatError(error),
                isLoadingChildren: false
            })));
        }
    }
    static { this.ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: WorkspaceMenuComponent, deps: [], target: i0.ɵɵFactoryTarget.Component }); }
    static { this.ɵcmp = i0.ɵɵngDeclareComponent({ minVersion: "17.0.0", version: "19.2.21", type: WorkspaceMenuComponent, isStandalone: true, selector: "app-workspace-menu", ngImport: i0, template: `
    <div class="workspace-menu">
      <div class="workspace-menu__header">
        <p class="eyebrow">Workspace menu</p>
        <h2>Tasks</h2>
        <!-- <p>Task navigation lives here. The menu structure can be expanded later without changing the shell.</p> -->
      </div>

      <section class="workspace-menu__managed-menu">
        <div class="workspace-menu__section-heading workspace-menu__section-heading--row">
          <div>
            <strong>{{ chill.T('F0E48F17-2E1F-43CC-A37F-21A503E7A1BF', 'Application menu', 'Menu applicazione') }}</strong>
            <!-- <span>{{ chill.T('D7D35597-D998-4892-9288-4FC4B48C53A9', 'Root nodes are loaded first; child branches are prepared lazily.', 'I nodi radice sono caricati per primi; i rami figli sono preparati in modo lazy.') }}</span> -->
          </div>
        </div>

        @if (menuLoadError()) {
          <p class="workspace-menu__status error">{{ menuLoadError() }}</p>
        } @else if (isLoadingMenu()) {
          <p class="workspace-menu__status">{{ chill.T('E5B9FD29-47DA-40A6-810C-85BC6241D07A', 'Loading menu...', 'Caricamento menu...') }}</p>
        } @else if (menuRoots().length === 0) {
          <p class="workspace-menu__status">{{ chill.T('96C1B2E5-D6CA-4C53-8353-D97D4F8E0B09', 'No menu items are available for the current user.', "Nessuna voce menu disponibile per l'utente corrente.") }}</p>
        } @else {
          <nav class="workspace-menu__tree" aria-label="Application menu">
            <ng-container
              [ngTemplateOutlet]="treeCollection"
              [ngTemplateOutletContext]="{ nodes: menuRoots(), depth: 0, parent: null }" />
          </nav>
        }

        @if (layout.isLayoutEditingEnabled()) {
          <button
            type="button"
            class="workspace-menu__item workspace-menu__item--compact workspace-menu__root-action"
            (click)="createMenuItem(null)">
            {{ chill.T('9CC0E7F1-D5E2-4A0F-B3BF-E11FB31C26D4', 'Add root item', 'Aggiungi nodo radice') }}
          </button>
        }
      </section>

      @if (layout.isLayoutEditingEnabled()) {
        <section class="workspace-menu__crud-launcher">
          <div class="workspace-menu__section-heading">
            <strong>Open CRUD task</strong>
            <span>Select a module and type, then confirm the view code.</span>
          </div>

          @if (schemaLoadError()) {
            <p class="workspace-menu__status error">{{ schemaLoadError() }}</p>
          } @else if (isLoadingSchemas()) {
            <p class="workspace-menu__status">Loading CRUD types...</p>
          }

          <label class="workspace-menu__field">
            <span>Module</span>
            <select
              [ngModel]="selectedModule()"
              (ngModelChange)="selectModule($event)"
              [disabled]="isLoadingSchemas() || moduleOptions().length === 0">
              @for (module of moduleOptions(); track module) {
                <option [value]="module">{{ module }}</option>
              }
            </select>
          </label>

          <label class="workspace-menu__field">
            <span>Type</span>
            <select
              [ngModel]="selectedChillType()"
              (ngModelChange)="selectedChillType.set($event)"
              [disabled]="isLoadingSchemas() || filteredCrudTypes().length === 0">
              @for (schema of filteredCrudTypes(); track schema.chillType) {
                <option [value]="schema.chillType">{{ schema.displayName }} ({{ schema.chillType }})</option>
              }
            </select>
          </label>

          <label class="workspace-menu__field">
            <span>View code</span>
            <input
              type="text"
              [ngModel]="viewCode()"
              (ngModelChange)="viewCode.set(normalizeViewCode($event))"
              placeholder="default" />
          </label>

          <button
            type="button"
            class="workspace-menu__item workspace-menu__item--launch"
            (click)="openCrudTask()"
            [disabled]="!selectedCrudSchema()">
            <strong>Open CRUD</strong>
            <span>{{ selectedCrudSchema()?.displayName || 'Choose a type to create a CRUD task.' }}</span>
          </button>

          <button
            type="button"
            class="workspace-menu__item workspace-menu__item--launch"
            (click)="addCrudTaskToMenu()"
            [disabled]="!selectedCrudSchema()">
            <strong>Add to menu</strong>
            <span>{{ selectedCrudSchema()?.displayName || 'Choose a type to add it to the application menu.' }}</span>
          </button>
        </section>

        <section class="workspace-menu__crud-launcher">
          <div class="workspace-menu__section-heading">
            <strong>Entity option</strong>
            <span>Select a model and type, including query types, then configure its options.</span>
          </div>

          @if (schemaLoadError()) {
            <p class="workspace-menu__status error">{{ schemaLoadError() }}</p>
          } @else if (isLoadingSchemas()) {
            <p class="workspace-menu__status">Loading types...</p>
          }

          <label class="workspace-menu__field">
            <span>Model</span>
            <select
              [ngModel]="selectedEntityOptionsModule()"
              (ngModelChange)="selectEntityOptionsModule($event)"
              [disabled]="isLoadingSchemas() || entityOptionsModuleOptions().length === 0">
              @for (module of entityOptionsModuleOptions(); track module) {
                <option [value]="module">{{ module }}</option>
              }
            </select>
          </label>

          <label class="workspace-menu__field">
            <span>Type</span>
            <select
              [ngModel]="selectedEntityOptionsChillType()"
              (ngModelChange)="selectedEntityOptionsChillType.set($event)"
              [disabled]="isLoadingSchemas() || filteredEntityOptionsTypes().length === 0">
              @for (schema of filteredEntityOptionsTypes(); track schema.chillType) {
                <option [value]="schema.chillType">{{ schema.displayName }} ({{ schema.kind }}: {{ schema.chillType }})</option>
              }
            </select>
          </label>

          <button
            type="button"
            class="workspace-menu__item workspace-menu__item--launch"
            (click)="openEntityOptionsDialog()"
            [disabled]="!selectedEntityOptionsSchema()">
            <strong>Configure</strong>
            <span>{{ selectedEntityOptionsSchema()?.displayName || 'Choose a type to configure its options.' }}</span>
          </button>
        </section>
      }

      <!-- <nav class="workspace-menu__list">
        @for (task of quickTasks(); track task.componentName) {
          <button
            type="button"
            class="workspace-menu__item"
            [class.active]="workspace.activeTask()?.taskType === task.componentName"
            (click)="openQuickTask(task.componentName)">
            <strong>{{ task.title }}</strong>
            <span>{{ task.description }}</span>
          </button>
        }
      </nav> -->

      <ng-template #treeCollection let-nodes="nodes" let-depth="depth" let-parent="parent">
        @for (node of nodes; track node.item.guid; let index = $index) {
          @if (layout.isLayoutEditingEnabled()) {
            <div
              class="workspace-menu__drop-zone"
              [style.--menu-depth]="depth"
              [class.is-active]="isDropTarget(parent, index)"
              (dragover)="allowMenuDrop($event)"
              (drop)="dropMenuItem(parent, index)">
            </div>
          }

          <ng-container [ngTemplateOutlet]="treeNode" [ngTemplateOutletContext]="{ $implicit: node, depth: depth }" />
        }

        @if (layout.isLayoutEditingEnabled()) {
          <div
            class="workspace-menu__drop-zone"
            [style.--menu-depth]="depth"
            [class.is-active]="isDropTarget(parent, nodes.length)"
            (dragover)="allowMenuDrop($event)"
            (drop)="dropMenuItem(parent, nodes.length)">
          </div>
        }
      </ng-template>

      <ng-template #treeNode let-node let-depth="depth">
        <div class="workspace-menu__tree-node" [style.--menu-depth]="depth">
          <div
            class="workspace-menu__tree-row"
            [class.is-dragging]="draggedMenuItemGuid() === node.item.guid"
            [draggable]="layout.isLayoutEditingEnabled()"
            (dragstart)="beginMenuDrag(node.item)"
            (dragend)="endMenuDrag()">
            <div
              class="workspace-menu__tree-main"
              [class.is-active]="isMenuTaskActive(node.item)"
              [class.is-pending-expand]="dragHoverExpandGuid() === node.item.guid"
              (dragover)="allowMenuDrop($event); hoverMenuItem(node)"
              (drop)="dropMenuItemAsChild(node, $event)"
              (dragleave)="leaveMenuItem(node, $event)">
              <div class="workspace-menu__tree-body">
                <button
                  type="button"
                  class="workspace-menu__tree-trigger"
                  [disabled]="node.item.componentName === null || node.item.componentName === ''"
                  (click)="openMenuItem(node.item)">
                  <span class="workspace-menu__tree-label">
                    <strong>{{ node.item.title }}</strong>
                  </span>
                </button>

                @if (layout.isLayoutEditingEnabled()) {
                  <div class="workspace-menu__tree-actions workspace-menu__tree-actions--inline">
                    <button
                      type="button"
                      class="workspace-menu__tree-action"
                      [attr.aria-label]="chill.T('918FE5BA-CF28-4A7E-BDD8-E9546CC53A67', 'Add child', 'Aggiungi figlio')"
                      [title]="chill.T('918FE5BA-CF28-4A7E-BDD8-E9546CC53A67', 'Add child', 'Aggiungi figlio')"
                      (click)="createMenuItem(node.item)">
                      <span class="material-symbol-icon" aria-hidden="true">add</span>
                    </button>
                    <button
                      type="button"
                      class="workspace-menu__tree-action"
                      [attr.aria-label]="chill.T('6E9A69C0-C4A1-433A-97BC-9E8D1CBD2B53', 'Edit', 'Modifica')"
                      [title]="chill.T('6E9A69C0-C4A1-433A-97BC-9E8D1CBD2B53', 'Edit', 'Modifica')"
                      (click)="editMenuItem(node.item)">
                      <span class="material-symbol-icon" aria-hidden="true">edit</span>
                    </button>
                    <button
                      type="button"
                      class="workspace-menu__tree-action"
                      [attr.aria-label]="chill.T('0D13D4B2-4D2B-4D17-9A89-C30979DA24D5', 'Delete', 'Elimina')"
                      [title]="chill.T('0D13D4B2-4D2B-4D17-9A89-C30979DA24D5', 'Delete', 'Elimina')"
                      (click)="deleteMenuItem(node.item)">
                      <span class="material-symbol-icon" aria-hidden="true">delete</span>
                    </button>
                  </div>
                } @else if (node.isExpanded) {
                  <div class="workspace-menu__tree-meta">
                    @if (node.item.description) {
                      <span>{{ node.item.description }}</span>
                    } @else {
                      <span>{{ node.item.componentName }}</span>
                    }
                  </div>
                }
              </div>

              @if(node.isLoadingChildren || node.hasChildren)
              {
                <button
                  type="button"
                  class="workspace-menu__tree-expander"
                  [disabled]="!node.hasChildren && !node.isLoadingChildren"
                  [class.is-placeholder]="!node.hasChildren && !node.isLoadingChildren"
                  (click)="toggleNode(node)"
                  [attr.aria-label]="node.isExpanded
                    ? chill.T('3E81EBAA-9CF7-4259-BCA8-483D30FC0A93', 'Collapse menu branch', 'Comprimi ramo menu')
                    : chill.T('D2EEB263-B9CA-4C31-910B-BB9C5DC585DF', 'Expand menu branch', 'Espandi ramo menu')">
                  @if (node.isLoadingChildren) {
                    <span class="workspace-menu__tree-expander-icon material-symbol-icon">more_horiz</span>
                  } @else if (node.hasChildren) {
                    <span class="workspace-menu__tree-expander-icon material-symbol-icon">
                      {{ node.isExpanded ? 'expand_less' : 'expand_more' }}
                    </span>
                  } @else {
                    <span class="workspace-menu__tree-expander-icon material-symbol-icon">chevron_right</span>
                  }
                </button>
              }
            </div>
          </div>

          @if (node.childrenError) {
            <p class="workspace-menu__status error workspace-menu__status--nested">{{ node.childrenError }}</p>
          }

          @if (node.isExpanded && node.children.length > 0) {
            <div class="workspace-menu__tree-children">
              <ng-container
                [ngTemplateOutlet]="treeCollection"
                [ngTemplateOutletContext]="{ nodes: node.children, depth: depth + 1, parent: node.item }" />
            </div>
          }
        </div>
      </ng-template>
    </div>
  `, isInline: true, styles: [":host{display:block;height:100%;min-height:0}\n"], dependencies: [{ kind: "ngmodule", type: CommonModule }, { kind: "directive", type: i1$1.NgTemplateOutlet, selector: "[ngTemplateOutlet]", inputs: ["ngTemplateOutletContext", "ngTemplateOutlet", "ngTemplateOutletInjector"] }, { kind: "ngmodule", type: FormsModule }, { kind: "directive", type: i1.NgSelectOption, selector: "option", inputs: ["ngValue", "value"] }, { kind: "directive", type: i1.ɵNgSelectMultipleOption, selector: "option", inputs: ["ngValue", "value"] }, { kind: "directive", type: i1.DefaultValueAccessor, selector: "input:not([type=checkbox])[formControlName],textarea[formControlName],input:not([type=checkbox])[formControl],textarea[formControl],input:not([type=checkbox])[ngModel],textarea[ngModel],[ngDefaultControl]" }, { kind: "directive", type: i1.SelectControlValueAccessor, selector: "select:not([multiple])[formControlName],select:not([multiple])[formControl],select:not([multiple])[ngModel]", inputs: ["compareWith"] }, { kind: "directive", type: i1.NgControlStatus, selector: "[formControlName],[ngModel],[formControl]" }, { kind: "directive", type: i1.NgModel, selector: "[ngModel]:not([formControlName]):not([formControl])", inputs: ["name", "disabled", "ngModel", "ngModelOptions"], outputs: ["ngModelChange"], exportAs: ["ngModel"] }] }); }
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: WorkspaceMenuComponent, decorators: [{
            type: Component,
            args: [{ selector: 'app-workspace-menu', standalone: true, imports: [CommonModule, FormsModule], template: `
    <div class="workspace-menu">
      <div class="workspace-menu__header">
        <p class="eyebrow">Workspace menu</p>
        <h2>Tasks</h2>
        <!-- <p>Task navigation lives here. The menu structure can be expanded later without changing the shell.</p> -->
      </div>

      <section class="workspace-menu__managed-menu">
        <div class="workspace-menu__section-heading workspace-menu__section-heading--row">
          <div>
            <strong>{{ chill.T('F0E48F17-2E1F-43CC-A37F-21A503E7A1BF', 'Application menu', 'Menu applicazione') }}</strong>
            <!-- <span>{{ chill.T('D7D35597-D998-4892-9288-4FC4B48C53A9', 'Root nodes are loaded first; child branches are prepared lazily.', 'I nodi radice sono caricati per primi; i rami figli sono preparati in modo lazy.') }}</span> -->
          </div>
        </div>

        @if (menuLoadError()) {
          <p class="workspace-menu__status error">{{ menuLoadError() }}</p>
        } @else if (isLoadingMenu()) {
          <p class="workspace-menu__status">{{ chill.T('E5B9FD29-47DA-40A6-810C-85BC6241D07A', 'Loading menu...', 'Caricamento menu...') }}</p>
        } @else if (menuRoots().length === 0) {
          <p class="workspace-menu__status">{{ chill.T('96C1B2E5-D6CA-4C53-8353-D97D4F8E0B09', 'No menu items are available for the current user.', "Nessuna voce menu disponibile per l'utente corrente.") }}</p>
        } @else {
          <nav class="workspace-menu__tree" aria-label="Application menu">
            <ng-container
              [ngTemplateOutlet]="treeCollection"
              [ngTemplateOutletContext]="{ nodes: menuRoots(), depth: 0, parent: null }" />
          </nav>
        }

        @if (layout.isLayoutEditingEnabled()) {
          <button
            type="button"
            class="workspace-menu__item workspace-menu__item--compact workspace-menu__root-action"
            (click)="createMenuItem(null)">
            {{ chill.T('9CC0E7F1-D5E2-4A0F-B3BF-E11FB31C26D4', 'Add root item', 'Aggiungi nodo radice') }}
          </button>
        }
      </section>

      @if (layout.isLayoutEditingEnabled()) {
        <section class="workspace-menu__crud-launcher">
          <div class="workspace-menu__section-heading">
            <strong>Open CRUD task</strong>
            <span>Select a module and type, then confirm the view code.</span>
          </div>

          @if (schemaLoadError()) {
            <p class="workspace-menu__status error">{{ schemaLoadError() }}</p>
          } @else if (isLoadingSchemas()) {
            <p class="workspace-menu__status">Loading CRUD types...</p>
          }

          <label class="workspace-menu__field">
            <span>Module</span>
            <select
              [ngModel]="selectedModule()"
              (ngModelChange)="selectModule($event)"
              [disabled]="isLoadingSchemas() || moduleOptions().length === 0">
              @for (module of moduleOptions(); track module) {
                <option [value]="module">{{ module }}</option>
              }
            </select>
          </label>

          <label class="workspace-menu__field">
            <span>Type</span>
            <select
              [ngModel]="selectedChillType()"
              (ngModelChange)="selectedChillType.set($event)"
              [disabled]="isLoadingSchemas() || filteredCrudTypes().length === 0">
              @for (schema of filteredCrudTypes(); track schema.chillType) {
                <option [value]="schema.chillType">{{ schema.displayName }} ({{ schema.chillType }})</option>
              }
            </select>
          </label>

          <label class="workspace-menu__field">
            <span>View code</span>
            <input
              type="text"
              [ngModel]="viewCode()"
              (ngModelChange)="viewCode.set(normalizeViewCode($event))"
              placeholder="default" />
          </label>

          <button
            type="button"
            class="workspace-menu__item workspace-menu__item--launch"
            (click)="openCrudTask()"
            [disabled]="!selectedCrudSchema()">
            <strong>Open CRUD</strong>
            <span>{{ selectedCrudSchema()?.displayName || 'Choose a type to create a CRUD task.' }}</span>
          </button>

          <button
            type="button"
            class="workspace-menu__item workspace-menu__item--launch"
            (click)="addCrudTaskToMenu()"
            [disabled]="!selectedCrudSchema()">
            <strong>Add to menu</strong>
            <span>{{ selectedCrudSchema()?.displayName || 'Choose a type to add it to the application menu.' }}</span>
          </button>
        </section>

        <section class="workspace-menu__crud-launcher">
          <div class="workspace-menu__section-heading">
            <strong>Entity option</strong>
            <span>Select a model and type, including query types, then configure its options.</span>
          </div>

          @if (schemaLoadError()) {
            <p class="workspace-menu__status error">{{ schemaLoadError() }}</p>
          } @else if (isLoadingSchemas()) {
            <p class="workspace-menu__status">Loading types...</p>
          }

          <label class="workspace-menu__field">
            <span>Model</span>
            <select
              [ngModel]="selectedEntityOptionsModule()"
              (ngModelChange)="selectEntityOptionsModule($event)"
              [disabled]="isLoadingSchemas() || entityOptionsModuleOptions().length === 0">
              @for (module of entityOptionsModuleOptions(); track module) {
                <option [value]="module">{{ module }}</option>
              }
            </select>
          </label>

          <label class="workspace-menu__field">
            <span>Type</span>
            <select
              [ngModel]="selectedEntityOptionsChillType()"
              (ngModelChange)="selectedEntityOptionsChillType.set($event)"
              [disabled]="isLoadingSchemas() || filteredEntityOptionsTypes().length === 0">
              @for (schema of filteredEntityOptionsTypes(); track schema.chillType) {
                <option [value]="schema.chillType">{{ schema.displayName }} ({{ schema.kind }}: {{ schema.chillType }})</option>
              }
            </select>
          </label>

          <button
            type="button"
            class="workspace-menu__item workspace-menu__item--launch"
            (click)="openEntityOptionsDialog()"
            [disabled]="!selectedEntityOptionsSchema()">
            <strong>Configure</strong>
            <span>{{ selectedEntityOptionsSchema()?.displayName || 'Choose a type to configure its options.' }}</span>
          </button>
        </section>
      }

      <!-- <nav class="workspace-menu__list">
        @for (task of quickTasks(); track task.componentName) {
          <button
            type="button"
            class="workspace-menu__item"
            [class.active]="workspace.activeTask()?.taskType === task.componentName"
            (click)="openQuickTask(task.componentName)">
            <strong>{{ task.title }}</strong>
            <span>{{ task.description }}</span>
          </button>
        }
      </nav> -->

      <ng-template #treeCollection let-nodes="nodes" let-depth="depth" let-parent="parent">
        @for (node of nodes; track node.item.guid; let index = $index) {
          @if (layout.isLayoutEditingEnabled()) {
            <div
              class="workspace-menu__drop-zone"
              [style.--menu-depth]="depth"
              [class.is-active]="isDropTarget(parent, index)"
              (dragover)="allowMenuDrop($event)"
              (drop)="dropMenuItem(parent, index)">
            </div>
          }

          <ng-container [ngTemplateOutlet]="treeNode" [ngTemplateOutletContext]="{ $implicit: node, depth: depth }" />
        }

        @if (layout.isLayoutEditingEnabled()) {
          <div
            class="workspace-menu__drop-zone"
            [style.--menu-depth]="depth"
            [class.is-active]="isDropTarget(parent, nodes.length)"
            (dragover)="allowMenuDrop($event)"
            (drop)="dropMenuItem(parent, nodes.length)">
          </div>
        }
      </ng-template>

      <ng-template #treeNode let-node let-depth="depth">
        <div class="workspace-menu__tree-node" [style.--menu-depth]="depth">
          <div
            class="workspace-menu__tree-row"
            [class.is-dragging]="draggedMenuItemGuid() === node.item.guid"
            [draggable]="layout.isLayoutEditingEnabled()"
            (dragstart)="beginMenuDrag(node.item)"
            (dragend)="endMenuDrag()">
            <div
              class="workspace-menu__tree-main"
              [class.is-active]="isMenuTaskActive(node.item)"
              [class.is-pending-expand]="dragHoverExpandGuid() === node.item.guid"
              (dragover)="allowMenuDrop($event); hoverMenuItem(node)"
              (drop)="dropMenuItemAsChild(node, $event)"
              (dragleave)="leaveMenuItem(node, $event)">
              <div class="workspace-menu__tree-body">
                <button
                  type="button"
                  class="workspace-menu__tree-trigger"
                  [disabled]="node.item.componentName === null || node.item.componentName === ''"
                  (click)="openMenuItem(node.item)">
                  <span class="workspace-menu__tree-label">
                    <strong>{{ node.item.title }}</strong>
                  </span>
                </button>

                @if (layout.isLayoutEditingEnabled()) {
                  <div class="workspace-menu__tree-actions workspace-menu__tree-actions--inline">
                    <button
                      type="button"
                      class="workspace-menu__tree-action"
                      [attr.aria-label]="chill.T('918FE5BA-CF28-4A7E-BDD8-E9546CC53A67', 'Add child', 'Aggiungi figlio')"
                      [title]="chill.T('918FE5BA-CF28-4A7E-BDD8-E9546CC53A67', 'Add child', 'Aggiungi figlio')"
                      (click)="createMenuItem(node.item)">
                      <span class="material-symbol-icon" aria-hidden="true">add</span>
                    </button>
                    <button
                      type="button"
                      class="workspace-menu__tree-action"
                      [attr.aria-label]="chill.T('6E9A69C0-C4A1-433A-97BC-9E8D1CBD2B53', 'Edit', 'Modifica')"
                      [title]="chill.T('6E9A69C0-C4A1-433A-97BC-9E8D1CBD2B53', 'Edit', 'Modifica')"
                      (click)="editMenuItem(node.item)">
                      <span class="material-symbol-icon" aria-hidden="true">edit</span>
                    </button>
                    <button
                      type="button"
                      class="workspace-menu__tree-action"
                      [attr.aria-label]="chill.T('0D13D4B2-4D2B-4D17-9A89-C30979DA24D5', 'Delete', 'Elimina')"
                      [title]="chill.T('0D13D4B2-4D2B-4D17-9A89-C30979DA24D5', 'Delete', 'Elimina')"
                      (click)="deleteMenuItem(node.item)">
                      <span class="material-symbol-icon" aria-hidden="true">delete</span>
                    </button>
                  </div>
                } @else if (node.isExpanded) {
                  <div class="workspace-menu__tree-meta">
                    @if (node.item.description) {
                      <span>{{ node.item.description }}</span>
                    } @else {
                      <span>{{ node.item.componentName }}</span>
                    }
                  </div>
                }
              </div>

              @if(node.isLoadingChildren || node.hasChildren)
              {
                <button
                  type="button"
                  class="workspace-menu__tree-expander"
                  [disabled]="!node.hasChildren && !node.isLoadingChildren"
                  [class.is-placeholder]="!node.hasChildren && !node.isLoadingChildren"
                  (click)="toggleNode(node)"
                  [attr.aria-label]="node.isExpanded
                    ? chill.T('3E81EBAA-9CF7-4259-BCA8-483D30FC0A93', 'Collapse menu branch', 'Comprimi ramo menu')
                    : chill.T('D2EEB263-B9CA-4C31-910B-BB9C5DC585DF', 'Expand menu branch', 'Espandi ramo menu')">
                  @if (node.isLoadingChildren) {
                    <span class="workspace-menu__tree-expander-icon material-symbol-icon">more_horiz</span>
                  } @else if (node.hasChildren) {
                    <span class="workspace-menu__tree-expander-icon material-symbol-icon">
                      {{ node.isExpanded ? 'expand_less' : 'expand_more' }}
                    </span>
                  } @else {
                    <span class="workspace-menu__tree-expander-icon material-symbol-icon">chevron_right</span>
                  }
                </button>
              }
            </div>
          </div>

          @if (node.childrenError) {
            <p class="workspace-menu__status error workspace-menu__status--nested">{{ node.childrenError }}</p>
          }

          @if (node.isExpanded && node.children.length > 0) {
            <div class="workspace-menu__tree-children">
              <ng-container
                [ngTemplateOutlet]="treeCollection"
                [ngTemplateOutletContext]="{ nodes: node.children, depth: depth + 1, parent: node.item }" />
            </div>
          }
        </div>
      </ng-template>
    </div>
  `, styles: [":host{display:block;height:100%;min-height:0}\n"] }]
        }] });

class WorkspaceTaskbarComponent {
    constructor() {
        this.workspace = inject(WorkspaceService);
    }
    activateTask(taskId) {
        void this.workspace.activateTask(taskId);
    }
    closeTask(taskId) {
        void this.workspace.closeTask(taskId);
    }
    static { this.ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: WorkspaceTaskbarComponent, deps: [], target: i0.ɵɵFactoryTarget.Component }); }
    static { this.ɵcmp = i0.ɵɵngDeclareComponent({ minVersion: "17.0.0", version: "19.2.21", type: WorkspaceTaskbarComponent, isStandalone: true, selector: "app-workspace-taskbar", ngImport: i0, template: `
    <div class="taskbar">
      @if (workspace.openTasks().length === 0) {
        <p class="taskbar__empty">Open tasks will appear here.</p>
      } @else {
        <div class="taskbar__mobile">
          <select
            class="taskbar__mobile-select"
            [ngModel]="workspace.activeTask()?.id ?? ''"
            (ngModelChange)="activateTask($event)">
            @for (task of workspace.openTasks(); track task.id) {
              <option [value]="task.id">{{ task.title }}</option>
            }
          </select>
        </div>

        <div class="taskbar__desktop">
          @for (task of workspace.openTasks(); track task.id) {
            <div class="taskbar__item" [class.active]="workspace.activeTask()?.id === task.id">
              <button type="button" class="taskbar__link" (click)="activateTask(task.id)">
                {{ task.title }}
              </button>
              <button
                type="button"
                class="taskbar__close"
                (click)="closeTask(task.id)"
                [attr.aria-label]="'Close ' + task.title">
                x
              </button>
            </div>
          }
        </div>
      }
    </div>
  `, isInline: true, styles: [".taskbar{min-width:0;width:100%}.taskbar__mobile{display:none;width:100%}.taskbar__mobile-select{width:100%;height:3rem;min-height:3rem;border:1px solid var(--border-color);border-radius:3rem;background:var(--surface-0);color:var(--text-main);padding:.5rem .75rem;font:inherit}@media(max-width:720px){.taskbar__desktop{display:none}.taskbar__mobile{display:block;flex:1 1 auto;min-width:0}}\n"], dependencies: [{ kind: "ngmodule", type: CommonModule }, { kind: "ngmodule", type: FormsModule }, { kind: "directive", type: i1.NgSelectOption, selector: "option", inputs: ["ngValue", "value"] }, { kind: "directive", type: i1.ɵNgSelectMultipleOption, selector: "option", inputs: ["ngValue", "value"] }, { kind: "directive", type: i1.SelectControlValueAccessor, selector: "select:not([multiple])[formControlName],select:not([multiple])[formControl],select:not([multiple])[ngModel]", inputs: ["compareWith"] }, { kind: "directive", type: i1.NgControlStatus, selector: "[formControlName],[ngModel],[formControl]" }, { kind: "directive", type: i1.NgModel, selector: "[ngModel]:not([formControlName]):not([formControl])", inputs: ["name", "disabled", "ngModel", "ngModelOptions"], outputs: ["ngModelChange"], exportAs: ["ngModel"] }] }); }
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: WorkspaceTaskbarComponent, decorators: [{
            type: Component,
            args: [{ selector: 'app-workspace-taskbar', standalone: true, imports: [CommonModule, FormsModule], template: `
    <div class="taskbar">
      @if (workspace.openTasks().length === 0) {
        <p class="taskbar__empty">Open tasks will appear here.</p>
      } @else {
        <div class="taskbar__mobile">
          <select
            class="taskbar__mobile-select"
            [ngModel]="workspace.activeTask()?.id ?? ''"
            (ngModelChange)="activateTask($event)">
            @for (task of workspace.openTasks(); track task.id) {
              <option [value]="task.id">{{ task.title }}</option>
            }
          </select>
        </div>

        <div class="taskbar__desktop">
          @for (task of workspace.openTasks(); track task.id) {
            <div class="taskbar__item" [class.active]="workspace.activeTask()?.id === task.id">
              <button type="button" class="taskbar__link" (click)="activateTask(task.id)">
                {{ task.title }}
              </button>
              <button
                type="button"
                class="taskbar__close"
                (click)="closeTask(task.id)"
                [attr.aria-label]="'Close ' + task.title">
                x
              </button>
            </div>
          }
        </div>
      }
    </div>
  `, styles: [".taskbar{min-width:0;width:100%}.taskbar__mobile{display:none;width:100%}.taskbar__mobile-select{width:100%;height:3rem;min-height:3rem;border:1px solid var(--border-color);border-radius:3rem;background:var(--surface-0);color:var(--text-main);padding:.5rem .75rem;font:inherit}@media(max-width:720px){.taskbar__desktop{display:none}.taskbar__mobile{display:block;flex:1 1 auto;min-width:0}}\n"] }]
        }] });

class WorkspacePageComponent {
    constructor() {
        this.chill = inject(ChillService);
        this.workspace = inject(WorkspaceService);
        this.dialog = inject(WorkspaceDialogService);
        this.toolbar = inject(WorkspaceToolbarService);
        this.route = inject(ActivatedRoute);
        this.router = inject(Router);
        this.themeMenu = viewChild('themeMenu');
        this.userMenu = viewChild('userMenu');
        this.taskOutlets = viewChildren(NgComponentOutlet);
        this.tokenClockHandle = null;
        this.themes = ['bright', 'dark', 'soft', 'cini'];
        this.nowMs = signal(Date.now());
        this.isRenewingToken = signal(false);
        this.activeToolbarButtons = computed(() => this.toolbar.buttons(this.workspace.activeTask()?.toolbarScope ?? 'workspace'));
        this.authTokenCopyLabel = computed(() => {
            this.nowMs();
            const hoursLabel = this.authTokenRemainingHoursLabel();
            const baseText = this.chill.T('59083B57-F07E-4F5F-AF93-1B67F2A717B5', 'Copy auth token', 'Copia token auth');
            return hoursLabel ? `${baseText} (${hoursLabel})` : baseText;
        });
    }
    ngOnInit() {
        this.tokenClockHandle = globalThis.setInterval(() => {
            this.nowMs.set(Date.now());
        }, 60000);
        this.workspace.registerTaskComponentResolver((taskId) => this.resolveTaskComponent(taskId));
        combineLatest([this.route.paramMap, this.route.queryParamMap]).subscribe(([paramMap, queryParamMap]) => {
            void this.workspace.activateTaskFromRoute(paramMap.get('taskId'), queryParamMap);
        });
    }
    ngOnDestroy() {
        if (this.tokenClockHandle) {
            globalThis.clearInterval(this.tokenClockHandle);
            this.tokenClockHandle = null;
        }
    }
    handleWindowKeydown(event) {
        if (event.key !== 'F2' || event.altKey || event.ctrlKey || event.metaKey || event.shiftKey) {
            return;
        }
        event.preventDefault();
        this.workspace.toggleLayoutEditingEnabled();
    }
    handleDocumentClick(event) {
        const target = event.target;
        if (!(target instanceof Node)) {
            return;
        }
        const themeMenu = this.themeMenu()?.nativeElement;
        if (themeMenu?.open && !themeMenu.contains(target)) {
            themeMenu.open = false;
        }
        const userMenu = this.userMenu()?.nativeElement;
        if (userMenu?.open && !userMenu.contains(target)) {
            userMenu.open = false;
        }
    }
    handleBeforeUnload(event) {
        if (this.workspace.canUnloadWorkspace()) {
            return;
        }
        event.preventDefault();
        event.returnValue = '';
    }
    userInitial() {
        const userName = this.chill.userName().trim();
        return userName ? userName[0].toUpperCase() : 'U';
    }
    setTheme(theme) {
        this.workspace.setTheme(theme);
        const themeMenu = this.themeMenu()?.nativeElement;
        if (themeMenu) {
            themeMenu.open = false;
        }
    }
    openPermissionsTask() {
        this.closeUserMenu();
        void this.workspace.openTask('permissions');
    }
    async copyAuthToken() {
        const token = this.chill.session()?.accessToken ?? '';
        if (!token) {
            return;
        }
        await this.writeClipboardText(token);
    }
    async renewAuthToken() {
        if (this.isRenewingToken() || !this.chill.session()?.refreshToken) {
            return;
        }
        this.isRenewingToken.set(true);
        try {
            await firstValueFrom(this.chill.refreshSession());
            this.nowMs.set(Date.now());
        }
        finally {
            this.isRenewingToken.set(false);
        }
    }
    goToChangePassword() {
        this.closeUserMenu();
        this.workspace.closeDrawer();
        void this.navigateAway('/reset-password');
    }
    logout() {
        this.closeUserMenu();
        this.chill.logout();
        void this.logoutAndReset();
    }
    closeUserMenu() {
        const userMenu = this.userMenu()?.nativeElement;
        if (userMenu) {
            userMenu.open = false;
        }
    }
    authTokenRemainingHoursLabel() {
        const expiresUtc = this.chill.session()?.accessTokenExpiresUtc?.trim() ?? '';
        if (!expiresUtc) {
            return '';
        }
        const expiresMs = Date.parse(expiresUtc);
        if (!Number.isFinite(expiresMs)) {
            return '';
        }
        const remainingMs = expiresMs - this.nowMs();
        if (remainingMs <= 0) {
            return this.chill.T('8B5933DD-3500-4EEC-B28E-038CA7C2DF3D', 'expired', 'scaduto');
        }
        const remainingHours = remainingMs / 3_600_000;
        const formattedHours = remainingHours >= 10
            ? Math.floor(remainingHours).toString()
            : Math.max(0.1, Math.floor(remainingHours * 10) / 10).toLocaleString(undefined, {
                maximumFractionDigits: 1
            });
        return this.chill.T('53A9DE67-EDAF-4B43-AC07-EEC3F6F5F98F', `${formattedHours} h left`, `${formattedHours} h rimanenti`);
    }
    async writeClipboardText(text) {
        if (navigator.clipboard?.writeText) {
            await navigator.clipboard.writeText(text);
            return;
        }
        const textarea = document.createElement('textarea');
        textarea.value = text;
        textarea.setAttribute('readonly', '');
        textarea.style.position = 'fixed';
        textarea.style.opacity = '0';
        document.body.appendChild(textarea);
        textarea.select();
        try {
            document.execCommand('copy');
        }
        finally {
            document.body.removeChild(textarea);
        }
    }
    isTaskVisible(taskId) {
        return this.workspace.activeTask()?.id === taskId;
    }
    taskInputs(task) {
        const inputs = { ...(task.inputs ?? {}) };
        if (this.supportsInput(task.component, 'visible')) {
            inputs['visible'] = this.isTaskVisible(task.id);
        }
        if (this.supportsInput(task.component, 'toolbarScope')) {
            inputs['toolbarScope'] = task.toolbarScope;
        }
        return inputs;
    }
    supportsInput(component, inputName) {
        const definition = component.ɵcmp;
        if (!definition?.inputs) {
            return false;
        }
        return Object.prototype.hasOwnProperty.call(definition.inputs, inputName);
    }
    resolveTaskComponent(taskId) {
        const taskIndex = this.workspace.openTasks().findIndex((task) => task.id === taskId);
        if (taskIndex < 0) {
            return null;
        }
        const outlet = this.taskOutlets()[taskIndex];
        const componentInstance = outlet.componentInstance;
        return this.isWorkspaceTaskComponent(componentInstance)
            ? componentInstance
            : null;
    }
    isWorkspaceTaskComponent(value) {
        return !!value && typeof value === 'object';
    }
    async navigateAway(url) {
        const reset = await this.workspace.reset();
        if (!reset) {
            return;
        }
        void this.router.navigateByUrl(url);
    }
    async logoutAndReset() {
        const reset = await this.workspace.reset();
        if (!reset) {
            return;
        }
        this.chill.logout();
        void this.router.navigateByUrl('/login');
    }
    static { this.ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: WorkspacePageComponent, deps: [], target: i0.ɵɵFactoryTarget.Component }); }
    static { this.ɵcmp = i0.ɵɵngDeclareComponent({ minVersion: "17.0.0", version: "19.2.21", type: WorkspacePageComponent, isStandalone: true, selector: "app-workspace-page", host: { listeners: { "window:keydown": "handleWindowKeydown($event)", "document:click": "handleDocumentClick($event)", "window:beforeunload": "handleBeforeUnload($event)" } }, viewQueries: [{ propertyName: "themeMenu", first: true, predicate: ["themeMenu"], descendants: true, isSignal: true }, { propertyName: "userMenu", first: true, predicate: ["userMenu"], descendants: true, isSignal: true }, { propertyName: "taskOutlets", predicate: NgComponentOutlet, descendants: true, isSignal: true }], ngImport: i0, template: `
    <section class="workspace-shell">
      <header class="workspace-topbar">
        <div class="workspace-topbar__left">
          <button
            type="button"
            class="icon-button"
            (click)="workspace.toggleDrawer()"
            [attr.aria-expanded]="workspace.isDrawerOpen()"
            [attr.aria-label]="chill.T('D3C89A1B-4D98-4264-A836-785998F8F09F', 'Open navigation menu', 'Apri menu di navigazione')">
            <span></span>
            <span></span>
            <span></span>
          </button>
        </div>

        <app-workspace-taskbar class="workspace-topbar__center" />

        @if (activeToolbarButtons().length > 0) {
          <div class="workspace-toolbar-actions">
            @for (button of activeToolbarButtons(); track button.id) {
              <button
                type="button"
                class="workspace-toolbar-button"
                [class.workspace-toolbar-button--accent]="button.accent"
                [class.workspace-toolbar-button--has-icon]="!!button.icon"
                (click)="button.action()"
                [disabled]="button.disabled"
                [attr.aria-label]="button.ariaLabel || button.label || button.primaryDefaultText">
                @if (button.icon) {
                  <span
                    class="workspace-toolbar-button__icon"
                    [class.material-symbol-icon]="button.iconClass === 'material-symbol-icon'"
                    aria-hidden="true">{{ button.icon }}</span>
                }
                @if (button.labelGuid && button.primaryDefaultText && button.secondaryDefaultText) {
                  <span class="workspace-toolbar-button__text">
                    <app-chill-i18n-button-label
                      [labelGuid]="button.labelGuid"
                      [primaryDefaultText]="button.primaryDefaultText"
                      [secondaryDefaultText]="button.secondaryDefaultText" />
                  </span>
                } @else {
                  <span class="workspace-toolbar-button__text">{{ button.label }}</span>
                }
              </button>
            }
          </div>
        }

        <div class="workspace-topbar__controls">
          <details class="theme-menu" #themeMenu>
            <summary
              class="theme-menu__summary"
              [attr.aria-label]="chill.T('C698F19E-58EA-41E2-8D31-05137F17C292', 'Theme selection', 'Selezione tema')">
              <span class="theme-menu__swatch" [attr.data-theme]="workspace.theme()"></span>
              <span class="theme-menu__label">{{ workspace.theme() }}</span>
            </summary>

            <div class="theme-menu__panel">
              @for (theme of themes; track theme) {
                <button
                  type="button"
                  class="theme-pill"
                  [class.active]="workspace.theme() === theme"
                  (click)="setTheme(theme)">
                  {{ theme }}
                </button>
              }
            </div>
          </details>

          <details class="user-menu" #userMenu>
            <summary>
              <span class="user-avatar">{{ userInitial() }}</span>
            </summary>

            <div class="user-menu__panel">
              <p class="user-menu__name">{{ chill.userName() || chill.T('B0311DA4-F864-4E15-93A4-894D177F7017', 'current user', 'utente corrente') }}</p>
              <button
                type="button"
                (click)="copyAuthToken()"
                [disabled]="!chill.session()?.accessToken"
                [attr.aria-label]="authTokenCopyLabel()">
                {{ authTokenCopyLabel() }}
              </button>
              <button
                type="button"
                (click)="renewAuthToken()"
                [disabled]="isRenewingToken() || !chill.session()?.refreshToken">
                @if (isRenewingToken()) {
                  {{ chill.T('3606439C-1C2C-45D4-BAC9-2F0C2AB1E783', 'Renewing token...', 'Rinnovo token...') }}
                } @else {
                  {{ chill.T('B9C91C98-E52E-49DA-A3BC-6593F38BB93D', 'Renew token', 'Rinnova token') }}
                }
              </button>
              <button type="button" (click)="openPermissionsTask()">
                <app-chill-i18n-button-label [labelGuid]="'830A6D96-0332-4B08-8EC7-B850702B4337'" [primaryDefaultText]="'Permissions'" [secondaryDefaultText]="'Permessi'" />
              </button>
              <button type="button" (click)="workspace.toggleLayoutEditingEnabled()">
                @if (workspace.isLayoutEditingEnabled()) {
                  <app-chill-i18n-button-label [labelGuid]="'84A896C2-2A1F-4DCE-8B33-A0F586F1DBE8'" [primaryDefaultText]="'Disable layout editing'" [secondaryDefaultText]="'Disabilita modifica layout'" />
                } @else {
                  <app-chill-i18n-button-label [labelGuid]="'A94DDDE0-3CDB-495A-84D7-8226AB21D6C7'" [primaryDefaultText]="'Enable layout editing'" [secondaryDefaultText]="'Abilita modifica layout'" />
                }
              </button>
              <button type="button" (click)="goToChangePassword()">
                <app-chill-i18n-button-label [labelGuid]="'56083997-E7B4-4AE0-B7C6-DB2B82186232'" [primaryDefaultText]="'Change password'" [secondaryDefaultText]="'Cambia password'" />
              </button>
              <button type="button" (click)="logout()">
                <app-chill-i18n-button-label [labelGuid]="'9177351F-738D-447C-8A75-06536CA6E50C'" [primaryDefaultText]="'Logout'" [secondaryDefaultText]="'Disconnetti'" />
              </button>
            </div>
          </details>
        </div>
      </header>

      <div class="workspace-main">
        <aside class="workspace-drawer" [class.open]="workspace.isDrawerOpen()">
          <app-workspace-menu />
        </aside>

        <main class="workspace-content">
          @if (workspace.openTasks().length > 0) {
            <div class="workspace-task-host">
              @for (task of workspace.openTasks(); track task.id) {
                <div class="workspace-task-pane" [hidden]="!isTaskVisible(task.id)">
                  <ng-container *ngComponentOutlet="task.component; inputs: taskInputs(task)" />
                </div>
              }
            </div>
          } @else {
            <section class="workspace-empty-state">
              <p class="eyebrow">
                <app-chill-i18n-label
                  [labelGuid]="'D8E9F1A4-3E8A-47A7-BE9C-1C702F81C6B0'"
                  [primaryDefaultText]="'ChillSharp UI'"
                  [secondaryDefaultText]="'ChillSharp UI'" />
              </p>
              <h2>
                <app-chill-i18n-label
                  [labelGuid]="'6E9CFCD5-61BD-42DA-9666-1570CAEF87D7'"
                  [primaryDefaultText]="'No active task'"
                  [secondaryDefaultText]="'Nessuna attivita'" />
              </h2>
              <p>
                <app-chill-i18n-label
                  [labelGuid]="'3C4B25A7-6208-4C74-8515-47A2E6E8655A'"
                  [primaryDefaultText]="'Open a task from the drawer to start working inside the workspace.'"
                  [secondaryDefaultText]="'Apri una attivita dal drawer per iniziare a lavorare nel workspace.'" />
              </p>
            </section>
          }
        </main>
      </div>

      @if (dialog.activeDialog()) {
        <app-workspace-dialog-host />
      }
    </section>
  `, isInline: true, styles: [".workspace-topbar{display:grid;grid-template-columns:auto minmax(0,1fr) auto auto;align-items:center;gap:.75rem}.workspace-topbar__left{grid-column:1}.workspace-topbar__center{grid-column:2;min-width:0}.workspace-topbar__controls{grid-column:4;display:flex;align-items:center;justify-content:flex-end;gap:.75rem}.workspace-toolbar-actions{grid-column:3;display:flex;align-items:center;justify-content:flex-end;flex-wrap:wrap;gap:.65rem;min-width:0}.workspace-task-host,.workspace-task-pane{display:block;height:100%;min-height:0}.workspace-task-pane[hidden]{display:none!important}.workspace-toolbar-button--accent{border-color:color-mix(in srgb,var(--accent) 45%,var(--border-color));background:linear-gradient(135deg,var(--accent),var(--accent-strong));color:#f8fffd}.workspace-toolbar-button--accent .workspace-toolbar-button__text,.workspace-toolbar-button--accent .workspace-toolbar-button__icon{color:#f8fffd}.workspace-toolbar-button--accent:disabled{border-color:var(--border-color);background:var(--surface-0);color:var(--text-main)}.workspace-toolbar-button__text{display:inline-flex;align-items:center}@media(max-width:720px){.workspace-topbar{grid-template-columns:auto minmax(0,1fr) auto;row-gap:.6rem}.workspace-topbar__left{grid-column:1;grid-row:1}.workspace-topbar__center{grid-column:2;grid-row:1;width:100%}.workspace-topbar__controls{grid-column:3;grid-row:1}.workspace-toolbar-actions{grid-column:1 / -1;grid-row:2;justify-content:flex-start}.workspace-toolbar-button--has-icon{min-width:2.75rem;justify-content:center;padding-inline:.75rem}.workspace-toolbar-button--has-icon .workspace-toolbar-button__text,.theme-menu__label{display:none}}\n"], dependencies: [{ kind: "ngmodule", type: CommonModule }, { kind: "directive", type: i1$1.NgComponentOutlet, selector: "[ngComponentOutlet]", inputs: ["ngComponentOutlet", "ngComponentOutletInputs", "ngComponentOutletInjector", "ngComponentOutletContent", "ngComponentOutletNgModule", "ngComponentOutletNgModuleFactory"], exportAs: ["ngComponentOutlet"] }, { kind: "component", type: WorkspaceTaskbarComponent, selector: "app-workspace-taskbar" }, { kind: "component", type: WorkspaceMenuComponent, selector: "app-workspace-menu" }, { kind: "component", type: WorkspaceDialogHostComponent, selector: "app-workspace-dialog-host" }, { kind: "component", type: ChillI18nLabelComponent, selector: "app-chill-i18n-label", inputs: ["labelGuid", "primaryDefaultText", "secondaryDefaultText", "editable"] }, { kind: "component", type: ChillI18nButtonLabelComponent, selector: "app-chill-i18n-button-label", inputs: ["labelGuid", "primaryDefaultText", "secondaryDefaultText", "editable"] }] }); }
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: WorkspacePageComponent, decorators: [{
            type: Component,
            args: [{ selector: 'app-workspace-page', standalone: true, imports: [CommonModule, NgComponentOutlet, WorkspaceTaskbarComponent, WorkspaceMenuComponent, WorkspaceDialogHostComponent, ChillI18nLabelComponent, ChillI18nButtonLabelComponent], template: `
    <section class="workspace-shell">
      <header class="workspace-topbar">
        <div class="workspace-topbar__left">
          <button
            type="button"
            class="icon-button"
            (click)="workspace.toggleDrawer()"
            [attr.aria-expanded]="workspace.isDrawerOpen()"
            [attr.aria-label]="chill.T('D3C89A1B-4D98-4264-A836-785998F8F09F', 'Open navigation menu', 'Apri menu di navigazione')">
            <span></span>
            <span></span>
            <span></span>
          </button>
        </div>

        <app-workspace-taskbar class="workspace-topbar__center" />

        @if (activeToolbarButtons().length > 0) {
          <div class="workspace-toolbar-actions">
            @for (button of activeToolbarButtons(); track button.id) {
              <button
                type="button"
                class="workspace-toolbar-button"
                [class.workspace-toolbar-button--accent]="button.accent"
                [class.workspace-toolbar-button--has-icon]="!!button.icon"
                (click)="button.action()"
                [disabled]="button.disabled"
                [attr.aria-label]="button.ariaLabel || button.label || button.primaryDefaultText">
                @if (button.icon) {
                  <span
                    class="workspace-toolbar-button__icon"
                    [class.material-symbol-icon]="button.iconClass === 'material-symbol-icon'"
                    aria-hidden="true">{{ button.icon }}</span>
                }
                @if (button.labelGuid && button.primaryDefaultText && button.secondaryDefaultText) {
                  <span class="workspace-toolbar-button__text">
                    <app-chill-i18n-button-label
                      [labelGuid]="button.labelGuid"
                      [primaryDefaultText]="button.primaryDefaultText"
                      [secondaryDefaultText]="button.secondaryDefaultText" />
                  </span>
                } @else {
                  <span class="workspace-toolbar-button__text">{{ button.label }}</span>
                }
              </button>
            }
          </div>
        }

        <div class="workspace-topbar__controls">
          <details class="theme-menu" #themeMenu>
            <summary
              class="theme-menu__summary"
              [attr.aria-label]="chill.T('C698F19E-58EA-41E2-8D31-05137F17C292', 'Theme selection', 'Selezione tema')">
              <span class="theme-menu__swatch" [attr.data-theme]="workspace.theme()"></span>
              <span class="theme-menu__label">{{ workspace.theme() }}</span>
            </summary>

            <div class="theme-menu__panel">
              @for (theme of themes; track theme) {
                <button
                  type="button"
                  class="theme-pill"
                  [class.active]="workspace.theme() === theme"
                  (click)="setTheme(theme)">
                  {{ theme }}
                </button>
              }
            </div>
          </details>

          <details class="user-menu" #userMenu>
            <summary>
              <span class="user-avatar">{{ userInitial() }}</span>
            </summary>

            <div class="user-menu__panel">
              <p class="user-menu__name">{{ chill.userName() || chill.T('B0311DA4-F864-4E15-93A4-894D177F7017', 'current user', 'utente corrente') }}</p>
              <button
                type="button"
                (click)="copyAuthToken()"
                [disabled]="!chill.session()?.accessToken"
                [attr.aria-label]="authTokenCopyLabel()">
                {{ authTokenCopyLabel() }}
              </button>
              <button
                type="button"
                (click)="renewAuthToken()"
                [disabled]="isRenewingToken() || !chill.session()?.refreshToken">
                @if (isRenewingToken()) {
                  {{ chill.T('3606439C-1C2C-45D4-BAC9-2F0C2AB1E783', 'Renewing token...', 'Rinnovo token...') }}
                } @else {
                  {{ chill.T('B9C91C98-E52E-49DA-A3BC-6593F38BB93D', 'Renew token', 'Rinnova token') }}
                }
              </button>
              <button type="button" (click)="openPermissionsTask()">
                <app-chill-i18n-button-label [labelGuid]="'830A6D96-0332-4B08-8EC7-B850702B4337'" [primaryDefaultText]="'Permissions'" [secondaryDefaultText]="'Permessi'" />
              </button>
              <button type="button" (click)="workspace.toggleLayoutEditingEnabled()">
                @if (workspace.isLayoutEditingEnabled()) {
                  <app-chill-i18n-button-label [labelGuid]="'84A896C2-2A1F-4DCE-8B33-A0F586F1DBE8'" [primaryDefaultText]="'Disable layout editing'" [secondaryDefaultText]="'Disabilita modifica layout'" />
                } @else {
                  <app-chill-i18n-button-label [labelGuid]="'A94DDDE0-3CDB-495A-84D7-8226AB21D6C7'" [primaryDefaultText]="'Enable layout editing'" [secondaryDefaultText]="'Abilita modifica layout'" />
                }
              </button>
              <button type="button" (click)="goToChangePassword()">
                <app-chill-i18n-button-label [labelGuid]="'56083997-E7B4-4AE0-B7C6-DB2B82186232'" [primaryDefaultText]="'Change password'" [secondaryDefaultText]="'Cambia password'" />
              </button>
              <button type="button" (click)="logout()">
                <app-chill-i18n-button-label [labelGuid]="'9177351F-738D-447C-8A75-06536CA6E50C'" [primaryDefaultText]="'Logout'" [secondaryDefaultText]="'Disconnetti'" />
              </button>
            </div>
          </details>
        </div>
      </header>

      <div class="workspace-main">
        <aside class="workspace-drawer" [class.open]="workspace.isDrawerOpen()">
          <app-workspace-menu />
        </aside>

        <main class="workspace-content">
          @if (workspace.openTasks().length > 0) {
            <div class="workspace-task-host">
              @for (task of workspace.openTasks(); track task.id) {
                <div class="workspace-task-pane" [hidden]="!isTaskVisible(task.id)">
                  <ng-container *ngComponentOutlet="task.component; inputs: taskInputs(task)" />
                </div>
              }
            </div>
          } @else {
            <section class="workspace-empty-state">
              <p class="eyebrow">
                <app-chill-i18n-label
                  [labelGuid]="'D8E9F1A4-3E8A-47A7-BE9C-1C702F81C6B0'"
                  [primaryDefaultText]="'ChillSharp UI'"
                  [secondaryDefaultText]="'ChillSharp UI'" />
              </p>
              <h2>
                <app-chill-i18n-label
                  [labelGuid]="'6E9CFCD5-61BD-42DA-9666-1570CAEF87D7'"
                  [primaryDefaultText]="'No active task'"
                  [secondaryDefaultText]="'Nessuna attivita'" />
              </h2>
              <p>
                <app-chill-i18n-label
                  [labelGuid]="'3C4B25A7-6208-4C74-8515-47A2E6E8655A'"
                  [primaryDefaultText]="'Open a task from the drawer to start working inside the workspace.'"
                  [secondaryDefaultText]="'Apri una attivita dal drawer per iniziare a lavorare nel workspace.'" />
              </p>
            </section>
          }
        </main>
      </div>

      @if (dialog.activeDialog()) {
        <app-workspace-dialog-host />
      }
    </section>
  `, styles: [".workspace-topbar{display:grid;grid-template-columns:auto minmax(0,1fr) auto auto;align-items:center;gap:.75rem}.workspace-topbar__left{grid-column:1}.workspace-topbar__center{grid-column:2;min-width:0}.workspace-topbar__controls{grid-column:4;display:flex;align-items:center;justify-content:flex-end;gap:.75rem}.workspace-toolbar-actions{grid-column:3;display:flex;align-items:center;justify-content:flex-end;flex-wrap:wrap;gap:.65rem;min-width:0}.workspace-task-host,.workspace-task-pane{display:block;height:100%;min-height:0}.workspace-task-pane[hidden]{display:none!important}.workspace-toolbar-button--accent{border-color:color-mix(in srgb,var(--accent) 45%,var(--border-color));background:linear-gradient(135deg,var(--accent),var(--accent-strong));color:#f8fffd}.workspace-toolbar-button--accent .workspace-toolbar-button__text,.workspace-toolbar-button--accent .workspace-toolbar-button__icon{color:#f8fffd}.workspace-toolbar-button--accent:disabled{border-color:var(--border-color);background:var(--surface-0);color:var(--text-main)}.workspace-toolbar-button__text{display:inline-flex;align-items:center}@media(max-width:720px){.workspace-topbar{grid-template-columns:auto minmax(0,1fr) auto;row-gap:.6rem}.workspace-topbar__left{grid-column:1;grid-row:1}.workspace-topbar__center{grid-column:2;grid-row:1;width:100%}.workspace-topbar__controls{grid-column:3;grid-row:1}.workspace-toolbar-actions{grid-column:1 / -1;grid-row:2;justify-content:flex-start}.workspace-toolbar-button--has-icon{min-width:2.75rem;justify-content:center;padding-inline:.75rem}.workspace-toolbar-button--has-icon .workspace-toolbar-button__text,.theme-menu__label{display:none}}\n"] }]
        }], propDecorators: { handleWindowKeydown: [{
                type: HostListener,
                args: ['window:keydown', ['$event']]
            }], handleDocumentClick: [{
                type: HostListener,
                args: ['document:click', ['$event']]
            }], handleBeforeUnload: [{
                type: HostListener,
                args: ['window:beforeunload', ['$event']]
            }] } });

function passwordMatchValidator$1(control) {
    const password = control.get('newPassword')?.value;
    const confirmPassword = control.get('confirmPassword')?.value;
    return password === confirmPassword ? null : { passwordMismatch: true };
}
class ConfirmResetPageComponent {
    constructor() {
        this.chill = inject(ChillService);
        this.formBuilder = inject(FormBuilder);
        this.route = inject(ActivatedRoute);
        this.router = inject(Router);
        this.isSubmitting = signal(false);
        this.errorMessage = signal('');
        this.successMessage = signal('');
        this.form = this.formBuilder.nonNullable.group({
            userId: [this.route.snapshot.queryParamMap.get('userId') ?? '', Validators.required],
            resetToken: [this.route.snapshot.paramMap.get('token') ?? this.route.snapshot.queryParamMap.get('token') ?? '', Validators.required],
            newPassword: ['', [Validators.required, Validators.minLength(6)]],
            confirmPassword: ['', [Validators.required, Validators.minLength(6)]]
        }, { validators: passwordMatchValidator$1 });
    }
    submit() {
        if (this.form.invalid || this.isSubmitting()) {
            this.form.markAllAsTouched();
            return;
        }
        this.isSubmitting.set(true);
        this.errorMessage.set('');
        this.successMessage.set('');
        const value = this.form.getRawValue();
        this.chill.confirmPasswordReset({
            UserId: value.userId,
            ResetToken: value.resetToken,
            NewPassword: value.newPassword
        }).subscribe({
            next: (response) => {
                this.isSubmitting.set(false);
                if (!response.Succeeded) {
                    this.errorMessage.set(this.chill.T('42917D4F-BE30-4D5C-9428-EC231D485C11', 'Password reset was rejected by the server.', 'La reimpostazione della password è stata rifiutata dal server.'));
                    return;
                }
                this.successMessage.set(this.chill.T('2B08DE64-CF1B-467A-A355-F541B8485D7E', 'Password updated successfully. Redirecting to login...', 'Password aggiornata correttamente. Reindirizzamento al login...'));
                setTimeout(() => {
                    void this.router.navigateByUrl('/login');
                }, 900);
            },
            error: (error) => {
                this.isSubmitting.set(false);
                this.errorMessage.set(this.chill.formatError(error));
            }
        });
    }
    static { this.ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: ConfirmResetPageComponent, deps: [], target: i0.ɵɵFactoryTarget.Component }); }
    static { this.ɵcmp = i0.ɵɵngDeclareComponent({ minVersion: "17.0.0", version: "19.2.21", type: ConfirmResetPageComponent, isStandalone: true, selector: "app-confirm-reset-page", ngImport: i0, template: `
    <section class="auth-page">
      <div class="auth-card wide">
        <p class="eyebrow"><app-chill-i18n-label [labelGuid]="'A651A560-1828-4D67-8D60-8B97011231D7'" [primaryDefaultText]="'ChillSharp Auth'" [secondaryDefaultText]="'Autenticazione ChillSharp'" /></p>
        <h1><app-chill-i18n-label [labelGuid]="'5346E633-5DF8-4A90-8349-7D5622312A84'" [primaryDefaultText]="'Confirm reset'" [secondaryDefaultText]="'Conferma reimpostazione'" /></h1>
        <p class="lede"><app-chill-i18n-label [labelGuid]="'A97AAEAA-92FA-4F47-B7DD-D84824ECE053'" [primaryDefaultText]="'Submit the UserId, ResetToken, and new password to complete the ChillSharp reset flow.'" [secondaryDefaultText]="'Invia UserId, ResetToken e la nuova password per completare il flusso di reset di ChillSharp.'" /></p>

        @if (successMessage()) {
          <div class="notice success">{{ successMessage() }}</div>
        }

        @if (errorMessage()) {
          <div class="notice error">{{ errorMessage() }}</div>
        }

        <form [formGroup]="form" (ngSubmit)="submit()" class="auth-form two-columns">
          <label class="full-width">
            <span><app-chill-i18n-label [labelGuid]="'CEB1B59C-FD4E-46D4-B11C-F8B33EA4C32E'" [primaryDefaultText]="'User ID'" [secondaryDefaultText]="'ID utente'" /></span>
            <input type="text" formControlName="userId" />
          </label>

          <label class="full-width">
            <span><app-chill-i18n-label [labelGuid]="'3D525E29-B464-49FF-AC72-6D7E05D0F226'" [primaryDefaultText]="'Reset token'" [secondaryDefaultText]="'Token di reset'" /></span>
            <textarea rows="5" formControlName="resetToken"></textarea>
          </label>

          <label>
            <span><app-chill-i18n-label [labelGuid]="'7B9AA727-6032-47E3-917E-EBA35C9C264F'" [primaryDefaultText]="'New password'" [secondaryDefaultText]="'Nuova password'" /></span>
            <input type="password" formControlName="newPassword" autocomplete="new-password" />
          </label>

          <label>
            <span><app-chill-i18n-label [labelGuid]="'14EB51FA-9D9B-427C-AFD6-CC54031B9B26'" [primaryDefaultText]="'Confirm password'" [secondaryDefaultText]="'Conferma password'" /></span>
            <input type="password" formControlName="confirmPassword" autocomplete="new-password" />
          </label>

          @if (form.hasError('passwordMismatch') && form.touched) {
            <div class="notice error full-width">{{ chill.T('12632967-9A9B-4A16-B7DF-D64B7BA7914A', 'Password confirmation does not match.', 'La conferma della password non corrisponde.') }}</div>
          }

          <button type="submit" class="full-width" [disabled]="isSubmitting() || form.invalid">
            @if (isSubmitting()) {
              <app-chill-i18n-button-label [labelGuid]="'34936727-D552-4649-A178-3377D418D5B6'" [primaryDefaultText]="'Applying reset...'" [secondaryDefaultText]="'Applicazione reset in corso...'" />
            } @else {
              <app-chill-i18n-button-label [labelGuid]="'5346E633-5DF8-4A90-8349-7D5622312A84'" [primaryDefaultText]="'Confirm reset'" [secondaryDefaultText]="'Conferma reimpostazione'" />
            }
          </button>
        </form>

        <nav class="auth-links">
          <a routerLink="/reset-password">{{ chill.T('D5B5D31E-1F31-4080-A628-0674B00ECF07', 'Request token again', 'Richiedi di nuovo il token') }}</a>
          <a routerLink="/login">{{ chill.T('1919121D-A2BC-403E-8FCA-E120630A1FAC', 'Back to login', 'Torna al login') }}</a>
        </nav>
      </div>
    </section>
  `, isInline: true, dependencies: [{ kind: "ngmodule", type: CommonModule }, { kind: "ngmodule", type: ReactiveFormsModule }, { kind: "directive", type: i1.ɵNgNoValidate, selector: "form:not([ngNoForm]):not([ngNativeValidate])" }, { kind: "directive", type: i1.DefaultValueAccessor, selector: "input:not([type=checkbox])[formControlName],textarea[formControlName],input:not([type=checkbox])[formControl],textarea[formControl],input:not([type=checkbox])[ngModel],textarea[ngModel],[ngDefaultControl]" }, { kind: "directive", type: i1.NgControlStatus, selector: "[formControlName],[ngModel],[formControl]" }, { kind: "directive", type: i1.NgControlStatusGroup, selector: "[formGroupName],[formArrayName],[ngModelGroup],[formGroup],form:not([ngNoForm]),[ngForm]" }, { kind: "directive", type: i1.FormGroupDirective, selector: "[formGroup]", inputs: ["formGroup"], outputs: ["ngSubmit"], exportAs: ["ngForm"] }, { kind: "directive", type: i1.FormControlName, selector: "[formControlName]", inputs: ["formControlName", "disabled", "ngModel"], outputs: ["ngModelChange"] }, { kind: "directive", type: RouterLink, selector: "[routerLink]", inputs: ["target", "queryParams", "fragment", "queryParamsHandling", "state", "info", "relativeTo", "preserveFragment", "skipLocationChange", "replaceUrl", "routerLink"] }, { kind: "component", type: ChillI18nLabelComponent, selector: "app-chill-i18n-label", inputs: ["labelGuid", "primaryDefaultText", "secondaryDefaultText", "editable"] }, { kind: "component", type: ChillI18nButtonLabelComponent, selector: "app-chill-i18n-button-label", inputs: ["labelGuid", "primaryDefaultText", "secondaryDefaultText", "editable"] }, { kind: "directive", type: NoticeTransitionDirective, selector: ".notice" }] }); }
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: ConfirmResetPageComponent, decorators: [{
            type: Component,
            args: [{
                    selector: 'app-confirm-reset-page',
                    standalone: true,
                    imports: [CommonModule, ReactiveFormsModule, RouterLink, ChillI18nLabelComponent, ChillI18nButtonLabelComponent, NoticeTransitionDirective],
                    template: `
    <section class="auth-page">
      <div class="auth-card wide">
        <p class="eyebrow"><app-chill-i18n-label [labelGuid]="'A651A560-1828-4D67-8D60-8B97011231D7'" [primaryDefaultText]="'ChillSharp Auth'" [secondaryDefaultText]="'Autenticazione ChillSharp'" /></p>
        <h1><app-chill-i18n-label [labelGuid]="'5346E633-5DF8-4A90-8349-7D5622312A84'" [primaryDefaultText]="'Confirm reset'" [secondaryDefaultText]="'Conferma reimpostazione'" /></h1>
        <p class="lede"><app-chill-i18n-label [labelGuid]="'A97AAEAA-92FA-4F47-B7DD-D84824ECE053'" [primaryDefaultText]="'Submit the UserId, ResetToken, and new password to complete the ChillSharp reset flow.'" [secondaryDefaultText]="'Invia UserId, ResetToken e la nuova password per completare il flusso di reset di ChillSharp.'" /></p>

        @if (successMessage()) {
          <div class="notice success">{{ successMessage() }}</div>
        }

        @if (errorMessage()) {
          <div class="notice error">{{ errorMessage() }}</div>
        }

        <form [formGroup]="form" (ngSubmit)="submit()" class="auth-form two-columns">
          <label class="full-width">
            <span><app-chill-i18n-label [labelGuid]="'CEB1B59C-FD4E-46D4-B11C-F8B33EA4C32E'" [primaryDefaultText]="'User ID'" [secondaryDefaultText]="'ID utente'" /></span>
            <input type="text" formControlName="userId" />
          </label>

          <label class="full-width">
            <span><app-chill-i18n-label [labelGuid]="'3D525E29-B464-49FF-AC72-6D7E05D0F226'" [primaryDefaultText]="'Reset token'" [secondaryDefaultText]="'Token di reset'" /></span>
            <textarea rows="5" formControlName="resetToken"></textarea>
          </label>

          <label>
            <span><app-chill-i18n-label [labelGuid]="'7B9AA727-6032-47E3-917E-EBA35C9C264F'" [primaryDefaultText]="'New password'" [secondaryDefaultText]="'Nuova password'" /></span>
            <input type="password" formControlName="newPassword" autocomplete="new-password" />
          </label>

          <label>
            <span><app-chill-i18n-label [labelGuid]="'14EB51FA-9D9B-427C-AFD6-CC54031B9B26'" [primaryDefaultText]="'Confirm password'" [secondaryDefaultText]="'Conferma password'" /></span>
            <input type="password" formControlName="confirmPassword" autocomplete="new-password" />
          </label>

          @if (form.hasError('passwordMismatch') && form.touched) {
            <div class="notice error full-width">{{ chill.T('12632967-9A9B-4A16-B7DF-D64B7BA7914A', 'Password confirmation does not match.', 'La conferma della password non corrisponde.') }}</div>
          }

          <button type="submit" class="full-width" [disabled]="isSubmitting() || form.invalid">
            @if (isSubmitting()) {
              <app-chill-i18n-button-label [labelGuid]="'34936727-D552-4649-A178-3377D418D5B6'" [primaryDefaultText]="'Applying reset...'" [secondaryDefaultText]="'Applicazione reset in corso...'" />
            } @else {
              <app-chill-i18n-button-label [labelGuid]="'5346E633-5DF8-4A90-8349-7D5622312A84'" [primaryDefaultText]="'Confirm reset'" [secondaryDefaultText]="'Conferma reimpostazione'" />
            }
          </button>
        </form>

        <nav class="auth-links">
          <a routerLink="/reset-password">{{ chill.T('D5B5D31E-1F31-4080-A628-0674B00ECF07', 'Request token again', 'Richiedi di nuovo il token') }}</a>
          <a routerLink="/login">{{ chill.T('1919121D-A2BC-403E-8FCA-E120630A1FAC', 'Back to login', 'Torna al login') }}</a>
        </nav>
      </div>
    </section>
  `
                }]
        }] });

class LoginPageComponent {
    constructor() {
        this.chill = inject(ChillService);
        this.formBuilder = inject(FormBuilder);
        this.router = inject(Router);
        this.isSubmitting = signal(false);
        this.errorMessage = signal('');
        this.serviceStatusMessage = signal(this.chill.T('A4AE0C28-6837-4586-B6D4-26FA90E7C458', 'Checking Chill service...', 'Verifica del servizio Chill in corso...'));
        this.serviceStatusKind = signal('info');
        this.form = this.formBuilder.nonNullable.group({
            userNameOrEmail: ['', Validators.required],
            password: ['', Validators.required]
        });
    }
    ngOnInit() {
        this.chill.test().subscribe({
            next: (response) => {
                this.serviceStatusKind.set('success');
                this.serviceStatusMessage.set(response || this.chill.T('C99B0A5B-433D-48DD-96D2-69D730A3EBCC', 'Chill service is available.', 'Il servizio Chill è disponibile.'));
            },
            error: (error) => {
                this.serviceStatusKind.set('error');
                this.serviceStatusMessage.set(`${this.chill.T('E271909F-89FE-4A7F-8763-A730FE770145', 'Chill service unavailable:', 'Servizio Chill non disponibile:')} ${this.chill.formatError(error)}`);
            }
        });
    }
    submit() {
        if (this.form.invalid || this.isSubmitting()) {
            this.form.markAllAsTouched();
            return;
        }
        this.isSubmitting.set(true);
        this.errorMessage.set('');
        this.chill.login({
            UserNameOrEmail: this.form.getRawValue().userNameOrEmail,
            Password: this.form.getRawValue().password
        }).subscribe({
            next: () => {
                this.isSubmitting.set(false);
                if (!this.chill.isAuthenticated()) {
                    this.errorMessage.set(this.chill.T('8995E23D-2E60-4D55-9E68-92BA746B9E16', 'Login returned successfully, but no access token was persisted.', 'Il login e riuscito, ma nessun token di accesso e stato salvato.'));
                    return;
                }
                void this.router.navigate(['/workspace']);
            },
            error: (error) => {
                this.isSubmitting.set(false);
                this.errorMessage.set(this.chill.formatError(error));
            }
        });
    }
    static { this.ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: LoginPageComponent, deps: [], target: i0.ɵɵFactoryTarget.Component }); }
    static { this.ɵcmp = i0.ɵɵngDeclareComponent({ minVersion: "17.0.0", version: "19.2.21", type: LoginPageComponent, isStandalone: true, selector: "app-login-page", ngImport: i0, template: `
    <section class="auth-page">
      <div class="auth-card">
        <p class="eyebrow"><app-chill-i18n-label [labelGuid]="'A651A560-1828-4D67-8D60-8B97011231D7'" [primaryDefaultText]="'ChillSharp Auth'" [secondaryDefaultText]="'Autenticazione ChillSharp'" /></p>
        <h1><app-chill-i18n-label [labelGuid]="'0C4F53D0-2087-486B-9F2A-AEBCC226AF09'" [primaryDefaultText]="'Login'" [secondaryDefaultText]="'Accesso'" /></h1>
        <p class="lede"><app-chill-i18n-label [labelGuid]="'D22E0294-1500-4C13-9008-6980F84F2758'" [primaryDefaultText]="'Authenticate against the ChillSharp Identity endpoints through a single Angular service.'" [secondaryDefaultText]="'Autenticati agli endpoint Identity di ChillSharp tramite un singolo servizio Angular.'" /></p>

        @if (chill.isAuthenticated()) {
          <div class="notice success">
            {{ chill.T('F1F3C98A-655B-438A-BF7F-F730BF947EB0', 'Active session for', 'Sessione attiva per') }} <strong>{{ chill.userName() || chill.T('B0311DA4-F864-4E15-93A4-894D177F7017', 'current user', 'utente corrente') }}</strong>.
          </div>
        }

        <div class="notice" [class.success]="serviceStatusKind() === 'success'" [class.error]="serviceStatusKind() === 'error'">
          {{ serviceStatusMessage() }}
        </div>

        @if (errorMessage()) {
          <div class="notice error">{{ errorMessage() }}</div>
        }

        <form [formGroup]="form" (ngSubmit)="submit()" class="auth-form">
          <label>
            <span><app-chill-i18n-label [labelGuid]="'EA8C79F7-B95E-40A8-B638-C09BFD355A94'" [primaryDefaultText]="'Username or email'" [secondaryDefaultText]="'Nome utente o email'" /></span>
            <input type="text" formControlName="userNameOrEmail" autocomplete="username" />
          </label>

          <label>
            <span><app-chill-i18n-label [labelGuid]="'A76807CB-91F6-41A5-B565-D86EEA811241'" [primaryDefaultText]="'Password'" [secondaryDefaultText]="'Password'" /></span>
            <input type="password" formControlName="password" autocomplete="current-password" />
          </label>

          <button type="submit" [disabled]="isSubmitting() || form.invalid">
            @if (isSubmitting()) {
              <app-chill-i18n-button-label [labelGuid]="'8B825C06-0160-4B9F-B697-C624456C87CA'" [primaryDefaultText]="'Signing in...'" [secondaryDefaultText]="'Accesso in corso...'" />
            } @else {
              <app-chill-i18n-button-label [labelGuid]="'CA6A46A2-9E63-4AA8-849F-63EEB430A227'" [primaryDefaultText]="'Sign in'" [secondaryDefaultText]="'Accedi'" />
            }
          </button>
        </form>

        <nav class="auth-links">
          <a routerLink="/register">{{ chill.T('61E5DBBB-413A-449B-BE0E-B4A991FA1E39', 'Create account', 'Crea account') }}</a>
          <a routerLink="/reset-password">{{ chill.T('E61B1755-5FB6-40ED-A8D2-B9158BF410D8', 'Forgot password', 'Password dimenticata') }}</a>
        </nav>
      </div>
    </section>
  `, isInline: true, dependencies: [{ kind: "ngmodule", type: CommonModule }, { kind: "ngmodule", type: ReactiveFormsModule }, { kind: "directive", type: i1.ɵNgNoValidate, selector: "form:not([ngNoForm]):not([ngNativeValidate])" }, { kind: "directive", type: i1.DefaultValueAccessor, selector: "input:not([type=checkbox])[formControlName],textarea[formControlName],input:not([type=checkbox])[formControl],textarea[formControl],input:not([type=checkbox])[ngModel],textarea[ngModel],[ngDefaultControl]" }, { kind: "directive", type: i1.NgControlStatus, selector: "[formControlName],[ngModel],[formControl]" }, { kind: "directive", type: i1.NgControlStatusGroup, selector: "[formGroupName],[formArrayName],[ngModelGroup],[formGroup],form:not([ngNoForm]),[ngForm]" }, { kind: "directive", type: i1.FormGroupDirective, selector: "[formGroup]", inputs: ["formGroup"], outputs: ["ngSubmit"], exportAs: ["ngForm"] }, { kind: "directive", type: i1.FormControlName, selector: "[formControlName]", inputs: ["formControlName", "disabled", "ngModel"], outputs: ["ngModelChange"] }, { kind: "directive", type: RouterLink, selector: "[routerLink]", inputs: ["target", "queryParams", "fragment", "queryParamsHandling", "state", "info", "relativeTo", "preserveFragment", "skipLocationChange", "replaceUrl", "routerLink"] }, { kind: "component", type: ChillI18nLabelComponent, selector: "app-chill-i18n-label", inputs: ["labelGuid", "primaryDefaultText", "secondaryDefaultText", "editable"] }, { kind: "component", type: ChillI18nButtonLabelComponent, selector: "app-chill-i18n-button-label", inputs: ["labelGuid", "primaryDefaultText", "secondaryDefaultText", "editable"] }, { kind: "directive", type: NoticeTransitionDirective, selector: ".notice" }] }); }
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: LoginPageComponent, decorators: [{
            type: Component,
            args: [{
                    selector: 'app-login-page',
                    standalone: true,
                    imports: [CommonModule, ReactiveFormsModule, RouterLink, ChillI18nLabelComponent, ChillI18nButtonLabelComponent, NoticeTransitionDirective],
                    template: `
    <section class="auth-page">
      <div class="auth-card">
        <p class="eyebrow"><app-chill-i18n-label [labelGuid]="'A651A560-1828-4D67-8D60-8B97011231D7'" [primaryDefaultText]="'ChillSharp Auth'" [secondaryDefaultText]="'Autenticazione ChillSharp'" /></p>
        <h1><app-chill-i18n-label [labelGuid]="'0C4F53D0-2087-486B-9F2A-AEBCC226AF09'" [primaryDefaultText]="'Login'" [secondaryDefaultText]="'Accesso'" /></h1>
        <p class="lede"><app-chill-i18n-label [labelGuid]="'D22E0294-1500-4C13-9008-6980F84F2758'" [primaryDefaultText]="'Authenticate against the ChillSharp Identity endpoints through a single Angular service.'" [secondaryDefaultText]="'Autenticati agli endpoint Identity di ChillSharp tramite un singolo servizio Angular.'" /></p>

        @if (chill.isAuthenticated()) {
          <div class="notice success">
            {{ chill.T('F1F3C98A-655B-438A-BF7F-F730BF947EB0', 'Active session for', 'Sessione attiva per') }} <strong>{{ chill.userName() || chill.T('B0311DA4-F864-4E15-93A4-894D177F7017', 'current user', 'utente corrente') }}</strong>.
          </div>
        }

        <div class="notice" [class.success]="serviceStatusKind() === 'success'" [class.error]="serviceStatusKind() === 'error'">
          {{ serviceStatusMessage() }}
        </div>

        @if (errorMessage()) {
          <div class="notice error">{{ errorMessage() }}</div>
        }

        <form [formGroup]="form" (ngSubmit)="submit()" class="auth-form">
          <label>
            <span><app-chill-i18n-label [labelGuid]="'EA8C79F7-B95E-40A8-B638-C09BFD355A94'" [primaryDefaultText]="'Username or email'" [secondaryDefaultText]="'Nome utente o email'" /></span>
            <input type="text" formControlName="userNameOrEmail" autocomplete="username" />
          </label>

          <label>
            <span><app-chill-i18n-label [labelGuid]="'A76807CB-91F6-41A5-B565-D86EEA811241'" [primaryDefaultText]="'Password'" [secondaryDefaultText]="'Password'" /></span>
            <input type="password" formControlName="password" autocomplete="current-password" />
          </label>

          <button type="submit" [disabled]="isSubmitting() || form.invalid">
            @if (isSubmitting()) {
              <app-chill-i18n-button-label [labelGuid]="'8B825C06-0160-4B9F-B697-C624456C87CA'" [primaryDefaultText]="'Signing in...'" [secondaryDefaultText]="'Accesso in corso...'" />
            } @else {
              <app-chill-i18n-button-label [labelGuid]="'CA6A46A2-9E63-4AA8-849F-63EEB430A227'" [primaryDefaultText]="'Sign in'" [secondaryDefaultText]="'Accedi'" />
            }
          </button>
        </form>

        <nav class="auth-links">
          <a routerLink="/register">{{ chill.T('61E5DBBB-413A-449B-BE0E-B4A991FA1E39', 'Create account', 'Crea account') }}</a>
          <a routerLink="/reset-password">{{ chill.T('E61B1755-5FB6-40ED-A8D2-B9158BF410D8', 'Forgot password', 'Password dimenticata') }}</a>
        </nav>
      </div>
    </section>
  `
                }]
        }] });

function passwordMatchValidator(control) {
    const password = control.get('password')?.value;
    const confirmPassword = control.get('confirmPassword')?.value;
    return password === confirmPassword ? null : { passwordMismatch: true };
}
class RegisterPageComponent {
    constructor() {
        this.chill = inject(ChillService);
        this.formBuilder = inject(FormBuilder);
        this.router = inject(Router);
        this.isSubmitting = signal(false);
        this.errorMessage = signal('');
        this.successMessage = signal('');
        this.form = this.formBuilder.nonNullable.group({
            userName: ['', Validators.required],
            email: ['', [Validators.required, Validators.email]],
            displayName: [''],
            password: ['', [Validators.required, Validators.minLength(6)]],
            confirmPassword: ['', [Validators.required, Validators.minLength(6)]],
            createChillAuthUser: [true]
        }, { validators: passwordMatchValidator });
    }
    submit() {
        if (this.form.invalid || this.isSubmitting()) {
            this.form.markAllAsTouched();
            return;
        }
        this.isSubmitting.set(true);
        this.errorMessage.set('');
        this.successMessage.set('');
        const value = this.form.getRawValue();
        this.chill.register({
            UserName: value.userName,
            Email: value.email,
            Password: value.password,
            DisplayName: value.displayName,
            DisplayCultureName: this.readBrowserCultureName(),
            DisplayTimeZone: this.readBrowserTimeZone(),
            CreateChillAuthUser: value.createChillAuthUser
        }).subscribe({
            next: () => {
                this.isSubmitting.set(false);
                this.successMessage.set(this.chill.T('C91817F6-2CA4-461E-9D2B-EAD56F4B79BF', 'Account created and authenticated successfully.', 'Account creato e autenticato correttamente.'));
                setTimeout(() => {
                    void this.router.navigate(['/workspace']);
                }, 700);
            },
            error: (error) => {
                this.isSubmitting.set(false);
                this.errorMessage.set(this.chill.formatError(error));
            }
        });
    }
    readBrowserCultureName() {
        const languages = globalThis.navigator?.languages;
        const browserCultureName = languages?.find((language) => typeof language === 'string' && language.trim())
            ?? globalThis.navigator?.language
            ?? '';
        return browserCultureName.trim() || CHILL_CULTURE;
    }
    readBrowserTimeZone() {
        return Intl.DateTimeFormat().resolvedOptions().timeZone || 'UTC';
    }
    static { this.ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: RegisterPageComponent, deps: [], target: i0.ɵɵFactoryTarget.Component }); }
    static { this.ɵcmp = i0.ɵɵngDeclareComponent({ minVersion: "17.0.0", version: "19.2.21", type: RegisterPageComponent, isStandalone: true, selector: "app-register-page", ngImport: i0, template: `
    <section class="auth-page">
      <div class="auth-card wide">
        <p class="eyebrow"><app-chill-i18n-label [labelGuid]="'A651A560-1828-4D67-8D60-8B97011231D7'" [primaryDefaultText]="'ChillSharp Auth'" [secondaryDefaultText]="'Autenticazione ChillSharp'" /></p>
        <h1><app-chill-i18n-label [labelGuid]="'0A777B5C-F7D1-4084-B32F-D5162E100AF6'" [primaryDefaultText]="'Register'" [secondaryDefaultText]="'Registrazione'" /></h1>
        <p class="lede"><app-chill-i18n-label [labelGuid]="'E4AB90A6-DB4D-4D34-BA80-8D07F6F5595D'" [primaryDefaultText]="'Create an ASP.NET Core Identity account and optionally the linked ChillSharp auth user.'" [secondaryDefaultText]="&quot;Crea un account ASP.NET Core Identity e, facoltativamente, l'utente auth collegato di ChillSharp.&quot;" /></p>

        @if (successMessage()) {
          <div class="notice success">{{ successMessage() }}</div>
        }

        @if (errorMessage()) {
          <div class="notice error">{{ errorMessage() }}</div>
        }

        <form [formGroup]="form" (ngSubmit)="submit()" class="auth-form two-columns">
          <label>
            <span><app-chill-i18n-label [labelGuid]="'2AF5EB08-932E-4D4D-9338-75E1808B5F16'" [primaryDefaultText]="'Username'" [secondaryDefaultText]="'Nome utente'" /></span>
            <input type="text" formControlName="userName" autocomplete="username" />
          </label>

          <label>
            <span><app-chill-i18n-label [labelGuid]="'311C8595-76C7-41DF-B4A0-0D0EF8E9A3D7'" [primaryDefaultText]="'Email'" [secondaryDefaultText]="'Email'" /></span>
            <input type="email" formControlName="email" autocomplete="email" />
          </label>

          <label class="full-width">
            <span><app-chill-i18n-label [labelGuid]="'C0D8A063-E084-460D-BF83-BCE32CB68588'" [primaryDefaultText]="'Display name'" [secondaryDefaultText]="'Nome visualizzato'" /></span>
            <input type="text" formControlName="displayName" autocomplete="name" />
          </label>

          <label>
            <span><app-chill-i18n-label [labelGuid]="'A76807CB-91F6-41A5-B565-D86EEA811241'" [primaryDefaultText]="'Password'" [secondaryDefaultText]="'Password'" /></span>
            <input type="password" formControlName="password" autocomplete="new-password" />
          </label>

          <label>
            <span><app-chill-i18n-label [labelGuid]="'14EB51FA-9D9B-427C-AFD6-CC54031B9B26'" [primaryDefaultText]="'Confirm password'" [secondaryDefaultText]="'Conferma password'" /></span>
            <input type="password" formControlName="confirmPassword" autocomplete="new-password" />
          </label>

          <label class="checkbox full-width">
            <input type="checkbox" formControlName="createChillAuthUser" />
            <span><app-chill-i18n-label [labelGuid]="'E69C6D4F-07EC-42BA-B30D-AFA77B84E595'" [primaryDefaultText]="'Create linked Chill auth user'" [secondaryDefaultText]="'Crea utente auth Chill collegato'" /></span>
          </label>

          @if (form.hasError('passwordMismatch') && form.touched) {
            <div class="notice error full-width">{{ chill.T('12632967-9A9B-4A16-B7DF-D64B7BA7914A', 'Password confirmation does not match.', 'La conferma della password non corrisponde.') }}</div>
          }

          <button type="submit" class="full-width" [disabled]="isSubmitting() || form.invalid">
            @if (isSubmitting()) {
              <app-chill-i18n-button-label [labelGuid]="'A39321BE-5534-40B7-B1A7-F32BF872C997'" [primaryDefaultText]="'Creating account...'" [secondaryDefaultText]="'Creazione account in corso...'" />
            } @else {
              <app-chill-i18n-button-label [labelGuid]="'61E5DBBB-413A-449B-BE0E-B4A991FA1E39'" [primaryDefaultText]="'Create account'" [secondaryDefaultText]="'Crea account'" />
            }
          </button>
        </form>

        <nav class="auth-links">
          <a routerLink="/login">{{ chill.T('1919121D-A2BC-403E-8FCA-E120630A1FAC', 'Back to login', 'Torna al login') }}</a>
          <a routerLink="/reset-password">{{ chill.T('1322FAE4-DBD5-4C8D-8988-FA6035551E02', 'Reset password', 'Reimposta password') }}</a>
        </nav>
      </div>
    </section>
  `, isInline: true, dependencies: [{ kind: "ngmodule", type: CommonModule }, { kind: "ngmodule", type: ReactiveFormsModule }, { kind: "directive", type: i1.ɵNgNoValidate, selector: "form:not([ngNoForm]):not([ngNativeValidate])" }, { kind: "directive", type: i1.DefaultValueAccessor, selector: "input:not([type=checkbox])[formControlName],textarea[formControlName],input:not([type=checkbox])[formControl],textarea[formControl],input:not([type=checkbox])[ngModel],textarea[ngModel],[ngDefaultControl]" }, { kind: "directive", type: i1.CheckboxControlValueAccessor, selector: "input[type=checkbox][formControlName],input[type=checkbox][formControl],input[type=checkbox][ngModel]" }, { kind: "directive", type: i1.NgControlStatus, selector: "[formControlName],[ngModel],[formControl]" }, { kind: "directive", type: i1.NgControlStatusGroup, selector: "[formGroupName],[formArrayName],[ngModelGroup],[formGroup],form:not([ngNoForm]),[ngForm]" }, { kind: "directive", type: i1.FormGroupDirective, selector: "[formGroup]", inputs: ["formGroup"], outputs: ["ngSubmit"], exportAs: ["ngForm"] }, { kind: "directive", type: i1.FormControlName, selector: "[formControlName]", inputs: ["formControlName", "disabled", "ngModel"], outputs: ["ngModelChange"] }, { kind: "directive", type: RouterLink, selector: "[routerLink]", inputs: ["target", "queryParams", "fragment", "queryParamsHandling", "state", "info", "relativeTo", "preserveFragment", "skipLocationChange", "replaceUrl", "routerLink"] }, { kind: "component", type: ChillI18nLabelComponent, selector: "app-chill-i18n-label", inputs: ["labelGuid", "primaryDefaultText", "secondaryDefaultText", "editable"] }, { kind: "component", type: ChillI18nButtonLabelComponent, selector: "app-chill-i18n-button-label", inputs: ["labelGuid", "primaryDefaultText", "secondaryDefaultText", "editable"] }, { kind: "directive", type: NoticeTransitionDirective, selector: ".notice" }] }); }
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: RegisterPageComponent, decorators: [{
            type: Component,
            args: [{
                    selector: 'app-register-page',
                    standalone: true,
                    imports: [CommonModule, ReactiveFormsModule, RouterLink, ChillI18nLabelComponent, ChillI18nButtonLabelComponent, NoticeTransitionDirective],
                    template: `
    <section class="auth-page">
      <div class="auth-card wide">
        <p class="eyebrow"><app-chill-i18n-label [labelGuid]="'A651A560-1828-4D67-8D60-8B97011231D7'" [primaryDefaultText]="'ChillSharp Auth'" [secondaryDefaultText]="'Autenticazione ChillSharp'" /></p>
        <h1><app-chill-i18n-label [labelGuid]="'0A777B5C-F7D1-4084-B32F-D5162E100AF6'" [primaryDefaultText]="'Register'" [secondaryDefaultText]="'Registrazione'" /></h1>
        <p class="lede"><app-chill-i18n-label [labelGuid]="'E4AB90A6-DB4D-4D34-BA80-8D07F6F5595D'" [primaryDefaultText]="'Create an ASP.NET Core Identity account and optionally the linked ChillSharp auth user.'" [secondaryDefaultText]="&quot;Crea un account ASP.NET Core Identity e, facoltativamente, l'utente auth collegato di ChillSharp.&quot;" /></p>

        @if (successMessage()) {
          <div class="notice success">{{ successMessage() }}</div>
        }

        @if (errorMessage()) {
          <div class="notice error">{{ errorMessage() }}</div>
        }

        <form [formGroup]="form" (ngSubmit)="submit()" class="auth-form two-columns">
          <label>
            <span><app-chill-i18n-label [labelGuid]="'2AF5EB08-932E-4D4D-9338-75E1808B5F16'" [primaryDefaultText]="'Username'" [secondaryDefaultText]="'Nome utente'" /></span>
            <input type="text" formControlName="userName" autocomplete="username" />
          </label>

          <label>
            <span><app-chill-i18n-label [labelGuid]="'311C8595-76C7-41DF-B4A0-0D0EF8E9A3D7'" [primaryDefaultText]="'Email'" [secondaryDefaultText]="'Email'" /></span>
            <input type="email" formControlName="email" autocomplete="email" />
          </label>

          <label class="full-width">
            <span><app-chill-i18n-label [labelGuid]="'C0D8A063-E084-460D-BF83-BCE32CB68588'" [primaryDefaultText]="'Display name'" [secondaryDefaultText]="'Nome visualizzato'" /></span>
            <input type="text" formControlName="displayName" autocomplete="name" />
          </label>

          <label>
            <span><app-chill-i18n-label [labelGuid]="'A76807CB-91F6-41A5-B565-D86EEA811241'" [primaryDefaultText]="'Password'" [secondaryDefaultText]="'Password'" /></span>
            <input type="password" formControlName="password" autocomplete="new-password" />
          </label>

          <label>
            <span><app-chill-i18n-label [labelGuid]="'14EB51FA-9D9B-427C-AFD6-CC54031B9B26'" [primaryDefaultText]="'Confirm password'" [secondaryDefaultText]="'Conferma password'" /></span>
            <input type="password" formControlName="confirmPassword" autocomplete="new-password" />
          </label>

          <label class="checkbox full-width">
            <input type="checkbox" formControlName="createChillAuthUser" />
            <span><app-chill-i18n-label [labelGuid]="'E69C6D4F-07EC-42BA-B30D-AFA77B84E595'" [primaryDefaultText]="'Create linked Chill auth user'" [secondaryDefaultText]="'Crea utente auth Chill collegato'" /></span>
          </label>

          @if (form.hasError('passwordMismatch') && form.touched) {
            <div class="notice error full-width">{{ chill.T('12632967-9A9B-4A16-B7DF-D64B7BA7914A', 'Password confirmation does not match.', 'La conferma della password non corrisponde.') }}</div>
          }

          <button type="submit" class="full-width" [disabled]="isSubmitting() || form.invalid">
            @if (isSubmitting()) {
              <app-chill-i18n-button-label [labelGuid]="'A39321BE-5534-40B7-B1A7-F32BF872C997'" [primaryDefaultText]="'Creating account...'" [secondaryDefaultText]="'Creazione account in corso...'" />
            } @else {
              <app-chill-i18n-button-label [labelGuid]="'61E5DBBB-413A-449B-BE0E-B4A991FA1E39'" [primaryDefaultText]="'Create account'" [secondaryDefaultText]="'Crea account'" />
            }
          </button>
        </form>

        <nav class="auth-links">
          <a routerLink="/login">{{ chill.T('1919121D-A2BC-403E-8FCA-E120630A1FAC', 'Back to login', 'Torna al login') }}</a>
          <a routerLink="/reset-password">{{ chill.T('1322FAE4-DBD5-4C8D-8988-FA6035551E02', 'Reset password', 'Reimposta password') }}</a>
        </nav>
      </div>
    </section>
  `
                }]
        }] });

class ResetPasswordPageComponent {
    constructor() {
        this.chill = inject(ChillService);
        this.formBuilder = inject(FormBuilder);
        this.isSubmitting = signal(false);
        this.errorMessage = signal('');
        this.successMessage = signal('');
        this.response = signal(null);
        this.form = this.formBuilder.nonNullable.group({
            userNameOrEmail: ['', Validators.required]
        });
    }
    submit() {
        if (this.form.invalid || this.isSubmitting()) {
            this.form.markAllAsTouched();
            return;
        }
        this.isSubmitting.set(true);
        this.errorMessage.set('');
        this.successMessage.set('');
        this.response.set(null);
        this.chill.requestPasswordReset({
            UserNameOrEmail: this.form.getRawValue().userNameOrEmail
        }).subscribe({
            next: (response) => {
                this.isSubmitting.set(false);
                this.response.set(response);
                this.successMessage.set(this.chill.T('0C3066C3-C623-4895-8C0A-28DAB44AF6A1', 'Reset request accepted by the ChillSharp auth endpoint.', 'Richiesta di reset accettata dall\'endpoint auth di ChillSharp.'));
            },
            error: (error) => {
                this.isSubmitting.set(false);
                this.errorMessage.set(this.chill.formatError(error));
            }
        });
    }
    static { this.ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: ResetPasswordPageComponent, deps: [], target: i0.ɵɵFactoryTarget.Component }); }
    static { this.ɵcmp = i0.ɵɵngDeclareComponent({ minVersion: "17.0.0", version: "19.2.21", type: ResetPasswordPageComponent, isStandalone: true, selector: "app-reset-password-page", ngImport: i0, template: `
    <section class="auth-page">
      <div class="auth-card">
        <p class="eyebrow"><app-chill-i18n-label [labelGuid]="'A651A560-1828-4D67-8D60-8B97011231D7'" [primaryDefaultText]="'ChillSharp Auth'" [secondaryDefaultText]="'Autenticazione ChillSharp'" /></p>
        <h1><app-chill-i18n-label [labelGuid]="'1322FAE4-DBD5-4C8D-8988-FA6035551E02'" [primaryDefaultText]="'Reset password'" [secondaryDefaultText]="'Reimposta password'" /></h1>
        <p class="lede"><app-chill-i18n-label [labelGuid]="'6F70BF2E-01D1-4102-A544-2B22CF54C2EA'" [primaryDefaultText]="'Request a password-reset token through the ChillSharp auth reset endpoint.'" [secondaryDefaultText]="&quot;Richiedi un token di reimpostazione password tramite l'endpoint auth di reset di ChillSharp.&quot;" /></p>

        @if (successMessage()) {
          <div class="notice success">{{ successMessage() }}</div>
        }

        @if (errorMessage()) {
          <div class="notice error">{{ errorMessage() }}</div>
        }

        <form [formGroup]="form" (ngSubmit)="submit()" class="auth-form">
          <label>
            <span><app-chill-i18n-label [labelGuid]="'EA8C79F7-B95E-40A8-B638-C09BFD355A94'" [primaryDefaultText]="'Username or email'" [secondaryDefaultText]="'Nome utente o email'" /></span>
            <input type="text" formControlName="userNameOrEmail" autocomplete="username" />
          </label>

          <button type="submit" [disabled]="isSubmitting() || form.invalid">
            @if (isSubmitting()) {
              <app-chill-i18n-button-label [labelGuid]="'33346EC7-C16F-4E58-A24E-097534F42BBE'" [primaryDefaultText]="'Requesting...'" [secondaryDefaultText]="'Richiesta in corso...'" />
            } @else {
              <app-chill-i18n-button-label [labelGuid]="'6E44886E-EA78-4749-AABF-53E1BE8022D8'" [primaryDefaultText]="'Request reset token'" [secondaryDefaultText]="'Richiedi token di reset'" />
            }
          </button>
        </form>

        @if (response()) {
          <div class="token-panel">
            <p class="token-title"><app-chill-i18n-label [labelGuid]="'DCCDB0A2-80BA-465D-8D96-8A5FF9A0179D'" [primaryDefaultText]="'Reset response'" [secondaryDefaultText]="'Risposta reset'" /></p>
            <dl>
              <div>
                <dt><app-chill-i18n-label [labelGuid]="'970FE371-F8CB-4C64-9DFE-7F72241D8D9A'" [primaryDefaultText]="'Accepted'" [secondaryDefaultText]="'Accettato'" /></dt>
                <dd>{{ response()?.IsAccepted
                  ? chill.T('E7FE0A44-0957-453A-A2AA-4F08FD38D8E1', 'Yes', 'Sì')
                  : chill.T('27EC1CA2-5AAA-4A14-B89A-9E4317349917', 'No', 'No') }}</dd>
              </div>
              <div>
                <dt><app-chill-i18n-label [labelGuid]="'CEB1B59C-FD4E-46D4-B11C-F8B33EA4C32E'" [primaryDefaultText]="'User ID'" [secondaryDefaultText]="'ID utente'" /></dt>
                <dd>{{ response()?.UserId || chill.T('6D5C3326-1F96-434A-B6EC-0E72C5A79A0F', 'Not returned by the server', 'Non restituito dal server') }}</dd>
              </div>
              <div>
                <dt><app-chill-i18n-label [labelGuid]="'3D525E29-B464-49FF-AC72-6D7E05D0F226'" [primaryDefaultText]="'Reset token'" [secondaryDefaultText]="'Token di reset'" /></dt>
                <dd class="wrap">{{ response()?.ResetToken || chill.T('6D5C3326-1F96-434A-B6EC-0E72C5A79A0F', 'Not returned by the server', 'Non restituito dal server') }}</dd>
              </div>
            </dl>

            @if (response()?.UserId && response()?.ResetToken) {
              <a
                class="button-link"
                [routerLink]="['/confirm-reset-password', response()?.ResetToken]"
                [queryParams]="{ userId: response()?.UserId }">
                {{ chill.T('8FA3C95A-E55C-40F4-BFF7-E712A11EEB22', 'Continue to confirmation', 'Continua alla conferma') }}
              </a>
            }
          </div>
        }

        <nav class="auth-links">
          <a routerLink="/login">{{ chill.T('1919121D-A2BC-403E-8FCA-E120630A1FAC', 'Back to login', 'Torna al login') }}</a>
          <a routerLink="/register">{{ chill.T('61E5DBBB-413A-449B-BE0E-B4A991FA1E39', 'Create account', 'Crea account') }}</a>
        </nav>
      </div>
    </section>
  `, isInline: true, dependencies: [{ kind: "ngmodule", type: CommonModule }, { kind: "ngmodule", type: ReactiveFormsModule }, { kind: "directive", type: i1.ɵNgNoValidate, selector: "form:not([ngNoForm]):not([ngNativeValidate])" }, { kind: "directive", type: i1.DefaultValueAccessor, selector: "input:not([type=checkbox])[formControlName],textarea[formControlName],input:not([type=checkbox])[formControl],textarea[formControl],input:not([type=checkbox])[ngModel],textarea[ngModel],[ngDefaultControl]" }, { kind: "directive", type: i1.NgControlStatus, selector: "[formControlName],[ngModel],[formControl]" }, { kind: "directive", type: i1.NgControlStatusGroup, selector: "[formGroupName],[formArrayName],[ngModelGroup],[formGroup],form:not([ngNoForm]),[ngForm]" }, { kind: "directive", type: i1.FormGroupDirective, selector: "[formGroup]", inputs: ["formGroup"], outputs: ["ngSubmit"], exportAs: ["ngForm"] }, { kind: "directive", type: i1.FormControlName, selector: "[formControlName]", inputs: ["formControlName", "disabled", "ngModel"], outputs: ["ngModelChange"] }, { kind: "directive", type: RouterLink, selector: "[routerLink]", inputs: ["target", "queryParams", "fragment", "queryParamsHandling", "state", "info", "relativeTo", "preserveFragment", "skipLocationChange", "replaceUrl", "routerLink"] }, { kind: "component", type: ChillI18nLabelComponent, selector: "app-chill-i18n-label", inputs: ["labelGuid", "primaryDefaultText", "secondaryDefaultText", "editable"] }, { kind: "component", type: ChillI18nButtonLabelComponent, selector: "app-chill-i18n-button-label", inputs: ["labelGuid", "primaryDefaultText", "secondaryDefaultText", "editable"] }, { kind: "directive", type: NoticeTransitionDirective, selector: ".notice" }] }); }
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: ResetPasswordPageComponent, decorators: [{
            type: Component,
            args: [{
                    selector: 'app-reset-password-page',
                    standalone: true,
                    imports: [CommonModule, ReactiveFormsModule, RouterLink, ChillI18nLabelComponent, ChillI18nButtonLabelComponent, NoticeTransitionDirective],
                    template: `
    <section class="auth-page">
      <div class="auth-card">
        <p class="eyebrow"><app-chill-i18n-label [labelGuid]="'A651A560-1828-4D67-8D60-8B97011231D7'" [primaryDefaultText]="'ChillSharp Auth'" [secondaryDefaultText]="'Autenticazione ChillSharp'" /></p>
        <h1><app-chill-i18n-label [labelGuid]="'1322FAE4-DBD5-4C8D-8988-FA6035551E02'" [primaryDefaultText]="'Reset password'" [secondaryDefaultText]="'Reimposta password'" /></h1>
        <p class="lede"><app-chill-i18n-label [labelGuid]="'6F70BF2E-01D1-4102-A544-2B22CF54C2EA'" [primaryDefaultText]="'Request a password-reset token through the ChillSharp auth reset endpoint.'" [secondaryDefaultText]="&quot;Richiedi un token di reimpostazione password tramite l'endpoint auth di reset di ChillSharp.&quot;" /></p>

        @if (successMessage()) {
          <div class="notice success">{{ successMessage() }}</div>
        }

        @if (errorMessage()) {
          <div class="notice error">{{ errorMessage() }}</div>
        }

        <form [formGroup]="form" (ngSubmit)="submit()" class="auth-form">
          <label>
            <span><app-chill-i18n-label [labelGuid]="'EA8C79F7-B95E-40A8-B638-C09BFD355A94'" [primaryDefaultText]="'Username or email'" [secondaryDefaultText]="'Nome utente o email'" /></span>
            <input type="text" formControlName="userNameOrEmail" autocomplete="username" />
          </label>

          <button type="submit" [disabled]="isSubmitting() || form.invalid">
            @if (isSubmitting()) {
              <app-chill-i18n-button-label [labelGuid]="'33346EC7-C16F-4E58-A24E-097534F42BBE'" [primaryDefaultText]="'Requesting...'" [secondaryDefaultText]="'Richiesta in corso...'" />
            } @else {
              <app-chill-i18n-button-label [labelGuid]="'6E44886E-EA78-4749-AABF-53E1BE8022D8'" [primaryDefaultText]="'Request reset token'" [secondaryDefaultText]="'Richiedi token di reset'" />
            }
          </button>
        </form>

        @if (response()) {
          <div class="token-panel">
            <p class="token-title"><app-chill-i18n-label [labelGuid]="'DCCDB0A2-80BA-465D-8D96-8A5FF9A0179D'" [primaryDefaultText]="'Reset response'" [secondaryDefaultText]="'Risposta reset'" /></p>
            <dl>
              <div>
                <dt><app-chill-i18n-label [labelGuid]="'970FE371-F8CB-4C64-9DFE-7F72241D8D9A'" [primaryDefaultText]="'Accepted'" [secondaryDefaultText]="'Accettato'" /></dt>
                <dd>{{ response()?.IsAccepted
                  ? chill.T('E7FE0A44-0957-453A-A2AA-4F08FD38D8E1', 'Yes', 'Sì')
                  : chill.T('27EC1CA2-5AAA-4A14-B89A-9E4317349917', 'No', 'No') }}</dd>
              </div>
              <div>
                <dt><app-chill-i18n-label [labelGuid]="'CEB1B59C-FD4E-46D4-B11C-F8B33EA4C32E'" [primaryDefaultText]="'User ID'" [secondaryDefaultText]="'ID utente'" /></dt>
                <dd>{{ response()?.UserId || chill.T('6D5C3326-1F96-434A-B6EC-0E72C5A79A0F', 'Not returned by the server', 'Non restituito dal server') }}</dd>
              </div>
              <div>
                <dt><app-chill-i18n-label [labelGuid]="'3D525E29-B464-49FF-AC72-6D7E05D0F226'" [primaryDefaultText]="'Reset token'" [secondaryDefaultText]="'Token di reset'" /></dt>
                <dd class="wrap">{{ response()?.ResetToken || chill.T('6D5C3326-1F96-434A-B6EC-0E72C5A79A0F', 'Not returned by the server', 'Non restituito dal server') }}</dd>
              </div>
            </dl>

            @if (response()?.UserId && response()?.ResetToken) {
              <a
                class="button-link"
                [routerLink]="['/confirm-reset-password', response()?.ResetToken]"
                [queryParams]="{ userId: response()?.UserId }">
                {{ chill.T('8FA3C95A-E55C-40F4-BFF7-E712A11EEB22', 'Continue to confirmation', 'Continua alla conferma') }}
              </a>
            }
          </div>
        }

        <nav class="auth-links">
          <a routerLink="/login">{{ chill.T('1919121D-A2BC-403E-8FCA-E120630A1FAC', 'Back to login', 'Torna al login') }}</a>
          <a routerLink="/register">{{ chill.T('61E5DBBB-413A-449B-BE0E-B4A991FA1E39', 'Create account', 'Crea account') }}</a>
        </nav>
      </div>
    </section>
  `
                }]
        }] });

const requireAuthGuard = () => {
    const chill = inject(ChillService);
    if (chill.isAuthenticated()) {
        return true;
    }
    return inject(Router).createUrlTree(['/login']);
};
const guestOnlyGuard = () => {
    const chill = inject(ChillService);
    if (!chill.isAuthenticated()) {
        return true;
    }
    return inject(Router).createUrlTree(['/workspace']);
};
const CHILL_SHARP_UI_ROUTES = [
    {
        path: '',
        pathMatch: 'full',
        redirectTo: 'login'
    },
    {
        path: '',
        component: AuthShellComponent,
        children: [
            { path: 'login', component: LoginPageComponent, canActivate: [guestOnlyGuard] },
            { path: 'register', component: RegisterPageComponent, canActivate: [guestOnlyGuard] },
            { path: 'reset-password', component: ResetPasswordPageComponent },
            { path: 'confirm-reset-password', component: ConfirmResetPageComponent },
            { path: 'confirm-reset-password/:token', component: ConfirmResetPageComponent }
        ]
    },
    { path: 'confirm-reset', pathMatch: 'full', redirectTo: 'confirm-reset-password' },
    { path: 'confirm-reset/:token', pathMatch: 'full', redirectTo: 'confirm-reset-password/:token' },
    { path: 'workspace', component: WorkspacePageComponent, canActivate: [requireAuthGuard] },
    { path: 'workspace/:taskId', component: WorkspacePageComponent, canActivate: [requireAuthGuard] },
    { path: '**', redirectTo: 'login' }
];

function provideChillSharpUiCore() {
    return [
        ...provideChillSharpClient({
            baseUrl: CHILL_BASE_URL,
            options: {
                cultureName: readStoredCultureName(),
                accessToken: readStoredAccessToken(),
                fetchImpl: authAwareFetch,
                signalRWithCredentials: false
            }
        }),
        {
            provide: ChillSharpNgClient,
            useFactory: () => new ChillSharpNgClient(inject(CHILL_SHARP_CLIENT))
        },
        {
            provide: APP_INITIALIZER,
            multi: true,
            useFactory: () => () => inject(WorkspaceTaskRegistryService).initialize()
        },
        {
            provide: APP_INITIALIZER,
            multi: true,
            useFactory: () => () => inject(ChillService).initialize()
        }
    ];
}
async function authAwareFetch(input, init) {
    const method = (init?.method ?? 'GET').toUpperCase();
    const headers = new Headers(init?.headers);
    const accessToken = readStoredAccessToken();
    if (accessToken && !headers.has('Authorization')) {
        headers.set('Authorization', `Bearer ${accessToken}`);
    }
    return globalThis.fetch(input, {
        ...init,
        method,
        headers
    });
}
function readStoredAccessToken() {
    const rawSession = globalThis.localStorage?.getItem(SESSION_STORAGE_KEY);
    if (!rawSession) {
        return '';
    }
    try {
        const parsed = JSON.parse(rawSession);
        return parsed.accessToken?.trim() ?? '';
    }
    catch {
        return '';
    }
}
function readStoredCultureName() {
    const rawPreferences = globalThis.localStorage?.getItem(USER_PREFERENCES_STORAGE_KEY);
    if (!rawPreferences) {
        return CHILL_CULTURE;
    }
    try {
        const parsed = JSON.parse(rawPreferences);
        return parsed.displayCultureName?.trim() || CHILL_CULTURE;
    }
    catch {
        return CHILL_CULTURE;
    }
}

class ChillTextEditorDialogComponent {
    constructor() {
        this.dialog = inject(WorkspaceDialogService);
        this.value = input('');
        this.language = input('plaintext');
        this.placeholder = input('');
        this.disabled = input(false);
        this.draft = signal('');
        effect(() => {
            this.draft.set(this.value());
        });
    }
    submit() {
        this.dialog.confirm(this.draft());
    }
    static { this.ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: ChillTextEditorDialogComponent, deps: [], target: i0.ɵɵFactoryTarget.Component }); }
    static { this.ɵcmp = i0.ɵɵngDeclareComponent({ minVersion: "17.1.0", version: "19.2.21", type: ChillTextEditorDialogComponent, isStandalone: true, selector: "app-chill-text-editor-dialog", inputs: { value: { classPropertyName: "value", publicName: "value", isSignal: true, isRequired: false, transformFunction: null }, language: { classPropertyName: "language", publicName: "language", isSignal: true, isRequired: false, transformFunction: null }, placeholder: { classPropertyName: "placeholder", publicName: "placeholder", isSignal: true, isRequired: false, transformFunction: null }, disabled: { classPropertyName: "disabled", publicName: "disabled", isSignal: true, isRequired: false, transformFunction: null } }, ngImport: i0, template: `
    <section class="text-editor-dialog">
      <app-chill-json-input
        [value]="draft()"
        [language]="language()"
        [placeholder]="placeholder()"
        [disabled]="disabled()"
        [mobileFullHeight]="true"
        minHeight="18rem"
        maxHeight="70vh"
        (valueChange)="draft.set($event)"></app-chill-json-input>
    </section>
  `, isInline: true, styles: [":host{display:block;min-height:0}.text-editor-dialog{display:grid;min-width:min(58rem,calc(100vw - 5rem));min-height:0}.text-editor-dialog app-chill-json-input{min-height:0}@media(max-width:720px){:host,.text-editor-dialog{height:100%;min-width:0}}\n"], dependencies: [{ kind: "ngmodule", type: CommonModule }, { kind: "component", type: ChillJsonInputComponent, selector: "app-chill-json-input", inputs: ["value", "placeholder", "invalid", "disabled", "language", "minHeight", "maxHeight", "mobileFullHeight"], outputs: ["valueChange", "blur"] }] }); }
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: ChillTextEditorDialogComponent, decorators: [{
            type: Component,
            args: [{ selector: 'app-chill-text-editor-dialog', standalone: true, imports: [CommonModule, ChillJsonInputComponent], template: `
    <section class="text-editor-dialog">
      <app-chill-json-input
        [value]="draft()"
        [language]="language()"
        [placeholder]="placeholder()"
        [disabled]="disabled()"
        [mobileFullHeight]="true"
        minHeight="18rem"
        maxHeight="70vh"
        (valueChange)="draft.set($event)"></app-chill-json-input>
    </section>
  `, styles: [":host{display:block;min-height:0}.text-editor-dialog{display:grid;min-width:min(58rem,calc(100vw - 5rem));min-height:0}.text-editor-dialog app-chill-json-input{min-height:0}@media(max-width:720px){:host,.text-editor-dialog{height:100%;min-width:0}}\n"] }]
        }], ctorParameters: () => [] });

var chillTextEditorDialog_component = /*#__PURE__*/Object.freeze({
    __proto__: null,
    ChillTextEditorDialogComponent: ChillTextEditorDialogComponent
});

const STANDARD_CULTURE_NAMES = [
    'it-IT',
    'en-GB',
    'en-US',
    'fr-FR',
    'de-DE',
    'es-ES',
    'pt-PT',
    'pt-BR',
    'nl-NL',
    'sv-SE',
    'da-DK',
    'nb-NO',
    'fi-FI',
    'pl-PL',
    'cs-CZ',
    'sk-SK',
    'hu-HU',
    'ro-RO',
    'el-GR',
    'tr-TR',
    'ru-RU',
    'uk-UA',
    'ar-SA',
    'he-IL',
    'hi-IN',
    'th-TH',
    'zh-CN',
    'zh-TW',
    'ja-JP',
    'ko-KR'
];
function getCultureNameOptions() {
    return STANDARD_CULTURE_NAMES
        .map((cultureName) => [cultureName, cultureName]);
}

const DATE_FORMAT_OPTIONS = [
    ['dd/MM/yyyy', 'dd/MM/yyyy'],
    ['MM/dd/yyyy', 'MM/dd/yyyy'],
    ['yyyy-MM-dd', 'yyyy-MM-dd'],
    ['DD/MM/YYYY', 'DD/MM/YYYY'],
    ['MM/DD/YYYY', 'MM/DD/YYYY'],
    ['YYYY-MM-DD', 'YYYY-MM-DD']
];
function getDateFormatOptions() {
    return [...DATE_FORMAT_OPTIONS];
}

const FALLBACK_IANA_TIME_ZONES = [
    'UTC',
    'Europe/London',
    'Europe/Rome',
    'Europe/Paris',
    'Europe/Berlin',
    'Europe/Madrid',
    'Europe/Athens',
    'America/New_York',
    'America/Chicago',
    'America/Denver',
    'America/Los_Angeles',
    'America/Sao_Paulo',
    'Asia/Dubai',
    'Asia/Kolkata',
    'Asia/Bangkok',
    'Asia/Singapore',
    'Asia/Tokyo',
    'Australia/Sydney'
];
function getIanaTimeZoneOptions() {
    const supportedValuesOf = Intl.supportedValuesOf;
    const timeZones = typeof supportedValuesOf === 'function'
        ? supportedValuesOf('timeZone')
        : FALLBACK_IANA_TIME_ZONES;
    return [...new Set(timeZones)]
        .filter((timeZone) => typeof timeZone === 'string' && timeZone.trim().length > 0)
        .sort((left, right) => left.localeCompare(right))
        .map((timeZone) => [timeZone, timeZone]);
}

const MANAGED_METADATA_KEYS = ['required', 'readonly', 'minLength', 'maxLength', 'pattern', 'min', 'max', 'options'];
class SchemaPropertyDialogComponent {
    constructor() {
        this.chill = inject(ChillService);
        this.dialog = inject(WorkspaceDialogService);
        this.schema = input(null);
        this.property = input(null);
        this.draft = signal(this.createEmptyDraft());
        this.dateFormatOptions = getDateFormatOptions();
        this.schemaTypeLabel = computed(() => this.schema()?.displayName?.trim() || this.schema()?.chillType?.trim() || '');
        this.metadataJsonInvalid = computed(() => !this.tryParseMetadata(this.draft().metadataJson).ok);
        this.selectedPropertyType = computed(() => this.draft().propertyType);
        this.showLengthSettings = computed(() => {
            const propertyType = this.selectedPropertyType();
            return propertyType === CHILL_PROPERTY_TYPE$1.String
                || propertyType === CHILL_PROPERTY_TYPE$1.Text
                || propertyType === CHILL_PROPERTY_TYPE$1.Json
                || propertyType === CHILL_PROPERTY_TYPE$1.Select;
        });
        this.showRegexPattern = computed(() => {
            const propertyType = this.selectedPropertyType();
            return propertyType === CHILL_PROPERTY_TYPE$1.String
                || propertyType === CHILL_PROPERTY_TYPE$1.Text;
        });
        this.showIntegerRange = computed(() => this.selectedPropertyType() === CHILL_PROPERTY_TYPE$1.Integer);
        this.showDecimalSettings = computed(() => this.selectedPropertyType() === CHILL_PROPERTY_TYPE$1.Decimal);
        this.showDateFormat = computed(() => {
            const propertyType = this.selectedPropertyType();
            return propertyType === CHILL_PROPERTY_TYPE$1.Date || propertyType === CHILL_PROPERTY_TYPE$1.DateTime;
        });
        this.showCustomFormat = computed(() => {
            const propertyType = this.selectedPropertyType();
            return propertyType === CHILL_PROPERTY_TYPE$1.String
                || propertyType === CHILL_PROPERTY_TYPE$1.Text
                || propertyType === CHILL_PROPERTY_TYPE$1.Json
                || propertyType === CHILL_PROPERTY_TYPE$1.Date
                || propertyType === CHILL_PROPERTY_TYPE$1.Time
                || propertyType === CHILL_PROPERTY_TYPE$1.DateTime
                || propertyType === CHILL_PROPERTY_TYPE$1.Duration;
        });
        this.showEnumValues = computed(() => this.selectedPropertyType() === CHILL_PROPERTY_TYPE$1.Select);
        this.validationMessages = computed(() => this.validateDraft(this.draft()));
        this.propertyTypeOptions = CHILL_PROPERTY_TYPE_OPTIONS;
        effect(() => {
            const property = this.property();
            this.draft.set(property ? this.createDraft(property) : this.createEmptyDraft());
        });
    }
    canDialogSubmit() {
        return this.validationMessages().length === 0;
    }
    submit() {
        const source = this.property();
        if (!source || !this.canDialogSubmit()) {
            return;
        }
        this.dialog.confirm(this.buildProperty(source, this.draft()));
    }
    updateText(key, value) {
        this.draft.update((current) => ({
            ...current,
            [key]: value
        }));
    }
    updateBoolean(key, value) {
        this.draft.update((current) => ({
            ...current,
            [key]: value === true
        }));
    }
    updatePropertyType(value) {
        const parsed = typeof value === 'number' ? value : Number(value);
        const property = this.property();
        if (!property || !Number.isFinite(parsed) || !canChangeChillPropertyType(property.propertyType, parsed)) {
            return;
        }
        this.draft.update((current) => ({
            ...current,
            propertyType: parsed
        }));
    }
    isPropertyTypeOptionDisabled(value) {
        const property = this.property();
        return !!property && !canChangeChillPropertyType(property.propertyType, value);
    }
    enumValuesPlaceholder() {
        return this.chill.T('563559B9-F9B4-4E7A-923A-086A517CDE8A', 'One value per line. Use "value = label" to customize the shown text.', 'Un valore per riga. Usa "valore = etichetta" per personalizzare il testo mostrato.');
    }
    metadataPlaceholder() {
        return '{\n  \n}';
    }
    createDraft(property) {
        return {
            name: property.name ?? '',
            displayName: property.displayName ?? property.name ?? '',
            propertyType: property.propertyType ?? CHILL_PROPERTY_TYPE$1.Unknown,
            isNullable: property.isNullable !== false,
            isReadOnly: property.isReadOnly ?? this.readBooleanMetadata(property.metadata, 'readonly'),
            minLength: this.formatOptionalNumber(property.minLength ?? this.readMetadataNumber(property.metadata, 'minLength')),
            maxLength: this.formatOptionalNumber(property.maxLength ?? this.readMetadataNumber(property.metadata, 'maxLength')),
            integerMinValue: this.formatOptionalNumber(property.integerMinValue ?? this.readMetadataNumber(property.metadata, 'min')),
            integerMaxValue: this.formatOptionalNumber(property.integerMaxValue ?? this.readMetadataNumber(property.metadata, 'max')),
            decimalMinValue: this.formatOptionalNumber(property.decimalMinValue ?? this.readMetadataNumber(property.metadata, 'min')),
            decimalMaxValue: this.formatOptionalNumber(property.decimalMaxValue ?? this.readMetadataNumber(property.metadata, 'max')),
            decimalPlaces: this.formatOptionalNumber(property.decimalPlaces),
            precision: this.formatOptionalNumber(property.precision),
            scale: this.formatOptionalNumber(property.scale),
            dateFormat: property.dateFormat ?? '',
            customFormat: property.customFormat ?? '',
            regexPattern: property.regexPattern ?? this.readMetadataString(property.metadata, 'pattern'),
            enumValues: this.serializeEnumValues(property),
            metadataJson: this.stringifyMetadata(property.metadata)
        };
    }
    createEmptyDraft() {
        return {
            name: '',
            displayName: '',
            propertyType: CHILL_PROPERTY_TYPE$1.String,
            isNullable: true,
            isReadOnly: false,
            minLength: '',
            maxLength: '',
            integerMinValue: '',
            integerMaxValue: '',
            decimalMinValue: '',
            decimalMaxValue: '',
            decimalPlaces: '',
            precision: '',
            scale: '',
            dateFormat: '',
            customFormat: '',
            regexPattern: '',
            enumValues: '',
            metadataJson: '{\n  \n}'
        };
    }
    validateDraft(draft) {
        const messages = [];
        if (!draft.name.trim()) {
            messages.push(this.chill.T('93531950-1BC3-470F-A460-84296F0E8569', 'Property name is required.', 'Il nome proprietà è obbligatorio.'));
        }
        if (!this.tryParseMetadata(draft.metadataJson).ok) {
            messages.push(this.chill.T('00B1B7D1-F59B-4D53-8D0A-C99D7D0A7180', 'Metadata must be a valid JSON object.', 'I metadata devono essere un oggetto JSON valido.'));
        }
        const minLength = this.parseOptionalInteger(draft.minLength);
        const maxLength = this.parseOptionalInteger(draft.maxLength);
        if (this.showLengthSettings()) {
            if (draft.minLength.trim() && minLength === null) {
                messages.push(this.chill.T('7A070C4A-8E91-4736-B7B6-65E58D4ED1F4', 'Min length must be an integer.', 'La lunghezza minima deve essere un intero.'));
            }
            if (draft.maxLength.trim() && maxLength === null) {
                messages.push(this.chill.T('4C6079A7-B5D8-4A05-B9D3-4349B4518D9A', 'Max length must be an integer.', 'La lunghezza massima deve essere un intero.'));
            }
            if (minLength !== null && maxLength !== null && minLength > maxLength) {
                messages.push(this.chill.T('E7502A18-FB9A-4E61-83BC-51E2D86CE83C', 'Min length cannot exceed max length.', 'La lunghezza minima non può superare la lunghezza massima.'));
            }
        }
        const integerMin = this.parseOptionalInteger(draft.integerMinValue);
        const integerMax = this.parseOptionalInteger(draft.integerMaxValue);
        if (this.showIntegerRange()) {
            if (draft.integerMinValue.trim() && integerMin === null) {
                messages.push(this.chill.T('389A718A-A7A3-4D91-8E93-0D7284F940B4', 'Integer min value must be an integer.', 'Il valore intero minimo deve essere un intero.'));
            }
            if (draft.integerMaxValue.trim() && integerMax === null) {
                messages.push(this.chill.T('68B76D88-8E84-4C49-A6AF-CBDBCB7A8892', 'Integer max value must be an integer.', 'Il valore intero massimo deve essere un intero.'));
            }
            if (integerMin !== null && integerMax !== null && integerMin > integerMax) {
                messages.push(this.chill.T('2A0B5BEA-E98A-43E1-8AE6-BF90105A10A1', 'Integer min value cannot exceed integer max value.', 'Il valore intero minimo non può superare il valore intero massimo.'));
            }
        }
        const decimalMin = this.parseOptionalDecimal(draft.decimalMinValue);
        const decimalMax = this.parseOptionalDecimal(draft.decimalMaxValue);
        if (this.showDecimalSettings()) {
            if (draft.decimalMinValue.trim() && decimalMin === null) {
                messages.push(this.chill.T('113D8E72-BF6E-489A-912C-7D19C85173CA', 'Decimal min value must be numeric.', 'Il valore decimale minimo deve essere numerico.'));
            }
            if (draft.decimalMaxValue.trim() && decimalMax === null) {
                messages.push(this.chill.T('74D8A6F0-B66C-4B06-BDE4-E64F210205F6', 'Decimal max value must be numeric.', 'Il valore decimale massimo deve essere numerico.'));
            }
            if (decimalMin !== null && decimalMax !== null && decimalMin > decimalMax) {
                messages.push(this.chill.T('A36EB919-7C96-4C93-A29C-5A4350E3B995', 'Decimal min value cannot exceed decimal max value.', 'Il valore decimale minimo non può superare il valore decimale massimo.'));
            }
            if (draft.decimalPlaces.trim() && this.parseOptionalInteger(draft.decimalPlaces) === null) {
                messages.push(this.chill.T('D01A619A-28B0-42B7-B246-8799D6A0F6D0', 'Decimal places must be an integer.', 'Le cifre decimali devono essere un intero.'));
            }
            if (draft.precision.trim() && this.parseOptionalInteger(draft.precision) === null) {
                messages.push(this.chill.T('3AB179E4-AC9A-40AA-A1A1-B81232A3FEA1', 'Precision must be an integer.', 'La precisione deve essere un intero.'));
            }
            if (draft.scale.trim() && this.parseOptionalInteger(draft.scale) === null) {
                messages.push(this.chill.T('5753A4E0-14B3-4E43-91E6-1592F6E87045', 'Scale must be an integer.', 'La scala deve essere un intero.'));
            }
        }
        if (this.showRegexPattern() && draft.regexPattern.trim()) {
            try {
                new RegExp(draft.regexPattern);
            }
            catch {
                messages.push(this.chill.T('6A78E1B5-7733-4663-8A7D-E4E4F09AE499', 'Regex pattern is invalid.', 'Il pattern regex non è valido.'));
            }
        }
        if (this.showEnumValues() && this.parseEnumOptions(draft.enumValues).length === 0) {
            messages.push(this.chill.T('18D2378E-C16D-4220-924C-0BC6F3EE62C4', 'Select properties need at least one enum value.', 'Le proprietà select richiedono almeno un valore enum.'));
        }
        return messages;
    }
    buildProperty(source, draft) {
        const metadataResult = this.tryParseMetadata(draft.metadataJson);
        const metadata = metadataResult.ok ? { ...metadataResult.value } : {};
        for (const key of MANAGED_METADATA_KEYS) {
            delete metadata[key];
        }
        metadata['required'] = draft.isNullable ? 'false' : 'true';
        if (draft.isReadOnly) {
            metadata['readonly'] = 'true';
        }
        const minLength = this.showLengthSettings() ? this.parseOptionalInteger(draft.minLength) : null;
        const maxLength = this.showLengthSettings() ? this.parseOptionalInteger(draft.maxLength) : null;
        const regexPattern = this.showRegexPattern() ? draft.regexPattern.trim() : '';
        if (minLength !== null) {
            metadata['minLength'] = String(minLength);
        }
        if (maxLength !== null) {
            metadata['maxLength'] = String(maxLength);
        }
        if (regexPattern) {
            metadata['pattern'] = regexPattern;
        }
        const integerMinValue = this.showIntegerRange() ? this.parseOptionalInteger(draft.integerMinValue) : null;
        const integerMaxValue = this.showIntegerRange() ? this.parseOptionalInteger(draft.integerMaxValue) : null;
        const decimalMinValue = this.showDecimalSettings() ? this.parseOptionalDecimal(draft.decimalMinValue) : null;
        const decimalMaxValue = this.showDecimalSettings() ? this.parseOptionalDecimal(draft.decimalMaxValue) : null;
        const decimalPlaces = this.showDecimalSettings() ? this.parseOptionalInteger(draft.decimalPlaces) : null;
        const precision = this.showDecimalSettings() ? this.parseOptionalInteger(draft.precision) : null;
        const scale = this.showDecimalSettings() ? this.parseOptionalInteger(draft.scale) : null;
        if (integerMinValue !== null) {
            metadata['min'] = String(integerMinValue);
        }
        else if (decimalMinValue !== null) {
            metadata['min'] = String(decimalMinValue);
        }
        if (integerMaxValue !== null) {
            metadata['max'] = String(integerMaxValue);
        }
        else if (decimalMaxValue !== null) {
            metadata['max'] = String(decimalMaxValue);
        }
        const enumOptions = this.showEnumValues() ? this.parseEnumOptions(draft.enumValues) : [];
        if (enumOptions.length > 0) {
            metadata['options'] = enumOptions.map((option) => [option.value, option.label]);
        }
        return {
            ...source,
            name: draft.name.trim(),
            displayName: draft.displayName.trim() || draft.name.trim(),
            propertyType: draft.propertyType,
            simplePropertyType: chillSimplePropertyType(draft.propertyType),
            isNullable: draft.isNullable,
            isReadOnly: draft.isReadOnly,
            minLength,
            maxLength,
            integerMinValue,
            integerMaxValue,
            decimalMinValue,
            decimalMaxValue,
            decimalPlaces,
            precision,
            scale,
            dateFormat: this.showDateFormat() ? draft.dateFormat.trim() : '',
            customFormat: this.showCustomFormat() ? draft.customFormat.trim() : '',
            regexPattern,
            enumValues: enumOptions.length > 0 ? enumOptions.map((option) => option.value) : null,
            metadata
        };
    }
    parseEnumOptions(value) {
        return value
            .split(/\r?\n/)
            .map((line) => line.trim())
            .filter((line) => line.length > 0)
            .map((line) => {
            const separatorIndex = line.indexOf('=');
            if (separatorIndex < 0) {
                return { value: line, label: line };
            }
            const optionValue = line.slice(0, separatorIndex).trim();
            const optionLabel = line.slice(separatorIndex + 1).trim();
            return {
                value: optionValue || optionLabel,
                label: optionLabel || optionValue
            };
        })
            .filter((option) => option.value.length > 0 && option.label.length > 0);
    }
    serializeEnumValues(property) {
        const enumValues = Array.isArray(property.enumValues)
            ? property.enumValues.filter((value) => typeof value === 'string' && value.trim().length > 0)
            : [];
        if (enumValues.length > 0) {
            return enumValues.join('\n');
        }
        const rawOptions = property.metadata?.['options'];
        if (!Array.isArray(rawOptions)) {
            return '';
        }
        return rawOptions.flatMap((entry) => {
            if (!Array.isArray(entry) || entry.length < 2) {
                return [];
            }
            const [value, label] = entry;
            const normalizedValue = typeof value === 'string' || typeof value === 'number' ? String(value).trim() : '';
            const normalizedLabel = typeof label === 'string' || typeof label === 'number' ? String(label).trim() : '';
            if (!normalizedValue || !normalizedLabel) {
                return [];
            }
            return [normalizedValue === normalizedLabel ? normalizedValue : `${normalizedValue} = ${normalizedLabel}`];
        }).join('\n');
    }
    tryParseMetadata(value) {
        const normalized = value.trim();
        if (!normalized) {
            return { ok: true, value: {} };
        }
        try {
            const parsed = JSON.parse(normalized);
            if (!parsed || typeof parsed !== 'object' || Array.isArray(parsed)) {
                return { ok: false };
            }
            return { ok: true, value: parsed };
        }
        catch {
            return { ok: false };
        }
    }
    stringifyMetadata(metadata) {
        try {
            return JSON.stringify(metadata ?? {}, null, 2);
        }
        catch {
            return '{\n  \n}';
        }
    }
    readMetadataNumber(metadata, key) {
        const value = metadata?.[key];
        if (typeof value === 'number' && Number.isFinite(value)) {
            return value;
        }
        if (typeof value === 'string' && value.trim()) {
            const parsed = Number(value);
            return Number.isFinite(parsed) ? parsed : null;
        }
        return null;
    }
    readMetadataString(metadata, key) {
        const value = metadata?.[key];
        return typeof value === 'string'
            ? value.trim()
            : typeof value === 'number'
                ? String(value)
                : '';
    }
    readBooleanMetadata(metadata, key) {
        const value = this.readMetadataString(metadata, key).toLowerCase();
        return value === 'true' || value === '1' || value === 'readonly';
    }
    parseOptionalInteger(value) {
        const normalized = value.trim();
        if (!normalized || !/^-?\d+$/.test(normalized)) {
            return normalized ? null : null;
        }
        const parsed = Number(normalized);
        return Number.isSafeInteger(parsed) ? parsed : null;
    }
    parseOptionalDecimal(value) {
        const normalized = value.trim();
        if (!normalized) {
            return null;
        }
        const parsed = Number(normalized.replace(',', '.'));
        return Number.isFinite(parsed) ? parsed : null;
    }
    formatOptionalNumber(value) {
        return value === null || value === undefined || Number.isNaN(value)
            ? ''
            : String(value);
    }
    static { this.ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: SchemaPropertyDialogComponent, deps: [], target: i0.ɵɵFactoryTarget.Component }); }
    static { this.ɵcmp = i0.ɵɵngDeclareComponent({ minVersion: "17.0.0", version: "19.2.21", type: SchemaPropertyDialogComponent, isStandalone: true, selector: "app-schema-property-dialog", inputs: { schema: { classPropertyName: "schema", publicName: "schema", isSignal: true, isRequired: false, transformFunction: null }, property: { classPropertyName: "property", publicName: "property", isSignal: true, isRequired: false, transformFunction: null } }, ngImport: i0, template: "<section class=\"schema-property-dialog\">\n  <p class=\"schema-property-dialog__lede\">\n    {{ chill.T(\n      'FF4665A2-D898-4CC5-B911-B90F3B44AC25',\n      'Edit the selected schema property and save the updated behavior.',\n      'Modifica la propriet\u00E0 di schema selezionata e salva il comportamento aggiornato.'\n    ) }}\n  </p>\n\n  @if (schemaTypeLabel()) {\n    <p class=\"schema-property-dialog__context\">\n      {{ chill.T('C797A0B9-9AB2-4E71-84DE-677416293548', 'Schema type', 'Tipo schema') }}:\n      <strong>{{ schemaTypeLabel() }}</strong>\n    </p>\n  }\n\n  @if (validationMessages().length > 0) {\n    <div class=\"schema-property-dialog__errors\">\n      @for (message of validationMessages(); track message) {\n        <p>{{ message }}</p>\n      }\n    </div>\n  }\n\n  <div class=\"schema-property-dialog__grid\">\n    <label class=\"schema-property-dialog__field\">\n      <span>{{ chill.T('57D9358A-8052-460F-8C0C-5B93B572E73A', 'Name', 'Nome') }}</span>\n      <input type=\"text\" [ngModel]=\"draft().name\" (ngModelChange)=\"updateText('name', $event)\" name=\"schema-property-name\" />\n    </label>\n\n    <label class=\"schema-property-dialog__field\">\n      <span>{{ chill.T('A276C7C0-E297-41EC-88D8-80C151BCA646', 'Display name', 'Nome visualizzato') }}</span>\n      <input type=\"text\" [ngModel]=\"draft().displayName\" (ngModelChange)=\"updateText('displayName', $event)\" name=\"schema-property-display-name\" />\n    </label>\n\n    <label class=\"schema-property-dialog__field\">\n      <span>{{ chill.T('A1803D67-C40D-41AF-BFD2-8F9B0E34C48B', 'Property type', 'Tipo propriet\u00E0') }}</span>\n      <select [ngModel]=\"draft().propertyType\" (ngModelChange)=\"updatePropertyType($event)\" name=\"schema-property-type\">\n        @for (option of propertyTypeOptions; track option.value) {\n          <option [ngValue]=\"option.value\" [disabled]=\"isPropertyTypeOptionDisabled(option.value)\">{{ option.label }}</option>\n        }\n      </select>\n    </label>\n\n    <label class=\"schema-property-dialog__field schema-property-dialog__field--toggle\">\n      <span>{{ chill.T('749AB7D9-6F87-488B-90A7-EB2CB0D9BA52', 'Nullable', 'Nullabile') }}</span>\n      <input type=\"checkbox\" [ngModel]=\"draft().isNullable\" (ngModelChange)=\"updateBoolean('isNullable', $event)\" name=\"schema-property-nullable\" />\n    </label>\n\n    <label class=\"schema-property-dialog__field schema-property-dialog__field--toggle\">\n      <span>{{ chill.T('7C94BA39-3A2B-4D04-9551-E4E7FB698D5D', 'Read only', 'Sola lettura') }}</span>\n      <input type=\"checkbox\" [ngModel]=\"draft().isReadOnly\" (ngModelChange)=\"updateBoolean('isReadOnly', $event)\" name=\"schema-property-readonly\" />\n    </label>\n\n    @if (showLengthSettings()) {\n      <label class=\"schema-property-dialog__field\">\n        <span>{{ chill.T('97CC1ABF-E541-4B39-91B3-1F907B3B73E4', 'Min length', 'Lunghezza minima') }}</span>\n        <input type=\"text\" [ngModel]=\"draft().minLength\" (ngModelChange)=\"updateText('minLength', $event)\" name=\"schema-property-min-length\" />\n      </label>\n\n      <label class=\"schema-property-dialog__field\">\n        <span>{{ chill.T('0D6433DA-7D03-469C-B175-872252D12B12', 'Max length', 'Lunghezza massima') }}</span>\n        <input type=\"text\" [ngModel]=\"draft().maxLength\" (ngModelChange)=\"updateText('maxLength', $event)\" name=\"schema-property-max-length\" />\n      </label>\n    }\n\n    @if (showRegexPattern()) {\n      <label class=\"schema-property-dialog__field schema-property-dialog__field--full\">\n        <span>{{ chill.T('375A5A93-8BDE-4731-91A4-CF0F2A6D52B5', 'Regex pattern', 'Pattern regex') }}</span>\n        <input type=\"text\" [ngModel]=\"draft().regexPattern\" (ngModelChange)=\"updateText('regexPattern', $event)\" name=\"schema-property-regex-pattern\" />\n      </label>\n    }\n\n    @if (showIntegerRange()) {\n      <label class=\"schema-property-dialog__field\">\n        <span>{{ chill.T('30BAA302-7E62-49B7-96F8-BE5A7E3A4D28', 'Integer min value', 'Valore intero minimo') }}</span>\n        <input type=\"text\" [ngModel]=\"draft().integerMinValue\" (ngModelChange)=\"updateText('integerMinValue', $event)\" name=\"schema-property-integer-min\" />\n      </label>\n\n      <label class=\"schema-property-dialog__field\">\n        <span>{{ chill.T('9DEFC702-E6D3-40D1-B8A8-B677A38AB707', 'Integer max value', 'Valore intero massimo') }}</span>\n        <input type=\"text\" [ngModel]=\"draft().integerMaxValue\" (ngModelChange)=\"updateText('integerMaxValue', $event)\" name=\"schema-property-integer-max\" />\n      </label>\n    }\n\n    @if (showDecimalSettings()) {\n      <label class=\"schema-property-dialog__field\">\n        <span>{{ chill.T('7D7A019B-B605-4B80-8AA2-BBEF519EAE72', 'Decimal min value', 'Valore decimale minimo') }}</span>\n        <input type=\"text\" [ngModel]=\"draft().decimalMinValue\" (ngModelChange)=\"updateText('decimalMinValue', $event)\" name=\"schema-property-decimal-min\" />\n      </label>\n\n      <label class=\"schema-property-dialog__field\">\n        <span>{{ chill.T('6B3A021A-4E64-48C5-9EF6-3043E414D83E', 'Decimal max value', 'Valore decimale massimo') }}</span>\n        <input type=\"text\" [ngModel]=\"draft().decimalMaxValue\" (ngModelChange)=\"updateText('decimalMaxValue', $event)\" name=\"schema-property-decimal-max\" />\n      </label>\n\n      <label class=\"schema-property-dialog__field\">\n        <span>{{ chill.T('8A948B20-50A2-4E3B-BF6A-DB13D9A4A4A5', 'Decimal places', 'Cifre decimali') }}</span>\n        <input type=\"text\" [ngModel]=\"draft().decimalPlaces\" (ngModelChange)=\"updateText('decimalPlaces', $event)\" name=\"schema-property-decimal-places\" />\n      </label>\n\n      <label class=\"schema-property-dialog__field\">\n        <span>{{ chill.T('B5F90B96-E2CF-4688-8CB1-1F22A4F6BCA0', 'Precision', 'Precisione') }}</span>\n        <input type=\"text\" [ngModel]=\"draft().precision\" (ngModelChange)=\"updateText('precision', $event)\" name=\"schema-property-precision\" />\n      </label>\n\n      <label class=\"schema-property-dialog__field\">\n        <span>{{ chill.T('65D01B52-5D8B-4540-8B76-5D155B8A50EA', 'Scale', 'Scala') }}</span>\n        <input type=\"text\" [ngModel]=\"draft().scale\" (ngModelChange)=\"updateText('scale', $event)\" name=\"schema-property-scale\" />\n      </label>\n    }\n\n    @if (showDateFormat()) {\n      <label class=\"schema-property-dialog__field\">\n        <span>{{ chill.T('9AB02B9F-6516-42D1-A8A8-9F5A977F31BB', 'Date format', 'Formato data') }}</span>\n        <input type=\"text\" list=\"schema-property-date-format-options\" [ngModel]=\"draft().dateFormat\" (ngModelChange)=\"updateText('dateFormat', $event)\" name=\"schema-property-date-format\" />\n        <datalist id=\"schema-property-date-format-options\">\n          @for (option of dateFormatOptions; track option[0]) {\n            <option [value]=\"option[0]\">{{ option[1] }}</option>\n          }\n        </datalist>\n      </label>\n    }\n\n    @if (showCustomFormat()) {\n      <label class=\"schema-property-dialog__field\">\n        <span>{{ chill.T('D7A9229B-2EFC-417D-B8E0-88A6C1B38106', 'Custom format', 'Formato personalizzato') }}</span>\n        <input type=\"text\" [ngModel]=\"draft().customFormat\" (ngModelChange)=\"updateText('customFormat', $event)\" name=\"schema-property-custom-format\" />\n      </label>\n    }\n\n    @if (showEnumValues()) {\n      <label class=\"schema-property-dialog__field schema-property-dialog__field--full\">\n        <span>{{ chill.T('5C131694-1FE7-4B91-861E-C331E8636409', 'Enum values', 'Valori enum') }}</span>\n        <textarea rows=\"6\" [ngModel]=\"draft().enumValues\" (ngModelChange)=\"updateText('enumValues', $event)\" name=\"schema-property-enum-values\" [placeholder]=\"enumValuesPlaceholder()\"></textarea>\n      </label>\n    }\n  </div>\n\n  <div class=\"schema-property-dialog__metadata\">\n    <div class=\"schema-property-dialog__metadata-header\">\n      <h3>{{ chill.T('01D8D2BD-F9B7-49F8-90EC-E96CFAB15811', 'Metadata', 'Metadata') }}</h3>\n      <p>{{ chill.T('ED433939-FE09-4B8D-92DB-67480D13F350', 'Dedicated fields override matching metadata keys when you save.', 'I campi dedicati sovrascrivono le chiavi metadata corrispondenti al salvataggio.') }}</p>\n    </div>\n\n    <app-chill-json-input\n      [value]=\"draft().metadataJson\"\n      [invalid]=\"metadataJsonInvalid()\"\n      [placeholder]=\"metadataPlaceholder()\"\n      (valueChange)=\"updateText('metadataJson', $event)\"></app-chill-json-input>\n  </div>\n</section>\n", styles: [":host{display:block}.schema-property-dialog{display:grid;gap:1.25rem}.schema-property-dialog__lede,.schema-property-dialog__context,.schema-property-dialog__metadata-header p{margin:0;color:var(--text-muted)}.schema-property-dialog__context strong{color:var(--text-main)}.schema-property-dialog__errors{display:grid;gap:.45rem;padding:.85rem 1rem;border:1px solid color-mix(in srgb,var(--danger) 55%,var(--border-color));border-radius:.8rem;background:color-mix(in srgb,var(--danger) 8%,var(--surface-2));color:var(--danger)}.schema-property-dialog__errors p,.schema-property-dialog__metadata-header h3{margin:0}.schema-property-dialog__grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:1rem}.schema-property-dialog__field{display:grid;gap:.4rem}.schema-property-dialog__field--full{grid-column:1/-1}.schema-property-dialog__field--toggle{align-content:end;grid-template-columns:1fr auto;align-items:center}.schema-property-dialog__field span,.schema-property-dialog__metadata-header h3{font-size:.9rem;font-weight:700;color:var(--text-main)}.schema-property-dialog__field input,.schema-property-dialog__field select,.schema-property-dialog__field textarea{width:100%;min-height:2.7rem;padding:.7rem .85rem;border:1px solid var(--border-color);border-radius:.8rem;background:var(--surface-0);color:var(--text-main);font:inherit}.schema-property-dialog__field textarea{min-height:6rem;resize:vertical}.schema-property-dialog__field--toggle input{width:1.05rem;height:1.05rem;min-height:0;padding:0;accent-color:var(--accent)}.schema-property-dialog__metadata{display:grid;gap:.75rem}.schema-property-dialog__metadata-header{display:grid;gap:.35rem}@media(max-width:720px){.schema-property-dialog__grid{grid-template-columns:1fr}.schema-property-dialog__field--full{grid-column:auto}}\n"], dependencies: [{ kind: "ngmodule", type: CommonModule }, { kind: "ngmodule", type: FormsModule }, { kind: "directive", type: i1.NgSelectOption, selector: "option", inputs: ["ngValue", "value"] }, { kind: "directive", type: i1.ɵNgSelectMultipleOption, selector: "option", inputs: ["ngValue", "value"] }, { kind: "directive", type: i1.DefaultValueAccessor, selector: "input:not([type=checkbox])[formControlName],textarea[formControlName],input:not([type=checkbox])[formControl],textarea[formControl],input:not([type=checkbox])[ngModel],textarea[ngModel],[ngDefaultControl]" }, { kind: "directive", type: i1.CheckboxControlValueAccessor, selector: "input[type=checkbox][formControlName],input[type=checkbox][formControl],input[type=checkbox][ngModel]" }, { kind: "directive", type: i1.SelectControlValueAccessor, selector: "select:not([multiple])[formControlName],select:not([multiple])[formControl],select:not([multiple])[ngModel]", inputs: ["compareWith"] }, { kind: "directive", type: i1.NgControlStatus, selector: "[formControlName],[ngModel],[formControl]" }, { kind: "directive", type: i1.NgModel, selector: "[ngModel]:not([formControlName]):not([formControl])", inputs: ["name", "disabled", "ngModel", "ngModelOptions"], outputs: ["ngModelChange"], exportAs: ["ngModel"] }, { kind: "component", type: ChillJsonInputComponent, selector: "app-chill-json-input", inputs: ["value", "placeholder", "invalid", "disabled", "language", "minHeight", "maxHeight", "mobileFullHeight"], outputs: ["valueChange", "blur"] }] }); }
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: SchemaPropertyDialogComponent, decorators: [{
            type: Component,
            args: [{ selector: 'app-schema-property-dialog', standalone: true, imports: [CommonModule, FormsModule, ChillJsonInputComponent], template: "<section class=\"schema-property-dialog\">\n  <p class=\"schema-property-dialog__lede\">\n    {{ chill.T(\n      'FF4665A2-D898-4CC5-B911-B90F3B44AC25',\n      'Edit the selected schema property and save the updated behavior.',\n      'Modifica la propriet\u00E0 di schema selezionata e salva il comportamento aggiornato.'\n    ) }}\n  </p>\n\n  @if (schemaTypeLabel()) {\n    <p class=\"schema-property-dialog__context\">\n      {{ chill.T('C797A0B9-9AB2-4E71-84DE-677416293548', 'Schema type', 'Tipo schema') }}:\n      <strong>{{ schemaTypeLabel() }}</strong>\n    </p>\n  }\n\n  @if (validationMessages().length > 0) {\n    <div class=\"schema-property-dialog__errors\">\n      @for (message of validationMessages(); track message) {\n        <p>{{ message }}</p>\n      }\n    </div>\n  }\n\n  <div class=\"schema-property-dialog__grid\">\n    <label class=\"schema-property-dialog__field\">\n      <span>{{ chill.T('57D9358A-8052-460F-8C0C-5B93B572E73A', 'Name', 'Nome') }}</span>\n      <input type=\"text\" [ngModel]=\"draft().name\" (ngModelChange)=\"updateText('name', $event)\" name=\"schema-property-name\" />\n    </label>\n\n    <label class=\"schema-property-dialog__field\">\n      <span>{{ chill.T('A276C7C0-E297-41EC-88D8-80C151BCA646', 'Display name', 'Nome visualizzato') }}</span>\n      <input type=\"text\" [ngModel]=\"draft().displayName\" (ngModelChange)=\"updateText('displayName', $event)\" name=\"schema-property-display-name\" />\n    </label>\n\n    <label class=\"schema-property-dialog__field\">\n      <span>{{ chill.T('A1803D67-C40D-41AF-BFD2-8F9B0E34C48B', 'Property type', 'Tipo propriet\u00E0') }}</span>\n      <select [ngModel]=\"draft().propertyType\" (ngModelChange)=\"updatePropertyType($event)\" name=\"schema-property-type\">\n        @for (option of propertyTypeOptions; track option.value) {\n          <option [ngValue]=\"option.value\" [disabled]=\"isPropertyTypeOptionDisabled(option.value)\">{{ option.label }}</option>\n        }\n      </select>\n    </label>\n\n    <label class=\"schema-property-dialog__field schema-property-dialog__field--toggle\">\n      <span>{{ chill.T('749AB7D9-6F87-488B-90A7-EB2CB0D9BA52', 'Nullable', 'Nullabile') }}</span>\n      <input type=\"checkbox\" [ngModel]=\"draft().isNullable\" (ngModelChange)=\"updateBoolean('isNullable', $event)\" name=\"schema-property-nullable\" />\n    </label>\n\n    <label class=\"schema-property-dialog__field schema-property-dialog__field--toggle\">\n      <span>{{ chill.T('7C94BA39-3A2B-4D04-9551-E4E7FB698D5D', 'Read only', 'Sola lettura') }}</span>\n      <input type=\"checkbox\" [ngModel]=\"draft().isReadOnly\" (ngModelChange)=\"updateBoolean('isReadOnly', $event)\" name=\"schema-property-readonly\" />\n    </label>\n\n    @if (showLengthSettings()) {\n      <label class=\"schema-property-dialog__field\">\n        <span>{{ chill.T('97CC1ABF-E541-4B39-91B3-1F907B3B73E4', 'Min length', 'Lunghezza minima') }}</span>\n        <input type=\"text\" [ngModel]=\"draft().minLength\" (ngModelChange)=\"updateText('minLength', $event)\" name=\"schema-property-min-length\" />\n      </label>\n\n      <label class=\"schema-property-dialog__field\">\n        <span>{{ chill.T('0D6433DA-7D03-469C-B175-872252D12B12', 'Max length', 'Lunghezza massima') }}</span>\n        <input type=\"text\" [ngModel]=\"draft().maxLength\" (ngModelChange)=\"updateText('maxLength', $event)\" name=\"schema-property-max-length\" />\n      </label>\n    }\n\n    @if (showRegexPattern()) {\n      <label class=\"schema-property-dialog__field schema-property-dialog__field--full\">\n        <span>{{ chill.T('375A5A93-8BDE-4731-91A4-CF0F2A6D52B5', 'Regex pattern', 'Pattern regex') }}</span>\n        <input type=\"text\" [ngModel]=\"draft().regexPattern\" (ngModelChange)=\"updateText('regexPattern', $event)\" name=\"schema-property-regex-pattern\" />\n      </label>\n    }\n\n    @if (showIntegerRange()) {\n      <label class=\"schema-property-dialog__field\">\n        <span>{{ chill.T('30BAA302-7E62-49B7-96F8-BE5A7E3A4D28', 'Integer min value', 'Valore intero minimo') }}</span>\n        <input type=\"text\" [ngModel]=\"draft().integerMinValue\" (ngModelChange)=\"updateText('integerMinValue', $event)\" name=\"schema-property-integer-min\" />\n      </label>\n\n      <label class=\"schema-property-dialog__field\">\n        <span>{{ chill.T('9DEFC702-E6D3-40D1-B8A8-B677A38AB707', 'Integer max value', 'Valore intero massimo') }}</span>\n        <input type=\"text\" [ngModel]=\"draft().integerMaxValue\" (ngModelChange)=\"updateText('integerMaxValue', $event)\" name=\"schema-property-integer-max\" />\n      </label>\n    }\n\n    @if (showDecimalSettings()) {\n      <label class=\"schema-property-dialog__field\">\n        <span>{{ chill.T('7D7A019B-B605-4B80-8AA2-BBEF519EAE72', 'Decimal min value', 'Valore decimale minimo') }}</span>\n        <input type=\"text\" [ngModel]=\"draft().decimalMinValue\" (ngModelChange)=\"updateText('decimalMinValue', $event)\" name=\"schema-property-decimal-min\" />\n      </label>\n\n      <label class=\"schema-property-dialog__field\">\n        <span>{{ chill.T('6B3A021A-4E64-48C5-9EF6-3043E414D83E', 'Decimal max value', 'Valore decimale massimo') }}</span>\n        <input type=\"text\" [ngModel]=\"draft().decimalMaxValue\" (ngModelChange)=\"updateText('decimalMaxValue', $event)\" name=\"schema-property-decimal-max\" />\n      </label>\n\n      <label class=\"schema-property-dialog__field\">\n        <span>{{ chill.T('8A948B20-50A2-4E3B-BF6A-DB13D9A4A4A5', 'Decimal places', 'Cifre decimali') }}</span>\n        <input type=\"text\" [ngModel]=\"draft().decimalPlaces\" (ngModelChange)=\"updateText('decimalPlaces', $event)\" name=\"schema-property-decimal-places\" />\n      </label>\n\n      <label class=\"schema-property-dialog__field\">\n        <span>{{ chill.T('B5F90B96-E2CF-4688-8CB1-1F22A4F6BCA0', 'Precision', 'Precisione') }}</span>\n        <input type=\"text\" [ngModel]=\"draft().precision\" (ngModelChange)=\"updateText('precision', $event)\" name=\"schema-property-precision\" />\n      </label>\n\n      <label class=\"schema-property-dialog__field\">\n        <span>{{ chill.T('65D01B52-5D8B-4540-8B76-5D155B8A50EA', 'Scale', 'Scala') }}</span>\n        <input type=\"text\" [ngModel]=\"draft().scale\" (ngModelChange)=\"updateText('scale', $event)\" name=\"schema-property-scale\" />\n      </label>\n    }\n\n    @if (showDateFormat()) {\n      <label class=\"schema-property-dialog__field\">\n        <span>{{ chill.T('9AB02B9F-6516-42D1-A8A8-9F5A977F31BB', 'Date format', 'Formato data') }}</span>\n        <input type=\"text\" list=\"schema-property-date-format-options\" [ngModel]=\"draft().dateFormat\" (ngModelChange)=\"updateText('dateFormat', $event)\" name=\"schema-property-date-format\" />\n        <datalist id=\"schema-property-date-format-options\">\n          @for (option of dateFormatOptions; track option[0]) {\n            <option [value]=\"option[0]\">{{ option[1] }}</option>\n          }\n        </datalist>\n      </label>\n    }\n\n    @if (showCustomFormat()) {\n      <label class=\"schema-property-dialog__field\">\n        <span>{{ chill.T('D7A9229B-2EFC-417D-B8E0-88A6C1B38106', 'Custom format', 'Formato personalizzato') }}</span>\n        <input type=\"text\" [ngModel]=\"draft().customFormat\" (ngModelChange)=\"updateText('customFormat', $event)\" name=\"schema-property-custom-format\" />\n      </label>\n    }\n\n    @if (showEnumValues()) {\n      <label class=\"schema-property-dialog__field schema-property-dialog__field--full\">\n        <span>{{ chill.T('5C131694-1FE7-4B91-861E-C331E8636409', 'Enum values', 'Valori enum') }}</span>\n        <textarea rows=\"6\" [ngModel]=\"draft().enumValues\" (ngModelChange)=\"updateText('enumValues', $event)\" name=\"schema-property-enum-values\" [placeholder]=\"enumValuesPlaceholder()\"></textarea>\n      </label>\n    }\n  </div>\n\n  <div class=\"schema-property-dialog__metadata\">\n    <div class=\"schema-property-dialog__metadata-header\">\n      <h3>{{ chill.T('01D8D2BD-F9B7-49F8-90EC-E96CFAB15811', 'Metadata', 'Metadata') }}</h3>\n      <p>{{ chill.T('ED433939-FE09-4B8D-92DB-67480D13F350', 'Dedicated fields override matching metadata keys when you save.', 'I campi dedicati sovrascrivono le chiavi metadata corrispondenti al salvataggio.') }}</p>\n    </div>\n\n    <app-chill-json-input\n      [value]=\"draft().metadataJson\"\n      [invalid]=\"metadataJsonInvalid()\"\n      [placeholder]=\"metadataPlaceholder()\"\n      (valueChange)=\"updateText('metadataJson', $event)\"></app-chill-json-input>\n  </div>\n</section>\n", styles: [":host{display:block}.schema-property-dialog{display:grid;gap:1.25rem}.schema-property-dialog__lede,.schema-property-dialog__context,.schema-property-dialog__metadata-header p{margin:0;color:var(--text-muted)}.schema-property-dialog__context strong{color:var(--text-main)}.schema-property-dialog__errors{display:grid;gap:.45rem;padding:.85rem 1rem;border:1px solid color-mix(in srgb,var(--danger) 55%,var(--border-color));border-radius:.8rem;background:color-mix(in srgb,var(--danger) 8%,var(--surface-2));color:var(--danger)}.schema-property-dialog__errors p,.schema-property-dialog__metadata-header h3{margin:0}.schema-property-dialog__grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:1rem}.schema-property-dialog__field{display:grid;gap:.4rem}.schema-property-dialog__field--full{grid-column:1/-1}.schema-property-dialog__field--toggle{align-content:end;grid-template-columns:1fr auto;align-items:center}.schema-property-dialog__field span,.schema-property-dialog__metadata-header h3{font-size:.9rem;font-weight:700;color:var(--text-main)}.schema-property-dialog__field input,.schema-property-dialog__field select,.schema-property-dialog__field textarea{width:100%;min-height:2.7rem;padding:.7rem .85rem;border:1px solid var(--border-color);border-radius:.8rem;background:var(--surface-0);color:var(--text-main);font:inherit}.schema-property-dialog__field textarea{min-height:6rem;resize:vertical}.schema-property-dialog__field--toggle input{width:1.05rem;height:1.05rem;min-height:0;padding:0;accent-color:var(--accent)}.schema-property-dialog__metadata{display:grid;gap:.75rem}.schema-property-dialog__metadata-header{display:grid;gap:.35rem}@media(max-width:720px){.schema-property-dialog__grid{grid-template-columns:1fr}.schema-property-dialog__field--full{grid-column:auto}}\n"] }]
        }], ctorParameters: () => [] });

var schemaPropertyDialog_component = /*#__PURE__*/Object.freeze({
    __proto__: null,
    SchemaPropertyDialogComponent: SchemaPropertyDialogComponent
});

class AttachmentUploadDialogComponent {
    constructor() {
        this.chill = inject(ChillService);
        this.dialog = inject(WorkspaceDialogService);
        this.attachToChillType = input('');
        this.attachToGuid = input('');
        this.form = new FormGroup({
            title: new FormControl('', { nonNullable: true }),
            description: new FormControl('', { nonNullable: true }),
            isPublic: new FormControl(false, { nonNullable: true })
        });
        this.selectedFile = signal(null);
        this.selectedFileName = signal('');
    }
    canDialogSubmit() {
        return !!this.selectedFile() && !!this.attachToChillType().trim() && !!this.attachToGuid().trim();
    }
    async submit() {
        const file = this.selectedFile();
        const attachToChillType = this.attachToChillType().trim();
        const attachToGuid = this.attachToGuid().trim();
        if (!file || !attachToChillType || !attachToGuid) {
            return;
        }
        const uploaded = await firstValueFrom(this.chill.uploadAttachment({
            chillType: attachToChillType,
            guid: attachToGuid
        }, {
            fileName: file.name,
            content: file,
            contentType: file.type || undefined
        }, {
            title: this.form.controls.title.value.trim() || null,
            description: this.form.controls.description.value.trim() || null,
            isPublic: this.form.controls.isPublic.value
        }));
        this.dialog.confirm(uploaded[0] ?? null);
    }
    onFileSelected(event) {
        const input = event.target;
        const file = input?.files?.[0] ?? null;
        this.selectedFile.set(file);
        this.selectedFileName.set(file?.name ?? '');
        if (file && !this.form.controls.title.value.trim()) {
            const fileName = file.name.replace(/\.[^.]+$/, '').trim();
            this.form.controls.title.setValue(fileName || file.name);
        }
    }
    static { this.ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: AttachmentUploadDialogComponent, deps: [], target: i0.ɵɵFactoryTarget.Component }); }
    static { this.ɵcmp = i0.ɵɵngDeclareComponent({ minVersion: "17.0.0", version: "19.2.21", type: AttachmentUploadDialogComponent, isStandalone: true, selector: "app-attachment-upload-dialog", inputs: { attachToChillType: { classPropertyName: "attachToChillType", publicName: "attachToChillType", isSignal: true, isRequired: false, transformFunction: null }, attachToGuid: { classPropertyName: "attachToGuid", publicName: "attachToGuid", isSignal: true, isRequired: false, transformFunction: null } }, ngImport: i0, template: `
    <section class="attachment-upload-dialog">
      <p class="attachment-upload-dialog__lede">
        {{ chill.T(
          '47A55AC3-9013-4A7E-81C4-E549EFE248E8',
          'Upload one attachment for the selected entity.',
          'Carica un allegato per l entita selezionata.'
        ) }}
      </p>

      <p class="attachment-upload-dialog__target">
        <strong>{{ attachToChillType() }}</strong>
        <span>{{ attachToGuid() }}</span>
      </p>

      <label class="attachment-upload-dialog__field">
        <span>{{ chill.T('E9E11821-898B-449D-BF17-E7136CB737D9', 'File', 'File') }}</span>
        <input type="file" (change)="onFileSelected($event)" />
      </label>

      @if (selectedFileName()) {
        <p class="attachment-upload-dialog__file">{{ selectedFileName() }}</p>
      }

      <form [formGroup]="form" class="attachment-upload-dialog__form">
        <label class="attachment-upload-dialog__field">
          <span>{{ chill.T('92C31A96-D747-490D-8A4D-2175C181E80C', 'Title', 'Titolo') }}</span>
          <input type="text" formControlName="title" />
        </label>

        <label class="attachment-upload-dialog__field">
          <span>{{ chill.T('D679D4D4-FB4E-474B-848C-5BBBE38A4F0C', 'Description', 'Descrizione') }}</span>
          <textarea rows="4" formControlName="description"></textarea>
        </label>

        <label class="attachment-upload-dialog__checkbox">
          <input type="checkbox" formControlName="isPublic" />
          <span>{{ chill.T('BC4B1775-3937-4B56-9EF2-A4F1962A5AF7', 'Public', 'Pubblico') }}</span>
        </label>
      </form>
    </section>
  `, isInline: true, styles: [":host{display:block}.attachment-upload-dialog{display:grid;gap:1rem}.attachment-upload-dialog__lede,.attachment-upload-dialog__target,.attachment-upload-dialog__file{margin:0}.attachment-upload-dialog__lede,.attachment-upload-dialog__target span,.attachment-upload-dialog__file{color:var(--text-muted)}.attachment-upload-dialog__target{display:grid;gap:.25rem}.attachment-upload-dialog__target strong{color:var(--text-main)}.attachment-upload-dialog__form{display:grid;gap:1rem}.attachment-upload-dialog__field{display:grid;gap:.45rem}.attachment-upload-dialog__field input,.attachment-upload-dialog__field textarea{width:100%;padding:.75rem .85rem;border:1px solid var(--border-color);border-radius:.75rem;background:var(--surface-0);color:var(--text-main);font:inherit}.attachment-upload-dialog__checkbox{display:inline-flex;align-items:center;gap:.6rem}\n"], dependencies: [{ kind: "ngmodule", type: CommonModule }, { kind: "ngmodule", type: ReactiveFormsModule }, { kind: "directive", type: i1.ɵNgNoValidate, selector: "form:not([ngNoForm]):not([ngNativeValidate])" }, { kind: "directive", type: i1.DefaultValueAccessor, selector: "input:not([type=checkbox])[formControlName],textarea[formControlName],input:not([type=checkbox])[formControl],textarea[formControl],input:not([type=checkbox])[ngModel],textarea[ngModel],[ngDefaultControl]" }, { kind: "directive", type: i1.CheckboxControlValueAccessor, selector: "input[type=checkbox][formControlName],input[type=checkbox][formControl],input[type=checkbox][ngModel]" }, { kind: "directive", type: i1.NgControlStatus, selector: "[formControlName],[ngModel],[formControl]" }, { kind: "directive", type: i1.NgControlStatusGroup, selector: "[formGroupName],[formArrayName],[ngModelGroup],[formGroup],form:not([ngNoForm]),[ngForm]" }, { kind: "directive", type: i1.FormGroupDirective, selector: "[formGroup]", inputs: ["formGroup"], outputs: ["ngSubmit"], exportAs: ["ngForm"] }, { kind: "directive", type: i1.FormControlName, selector: "[formControlName]", inputs: ["formControlName", "disabled", "ngModel"], outputs: ["ngModelChange"] }] }); }
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: AttachmentUploadDialogComponent, decorators: [{
            type: Component,
            args: [{ selector: 'app-attachment-upload-dialog', standalone: true, imports: [CommonModule, ReactiveFormsModule], template: `
    <section class="attachment-upload-dialog">
      <p class="attachment-upload-dialog__lede">
        {{ chill.T(
          '47A55AC3-9013-4A7E-81C4-E549EFE248E8',
          'Upload one attachment for the selected entity.',
          'Carica un allegato per l entita selezionata.'
        ) }}
      </p>

      <p class="attachment-upload-dialog__target">
        <strong>{{ attachToChillType() }}</strong>
        <span>{{ attachToGuid() }}</span>
      </p>

      <label class="attachment-upload-dialog__field">
        <span>{{ chill.T('E9E11821-898B-449D-BF17-E7136CB737D9', 'File', 'File') }}</span>
        <input type="file" (change)="onFileSelected($event)" />
      </label>

      @if (selectedFileName()) {
        <p class="attachment-upload-dialog__file">{{ selectedFileName() }}</p>
      }

      <form [formGroup]="form" class="attachment-upload-dialog__form">
        <label class="attachment-upload-dialog__field">
          <span>{{ chill.T('92C31A96-D747-490D-8A4D-2175C181E80C', 'Title', 'Titolo') }}</span>
          <input type="text" formControlName="title" />
        </label>

        <label class="attachment-upload-dialog__field">
          <span>{{ chill.T('D679D4D4-FB4E-474B-848C-5BBBE38A4F0C', 'Description', 'Descrizione') }}</span>
          <textarea rows="4" formControlName="description"></textarea>
        </label>

        <label class="attachment-upload-dialog__checkbox">
          <input type="checkbox" formControlName="isPublic" />
          <span>{{ chill.T('BC4B1775-3937-4B56-9EF2-A4F1962A5AF7', 'Public', 'Pubblico') }}</span>
        </label>
      </form>
    </section>
  `, styles: [":host{display:block}.attachment-upload-dialog{display:grid;gap:1rem}.attachment-upload-dialog__lede,.attachment-upload-dialog__target,.attachment-upload-dialog__file{margin:0}.attachment-upload-dialog__lede,.attachment-upload-dialog__target span,.attachment-upload-dialog__file{color:var(--text-muted)}.attachment-upload-dialog__target{display:grid;gap:.25rem}.attachment-upload-dialog__target strong{color:var(--text-main)}.attachment-upload-dialog__form{display:grid;gap:1rem}.attachment-upload-dialog__field{display:grid;gap:.45rem}.attachment-upload-dialog__field input,.attachment-upload-dialog__field textarea{width:100%;padding:.75rem .85rem;border:1px solid var(--border-color);border-radius:.75rem;background:var(--surface-0);color:var(--text-main);font:inherit}.attachment-upload-dialog__checkbox{display:inline-flex;align-items:center;gap:.6rem}\n"] }]
        }] });

var attachmentUploadDialog_component = /*#__PURE__*/Object.freeze({
    __proto__: null,
    AttachmentUploadDialogComponent: AttachmentUploadDialogComponent
});

class AuthRoleDialogComponent {
    constructor() {
        this.chill = inject(ChillService);
        this.dialog = inject(WorkspaceDialogService);
        this.roleGuid = input('');
        this.isLoading = signal(false);
        this.isValid = signal(true);
        this.loadError = signal('');
        this.isEditMode = computed(() => !!this.roleGuid().trim());
        this.schema = computed(() => ({
            chillType: 'Auth.Role',
            chillViewCode: 'dialog',
            displayName: this.isEditMode()
                ? this.chill.T('6E9A69C0-C4A1-433A-97BC-9E8D1CBD2B53', 'Edit', 'Modifica')
                : this.chill.T('0B47EAA4-33BC-4D1C-B8C6-F75D3A5C8864', 'Create role', 'Crea ruolo'),
            metadata: {},
            properties: this.properties
        }));
        this.form = new FormGroup({
            name: new FormControl('', { nonNullable: true }),
            description: new FormControl('', { nonNullable: true }),
            isActive: new FormControl(true, { nonNullable: true })
        });
        this.properties = [
            {
                name: 'name',
                displayName: this.chill.T('7767C44A-8F47-4E39-BB2A-0B297887A0D3', 'Name', 'Nome'),
                propertyType: CHILL_PROPERTY_TYPE$1.String,
                isNullable: false,
                metadata: { required: 'true', maxLength: '255' }
            },
            {
                name: 'description',
                displayName: this.chill.T('97A7BFE7-22A7-4665-B0D8-C75506A8F794', 'Description', 'Descrizione'),
                propertyType: CHILL_PROPERTY_TYPE$1.Text,
                isNullable: true,
                metadata: { maxLength: '1000', multiline: 'true' }
            },
            {
                name: 'isActive',
                displayName: this.chill.T('7A2D49F9-9A14-4FD1-8A8B-9B604CA3796C', 'Role is active', 'Ruolo attivo'),
                propertyType: CHILL_PROPERTY_TYPE$1.Boolean,
                isNullable: false,
                metadata: {}
            }
        ];
        effect(() => {
            const roleGuid = this.roleGuid().trim();
            this.loadError.set('');
            if (!roleGuid) {
                this.isLoading.set(false);
                this.populateForm({
                    name: '',
                    description: '',
                    isActive: true
                });
                return;
            }
            this.isLoading.set(true);
            void this.loadRole(roleGuid);
        });
    }
    canDialogSubmit() {
        return !this.isLoading() && !this.loadError() && this.isValid();
    }
    async submit() {
        if (!this.canDialogSubmit()) {
            return;
        }
        const roleGuid = this.roleGuid().trim();
        const payload = this.readPayload();
        const savedRole = roleGuid
            ? await firstValueFrom(this.chill.updateAuthRole(roleGuid, payload))
            : await firstValueFrom(this.chill.createAuthRole(payload));
        this.dialog.confirm(savedRole);
    }
    async loadRole(roleGuid) {
        try {
            const role = await firstValueFrom(this.chill.getAuthRoleAccess(roleGuid));
            this.populateForm({
                name: role.role.name,
                description: role.role.description,
                isActive: role.role.isActive
            });
        }
        catch (error) {
            this.loadError.set(this.chill.formatError(error));
        }
        finally {
            this.isLoading.set(false);
        }
    }
    populateForm(role) {
        this.form.controls['name'].setValue(role.name);
        this.form.controls['description'].setValue(role.description);
        this.form.controls['isActive'].setValue(role.isActive);
    }
    readPayload() {
        return {
            name: this.readString('name'),
            description: this.readString('description'),
            isActive: this.form.controls['isActive'].value === true
        };
    }
    readString(controlName) {
        const value = this.form.controls[controlName].value;
        return typeof value === 'string' ? value.trim() : '';
    }
    static { this.ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: AuthRoleDialogComponent, deps: [], target: i0.ɵɵFactoryTarget.Component }); }
    static { this.ɵcmp = i0.ɵɵngDeclareComponent({ minVersion: "17.0.0", version: "19.2.21", type: AuthRoleDialogComponent, isStandalone: true, selector: "app-auth-role-dialog", inputs: { roleGuid: { classPropertyName: "roleGuid", publicName: "roleGuid", isSignal: true, isRequired: false, transformFunction: null } }, ngImport: i0, template: `
    <section class="auth-entity-dialog">
      <p class="auth-entity-dialog__lede">
        {{ isEditMode()
          ? chill.T('65FB65D7-7D87-4661-9AD4-0FE7384983E7', 'Update role details and save the changes.', 'Aggiorna i dettagli ruolo e salva le modifiche.')
          : chill.T('C1264E7A-11D0-44E2-9026-2A1AA6F9AF82', 'Create a new role and use it immediately in permission assignments.', 'Crea un nuovo ruolo e usalo subito nelle assegnazioni dei permessi.') }}
      </p>

      @if (loadError()) {
        <p class="auth-entity-dialog__message auth-entity-dialog__message--error">{{ loadError() }}</p>
      } @else if (isLoading()) {
        <p class="auth-entity-dialog__message">
          {{ chill.T('39994747-E0B9-4404-BF92-CB98BA434832', 'Loading role details...', 'Caricamento dettagli ruolo...') }}
        </p>
      } @else {
        <app-chill-polymorphic-input
          [form]="form"
          [schema]="schema()"
          [showLabels]="true"
          (validityChange)="isValid.set($event)"></app-chill-polymorphic-input>
      }
    </section>
  `, isInline: true, styles: [":host{display:block}.auth-entity-dialog{display:grid;gap:1rem}.auth-entity-dialog__lede,.auth-entity-dialog__message{margin:0;color:var(--text-muted)}.auth-entity-dialog__message--error{color:var(--danger)}\n"], dependencies: [{ kind: "ngmodule", type: CommonModule }, { kind: "ngmodule", type: ReactiveFormsModule }, { kind: "component", type: ChillPolymorphicInputComponent, selector: "app-chill-polymorphic-input", inputs: ["form", "schema", "propertyNames", "readonlyPropertyNames", "externalErrors", "showLabels"], outputs: ["valueChange", "validityChange", "fieldBlur", "lookupDialogOpenChange", "editorDialogOpenChange"] }] }); }
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: AuthRoleDialogComponent, decorators: [{
            type: Component,
            args: [{ selector: 'app-auth-role-dialog', standalone: true, imports: [CommonModule, ReactiveFormsModule, ChillPolymorphicInputComponent], template: `
    <section class="auth-entity-dialog">
      <p class="auth-entity-dialog__lede">
        {{ isEditMode()
          ? chill.T('65FB65D7-7D87-4661-9AD4-0FE7384983E7', 'Update role details and save the changes.', 'Aggiorna i dettagli ruolo e salva le modifiche.')
          : chill.T('C1264E7A-11D0-44E2-9026-2A1AA6F9AF82', 'Create a new role and use it immediately in permission assignments.', 'Crea un nuovo ruolo e usalo subito nelle assegnazioni dei permessi.') }}
      </p>

      @if (loadError()) {
        <p class="auth-entity-dialog__message auth-entity-dialog__message--error">{{ loadError() }}</p>
      } @else if (isLoading()) {
        <p class="auth-entity-dialog__message">
          {{ chill.T('39994747-E0B9-4404-BF92-CB98BA434832', 'Loading role details...', 'Caricamento dettagli ruolo...') }}
        </p>
      } @else {
        <app-chill-polymorphic-input
          [form]="form"
          [schema]="schema()"
          [showLabels]="true"
          (validityChange)="isValid.set($event)"></app-chill-polymorphic-input>
      }
    </section>
  `, styles: [":host{display:block}.auth-entity-dialog{display:grid;gap:1rem}.auth-entity-dialog__lede,.auth-entity-dialog__message{margin:0;color:var(--text-muted)}.auth-entity-dialog__message--error{color:var(--danger)}\n"] }]
        }], ctorParameters: () => [] });

var authRoleDialog_component = /*#__PURE__*/Object.freeze({
    __proto__: null,
    AuthRoleDialogComponent: AuthRoleDialogComponent
});

class AuthUserDialogComponent {
    constructor() {
        this.chill = inject(ChillService);
        this.dialog = inject(WorkspaceDialogService);
        this.userGuid = input('');
        this.isLoading = signal(false);
        this.isValid = signal(true);
        this.loadError = signal('');
        this.isEditMode = computed(() => !!this.userGuid().trim());
        this.schema = computed(() => ({
            chillType: 'Auth.User',
            chillViewCode: 'dialog',
            displayName: this.isEditMode()
                ? this.chill.T('C082531D-0F50-49D4-B677-C752D1A4DAA4', 'Edit user', 'Modifica utente')
                : this.chill.T('9E2BFF8D-BF6C-4C8D-BE6A-972425BA63DB', 'New user', 'Nuovo utente'),
            metadata: {},
            properties: this.properties
        }));
        this.form = new FormGroup({
            externalId: new FormControl('', { nonNullable: true }),
            userName: new FormControl('', { nonNullable: true }),
            displayName: new FormControl('', { nonNullable: true }),
            displayCultureName: new FormControl('', { nonNullable: true }),
            displayTimeZone: new FormControl('', { nonNullable: true }),
            displayDateFormat: new FormControl('', { nonNullable: true }),
            displayNumberFormat: new FormControl('', { nonNullable: true }),
            isActive: new FormControl(true, { nonNullable: true }),
            canManagePermissions: new FormControl(false, { nonNullable: true }),
            canManageSchema: new FormControl(false, { nonNullable: true }),
            menuHierarchy: new FormControl('', { nonNullable: true })
        });
        this.cultureNameOptions = getCultureNameOptions();
        this.dateFormatOptions = getDateFormatOptions();
        this.timeZoneOptions = getIanaTimeZoneOptions();
        this.properties = [
            {
                name: 'userName',
                displayName: this.chill.T('2AF5EB08-932E-4D4D-9338-75E1808B5F16', 'Username', 'Nome utente'),
                propertyType: CHILL_PROPERTY_TYPE$1.String,
                isNullable: false,
                metadata: { required: 'true', maxLength: '255' }
            },
            {
                name: 'displayName',
                displayName: this.chill.T('C0D8A063-E084-460D-BF83-BCE32CB68588', 'Display name', 'Nome visualizzato'),
                propertyType: CHILL_PROPERTY_TYPE$1.String,
                isNullable: false,
                metadata: { required: 'true', maxLength: '255' }
            },
            {
                name: 'externalId',
                displayName: this.chill.T('12FB2D99-C4BF-4F0A-9FD5-9C10AFB5B38A', 'External id', 'Id esterno'),
                propertyType: CHILL_PROPERTY_TYPE$1.String,
                isNullable: true,
                metadata: { maxLength: '255' }
            },
            {
                name: 'displayCultureName',
                displayName: this.chill.T('771A6B48-7330-4852-A6B5-5BD314EC5662', 'Culture', 'Cultura'),
                propertyType: CHILL_PROPERTY_TYPE$1.Select,
                isNullable: false,
                metadata: {
                    required: 'true',
                    options: this.cultureNameOptions
                }
            },
            {
                name: 'displayTimeZone',
                displayName: this.chill.T('98E424F1-0183-4D9E-9B69-CDB16EBD41CF', 'Time zone', 'Fuso orario'),
                propertyType: CHILL_PROPERTY_TYPE$1.Select,
                isNullable: false,
                metadata: {
                    required: 'true',
                    options: this.timeZoneOptions
                }
            },
            {
                name: 'displayDateFormat',
                displayName: this.chill.T('49988673-8DBD-4C2B-9430-DA3054F0E294', 'Date format', 'Formato data'),
                propertyType: CHILL_PROPERTY_TYPE$1.Select,
                isNullable: false,
                metadata: {
                    required: 'true',
                    options: this.dateFormatOptions
                }
            },
            {
                name: 'displayNumberFormat',
                displayName: this.chill.T('22A3BF58-7889-4AD7-A9EF-06B6A69A8D3C', 'Number format', 'Formato numerico'),
                propertyType: CHILL_PROPERTY_TYPE$1.String,
                isNullable: false,
                metadata: { required: 'true', maxLength: '64' }
            },
            {
                name: 'menuHierarchy',
                displayName: this.chill.T('D133779C-B96F-4DB8-9F4B-7FE8874228C9', 'Menu hierarchy', 'Gerarchia menu'),
                propertyType: CHILL_PROPERTY_TYPE$1.String,
                isNullable: true,
                metadata: { maxLength: '255' }
            },
            {
                name: 'isActive',
                displayName: this.chill.T('8159D7BE-FBAA-44EB-9B41-A72B5F38F34C', 'User is active', 'Utente attivo'),
                propertyType: CHILL_PROPERTY_TYPE$1.Boolean,
                isNullable: false,
                metadata: {}
            },
            {
                name: 'canManagePermissions',
                displayName: this.chill.T('3E834972-E367-492B-9C2E-14CFEDB3607E', 'Can manage permissions', 'Può gestire i permessi'),
                propertyType: CHILL_PROPERTY_TYPE$1.Boolean,
                isNullable: false,
                metadata: {}
            },
            {
                name: 'canManageSchema',
                displayName: this.chill.T('5BCDD500-C65B-45D7-8B5A-EC0D9B8B82DE', 'Can manage schema', 'Può gestire lo schema'),
                propertyType: CHILL_PROPERTY_TYPE$1.Boolean,
                isNullable: false,
                metadata: {}
            }
        ];
        effect(() => {
            const userGuid = this.userGuid().trim();
            this.loadError.set('');
            if (!userGuid) {
                this.isLoading.set(false);
                this.populateForm(null);
                return;
            }
            this.isLoading.set(true);
            void this.loadUser(userGuid);
        });
    }
    canDialogSubmit() {
        return !this.isLoading() && !this.loadError() && this.isValid();
    }
    async submit() {
        if (!this.canDialogSubmit()) {
            return;
        }
        const userGuid = this.userGuid().trim();
        const payload = this.readPayload();
        const savedUser = userGuid
            ? await firstValueFrom(this.chill.updateAuthUser(userGuid, payload))
            : await firstValueFrom(this.chill.createAuthUser(payload));
        this.dialog.confirm(savedUser);
    }
    async loadUser(userGuid) {
        try {
            const user = await firstValueFrom(this.chill.getAuthUserDetails(userGuid));
            this.populateForm(user);
        }
        catch (error) {
            this.loadError.set(this.chill.formatError(error));
        }
        finally {
            this.isLoading.set(false);
        }
    }
    populateForm(user) {
        this.form.controls['externalId'].setValue(user?.externalId ?? '');
        this.form.controls['userName'].setValue(user?.userName ?? '');
        this.form.controls['displayName'].setValue(user?.displayName ?? '');
        this.form.controls['displayCultureName'].setValue(user?.displayCultureName ?? this.readBrowserCultureName());
        this.form.controls['displayTimeZone'].setValue(user?.displayTimeZone ?? this.readBrowserTimeZone());
        this.form.controls['displayDateFormat'].setValue(user?.displayDateFormat ?? 'dd/MM/yyyy');
        this.form.controls['displayNumberFormat'].setValue(user?.displayNumberFormat ?? 'it-IT');
        this.form.controls['isActive'].setValue(user?.isActive ?? true);
        this.form.controls['canManagePermissions'].setValue(user?.canManagePermissions ?? false);
        this.form.controls['canManageSchema'].setValue(user?.canManageSchema ?? false);
        this.form.controls['menuHierarchy'].setValue(user?.menuHierarchy ?? '');
    }
    readPayload() {
        return {
            externalId: this.readString('externalId'),
            userName: this.readString('userName'),
            displayName: this.readString('displayName'),
            displayCultureName: this.readString('displayCultureName'),
            displayTimeZone: this.readString('displayTimeZone'),
            displayDateFormat: this.readString('displayDateFormat'),
            displayNumberFormat: this.readString('displayNumberFormat'),
            isActive: this.readBoolean('isActive'),
            canManagePermissions: this.readBoolean('canManagePermissions'),
            canManageSchema: this.readBoolean('canManageSchema'),
            menuHierarchy: this.readString('menuHierarchy')
        };
    }
    readString(controlName) {
        const value = this.form.controls[controlName].value;
        return typeof value === 'string' ? value.trim() : '';
    }
    readBoolean(controlName) {
        return this.form.controls[controlName].value === true;
    }
    readBrowserCultureName() {
        const languages = globalThis.navigator?.languages;
        const browserCultureName = languages?.find((language) => typeof language === 'string' && language.trim())
            ?? globalThis.navigator?.language
            ?? '';
        return browserCultureName.trim() || 'it-IT';
    }
    readBrowserTimeZone() {
        return Intl.DateTimeFormat().resolvedOptions().timeZone || 'UTC';
    }
    static { this.ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: AuthUserDialogComponent, deps: [], target: i0.ɵɵFactoryTarget.Component }); }
    static { this.ɵcmp = i0.ɵɵngDeclareComponent({ minVersion: "17.0.0", version: "19.2.21", type: AuthUserDialogComponent, isStandalone: true, selector: "app-auth-user-dialog", inputs: { userGuid: { classPropertyName: "userGuid", publicName: "userGuid", isSignal: true, isRequired: false, transformFunction: null } }, ngImport: i0, template: `
    <section class="auth-entity-dialog">
      <p class="auth-entity-dialog__lede">
        {{ isEditMode()
          ? chill.T('1D88DB28-33B4-4D07-97D0-3BD2D8DDF1D0', 'Update user details and save the changes.', 'Aggiorna i dettagli utente e salva le modifiche.')
          : chill.T('104D3BC3-A842-4929-BD05-EAAE482900AD', 'Create a new user and make it immediately available for permission editing.', 'Crea un nuovo utente e rendilo subito disponibile per la modifica dei permessi.') }}
      </p>

      @if (loadError()) {
        <p class="auth-entity-dialog__message auth-entity-dialog__message--error">{{ loadError() }}</p>
      } @else if (isLoading()) {
        <p class="auth-entity-dialog__message">
          {{ chill.T('B80FE0B4-3BB7-4A7E-B9EB-1A456E3A8F68', 'Loading user details...', 'Caricamento dettagli utente...') }}
        </p>
      } @else {
        <app-chill-polymorphic-input
          [form]="form"
          [schema]="schema()"
          [showLabels]="true"
          (validityChange)="isValid.set($event)"></app-chill-polymorphic-input>
      }
    </section>
  `, isInline: true, styles: [":host{display:block}.auth-entity-dialog{display:grid;gap:1rem}.auth-entity-dialog__lede,.auth-entity-dialog__message{margin:0;color:var(--text-muted)}.auth-entity-dialog__message--error{color:var(--danger)}\n"], dependencies: [{ kind: "ngmodule", type: CommonModule }, { kind: "ngmodule", type: ReactiveFormsModule }, { kind: "component", type: ChillPolymorphicInputComponent, selector: "app-chill-polymorphic-input", inputs: ["form", "schema", "propertyNames", "readonlyPropertyNames", "externalErrors", "showLabels"], outputs: ["valueChange", "validityChange", "fieldBlur", "lookupDialogOpenChange", "editorDialogOpenChange"] }] }); }
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: AuthUserDialogComponent, decorators: [{
            type: Component,
            args: [{ selector: 'app-auth-user-dialog', standalone: true, imports: [CommonModule, ReactiveFormsModule, ChillPolymorphicInputComponent], template: `
    <section class="auth-entity-dialog">
      <p class="auth-entity-dialog__lede">
        {{ isEditMode()
          ? chill.T('1D88DB28-33B4-4D07-97D0-3BD2D8DDF1D0', 'Update user details and save the changes.', 'Aggiorna i dettagli utente e salva le modifiche.')
          : chill.T('104D3BC3-A842-4929-BD05-EAAE482900AD', 'Create a new user and make it immediately available for permission editing.', 'Crea un nuovo utente e rendilo subito disponibile per la modifica dei permessi.') }}
      </p>

      @if (loadError()) {
        <p class="auth-entity-dialog__message auth-entity-dialog__message--error">{{ loadError() }}</p>
      } @else if (isLoading()) {
        <p class="auth-entity-dialog__message">
          {{ chill.T('B80FE0B4-3BB7-4A7E-B9EB-1A456E3A8F68', 'Loading user details...', 'Caricamento dettagli utente...') }}
        </p>
      } @else {
        <app-chill-polymorphic-input
          [form]="form"
          [schema]="schema()"
          [showLabels]="true"
          (validityChange)="isValid.set($event)"></app-chill-polymorphic-input>
      }
    </section>
  `, styles: [":host{display:block}.auth-entity-dialog{display:grid;gap:1rem}.auth-entity-dialog__lede,.auth-entity-dialog__message{margin:0;color:var(--text-muted)}.auth-entity-dialog__message--error{color:var(--danger)}\n"] }]
        }], ctorParameters: () => [] });

var authUserDialog_component = /*#__PURE__*/Object.freeze({
    __proto__: null,
    AuthUserDialogComponent: AuthUserDialogComponent
});

class ConfirmMessageDialogComponent {
    constructor() {
        this.dialog = inject(WorkspaceDialogService);
        this.description = input('');
        this.buttons = input([]);
    }
    select(value) {
        this.dialog.confirm(value);
    }
    static { this.ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: ConfirmMessageDialogComponent, deps: [], target: i0.ɵɵFactoryTarget.Component }); }
    static { this.ɵcmp = i0.ɵɵngDeclareComponent({ minVersion: "17.0.0", version: "19.2.21", type: ConfirmMessageDialogComponent, isStandalone: true, selector: "app-confirm-message-dialog", inputs: { description: { classPropertyName: "description", publicName: "description", isSignal: true, isRequired: false, transformFunction: null }, buttons: { classPropertyName: "buttons", publicName: "buttons", isSignal: true, isRequired: false, transformFunction: null } }, ngImport: i0, template: `
    <section class="confirm-message-dialog">
      @if (description()) {
        <p class="confirm-message-dialog__description">{{ description() }}</p>
      }

      <div class="confirm-message-dialog__actions">
        @for (button of buttons(); track $index) {
          <button
            type="button"
            class="confirm-message-dialog__button"
            [class.primary]="button.primary === true"
            [class.secondary]="button.primary !== true"
            (click)="select(button.value)">
            {{ button.label }}
          </button>
        }
      </div>
    </section>
  `, isInline: true, styles: [":host{display:block}.confirm-message-dialog{display:grid;gap:1rem}.confirm-message-dialog__description{margin:0;color:var(--text-main);line-height:1.5;overflow-wrap:anywhere;word-break:break-word}.confirm-message-dialog__actions{display:flex;flex-wrap:wrap;justify-content:flex-end;gap:.75rem}.confirm-message-dialog__button{min-height:2.9rem;padding:.75rem 1.1rem;border-radius:.8rem;border:1px solid var(--border-color);cursor:pointer;font:inherit;font-weight:700}.confirm-message-dialog__button.secondary{background:var(--surface-0);color:var(--text-main)}.confirm-message-dialog__button.primary{border-color:transparent;background:linear-gradient(135deg,var(--accent),var(--accent-strong));color:var(--surface-0)}@media(max-width:720px){.confirm-message-dialog__actions{display:grid}}\n"], dependencies: [{ kind: "ngmodule", type: CommonModule }] }); }
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: ConfirmMessageDialogComponent, decorators: [{
            type: Component,
            args: [{ selector: 'app-confirm-message-dialog', standalone: true, imports: [CommonModule], template: `
    <section class="confirm-message-dialog">
      @if (description()) {
        <p class="confirm-message-dialog__description">{{ description() }}</p>
      }

      <div class="confirm-message-dialog__actions">
        @for (button of buttons(); track $index) {
          <button
            type="button"
            class="confirm-message-dialog__button"
            [class.primary]="button.primary === true"
            [class.secondary]="button.primary !== true"
            (click)="select(button.value)">
            {{ button.label }}
          </button>
        }
      </div>
    </section>
  `, styles: [":host{display:block}.confirm-message-dialog{display:grid;gap:1rem}.confirm-message-dialog__description{margin:0;color:var(--text-main);line-height:1.5;overflow-wrap:anywhere;word-break:break-word}.confirm-message-dialog__actions{display:flex;flex-wrap:wrap;justify-content:flex-end;gap:.75rem}.confirm-message-dialog__button{min-height:2.9rem;padding:.75rem 1.1rem;border-radius:.8rem;border:1px solid var(--border-color);cursor:pointer;font:inherit;font-weight:700}.confirm-message-dialog__button.secondary{background:var(--surface-0);color:var(--text-main)}.confirm-message-dialog__button.primary{border-color:transparent;background:linear-gradient(135deg,var(--accent),var(--accent-strong));color:var(--surface-0)}@media(max-width:720px){.confirm-message-dialog__actions{display:grid}}\n"] }]
        }] });

var confirmMessageDialog_component = /*#__PURE__*/Object.freeze({
    __proto__: null,
    ConfirmMessageDialogComponent: ConfirmMessageDialogComponent
});

class EntityOptionsDialogComponent {
    constructor() {
        this.chill = inject(ChillService);
        this.dialog = inject(WorkspaceDialogService);
        this.chillType = input('');
        this.displayName = input('');
        this.isLoading = signal(true);
        this.isValid = signal(true);
        this.loadError = signal('');
        this.entityOptions = signal(null);
        this.schema = computed(() => ({
            chillType: this.chillType().trim() || 'Entity.Options',
            chillViewCode: 'dialog',
            displayName: this.displayName().trim() || this.chill.T('F03D0E56-1B98-40C3-80E0-33054E81A020', 'Entity options', 'Opzioni entita'),
            metadata: {},
            properties: this.properties
        }));
        this.form = new FormGroup({
            checksumEnabled: new FormControl(false, { nonNullable: true }),
            handleAttachments: new FormControl(false, { nonNullable: true }),
            labelFormatString: new FormControl('', { nonNullable: true }),
            shortLabelFormatString: new FormControl('', { nonNullable: true }),
            fullTextContentFormatString: new FormControl('', { nonNullable: true }),
            changeLogEnabled: new FormControl(false, { nonNullable: true }),
            enableMCP: new FormControl(false, { nonNullable: true }),
            mcpDescription: new FormControl('', { nonNullable: true })
        });
        this.properties = [
            {
                name: 'checksumEnabled',
                displayName: this.chill.T('A479967A-677D-4F6E-A979-4597AE47FA97', 'Checksum enabled', 'Checksum abilitato'),
                propertyType: CHILL_PROPERTY_TYPE$1.Boolean,
                isNullable: false,
                metadata: {}
            },
            {
                name: 'handleAttachments',
                displayName: this.chill.T('64A3253C-1767-4384-A27A-D2A13FDC1634', 'Handle attachments', 'Gestisci allegati'),
                propertyType: CHILL_PROPERTY_TYPE$1.Boolean,
                isNullable: false,
                metadata: {}
            },
            {
                name: 'labelFormatString',
                displayName: this.chill.T('DA742D97-35D6-4F9F-9E58-3D476C9DA5A4', 'Label format string', 'Formato etichetta'),
                propertyType: CHILL_PROPERTY_TYPE$1.String,
                isNullable: true,
                metadata: { maxLength: '4000' }
            },
            {
                name: 'shortLabelFormatString',
                displayName: this.chill.T('7FCB4208-0CD6-450F-9028-D5A1CC185610', 'Short label format string', 'Formato etichetta breve'),
                propertyType: CHILL_PROPERTY_TYPE$1.String,
                isNullable: true,
                metadata: { maxLength: '4000' }
            },
            {
                name: 'fullTextContentFormatString',
                displayName: this.chill.T('B79CA08C-3E94-45FC-B2B2-9A6B2E630B01', 'Full text content format string', 'Formato contenuto full text'),
                propertyType: CHILL_PROPERTY_TYPE$1.Text,
                isNullable: true,
                metadata: { maxLength: '4000' }
            },
            {
                name: 'changeLogEnabled',
                displayName: this.chill.T('CC8E9A2C-4A5B-48B3-9FA8-489F73F149E5', 'Change log enabled', 'Change log abilitato'),
                propertyType: CHILL_PROPERTY_TYPE$1.Boolean,
                isNullable: false,
                metadata: {}
            },
            {
                name: 'enableMCP',
                displayName: this.chill.T('08469F6C-2B99-4BE9-8191-C3D085FF55C3', 'MCP enabled', 'MCP abilitato'),
                propertyType: CHILL_PROPERTY_TYPE$1.Boolean,
                isNullable: false,
                metadata: {}
            },
            {
                name: 'mcpDescription',
                displayName: this.chill.T('5265F0B1-B0BE-448E-88CB-80C99BF6AB92', 'MCP description', 'Descrizione MCP'),
                propertyType: CHILL_PROPERTY_TYPE$1.Text,
                isNullable: true,
                metadata: { maxLength: '4000' }
            }
        ];
        effect(() => {
            const chillType = this.chillType().trim();
            if (!chillType) {
                this.isLoading.set(false);
                this.loadError.set(this.chill.T('0F90C3B9-A9B7-4FFB-9465-D07D66737AA4', 'The selected entity type is unavailable.', 'Il tipo entita selezionato non e disponibile.'));
                return;
            }
            this.isLoading.set(true);
            this.loadError.set('');
            void this.loadEntityOptions(chillType);
        });
    }
    canDialogSubmit() {
        return !this.isLoading() && !this.loadError() && this.isValid();
    }
    async submit() {
        const currentOptions = this.entityOptions();
        const chillType = this.chillType().trim();
        if (!currentOptions || !chillType || !this.canDialogSubmit()) {
            return;
        }
        const payload = {
            chillType,
            checksumEnabled: this.readBoolean('checksumEnabled'),
            handleAttachments: this.readBoolean('handleAttachments'),
            labelFormatString: this.readOptionalString('labelFormatString'),
            shortLabelFormatString: this.readOptionalString('shortLabelFormatString'),
            fullTextContentFormatString: this.readOptionalString('fullTextContentFormatString'),
            changeLogEnabled: this.readBoolean('changeLogEnabled'),
            enableMCP: this.readBoolean('enableMCP'),
            mcpDescription: this.readOptionalString('mcpDescription')
        };
        const savedOptions = await firstValueFrom(this.chill.setEntityOptions(payload));
        this.entityOptions.set(savedOptions);
        this.dialog.confirm(savedOptions);
    }
    async loadEntityOptions(chillType) {
        try {
            const entityOptions = await firstValueFrom(this.chill.getEntityOptions(chillType));
            this.entityOptions.set(entityOptions);
            this.form.controls['checksumEnabled'].setValue(entityOptions.checksumEnabled);
            this.form.controls['handleAttachments'].setValue(entityOptions.handleAttachments);
            this.form.controls['labelFormatString'].setValue(entityOptions.labelFormatString ?? '');
            this.form.controls['shortLabelFormatString'].setValue(entityOptions.shortLabelFormatString ?? '');
            this.form.controls['fullTextContentFormatString'].setValue(entityOptions.fullTextContentFormatString ?? '');
            this.form.controls['changeLogEnabled'].setValue(entityOptions.changeLogEnabled);
            this.form.controls['enableMCP'].setValue(entityOptions.enableMCP);
            this.form.controls['mcpDescription'].setValue(entityOptions.mcpDescription ?? '');
        }
        catch (error) {
            this.entityOptions.set(null);
            this.loadError.set(this.chill.formatError(error));
        }
        finally {
            this.isLoading.set(false);
        }
    }
    readOptionalString(controlName) {
        const value = this.form.controls[controlName].value;
        return typeof value === 'string' && value.trim() ? value.trim() : null;
    }
    readBoolean(controlName) {
        return this.form.controls[controlName].value === true;
    }
    static { this.ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: EntityOptionsDialogComponent, deps: [], target: i0.ɵɵFactoryTarget.Component }); }
    static { this.ɵcmp = i0.ɵɵngDeclareComponent({ minVersion: "17.0.0", version: "19.2.21", type: EntityOptionsDialogComponent, isStandalone: true, selector: "app-entity-options-dialog", inputs: { chillType: { classPropertyName: "chillType", publicName: "chillType", isSignal: true, isRequired: false, transformFunction: null }, displayName: { classPropertyName: "displayName", publicName: "displayName", isSignal: true, isRequired: false, transformFunction: null } }, ngImport: i0, template: `
    <section class="entity-options-dialog">
      <p class="entity-options-dialog__lede">
        {{ chill.T(
          'A63A8D6A-85B4-48A3-822D-AF4F6C67D5AA',
          'Update the behavior and label formatting used by the selected type.',
          'Aggiorna il comportamento e la formattazione delle etichette usata dal tipo selezionato.'
        ) }}
      </p>

      <p class="entity-options-dialog__type">
        {{ chill.T('E17E30B7-A5A7-4A29-AE1F-9AC4F7F752AB', 'Configured type', 'Tipo configurato') }}:
        <strong>{{ chillType() }}</strong>
      </p>

      @if (loadError()) {
        <p class="entity-options-dialog__message entity-options-dialog__message--error">{{ loadError() }}</p>
      } @else if (isLoading()) {
        <p class="entity-options-dialog__message">
          {{ chill.T('F60D8E5F-A52A-44C1-A6D5-7B59AF04B3D5', 'Loading entity options...', 'Caricamento opzioni entita...') }}
        </p>
      } @else {
        <app-chill-polymorphic-input
          [form]="form"
          [schema]="schema()"
          [showLabels]="true"
          (validityChange)="isValid.set($event)"></app-chill-polymorphic-input>
      }
    </section>
  `, isInline: true, styles: [":host{display:block}.entity-options-dialog{display:grid;gap:1rem}.entity-options-dialog__lede,.entity-options-dialog__type,.entity-options-dialog__message{margin:0;color:var(--text-muted)}.entity-options-dialog__type strong{color:var(--text-main)}.entity-options-dialog__message--error{color:var(--danger)}\n"], dependencies: [{ kind: "ngmodule", type: CommonModule }, { kind: "ngmodule", type: ReactiveFormsModule }, { kind: "component", type: ChillPolymorphicInputComponent, selector: "app-chill-polymorphic-input", inputs: ["form", "schema", "propertyNames", "readonlyPropertyNames", "externalErrors", "showLabels"], outputs: ["valueChange", "validityChange", "fieldBlur", "lookupDialogOpenChange", "editorDialogOpenChange"] }] }); }
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: EntityOptionsDialogComponent, decorators: [{
            type: Component,
            args: [{ selector: 'app-entity-options-dialog', standalone: true, imports: [CommonModule, ReactiveFormsModule, ChillPolymorphicInputComponent], template: `
    <section class="entity-options-dialog">
      <p class="entity-options-dialog__lede">
        {{ chill.T(
          'A63A8D6A-85B4-48A3-822D-AF4F6C67D5AA',
          'Update the behavior and label formatting used by the selected type.',
          'Aggiorna il comportamento e la formattazione delle etichette usata dal tipo selezionato.'
        ) }}
      </p>

      <p class="entity-options-dialog__type">
        {{ chill.T('E17E30B7-A5A7-4A29-AE1F-9AC4F7F752AB', 'Configured type', 'Tipo configurato') }}:
        <strong>{{ chillType() }}</strong>
      </p>

      @if (loadError()) {
        <p class="entity-options-dialog__message entity-options-dialog__message--error">{{ loadError() }}</p>
      } @else if (isLoading()) {
        <p class="entity-options-dialog__message">
          {{ chill.T('F60D8E5F-A52A-44C1-A6D5-7B59AF04B3D5', 'Loading entity options...', 'Caricamento opzioni entita...') }}
        </p>
      } @else {
        <app-chill-polymorphic-input
          [form]="form"
          [schema]="schema()"
          [showLabels]="true"
          (validityChange)="isValid.set($event)"></app-chill-polymorphic-input>
      }
    </section>
  `, styles: [":host{display:block}.entity-options-dialog{display:grid;gap:1rem}.entity-options-dialog__lede,.entity-options-dialog__type,.entity-options-dialog__message{margin:0;color:var(--text-muted)}.entity-options-dialog__type strong{color:var(--text-main)}.entity-options-dialog__message--error{color:var(--danger)}\n"] }]
        }], ctorParameters: () => [] });

var entityOptionsDialog_component = /*#__PURE__*/Object.freeze({
    __proto__: null,
    EntityOptionsDialogComponent: EntityOptionsDialogComponent
});

class UserProfileDialogComponent {
    constructor() {
        this.chill = inject(ChillService);
        this.dialog = inject(WorkspaceDialogService);
        this.userGuid = input('');
        this.isLoading = signal(true);
        this.isValid = signal(true);
        this.loadError = signal('');
        this.user = signal(null);
        this.schema = computed(() => ({
            chillType: 'Auth.User',
            chillViewCode: 'dialog',
            displayName: this.chill.T('EF63959A-FF5D-4AC5-8AE5-BEB27B2FAE90', 'User profile', 'Profilo utente'),
            metadata: {},
            properties: this.properties
        }));
        this.form = new FormGroup({
            displayName: new FormControl('', { nonNullable: true }),
            displayCultureName: new FormControl('', { nonNullable: true }),
            displayTimeZone: new FormControl('', { nonNullable: true }),
            displayDateFormat: new FormControl('', { nonNullable: true }),
            displayNumberFormat: new FormControl('', { nonNullable: true })
        });
        this.cultureNameOptions = getCultureNameOptions();
        this.dateFormatOptions = getDateFormatOptions();
        this.timeZoneOptions = getIanaTimeZoneOptions();
        this.properties = [
            {
                name: 'displayName',
                displayName: this.chill.T('4971B652-5F24-4D38-9D9B-6D9BCE10BCB0', 'Display name', 'Nome visualizzato'),
                propertyType: CHILL_PROPERTY_TYPE$1.String,
                isNullable: false,
                metadata: { required: 'true', maxLength: '255' }
            },
            {
                name: 'displayCultureName',
                displayName: this.chill.T('771A6B48-7330-4852-A6B5-5BD314EC5662', 'Culture', 'Cultura'),
                propertyType: CHILL_PROPERTY_TYPE$1.Select,
                isNullable: false,
                metadata: {
                    required: 'true',
                    options: this.cultureNameOptions
                }
            },
            {
                name: 'displayTimeZone',
                displayName: this.chill.T('98E424F1-0183-4D9E-9B69-CDB16EBD41CF', 'Time zone', 'Fuso orario'),
                propertyType: CHILL_PROPERTY_TYPE$1.Select,
                isNullable: false,
                metadata: {
                    required: 'true',
                    options: this.timeZoneOptions
                }
            },
            {
                name: 'displayDateFormat',
                displayName: this.chill.T('49988673-8DBD-4C2B-9430-DA3054F0E294', 'Date format', 'Formato data'),
                propertyType: CHILL_PROPERTY_TYPE$1.Select,
                isNullable: false,
                metadata: {
                    required: 'true',
                    options: this.dateFormatOptions
                }
            },
            {
                name: 'displayNumberFormat',
                displayName: this.chill.T('22A3BF58-7889-4AD7-A9EF-06B6A69A8D3C', 'Number format', 'Formato numerico'),
                propertyType: CHILL_PROPERTY_TYPE$1.String,
                isNullable: false,
                metadata: { required: 'true', maxLength: '64' }
            }
        ];
        effect(() => {
            const userGuid = this.userGuid().trim();
            if (!userGuid) {
                this.isLoading.set(false);
                this.loadError.set(this.chill.T('A7EC70B4-4788-4850-A135-8743E2D9D86B', 'The current user identifier is unavailable.', 'L identificatore dell utente corrente non e disponibile.'));
                return;
            }
            this.isLoading.set(true);
            this.loadError.set('');
            void this.loadUser(userGuid);
        });
    }
    canDialogSubmit() {
        return !this.isLoading() && !this.loadError() && this.isValid();
    }
    async submit() {
        const userGuid = this.userGuid().trim();
        const currentUser = this.user();
        if (!userGuid || !currentUser || !this.canDialogSubmit()) {
            return;
        }
        const request = {
            displayName: this.readString('displayName'),
            displayCultureName: this.readString('displayCultureName'),
            displayTimeZone: this.readString('displayTimeZone'),
            displayDateFormat: this.readString('displayDateFormat'),
            displayNumberFormat: this.readString('displayNumberFormat')
        };
        await firstValueFrom(this.chill.updateUserProfile(userGuid, request));
        this.dialog.confirm(request);
    }
    async loadUser(userGuid) {
        try {
            const user = await firstValueFrom(this.chill.getAuthUserDetails(userGuid));
            this.user.set(user);
            this.form.controls['displayName'].setValue(user.displayName ?? '');
            this.form.controls['displayCultureName'].setValue(user.displayCultureName ?? '');
            this.form.controls['displayTimeZone'].setValue(user.displayTimeZone ?? '');
            this.form.controls['displayDateFormat'].setValue(user.displayDateFormat ?? '');
            this.form.controls['displayNumberFormat'].setValue(user.displayNumberFormat ?? '');
        }
        catch (error) {
            this.user.set(null);
            this.loadError.set(this.chill.formatError(error));
        }
        finally {
            this.isLoading.set(false);
        }
    }
    readString(controlName) {
        const value = this.form.controls[controlName].value;
        return typeof value === 'string' ? value.trim() : '';
    }
    static { this.ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: UserProfileDialogComponent, deps: [], target: i0.ɵɵFactoryTarget.Component }); }
    static { this.ɵcmp = i0.ɵɵngDeclareComponent({ minVersion: "17.0.0", version: "19.2.21", type: UserProfileDialogComponent, isStandalone: true, selector: "app-user-profile-dialog", inputs: { userGuid: { classPropertyName: "userGuid", publicName: "userGuid", isSignal: true, isRequired: false, transformFunction: null } }, ngImport: i0, template: `
    <section class="user-profile-dialog">
      <p class="user-profile-dialog__lede">
        {{ chill.T(
          '90F2A89E-7E41-449B-B8DA-934ECA76E4B8',
          'Update your personal preferences and save them to your account.',
          'Aggiorna le tue preferenze personali e salvale nel tuo account.'
        ) }}
      </p>

      @if (loadError()) {
        <p class="user-profile-dialog__message user-profile-dialog__message--error">{{ loadError() }}</p>
      } @else if (isLoading()) {
        <p class="user-profile-dialog__message">
          {{ chill.T('B58564E5-6F58-4068-812F-B1A9344E474F', 'Loading user profile...', 'Caricamento profilo utente...') }}
        </p>
      } @else {
        <app-chill-polymorphic-input
          [form]="form"
          [schema]="schema()"
          [showLabels]="true"
          (validityChange)="isValid.set($event)"></app-chill-polymorphic-input>
      }
    </section>
  `, isInline: true, styles: [":host{display:block}.user-profile-dialog{display:grid;gap:1rem}.user-profile-dialog__lede,.user-profile-dialog__message{margin:0;color:var(--text-muted)}.user-profile-dialog__message--error{color:var(--danger)}\n"], dependencies: [{ kind: "ngmodule", type: CommonModule }, { kind: "ngmodule", type: ReactiveFormsModule }, { kind: "component", type: ChillPolymorphicInputComponent, selector: "app-chill-polymorphic-input", inputs: ["form", "schema", "propertyNames", "readonlyPropertyNames", "externalErrors", "showLabels"], outputs: ["valueChange", "validityChange", "fieldBlur", "lookupDialogOpenChange", "editorDialogOpenChange"] }] }); }
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: UserProfileDialogComponent, decorators: [{
            type: Component,
            args: [{ selector: 'app-user-profile-dialog', standalone: true, imports: [CommonModule, ReactiveFormsModule, ChillPolymorphicInputComponent], template: `
    <section class="user-profile-dialog">
      <p class="user-profile-dialog__lede">
        {{ chill.T(
          '90F2A89E-7E41-449B-B8DA-934ECA76E4B8',
          'Update your personal preferences and save them to your account.',
          'Aggiorna le tue preferenze personali e salvale nel tuo account.'
        ) }}
      </p>

      @if (loadError()) {
        <p class="user-profile-dialog__message user-profile-dialog__message--error">{{ loadError() }}</p>
      } @else if (isLoading()) {
        <p class="user-profile-dialog__message">
          {{ chill.T('B58564E5-6F58-4068-812F-B1A9344E474F', 'Loading user profile...', 'Caricamento profilo utente...') }}
        </p>
      } @else {
        <app-chill-polymorphic-input
          [form]="form"
          [schema]="schema()"
          [showLabels]="true"
          (validityChange)="isValid.set($event)"></app-chill-polymorphic-input>
      }
    </section>
  `, styles: [":host{display:block}.user-profile-dialog{display:grid;gap:1rem}.user-profile-dialog__lede,.user-profile-dialog__message{margin:0;color:var(--text-muted)}.user-profile-dialog__message--error{color:var(--danger)}\n"] }]
        }], ctorParameters: () => [] });

const CRUD_CONFIGURATION_KEYS = new Set([
    'chillType',
    'chillQuery',
    'viewCode',
    'disableAdd',
    'disableCreate',
    'disableEdit',
    'disableInlineEdit',
    'disableDelete',
    'relationLabel',
    'defaultValues',
    'fixedValues',
    'fixedQueryValues',
    'defaultQueryValues',
    'relations'
]);
class WorkspaceMenuItemDialogComponent {
    constructor() {
        this.chill = inject(ChillService);
        this.workspace = inject(WorkspaceService);
        this.toolbar = inject(WorkspaceToolbarService);
        this.item = input(null);
        this.parent = input(null);
        this.visible = input(true);
        this.isValid = signal(true);
        this.isGeneratingConfigurationExample = signal(false);
        this.selectedComponentName = signal('');
        this.componentOptions = computed(() => {
            const registryOptions = this.workspace.availableTasks()
                .map((task) => [task.componentName, `${task.title} (${task.componentName})`])
                .sort((left, right) => left[1].localeCompare(right[1]));
            return [
                ['', 'Menu empty node'],
                ...registryOptions
            ];
        });
        this.selectedTaskDefinition = computed(() => {
            const componentName = this.selectedComponentName().toLowerCase();
            if (!componentName) {
                return null;
            }
            return this.workspace.availableTasks().find((task) => task.componentName === componentName) ?? null;
        });
        this.selectedComponentConfigurationJsonExample = computed(() => this.selectedTaskDefinition()?.componentConfigurationJsonExample?.trim() || '{}');
        this.properties = computed(() => [
            {
                name: 'title',
                displayName: 'Title',
                propertyType: CHILL_PROPERTY_TYPE$1.String,
                isNullable: false,
                metadata: { required: 'true', maxLength: '255' }
            },
            {
                name: 'description',
                displayName: 'Description',
                propertyType: CHILL_PROPERTY_TYPE$1.Text,
                isNullable: true,
                metadata: {}
            },
            {
                name: 'componentName',
                displayName: 'ComponentName',
                propertyType: CHILL_PROPERTY_TYPE$1.Select,
                isNullable: true,
                metadata: {
                    required: 'false',
                    options: this.componentOptions()
                }
            },
            {
                name: 'componentConfigurationJson',
                displayName: 'ComponentConfigurationJson',
                propertyType: CHILL_PROPERTY_TYPE$1.Json,
                isNullable: true,
                metadata: {}
            },
            {
                name: 'menuHierarchy',
                displayName: 'MenuHierarchy',
                propertyType: CHILL_PROPERTY_TYPE$1.String,
                isNullable: true,
                metadata: { required: 'false', maxLength: '255' }
            }
        ]);
        this.schema = computed(() => ({
            chillType: 'Workspace.MenuItem',
            chillViewCode: 'dialog',
            displayName: this.chill.T('A92F5438-A4FB-4FC4-BA51-308D38208E77', 'Menu item', 'Voce di menu'),
            metadata: {},
            properties: this.properties()
        }));
        this.form = new FormGroup({
            title: new FormControl('', { nonNullable: true }),
            description: new FormControl('', { nonNullable: true }),
            componentName: new FormControl('', { nonNullable: true }),
            componentConfigurationJson: new FormControl('', { nonNullable: true }),
            menuHierarchy: new FormControl('', { nonNullable: true })
        });
        this.componentNameSubscription = this.form.controls['componentName'].valueChanges.subscribe((value) => {
            this.selectedComponentName.set(typeof value === 'string' ? value.trim() : '');
        });
        effect(() => {
            const source = this.item();
            this.form.controls['title'].setValue(source?.title ?? '');
            this.form.controls['description'].setValue(source?.description ?? '');
            this.form.controls['componentName'].setValue(source?.componentName ?? '');
            this.selectedComponentName.set(source?.componentName?.trim() ?? '');
            this.form.controls['componentConfigurationJson'].setValue(source?.componentConfigurationJson ?? '');
            this.form.controls['menuHierarchy'].setValue(source?.menuHierarchy ?? '');
        });
        effect(() => {
            const selectedTask = this.selectedTaskDefinition();
            this.toolbar.setButtons([
                {
                    id: 'menu-item-apply-configuration-example',
                    labelGuid: '64BFBDFC-EA1B-47C5-95E1-8B5074B9E98A',
                    primaryDefaultText: 'Use config example',
                    secondaryDefaultText: 'Usa esempio config',
                    ariaLabel: this.chill.T('64BFBDFC-EA1B-47C5-95E1-8B5074B9E98A', 'Use config example', 'Usa esempio config'),
                    icon: 'data_object',
                    iconClass: 'material-symbol-icon',
                    action: () => void this.applyComponentConfigurationJsonExample(),
                    disabled: !selectedTask || this.isGeneratingConfigurationExample()
                }
            ], 'dialog');
        });
    }
    ngOnDestroy() {
        this.componentNameSubscription.unsubscribe();
        this.toolbar.clearButtons('dialog');
    }
    canDialogSubmit() {
        return this.isValid();
    }
    dialogResult() {
        const source = this.item();
        const parent = this.parent();
        return {
            value: {
                guid: source?.guid ?? '',
                positionNo: source?.positionNo ?? 0,
                title: this.readString('title'),
                description: this.readOptionalString('description'),
                parent,
                componentName: this.readString('componentName'),
                componentConfigurationJson: this.readOptionalString('componentConfigurationJson'),
                menuHierarchy: this.readString('menuHierarchy')
            }
        };
    }
    parentTitle() {
        return this.parent()?.title?.trim() ?? '';
    }
    async applyComponentConfigurationJsonExample() {
        const selectedTask = this.selectedTaskDefinition();
        if (!selectedTask) {
            return;
        }
        this.isGeneratingConfigurationExample.set(true);
        try {
            const nextValue = this.selectedComponentName().trim().toLowerCase() === 'crud'
                ? await this.generateCrudComponentConfigurationJsonExample()
                : this.selectedComponentConfigurationJsonExample();
            this.form.controls['componentConfigurationJson'].setValue(nextValue);
        }
        catch {
            this.form.controls['componentConfigurationJson'].setValue(this.readString('componentConfigurationJson') || this.selectedComponentConfigurationJsonExample());
        }
        finally {
            this.isGeneratingConfigurationExample.set(false);
        }
        this.form.controls['componentConfigurationJson'].markAsDirty();
        this.form.controls['componentConfigurationJson'].markAsTouched();
    }
    readString(controlName) {
        const value = this.form.controls[controlName].value;
        return typeof value === 'string' ? value.trim() : '';
    }
    readOptionalString(controlName) {
        const value = this.readString(controlName);
        return value ? value : null;
    }
    async generateCrudComponentConfigurationJsonExample() {
        const currentConfiguration = this.parseConfigurationJson(this.readString('componentConfigurationJson')) ?? this.parseConfigurationJson(this.selectedComponentConfigurationJsonExample());
        if (!currentConfiguration) {
            return this.selectedComponentConfigurationJsonExample();
        }
        const templateConfiguration = this.parseConfigurationJson(this.selectedComponentConfigurationJsonExample())
            ?? this.createEmptyCrudConfiguration();
        const seedConfiguration = this.composeCrudConfigurationSeed(templateConfiguration, currentConfiguration);
        const chillType = this.readConfigurationString(seedConfiguration, 'chillType');
        if (!chillType) {
            return JSON.stringify(seedConfiguration, null, 2);
        }
        const viewCode = this.readConfigurationString(seedConfiguration, 'viewCode') || 'default';
        const nextConfiguration = await this.buildCrudConfigurationFromSchema(seedConfiguration, chillType, viewCode, new Set());
        return JSON.stringify(nextConfiguration, null, 2);
    }
    async buildCrudConfigurationFromSchema(seedConfiguration, chillType, viewCode, visited) {
        const normalizedChillType = chillType.trim();
        const normalizedViewCode = viewCode.trim() || 'default';
        const visitKey = `${normalizedChillType.toLowerCase()}|${normalizedViewCode.toLowerCase()}`;
        if (visited.has(visitKey)) {
            return this.createCrudConfigurationObject(seedConfiguration, []);
        }
        const nextVisited = new Set(visited);
        nextVisited.add(visitKey);
        const schema = await firstValueFrom(this.chill.getSchema(normalizedChillType, normalizedViewCode, undefined, true));
        const relationConfigurations = await Promise.all((schema?.relations ?? []).map((relation) => this.buildCrudRelationConfiguration(relation, normalizedViewCode, nextVisited)));
        return this.createCrudConfigurationObject(seedConfiguration, relationConfigurations.filter((configuration) => configuration !== null));
    }
    async buildCrudRelationConfiguration(relation, viewCode, visited) {
        const chillType = this.normalizeString(relation.chillType);
        if (!chillType) {
            return null;
        }
        const seedConfiguration = this.createCrudConfigurationObject({
            chillType,
            chillQuery: this.normalizeString(relation.chillQuery) || null,
            viewCode,
            relationLabel: this.mapRelationLabel(relation.relationLabel) ?? this.createEmptyRelationLabel(),
            fixedValues: this.normalizeJsonRecord(relation.fixedValues),
            fixedQueryValues: this.normalizeJsonRecord(relation.fixedQueryValues)
        }, []);
        try {
            return await this.buildCrudConfigurationFromSchema(seedConfiguration, chillType, viewCode, visited);
        }
        catch {
            return seedConfiguration;
        }
    }
    composeCrudConfigurationSeed(templateConfiguration, currentConfiguration) {
        const chillType = this.readConfigurationString(currentConfiguration, 'chillType')
            || this.readConfigurationString(templateConfiguration, 'chillType');
        const chillQuery = this.readConfigurationString(currentConfiguration, 'chillQuery')
            || this.readConfigurationString(templateConfiguration, 'chillQuery');
        const viewCode = this.readConfigurationString(currentConfiguration, 'viewCode')
            || this.readConfigurationString(templateConfiguration, 'viewCode')
            || 'default';
        return this.createCrudConfigurationObject({
            chillType,
            chillQuery: chillQuery || null,
            viewCode,
            disableAdd: this.readConfigurationBoolean(currentConfiguration, 'disableAdd', this.readConfigurationBoolean(templateConfiguration, 'disableAdd', false)),
            disableCreate: this.readConfigurationBoolean(currentConfiguration, 'disableCreate', this.readConfigurationBoolean(templateConfiguration, 'disableCreate', false)),
            disableEdit: this.readConfigurationBoolean(currentConfiguration, 'disableEdit', this.readConfigurationBoolean(templateConfiguration, 'disableEdit', false)),
            disableInlineEdit: this.readConfigurationBoolean(currentConfiguration, 'disableInlineEdit', this.readConfigurationBoolean(templateConfiguration, 'disableInlineEdit', false)),
            disableDelete: this.readConfigurationBoolean(currentConfiguration, 'disableDelete', this.readConfigurationBoolean(templateConfiguration, 'disableDelete', false)),
            relationLabel: this.readRelationLabelValue(currentConfiguration, 'relationLabel')
                ?? this.readRelationLabelValue(templateConfiguration, 'relationLabel')
                ?? this.createEmptyRelationLabel(),
            defaultValues: this.readConfigurationRecord(currentConfiguration, 'defaultValues')
                ?? this.readConfigurationRecord(templateConfiguration, 'defaultValues')
                ?? {},
            fixedValues: this.readConfigurationRecord(currentConfiguration, 'fixedValues')
                ?? this.readConfigurationRecord(templateConfiguration, 'fixedValues')
                ?? {},
            fixedQueryValues: this.readConfigurationRecord(currentConfiguration, 'fixedQueryValues')
                ?? this.readConfigurationRecord(templateConfiguration, 'fixedQueryValues')
                ?? {},
            defaultQueryValues: this.readConfigurationRecord(currentConfiguration, 'defaultQueryValues')
                ?? this.readConfigurationRecord(templateConfiguration, 'defaultQueryValues')
                ?? {},
            ...this.readAdditionalConfigurationEntries(templateConfiguration),
            ...this.readAdditionalConfigurationEntries(currentConfiguration)
        }, []);
    }
    createCrudConfigurationObject(configuration, relations) {
        const chillType = this.readConfigurationString(configuration, 'chillType');
        const chillQuery = this.readConfigurationString(configuration, 'chillQuery');
        const viewCode = this.readConfigurationString(configuration, 'viewCode') || 'default';
        return {
            chillType,
            chillQuery: chillQuery || null,
            viewCode,
            disableAdd: this.readConfigurationBoolean(configuration, 'disableAdd', false),
            disableCreate: this.readConfigurationBoolean(configuration, 'disableCreate', false),
            disableEdit: this.readConfigurationBoolean(configuration, 'disableEdit', false),
            disableInlineEdit: this.readConfigurationBoolean(configuration, 'disableInlineEdit', false),
            disableDelete: this.readConfigurationBoolean(configuration, 'disableDelete', false),
            relationLabel: this.readRelationLabelValue(configuration, 'relationLabel') ?? this.createEmptyRelationLabel(),
            defaultValues: this.readConfigurationRecord(configuration, 'defaultValues') ?? {},
            fixedValues: this.readConfigurationRecord(configuration, 'fixedValues') ?? {},
            fixedQueryValues: this.readConfigurationRecord(configuration, 'fixedQueryValues') ?? {},
            defaultQueryValues: this.readConfigurationRecord(configuration, 'defaultQueryValues') ?? {},
            relations,
            ...this.readAdditionalConfigurationEntries(configuration)
        };
    }
    createEmptyCrudConfiguration() {
        return {
            chillType: '',
            chillQuery: null,
            viewCode: 'default',
            disableAdd: false,
            disableCreate: false,
            disableEdit: false,
            disableInlineEdit: false,
            disableDelete: false,
            relationLabel: this.createEmptyRelationLabel(),
            defaultValues: {},
            fixedValues: {},
            fixedQueryValues: {},
            defaultQueryValues: {},
            relations: []
        };
    }
    createEmptyRelationLabel() {
        return {
            labelGuid: '',
            primaryDefaultText: '',
            secondaryDefaultText: ''
        };
    }
    parseConfigurationJson(value) {
        const normalizedValue = value.trim();
        if (!normalizedValue) {
            return {};
        }
        try {
            const parsed = JSON.parse(normalizedValue);
            return parsed && typeof parsed === 'object' && !Array.isArray(parsed)
                ? parsed
                : null;
        }
        catch {
            return null;
        }
    }
    readConfigurationString(configuration, key) {
        const value = this.readConfigurationValue(configuration, key);
        return typeof value === 'string' && value.trim()
            ? value.trim()
            : '';
    }
    readConfigurationBoolean(configuration, key, fallbackValue) {
        const value = this.readConfigurationValue(configuration, key);
        return typeof value === 'boolean'
            ? value
            : fallbackValue;
    }
    readConfigurationRecord(configuration, key) {
        const value = this.readConfigurationValue(configuration, key);
        return this.normalizeJsonRecord(value);
    }
    readRelationLabelValue(configuration, key) {
        const value = this.readConfigurationValue(configuration, key);
        if (typeof value === 'string') {
            const normalizedValue = value.trim();
            return normalizedValue ? normalizedValue : null;
        }
        return this.mapRelationLabel(value);
    }
    readAdditionalConfigurationEntries(configuration) {
        return Object.fromEntries(Object.entries(configuration)
            .filter(([key]) => !CRUD_CONFIGURATION_KEYS.has(key.toLowerCase())));
    }
    readConfigurationValue(configuration, key) {
        const directValue = configuration[key];
        if (directValue !== undefined) {
            return directValue;
        }
        const matchedKey = Object.keys(configuration).find((candidate) => candidate.toLowerCase() === key.toLowerCase());
        return matchedKey ? configuration[matchedKey] : undefined;
    }
    mapRelationLabel(value) {
        if (!value || typeof value !== 'object' || Array.isArray(value)) {
            return null;
        }
        const relationLabel = value;
        return {
            labelGuid: this.normalizeString(relationLabel.labelGuid),
            primaryDefaultText: this.normalizeString(relationLabel.primaryDefaultText),
            secondaryDefaultText: this.normalizeString(relationLabel.secondaryDefaultText)
        };
    }
    normalizeJsonRecord(value) {
        if (!value || typeof value !== 'object' || Array.isArray(value)) {
            return null;
        }
        return Object.fromEntries(Object.entries(value)
            .map(([key, entryValue]) => [key.trim(), entryValue])
            .filter(([key]) => key.length > 0));
    }
    normalizeString(value) {
        return typeof value === 'string'
            ? value.trim()
            : '';
    }
    static { this.ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: WorkspaceMenuItemDialogComponent, deps: [], target: i0.ɵɵFactoryTarget.Component }); }
    static { this.ɵcmp = i0.ɵɵngDeclareComponent({ minVersion: "17.0.0", version: "19.2.21", type: WorkspaceMenuItemDialogComponent, isStandalone: true, selector: "app-workspace-menu-item-dialog", inputs: { item: { classPropertyName: "item", publicName: "item", isSignal: true, isRequired: false, transformFunction: null }, parent: { classPropertyName: "parent", publicName: "parent", isSignal: true, isRequired: false, transformFunction: null }, visible: { classPropertyName: "visible", publicName: "visible", isSignal: true, isRequired: false, transformFunction: null } }, ngImport: i0, template: `
    <section class="menu-item-dialog">
      <p class="menu-item-dialog__lede">
        {{ parentTitle()
          ? chill.T('E603B25F-2943-4FE0-A4B0-11A0D8884D38', 'Configure the selected child menu item.', 'Configura la voce di menu figlia selezionata.')
          : chill.T('E7E1166A-D146-49C6-BE63-8C514B2D6097', 'Configure the selected root menu item.', 'Configura la voce di menu radice selezionata.') }}
      </p>

      @if (parentTitle()) {
        <p class="menu-item-dialog__parent">
          {{ chill.T('35D75951-2BAA-4DEB-959A-9ED9D75BE4C8', 'Parent', 'Padre') }}: <strong>{{ parentTitle() }}</strong>
        </p>
      }

      <app-chill-polymorphic-input
        [form]="form"
        [schema]="schema()"
        [showLabels]="true"
        (validityChange)="isValid.set($event)"></app-chill-polymorphic-input>
    </section>
  `, isInline: true, styles: [":host{display:block}.menu-item-dialog{display:grid;gap:1rem}.menu-item-dialog__lede,.menu-item-dialog__parent{margin:0;color:var(--text-muted)}.menu-item-dialog__parent strong{color:var(--text-main)}\n"], dependencies: [{ kind: "ngmodule", type: CommonModule }, { kind: "ngmodule", type: ReactiveFormsModule }, { kind: "component", type: ChillPolymorphicInputComponent, selector: "app-chill-polymorphic-input", inputs: ["form", "schema", "propertyNames", "readonlyPropertyNames", "externalErrors", "showLabels"], outputs: ["valueChange", "validityChange", "fieldBlur", "lookupDialogOpenChange", "editorDialogOpenChange"] }] }); }
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "19.2.21", ngImport: i0, type: WorkspaceMenuItemDialogComponent, decorators: [{
            type: Component,
            args: [{ selector: 'app-workspace-menu-item-dialog', standalone: true, imports: [CommonModule, ReactiveFormsModule, ChillPolymorphicInputComponent], template: `
    <section class="menu-item-dialog">
      <p class="menu-item-dialog__lede">
        {{ parentTitle()
          ? chill.T('E603B25F-2943-4FE0-A4B0-11A0D8884D38', 'Configure the selected child menu item.', 'Configura la voce di menu figlia selezionata.')
          : chill.T('E7E1166A-D146-49C6-BE63-8C514B2D6097', 'Configure the selected root menu item.', 'Configura la voce di menu radice selezionata.') }}
      </p>

      @if (parentTitle()) {
        <p class="menu-item-dialog__parent">
          {{ chill.T('35D75951-2BAA-4DEB-959A-9ED9D75BE4C8', 'Parent', 'Padre') }}: <strong>{{ parentTitle() }}</strong>
        </p>
      }

      <app-chill-polymorphic-input
        [form]="form"
        [schema]="schema()"
        [showLabels]="true"
        (validityChange)="isValid.set($event)"></app-chill-polymorphic-input>
    </section>
  `, styles: [":host{display:block}.menu-item-dialog{display:grid;gap:1rem}.menu-item-dialog__lede,.menu-item-dialog__parent{margin:0;color:var(--text-muted)}.menu-item-dialog__parent strong{color:var(--text-main)}\n"] }]
        }], ctorParameters: () => [] });

var workspaceMenuItemDialog_component = /*#__PURE__*/Object.freeze({
    __proto__: null,
    WorkspaceMenuItemDialogComponent: WorkspaceMenuItemDialogComponent
});

/// <reference path="./lib/runtime-config.d.ts" />

/**
 * Generated bundle index. Do not edit.
 */

export { AttachmentUploadDialogComponent, AuthRoleDialogComponent, AuthSearchSelectComponent, AuthShellComponent, AuthUserDialogComponent, CHILL_BASE_URL, CHILL_CULTURE, CHILL_PRIMARY_TEXT_CULTURE, CHILL_PROPERTY_TYPE$1 as CHILL_PROPERTY_TYPE, CHILL_PROPERTY_TYPE_OPTIONS, CHILL_SECONDARY_TEXT_CULTURE, CHILL_SHARP_UI_ROUTES, CHILL_UI_STORAGE_KEY_PREFIX, ChillFormComponent, ChillI18nButtonLabelComponent, ChillI18nLabelComponent, ChillJsonInputComponent, ChillPolymorphicInputComponent, ChillPolymorphicOutputComponent, ChillService, ChillSharpUiRootComponent, ChillTableComponent, ChillTextEditorDialogComponent, ConfirmMessageDialogComponent, ConfirmResetPageComponent, CrudPageComponent, CrudPageComponentConfiguration, CrudTaskComponent, EntityOptionsDialogComponent, LoginPageComponent, NoticeTransitionDirective, PermissionAction, PermissionEditorComponent, PermissionEffect, PermissionScope, PermissionsPageComponent, RegisterPageComponent, ResetPasswordPageComponent, RolePermissionComponent, SESSION_STORAGE_KEY, SchemaPropertyDialogComponent, USER_PREFERENCES_STORAGE_KEY, UserPermissionComponent, UserProfileDialogComponent, WORKSPACE_LAYOUT_EDITING_STORAGE_KEY, WORKSPACE_THEME_STORAGE_KEY, WorkspaceDialogHostComponent, WorkspaceDialogService, WorkspaceLayoutService, WorkspaceMenuComponent, WorkspaceMenuItemDialogComponent, WorkspacePageComponent, WorkspaceService, WorkspaceTaskRegistryService, WorkspaceTaskbarComponent, WorkspaceToolbarService, applySchemaRelationsToCrudConfiguration, buildCrudRelationsFromSchema, canChangeChillPropertyType, chillSimplePropertyType, getCultureNameOptions, getDateFormatOptions, getIanaTimeZoneOptions, provideChillSharpUiCore };
//# sourceMappingURL=chill-sharp-ui-core.mjs.map
