import { Component, inject, input, output } from '@angular/core';
import { ConnectedPosition, OverlayModule } from '@angular/cdk/overlay';
import { FormsModule } from '@angular/forms';
import type { JsonObject } from '@chill-sharp/ng-client';
import { CHILL_PROPERTY_TYPE, type ChillPropertySchema } from '../../models/chill-schema.models';
import { ChillService } from '../../services/chill.service';

@Component({
  selector: 'app-chill-polymorphic-lookup-control',
  standalone: true,
  imports: [FormsModule, OverlayModule],
  template: `<div class="lookup">
    @if (isCollection()) {
      @for (entity of selectedEntities(); track lookupGuid(entity) || $index) {
        <div class="lookup-selected" [title]="lookupLabel(entity)">
          <span class="lookup-selected__label lookup-selected__label--full">{{ lookupLabel(entity) || selectValueText }}</span>
          <span class="lookup-selected__label lookup-selected__label--short">{{ lookupShortLabel(entity) || lookupLabel(entity) || selectValueText }}</span>
          <button type="button" class="lookup-selected__clear" (click)="remove.emit(entity)" [disabled]="readOnly()" [attr.aria-label]="clearText">X</button>
        </div>
      }
    } @else if (isSingleEntity() && hasSelectedEntity()) {
      <div class="lookup-selected" [title]="selectedLabel()">
        <span class="lookup-selected__label lookup-selected__label--full">{{ selectedLabel() || selectValueText }}</span>
        <span class="lookup-selected__label lookup-selected__label--short">{{ selectedShortLabel() || selectedLabel() || selectValueText }}</span>
        <button type="button" class="lookup-selected__clear" (click)="clear.emit()" [disabled]="readOnly()" [attr.aria-label]="clearText">X</button>
      </div>
    } @else {
      <div class="lookup-bar">
        <div class="lookup-input-slot" cdkOverlayOrigin #lookupOrigin="cdkOverlayOrigin" #lookupOriginElement>
          <input type="text" [ngModel]="term()" (ngModelChange)="termChange.emit($event)" (focus)="focus.emit()" (blur)="blur.emit()" [disabled]="readOnly()" [name]="property().name" [placeholder]="placeholder() || searchText" />
        </div>
        <button type="button" class="lookup-dialog" (mousedown)="beginDialog.emit()" (click)="openDialog.emit()" [disabled]="readOnly() || !canOpenDialog()" [attr.aria-label]="openPickerText">...</button>
        @if (isCollection()) { <button type="button" class="lookup-clear" (click)="clear.emit()" [disabled]="readOnly()">{{ clearText }}</button> }
      </div>
      <ng-template cdkConnectedOverlay [cdkConnectedOverlayOrigin]="lookupOrigin" [cdkConnectedOverlayOpen]="results().length > 0" [cdkConnectedOverlayPositions]="positions()" [cdkConnectedOverlayPush]="true" [cdkConnectedOverlayFlexibleDimensions]="true" [cdkConnectedOverlayViewportMargin]="8" [cdkConnectedOverlayOffsetY]="6">
        <div class="lookup-results" role="listbox" [style.--lookup-overlay-width.px]="overlayWidth(lookupOriginElement)">
          @for (result of results(); track $index) { <button type="button" class="lookup-result" [class.is-selected]="isSelected(result)" (mousedown)="$event.preventDefault()" (click)="select.emit(result)">{{ lookupLabel(result) || selectValueText }}</button> }
        </div>
      </ng-template>
    }
    @if (searching()) { <small class="lookup-status">{{ searchingText }}</small> }
    @if (error()) { <small class="field-error">{{ error() }}</small> }
  </div>`,
  styleUrl: './chill-polymorphic-lookup-control.component.scss'
})
export class ChillPolymorphicLookupControlComponent {
  readonly chill = inject(ChillService);
  readonly property = input.required<ChillPropertySchema>();
  readonly readOnly = input(false);
  readonly placeholder = input('');
  readonly term = input('');
  readonly results = input<JsonObject[]>([]);
  readonly error = input('');
  readonly searching = input(false);
  readonly selectedEntities = input<JsonObject[]>([]);
  readonly selectedLabel = input('');
  readonly selectedShortLabel = input('');
  readonly selectedGuid = input('');
  readonly hasSelectedEntity = input(false);
  readonly canOpenDialog = input(false);
  readonly positions = input<ConnectedPosition[]>([]);
  readonly termChange = output<string>();
  readonly focus = output<void>();
  readonly blur = output<void>();
  readonly beginDialog = output<void>();
  readonly openDialog = output<void>();
  readonly clear = output<void>();
  readonly select = output<JsonObject>();
  readonly remove = output<JsonObject>();

  get selectValueText(): string { return this.chill.T('B93CA88B-01FE-44B6-9C2F-C9878A7B324B', 'Select value', 'Seleziona valore'); }
  get clearText(): string { return this.chill.T('34015BA4-E0CA-460E-B82B-A4E2D4D8A184', 'Clear', 'Pulisci'); }
  get searchText(): string { return this.chill.T('FA7B8E01-658C-4D63-B53F-D476CD697892', 'Search entity', 'Cerca entita'); }
  get openPickerText(): string { return this.chill.T('B6D48459-73D4-4234-977E-8D79E510A20D', 'Open entity picker', 'Apri selettore entita'); }
  get searchingText(): string { return this.chill.T('ABAF5996-C2BB-4F85-BE0F-CC75883A648B', 'Searching...', 'Ricerca in corso...'); }
  isCollection(): boolean { return this.property().propertyType === CHILL_PROPERTY_TYPE.ChillEntityCollection; }
  isSingleEntity(): boolean { return this.property().propertyType === CHILL_PROPERTY_TYPE.ChillEntity; }
  overlayWidth(origin: HTMLElement | null): number { return origin ? Math.ceil(origin.getBoundingClientRect().width) : 0; }
  isSelected(result: JsonObject): boolean {
    const guid = this.lookupGuid(result);
    return !!guid && (this.selectedEntities().some((entity) => this.lookupGuid(entity) === guid) || this.selectedGuid() === guid);
  }
  lookupLabel(result: JsonObject): string {
    const value = result['Label'] ?? result['label'] ?? result['DisplayName'] ?? result['displayName'] ?? result['Name'] ?? result['name'] ?? result['Guid'] ?? result['guid'];
    return typeof value === 'string' ? value.trim() : typeof value === 'number' || typeof value === 'boolean' ? String(value) : '';
  }
  lookupShortLabel(result: JsonObject): string {
    const value = result['ShortLabel'] ?? result['shortLabel'] ?? result['ShortName'] ?? result['shortName'] ?? result['Code'];
    return typeof value === 'string' && value.trim() ? value.trim() : typeof value === 'number' || typeof value === 'boolean' ? String(value) : this.lookupLabel(result);
  }
  lookupGuid(result: JsonObject): string { const value = result['Guid'] ?? result['guid']; return typeof value === 'string' ? value.trim() : ''; }
}
