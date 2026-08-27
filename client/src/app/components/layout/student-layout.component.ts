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
  templateUrl: "./student-layout.component.html",
  styleUrls: ["./student-layout.component.css"],
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
