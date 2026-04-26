import { Routes } from '@angular/router';
import { CHILL_SHARP_UI_ROUTES } from '@chill-sharp/ui-core';
import { getClientFeatureRoutes } from './core/plugins/register-client-plugins';

const coreRoutes = CHILL_SHARP_UI_ROUTES.filter((route) => route.path !== '**');

export const appRoutes: Routes = [
  ...getClientFeatureRoutes(),
  ...coreRoutes,
  {
    path: '**',
    redirectTo: 'login'
  }
];
