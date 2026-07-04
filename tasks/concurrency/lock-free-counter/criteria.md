The code is idiomatic modern C# (.NET 8 / C# 12):

- Uses an atomic operation (`Interlocked.Increment`, and `Interlocked.Read` for the 64-bit read) rather than a plain `_value++`, which is not atomic and loses updates under contention.
- Does not reach for a `lock` where a lock-free interlocked operation expresses a simple counter more directly.
- Naming follows .NET conventions.
