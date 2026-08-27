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
  templateUrl: "./topbar.component.html",
  styleUrls: ["./topbar.component.css"],
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
