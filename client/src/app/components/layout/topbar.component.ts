import { Component, EventEmitter, Output } from "@angular/core";
import { RouterModule } from "@angular/router";
import { AvatarModule } from "primeng/avatar";
import { ButtonModule } from "primeng/button";
import { ToolbarModule } from "primeng/toolbar";

@Component({
  selector: "app-topbar",
  standalone: true,
  imports: [ButtonModule, AvatarModule, RouterModule, ToolbarModule],
  template: `
    <p-toolbar class="sg-topbar" aria-label="סרגל עליון">
      <div class="p-toolbar-group-left"></div>

      <div class="p-toolbar-group-center">
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
      </div>

      <div class="p-toolbar-group-left flex align-items-center gap-2">
        <p-button
          icon="pi pi-bell"
          [text]="true"
          [rounded]="true"
          severity="secondary"
          [badge]="'3'"
          badgeSeverity="danger"
        >
        </p-button>
        <p-avatar
          label="T"
          shape="circle"
          [style]="{
            'background-color': 'var(--accent)',
            color: 'var(--accent-ink)',
          }"
        >
        </p-avatar>
      </div>
    </p-toolbar>
  `,
  styles: [],
})
export class TopbarComponent {
  @Output() menuClick = new EventEmitter<void>();

  constructor() {}
}
