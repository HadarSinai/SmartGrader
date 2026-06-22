import { Injectable } from '@angular/core';

export interface AccessibilityState {
  scale: number;
  highContrast: boolean;
  highlightLinks: boolean;
  reduceMotion: boolean;
}

const KEY = 'sg_accessibility';

@Injectable({ providedIn: 'root' })
export class AccessibilityService {
  state: AccessibilityState = {
    scale: 1,
    highContrast: false,
    highlightLinks: false,
    reduceMotion: false
  };

  init() {
    try {
      const saved = localStorage.getItem(KEY);
      if (saved) this.state = { ...this.state, ...JSON.parse(saved) };
    } catch {}
    this.apply();
  }

  update(patch: Partial<AccessibilityState>) {
    this.state = { ...this.state, ...patch };
    try { localStorage.setItem(KEY, JSON.stringify(this.state)); } catch {}
    this.apply();
  }

  private apply() {
    document.body.style.fontSize = `${this.state.scale * 100}%`;
    document.body.classList.toggle('a11y-contrast', this.state.highContrast);
    document.body.classList.toggle('a11y-links', this.state.highlightLinks);
    document.body.classList.toggle('a11y-reduce-motion', this.state.reduceMotion);
  }
}
