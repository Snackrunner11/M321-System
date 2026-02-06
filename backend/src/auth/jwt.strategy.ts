import { Injectable } from '@nestjs/common';
import { PassportStrategy } from '@nestjs/passport';
import { ExtractJwt, Strategy } from 'passport-jwt';
import { passportJwtSecret } from 'jwks-rsa';
import { ConfigService } from '@nestjs/config'; // <--- NEU

interface JwtPayload {
  sub: string;
  preferred_username?: string;
  email?: string;
  realm_access?: { roles?: string[] };
  team?: number;
}

@Injectable()
export class JwtStrategy extends PassportStrategy(Strategy) {
  // ConfigService im Constructor injizieren
  constructor(private configService: ConfigService) {
    super({
      secretOrKeyProvider: passportJwtSecret({
        cache: true,
        rateLimit: true,
        jwksRequestsPerMinute: 5,
        // Wert aus der .env Datei lesen
        jwksUri: configService.get<string>('KEYCLOAK_JWKS_URI')!,
      }),
      jwtFromRequest: ExtractJwt.fromAuthHeaderAsBearerToken(),
      // Wert aus der .env Datei lesen
      issuer: configService.get<string>('KEYCLOAK_ISSUER'),
      algorithms: ['RS256'],
      ignoreExpiration: false,
    });
  }

  validate(payload: JwtPayload) {
    return {
      userId: payload.sub,
      username: payload.preferred_username,
      email: payload.email,
      roles: payload.realm_access?.roles || [],
      team: payload.team,
    };
  }
}
