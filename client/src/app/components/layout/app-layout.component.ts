import { CommonModule } from "@angular/common";
import { Component } from "@angular/core";
import { NavigationEnd, Router, RouterModule } from "@angular/router";
import { filter } from "rxjs/operators";
import { TopbarComponent } from "./topbar.component";

@Component({
  selector: "app-layout",
  standalone: true,
  imports: [CommonModule, RouterModule, TopbarComponent],
  template: `
    <div class="sg-shell min-h-screen relative overflow-x-hidden">
      <header class="sg-header">
        <div class="sg-page">
          <app-topbar></app-topbar>
        </div>
      </header>

      <!-- Compact welcome strip — Dashboard only -->
      <section *ngIf="isDashboard" class="sg-welcome" aria-label="קבלת פנים">
        <div class="sg-page">
          <div class="sg-welcome-card">
            <div class="sg-welcome-title">שלום, טוב לראות אותך שוב</div>
            <div class="sg-welcome-sub">
              הנה תמונת המצב העדכנית של הכיתה שלך
            </div>
          </div>
        </div>
      </section>

      <main class="p-0" aria-label="תוכן">
        <router-outlet></router-outlet>
      </main>

      <footer class="sg-footer" aria-label="פוטר">
        <div class="sg-footer-inner">
          <div class="sg-footer-brand">SmartGrader</div>
          <nav class="sg-footer-links" aria-label="קישורי פוטר">
            <a routerLink="/">לוח בקרה</a>
            <a routerLink="/lessons">שיעורים</a>
            <a routerLink="/students">סטודנטים</a>
          </nav>
        </div>
      </footer>
    </div>
  `,
  styles: [
    `
      .sg-shell {
        background: var(--app-bg);
      }

      .sg-header {
        border-bottom: 1px solid var(--app-border);
        background: var(--app-surface);
        padding-block: var(--space-2);
      }

      .sg-welcome {
        padding-block: var(--space-4) 0;
      }

      .sg-welcome-card {
        background: var(--app-surface-2);
        border: 1px solid var(--app-border);
        border-radius: var(--radius-lg);
        padding: var(--space-4) var(--space-6);
      }

      .sg-welcome-title {
        font-size: var(--text-lg);
        font-weight: 700;
        color: var(--app-text-strong);
      }

      .sg-welcome-sub {
        font-size: var(--text-sm);
        color: var(--app-muted);
        margin-top: var(--space-1);
      }

      .sg-footer-links a {
        color: var(--app-text-strong);
        font-weight: 600;
      }
    `,
  ],
})
export class AppLayoutComponent {
  isDashboard = true;

  constructor(private router: Router) {
    this.router.events
      .pipe(filter((e) => e instanceof NavigationEnd))
      .subscribe((e) => {
        const url = (e as NavigationEnd).urlAfterRedirects;
        this.isDashboard = url === "/" || url === "";
      });
  }
}
