# SharpBench

[![CI](https://github.com/dermottcreegan/SharpBench/actions/workflows/ci.yml/badge.svg)](https://github.com/dermottcreegan/SharpBench/actions/workflows/ci.yml)

**Which LLM writes the best C#?** A reproducible benchmark of frontier models on real-world C# tasks — async correctness, LINQ pitfalls, spans, nullability, and legacy refactoring — built on [NetEval](https://github.com/dermottcreegan/NetEval), an LLM eval framework for .NET by the same author.

## How it works

Every model gets the same system prompt and the same task set — 14 tasks so far, building toward 48 (see [`tasks/README.md`](tasks/README.md) for the per-category backlog). Each answer is scored on two independent axes:

1. **Functional (weight 0.7, objective)** — `RoslynTestJudge` compiles the model's code together with a hidden xUnit test file and runs the tests. Score = fraction of hidden tests passing.
2. **Idiom (weight 0.3, LLM-judged)** — NetEval's `ChatClientJudge` scores the code against a per-task style rubric. One fixed judge model scores every contestant, and all judge transcripts are published with the results.

A task counts as **passed** when all hidden tests pass *and* the weighted score reaches 0.7. The idiom judge is advisory: it moves the score (and the rankings), but a style nitpick alone cannot veto a functionally correct solution.

Latency and contestant token usage are measured per generation; raw per-task outputs and verdicts are committed under `results/` as JSONL.

## Results

*Coming with the first full run.*

## Run it yourself

SharpBench consumes [NetEval](https://github.com/dermottcreegan/NetEval) from NuGet, so a clone builds standalone:

```
dotnet run --project src/SharpBench.Runner                    # list the task inventory (no keys needed)
dotnet run --project src/SharpBench.Runner -- --model <label> # benchmark one model
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
