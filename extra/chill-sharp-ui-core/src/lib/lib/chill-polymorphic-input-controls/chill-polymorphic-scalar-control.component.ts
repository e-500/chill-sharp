import { Component, input, output } from '@angular/core';
import { FormControl, FormsModule, ReactiveFormsModule } from '@angular/forms';
import type { JsonValue } from '@chill-sharp/ng-client';

@Component({
  selector: 'app-chill-polymorphic-scalar-control',
  standalone: true,
  imports: [FormsModule, ReactiveFormsModule],
  template: `<div class="scalar-input">
    @if (formatted()) {
      <input type="text" [ngModel]="value()" (ngModelChange)="valueChange.emit($event)" (blur)="blur.emit()" [disabled]="readOnly()" [name]="name()" [placeholder]="placeholder()" />
    } @else {
      <input [type]="type()" [step]="step()" [formControl]="control()" (blur)="blur.emit()" [name]="name()" [placeholder]="placeholder()" />
    }
    @if (staticSuffix()) {
      <span class="scalar-input__suffix" aria-hidden="true">{{ staticSuffix() }}</span>
    }
  </div>`,
  styles: `.scalar-input { display: grid; grid-template-columns: minmax(0, 1fr) auto; align-items: center; gap: .55rem; } .scalar-input__suffix { color: var(--text-muted); white-space: nowrap; }`,
  styleUrl: './chill-polymorphic-control.shared.scss'
})
export class ChillPolymorphicScalarControlComponent {
  readonly control = input.required<FormControl<JsonValue>>();
  readonly name = input.required<string>();
  readonly placeholder = input('');
  readonly readOnly = input(false);
  readonly formatted = input(false);
  readonly value = input('');
  readonly type = input<'text' | 'number'>('text');
  readonly step = input<string | null>(null);
  readonly staticSuffix = input('');
  readonly valueChange = output<string>();
  readonly blur = output<void>();
}
