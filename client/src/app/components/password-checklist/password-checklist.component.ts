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
  template: `
    <ul class="sg-password-checklist" aria-live="polite">
      <li *ngFor="let rule of rules" [class.sg-rule-met]="isMet(rule)">
        <i
          class="pi"
          [ngClass]="isMet(rule) ? 'pi-check-circle' : 'pi-circle'"
          aria-hidden="true"
        ></i>
        <span class="sg-visually-hidden">{{
          isMet(rule) ? "מתקיים:" : "לא מתקיים:"
        }}</span>
        {{ rule.label }}
      </li>
    </ul>
  `,
  styles: [
    `
      .sg-password-checklist {
        list-style: none;
        margin: 0.35rem 0 0;
        padding: 0;
        display: flex;
        flex-direction: column;
        gap: 0.25rem;
        font-size: var(--text-sm);
        color: var(--app-muted);
      }

      .sg-password-checklist li {
        display: flex;
        align-items: center;
        gap: 0.4rem;
      }

      .sg-password-checklist li i {
        font-size: 0.85rem;
      }

      .sg-password-checklist li.sg-rule-met {
        color: var(--status-success);
      }

      .sg-visually-hidden {
        position: absolute;
        width: 1px;
        height: 1px;
        overflow: hidden;
        clip: rect(0 0 0 0);
        white-space: nowrap;
      }
    `,
  ],
})
export class PasswordChecklistComponent {
  @Input() password: string | null = "";

  readonly rules: PasswordRule[] = PASSWORD_RULES;

  isMet(rule: PasswordRule): boolean {
    return rule.test(this.password ?? "");
  }
}
