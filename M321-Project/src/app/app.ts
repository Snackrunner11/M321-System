import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class AppComponent implements OnInit {
  board: any[][] = [];
  
  selectedPixel: { x: number, y: number } | null = null;

  constructor(private http: HttpClient) {}

  ngOnInit() {
    this.http.get<any[][]>('http://localhost:3000/api/board').subscribe({
      next: (data) => {
        this.board = data;
      },
      error: (err) => console.error('Frontend Fehler:', err)
    });
  }

  onPixelClick(x: number, y: number) {
    this.selectedPixel = { x, y };
    console.log(`Pixel geklickt: ${x}, ${y}`);
  }
}