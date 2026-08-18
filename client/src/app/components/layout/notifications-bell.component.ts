import { Component, OnInit, ViewChild } from "@angular/core";
import { DatePipe } from "@angular/common";
import { RouterModule } from "@angular/router";
import { BadgeModule } from "primeng/badge";
import { ButtonModule } from "primeng/button";
import { OverlayPanel, OverlayPanelModule } from "primeng/overlaypanel";
import { AuthService } from "../../services/auth.service";
import { NotificationsService } from "../../services/notifications.service";

@Component({
  selector: "app-notifications-bell",
  standalone: true,
  imports: [
    ButtonModule,
    BadgeModule,
    OverlayPanelModule,
    RouterModule,
    DatePipe,
  ],
  template: `
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
                [routerLink]="
                  auth.isStudent()
                    ? ['/my', 'submissions', n.id]
                    : ['/students', n.studentId, 'submissions', n.id]
                "
                (click)="notifPanel.hide()"
              >
                <span class="sg-notif-text">
                  @if (auth.isStudent()) {
                    ההגשה שלך בתרגיל "{{ n.assignmentName }}" נבדקה
                  } @else {
                    ההגשה של {{ n.studentName }} בתרגיל
                    "{{ n.assignmentName }}" נבדקה
                  }
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
  `,
  styles: [
    `
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
export class NotificationsBellComponent implements OnInit {
  @ViewChild("notifPanel") notifPanel!: OverlayPanel;

  constructor(
    public auth: AuthService,
    public notifications: NotificationsService,
  ) {}

  ngOnInit(): void {
    this.notifications.start();
  }

  toggleNotifications(event: Event): void {
    this.notifPanel?.toggle(event);
  }
}
