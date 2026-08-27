import { CommonModule } from "@angular/common";
import { Component } from "@angular/core";
import { NavigationEnd, Router, RouterModule } from "@angular/router";
import { filter } from "rxjs/operators";
import { TopbarComponent } from "./topbar.component";

@Component({
  selector: "app-layout",
  standalone: true,
  imports: [CommonModule, RouterModule, TopbarComponent],
  templateUrl: "./app-layout.component.html",
  styleUrls: ["./app-layout.component.css"],
})
export class AppLayoutComponent {
  isDashboard = true;

  constructor(private router: Router) {
    this.router.events
      .pipe(filter((e) => e instanceof NavigationEnd))
      .subscribe((e) => {
        const url = (e as NavigationEnd).urlAfterRedirects;
        this.isDashboard = url === "/" || url === "";
      });
  }
}
