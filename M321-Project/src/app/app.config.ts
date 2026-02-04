import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideOAuthClient, AuthConfig } from 'angular-oauth2-oidc';

import { routes } from './app.routes';

export const authConfig: AuthConfig = {
  issuer: 'http://localhost:18080/realms/pixelboard-test',
  
  redirectUri: window.location.origin,
  clientId: 'student_client',
  responseType: 'code',
  scope: 'openid profile',
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