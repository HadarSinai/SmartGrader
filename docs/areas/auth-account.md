# Area: Auth & Account

> SmartGrader · Version 1.0 · Last updated 2026-08-27 · Status: as-built

| Version | Date | Change |
|---|---|---|
| 1.0 | 2026-08-27 | First edition. |

## Purpose

Getting in, getting back in, and the one screen a person has for her own account.

Almost every requirement here is a **security decision expressed as a deliberately unhelpful user
experience** — one error message for four different failures, an empty success for an unknown address.
That is not an oversight to improve; it is the requirement.

## Who Uses This

Everyone, at the moment they are least patient: a teacher who mistyped her password, a student on a
school computer, an admin the morning of the first lesson.

## Screens & Routes

<!-- gen:arearoutes auth-account -->

| `path` | Full route | Screen |
|---|---|---|
| `login` | `/login` | Login |
| `forgot-password` | `/forgot-password` | Request a reset link |
| `reset-password` | `/reset-password` | Set a new password from a link |
| `profile` | `/profile` | My account — teacher and admin shell |

<!-- /gen -->

The first three carry **no guard** — whoever needs them is by definition someone who cannot
authenticate. `profile` carries `teacherGuard`; the same component also serves `/my/profile` in the
student shell, where the name and email fields are hidden.

**`forgot-password` and `reset-password` are lazily loaded**, unlike every other route here. They open
a few times a year, and importing them eagerly pushed the main bundle past its 2 MB budget — so every
load of the system paid for two screens almost nobody opens.

⚠️ **There is no `register` route, and no endpoint behind one.** The former
`POST /api/auth/register-teacher` was `[AllowAnonymous]`, so anyone who found the URL created a full
teacher account with nobody approving it. It was removed, not fixed.

## Functional Requirements

**AU-1** Login shall return **one identical message** for an unknown username, a wrong password and a
locked account (`B-11`).

**AU-2** Five consecutive failed logins shall lock the account for fifteen minutes (`B-12`).

**AU-3** The system shall not record a failed login while the account is already locked (`B-13`).

**AU-4** An expired lockout shall reset the failure counter (`B-14`).

**AU-5** A login error shall be displayed inline above the button in the error colour with an icon —
not as a toast.

**AU-6** On successful login the system shall route by role: a teacher or admin to the dashboard, a
student to «המסע שלי».

**AU-7** `forgot-password` shall return an **identical empty 200 on every path** — registered address,
unknown address, student account, and mail-server failure alike (`B-16`).

**AU-8** A failed reset email shall be written to the log, because it is the only trace that exists
(`B-16`, `AD-9`).

**AU-9** `reset-password` shall return **one generic message** for a missing, expired, superseded or
already-used token (`B-17`).

**AU-10** Requesting a new link shall invalidate every outstanding link for that user (`B-18`).

**AU-11** Only a hash of the reset token shall be stored (`B-19`).

**AU-12** A reset link shall expire one hour after it is issued.

**AU-13** Students shall be excluded from email recovery; a student's password shall be reset by her
teacher (`B-21`).

**AU-14** The password field shall show the four password rules live as the user types.

**AU-15** A username shall be immutable; an email shall be normalised to lowercase and shall be unique
(`B-23`).

**AU-16** A teacher or admin shall be able to change her own name and email; a student shall be able to
change only her password.

**AU-17** A 401 response shall be handled by the interceptor and shall return the user to login.

## Applicable Rules

| Rule | Why it reaches this area |
|---|---|
| `B-11` … `B-15` | login, lockout, and the two throttles |
| `B-16` … `B-21` | password recovery, end to end |
| `B-22`, `B-23`, `B-24` | who creates whom, and what is immutable |
| `B-20` | ⚠️ a reset does **not** revoke live sessions |
| `B-50` | ⚠️ the admin row that cannot recover |
| `B-51` | the token lives in browser storage, which is why nothing is rendered as HTML |
| `D-3`, `D-6`, `D-8` | focus, labels and contrast on the one screen every user meets |

## Acceptance Criteria

**AC-1 (AU-1)** Given three attempts — an unknown username, a real username with a wrong password, and
a locked account — when each is submitted, then all three responses are byte-identical.

**AC-2 (AU-2, AU-3)** Given five failed logins, when a sixth is attempted immediately, then the account
is locked and the failure counter does **not** increase further.

**AC-3 (AU-4)** Given a lockout that expired an hour ago and four earlier failures, when one more login
fails, then the counter reads 1, not 5.

**AC-4 (AU-7)** Given a registered address, an unknown address, and a student's account, when
`forgot-password` is called for each, then all three return an empty 200 and the responses are
indistinguishable — **including when the mail server is down**.

**AC-5 (AU-9)** Given an expired token and a never-issued token, when each is submitted to
`reset-password`, then both return the same message.

**AC-6 (AU-10)** Given an outstanding link, when a second link is requested, then the first stops
working.

**AC-7 (AU-16)** Given a student on `/my/profile`, when the screen renders, then the name and email
fields are absent and only the password section is shown.

**AC-8 (AU-6)** Given a student logs in successfully, when the redirect happens, then she lands on
`/my`, not on `/`.

## Screen Composition

*Filled in phase A5.* Expected to be a consistency pass rather than a redesign — three narrow, single-
purpose screens. Note that all three are among the **14 files carrying hardcoded colours** that A6 must
convert to tokens.

## Explicitly Not Supported

- **No self-registration**, for any role.
- **No SSO.** Recorded in `auth-plan.md` as deferred and still deferred; it would sit on the same
  `User` table with provider columns.
- **No refresh tokens and no "remember me".** When the token expires, the user logs in again.
- ⚠️ **A password reset does not end active sessions** (`B-20`). A session signed in with the old
  password stays valid until its token expires. Acceptable for a forgotten password; **do not assume a
  reset covers a stolen session.**
- **No forced password change on first login** — decided against; the password the teacher sets is
  permanent until changed.
- **A student cannot recover her own account.** She has no email.
- **No two-factor authentication.**
- **A username can never be changed.** The email is the mutable identifier.
