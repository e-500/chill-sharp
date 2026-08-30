import { Component, input } from '@angular/core';

@Component({ selector: 'app-chill-polymorphic-output-number-control', standalone: true, template: `{{ text() }}` })
export class ChillPolymorphicOutputNumberControlComponent {
  readonly text = input('');
}
