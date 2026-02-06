import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideOAuthClient, AuthConfig } from 'angular-oauth2-oidc';

import { routes } from './app.routes';
import { errorInterceptor } from './error.interceptor';
import { environment } from '../environments/environment'; // <--- Importieren

// Die Config holt sich jetzt die Daten aus der environment Datei
export const authConfig: AuthConfig = {
  issuer: environment.keycloak.issuer,
  redirectUri: window.location.origin,
  clientId: environment.keycloak.clientId,
  dummyClientSecret: environment.keycloak.dummyClientSecret,
  
  responseType: 'code',
  scope: 'openid',
  requireHttps: false,
  showDebugInformation: true,
  strictDiscoveryDocumentValidation: false 
};

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(withInterceptors([errorInterceptor])),
    provideOAuthClient()
  ]
};
