import { Provider } from '@angular/core';
import { getClientOverrideProviders } from '../overrides/register-client-overrides';

export function provideClientTemplateProviders(): Provider[] {
  return [
    ...getClientOverrideProviders()
  ];
}
