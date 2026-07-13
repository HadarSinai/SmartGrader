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
| `~/.copilot/skills/<name>/` | Personal (roaming)  |

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
- [ ] `description` includes "when to use" + relevant keywords, wrapped in quotes if it contains `:`.
- [ ] The file is located at `.github/skills/<name>/SKILL.md` (or another appropriate scope location).
- [ ] The body includes: when to use + step-by-step workflow, with at least 3 concrete examples under "When to Use" (if possible).
- [ ] Additional files (if any) are linked with a relative `./` path.
- [ ] The file length is reasonable (under 500 lines); heavy content moved to `references/`.
