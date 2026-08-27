import { CommonModule } from "@angular/common";
import { Component, Input } from "@angular/core";
import {
  PASSWORD_RULES,
  PasswordRule,
} from "../../core/validators/password.validator";

@Component({
  selector: "app-password-checklist",
  standalone: true,
  imports: [CommonModule],
  templateUrl: "./password-checklist.component.html",
  styleUrls: ["./password-checklist.component.css"],
})
export class PasswordChecklistComponent {
  @Input() password: string | null = "";

  readonly rules: PasswordRule[] = PASSWORD_RULES;

  isMet(rule: PasswordRule): boolean {
    return rule.test(this.password ?? "");
  }
}
