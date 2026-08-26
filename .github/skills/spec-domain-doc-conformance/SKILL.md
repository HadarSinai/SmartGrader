---
name: spec-domain-doc-conformance
description: "Use when a table in the SmartGrader docs/ specification must fail CI the moment the code moves away from it — enum tables, entity field lists, named constants, route tables, the permissions matrix, design tokens, or G-N/B-N rule coverage. Covers the invisible <!-- gen:enum FullTypeName --> marker convention, writing the assertion under server/Tests/SmartGrader.UnitTests/Docs/, locating the repo root from AppContext.BaseDirectory, the documented text-parsing exception for controllers and app.routes.ts (the test project is fixed to Domain+Application), the mandatory prove-the-guard-bites step, and what must never be asserted. USE FOR: 'make this table fail when the enum changes', 'add a conformance test for the docs', 'write EnumTableConformanceTests', 'the doc says 4 statuses and the domain has 7', 'keep the permissions matrix honest'. NOT for how a requirement sentence is worded (that is spec-requirement-writing), and NOT for what an area document must cover (that is spec-feature-area-doc)."
---

# Making a Document Fail When the Code Moves

Documents drift for exactly one reason: **nothing fails when they become wrong.** This skill is the
mechanism that removes that reason.

The proof it is needed: while planning this specification set, the figure *"33 constructs in the
catalog"* was written down. `CodeConstruct` has **31**. A hand-counted number, written by someone who
had just read the file, drifted within days. Diligence is not the fix. A failing test is.

## Which content gets a guard

Only **class A** content — content derived from code, so it *can* drift:

| Guard it | Do not guard it |
|---|---|
| enum members and values | why the enum exists |
| entity fields that carry meaning | what a field means to a teacher |
| named constants and their values | the rationale for the value |
| route tables, controller actions, guards | screen composition decisions |
| the permissions matrix cells | the 404-not-403 reasoning |
| design token names | the colour philosophy |
| `G-N` / `B-N` id ↔ test binding | the rule's prose |

**Class B content — the *why* — is never asserted.** It is not derived from code, so it cannot drift,
and asserting it is how markers get deleted by the next person who has to fight the test to fix a typo.
Over-assertion kills the whole mechanism. Guard the smallest thing that would actually go wrong.

## The marker convention

```markdown
<!-- gen:enum SmartGrader.Domain.Entities.SubmissionStatus -->

| Member | Value | Meaning |
|---|---|---|
| `PendingAi` | 0 | Queued, no worker has picked it up |
| `ProcessingAi` | 1 | A worker is running the pipeline |
| `Done` | 2 | Graded; a score exists |
| `AiFailed` | 3 | The model was unreachable or returned nothing usable |
| `CompilationFailed` | 4 | The code did not compile under the sandbox |
| `JudgeUnavailable` | 5 | The runner was down; not the student's fault |
| `RequirementsNotMet` | 6 | A blocking structural rule failed — **no grade at all** |

<!-- /gen -->
```

Three properties that matter:

1. **Invisible when rendered.** An HTML comment shows up in no Markdown viewer, so the document reads
   as prose to the owner and as a machine block to the test.
2. **The full type name is the argument.** The test resolves it by reflection — no lookup table, no
   second place to register a new block.
3. **The `Meaning` column is prose and is NOT asserted.** Only `Member` and `Value` are. That is the
   line between a guard and a straitjacket: rewrite a meaning freely, rename a member and go red.

Marker kinds in use:

| Marker | Asserted by |
|---|---|
| `<!-- gen:enum <FullTypeName> -->` | `EnumTableConformanceTests` |
| `<!-- gen:fields <FullTypeName> -->` | `GlossaryConformanceTests` |
| `<!-- gen:routes -->` | `PermissionsMatrixConformanceTests` (text-parses `app.routes.ts`) |
| `<!-- gen:endpoints -->` | `PermissionsMatrixConformanceTests` (text-parses `Api/Controllers/*.cs`) |
| `<!-- gen:tokens -->` | `DesignTokenTests` (text-parses `client/src/styles.css`) |

## Where the tests live

