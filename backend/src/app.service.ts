import { Injectable, Logger } from '@nestjs/common';
import { ConfigService } from '@nestjs/config';

export interface Pixel {
  Red: number;
  Green: number;
  Blue: number;
}

@Injectable()
export class AppService {
  private readonly logger = new Logger(AppService.name);
  private apiUrl = 'http://localhost:5085/api/color';

  constructor(private configService: ConfigService) {
    const configuredUrl = this.configService.get<string>('API_URL');
    if (configuredUrl) {
      this.apiUrl = configuredUrl.replace('/board', '/color');
    }
  }

  async getBoard(): Promise<Pixel[][]> {
    const start = performance.now();
    this.logger.log('Starte schnellen Download (Parallel)...');

    const allPromises: Promise<Pixel>[] = [];
    for (let y = 0; y < 16; y++) {
      for (let x = 0; x < 16; x++) {
        allPromises.push(this.getSinglePixel(x, y));
      }
    }

    const allPixels = await Promise.all(allPromises);

    const board: Pixel[][] = [];
    for (let y = 0; y < 16; y++) {
      const row = allPixels.slice(y * 16, (y + 1) * 16);
      board.push(row);
    }

    const end = performance.now();
    this.logger.log(`Fertig! Dauer: ${((end - start) / 1000).toFixed(2)} Sekunden`);
    return board;
  }

  private async getSinglePixel(x: number, y: number): Promise<Pixel> {
    try {
      const response = await fetch(`${this.apiUrl}/${x}/${y}`);
      if (!response.ok) {
        return { Red: 0, Green: 0, Blue: 0 };
      }

      return await response.json() as Pixel;
    } catch {
      return { Red: 0, Green: 0, Blue: 0 };
    }
  }
}
