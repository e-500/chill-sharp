import { environment } from '../environments/environment';

export interface ClientAppConfig {
  appName: string;
  apiBaseUrl: string;
  tenantCode: string;
  themeName: string;
  supportEmail: string;
}

export const CLIENT_APP_CONFIG: ClientAppConfig = {
  appName: 'ChillSharp Client Template',
  apiBaseUrl: environment.apiBaseUrl,
  tenantCode: 'template-client',
  themeName: 'template-default',
  supportEmail: 'support@example.com'
};
