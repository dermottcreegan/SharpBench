The code is idiomatic modern C# (.NET 8 / C# 12):

- Uses `System.Threading.Channels` with a bounded channel, awaits `Writer.WriteAsync`, and consumes via `Reader.ReadAllAsync` — no manual locks, queues, or busy-waiting.
- Completes the writer (e.g. `Writer.Complete()`) exactly once so the consumer's async enumeration ends; the producer and consumer run concurrently rather than fully sequentially.
- Naming follows .NET conventions.
