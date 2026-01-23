import { Injectable, Logger, OnApplicationBootstrap } from '@nestjs/common';
import { AppService, Pixel } from './app.service';
import { EventsGateway } from './events.gateway';

@Injectable()
export class BoardStateService implements OnApplicationBootstrap {
  private readonly logger = new Logger(BoardStateService.name);
  private boardCache: Pixel[][] = [];

  private lastFetchDuration = 0;
  private pendingUpdates = new Map<string, number>();

  constructor(
    private readonly appService: AppService,
    private readonly eventsGateway: EventsGateway,
  ) {}

  onApplicationBootstrap() {
    this.logger.log('Initialisiere Board Cache...');
    void this.updateLoop();
  }

  private async updateLoop() {
    while (true) {
      try {
        const start = Date.now();
        const remoteBoard = await this.appService.getBoard();
        const duration = Date.now() - start;
        this.lastFetchDuration = duration;

        this.logger.log(`Board aktualisiert in ${duration} ms`);

        if (remoteBoard && remoteBoard.length > 0) {
          this.mergeWithPendingUpdates(remoteBoard);
          this.checkForChangesAndPush(remoteBoard);
          this.boardCache = remoteBoard;
        }
      } catch (error) {
        this.logger.error('Fehler beim Aktualisieren des Caches', error);
      }

      await new Promise((resolve) => setTimeout(resolve, 200));
    }
  }

  getBoard() {
    return this.boardCache;
  }

  getLastDuration() {
    return this.lastFetchDuration;
  }

  private mergeWithPendingUpdates(remoteBoard: Pixel[][]) {
    const now = Date.now();
    this.pendingUpdates.forEach((expiryTime, key) => {
      if (now < expiryTime) {
        const [xStr, yStr] = key.split(',');
        const x = parseInt(xStr, 10);
        const y = parseInt(yStr, 10);
        if (this.boardCache[y] && this.boardCache[y][x]) {
          remoteBoard[y][x] = this.boardCache[y][x];
        }
      } else {
        this.pendingUpdates.delete(key);
      }
    });
  }

  private checkForChangesAndPush(newBoard: Pixel[][]) {
    if (this.boardCache.length === 0) return;
    for (let y = 0; y < 16; y++) {
      for (let x = 0; x < 16; x++) {
        const oldP = this.boardCache[y]?.[x];
        const newP = newBoard[y]?.[x];
        if (oldP && newP && !this.areColorsEqual(oldP, newP)) {
          this.eventsGateway.broadcastPixelUpdate(x, y, newP);
        }
      }
    }
  }

  private areColorsEqual(a: Pixel, b: Pixel): boolean {
    return a.Red === b.Red && a.Green === b.Green && a.Blue === b.Blue;
  }

  updateCachePixel(x: number, y: number, color: Pixel) {
    if (this.boardCache.length > y && this.boardCache[y]) {
      this.boardCache[y][x] = color;
      this.pendingUpdates.set(`${x},${y}`, Date.now() + 2000);
    }
  }
}
