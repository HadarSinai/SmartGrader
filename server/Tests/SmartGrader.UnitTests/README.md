# SmartGrader.UnitTests

חבילת בדיקות היחידה של השרת. הכללים המלאים: `.claude/skills/backend-unit-test-pattern/SKILL.md`.

```
dotnet test server/Tests/SmartGrader.UnitTests/SmartGrader.UnitTests.csproj
```

---

## 🔴 חובה להריץ — חלק מהבדיקות עדיין לא אומתו

**נכון ל-25.8.2026.** לא כל הבדיקות בתיקייה הזו רצו אי פעם. יש להריץ את החבילה
במלואה ולתקן את מה שאדום **לפני** שסומכים עליה.

### מה אומת ומה לא

| קבצים | מצב |
|---|---|
| `Domain/ScoreCalculatorTests.cs`, `Domain/LessonScoreCalculatorTests.cs` | ✅ אומת — 27 בדיקות ירוקות |
| `Domain/StructuralRuleTests.cs`, `Analysis/RoslynCodeAnalysisServiceTests.cs` | ✅ אומת — סה"כ 116 ירוקות |
| `Domain/UserLockoutTests.cs`, `SubmissionTests.cs`, `LessonResultTests.cs`, `PasswordResetTokenTests.cs` | ✅ אומת — סה"כ 172 ירוקות |
| **`Common/*.cs`** (תאריך עברי, ניסוח בעברית, טוקן איפוס, סיפי התראות) | ❌ **נכתב אך מעולם לא הורץ** |

הבדיקות ב-`Common/` נכתבו לפי הקוד עצמו וההערות בו, בלי הרצה. הן עשויות להיות
נכונות לגמרי — אבל זה **לא ידוע**, וכל אדום שם הוא קודם כול חשד לציפייה שגויה
בבדיקה ולא לבאג בקוד. במיוחד `HebrewDateConverterTests`: ערכי אדר א׳/ב׳ ואורכי
חודשים לא אומתו מול הלוח האמיתי.

### למה לא הורצו

`Smart App Control` של ווינדוס 11 (דלוק ואוכף במחשב הפיתוח) חוסם את טעינת
`Domain.dll` שנבנה מקומית, כי הוא לא חתום ואין לו מוניטין בענן:

```
Could not load file or assembly '...\Domain.dll'.
An Application Control policy has blocked this file. (0x800711C7)
```

זו אינה בעיה בקוד ולא ב-Defender (אין שום זיהוי איום). בנייה נקייה מחדש לא עוזרת.

### מה יפתור

1. **CI ב-GitHub Actions** — רץ על לינוקס, שם המדיניות לא קיימת. ההמלצה, וגם
   שלב 9 בתוכנית המקורית.
2. **כיבוי Smart App Control** — פותר מיידית. ⚠️ בלתי הפיך בלי התקנה מחדש של ווינדוס.
3. **חתימת ה-assemblies** בתעודת קוד מיצרן מוכר.

### אחרי שמריצים

- לעדכן את הטבלה למעלה, או למחוק את הסעיף הזה כשהכול ירוק.
- **לבצע בדיקת שבירה מכוונת** לכל קובץ ב-`Common/` (לשבור זמנית את הקוד, לוודא
  שהבדיקה מאדימה, להחזיר) — זה מה שמוכיח שהבדיקה באמת שומרת על משהו. לשאר
  הקבצים זה כבר בוצע.
