import { Controller, Post, Body } from '@nestjs/common';
import { AuthService, TokenResponse } from './auth.service';

class LoginDto {
  username!: string;
  password!: string;
}

@Controller('api/auth')
export class AuthController {
  constructor(private readonly authService: AuthService) {}

  @Post('login')
  async login(@Body() body: LoginDto): Promise<TokenResponse> {
    return this.authService.login(body.username, body.password);
  }
}
