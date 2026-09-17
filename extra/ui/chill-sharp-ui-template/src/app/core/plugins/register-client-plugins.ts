import { Routes } from '@angular/router';
import { ClientHomeComponent } from '../../pages/client-home/client-home.component';

export function getClientFeatureRoutes(): Routes {
  return [
    {
      path: 'client-home',
      component: ClientHomeComponent
    }
  ];
}
