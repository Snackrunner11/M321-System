import { Injectable } from '@nestjs/common';
import { PassportStrategy } from '@nestjs/passport';
import { ExtractJwt, Strategy } from 'passport-jwt';
import { passportJwtSecret } from 'jwks-rsa';

// Wir definieren genau, wie das Token aussieht, um ESLint glücklich zu machen
interface JwtPayload {
  sub: string;
  preferred_username?: string;
  email?: string;
  realm_access?: {
    roles?: string[];
  };
  team?: number;
}

@Injectable()
export class JwtStrategy extends PassportStrategy(Strategy) {
  constructor() {
    super({
      secretOrKeyProvider: passportJwtSecret({
        cache: true,
        rateLimit: true,
        jwksRequestsPerMinute: 5,
        jwksUri:
          'http://localhost:18080/realms/pixelboard-test/protocol/openid-connect/certs',
      }),
      jwtFromRequest: ExtractJwt.fromAuthHeaderAsBearerToken(),
      issuer: 'http://localhost:18080/realms/pixelboard-test',
      algorithms: ['RS256'],
      ignoreExpiration: false,
    });
  }

  // 'async' entfernt, da wir kein 'await' nutzen
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