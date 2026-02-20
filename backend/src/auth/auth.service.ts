import { Injectable, UnauthorizedException } from '@nestjs/common';
import { HttpService } from '@nestjs/axios';
import { ConfigService } from '@nestjs/config';
import { firstValueFrom } from 'rxjs';

export interface TokenResponse {
  access_token: string;
  expires_in: number;
  refresh_expires_in: number;
  refresh_token: string;
  token_type: string;
}

@Injectable()
export class AuthService {
  constructor(
    private readonly httpService: HttpService,
    private readonly configService: ConfigService,
  ) {}

  async login(username: string, passwort: string): Promise<TokenResponse> {
    const tokenUrl = this.configService.get<string>('KEYCLOAK_TOKEN_URL');
    const clientId = this.configService.get<string>('KEYCLOAK_CLIENT_ID');
    const clientSecret = this.configService.get<string>(
      'KEYCLOAK_CLIENT_SECRET',
    );

    if (!tokenUrl || !clientId) {
      throw new Error('Keycloak Konfiguration fehlt in den Umgebungsvariablen');
    }

    const params = new URLSearchParams();
    params.append('grant_type', 'password');
    params.append('client_id', clientId);
    if (clientSecret) {
      params.append('client_secret', clientSecret);
    }
    params.append('username', username);
    params.append('password', passwort);

    try {
      const response = await firstValueFrom(
        this.httpService.post<TokenResponse>(tokenUrl, params.toString(), {
          headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        }),
      );
      return response.data;
    } catch {
      throw new UnauthorizedException('Ungültige Zugangsdaten');
    }
  }
}
