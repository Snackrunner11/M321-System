import { Component, OnInit, OnDestroy } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { io, Socket } from 'socket.io-client';

interface Pixel {
  Red: number;
  Green: number;
  Blue: number;
}

interface Team {
  ID: number;
  Name: string;
  Color: Pixel;
}

interface BoardResponse {
  board: Pixel[][];
  duration: number;
}

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class AppComponent implements OnInit, OnDestroy {
  board: any[][] = [];
  
  isDevMode = false;

  teams: Team[] = [
    { ID: 0, Name: 'Team 1 (Gelb)',       Color: { Red: 255, Green: 255, Blue: 0 } },
    { ID: 1, Name: 'Team 2 (Blau)',       Color: { Red: 0, Green: 0, Blue: 255 } },
    { ID: 2, Name: 'Team 3 (Lila)',       Color: { Red: 128, Green: 0, Blue: 128 } },
    { ID: 3, Name: 'Team 4 (Grün)',       Color: { Red: 0, Green: 128, Blue: 0 } },
    { ID: 4, Name: 'Team 5 (Pink)',       Color: { Red: 255, Green: 150, Blue: 150 } },
    { ID: 5, Name: 'Team 6 (Cyan)',       Color: { Red: 0, Green: 255, Blue: 255 } },
    { ID: 6, Name: 'Team 7 (Oliv)',       Color: { Red: 128, Green: 128, Blue: 0 } },
    { ID: 7, Name: 'Team 8 (Dunkelblau)', Color: { Red: 0, Green: 0, Blue: 139 } },
    { ID: 8, Name: 'Team 9 (Indigo)',     Color: { Red: 75, Green: 0, Blue: 130 } },
    { ID: 9, Name: 'Team 10 (Dunkelgrün)',Color: { Red: 0, Green: 100, Blue: 0 } },
    { ID: 10, Name: 'Team 11 (Altrosa)',  Color: { Red: 180, Green: 100, Blue: 100 } },
    { ID: 11, Name: 'Team 12 (Graublau)', Color: { Red: 95, Green: 158, Blue: 160 } },
    { ID: 12, Name: 'Team 13 (Gold)',     Color: { Red: 120, Green: 120, Blue: 20 } },
    { ID: 13, Name: 'Team 14 (Navy)',     Color: { Red: 0, Green: 0, Blue: 80 } },
    { ID: 14, Name: 'Team 15 (Tief-Lila)',Color: { Red: 48, Green: 25, Blue: 52 } },
    { ID: 15, Name: 'Team 16 (Tief-Grün)',Color: { Red: 20, Green: 50, Blue: 20 } },
  ];

  currentTeamIndex = 0;
  loadingTime = 0;
  private socket: Socket | undefined;

  constructor(private http: HttpClient) {}

  ngOnInit() {
    this.isDevMode = window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1';

    this.loadBoard();
    setInterval(() => this.loadBoard(), 5000);

    this.socket = io('http://localhost:3000');
    this.socket.on('pixelUpdate', (data: { x: number, y: number, color: Pixel }) => {
      this.updateLocalPixel(data.x, data.y, data.color);
    });
  }

  ngOnDestroy() {
    if (this.socket) this.socket.disconnect();
  }

  loadBoard() {
    this.http.get<BoardResponse>('http://localhost:3000/api/board').subscribe({
      next: (data) => {
        if (data.board && data.board.length > 0) {
          this.board = data.board;
          this.loadingTime = data.duration;
        }
      },
      error: (err) => console.error('Fehler:', err)
    });
  }

  onLeftClick(x: number, y: number) {
    const team = this.selectedTeam;
    if (!team) return;

    this.updateLocalPixel(x, y, team.Color);

    const payload = { x, y, teamId: team.ID };
    this.http.post('http://localhost:3000/api/pixel', payload).subscribe({
      next: () => {}, 
      error: (err) => console.error('Fehler:', err)
    });
  }

  onRightClick(event: MouseEvent) {
    event.preventDefault();
    this.currentTeamIndex = (this.currentTeamIndex + 1) % this.teams.length;
  }

  get selectedTeam(): Team {
    return this.teams[this.currentTeamIndex];
  }

  get currentBrushColor(): string {
    const c = this.selectedTeam.Color;
    return `rgb(${c.Red}, ${c.Green}, ${c.Blue})`;
  }

  private updateLocalPixel(x: number, y: number, color: Pixel) {
    if (this.board[y] && this.board[y][x]) {
      const p = this.board[y][x];
      p.Red = color.Red; p.Green = color.Green; p.Blue = color.Blue;
      p.r = color.Red; p.g = color.Green; p.b = color.Blue;
      p.red = color.Red; p.green = color.Green; p.blue = color.Blue;
    }
  }
}
