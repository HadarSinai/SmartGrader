import { Component, EventEmitter, Output } from "@angular/core";
import { Router, RouterModule } from "@angular/router";
import { AvatarModule } from "primeng/avatar";
import { ButtonModule } from "primeng/button";
import { ToolbarModule } from "primeng/toolbar";
import { TooltipModule } from "primeng/tooltip";
import { AuthService } from "../../services/auth.service";

@Component({
  selector: "app-topbar",
  standalone: true,
  imports: [
    ButtonModule,
    AvatarModule,
    RouterModule,
    ToolbarModule,
    TooltipModule,
  ],
  template: `
    <p-toolbar class="sg-topbar" aria-label="סרגל עליון">
      <div class="p-toolbar-group-left"></div>

      <div class="p-toolbar-group-center">
        @if (auth.isTeacher()) {
          <nav class="sg-nav" aria-label="ניווט ראשי">
            <a
              routerLink="/"
              routerLinkActive="active"
              [routerLinkActiveOptions]="{ exact: true }"
              >לוח בקרה</a
            >
            <a routerLink="/students" routerLinkActive="active">סטודנטים</a>
            <a routerLink="/assignments" routerLinkActive="active">תרגילים</a>
            <a routerLink="/lessons" routerLinkActive="active">שיעורים</a>
            <a routerLink="/submissions" routerLinkActive="active">הגשות</a>
          </nav>
        }
      </div>

      <div class="p-toolbar-group-left flex align-items-center gap-2">
        <p-button
          icon="pi pi-bell"
          [text]="true"
          [rounded]="true"
          severity="secondary"
          ariaLabel="התראות"
          pTooltip="התראות יהיו זמינות בקרוב"
          tooltipPosition="bottom"
        >
        </p-button>
        <div class="flex align-items-center gap-2">
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
