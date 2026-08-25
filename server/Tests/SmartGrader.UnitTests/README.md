# SmartGrader.UnitTests

חבילת בדיקות היחידה של השרת. הכללים המלאים: `.claude/skills/backend-unit-test-pattern/SKILL.md`.

```
dotnet test server/Tests/SmartGrader.UnitTests/SmartGrader.UnitTests.csproj
```

---

## מצב האימות

**כל הבדיקות בתיקייה עברו ב-CI על לינוקס** (ריצה `ea136e2`, 25.8.2026). זה כולל את
`Common/` — שנכתבה בלי אפשרות להריץ אותה מקומית, ולכן האימות הזה הוא מה שהופך
אותה למשהו שאפשר לסמוך עליו.

**מסקנה נלווית:** `HebrewDateConverter.ToHebrewString` מחזיר גימטריה גם על לינוקס.
היה חשד שזו התנהגות ייחודית ל-NLS של ווינדוס ושעל ICU יתקבלו ספרות — הריצה
הירוקה מפריכה אותו, כלומר תאריכים עבריים בטוחים גם בפריסה לאז'ור על לינוקס.

### ⚠️ מה שעדיין חסר: בדיקות שבירה מכוונת ל-`Common/`

לכל שאר הקבצים בוצעה בדיקת שבירה מכוונת — שברנו זמנית את קוד הייצור, ראינו את
הבדיקה מאדימה, והחזרנו. **ל-`Common/` זה טרם נעשה.** בדיקה ירוקה שלא ראו אותה
נכשלת עלולה לעבור מסיבה לא נכונה, ולכן זה עדיין פתוח.

### רקע: למה זה לא רץ מקומית

`Smart App Control` של ווינדוס 11 (דלוק ואוכף במחשב הפיתוח) חוסם את טעינת
`Domain.dll` שנבנה מקומית, כי הוא לא חתום ואין לו מוניטין בענן:

```
Could not load file or assembly '...\Domain.dll'.
An Application Control policy has blocked this file. (0x800711C7)
```

זו אינה בעיה בקוד ולא ב-Defender (אין שום זיהוי איום). בנייה נקייה מחדש לא עוזרת.

### הפתרון שנבחר

**CI ב-GitHub Actions** (`.github/workflows/tests.yml`) — רץ על לינוקס, שם המדיניות
לא קיימת. הרצה מקומית עדיין עובדת לפעמים, אבל **ה-CI הוא מקור האמת**.

⚠️ הלוג הגולמי של Actions דורש הזדהות גם בריפו ציבורי. לכן ה-workflow מפרסם כשלים
גם לתקציר העבודה וגם כאנוטציות, שנגישות דרך ה-API הציבורי בלי טוקן:

```
https://api.github.com/repos/HadarSinai/SmartGrader/actions/runs?per_page=1
https://api.github.com/repos/HadarSinai/SmartGrader/check-runs/{job_id}/annotations
```
