import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideOAuthClient, AuthConfig } from 'angular-oauth2-oidc';

import { routes } from './app.routes';
import { errorInterceptor } from './error.interceptor';

export const authConfig: AuthConfig = {
  // Adresse des lokalen Keycloak Servers
  issuer: 'http://localhost:18080/realms/pixelboard-test',
  
  redirectUri: window.location.origin,
  clientId: 'student_client',
  
  // Dein lokales Secret
  dummyClientSecret: 'W3O3oYj1Qny3ATcvE6nrozLe17KyW6e9',

  responseType: 'code',
  
  // WICHTIG: Nur 'openid'. Kein 'profile', kein 'email'.
  scope: 'openid',
  
  requireHttps: false,
  showDebugInformation: true,
  strictDiscoveryDocumentValidation: false 
};

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    
    // Hier wird der Fehler Wächter registriert
    provideHttpClient(withInterceptors([errorInterceptor])),
    
    provideOAuthClient()
  ]
};