import { Component } from '@angular/core';
import { ChillSharpUiRootComponent } from '@chill-sharp/ui-core';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [ChillSharpUiRootComponent],
  template: '<chill-sharp-ui-root />'
})
export class AppComponent {}
