import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideOAuthClient, AuthConfig } from 'angular-oauth2-oidc';

import { routes } from './app.routes';

export const authConfig: AuthConfig = {
  // Der Pfad für den Proxy
  issuer: '/realms/pixelboard-test',
  
  redirectUri: window.location.origin,
  clientId: 'student_client',
  responseType: 'code',
  scope: 'openid profile',
  requireHttps: false, 
  showDebugInformation: true,
  
  // WICHTIG: Das hier löst Ihren "invalid issuer" Fehler!
  // Es erlaubt Angular, die Antwort von Keycloak zu akzeptieren, 
  // auch wenn die URL (localhost:18080) leicht anders aussieht als angefragt (/realms/...).
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