import { Module } from '@nestjs/common';
import { ConfigModule } from '@nestjs/config'; // <--- NEU
import { AppController } from './app.controller';
import { AppService } from './app.service';
import { AuthModule } from './auth/auth.module';
import { EventsGateway } from './events.gateway';
import { BoardStateService } from './board-state.service';

@Module({
  imports: [
    // Lädt die .env Datei global für die ganze App
    ConfigModule.forRoot({
      isGlobal: true,
    }),
    AuthModule,
  ],
  controllers: [AppController],
  providers: [AppService, EventsGateway, BoardStateService],
})
export class AppModule {}
