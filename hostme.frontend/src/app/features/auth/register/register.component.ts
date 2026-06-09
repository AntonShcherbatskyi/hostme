import { Component, signal } from '@angular/core';
import {
  FormBuilder,
  FormGroup,
  Validators,
  AbstractControl,
  ValidationErrors,
  ReactiveFormsModule,
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../core/services/auth.service';
import { HttpErrorResponse } from '@angular/common/http';

function passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
  const password = control.get('password')?.value;
  const confirm = control.get('confirmPassword')?.value;
  return password && confirm && password !== confirm ? { passwordMismatch: true } : null;
}

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, CommonModule],
  templateUrl: './register.component.html',
  styleUrl: './register.component.css',
})
export class RegisterComponent {
  registerForm: FormGroup;
  isLoading = signal(false);
  serverErrors = signal<string[]>([]);
  showPassword = signal(false);
  showConfirmPassword = signal(false);
  registrationSuccess = signal(false);

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {
    this.registerForm = this.fb.group(
      {
        username: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(100)]],
        email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
        password: ['', [Validators.required, Validators.minLength(6), Validators.maxLength(100)]],
        confirmPassword: ['', Validators.required],
      },
      { validators: passwordMatchValidator }
    );
  }

  get username() { return this.registerForm.get('username')!; }
  get email() { return this.registerForm.get('email')!; }
  get password() { return this.registerForm.get('password')!; }
  get confirmPassword() { return this.registerForm.get('confirmPassword')!; }

  togglePassword(): void { this.showPassword.update((v) => !v); }
  toggleConfirmPassword(): void { this.showConfirmPassword.update((v) => !v); }

  onSubmit(): void {
    if (this.registerForm.invalid || this.isLoading()) return;

    this.serverErrors.set([]);
    this.isLoading.set(true);

    const { username, email, password } = this.registerForm.value;
    this.authService.register({ username, email, password }).subscribe({
      next: (res) => {
        this.isLoading.set(false);
        if (res.isError) {
          this.serverErrors.set(res.errors);
        } else {
          this.registrationSuccess.set(true);
          setTimeout(() => this.router.navigate(['/auth/login']), 2000);
        }
      },
      error: (err: HttpErrorResponse) => {
        this.isLoading.set(false);
        if (err.error?.errors?.length) {
          this.serverErrors.set(err.error.errors);
        } else {
          this.serverErrors.set(['An unexpected error occurred. Please try again.']);
        }
      },
    });
  }
}
