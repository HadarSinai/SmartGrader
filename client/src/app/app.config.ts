import { registerLocaleData } from "@angular/common";
import { provideHttpClient, withInterceptors } from "@angular/common/http";
import localeHe from "@angular/common/locales/he";
import { ApplicationConfig, LOCALE_ID } from "@angular/core";
import { provideAnimations } from "@angular/platform-browser/animations";
import { provideRouter } from "@angular/router";
import { MessageService } from "primeng/api";
import { routes } from "./app.routes";
import { apiErrorInterceptor } from "./core/http/api-error.interceptor";
import { authInterceptor } from "./core/http/auth.interceptor";

registerLocaleData(localeHe);

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor, apiErrorInterceptor])),
    provideAnimations(),
    MessageService,
    { provide: LOCALE_ID, useValue: "he" },
  ],
};
