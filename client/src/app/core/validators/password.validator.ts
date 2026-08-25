import { AbstractControl, ValidationErrors, ValidatorFn } from "@angular/forms";

export interface PasswordRule {
  key: string;
  label: string;
  test: (value: string) => boolean;
}

export const PASSWORD_RULES: PasswordRule[] = [
  {
    key: "passwordMinLength",
    label: "לפחות 8 תווים",
    test: (v) => v.length >= 8,
  },
  {
    key: "passwordUppercase",
    label: "אות גדולה באנגלית (A-Z)",
    test: (v) => /[A-Z]/.test(v),
  },
  {
    key: "passwordLowercase",
    label: "אות קטנה באנגלית (a-z)",
    test: (v) => /[a-z]/.test(v),
  },
  {
    key: "passwordDigit",
    label: "ספרה (0-9)",
    test: (v) => /[0-9]/.test(v),
  },
];

/**
 * Validates password strength (min length, uppercase, lowercase, digit).
 * Empty values are considered valid — pair with Validators.required when needed.
 */
export const passwordStrengthValidator: ValidatorFn = (
  control: AbstractControl,
): ValidationErrors | null => {
  const value: string = control.value ?? "";
  if (!value) {
    return null;
  }

  const errors: ValidationErrors = {};
  for (const rule of PASSWORD_RULES) {
    if (!rule.test(value)) {
      errors[rule.key] = true;
    }
  }
  return Object.keys(errors).length > 0 ? errors : null;
};

/**
 * Cross-field validator: `password` and `confirmPassword` must match.
 * Apply on the FormGroup, not on a control:
 * `this.fb.group({...}, { validators: passwordsMatch })`.
 *
 * Returns null while either field is still empty, so the "אינן תואמות" message
 * doesn't appear mid-typing — pair each field with Validators.required.
 */
export function passwordsMatch(
  group: AbstractControl,
): ValidationErrors | null {
  const password = group.get("password")?.value;
  const confirm = group.get("confirmPassword")?.value;
  return password && confirm && password !== confirm
    ? { passwordsMismatch: true }
    : null;
}
