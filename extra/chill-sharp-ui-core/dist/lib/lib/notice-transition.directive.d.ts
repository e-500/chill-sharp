import { AfterViewInit, OnDestroy } from '@angular/core';
import * as i0 from "@angular/core";
export declare class NoticeTransitionDirective implements AfterViewInit, OnDestroy {
    private readonly elementRef;
    private enterFrame;
    private enterCleanupTimer;
    private enterCleanup;
    ngAfterViewInit(): void;
    ngOnDestroy(): void;
    private animateEnter;
    private animateLeaveClone;
    private cancelEnterAnimation;
    private shouldSkipAnimation;
    static ɵfac: i0.ɵɵFactoryDeclaration<NoticeTransitionDirective, never>;
    static ɵdir: i0.ɵɵDirectiveDeclaration<NoticeTransitionDirective, ".notice", never, {}, {}, never, never, true, never>;
}
//# sourceMappingURL=notice-transition.directive.d.ts.map