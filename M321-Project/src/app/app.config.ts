import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideOAuthClient, AuthConfig } from 'angular-oauth2-oidc';

import { routes } from './app.routes';

export const authConfig: AuthConfig = {
  // Wir verbinden uns mit deinem LOKALEN Keycloak
  issuer: 'http://localhost:18080/realms/pixelboard-test',
  
  redirectUri: window.location.origin,
  clientId: 'student_client',
  
  // Dein lokales Secret, das du gerade gefunden hast:
  dummyClientSecret: 'W3O3oYj1Qny3ATcvE6nrozLe17KyW6e9',

  responseType: 'code',
  scope: 'openid profile',
  
  // Lokal ist HTTPS aus, wichtig für Docker/Localhost
  requireHttps: false, 
  showDebugInformation: true,
  strictDiscoveryDocumentValidation: false 
};

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(),
    provideOAuthClient()
  ]
};