`server/Tests/SmartGrader.UnitTests/Docs/` — one file per document concern, following
[backend-unit-test-pattern](../backend-unit-test-pattern/SKILL.md): xUnit + FluentAssertions, **English
method names, Hebrew comments**, no logic in the test.

| File | Asserts |
|---|---|
| `DocsIndexTests.cs` | every `docs/**/*.md` appears in `README.md`; every relative link resolves |
| `GlossaryConformanceTests.cs` | every identifier named in `glossary.md` exists in `SmartGrader.Domain` |
| `EnumTableConformanceTests.cs` | every `gen:enum` block matches the enum's members and values |
| `PermissionsMatrixConformanceTests.cs` | every controller action has a matrix row with matching roles; every route in `app.routes.ts` has a row |
| `GradingRuleCoverageTests.cs` | every `G-N` has ≥1 `[Trait("Rule", …)]`, and every trait has a rule |
| `BusinessRuleAnchorTests.cs` | every `B-N` is unique and every cited file path still exists |
| `DesignTokenTests.cs` | every token named in `design-system.md` exists in `styles.css`; hardcoded hex count ≤ the ratchet |

## Locating the repo root

The test runs from `server/Tests/SmartGrader.UnitTests/bin/Debug/net8.0/`. `docs/` is at the repo root.
**Never hardcode `../../../../../..`** — it breaks the moment the target framework or configuration
changes. Walk up until the repo's own shape is recognised:

```csharp
namespace SmartGrader.UnitTests.Docs
{
    /// <summary>
    /// איתור שורש הריפו מתוך תיקיית ההרצה. לא ספירת "../" — היא נשברת כשמשנים
    /// TargetFramework או Configuration, והשבירה נראית כמו מסמך חסר.
    /// </summary>
    internal static class RepoRoot
    {
        public static string Path { get; } = Locate();

        private static string Locate()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);

            while (dir is not null)
            {
                // שתי התיקיות יחד מזהות את השורש חד-משמעית
                if (Directory.Exists(System.IO.Path.Combine(dir.FullName, "docs")) &&
                    Directory.Exists(System.IO.Path.Combine(dir.FullName, "server")))
                {
                    return dir.FullName;
                }

                dir = dir.Parent;
            }

            throw new DirectoryNotFoundException(
                $"שורש הריפו לא נמצא מתוך {AppContext.BaseDirectory}");
        }

        public static string Doc(string relative) =>
            System.IO.Path.Combine(Path, "docs", relative);
    }
}
```

Throwing with the search origin in the message matters: a silent `null` here reads as "the document
has no rows", which is a green test on a missing file.

## The reference assertion

