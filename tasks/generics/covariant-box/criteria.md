The code is idiomatic modern C# (.NET 8 / C# 12):

- Declares the interface type parameter covariant with `out T`, so covariance is expressed in the type system rather than worked around with casts at call sites.
- `Box<T>` is a minimal immutable holder (value set once, read-only property); naming follows .NET conventions.
