import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { CLIENT_APP_CONFIG } from '../../../config/app-config';
import { readClientRuntimeConfig } from '../../../config/runtime-config';

@Component({
  selector: 'app-client-home',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section class="client-template-page">
      <p class="eyebrow">Client Template</p>
      <h1>{{ appConfig.appName }}</h1>
      <p>
        This is a client-owned route that lives in the template shell instead of the shared
        <code>&#64;chill-sharp/ui-core</code> package.
      </p>

      <div class="client-template-grid">
        <article class="client-template-card">
          <strong>Tenant</strong>
          <p>{{ runtimeConfig.tenantCode }}</p>
        </article>

        <article class="client-template-card">
          <strong>API</strong>
          <p>{{ appConfig.apiBaseUrl }}</p>
        </article>

        <article class="client-template-card">
          <strong>Theme</strong>
          <p>{{ appConfig.themeName }}</p>
        </article>
      </div>
    </section>
  `
})
export class ClientHomeComponent {
  protected readonly appConfig = CLIENT_APP_CONFIG;
  protected readonly runtimeConfig = readClientRuntimeConfig();
}
