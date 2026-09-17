import { Component, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import type { ChillPropertySelectOptionTuple } from '../../models/chill-schema.models';

@Component({
  selector: 'app-chill-polymorphic-select-control',
  standalone: true,
  imports: [FormsModule],
  template: `<select [ngModel]="value()" (ngModelChange)="valueChange.emit($event)" [disabled]="readOnly()" [name]="name()">
    @for (option of options(); track option[0] + ':' + option[1]) { <option [value]="option[0]">{{ option[1] }}</option> }
  </select>`,
  styleUrl: './chill-polymorphic-control.shared.scss'
})
export class ChillPolymorphicSelectControlComponent {
  readonly name = input.required<string>();
  readonly value = input('');
  readonly options = input<ChillPropertySelectOptionTuple[]>([]);
  readonly readOnly = input(false);
  readonly valueChange = output<string>();
}
