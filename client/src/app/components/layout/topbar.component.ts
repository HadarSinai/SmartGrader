import { Component, EventEmitter, Output } from "@angular/core";
import { Router, RouterModule } from "@angular/router";
import { AvatarModule } from "primeng/avatar";
import { ButtonModule } from "primeng/button";
import { ToolbarModule } from "primeng/toolbar";
import { TooltipModule } from "primeng/tooltip";
import { AuthService } from "../../services/auth.service";
import { NotificationsBellComponent } from "./notifications-bell.component";

@Component({
  selector: "app-topbar",
  standalone: true,
  imports: [
    ButtonModule,
    AvatarModule,
    RouterModule,
    ToolbarModule,
    TooltipModule,
    NotificationsBellComponent,
  ],
  template: `
    <p-toolbar class="sg-topbar" aria-label="סרגל עליון">
      <div class="p-toolbar-group-left">
        <a class="sg-brand" routerLink="/" aria-label="SmartGrader – דף הבית">
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
        @if (auth.isTeacher() || auth.isAdmin()) {
          <nav class="sg-nav" aria-label="ניווט ראשי">
            <a
              routerLink="/"
              routerLinkActive="active"
              [routerLinkActiveOptions]="{ exact: true }"
            >
              <i class="pi pi-home" aria-hidden="true"></i>
              לוח בקרה
            </a>
            <a routerLink="/students" routerLinkActive="active">
              <i class="pi pi-users" aria-hidden="true"></i>
              סטודנטים
            </a>
            <a routerLink="/classes" routerLinkActive="active">
              <i class="pi pi-building" aria-hidden="true"></i>
              כיתות
            </a>
            <a routerLink="/assignments" routerLinkActive="active">
              <i class="pi pi-file-edit" aria-hidden="true"></i>
              תרגילים
            </a>
            <a routerLink="/courses" routerLinkActive="active">
              <i class="pi pi-bookmark" aria-hidden="true"></i>
              מקצועות
            </a>
            <a routerLink="/lessons" routerLinkActive="active">
              <i class="pi pi-book" aria-hidden="true"></i>
              שיעורים
            </a>
            <a routerLink="/submissions" routerLinkActive="active">
              <i class="pi pi-inbox" aria-hidden="true"></i>
              הגשות
            </a>
            @if (auth.isAdmin()) {
              <a routerLink="/teachers" routerLinkActive="active">
                <i class="pi pi-id-card" aria-hidden="true"></i>
                מורות
              </a>
              <a routerLink="/logs" routerLinkActive="active">
                <i class="pi pi-history" aria-hidden="true"></i>
                יומן מערכת
              </a>
            }
          </nav>
        }
      </div>

      <div class="p-toolbar-group-left flex align-items-center gap-2">
        @if (auth.isTeacher() || auth.isAdmin()) {
          <app-notifications-bell></app-notifications-bell>
        }
        <div class="flex align-items-center gap-2">
          <!-- האווטר והשם הם הכניסה לחשבון האישי — המוסכמה המוכרת מכל אפליקציה.
               כפתור ה-pi-user שלידם הוא אותו יעד בדיוק, גלוי לעין למי שלא מנחשת
               שאפשר ללחוץ על השם. -->
          <a
            class="sg-topbar-identity"
            routerLink="/profile"
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
            routerLink="/profile"
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
      </div>
    </p-toolbar>
  `,
  styles: [
    `
      .sg-topbar-identity {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        text-decoration: none;
        border-radius: var(--radius-md, 8px);
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
export class TopbarComponent {
  @Output() menuClick = new EventEmitter<void>();

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
