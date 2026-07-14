import { CommonModule } from "@angular/common";
import { Component } from "@angular/core";
import {
    AbstractControl,
    FormBuilder,
    FormGroup,
    ReactiveFormsModule,
    ValidationErrors,
    Validators,
} from "@angular/forms";
import { Router, RouterModule } from "@angular/router";
import { MessageService } from "primeng/api";
import { ButtonModule } from "primeng/button";
import { InputTextModule } from "primeng/inputtext";
import { PasswordModule } from "primeng/password";
import { AuthService } from "../../services/auth.service";

function passwordsMatch(group: AbstractControl): ValidationErrors | null {
  const password = group.get("password")?.value;
  const confirm = group.get("confirmPassword")?.value;
  return password && confirm && password !== confirm
    ? { passwordsMismatch: true }
    : null;
}

@Component({
  selector: "app-register",
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    ButtonModule,
    InputTextModule,
    PasswordModule,
  ],
  template: `
    <div class="sg-auth-page">
      <div class="sg-card sg-auth-card">
        <header class="sg-auth-header">
          <h1>SmartGrader</h1>
          <p>הרשמת מורה חדשה</p>
        </header>

        <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
          <div class="sg-auth-field">
            <label for="fullName">שם מלא</label>
            <input
              pInputText
              id="fullName"
              type="text"
              formControlName="fullName"
              autocomplete="name"
            />
            <small
              class="p-error"
              *ngIf="
                form.get('fullName')?.invalid && form.get('fullName')?.touched
              "
            >
              נדרש שם מלא
            </small>
          </div>

          <div class="sg-auth-field">
            <label for="email">אימייל</label>
            <input
              pInputText
              id="email"
              type="email"
              formControlName="email"
              autocomplete="email"
              dir="ltr"
            />
            <small
              class="p-error"
              *ngIf="form.get('email')?.invalid && form.get('email')?.touched"
            >
              נדרש אימייל תקין
            </small>
          </div>

          <div class="sg-auth-field">
            <label for="password">סיסמה</label>
            <p-password
              inputId="password"
              formControlName="password"
              [feedback]="false"
              [toggleMask]="true"
              autocomplete="new-password"
              styleClass="w-full"
              inputStyleClass="w-full"
            />
            <small
              class="p-error"
              *ngIf="
                form.get('password')?.invalid && form.get('password')?.touched
              "
            >
              נדרשת סיסמה באורך 8 תווים לפחות
            </small>
          </div>

          <div class="sg-auth-field">
            <label for="confirmPassword">אימות סיסמה</label>
            <p-password
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
                form.hasError('passwordsMismatch') &&
                form.get('confirmPassword')?.touched
              "
            >
              הסיסמאות אינן תואמות
            </small>
          </div>

          <div class="sg-auth-error" *ngIf="registerError" role="alert">
            <i class="pi pi-exclamation-circle" aria-hidden="true"></i>
            <span>{{ registerError }}</span>
          </div>

          <p-button
            type="submit"
            label="הרשמה"
            styleClass="w-full"
            [loading]="loading"
            [disabled]="loading"
          />
        </form>

        <footer class="sg-auth-footer">
          <span>כבר רשומה?</span>
          <a routerLink="/login">כניסה</a>
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

      .sg-auth-field input {
        width: 100%;
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
export class RegisterComponent {
  form: FormGroup;
  loading = false;
  registerError: string | null = null;

  constructor(
    private fb: FormBuilder,
    private auth: AuthService,
    private router: Router,
    private toast: MessageService,
  ) {
    this.form = this.fb.group(
      {
        fullName: ["", [Validators.required]],
        email: ["", [Validators.required, Validators.email]],
        password: ["", [Validators.required, Validators.minLength(8)]],
        confirmPassword: ["", [Validators.required]],
      },
      { validators: passwordsMatch },
    );
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.registerError = null;

    const { fullName, email, password } = this.form.value;

    this.auth.registerTeacher({ fullName, email, password }).subscribe({
      next: () => {
        this.loading = false;
        this.toast.add({
          severity: "success",
          summary: "ברוכה הבאה!",
          detail: "ההרשמה הושלמה בהצלחה",
          life: 4000,
        });
        this.router.navigate(["/"]);
      },
      error: (err) => {
        this.loading = false;
        this.registerError =
          err?.status === 409
            ? "כבר קיים חשבון עם האימייל הזה"
            : "ההרשמה נכשלה, נסי שוב";
      },
    });
  }
}
