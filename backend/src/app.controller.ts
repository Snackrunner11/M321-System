import {
  Controller,
  Get,
  Post,
  Body,
  OnModuleInit,
  Logger,
  UseGuards,
  Request,
} from '@nestjs/common';
import { AuthGuard } from '@nestjs/passport';
import { AppService, Team } from './app.service';
import { BoardStateService } from './board-state.service';
import { EventsGateway } from './events.gateway';

@Controller('api')
export class AppController implements OnModuleInit {
  private readonly logger = new Logger(AppController.name);

  constructor(
    private readonly appService: AppService,
    private readonly boardStateService: BoardStateService,
    private readonly eventsGateway: EventsGateway,
  ) {}

  async onModuleInit() {
    await this.appService.getTeams();
  }

  // JEDER darf die Teams sehen
  @Get('teams')
  async getTeams(): Promise<Team[]> {
    return await this.appService.getTeams();
  }

  // JEDER darf das Board sehen
  @Get('board')
  getBoard() {
    return {
      board: this.boardStateService.getBoard(),
      duration: this.boardStateService.getLastDuration(),
    };
  }

  // NUR EINGELOGGTE USER dürfen malen
  // Das beantwortet die Frage nach dem Login Zwang
  @UseGuards(AuthGuard('jwt'))
  @Post('pixel')
  async setPixel(
    @Body() body: { x: number; y: number; teamId: number },
    @Request() req: any, // 'any' verhindert Typfehler beim Zugriff auf .user
  ) {
    // HIER IST DER FIX:
    // Wir greifen auf 'req.user' zu. Damit ist 'req' benutzt und der Fehler ist weg.
    // Gleichzeitig siehst du im Server Log, wer gemalt hat.
    const user = req.user;
    this.logger.log(`Pixel gesetzt von User: ${user?.username || 'Unbekannt'}`);

    await this.appService.setPixel(body.x, body.y, body.teamId);

    const teams = await this.appService.getTeams();

    const team = teams.find(
      (t: Team) =>
        Number(t.ID) === Number(body.teamId) ||
        Number(t.id) === Number(body.teamId),
    );

    if (!team) {
      this.logger.warn(
        `ACHTUNG: Team ID ${body.teamId} nicht gefunden! Sende Schwarz.`,
      );
    }

    const currentColor = team ? team.Color : { Red: 0, Green: 0, Blue: 0 };

    this.boardStateService.updateCachePixel(body.x, body.y, currentColor);

    this.eventsGateway.broadcastPixelUpdate(
      body.x,
      body.y,
      currentColor as unknown as Record<string, any>,
    );

    return { status: 'ok' };
  }
}