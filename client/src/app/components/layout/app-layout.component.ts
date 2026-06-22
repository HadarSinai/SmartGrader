import { CommonModule } from "@angular/common";
import { Component } from "@angular/core";
import { FormsModule } from "@angular/forms";
import {
  ActivatedRoute,
  NavigationEnd,
  Router,
  RouterModule,
} from "@angular/router";
import { InputTextModule } from "primeng/inputtext";
import { RatingModule } from "primeng/rating";
import { filter } from "rxjs/operators";
import { TopbarComponent } from "./topbar.component";

@Component({
  selector: "app-layout",
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    InputTextModule,
    RatingModule,
    TopbarComponent,
  ],
  template: `
    <div class="sg-shell min-h-screen relative overflow-x-hidden">
      <section class="sg-hero" aria-labelledby="hero-title">
        <div
          class="sg-hero-bg "
          aria-hidden="true"
          [ngStyle]="{ 'background-image': 'url(' + heroImage + ')' }"
        ></div>
        <div class="sg-hero-overlay">
          <div class="sg-hero-inner sg-page" aria-label="אזור עליון">
            <div class="sg-hero-topbar" aria-label="פס ניווט עליון">
              <app-topbar></app-topbar>
            </div>

            <div class="sg-hero-content">
              <div class="sg-hero-card">
                <div class="sg-hero-title">ברוכה הבאה</div>
                <div class="sg-hero-sub">מורה יקרה!</div>
                <div class="sg-hero-sub">{{ heroTitle }}</div>

                <div class="sg-hero-bullets">
                  <div>{{ heroSubtitle }}</div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>
      <main class="p-0" aria-label="תוכן">
        <router-outlet></router-outlet>
      </main>

      <footer class="sg-footer" aria-label="פוטר">
        <div class="sg-footer-inner">
          <div class="sg-footer-brand">HS</div>
          <div class="sg-footer-text">
            <div class="sg-footer-quote">
              אתר איכותי תלמידות<br />לכל אחת את הזכות
            </div>
            <div class="sg-footer-search">
              <input
                pInputText
                type="text"
                placeholder="חיפוש"
                aria-label="חיפוש"
              />
            </div>
            <p-rating
              [readonly]="true"
              [cancel]="false"
              [stars]="5"
              [(ngModel)]="footerRating"
              aria-label="דירוג"
            ></p-rating>
          </div>
          <div class="sg-footer-links" aria-label="קישורים">
            <div>לוח בקרה</div>
            <div>שיעורים</div>
            <div>סטודנטים</div>
            <div>תרגילים</div>
            <div>הגשות</div>
          </div>
        </div>
      </footer>
    </div>
  `,
  styles: [
    `
      .sg-shell {
        background: var(--app-bg);
      }
      .sg-hero {
        position: relative;
        height: 300px;
        overflow: hidden;
        border-bottom: 1px solid rgba(58, 48, 40, 0.1);
      }

      .sg-hero-bg {
        position: absolute;
        inset: 0;
        background-size: cover;
        background-position: center;
        filter: saturate(0.92) brightness(1.05);
      }

      .sg-hero-overlay {
        position: absolute;
        inset: 0;
        background: linear-gradient(
          90deg,
          rgba(58, 48, 40, 0.35) 0%,
          rgba(58, 48, 40, 0.1) 45%,
          rgba(58, 48, 40, 0) 100%
        );
      }

      /* Soft translucent blocks like the Figma hero */
      .sg-hero-overlay::before,
      .sg-hero-overlay::after {
        content: "";
        position: absolute;
        top: 92px;
        width: 180px;
        height: 62px;
        background: rgba(138, 106, 84, 0.35);
        border-radius: 18px;
        box-shadow: 0 18px 42px rgba(58, 48, 40, 0.18);
        backdrop-filter: blur(2px);
      }
      .sg-hero-overlay::before {
        left: 46%;
        transform: translateX(-50%);
      }
      .sg-hero-overlay::after {
        left: 52%;
        top: 162px;
        transform: translateX(-50%);
        width: 220px;
      }

      .sg-hero-overlay {
        display: block;
      }

      .sg-hero-inner {
        height: 100%;
        display: flex;
        flex-direction: column;
        justify-content: flex-start;
      }

      .sg-hero-topbar {
        padding-block-start: 3.25rem;
        z-index: 3;
      }

      .sg-hero-content {
        flex: 1;
        display: flex;
        align-items: flex-start;
        padding-block-start: 1.2rem;
      }

      .sg-hero-card {
        position: relative;
        margin-inline-start: auto;
        background: rgba(138, 106, 84, 0.6);
        border-radius: 28px;
        padding: 1.25rem 1.25rem;
        color: #fff;
        width: min(420px, 90%);
        box-shadow: 0 18px 42px rgba(58, 48, 40, 0.22);
        backdrop-filter: blur(2px);
      }

      .sg-hero-title {
        font-size: 2rem;
        font-weight: 800;
        line-height: 1.1;
      }
      .sg-hero-sub {
        font-size: 1.3rem;
        font-weight: 700;
        margin-top: 0.25rem;
      }
      .sg-hero-bullets {
        margin-top: 0.85rem;
        opacity: 0.95;
        font-weight: 600;
      }

      @media (max-width: 768px) {
        .sg-hero {
          height: 260px;
        }

        .sg-hero-topbar {
          padding-block-start: 1rem;
        }

        .sg-hero-content {
          padding-block-start: 0.75rem;
        }

        .sg-hero-overlay::before,
        .sg-hero-overlay::after {
          display: none;
        }

        .sg-hero-card {
          margin-inline: auto;
          width: min(520px, 100%);
          border-radius: 22px;
        }

        .sg-hero-title {
          font-size: 1.6rem;
        }

        .sg-hero-sub {
          font-size: 1.05rem;
        }
      }

      @media (max-width: 420px) {
        .sg-hero {
          height: 240px;
        }

        .sg-hero-card {
          padding: 1rem;
        }
      }
    `,
  ],
})
export class AppLayoutComponent {
  heroImage = "/assets/hero-classroom.jpg";
  heroTitle = "ברוכה הבאה";
  heroSubtitle = "ניהול פרופילי תלמידים ומעקב אחר ביצועים";
  footerRating = 4;

  constructor(
    private router: Router,
    private route: ActivatedRoute,
  ) {
    this.router.events
      .pipe(filter((e) => e instanceof NavigationEnd))
      .subscribe(() => {
        let r = this.route;
        while (r.firstChild) r = r.firstChild;

        const data = r.snapshot.data;
        this.heroImage = data["heroImage"] ?? this.heroImage;
        this.heroTitle = data["heroTitle"] ?? this.heroTitle;
        this.heroSubtitle = data["heroSubtitle"] ?? this.heroSubtitle;
      });
  }
}
