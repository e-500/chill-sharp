import { CommonModule, DOCUMENT } from '@angular/common';
import { Component, HostListener, OnDestroy, computed, effect, inject, input, signal } from '@angular/core';
import type { WorkspaceTaskComponentInterface, WorkspaceTaskConfiguration } from '../../models/workspace-task.models';
import { WorkspaceToolbarService } from '../../services/workspace-toolbar.service';

interface GoToUrlTaskConfiguration {
  url: string;
  target: string;
}

type GoToUrlOpenState = 'pending' | 'opened' | 'blocked' | 'invalid';

@Component({
  selector: 'app-goto-url-task',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section class="goto-url-task">
      <div class="goto-url-task__card">
        <p class="goto-url-task__eyebrow">GoToUrl</p>
        <h2>{{ heading() }}</h2>
        <p>{{ message() }}</p>

        @if (configuration(); as config) {
          <dl class="goto-url-task__details">
            <div>
              <dt>URL</dt>
              <dd>{{ config.url }}</dd>
            </div>
            <div>
              <dt>Target</dt>
              <dd>{{ config.target }}</dd>
            </div>
          </dl>

          <a
            class="goto-url-task__link"
            [href]="config.url"
            [target]="config.target"
            rel="noopener noreferrer"
            (click)="openConfiguredUrl()">
            Open link
          </a>
        }
      </div>
    </section>
  `,
  styles: `
    :host,
    .goto-url-task {
      display: block;
      height: 100%;
      min-height: 0;
    }

    .goto-url-task {
      display: grid;
      place-items: center;
      padding: 1.5rem;
    }

    .goto-url-task__card {
      width: min(100%, 38rem);
      display: grid;
      gap: 1rem;
      padding: 1.5rem;
      border: 1px solid var(--border-color);
      border-radius: 1rem;
      background: var(--surface-0);
      box-shadow: 0 1.25rem 2.5rem color-mix(in srgb, var(--shadow-color) 16%, transparent);
    }

    .goto-url-task__eyebrow,
    .goto-url-task__details dt {
      margin: 0;
      color: var(--text-muted);
      font-size: 0.8rem;
      font-weight: 700;
      letter-spacing: 0.08em;
      text-transform: uppercase;
    }

    .goto-url-task__card h2,
    .goto-url-task__card p,
    .goto-url-task__details,
    .goto-url-task__details dd {
      margin: 0;
    }

    .goto-url-task__details {
      display: grid;
      gap: 0.85rem;
    }

    .goto-url-task__details div {
      display: grid;
      gap: 0.25rem;
      padding: 0.85rem 1rem;
      border-radius: 0.85rem;
      background: color-mix(in srgb, var(--surface-1) 78%, transparent);
    }

    .goto-url-task__details dd {
      overflow-wrap: anywhere;
      color: var(--text-main);
    }

    .goto-url-task__link {
      justify-self: start;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      min-height: 2.75rem;
      padding: 0.75rem 1rem;
      border-radius: 999px;
      background: linear-gradient(135deg, var(--accent), var(--accent-strong));
      color: #f8fffd;
      font-weight: 700;
      text-decoration: none;
    }
  `
})
export class GoToUrlTaskComponent implements WorkspaceTaskComponentInterface, OnDestroy {
  static getComponentConfigurationJsonExample(): WorkspaceTaskConfiguration | null {
    return {
      url: 'https://example.com',
      target: '_blank'
    };
  }

  private readonly document = inject(DOCUMENT);
  private readonly toolbar = inject(WorkspaceToolbarService);

  readonly componentConfiguration = input<WorkspaceTaskConfiguration | null>(null);
  readonly toolbarScope = input('workspace');
  readonly visible = input(true);

  readonly openState = signal<GoToUrlOpenState>('pending');
  readonly configuration = computed(() => this.readConfiguration(this.componentConfiguration()));
  readonly heading = computed(() => {
    switch (this.openState()) {
      case 'opened':
        return 'Link opened';
      case 'blocked':
        return 'Popup blocked';
      case 'invalid':
        return 'Invalid configuration';
      default:
        return 'Opening link';
    }
  });
  readonly message = computed(() => {
    const config = this.configuration();
    switch (this.openState()) {
      case 'opened':
        return `The URL has been opened${config ? ` in target "${config.target}".` : '.'}`;
      case 'blocked':
        return 'The browser blocked the automatic navigation. Use the link below to continue.';
      case 'invalid':
        return 'The GoToUrl menu item requires both "url" and "target" in componentConfigurationJson.';
      default:
        return 'The configured URL is being opened. If nothing happens, use the link below.';
    }
  });

  private hasAttemptedOpen = false;

  constructor() {
    effect(() => {
      this.toolbar.clearButtons(this.toolbarScope());
    });

    effect(() => {
      const config = this.configuration();
      const isVisible = this.visible();
      if (!isVisible || this.hasAttemptedOpen) {
        return;
      }

      this.hasAttemptedOpen = true;
      this.openConfiguredUrl();
    });
  }

  ngOnDestroy(): void {
    this.toolbar.clearButtons(this.toolbarScope());
  }

  @HostListener('window:keydown', ['$event'])
  handleWindowKeydown(event: KeyboardEvent): void {
    if (!this.visible() || !event.ctrlKey || !event.altKey || event.metaKey || event.shiftKey || event.key.toLowerCase() !== 'r') {
      return;
    }

    event.preventDefault();
    this.openConfiguredUrl();
  }

  openConfiguredUrl(): void {
    const config = this.configuration();
    if (!config) {
      this.openState.set('invalid');
      return;
    }

    const view = this.document.defaultView;
    if (!view) {
      this.openState.set('blocked');
      return;
    }

    const result = view.open(config.url, config.target, 'noopener,noreferrer');
    this.openState.set(result ? 'opened' : 'blocked');
  }

  private readConfiguration(configuration: WorkspaceTaskConfiguration | null): GoToUrlTaskConfiguration | null {
    const url = this.readConfigurationString(configuration, 'url');
    const target = this.readConfigurationString(configuration, 'target');
    if (!url || !target) {
      return null;
    }

    return { url, target };
  }

  private readConfigurationString(
    configuration: WorkspaceTaskConfiguration | null,
    key: string
  ): string {
    if (!configuration) {
      return '';
    }

    const directValue = configuration[key];
    if (typeof directValue === 'string' && directValue.trim()) {
      return directValue.trim();
    }

    const matchedEntry = Object.entries(configuration)
      .find(([entryKey, entryValue]) => entryKey.toLowerCase() === key.toLowerCase()
        && typeof entryValue === 'string'
        && entryValue.trim());

    return typeof matchedEntry?.[1] === 'string'
      ? matchedEntry[1].trim()
      : '';
  }
}
