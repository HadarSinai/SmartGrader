import { CommonModule } from "@angular/common";
import { Component, OnInit, inject } from "@angular/core";
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from "@angular/forms";
import { ActivatedRoute, Router, RouterModule } from "@angular/router";
import { MessageService } from "primeng/api";
import { ButtonModule } from "primeng/button";
import { PasswordModule } from "primeng/password";
import { PasswordChecklistComponent } from "../../components/password-checklist/password-checklist.component";
import { NoHebrewDirective } from "../../core/directives/no-hebrew.directive";
import {
  passwordStrengthValidator,
  passwordsMatch,
} from "../../core/validators/password.validator";
import { AuthService } from "../../services/auth.service";

@Component({
  selector: "app-reset-password",
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    ButtonModule,
    PasswordModule,
    PasswordChecklistComponent,
    NoHebrewDirective,
  ],
  template: `
    <div class="sg-auth-page">
      <div class="sg-card sg-auth-card">
        <header class="sg-auth-header">
          <h1>בחירת סיסמה חדשה</h1>
          <p *ngIf="token">הסיסמה החדשה נכנסת לתוקף מיד</p>
        </header>

        <!-- כניסה לכתובת בלי token — למשל הדבקה חלקית של הקישור מהמייל. אין טעם
             להציג טופס שייכשל ממילא בשרת. -->
        <ng-container *ngIf="!token; else resetForm">
          <div class="sg-auth-error" role="alert">
            <i class="pi pi-exclamation-circle" aria-hidden="true"></i>
            <span>
              הקישור אינו תקין. כדאי לוודא שהועתק במלואו מהמייל, או לבקש קישור
              חדש.
            </span>
          </div>
        </ng-container>

        <ng-template #resetForm>
          <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
            <div class="sg-auth-field">
              <label for="password">סיסמה חדשה</label>
              <p-password
                sgNoHebrew
                inputId="password"
                formControlName="password"
                [feedback]="false"
                [toggleMask]="true"
                autocomplete="new-password"
                styleClass="w-full"
                inputStyleClass="w-full"
              />
              <app-password-checklist
                *ngIf="form.get('password')?.value"
                [password]="form.get('password')?.value"
              />
              <small
                class="p-error"
                *ngIf="
                  form.get('password')?.hasError('required') &&
                  form.get('password')?.touched
                "
              >
                נדרשת סיסמה
              </small>
            </div>

            <div class="sg-auth-field">
              <label for="confirmPassword">אימות הסיסמה</label>
              <p-password
                sgNoHebrew
                inputId="confirmPassword"
                formControlName="confirmPassword"
                [feedback]="false"
                [toggleMask]="true"
                autocomplete="new-password"
                styleClass="w-full"
                inputStyleClass="w-full"
              />
              <small
                class="p-error"
                *ngIf="
                  form.get('confirmPassword')?.hasError('required') &&
                  form.get('confirmPassword')?.touched
                "
              >
                נדרש אימות של הסיסמה
              </small>
              <small
                class="p-error"
                *ngIf="
                  form.hasError('passwordsMismatch') &&
                  form.get('confirmPassword')?.touched
                "
              >
                הסיסמאות אינן תואמות
              </small>
            </div>

            <div class="sg-auth-error" *ngIf="submitError" role="alert">
              <i class="pi pi-exclamation-circle" aria-hidden="true"></i>
              <span>{{ submitError }}</span>
            </div>

            <p-button
              type="submit"
              label="שמירת הסיסמה"
              styleClass="w-full"
              [loading]="loading"
              [disabled]="loading"
            />
          </form>
        </ng-template>

        <footer class="sg-auth-footer">
          <a routerLink="/forgot-password">בקשת קישור חדש</a>
          <span aria-hidden="true">·</span>
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
        background: var(--app-surface, #fbfaf8);
        border: 1px solid var(--app-border, #e5e0d8);
        border-radius: var(--radius-lg, 12px);
        box-shadow: var(--shadow-md, 0 2px 8px rgba(0, 0, 0, 0.06));
        padding: var(--space-6, 2rem);
      }

      .sg-auth-header {
        text-align: center;
        margin-bottom: var(--space-5, 1.5rem);
      }

      .sg-auth-header h1 {
        margin: 0 0 0.25rem;
        font-size: var(--text-xl, 1.5rem);
        color: var(--app-text-strong);
      }

      .sg-auth-header p {
        margin: 0;
        color: var(--app-text-muted, #75695e);
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

      .sg-auth-error {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        color: var(--status-error, #b4552d);
        background: color-mix(
          in srgb,
          var(--status-error, #b4552d) 10%,
          transparent
        );
        border-radius: var(--radius-sm, 6px);
        padding: 0.6rem 0.75rem;
        margin-bottom: var(--space-4, 1rem);
        font-size: var(--text-sm, 0.875rem);
      }

      .sg-auth-footer {
        margin-top: var(--space-5, 1.5rem);
        text-align: center;
        font-size: var(--text-sm, 0.875rem);
        color: var(--app-text-muted, #75695e);
        display: flex;
        justify-content: center;
        gap: 0.5rem;
      }

      .sg-auth-footer a {
        color: var(--accent, #8a6a54);
        font-weight: 600;
        text-decoration: none;
      }

      .sg-auth-footer a:hover {
        text-decoration: underline;
      }

      :host ::ng-deep .w-full,
      :host ::ng-deep .p-password {
        width: 100%;
      }
    `,
  ],
})
export class ResetPasswordComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly messageService = inject(MessageService);

  form: FormGroup;
  token: string | null = null;
  loading = false;
  submitError: string | null = null;

  constructor() {
    this.form = this.fb.group(
      {
        password: ["", [Validators.required, passwordStrengthValidator]],
        confirmPassword: ["", [Validators.required]],
      },
      { validators: passwordsMatch },
    );
  }

  ngOnInit(): void {
    this.token = this.route.snapshot.queryParamMap.get("token");
  }

  submit(): void {
    if (this.form.invalid || !this.token) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.submitError = null;

    this.auth
      .resetPassword({
        token: this.token,
        newPassword: this.form.value.password,
      })
      .subscribe({
        next: () => {
          this.messageService.add({
            severity: "success",
            summary: "בוצע",
            detail: "הסיסמה עודכנה. אפשר להיכנס עם הסיסמה החדשה.",
          });
          this.router.navigate(["/login"]);
        },
        error: (err: { status?: number; error?: { detail?: string } }) => {
          this.loading = false;

          if (err?.status === 429) {
            this.submitError = "נשלחו יותר מדי בקשות. יש להמתין דקה ולנסות שוב.";
            return;
          }

          // BusinessRuleException חוזר כ-ProblemDetails, וההודעה יושבת ב-detail.
          // היא גנרית אחת לטוקן שפג, לטוקן שנוצל, ולטוקן שקישור חדש גבר עליו —
          // אין לנסות לפרש אותה כאן, השרת נמנע מלהבחין ביניהם בכוונה.
          this.submitError =
            err?.error?.detail ||
            "עדכון הסיסמה נכשל. ייתכן שהקישור אינו תקף יותר — יש לבקש קישור חדש.";
        },
      });
  }
}
