import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-auth-shell',
  standalone: true,
  imports: [RouterOutlet],
  template: `
    <div class="auth-layout">
      <div class="auth-brand">
        <div class="auth-brand__logo">
          <svg width="36" height="36" viewBox="0 0 36 36" fill="none">
            <rect width="36" height="36" rx="9" fill="#000000"/>
            <path d="M18 7L28 12V19C28 24.523 23.523 29.5 18 31C12.477 29.5 8 24.523 8 19V12L18 7Z"
              fill="#ffffff" fill-opacity="0.92"/>
          </svg>
          <span class="auth-brand__name">HostMe</span>
        </div>
        <p class="auth-brand__tagline">Deploy your sites in seconds</p>
        <div class="auth-brand__features">
          <div class="auth-feature">
            <span class="auth-feature__icon">⚡</span>
            <span>Lightning fast deployments</span>
          </div>
          <div class="auth-feature">
            <span class="auth-feature__icon">🔒</span>
            <span>Secure &amp; reliable hosting</span>
          </div>
          <div class="auth-feature">
            <span class="auth-feature__icon">🌐</span>
            <span>Global CDN distribution</span>
          </div>
        </div>
      </div>
      <div class="auth-content">
        <router-outlet />
      </div>
    </div>
  `,
  styleUrl: './auth-shell.component.css',
})
export class AuthShellComponent {}
