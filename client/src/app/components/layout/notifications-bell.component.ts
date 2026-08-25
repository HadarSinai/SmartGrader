import { Component, OnInit, ViewChild } from "@angular/core";
import { DatePipe, NgClass } from "@angular/common";
import { RouterModule } from "@angular/router";
import { BadgeModule } from "primeng/badge";
import { ButtonModule } from "primeng/button";
import { OverlayPanel, OverlayPanelModule } from "primeng/overlaypanel";
import { ClassSignalType } from "@models/notification.model";
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
    NgClass,
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
      @if (auth.isStudent()) {
        @if (notifications.graded().length === 0) {
          <div class="sg-notif-empty">אין התראות חדשות</div>
        } @else {
          <ul class="sg-notif-list" role="list">
            @for (n of notifications.graded(); track n.id) {
              <li class="sg-notif-item">
                <a
                  [routerLink]="['/my', 'submissions', n.id]"
                  (click)="notifPanel.hide()"
                >
                  <span class="sg-notif-text">
                    ההגשה שלך בתרגיל "{{ n.assignmentName }}" נבדקה
                  </span>
                  <span class="sg-notif-time">
                    {{ n.submittedAt | date: "dd.MM.yy HH:mm" }}
                  </span>
                </a>
              </li>
            }
          </ul>
        }
      } @else {
        @if (notifications.signals().length === 0) {
          <div class="sg-notif-empty">אין ממצאים מאתמול</div>
        } @else {
          <ul class="sg-notif-list" role="list">
            @for (s of notifications.signals(); track s.key) {
              <li class="sg-notif-item">
                <a
                  class="sg-notif-signal"
                  [routerLink]="['/lessons', s.lessonId, 'assignments']"
                  (click)="notifPanel.hide()"
                >
                  <i
                    class="sg-notif-icon pi"
                    [ngClass]="iconClass(s.type)"
                    aria-hidden="true"
                  ></i>
                  <span class="sg-notif-body">
                    <span class="sg-notif-text">{{ s.message }}</span>
                    <span class="sg-notif-time">{{ s.lessonSubject }}</span>
                  </span>
                </a>
              </li>
            }
          </ul>
        }
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

      /* סיגנל = משפט + אייקון, לא שורת הגשה. השורה היא שורה אחת אופקית
         כדי שהאייקון יישאר צמוד לתחילת המשפט גם ב-RTL. */
      .sg-notif-item a.sg-notif-signal {
        flex-direction: row;
        align-items: flex-start;
        gap: var(--space-2);
      }

      .sg-notif-body {
        display: flex;
        flex-direction: column;
        gap: var(--space-1);
        min-width: 0;
      }

      .sg-notif-icon {
        flex: 0 0 auto;
        margin-top: 0.2rem;
        font-size: 0.95rem;
      }

      /* מה קרה לכיתה מול מה שבור בתרגיל — שני צבעים, לא אחד */
      .sg-notif-icon.sg-class {
        color: var(--app-warning, #b45309);
      }

      .sg-notif-icon.sg-exercise {
        color: var(--app-danger, #b91c1c);
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

  /**
   * שני סוגי סיגנל מובחנים בעין: מה שקרה לכיתה (כתום — משהו ללמד מחדש)
   * מול מה ששבור בתרגיל (אדום — משהו לתקן בניסוח).
   */
  iconClass(type: ClassSignalType): string {
    switch (type) {
      case "StructuralRequirementFailed":
        return "pi-list-check sg-class";
      case "TestCaseFailed":
        return "pi-times-circle sg-class";
      case "NobodyPassed":
        return "pi-exclamation-triangle sg-exercise";
      case "CompilationFailedForMost":
        return "pi-ban sg-exercise";
    }
  }
}
