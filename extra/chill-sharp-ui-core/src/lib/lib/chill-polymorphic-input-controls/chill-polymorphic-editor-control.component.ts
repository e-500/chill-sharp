import { Component, inject, input, output } from '@angular/core';
import { ChillJsonInputComponent } from '../chill-json-input.component';
import { ChillService } from '../../services/chill.service';

@Component({
  selector: 'app-chill-polymorphic-editor-control',
  standalone: true,
  imports: [ChillJsonInputComponent],
  template: `<div class="editor-field">
    <app-chill-json-input [value]="value()" [language]="language()" [placeholder]="placeholder()" [invalid]="invalid()" [disabled]="disabled()" (valueChange)="valueChange.emit($event)" (blur)="blur.emit()" />
    <button type="button" class="editor-dialog-button" (mousedown)="beginDialog.emit()" (click)="openDialog.emit()" [attr.aria-label]="chill.T('EC935816-DCBC-4AE7-BA13-36D66A7D7EBD', 'Open editor dialog', 'Apri editor in dialog')"><span class="material-symbol-icon" aria-hidden="true">open_in_full</span></button>
  </div>`,
  styles: [`.editor-field { position: relative; min-width: 0; } app-chill-json-input { width: 100%; } .editor-dialog-button { position: absolute; top: .35rem; right: .35rem; z-index: 1; display: inline-grid; place-items: center; width: 2rem; height: 2rem; padding: 0; border: 1px solid color-mix(in srgb, var(--accent) 26%, var(--border-color)); border-radius: .4rem; background: color-mix(in srgb, var(--surface-0) 92%, transparent); color: var(--text-main); box-shadow: var(--shadow-soft); cursor: pointer; } .editor-dialog-button .material-symbol-icon { font-size: 1.05rem; } :host-context(:root[data-theme='dark']) .editor-dialog-button { background: rgba(9, 19, 26, .58); }`]
})
export class ChillPolymorphicEditorControlComponent {
  readonly chill = inject(ChillService);
  readonly value = input('');
  readonly language = input<'json' | 'plaintext'>('plaintext');
  readonly placeholder = input('');
  readonly invalid = input(false);
  readonly disabled = input(false);
  readonly valueChange = output<string>();
  readonly blur = output<void>();
  readonly beginDialog = output<void>();
  readonly openDialog = output<void>();
}
