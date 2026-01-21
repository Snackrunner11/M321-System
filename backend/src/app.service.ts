import { Injectable } from '@nestjs/common';
import { ConfigService } from '@nestjs/config';

export interface Pixel {
  Red: number;
  Green: number;
  Blue: number;
}

@Injectable()
export class AppService {
  private apiUrl = 'https://edu.jakobmeier.ch/api/board';

  constructor(private configService: ConfigService) {
    const configuredUrl = this.configService.get<string>('API_URL');
    if (configuredUrl) {
      this.apiUrl = configuredUrl;
    }
  }

  async getBoard(): Promise<Pixel[][]> {
    try {
      const response = await fetch(this.apiUrl);
      const text = await response.text();
      if (!text) {
        return [];
      }
      return JSON.parse(text) as Pixel[][];
    } catch {
      return [];
    }
  }
}
