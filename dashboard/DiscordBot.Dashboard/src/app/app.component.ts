import { Component, OnInit } from '@angular/core';
import { LanguageService } from './core/services/language.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit {
  constructor(private language: LanguageService) {}

  ngOnInit(): void {
    this.language.init();
  }
}
