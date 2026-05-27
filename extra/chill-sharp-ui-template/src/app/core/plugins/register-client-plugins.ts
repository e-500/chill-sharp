import { Routes } from '@angular/router';
import { HelloPluginComponent } from './hello-plugin/hello-plugin.component';
import { ClientHomeComponent } from '../../pages/client-home/client-home.component';

export function getClientFeatureRoutes(): Routes {
  return [
    {
      path: 'hello-plugin/:name',
      component: HelloPluginComponent
    },
    {
      path: 'client-home',
      component: ClientHomeComponent
    }
  ];
}
