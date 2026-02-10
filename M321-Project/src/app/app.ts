import { Component, OnInit, OnDestroy } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { OAuthService } from 'angular-oauth2-oidc';
import { authConfig } from './app.config';
import { FormsModule } from '@angular/forms';
import { io, Socket } from 'socket.io-client';

interface Pixel { Red: number; Green: number; Blue: number; }
interface Team { ID: number; Name: string; Color: Pixel; }

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class AppComponent implements OnInit, OnDestroy {
  board: any[][] = [];
  isDevMode = false;
  
  loadingTime = 0;
  errorMessage: string = '';
  successMessage: string = '';
  
  private apiUrl = 'http://localhost:3000'; 
  
  public socket!: Socket;

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

  constructor(private http: HttpClient, public oauthService: OAuthService) {
    this.configureAuth();
  }

  private async configureAuth() {
    this.oauthService.configure(authConfig);
    await this.oauthService.loadDiscoveryDocumentAndTryLogin();
  }

  ngOnInit() {
    this.isDevMode = window.location.hostname === 'localhost';
    this.initializeEmptyBoard();
    
    this.loadInitialBoard();

    this.connectToWebSocket();
  }

  ngOnDestroy() {
    if (this.socket) {
      this.socket.disconnect();
    }
  }

  private connectToWebSocket() {
    this.socket = io(this.apiUrl, {
      transports: ['websocket'] 
    });

    this.socket.on('connect', () => {
      console.log('✅ Live-Verbindung zum Backend steht!');
    });

    this.socket.on('pixelUpdate', (data: { x: number, y: number, color: Pixel }) => {
      this.updateLocalPixel(data.x, data.y, data.color);
    });

    this.socket.on('disconnect', () => {
      console.warn('Verbindung zum Backend verloren.');
    });
  }

  private checkTokenAndRefresh(): boolean {
    if (!this.oauthService.hasValidAccessToken()) {
        console.warn('Kein gültiges Token vorhanden. Starte Login...');
        this.oauthService.initCodeFlow();
        return false;
    }

    const expiration = this.oauthService.getAccessTokenExpiration(); 
    const now = Date.now();
    
    const timeLeft = (expiration - now) / 1000;

    if (timeLeft < 60 && timeLeft > 0) {
      console.warn(`⚠️ Token läuft in ${timeLeft.toFixed(0)} Sekunden ab!`);
    }

    if (timeLeft <= 5) {
      console.error('❌ Token ist abgelaufen! Erneuere Session...');
      this.oauthService.initCodeFlow();
      return false;
    }

    return true;
  }

  onLeftClick(x: number, y: number) {
    this.errorMessage = '';
    
    if (!this.checkTokenAndRefresh()) {
      return; 
    }

    const claims = this.userClaims;
    const myTeamId = claims ? claims['team'] : null;

    if (myTeamId === null || myTeamId === undefined) {
      this.errorMessage = "Fehler: Kein Team gefunden! Bitte neu einloggen.";
      return;
    }

    const myTeam = this.teams.find(t => t.ID == myTeamId);
    if (myTeam) this.updateLocalPixel(x, y, myTeam.Color);

    const payload = { x: x, y: y, teamId: myTeamId };
    
    const headers = this.getAuthHeaders().set('Content-Type', 'application/json');
    
    this.http.post(`${this.apiUrl}/api/pixel`, payload, { headers }).subscribe({
      next: () => {}, 
      error: (err) => {
        console.error('Fehler:', err);
        this.errorMessage = "Fehler beim Senden";
      }
    });
  }

  private loadInitialBoard() {
    this.http.get<any>(`${this.apiUrl}/api/board`).subscribe({
      next: (res) => {
        if (res.board) {
            this.board = res.board;
        }
      },
      error: (err) => console.error('Fehler beim Laden des Boards:', err)
    });
  }
  
  hardReset() {
    this.oauthService.logOut();
    localStorage.clear();
    sessionStorage.clear();
    window.location.reload();
  }

  get userClaims(): any {
    return this.oauthService.getIdentityClaims();
  }

  get selectedTeam(): Team {
    const claims = this.userClaims;
    if (claims && claims['team'] !== undefined) {
      const found = this.teams.find(t => t.ID == claims['team']);
      if (found) return found;
    }
    return { ID: -1, Name: 'Kein Team', Color: { Red: 128, Green: 128, Blue: 128 } };
  }

  get currentBrushColor(): string {
    const c = this.selectedTeam.Color;
    return `rgb(${c.Red}, ${c.Green}, ${c.Blue})`;
  }

  registerPlayer(gamerTag: string) {
    if (!gamerTag) return;
    if (!this.checkTokenAndRefresh()) return; 

    const payload = { Name: gamerTag };
    this.http.post(`${this.apiUrl}/api/player/register`, payload, { 
      headers: this.getAuthHeaders().set('Content-Type', 'application/json'), 
      responseType: 'text' 
    }).subscribe({
      next: () => this.successMessage = `Registriert!`,
      error: (err) => this.handleError(err)
    });
  }

  registerTeam(teamName: string) {
    if (!teamName) return;
    if (!this.checkTokenAndRefresh()) return;

    const payload = JSON.stringify(teamName);
    const headers = this.getAuthHeaders().set('Content-Type', 'application/json');
    this.http.post(`${this.apiUrl}/api/team/register`, payload, { headers, responseType: 'text' }).subscribe({
      next: () => this.successMessage = `Team registriert!`,
      error: (err) => {
        this.http.put(`${this.apiUrl}/api/team/name`, payload, { headers, responseType: 'text' }).subscribe({
            next: () => this.successMessage = `Team umbenannt!`,
            error: (e) => this.handleError(e)
        });
      }
    });
  }

  onRightClick(event: MouseEvent) {
    event.preventDefault();
  }

  private handleError(err: any) {
    if (err.status === 200) return;
    this.errorMessage = err.error || err.message || "Fehler";
  }

  private getAuthHeaders(): HttpHeaders {
    const token = this.oauthService.getAccessToken();
    return new HttpHeaders().set('Authorization', 'Bearer ' + token);
  }

  private initializeEmptyBoard() {
    this.board = [];
    for (let y = 0; y < 16; y++) {
      this.board[y] = [];
      for (let x = 0; x < 16; x++) {
        this.board[y][x] = { Red: 30, Green: 30, Blue: 30 };
      }
    }
  }

  private updateLocalPixel(x: number, y: number, color: Pixel) {
    if (this.board[y] && this.board[y][x]) {
       this.board[y][x] = { ...color }; 
    }
  }
}