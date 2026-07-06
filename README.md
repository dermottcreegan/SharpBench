# SharpBench

[![CI](https://github.com/dermottcreegan/SharpBench/actions/workflows/ci.yml/badge.svg)](https://github.com/dermottcreegan/SharpBench/actions/workflows/ci.yml)

**Which LLM writes the best C#?** A reproducible benchmark of frontier models on real-world C# tasks — async correctness, LINQ pitfalls, spans, nullability, and legacy refactoring — built on [NetEval](https://github.com/dermottcreegan/NetEval), an LLM eval framework for .NET by the same author.

## How it works

Every model gets the same system prompt and the same task set — 14 tasks so far, building toward 48 (see [`tasks/README.md`](tasks/README.md) for the per-category backlog). Each answer is scored on two independent axes:

1. **Functional (weight 0.7, objective)** — `RoslynTestJudge` compiles the model's code together with a hidden xUnit test file and runs the tests. Score = fraction of hidden tests passing.
2. **Idiom (weight 0.3, LLM-judged)** — NetEval's `ChatClientJudge` scores the code against a per-task style rubric. One fixed judge model scores every contestant, and all judge transcripts are published with the results.

A task counts as **passed** when all hidden tests pass *and* the weighted score reaches 0.7. The idiom judge is advisory: it moves the score (and the rankings), but a style nitpick alone cannot veto a functionally correct solution.

Sampling is stochastic, so each task is generated **3 times** by default (`--runs <n>` overrides; use `--runs 1` for cheap smoke runs). A task counts as passed when at least half its generations pass; scores average over all generations.

Latency and contestant token usage are measured per generation; raw per-task outputs and verdicts are committed under `results/` as JSONL.

## Results

Full run, 2026-07-05 — 14 tasks × 3 generations per model, idiom judge `claude-opus-4-8`.
Regenerate with `dotnet run --project src/SharpBench.Runner -- --report`.

| Model | Passed | Gens/task | Mean score | Mean latency | Tokens in / out | Cost / run |
|---|---|---|---|---|---|---|
| gemini-2.5-pro | 14/14 | 3 | 0.988 | 16.3 s | 9,159 / 96,660 | $0.978 |
| claude-sonnet-5 | 14/14 | 3 | 0.986 | 4.0 s | 12,978 / 11,091 | $0.205 |
| gpt-4o | 14/14 | 3 | 0.980 | 1.5 s | 8,883 / 4,520 | $0.067 |
| claude-opus-4-8 | 13/14 | 3 | 0.961 | 3.3 s | 12,978 / 7,334 | $0.248 |
| ollama:qwen2.5-coder:7b | 11/14 | 3 | 0.765 | 12.7 s | 8,943 / 4,918 | $0 (local) |

*A task passes when at least half its generations pass; score and latency average over all generations.*
*Cost covers every generation, at list per-MTok rates from [`pricing.json`](pricing.json) (as of 2026-07-05).*

Three frontier models pass 14/14; cost and latency separate them sharply. Gemini 2.5 Pro tops
the score column but spends ~97K output tokens (mostly reasoning) — ~15× gpt-4o's price for
roughly the same pass rate. Claude Opus 4.8 dropped `generics/generic-sum` by calling
`ArgumentNullException.ThrowIfNull` without `using System;` in 2 of 3 generations — candidates
compile without implicit usings, and the contract requires every using directive. It's a one-keystroke
fix in an IDE, but a hard failure for any pipeline that compiles model output directly — the gap
between "looks right" and "compiles" is precisely what this benchmark measures. It's also the
multi-generation protocol working as intended: the earlier single-generation run happened to
sample the one generation where the directive was present.

### Mean score by category

| Category | gemini-2.5-pro | claude-sonnet-5 | gpt-4o | claude-opus-4-8 | ollama:qwen2.5-coder:7b |
|---|---|---|---|---|---|
| async | 0.97 | 0.91 | 0.90 | 0.98 | 0.35 |
| concurrency | 0.99 | 0.99 | 0.98 | 1.00 | 0.69 |
| generics | 0.98 | 1.00 | 0.99 | 0.76 | 0.99 |
| idiom | 1.00 | 1.00 | 1.00 | 1.00 | 0.91 |
| linq | 0.98 | 0.98 | 0.96 | 0.99 | 0.75 |
| nullability | 0.98 | 0.99 | 0.98 | 1.00 | 0.98 |
| refactoring | 1.00 | 1.00 | 1.00 | 1.00 | 0.52 |
| spans | 0.99 | 0.98 | 0.98 | 0.99 | 0.72 |

### Failed tasks

- claude-opus-4-8: generics/generic-sum (1/3 generations passed, mean score 0.53)
- ollama:qwen2.5-coder:7b: async/cancellation-propagation (1/3 generations passed, mean score 0.35)
- ollama:qwen2.5-coder:7b: refactoring/modernize-describe (1/3 generations passed, mean score 0.27)
- ollama:qwen2.5-coder:7b: spans/sum-csv-ints (1/3 generations passed, mean score 0.45)

## Run it yourself

SharpBench consumes [NetEval](https://github.com/dermottcreegan/NetEval) from NuGet, so a clone builds standalone:

```
dotnet run --project src/SharpBench.Runner                    # list the task inventory (no keys needed)
dotnet run --project src/SharpBench.Runner -- --model <label> # benchmark one model (3 generations/task)
dotnet run --project src/SharpBench.Runner -- --model <label> --runs 1  # cheap single-generation smoke run
dotnet run --project src/SharpBench.Runner -- --report        # markdown leaderboard (no keys needed)
dotnet run --project src/SharpBench.Runner -- --rejudge <label>  # re-score committed generations with a different idiom judge
```

`<label>` selects the provider. Frontier model IDs are self-identifying; Ollama uses an explicit prefix:

| Label | Provider | Key (env var) |
|---|---|---|
| `claude-sonnet-5`, `claude-opus-4-8`, … | Anthropic | `ANTHROPIC_API_KEY` |
| `gpt-4o`, `o3-mini`, … | OpenAI | `OPENAI_API_KEY` |
| `gemini-2.5-pro`, … | Google Gemini | `GEMINI_API_KEY` (or `GOOGLE_API_KEY`) |
| `ollama:qwen2.5-coder:7b`, … | Ollama (local) | — (`OLLAMA_HOST`, default `http://localhost:11434`) |

An explicit `provider:model` prefix (`openai:gpt-4o`, `claude:…`, `gemini:…`) also works. Any
`Microsoft.Extensions.AI.IChatClient` is supported — add providers in `ChatClientFactory`.

The idiom judge is one fixed model for every contestant, defaulting to `claude-opus-4-8`
(override with `SHARPBENCH_JUDGE_MODEL`; e.g. an `ollama:` model for offline smoke runs).
To measure how much the judge's identity matters, `--rejudge <label>` re-scores the
committed generations with a different idiom judge — no new generations, so any leaderboard
delta is attributable to the judge alone — and writes a parallel result set under
`results-rejudge/<label>/`.

Each candidate's code is compiled and executed in a disposable `SharpBench.Sandbox` child process
with a hard wall-clock timeout, so a runaway or hostile answer can't wedge the harness (see
"Honest limitations" below for what that isolation does and doesn't cover).

## Honest limitations

- Hidden tests stop being hidden the moment this repo is public; future models may train on them. The task set is versioned (`tasks-v1`) so it can be rotated without invalidating old results.
- The idiom layer is LLM-as-judge and inherits that bias; that's why it's the minority weight and fully transcript-published.
- A candidate's code runs in a disposable `SharpBench.Sandbox` child process (compiled fresh per candidate, loaded into a collectible `AssemblyLoadContext`), and the parent kills the whole process tree on a hard wall-clock timeout — so a hanging or `Environment.Exit()`-calling candidate can't wedge the harness. That timeout-and-kill is the only boundary, though: nothing inside the sandbox process currently blocks filesystem or network access, so it isn't safe to point at untrusted, adversarial submissions without an added OS-level boundary (container, restricted user, no network).
- The idiom layer can still swing rankings even though it can no longer veto a pass: a functionally perfect solution scores between 0.70 and 1.00 depending entirely on one LLM's style opinion. Read the score column with that in mind.

## License

MIT
