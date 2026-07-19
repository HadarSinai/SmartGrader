import { Component, OnInit, ViewChild, EventEmitter, Output } from "@angular/core";
import { DatePipe } from "@angular/common";
import { Router, RouterModule } from "@angular/router";
import { AvatarModule } from "primeng/avatar";
import { BadgeModule } from "primeng/badge";
import { ButtonModule } from "primeng/button";
import { OverlayPanel, OverlayPanelModule } from "primeng/overlaypanel";
import { ToolbarModule } from "primeng/toolbar";
import { TooltipModule } from "primeng/tooltip";
import { AuthService } from "../../services/auth.service";
import { NotificationsService } from "../../services/notifications.service";

@Component({
  selector: "app-topbar",
  standalone: true,
  imports: [
    ButtonModule,
    AvatarModule,
    RouterModule,
    ToolbarModule,
    TooltipModule,
    BadgeModule,
    OverlayPanelModule,
    DatePipe,
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
            <a routerLink="/lessons" routerLinkActive="active">
              <i class="pi pi-book" aria-hidden="true"></i>
              שיעורים
            </a>
            <a routerLink="/submissions" routerLinkActive="active">
              <i class="pi pi-inbox" aria-hidden="true"></i>
              הגשות
            </a>
            @if (auth.isAdmin()) {
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
          <p-button
            icon="pi pi-bell"
            [text]="true"
            [rounded]="true"
            severity="secondary"
            ariaLabel="התראות"
            [badge]="
              notifications.unreadCount() > 0
                ? notifications.unreadCount().toString()
                : undefined
            "
            badgeClass="p-badge-danger"
            (onClick)="toggleNotifications($event)"
          >
          </p-button>

          <p-overlayPanel
            #notifPanel
            (onShow)="notifications.markAllRead()"
            styleClass="sg-notif-panel"
          >
            @if (notifications.items().length === 0) {
              <div class="sg-notif-empty">אין התראות חדשות</div>
            } @else {
              <ul class="sg-notif-list" role="list">
                @for (n of notifications.items(); track n.id) {
                  <li class="sg-notif-item">
                    <a
                      [routerLink]="['/students', n.studentId, 'submissions', n.id]"
                      (click)="notifPanel.hide()"
                    >
                      <span class="sg-notif-text">
                        ההגשה של {{ n.studentName }} בתרגיל "{{ n.assignmentName }}" נבדקה
                      </span>
                      <span class="sg-notif-time">
                        {{ n.submittedAt | date: "dd.MM.yy HH:mm" }}
                      </span>
                    </a>
                  </li>
                }
              </ul>
            }
          </p-overlayPanel>
        }
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

      .sg-notif-empty {
        padding: var(--space-4);
        text-align: center;
        color: var(--app-muted);
        font-size: var(--text-sm);
      }

      .sg-notif-list {
        list-style: none;
        margin: 0;
        padding: 0;
        min-width: 320px;
        max-width: 480px;
        max-height: 400px;
        overflow-y: auto;
      }

      .sg-notif-item {
        border-bottom: 1px solid var(--app-border);
      }

      .sg-notif-item:last-child {
        border-bottom: none;
      }

      .sg-notif-item a {
        display: flex;
        flex-direction: column;
        gap: var(--space-1);
        padding: var(--space-3);
        text-decoration: none;
        color: var(--app-text);
        transition: background-color 0.2s;
      }

      .sg-notif-item a:hover {
        background-color: var(--app-surface-2);
      }

      .sg-notif-text {
        font-weight: 500;
        line-height: 1.4;
      }

      .sg-notif-time {
        font-size: var(--text-sm);
        color: var(--app-muted);
      }

      :global(.sg-notif-panel .p-overlaypanel-content) {
        padding: 0;
        border-radius: var(--radius-md);
        box-shadow: var(--shadow-md);
      }
    `,
  ],
})
export class TopbarComponent implements OnInit {
  @Output() menuClick = new EventEmitter<void>();
  @ViewChild("notifPanel") notifPanel!: OverlayPanel;

  constructor(
    public auth: AuthService,
    public notifications: NotificationsService,
    private router: Router,
  ) {}

  ngOnInit(): void {
    this.notifications.start();
  }

  toggleNotifications(event: Event): void {
    this.notifPanel?.toggle(event);
  }

  avatarInitial(): string {
    return this.auth.fullName().charAt(0) || "?";
  }

  logout(): void {
    this.auth.logout();
    this.router.navigate(["/login"]);
  }
}
