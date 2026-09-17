import { Component, input } from '@angular/core';

@Component({ selector: 'app-chill-polymorphic-output-boolean-control', standalone: true, template: `{{ text() }}` })
export class ChillPolymorphicOutputBooleanControlComponent {
  readonly text = input('');
}
