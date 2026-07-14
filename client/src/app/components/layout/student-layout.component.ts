import { CommonModule } from "@angular/common";
import { Component } from "@angular/core";
import { Router, RouterModule } from "@angular/router";
import { AvatarModule } from "primeng/avatar";
import { ButtonModule } from "primeng/button";
import { ToolbarModule } from "primeng/toolbar";
import { TooltipModule } from "primeng/tooltip";
import { AuthService } from "../../services/auth.service";

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
  ],
  template: `
    <div class="sg-shell min-h-screen relative overflow-x-hidden">
      <header class="sg-header">
        <div class="sg-page">
          <p-toolbar class="sg-topbar" aria-label="סרגל עליון">
            <div class="p-toolbar-group-left"></div>

            <div class="p-toolbar-group-center">
              <nav class="sg-nav" aria-label="ניווט ראשי">
                <a routerLink="/my/lessons" routerLinkActive="active"
                  >השיעורים שלי</a
                >
                <a routerLink="/my/grades" routerLinkActive="active"
                  >הציונים שלי</a
                >
              </nav>
            </div>

            <div class="p-toolbar-group-left flex align-items-center gap-2">
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
      .sg-shell {
        background: var(--app-bg);
      }

      .sg-header {
        border-bottom: 1px solid var(--app-border);
        background: var(--app-surface);
        padding-block: var(--space-2);
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
