import { Injectable, Logger } from '@nestjs/common';
import { ConfigService } from '@nestjs/config';

export interface Pixel {
  Red: number;
  Green: number;
  Blue: number;
}

export interface Team {
  ID: number;
  id?: number;
  Name: string;
  Color: Pixel;
}

@Injectable()
export class AppService {
  private readonly logger = new Logger(AppService.name);
  private baseUrl = 'http://localhost:5085/api';

  constructor(private configService: ConfigService) {
    const configuredUrl = this.configService.get<string>('API_URL');
    if (configuredUrl) {
      this.baseUrl = configuredUrl
        .replace('/board', '')
        .replace('/color', '')
        .replace(/\/$/, '');
    }
  }

  getTeams(): Promise<Team[]> {
    return Promise.resolve([
      {
        ID: 0,
        Name: 'Team 1 (Gelb)',
        Color: { Red: 255, Green: 255, Blue: 0 },
      },
      {
        ID: 1,
        Name: 'Team 2 (Blau)',
        Color: { Red: 0, Green: 0, Blue: 255 },
      },
      {
        ID: 2,
        Name: 'Team 3 (Lila)',
        Color: { Red: 128, Green: 0, Blue: 128 },
      },
      {
        ID: 3,
        Name: 'Team 4 (Grün)',
        Color: { Red: 0, Green: 128, Blue: 0 },
      },
      {
        ID: 4,
        Name: 'Team 5 (Pink)',
        Color: { Red: 255, Green: 150, Blue: 150 },
      },
      {
        ID: 5,
        Name: 'Team 6 (Cyan)',
        Color: { Red: 0, Green: 255, Blue: 255 },
      },
      {
        ID: 6,
        Name: 'Team 7 (Oliv)',
        Color: { Red: 128, Green: 128, Blue: 0 },
      },
      {
        ID: 7,
        Name: 'Team 8 (Dunkelblau)',
        Color: { Red: 0, Green: 0, Blue: 139 },
      },
      {
        ID: 8,
        Name: 'Team 9 (Indigo)',
        Color: { Red: 75, Green: 0, Blue: 130 },
      },
      {
        ID: 9,
        Name: 'Team 10 (Dunkelgrün)',
        Color: { Red: 0, Green: 100, Blue: 0 },
      },
      {
        ID: 10,
        Name: 'Team 11 (Altrosa)',
        Color: { Red: 180, Green: 100, Blue: 100 },
      },
      {
        ID: 11,
        Name: 'Team 12 (Graublau)',
        Color: { Red: 95, Green: 158, Blue: 160 },
      },
      {
        ID: 12,
        Name: 'Team 13 (Gold)',
        Color: { Red: 120, Green: 120, Blue: 20 },
      },
      {
        ID: 13,
        Name: 'Team 14 (Navy)',
        Color: { Red: 0, Green: 0, Blue: 80 },
      },
      {
        ID: 14,
        Name: 'Team 15 (Tief-Lila)',
        Color: { Red: 48, Green: 25, Blue: 52 },
      },
      {
        ID: 15,
        Name: 'Team 16 (Tief-Grün)',
        Color: { Red: 20, Green: 50, Blue: 20 },
      },
    ]);
  }

  async setPixel(x: number, y: number, teamId: number): Promise<void> {
    const serverX = y;
    const serverY = x;

    const payload = { X: serverX, Y: serverY, Team: teamId };

    try {
      await fetch(`${this.baseUrl}/color`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      });
    } catch (e) {
      this.logger.error('Fehler beim Senden des Pixels', e);
    }
  }

  async getBoard(): Promise<Pixel[][]> {
    const board: Pixel[][] = [];
    for (let y = 0; y < 16; y++) {
      const rowPromises: Promise<Pixel>[] = [];
      for (let x = 0; x < 16; x++) {
        rowPromises.push(this.getSinglePixel(y, x));
      }
      const rowPixels = await Promise.all(rowPromises);
      board.push(rowPixels);
    }
    return board;
  }

  private async getSinglePixel(x: number, y: number): Promise<Pixel> {
    try {
      const response = await fetch(`${this.baseUrl}/color/${x}/${y}`);
      if (!response.ok) return { Red: 0, Green: 0, Blue: 0 };
      const text = await response.text();
      if (!text) return { Red: 0, Green: 0, Blue: 0 };
      return JSON.parse(text) as Pixel;
    } catch {
      return { Red: 0, Green: 0, Blue: 0 };
    }
  }
}
