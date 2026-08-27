import { CommonModule } from "@angular/common";
import { Component } from "@angular/core";
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from "@angular/forms";
import { RouterModule } from "@angular/router";
import { ButtonModule } from "primeng/button";
import { InputTextModule } from "primeng/inputtext";
import { NoHebrewDirective } from "../../core/directives/no-hebrew.directive";
import { AuthService } from "../../services/auth.service";

@Component({
  selector: "app-forgot-password",
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    ButtonModule,
    InputTextModule,
    NoHebrewDirective,
  ],
  template: `
    <div class="sg-auth-page">
      <div class="sg-card sg-auth-card">
        <header class="sg-auth-header">
          <h1>שחזור סיסמה</h1>
          <p *ngIf="!submitted">
            יש להזין את כתובת המייל של החשבון, ויישלח אליה קישור לבחירת סיסמה
            חדשה
          </p>
        </header>

        <!-- ⚠️ אותה הודעה בדיוק בכל מקרה — גם כשהכתובת אינה רשומה. הודעה שמבחינה
             ביניהם הייתה הופכת את המסך למונה חשבונות קיימים. -->
        <div *ngIf="submitted" class="sg-auth-confirm" role="status">
          <i class="pi pi-envelope" aria-hidden="true"></i>
          <div>
            <div class="sg-auth-confirm-title">
              אם הכתובת רשומה במערכת, נשלח אליה קישור
            </div>
            <div class="sg-auth-confirm-note">
              הקישור תקף לשעה אחת. אם המייל לא הגיע, כדאי לבדוק בתיקיית הספאם או
              לנסות שוב.
            </div>
          </div>
        </div>

        <form
          *ngIf="!submitted"
          [formGroup]="form"
          (ngSubmit)="submit()"
          novalidate
        >
          <div class="sg-auth-field">
            <label for="email">כתובת מייל</label>
            <input
              pInputText
              sgNoHebrew
              id="email"
              type="email"
              formControlName="email"
              autocomplete="email"
              placeholder="teacher@example.com"
            />
            <small
              class="p-error"
              *ngIf="
                form.get('email')?.hasError('required') &&
                form.get('email')?.touched
              "
            >
              נדרשת כתובת מייל
            </small>
            <small
              class="p-error"
              *ngIf="
                form.get('email')?.hasError('email') &&
                form.get('email')?.touched
              "
            >
              כתובת המייל אינה תקינה
            </small>
          </div>

          <!-- 429 הוא המצב היחיד שכן מדווח: הוא אינו מעיד דבר על החשבון, ובלעדיו
               המשתמשת מקבלת "נשלח קישור" ומחכה למייל שלא יצא. -->
          <div class="sg-auth-error" *ngIf="rateLimited" role="alert">
            <i class="pi pi-exclamation-circle" aria-hidden="true"></i>
            <span>נשלחו יותר מדי בקשות. יש להמתין דקה ולנסות שוב.</span>
          </div>

          <p-button
            type="submit"
            label="שליחת קישור"
            styleClass="w-full"
            [loading]="loading"
            [disabled]="loading"
          />
        </form>

        <footer class="sg-auth-footer">
          <a routerLink="/login">חזרה למסך הכניסה</a>
        </footer>
      </div>
    </div>
  `,
  styles: [
    `
      .sg-auth-page {
        min-height: 100vh;
        display: flex;
        align-items: center;
        justify-content: center;
        background: var(--app-bg);
        padding: var(--space-4, 1rem);
      }

      .sg-auth-card {
        width: 100%;
        max-width: 400px;
        background: var(--app-surface);
        border: 1px solid var(--app-border);
        border-radius: var(--radius-lg, 12px);
        box-shadow: var(--shadow-md, 0 2px 8px rgba(0, 0, 0, 0.06));
        padding: var(--space-6, 2rem);
      }

      .sg-auth-header {
        text-align: center;
        margin-bottom: var(--space-6);
      }

      .sg-auth-header h1 {
        margin: 0 0 0.25rem;
        font-size: var(--text-xl, 1.5rem);
        color: var(--app-text-strong);
      }

      .sg-auth-header p {
        margin: 0;
        color: var(--app-muted);
        font-size: var(--text-sm, 0.875rem);
      }

      .sg-auth-field {
        display: flex;
        flex-direction: column;
        gap: 0.35rem;
        margin-bottom: var(--space-4, 1rem);
      }

      .sg-auth-field label {
        font-weight: 600;
        color: var(--app-text-strong);
        font-size: var(--text-sm, 0.875rem);
      }

      .sg-auth-field input {
        width: 100%;
      }

      .sg-auth-error {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        color: var(--status-error);
        background: color-mix(
          in srgb,
          var(--status-error) 10%,
          transparent
        );
        border-radius: var(--radius-sm, 6px);
        padding: 0.6rem 0.75rem;
        margin-bottom: var(--space-4, 1rem);
        font-size: var(--text-sm, 0.875rem);
      }

      .sg-auth-confirm {
        display: flex;
        align-items: flex-start;
        gap: 0.6rem;
        color: var(--status-success);
        background: color-mix(
          in srgb,
          var(--status-success) 10%,
          transparent
        );
        border-radius: var(--radius-sm, 6px);
        padding: 0.75rem;
        font-size: var(--text-sm, 0.875rem);
      }

      .sg-auth-confirm i {
        margin-top: 0.15rem;
      }

      .sg-auth-confirm-title {
        font-weight: 600;
      }

      .sg-auth-confirm-note {
        margin-top: 0.35rem;
        color: var(--app-muted);
      }

      .sg-auth-footer {
        margin-top: var(--space-6);
        text-align: center;
        font-size: var(--text-sm, 0.875rem);
        color: var(--app-muted);
        display: flex;
        justify-content: center;
        gap: 0.5rem;
      }

      .sg-auth-footer a {
        color: var(--accent);
        font-weight: 600;
        text-decoration: none;
      }

      .sg-auth-footer a:hover {
        text-decoration: underline;
      }

      :host ::ng-deep .w-full {
        width: 100%;
      }
    `,
  ],
})
export class ForgotPasswordComponent {
  form: FormGroup;
  loading = false;
  submitted = false;
  rateLimited = false;

  constructor(
    private fb: FormBuilder,
    private auth: AuthService,
  ) {
    this.form = this.fb.group({
      email: ["", [Validators.required, Validators.email]],
    });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.rateLimited = false;

    this.auth.forgotPassword(this.form.value).subscribe({
      next: () => {
        this.loading = false;
        this.submitted = true;
      },
      error: (err: { status?: number }) => {
        this.loading = false;

        if (err?.status === 429) {
          this.rateLimited = true;
          return;
        }

        // ⚠️ כל שאר השגיאות מציגות את אותו אישור. השרת מחזיר 200 בכל מסלול אמיתי,
        // ולכן מה שנשאר כאן הוא תקלת רשת — ומסך שגיאה שמופיע רק לפעמים היה בעצמו
        // רמז לגבי החשבון. התקלה התפעולית נרשמת בצד השרת, בטבלת הלוגים.
        this.submitted = true;
      },
    });
  }
}
