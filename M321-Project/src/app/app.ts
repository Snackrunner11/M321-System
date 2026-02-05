import { Component, OnInit } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { OAuthService } from 'angular-oauth2-oidc';
import { authConfig } from './app.config';
import { FormsModule } from '@angular/forms'; 

// Interfaces
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

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class AppComponent implements OnInit {
  board: any[][] = [];
  isDevMode = false;
  
  // HTML Variablen
  loadingTime = 0;
  errorMessage: string = '';
  successMessage: string = '';
  
  // URL zum Backend
  private apiUrl = 'http://localhost:4200'; 

  // --- DEINE TEAMS ---
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

  constructor(
    private http: HttpClient, 
    public oauthService: OAuthService
  ) {
    this.configureAuth();
  }

  private async configureAuth() {
    this.oauthService.configure(authConfig);
    await this.oauthService.loadDiscoveryDocumentAndTryLogin();

    if (this.oauthService.hasValidAccessToken()) {
      console.log('✅ Login erfolgreich! Token vorhanden.');
    }
  }

  ngOnInit() {
    this.isDevMode = window.location.hostname === 'localhost';
    this.initializeEmptyBoard();
  }

  get userClaims(): any {
    return this.oauthService.getIdentityClaims();
  }

  private initializeEmptyBoard() {
    this.board = [];
    for (let y = 0; y < 16; y++) {
      this.board[y] = [];
      for (let x = 0; x < 16; x++) {
        this.board[y][x] = { Red: 30, Green: 30, Blue: 30, r: 30, g: 30, b: 30 };
      }
    }
  }

  // Garantie gegen "Undefined" Fehler
  get selectedTeam(): Team {
    const claims = this.userClaims;
    if (claims && claims['team'] !== undefined) {
      const found = this.teams.find(t => t.ID == claims['team']);
      if (found) return found;
    }
    return this.teams[0];
  }

  get currentBrushColor(): string {
    const t = this.selectedTeam;
    const c = t ? t.Color : { Red: 255, Green: 255, Blue: 255 };
    return `rgb(${c.Red}, ${c.Green}, ${c.Blue})`;
  }

  // --- HIER IST DER FIX (responseType: 'text') ---
  onLeftClick(x: number, y: number) {
    this.errorMessage = '';

    if (!this.oauthService.hasValidAccessToken()) {
      this.oauthService.initCodeFlow();
      return;
    }

    const claims = this.oauthService.getIdentityClaims() as any;
    const myTeamId = claims ? claims['team'] : null;

    if (myTeamId === null || myTeamId === undefined) {
      this.errorMessage = "Fehler: Kein Team im Token!";
      return;
    }

    // Lokales Update
    const myTeam = this.teams.find(t => t.ID == myTeamId);
    if (myTeam) this.updateLocalPixel(x, y, myTeam.Color);

    const payload = { 
      X: x, 
      Y: y, 
      Team: myTeamId 
    };
    
    const headers = this.getAuthHeaders();
    
    // WICHTIG: responseType: 'text' verhindert den "Unexpected token O" Fehler
    this.http.post(`${this.apiUrl}/api/color`, payload, { headers, responseType: 'text' }).subscribe({
      next: (response) => console.log('Pixel erfolgreich gesendet!', response),
      error: (err) => {
        console.error('Fehler:', err);
        // Wir zeigen den Fehler nur an, wenn es KEIN "OK"-Text Fehler ist (falls er doch durchrutscht)
        if (err.status !== 200) {
            this.errorMessage = err.error || err.message;
        }
      }
    });
  }

  registerPlayer(gamerTag: string) {
    if (!gamerTag) return;
    this.errorMessage = '';
    
    const payload = { Name: gamerTag };
    const headers = this.getAuthHeaders();
    
    // Auch hier 'text', falls der Server einfach "Success" oder eine Zahl als Text zurückgibt
    this.http.post(`${this.apiUrl}/api/player/register`, payload, { headers, responseType: 'text' }).subscribe({
      next: () => {
        this.successMessage = `Spieler '${gamerTag}' registriert!`;
        this.oauthService.initCodeFlow();
      },
      error: (err) => this.handleError(err)
    });
  }

  registerTeam(teamName: string) {
    if (!teamName) return;
    this.errorMessage = '';
    
    const payload = JSON.stringify(teamName);
    let headers = this.getAuthHeaders().set('Content-Type', 'application/json');

    this.http.post(`${this.apiUrl}/api/team/register`, payload, { headers, responseType: 'text' }).subscribe({
      next: () => this.successMessage = `Team '${teamName}' registriert!`,
      error: (err) => {
        // Fallback PUT
        this.http.put(`${this.apiUrl}/api/team/name`, payload, { headers, responseType: 'text' }).subscribe({
            next: () => this.successMessage = `Team Name geändert!`,
            error: (e) => this.handleError(e)
        });
      }
    });
  }

  onRightClick(event: MouseEvent) {
    event.preventDefault();
  }

  private handleError(err: any) {
    // Ignoriere Parsing Fehler bei Status 200
    if (err.status === 200) return;
    this.errorMessage = err.error || err.message || "Fehler";
  }

  private getAuthHeaders(): HttpHeaders {
    const token = this.oauthService.getAccessToken();
    return new HttpHeaders().set('Authorization', 'Bearer ' + token);
  }

  private updateLocalPixel(x: number, y: number, color: Pixel) {
    if (this.board[y] && this.board[y][x]) {
       this.board[y][x] = { 
         ...color, 
         r: color.Red, g: color.Green, b: color.Blue,
         red: color.Red, green: color.Green, blue: color.Blue,
         Red: color.Red, Green: color.Green, Blue: color.Blue
       }; 
    }
  }
}