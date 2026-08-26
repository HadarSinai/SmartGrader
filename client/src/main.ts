import { bootstrapApplication } from '@angular/platform-browser';
import { AppComponent } from './app/app.component';
import { appConfig } from './app/app.config';
import {
  applyAccessibilityState,
  readAccessibilityState,
} from './app/services/accessibility.service';

// מחיל את העדפות הנגישות לפני ש-Angular עולה, כדי שלא יהיה הבהוב ערכת נושא.
//
// ⚠️ קורא את אותו מפתח בדיוק שהשירות כותב אליו, דרך אותן פונקציות. קודם היה כאן קוד
// עצמאי שקרא מפתחות גולמיים (`theme`, `reduceMotion`, `fontScale`) שאף אחד מלבד הווידג'ט
// לא כתב — ולכן "איפוס" בשירות לא השפיע כאן, וההעדפה הישנה חזרה בכל טעינה.
applyAccessibilityState(readAccessibilityState());

bootstrapApplication(AppComponent, appConfig)
  .catch((err) => console.error(err));
