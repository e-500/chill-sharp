import { Component, input } from '@angular/core';

@Component({
  selector: 'app-chill-polymorphic-output-temporal-control',
  standalone: true,
  template: `@if (parts(); as resolvedParts) {
    <span class="spaced-parts">@for (part of resolvedParts; track $index; let isFirst = $first) { @if (!isFirst) { <span class="spaced-parts__separator" aria-hidden="true">&nbsp;</span><wbr> } <span class="spaced-parts__part">{{ part }}</span> }</span>
  } @else { {{ text() }} }`,
  styles: `.spaced-parts { display: inline; } .spaced-parts__part { white-space: nowrap; }`
})
export class ChillPolymorphicOutputTemporalControlComponent {
  readonly parts = input<string[] | null>(null);
  readonly text = input('');
}
