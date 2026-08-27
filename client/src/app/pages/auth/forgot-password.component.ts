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
  templateUrl: "./forgot-password.component.html",
  styleUrls: ["./forgot-password.component.css"],
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
