import { CommonModule } from "@angular/common";
import { Component } from "@angular/core";
import { Router, RouterModule } from "@angular/router";
import { AvatarModule } from "primeng/avatar";
import { ButtonModule } from "primeng/button";
import { ToolbarModule } from "primeng/toolbar";
import { TooltipModule } from "primeng/tooltip";
import { AuthService } from "../../services/auth.service";
import { NotificationsBellComponent } from "./notifications-bell.component";

@Component({
  selector: "app-student-layout",
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    ButtonModule,
    AvatarModule,
    ToolbarModule,
    TooltipModule,
    NotificationsBellComponent,
  ],
  template: `
    <div class="sg-shell min-h-screen relative overflow-x-hidden">
      <header class="sg-header">
        <div class="sg-page">
          <p-toolbar class="sg-topbar" aria-label="סרגל עליון">
            <div class="p-toolbar-group-left">
              <a
                class="sg-brand"
                routerLink="/my/lessons"
                aria-label="SmartGrader – המסע שלי"
              >
                <img
                  src="assets/favicon.png"
                  alt=""
                  class="sg-brand-logo"
                  aria-hidden="true"
                />
                <span class="sg-brand-name">SmartGrader</span>
              </a>
            </div>

            <div class="p-toolbar-group-center">
              <nav class="sg-nav" aria-label="ניווט ראשי">
                <a routerLink="/my/lessons" routerLinkActive="active">
                  <i class="pi pi-book" aria-hidden="true"></i>
                  השיעורים שלי
                </a>
                <a routerLink="/my/grades" routerLinkActive="active">
                  <i class="pi pi-chart-line" aria-hidden="true"></i>
                  הציונים שלי
                </a>
              </nav>
            </div>

            <div class="p-toolbar-group-left flex align-items-center gap-2">
              <app-notifications-bell></app-notifications-bell>
              <!-- אותה כניסה בדיוק כמו בסרגל המורה, ליעד /my/profile: שם התלמידה
                   מחליפה סיסמה. -->
              <a
                class="sg-topbar-identity"
                routerLink="/my/profile"
                routerLinkActive="active"
                aria-label="החשבון שלי"
                pTooltip="החשבון שלי"
                tooltipPosition="bottom"
              >
                <p-avatar
                  [label]="avatarInitial()"
                  shape="circle"
                  [style]="{
                    'background-color': 'var(--accent)',
                    color: 'var(--accent-ink)',
                  }"
                >
                </p-avatar>
                <span class="sg-topbar-user">{{ auth.fullName() }}</span>
              </a>
              <p-button
                icon="pi pi-user"
                [text]="true"
                [rounded]="true"
                severity="secondary"
                ariaLabel="החשבון שלי"
                pTooltip="החשבון שלי"
                tooltipPosition="bottom"
                routerLink="/my/profile"
              >
              </p-button>
              <p-button
                icon="pi pi-sign-out"
                [text]="true"
                [rounded]="true"
                severity="secondary"
                ariaLabel="התנתקות"
                pTooltip="התנתקות"
                tooltipPosition="bottom"
                (onClick)="logout()"
              >
              </p-button>
            </div>
          </p-toolbar>
        </div>
      </header>

      <main class="p-0" aria-label="תוכן">
        <router-outlet></router-outlet>
      </main>
    </div>
  `,
  styles: [
    `
      /* ‎.sg-shell‎ ו-‎.sg-header‎ משותפים לשני ה-layouts ויושבים ב-styles.css. */

      .sg-topbar-identity {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        text-decoration: none;
        border-radius: var(--radius-md);
        padding: 0.25rem;
      }

      .sg-topbar-identity:hover .sg-topbar-user,
      .sg-topbar-identity.active .sg-topbar-user {
        color: var(--accent);
      }

      .sg-topbar-user {
        font-weight: 600;
        color: var(--app-text-strong);
        white-space: nowrap;
      }

      @media (max-width: 420px) {
        .sg-topbar-user {
          display: none;
        }
      }
    `,
  ],
})
export class StudentLayoutComponent {
  constructor(
    public auth: AuthService,
    private router: Router,
  ) {}

  avatarInitial(): string {
    return this.auth.fullName().charAt(0) || "?";
  }

  logout(): void {
    this.auth.logout();
    this.router.navigate(["/login"]);
  }
}
