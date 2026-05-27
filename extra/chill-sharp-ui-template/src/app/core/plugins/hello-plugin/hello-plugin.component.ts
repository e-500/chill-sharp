import { CommonModule } from '@angular/common';
import { Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { map } from 'rxjs';

@Component({
  selector: 'app-hello-plugin',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section class="client-template-page">
      <p class="eyebrow">Hello Plugin</p>
      <h1>Hello {{ name() }}</h1>
    </section>
  `
})
export class HelloPluginComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly routeName = toSignal(this.route.paramMap.pipe(
    map((params) => params.get('name')?.trim() ?? '')
  ), { initialValue: '' });

  protected readonly name = computed(() => {
    const value = this.routeName();
    return value || 'World';
  });
}
