---
name: create-skill
description: "Use when creating, reviewing, or fixing a SKILL.md file for VS Code Copilot Agent Skills. השתמש כאשר יוצרים, בונים, בודקים או מתקנים קובץ סקיל חדש, מגדירים frontmatter, מבנה תיקיות, או description לגילוי אוטומטי."
---

# Rules for Creating a SKILL.md File

A Skill is a folder with instructions + assets (scripts, templates, reference docs) that the AI loads **on demand** when a task matches its `description`. Unlike instructions (which are always loaded or loaded based on `applyTo`), a skill is only loaded when needed — this saves context.

## When to Use a Skill (instead of instructions/prompt)

| Type                               | When it fits                                                                                |
| ---------------------------------- | ------------------------------------------------------------------------------------------- |
| Instructions (`*.instructions.md`) | A rule that always applies, or applies based on a file pattern (`applyTo`)                  |
| Prompt (`*.prompt.md`)             | A one-time, focused action with parameters                                                  |
| **Skill**                          | A repeatable, specific workflow with accompanying assets (scripts/templates/reference docs) |
| Custom Agent (`*.agent.md`)        | Needs context isolation or different tool restrictions per stage                            |

If a task "repeats itself" only in a specific context (e.g. "test a website with Playwright", "create a PR", "write a new skill") — that's a good candidate for a Skill.

## Folder Structure

```
.github/skills/<skill-name>/
├── SKILL.md           # Required, the name field must match the folder name
├── scripts/           # Executable code (optional)
├── references/        # Additional docs loaded only when needed (optional)
└── assets/            # Templates, boilerplate (optional)
```

Possible locations (by scope):

| Path                        | Scope               |
| --------------------------- | ------------------- |
| `.github/skills/<name>/`    | Project (workspace) |
| `.claude/skills/<name>/`    | Project (workspace) — the mirror, see below |
| `~/.copilot/skills/<name>/` | Personal (roaming)  |

## Mirroring: every project skill is written twice

This repository is worked on by two tools that read skills from two different folders. A skill that
exists in only one of them is invisible to the other — silently, with no error. So **every project
skill lives at both paths, byte-identical**:

```
.github/skills/<name>/SKILL.md      ← Copilot discovers here
.claude/skills/<name>/SKILL.md      ← Claude Code discovers here
```

