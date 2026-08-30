import { Component, input, output } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import type { JsonValue } from '@chill-sharp/ng-client';

@Component({
  selector: 'app-chill-polymorphic-boolean-control',
  standalone: true,
  imports: [ReactiveFormsModule],
  template: `<input type="checkbox" [formControl]="control()" [disabled]="readOnly()" (blur)="blur.emit()" [name]="name()" />`,
  styleUrl: './chill-polymorphic-control.shared.scss'
})
export class ChillPolymorphicBooleanControlComponent {
  readonly control = input.required<FormControl<JsonValue>>();
  readonly name = input.required<string>();
  readonly readOnly = input(false);
  readonly blur = output<void>();
}
