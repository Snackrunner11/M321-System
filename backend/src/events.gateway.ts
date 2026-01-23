import {
  WebSocketGateway,
  WebSocketServer,
  OnGatewayInit,
} from '@nestjs/websockets';
import { Server } from 'socket.io';

@WebSocketGateway({
  cors: {
    origin: '*',
  },
})
export class EventsGateway implements OnGatewayInit {
  @WebSocketServer()
  server: Server;

  afterInit() {
    console.log('WebSocket Gateway initialisiert!');
  }

  broadcastPixelUpdate(x: number, y: number, color: Record<string, any>) {
    this.server.emit('pixelUpdate', { x, y, color });
  }
}