Until now this rule lived only in the root `CLAUDE.md` ("Mirrored into `.claude/skills/` … keep both
copies in sync when a pattern changes"), which is the wrong place for it: someone writing a skill
opens *this* file, not that one.

**Rules:**

1. **Both copies, or neither.** Create, edit and delete in pairs — including `references/`, `scripts/`
   and `assets/` subfolders.
2. **Byte-identical.** Not "equivalent". Relative links inside a skill (`../other-skill/SKILL.md`)
   resolve the same way under both roots precisely *because* the trees are identical — one adjusted
   path breaks that.
3. **Never link across the two roots.** A skill under `.claude/skills/` must not link into
   `.github/skills/`, or the mirror stops being a mirror and becomes an alias.
4. **Write one, copy it.** Do not hand-edit the second copy; copy the file. Two hand-typed "identical"
   files diverge on the first typo fix.

**Verification — run after any skill change:**

```bash
diff <(ls .claude/skills) <(ls .github/skills)          # folder sets identical
diff -r .claude/skills .github/skills                    # contents identical
```

Both must print nothing.

## Frontmatter Format

```yaml
---
name: skill-name # Required: 1-64 chars, lowercase letters + numbers + hyphens, must match the folder name
description: "What the skill does and when to use it. Up to 1024 chars."
argument-hint: "Optional hint shown when invoked as a slash command"
user-invocable: true # Optional: whether to show it as a slash command (default: true)
disable-model-invocation: false # Optional: if true, the AI won't auto-load the skill based on description
---
```

### Especially Important: `description`

The `description` field is the skill's **only discovery surface** — the AI decides whether to load the skill _solely_ based on `name` + `description` (about 100 tokens), without reading the rest of the file. Therefore:

- Include keywords/phrases that users would actually say ("use when...", "USE FOR:", synonyms).
- Also specify when **not** to use the skill, if there's overlap with other tools/skills.
- If there's a colon (`:`) inside the text — wrap the entire value in quotes (`description: "Use when: doing X"`), otherwise the YAML will silently break.

#### The convention used by every skill in this repository

Four parts, in this order. Every `backend-*`, `client-*` and `spec-*` skill here follows it, and a new
skill that does not will lose the discovery contest against the ones that do.

```
Use when <the situation, named concretely — not the topic>: <the specific
artifacts, files or symptoms>. Covers <the two or three things the body
actually teaches that a reader could not guess>. USE FOR: '<what the user
literally types>', '<a second phrasing>', '<a symptom, in the user's words>'.
NOT for <the neighbouring case> (that is <sibling-skill-name>), and NOT for
<a second neighbouring case> (that is <other-sibling>).
```

| Part | Purpose | Failure it prevents |
| ---- | ------- | ------------------- |
| `Use when …` | The trigger situation, with real file paths and symbol names | A description written as a topic ("about grading") matches everything and gets loaded for nothing |
| `Covers …` | The non-obvious content | Two skills whose triggers overlap; this is what separates them |
| `USE FOR: '…'` | Quoted phrases in the **user's** words, including symptoms | The skill is written from the author's vocabulary and the user never says those words |
| `NOT for … (that is <sibling>)` | Hands off to the named sibling | Two skills load together and contradict each other |

Rules that matter more than they look:

- **Name the sibling skill explicitly.** "NOT for other cases" routes nobody. `NOT for the CQRS handler
  shell itself (see backend-mediatr-query-handler-pattern)` does.
- **Put symptoms in `USE FOR:`, not just tasks.** People arrive with `'the feedback leaked the expected
  output'` far more often than with `'change the AI feedback prompt'`.
- **The description is subject to the mirroring rule too** — and it is the part people forget to copy,
  because it is a single line at the top rather than a visible block of body text.
- **If a skill's description names a file path, that path is part of the discovery surface.** Moving or
  deleting the file means editing the description, not only the body.

## File Body

Recommended structure:

1. **Title** — a clear name for the skill.
2. **When to Use** — concrete triggers and use cases (include at least 3 examples if possible).
3. **Workflow** — clear, numbered, actionable steps.
4. **References to assets** — relative links to additional files, e.g. `[script](./scripts/test.js)`.
5. (If relevant) **Common Pitfalls / Anti-patterns**.

## Minimal Template to Copy

```markdown
---
name: my-skill-name
description: "Short description of what the skill does and when to invoke it, with discovery keywords."
---

# Skill Name

## When to Use

- Use case 1
- Use case 2

## Workflow

1. First step
2. Second step
3. [Run script](./scripts/run.js)
```

## Core Rules

1. **Keyword-rich description** — this is the only way the skill will be "found".
2. **Progressive loading** — the AI reads only `name` and `description` for discovery. The rest of the file is loaded only upon invocation. Keep `SKILL.md` short (under 500 lines) and move heavy content or code to the `references/` or `scripts/` folders.
3. **Relative paths** — always use `./` for skill files, no more than one level deep.
4. **Self-contained** — all the procedural knowledge needed is in the file (or referenced from it), with no dependency on external context.

## Checklist Before Finishing

- [ ] `name` matches the folder name exactly (lowercase + hyphens).
- [ ] `description` follows the four-part convention (`Use when` / `Covers` / `USE FOR:` / `NOT for … (that is <sibling>)`), wrapped in quotes if it contains `:`.
- [ ] The `NOT for` clause names a real sibling skill by name.
- [ ] The file is located at `.github/skills/<name>/SKILL.md` (or another appropriate scope location).
- [ ] **Mirrored**: the identical folder exists at `.claude/skills/<name>/`, and `diff -r .claude/skills .github/skills` prints nothing.
- [ ] The body includes: when to use + step-by-step workflow, with at least 3 concrete examples under "When to Use" (if possible).
- [ ] Additional files (if any) are linked with a relative `./` path.
- [ ] The file length is reasonable (under 500 lines); heavy content moved to `references/`.
