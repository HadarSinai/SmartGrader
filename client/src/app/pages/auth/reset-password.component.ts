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
  templateUrl: "./reset-password.component.html",
  styleUrls: ["./reset-password.component.css"],
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
