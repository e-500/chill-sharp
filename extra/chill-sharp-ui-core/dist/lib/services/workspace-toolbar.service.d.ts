import * as i0 from "@angular/core";
export interface WorkspaceToolbarButton {
    id: string;
    label?: string;
    labelGuid?: string | null;
    primaryDefaultText?: string | null;
    secondaryDefaultText?: string | null;
    ariaLabel?: string;
    icon?: string | null;
    iconClass?: string | null;
    accent?: boolean;
    action: () => void;
    disabled?: boolean;
}
export declare class WorkspaceToolbarService {
    private readonly buttonScopesState;
    buttons(scope?: string): WorkspaceToolbarButton[];
    setButtons(buttons: WorkspaceToolbarButton[], scope?: string): void;
    clearButtons(scope?: string): void;
    static ɵfac: i0.ɵɵFactoryDeclaration<WorkspaceToolbarService, never>;
    static ɵprov: i0.ɵɵInjectableDeclaration<WorkspaceToolbarService>;
}
//# sourceMappingURL=workspace-toolbar.service.d.ts.map