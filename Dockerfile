# SmartGrader — תמונה אחת שמכילה גם את ה-API וגם את הלקוח.
#
# למה אחת ולא שתיים: הלקוח מוגש מתוך wwwroot של ה-API, כלומר הדפדפן מדבר עם מקור אחד.
# זה מייתר CORS לגמרי (App:AllowedOrigins יכול להישאר ריק), מייתר apiBaseUrl בלקוח,
# ומייתר שרת סטטי נפרד. שלושה דברים שאפשר לטעות בהם — נעלמים.
#
# ההקשר לבנייה הוא **שורש הריפו**, לא server/ ולא client/:
#   docker build -t smartgrader .

# ─── שלב 1: בניית הלקוח ──────────────────────────────────────────────────────
FROM node:20-alpine AS client

WORKDIR /src/client

# package.json ו-lock לבד קודם: כל עוד התלויות לא השתנו, npm ci נלקח מהמטמון
# גם כשקוד המקור השתנה. זה ההבדל בין בנייה של דקה לבנייה של חמש.
COPY client/package.json client/package-lock.json ./
RUN npm ci

COPY client/ ./
RUN npm run build -- --configuration production

# ─── שלב 2: בניית ה-API ──────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS server

WORKDIR /src

# אותו שיקול מטמון: קבצי הפרויקט לפני קוד המקור, כדי ש-restore לא ירוץ מחדש על
# כל שינוי בקוד. ⚠️ Tests אינו מועתק — הוא אינו נדרש לפרסום ו-Infrastructure שלו
# היה גורר תלויות מיותרות לתוך שלב הבנייה.
COPY server/Domain/Domain.csproj          server/Domain/
COPY server/Application/Application.csproj server/Application/
COPY server/Infrastructure/Infrastructure.csproj server/Infrastructure/
COPY server/Api/Api.csproj                 server/Api/
RUN dotnet restore server/Api/Api.csproj

COPY server/Domain/         server/Domain/
COPY server/Application/    server/Application/
COPY server/Infrastructure/ server/Infrastructure/
COPY server/Api/            server/Api/

RUN dotnet publish server/Api/Api.csproj -c Release -o /app/publish --no-restore

# ─── שלב 3: הרצה ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app

COPY --from=server /app/publish ./

# הלקוח נכנס ל-wwwroot. הנתיב כולל /browser כי זהו הפלט של ה-application builder
# של אנגולר 17 — לא dist/<name> ישירות, כפי שהיה ב-builder הישן.
COPY --from=client /src/client/dist/grading-system-frontend/browser ./wwwroot

# ⚠️ התיקייה נוצרת כאן ולא נסמכת על ה-volume: קונטיינר שרץ בלי volume (בדיקה מקומית,
# טעות ב-compose) היה נופל באתחול על נתיב שאינו קיים, במקום ליצור מסד ריק.
RUN mkdir -p /app/data

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    ConnectionStrings__Default="Data Source=/app/data/GradeSheet.db" \
    ConnectionStrings__Hangfire="Data Source=/app/data/hangfire.db"

EXPOSE 8080

ENTRYPOINT ["dotnet", "Api.dll"]
