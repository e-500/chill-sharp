import { Component, input } from '@angular/core';

@Component({ selector: 'app-chill-polymorphic-output-lookup-control', standalone: true, template: `{{ text() }}` })
export class ChillPolymorphicOutputLookupControlComponent {
  readonly text = input('');
}
