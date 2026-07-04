# Task format

Every task lives at `tasks/<category>/<task-id>/` and contains three required files, plus two
optional validation files:

| File | Consumed by | Purpose |
|---|---|---|
| `prompt.md` | the contestant model | What to write. Must pin exact type and method signatures so the hidden tests can call the result. |
| `criteria.md` | the LLM idiom judge (`ChatClientJudge`, weight 0.3) | Style rubric: modern idioms, allocations, naming, nullability. |
| `HiddenTests.cs` | the Roslyn judge (`RoslynTestJudge`, weight 0.7) | Self-contained xUnit test file compiled *together with* the model's output and executed. |
| `reference.cs` *(optional)* | `TaskContractTests` | A correct solution the hidden tests must fully accept (score 1.0). |
| `wrong.cs` *(optional)* | `TaskContractTests` | A classic-wrong solution the hidden tests must reject (score < 1.0). |

**Provide `reference.cs` and `wrong.cs` for every task.** When both are present, `TaskContractTests`
(in `tests/SharpBench.Judging.Tests`) validates the task on every CI run: the reference must pass all
hidden tests and the wrong solution must fail at least one. This is what proves a task's hidden tests
actually measure what they claim — a task whose tests pass a broken answer, or fail a good one, fails
CI. The runner ignores these two files; only the tests read them.

Rules for `HiddenTests.cs`:

- **Explicit usings only** — the file is compiled at runtime without ImplicitUsings, so `using System;` etc. must be spelled out.
- Only `xunit.assert` + `xunit.core` are referenced: `[Fact]`, `Assert.*`, and the .NET 8 BCL. No `[Theory]` (the lightweight runner only discovers `[Fact]`), no other packages.
- Each fact must finish within the 10s per-test timeout even when the candidate misbehaves (e.g. pair an infinite `Task.Delay` with `CancelAfter`).
- Test the *contract*, not the implementation — plus one fact per classic bug the task is designed to catch (double enumeration, `max = 0` init, missing token propagation…).
- These files are not compiled by `dotnet build`; a task is only validated when the runner executes it. Keep the C# conservative.

## Categories (target: 6 tasks each)

| Category | Status |
|---|---|
| `async` — cancellation, sync-over-async, async streams | 1/6 |
| `linq` — deferred execution, single-pass, grouping | 1/6 |
| `idiom` — records, pattern matching, collection expressions | 2/6 |
| `spans` — allocation-free parsing, `ArrayPool` | 2/6 |
| `generics` — variance, generic math (`INumber<T>`) | 2/6 |
| `nullability` — clean `#nullable enable` API design | 2/6 |
| `concurrency` — `Channel<T>`, lock-free caches | 2/6 |
| `refactoring` — modernize legacy code without behavior change | 2/6 |

Every category now has at least one exemplar task with a validated `reference.cs`/`wrong.cs` pair;
the remainder (to reach 6 each) are the backlog, each held to the same contract on the way in.

Version the whole set (this is `tasks-v1`): once public, tasks may leak into training data, so rotate tasks by version rather than editing them in place.
