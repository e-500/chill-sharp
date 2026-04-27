import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'chill-sharp-ui-root',
  standalone: true,
  imports: [RouterOutlet],
  template: '<router-outlet />'
})
export class ChillSharpUiRootComponent {}
