# Auth — what was deferred and is still open

`docs/auth-plan.md` was the work plan for building authentication. Auth shipped, and the plan was
deleted in Plan A's phase A7. **Everything in it that was implemented now has a rule id** in
[docs/business-rules.md](../../docs/business-rules.md) — login and lockout, password recovery, account
creation, the password policy (`B-11` … `B-24`). What was *not* implemented is here, so that deleting
the plan did not delete the backlog.

Four of the original plan's deferred items turned out to have shipped since, and are **not** repeated
below: the student area (`/my/*`), forgot-password and reset by email, student import that creates
login accounts (`ImportStudentsHandler` — Excel rather than CSV), and replacing the free-text
`Lesson.TeacherName` with a real FK (`Lesson.TeacherId` → `User`).

---

## Still open — features

### SSO (Microsoft / Google)

Not started; no `ExternalProvider` / `ExternalId` columns exist. The original design note still holds:
it can be added on top of the same `User` table rather than beside it, so a user who signs in with
Google is the same row as a user who signs in with a password.

**If it is picked up:** the row scope in [docs/permissions.md](../../docs/permissions.md) is keyed on
`User.Id` and its role claim, so nothing downstream changes — only how the claim is obtained.

### Refresh tokens / "remember me"

Not started. Today a token expires and the user logs in again; `ApiErrorInterceptor` handles the 401.

⚠️ **Read `B-51` before designing this.** The access token lives in browser storage, which is only
acceptable while student-submitted source is rendered as escaped text and never as HTML. A refresh
token in the same place raises the cost of that discipline slipping — it turns a session-length
exposure into a persistent one. If refresh tokens are added, the refresh token belongs in an
`HttpOnly` cookie, not in `localStorage`.

---

## Still open — deployment

### The API is not containerized

`docker-compose.yml` runs the Judge0 infrastructure only. When the API is deployed, the JWT signing
key must be passed as the `Jwt__Key` environment variable. Until then the development key lives in
`appsettings.Development.json`, which is gitignored.

The same applies to every other secret named in [server/CLAUDE.md](../../server/CLAUDE.md) § Secrets:
SMTP credentials, the OpenAI key, the admin bootstrap password. `appsettings.json` ships with empty
placeholders and is committed.

### ⚠️ Rotate any API key that has ever been committed

**A key that appeared in git history is compromised, and deleting it from the file does not remove it
from the history.** Rotation has to happen at the provider — OpenAI, RapidAPI/Judge0 — not in the
repository. This is a manual action with no code change, which is exactly why it is easy to leave
undone; it belongs on the go-live checklist beside `B-8` and `B-50`.