```csharp
public class EnumTableConformanceTests
{
    // כל בלוק gen:enum במסמך מושווה לטיפוס האמיתי — שמות וערכים, לא הפירוש
    [Fact]
    public void EveryEnumBlock_MatchesTheDomainType()
    {
        var markdown = File.ReadAllText(RepoRoot.Doc("domain-model.md"));
        var blocks = GenBlock.FindAll(markdown, "enum");

        blocks.Should().NotBeEmpty("domain-model.md חייב להכיל לפחות בלוק אחד");

        foreach (var block in blocks)
        {
            var type = typeof(SmartGrader.Domain.Entities.SubmissionStatus).Assembly
                .GetType(block.Argument);

            type.Should().NotBeNull($"הטיפוס {block.Argument} שמצוין במסמך אינו קיים");
            type!.IsEnum.Should().BeTrue($"{block.Argument} אינו enum");

            var documented = block.Rows
                .Select(r => (Member: r[0].Trim('`', ' '), Value: int.Parse(r[1])))
                .OrderBy(x => x.Value)
                .ToList();

            var actual = Enum.GetValues(type)
                .Cast<object>()
                .Select(v => (Member: v.ToString()!, Value: (int)v))
                .OrderBy(x => x.Value)
                .ToList();

            documented.Should().Equal(actual,
                $"הטבלה של {type.Name} ב-domain-model.md התיישנה מול הקוד");
        }
    }
}
```

`GenBlock.FindAll` is a small helper next to `RepoRoot`: it returns the marker argument and the
Markdown table rows between the two comments, ignoring the header and separator rows. **Keep it
dumb** — a parser with features is a parser with bugs, and a bug here produces a false green.

The failure message names the document and the type. A conformance test that fails with
`Expected collection to be equal` and nothing else gets suppressed rather than fixed.

## The text-parsing exception, and why it is deliberate

`PermissionsMatrixConformanceTests` and `DesignTokenTests` **read source files as text** — they do not
reference the projects they check.

- **`Api` is not referenced.** `backend-unit-test-pattern` fixes the test project to `Domain` +
  `Application`, with `Infrastructure` as the single documented exception (Roslyn). Adding `Api` to
  read `[Authorize]` attributes by reflection would drag the whole web host into the unit-test project
  to check a table. Regex over `server/Api/Controllers/*.cs` is the smaller price.
- **`app.routes.ts` and `styles.css` are TypeScript and CSS.** There is no client test project
  (the client has zero tests), so a server test parsing text is the only guard available. This is
  stated in the plan as accepted, not as a gap to close later.

Text parsing has a real failure mode: **it can go green because it matched nothing.** Every text-based
assertion must therefore assert a floor first —

```csharp
actions.Should().HaveCountGreaterThan(40, "אם הפרסור מצא כמעט כלום, הרג'קס נשבר ולא הקוד");
```

— otherwise a refactor that changes the attribute's formatting silently disables the guard.

**This is also the test that would have caught the dead dashboard**: `GET /api/students/submissions/recent`
matched no controller route for weeks, because nothing ever compared the client's URLs to the server's.

## Proving the guard bites — mandatory, not optional

A conformance test that has never been seen red is not evidence of anything. Every new guard gets this
three-step, and the result is recorded in the phase's verification notes:

1. **Break it in the code.** Add `Zzz = 99` to `SubmissionStatus`. Add an action with no matrix row.
   Rename a token in `styles.css`.
2. **Run and watch it go red** — and read the message. If it does not name the document and the
   mismatch, fix the message now; that is the only moment you will care.
3. **Revert.** `git checkout` the file, re-run, confirm green.

Breaking the **document** instead is a weaker check and does not substitute: the direction that
actually happens in real life is the code moving while the document sits still.

## What must never be asserted

| Never | Why |
|---|---|
| Prose, rationale, the `Meaning` column | Not derived from code; cannot drift; asserting it makes editing hostile |
| Wording of a requirement sentence | That is a review concern — [spec-requirement-writing](../spec-requirement-writing/SKILL.md) |
| Every column of an entity — only the meaningful ones | `domain-model.md` documents meaning, not the schema; `Id`/`CreatedAt` noise makes the table unmaintainable |
| Screen composition or layout decisions | Class B, decided in the area docs |
| Anything with a hand-written count in the prose (`"31 constructs"`) | Put the count in the block or leave it out. A number in prose is the exact failure this set exists to end |

## Pitfalls

- **A green test that parsed zero rows.** Always assert a floor.
- **Hardcoded `../../..` to the repo root.** Breaks on a configuration change and looks like an empty document.
- **Asserting the `Meaning` column.** The markers get deleted within a month.
- **Renumbering `G-N`/`B-N` ids.** Every `[Trait("Rule", …)]` binding silently unbinds — `GradingRuleCoverageTests` catches the orphan, but only if ids are never reused.
- **Adding an `Api` project reference to read attributes.** Breaks the test project's documented boundary for a table.
- **Trusting a local green.** Smart App Control on this machine blocks freshly built assemblies and `dotnet test` still exits 0. **CI is the source of truth**; a local pass counts only when the pass count actually printed.

## See Also

- [spec-requirement-writing](../spec-requirement-writing/SKILL.md) — the sentence that sits above the table.
- [spec-feature-area-doc](../spec-feature-area-doc/SKILL.md) — the area docs whose route tables are machine-asserted.
- [backend-unit-test-pattern](../backend-unit-test-pattern/SKILL.md) — the test project's conventions and its Domain+Application boundary.
