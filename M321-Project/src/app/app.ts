import { Component, OnInit } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { OAuthService } from 'angular-oauth2-oidc';
import { authConfig } from './app.config';
import { FormsModule } from '@angular/forms'; 

interface Pixel { Red: number; Green: number; Blue: number; }
interface Team { ID: number; Name: string; Color: Pixel; }

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
  
  loadingTime = 0;
  errorMessage: string = '';
  successMessage: string = '';
  
  // Leer lassen, damit der Proxy (localhost:4200 -> localhost:5085) genutzt wird
  private apiUrl = ''; 

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

    if (this.oauthService.hasValidAccessToken()) {
      console.log('✅ Login erfolgreich! Token bereit.');
    }
  }

  ngOnInit() {
    this.isDevMode = window.location.hostname === 'localhost';
    this.initializeEmptyBoard();
  }

  // --- HILFSFUNKTIONEN ---
  
  // Falls Tokens klemmen: Alles löschen
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
    // Fallback, falls noch kein Team zugewiesen ist
    return { ID: -1, Name: 'Kein Team', Color: { Red: 128, Green: 128, Blue: 128 } };
  }

  get currentBrushColor(): string {
    const c = this.selectedTeam.Color;
    return `rgb(${c.Red}, ${c.Green}, ${c.Blue})`;
  }

  // --- API AKTIONEN ---

  onLeftClick(x: number, y: number) {
    this.errorMessage = '';

    if (!this.oauthService.hasValidAccessToken()) {
      this.oauthService.initCodeFlow();
      return;
    }

    const claims = this.userClaims;
    const myTeamId = claims ? claims['team'] : null;

    if (myTeamId === null || myTeamId === undefined) {
      this.errorMessage = "Fehler: Kein Team im Token! Bist du in Keycloak der Gruppe zugewiesen?";
      return;
    }

    // 1. Lokal sofort malen (für Geschwindigkeit)
    const myTeam = this.teams.find(t => t.ID == myTeamId);
    if (myTeam) this.updateLocalPixel(x, y, myTeam.Color);

    // 2. An Server senden
    const payload = { X: x, Y: y, Team: myTeamId };
    // Content-Type ist hier wichtig, damit der Server das JSON versteht
    const headers = this.getAuthHeaders().set('Content-Type', 'application/json');
    
    this.http.post(`${this.apiUrl}/api/color`, payload, { headers, responseType: 'text' }).subscribe({
      next: (response) => console.log('Pixel gesendet:', response),
      error: (err) => {
        console.error('Fehler beim Malen:', err);
        if (err.status !== 200) {
            this.errorMessage = "Server Fehler: " + (err.error || err.message || err.statusText);
        }
      }
    });
  }

  registerPlayer(gamerTag: string) {
    if (!gamerTag) return;
    this.errorMessage = '';
    
    const payload = { Name: gamerTag };
    const headers = this.getAuthHeaders(); // Content-Type wird von Angular bei Objekten oft automatisch gesetzt, aber sicherheitshalber:
    
    this.http.post(`${this.apiUrl}/api/player/register`, payload, { 
      headers: headers.set('Content-Type', 'application/json'), 
      responseType: 'text' 
    }).subscribe({
      next: () => {
        this.successMessage = `Spieler '${gamerTag}' registriert!`;
        // Token erneuern, falls der Server Claims ändert (selten nötig, aber gut)
        // this.oauthService.initCodeFlow(); 
      },
      error: (err) => this.handleError(err)
    });
  }

  registerTeam(teamName: string) {
    if (!teamName) return;
    this.errorMessage = '';
    
    // API erwartet einen String als JSON Body: "TeamName"
    const payload = JSON.stringify(teamName);
    const headers = this.getAuthHeaders().set('Content-Type', 'application/json');

    this.http.post(`${this.apiUrl}/api/team/register`, payload, { headers, responseType: 'text' }).subscribe({
      next: () => this.successMessage = `Team '${teamName}' registriert!`,
      error: (err) => {
        // Falls POST fehlschlägt (Team existiert schon), versuchen wir PUT (Namen ändern)
        console.log("POST fehlgeschlagen, versuche PUT...");
        this.http.put(`${this.apiUrl}/api/team/name`, payload, { headers, responseType: 'text' }).subscribe({
            next: () => this.successMessage = `Team Name in '${teamName}' geändert!`,
            error: (e) => this.handleError(e)
        });
      }
    });
  }

  onRightClick(event: MouseEvent) {
    event.preventDefault(); // Kontextmenü unterdrücken
  }

  // --- HELPER ---

  private handleError(err: any) {
    if (err.status === 200) return; // Manchmal wirft Angular Fehler trotz 200 OK beim Parsen
    this.errorMessage = err.error || err.message || "Unbekannter Fehler";
  }

  private getAuthHeaders(): HttpHeaders {
    const token = this.oauthService.getAccessToken();
    if (!token) console.warn("Achtung: Kein Token gefunden!");
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