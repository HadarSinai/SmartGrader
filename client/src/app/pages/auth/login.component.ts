import { CommonModule } from "@angular/common";
import { Component } from "@angular/core";
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from "@angular/forms";
import { Router, RouterModule } from "@angular/router";
import { ButtonModule } from "primeng/button";
import { InputTextModule } from "primeng/inputtext";
import { PasswordModule } from "primeng/password";
import { NoHebrewDirective } from "../../core/directives/no-hebrew.directive";
import { AuthService } from "../../services/auth.service";

@Component({
  selector: "app-login",
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    ButtonModule,
    InputTextModule,
    PasswordModule,
    NoHebrewDirective,
  ],
  template: `
    <div class="sg-auth-page">
      <div class="sg-card sg-auth-card">
        <header class="sg-auth-header">
          <h1>SmartGrader</h1>
          <p>מערכת בדיקת תרגילים חכמה</p>
        </header>

        <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
          <div class="sg-auth-field">
            <label for="username">שם משתמש</label>
            <input
              pInputText
              id="username"
              type="text"
              formControlName="username"
              autocomplete="username"
            />
            <small
              class="p-error"
              *ngIf="
                form.get('username')?.invalid && form.get('username')?.touched
              "
            >
              נדרש שם משתמש
            </small>
          </div>

          <div class="sg-auth-field">
            <label for="password">סיסמה</label>
            <p-password
              sgNoHebrew
              inputId="password"
              formControlName="password"
              [feedback]="false"
              [toggleMask]="true"
              autocomplete="current-password"
              styleClass="w-full"
              inputStyleClass="w-full"
            />
            <small
              class="p-error"
              *ngIf="
                form.get('password')?.invalid && form.get('password')?.touched
              "
            >
              נדרשת סיסמה
            </small>
            <a class="sg-auth-forgot" routerLink="/forgot-password"
              >שכחתי סיסמה</a
            >
          </div>

          <div class="sg-auth-error" *ngIf="loginError" role="alert">
            <i class="pi pi-exclamation-circle" aria-hidden="true"></i>
            <span>שם משתמש או סיסמה שגויים</span>
          </div>

          <p-button
            type="submit"
            label="כניסה"
            styleClass="w-full"
            [loading]="loading"
            [disabled]="loading"
          />
        </form>

        <!-- אין כאן קישור: הרשמה עצמית נסגרה. הטקסט עונה על השאלה שהמשתמשת הייתה
             שואלת במקום להשאיר אותה מול מסך סתום. -->
        <footer class="sg-auth-footer">
          <span>אין לך חשבון? יש לפנות למנהלת המערכת</span>
        </footer>
      </div>
    </div>
  `,
  styles: [
    `
      /* המשותף לשלושת מסכי ה-auth יושב ב-styles.css. כאן רק מה שייחודי לכניסה. */

      /* מיושר לקצה ההתחלה של השורה — בעברית זהו הצד הימני, ולכן start ולא right:
         ערך קשיח היה מציב את הקישור בצד הלא נכון אם הממשק יוצג אי-פעם ב-LTR. */
      .sg-auth-forgot {
        align-self: flex-start;
        margin-top: 0.15rem;
        font-size: var(--text-sm);
        color: var(--accent);
        font-weight: 600;
        text-decoration: none;
      }

      .sg-auth-forgot:hover {
        text-decoration: underline;
      }

      :host ::ng-deep .w-full,
      :host ::ng-deep .p-password {
        width: 100%;
      }
    `,
  ],
})
export class LoginComponent {
  form: FormGroup;
  loading = false;
  loginError = false;

  constructor(
    private fb: FormBuilder,
    private auth: AuthService,
    private router: Router,
  ) {
    this.form = this.fb.group({
      username: ["", [Validators.required]],
      password: ["", [Validators.required]],
    });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.loginError = false;

    this.auth.login(this.form.value).subscribe({
      next: () => {
        this.loading = false;
        this.router.navigate(this.auth.homeRoute());
      },
      error: () => {
        this.loading = false;
        this.loginError = true;
      },
    });
  }
}
