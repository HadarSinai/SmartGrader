import { bootstrapApplication } from '@angular/platform-browser';
import { AppComponent } from './app/app.component';
import { appConfig } from './app/app.config';

// Apply saved accessibility preferences ASAP (prevents theme flash)
try {
  const theme = localStorage.getItem('theme');
  document.body.classList.toggle('dark', theme === 'dark');

  const reduceMotion = localStorage.getItem('reduceMotion');
  document.body.classList.toggle('reduced-motion', reduceMotion === '1');

  const fontScale = localStorage.getItem('fontScale');
  document.documentElement.style.fontSize = fontScale === 'large' ? '15.5px' : '14px';
} catch {
  // ignore
}

bootstrapApplication(AppComponent, appConfig)
  .catch((err) => console.error(err));
