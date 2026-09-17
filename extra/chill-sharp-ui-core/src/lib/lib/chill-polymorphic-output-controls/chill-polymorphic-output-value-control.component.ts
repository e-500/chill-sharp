import { Component, input } from '@angular/core';

@Component({ selector: 'app-chill-polymorphic-output-value-control', standalone: true, template: `{{ text() }}` })
export class ChillPolymorphicOutputValueControlComponent {
  readonly text = input('');
}
