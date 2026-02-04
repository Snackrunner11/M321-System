import { Component, OnInit, OnDestroy, NgZone } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { OAuthService } from 'angular-oauth2-oidc';
import { authConfig } from './app.config';

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
  
  private apiUrl = ''; 
  private socket: WebSocket | undefined;

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

  constructor(
    private http: HttpClient, 
    private oauthService: OAuthService,
    private ngZone: NgZone 
  ) {
    this.configureAuth();
  }

  private configureAuth() {
    this.oauthService.configure(authConfig);
    this.oauthService.loadDiscoveryDocumentAndLogin();
  }

  ngOnInit() {
    this.isDevMode = window.location.hostname === 'localhost';
    this.initializeEmptyBoard();
    this.connectWebSocket(); 
  }

  ngOnDestroy() {
    if (this.socket) {
      this.socket.close();
    }
  }

  private initializeEmptyBoard() {
    this.board = [];
    for (let y = 0; y < 16; y++) {
      this.board[y] = [];
      for (let x = 0; x < 16; x++) {
        this.board[y][x] = { 
          Red: 30, Green: 30, Blue: 30,
          r: 30, g: 30, b: 30 
        };
      }
    }
  }

  private connectWebSocket() {
    const protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
    const wsUrl = `${protocol}//${window.location.host}/ws-pixels`;

    console.log('Verbinde WebSocket:', wsUrl);
    this.socket = new WebSocket(wsUrl);

    this.socket.onopen = () => {
      console.log('WebSocket verbunden!');
      this.socket?.send('subscribe');
    };

    this.socket.onmessage = (event) => {
      this.ngZone.run(() => {
        this.parsePixelMessage(event.data.toString());
      });
    };

    this.socket.onerror = (err) => {
      console.error('WebSocket Fehler:', err);
    };

    this.socket.onclose = () => {
      setTimeout(() => this.connectWebSocket(), 3000);
    };
  }

  private parsePixelMessage(msg: string) {
    const lines = msg.split('\n');
    for (const line of lines) {
      const parts = line.trim().split(' ');
      if (parts.length === 5) {
        const x = parseInt(parts[0]);
        const y = parseInt(parts[1]);
        const r = parseInt(parts[2]);
        const g = parseInt(parts[3]);
        const b = parseInt(parts[4]);
        
        if (!isNaN(x) && !isNaN(y)) {
          this.updateLocalPixel(x, y, { Red: r, Green: g, Blue: b });
        }
      }
    }
  }

  onLeftClick(x: number, y: number) {
    const team = this.selectedTeam;
    if (!team) return;

    if (!this.oauthService.hasValidAccessToken()) {
      console.warn("Nicht eingeloggt! Login wird gestartet...");
      this.oauthService.initLoginFlow();
      return;
    }

    this.updateLocalPixel(x, y, team.Color);

    const payload = { 
      X: x, 
      Y: y, 
      Team: team.ID 
    };
    
    const headers = this.getAuthHeaders();
    this.http.post(`${this.apiUrl}/api/color`, payload, { headers }).subscribe({
      next: () => {},
      error: (err) => console.error('Fehler beim Zeichnen:', err)
    });
  }
  
  registerPlayer(gamerTag: string) {
    if (!gamerTag) return;
    const payload = { Name: gamerTag };
    const headers = this.getAuthHeaders();
    
    this.http.post(`${this.apiUrl}/api/player/register`, payload, { headers }).subscribe({
      next: () => alert('Spieler erfolgreich registriert!'),
      error: (err) => {
        console.error(err);
        alert('Fehler bei Spieler-Registrierung (siehe Konsole)');
      }
    });
  }

  registerTeam(teamName: string) {
    if (!teamName) return;
    const payload = JSON.stringify(teamName); 
    let headers = this.getAuthHeaders().set('Content-Type', 'application/json');

    this.http.post(`${this.apiUrl}/api/team/register`, payload, { headers }).subscribe({
      next: () => alert('Team erfolgreich registriert!'),
      error: (err) => {
        console.warn('POST fehlgeschlagen, versuche PUT...', err);
        this.http.put(`${this.apiUrl}/api/team/name`, payload, { headers }).subscribe({
            next: () => alert('Team Name (PUT) erfolgreich!'),
            error: (e) => alert('Fehler bei Team-Registrierung')
        });
      }
    });
  }

  private getAuthHeaders(): HttpHeaders {
    const token = this.oauthService.getAccessToken();
    let headers = new HttpHeaders();
    if (token) {
      headers = headers.set('Authorization', 'Bearer ' + token);
    }
    return headers;
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
    if (!this.board[y]) this.board[y] = []; 
    if (!this.board[y][x]) {
        this.board[y][x] = { ...color, r: color.Red, g: color.Green, b: color.Blue };
    }

    const p = this.board[y][x];
    p.Red = color.Red; p.Green = color.Green; p.Blue = color.Blue;
    p.r = color.Red; p.g = color.Green; p.b = color.Blue;
    p.red = color.Red; p.green = color.Green; p.blue = color.Blue;
  }
}