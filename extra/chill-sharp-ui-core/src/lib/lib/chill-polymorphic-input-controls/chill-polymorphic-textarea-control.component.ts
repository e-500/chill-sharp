import { Component, input, output } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import type { JsonValue } from '@chill-sharp/ng-client';

@Component({
  selector: 'app-chill-polymorphic-textarea-control',
  standalone: true,
  imports: [ReactiveFormsModule],
  template: `<textarea rows="4" [formControl]="control()" [disabled]="readOnly()" (blur)="blur.emit()" [name]="name()" [placeholder]="placeholder()"></textarea>`,
  styleUrl: './chill-polymorphic-control.shared.scss'
})
export class ChillPolymorphicTextareaControlComponent {
  readonly control = input.required<FormControl<JsonValue>>();
  readonly name = input.required<string>();
  readonly placeholder = input('');
  readonly readOnly = input(false);
  readonly blur = output<void>();
}
