import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SidebarModule } from 'primeng/sidebar';
import { ButtonModule } from 'primeng/button';
import { SliderModule } from 'primeng/slider';
import { InputSwitchModule } from 'primeng/inputswitch';
import { FormsModule } from '@angular/forms';
import { AccessibilityService } from '../../services/accessibility.service';

@Component({
  selector: 'app-accessibility-widget',
  standalone: true,
  imports: [CommonModule, FormsModule, SidebarModule, ButtonModule, SliderModule, InputSwitchModule],
  templateUrl: './accessibility-widget.component.html',
  styleUrls: ['./accessibility-widget.component.css']
})
export class AccessibilityWidgetComponent {
  visible = false;

  // Theme / UI prefs (persisted)
  isDark = false;
  largeText = false;
  reduceMotion = false;

  constructor(public a11y: AccessibilityService) {
    this.isDark = localStorage.getItem('theme') === 'dark' || document.body.classList.contains('dark');
    this.largeText = localStorage.getItem('fontScale') === 'large';
    this.reduceMotion = localStorage.getItem('reduceMotion') === '1' || document.body.classList.contains('reduced-motion');
    this.applyTheme(false);
    this.applyFontScale(false);
    this.applyReducedMotion(false);
  }

  applyTheme(persist: boolean = true): void {
    document.body.classList.toggle('dark', this.isDark);
    if (persist) localStorage.setItem('theme', this.isDark ? 'dark' : 'light');
  }

  applyFontScale(persist: boolean = true): void {
    const fontSize = this.largeText ? '15.5px' : '14px';
    document.documentElement.style.fontSize = fontSize;
    if (persist) localStorage.setItem('fontScale', this.largeText ? 'large' : 'default');
  }

  applyReducedMotion(persist: boolean = true): void {
    document.body.classList.toggle('reduced-motion', this.reduceMotion);
    if (persist) localStorage.setItem('reduceMotion', this.reduceMotion ? '1' : '0');
  }

  reset(): void {
    this.a11y.update({ scale: 1, highContrast: false, highlightLinks: false, reduceMotion: false });
    this.isDark = false;
    this.largeText = false;
    this.reduceMotion = false;
    this.applyTheme();
    this.applyFontScale();
    this.applyReducedMotion();
  }
}
