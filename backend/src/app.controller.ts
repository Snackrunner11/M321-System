import { Controller, Get } from '@nestjs/common';
import { AppService, Pixel } from './app.service';

@Controller('api')
export class AppController {
  constructor(private readonly appService: AppService) {}

  @Get('board')
  async getBoard(): Promise<Pixel[][]> {
    return await this.appService.getBoard();
  }
}
