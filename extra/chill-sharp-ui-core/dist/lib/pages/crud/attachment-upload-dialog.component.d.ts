import { FormControl, FormGroup } from '@angular/forms';
import { ChillService } from '../../services/chill.service';
import * as i0 from "@angular/core";
interface AttachmentUploadFormValue {
    title: FormControl<string>;
    description: FormControl<string>;
    isPublic: FormControl<boolean>;
}
export declare class AttachmentUploadDialogComponent {
    readonly chill: ChillService;
    private readonly dialog;
    readonly attachToChillType: import("@angular/core").InputSignal<string>;
    readonly attachToGuid: import("@angular/core").InputSignal<string>;
    readonly form: FormGroup<AttachmentUploadFormValue>;
    readonly selectedFile: import("@angular/core").WritableSignal<File | null>;
    readonly selectedFileName: import("@angular/core").WritableSignal<string>;
    canDialogSubmit(): boolean;
    submit(): Promise<void>;
    onFileSelected(event: Event): void;
    static ɵfac: i0.ɵɵFactoryDeclaration<AttachmentUploadDialogComponent, never>;
    static ɵcmp: i0.ɵɵComponentDeclaration<AttachmentUploadDialogComponent, "app-attachment-upload-dialog", never, { "attachToChillType": { "alias": "attachToChillType"; "required": false; "isSignal": true; }; "attachToGuid": { "alias": "attachToGuid"; "required": false; "isSignal": true; }; }, {}, never, never, true, never>;
}
export {};
//# sourceMappingURL=attachment-upload-dialog.component.d.ts.map