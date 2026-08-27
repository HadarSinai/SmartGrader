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
  templateUrl: "./notifications-bell.component.html",
  styleUrls: ["./notifications-bell.component.css"],
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
