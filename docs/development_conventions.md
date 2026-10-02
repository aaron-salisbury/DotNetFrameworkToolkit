# DotNetFrameworkToolkit Development Conventions

## Purpose and scope

These conventions apply to human contributors and AI coding assistants. Prefer clear,
simple implementations and deliberate contracts over speculative abstractions.

DotNetFrameworkToolkit helps legacy applications use familiar dependency injection,
logging, validation, and result patterns. It does not prescribe an application's
Business/Data/Presentation layers, UI framework, persistence model, or composition root.

## Platform and dependencies

- The shipped library must remain on **.NET Framework 2.0 / CLR 2.0**. Do not
  retarget it to modern .NET or raise consumers' runtime requirements.
- Modern C# syntax is welcome when the configured modern compiler can compile it
  against the actual net20 reference assemblies without newer runtime dependencies.
  Language version and framework version are separate decisions.
- File-scoped namespaces, target-typed construction, and collection expressions
  may be used where their lowering works on net20. Do not introduce records,
  Task-based APIs, LINQ, AsyncLocal, ExceptionDispatchInfo, or other newer framework
  requirements without a deliberate, net20-compatible implementation decision.
- Packages and their transitive runtime dependencies must support net20. Keep
  Unity and Enterprise Library details inside their adapters where practical.
- The Cake.Frosting build executable targets modern .NET independently. Modern
  build-only APIs and packages must not leak into the distributed library.

## Organization and library contracts

- Keep shared primitives in `Core` and cohesive capabilities in `Modules`.
  Core must not accumulate application-specific rules or persistence policy.
- Keep implementation details internal unless consumers need a supported API.
  Public contracts should state behavior, ownership, failure modes, and threading.
- Prefer constructor injection and resolve services at application composition
  boundaries. The toolkit supplies adapters; consuming applications own startup.
- Preserve existing namespaces deliberately. Implementation subfolders need not
  introduce another namespace; do not move public types just to match folders.
- Do not add repositories, mappings, layers, or interfaces solely for symmetry.

## Results, exceptions, and validation

- Use `ProcessResult<T, TError>` for expected outcomes callers should branch on.
  Reserve the enum's zero/default value for success; use `Failure(error)` for failures.
- Use `ProcessResult<T>` for exceptions deliberately captured at a boundary.
  Unexpected exceptions should otherwise propagate. Never manufacture an exception
  merely to communicate an ordinary NotFound or validation outcome.
- Use `ValidationResult<T>` for validation feedback. Return snapshots rather than
  mutable backing collections; mutations must preserve validity and notifications.
- Prefer `Value` after checking success or `TryGet(out value)`. Boolean conversion
  describes operation success: a successful result containing `false` is still successful.
- Preserve the original exception and stack. Since net20 lacks ExceptionDispatchInfo,
  failed value access wraps the original exception as an inner exception.
- Logging must not replace an operation's original failure. Keep result construction
  separate from logging in new APIs; preserve diagnostics in compatibility helpers.

## Threading, lifetimes, and ownership

- State whether each type is thread-safe, thread-owned, or requires external coordination.
  UI validation objects are thread-owned; that restriction does not apply to every service.
- Synchronize compound collection updates and expose snapshots for concurrent readers.
  Snapshot mutable registration/configuration inputs at the documented boundary.
- Document who owns supplied instances and created resources. Dispose owned objects
  predictably and preserve multiple shutdown failures where appropriate.
- Drain active operations before teardown. Never dispose a lifetime from an active
  operation or synchronously wait on its disposal from a callback/constructor.
- Keep external callbacks outside collection locks. Do not synchronously marshal or
  wait for another thread while holding a lock it may need.
- Logging scopes are synchronous and thread-owned on net20; do not promise async
  propagation. Never log passwords, hashes, or other secret material.

## C# naming and formatting

- Use file-scoped namespaces and alphabetically ordered using directives without
  a separate System-first group. Place using directives outside the namespace.
- Prefer explicit local types and target-typed `new` where clear. Use built-in
  aliases such as `int`, `string`, and `bool`.
- Private fields use `_camelCase`; constants use `UPPER_SNAKE_CASE`; public and
  internal members use `PascalCase`; parameters and locals use `camelCase`.
- Use ordinary constructors for classes rather than primary constructors. Keep
  parameter lists on one line where readable.
- Use `string.Empty` for an empty string. There is no built-in EditorConfig rule
  that reliably enforces this preference; enforce it during review.
- **Always use braces for `if`, `else`, loops, `using` statements, and `lock`,
  even when the body contains only one statement.**
- Put braces on separate lines (Allman style). Expand method, constructor,
  operator, accessor, try/catch/finally, and lambda blocks. Put each executable
  statement on its own line; never compress several statements into one line.
- Methods, constructors, operators, and local functions use block bodies.
  Simple expression-bodied properties/indexers/accessors are acceptable. Auto-properties
  may remain on one line because they contain no executable statements.
- Use four spaces, LF line endings, UTF-8, a final newline, and no trailing whitespace.

```csharp
public bool TryGet(out T value)
{
    if (!IsSuccessful)
    {
        value = default;
        return false;
    }

    value = Value;
    return true;
}
```

Root `.editorconfig` defines editor preferences and enables IDE0011 (missing braces)
as a warning. An editor/analyzer must support these settings for diagnostics to appear;
the legacy project does not automatically make every style diagnostic a CI build check.

## Documentation, review, and validation

- Document public APIs with XML comments, including non-obvious ownership and threading
  constraints. Avoid copying application-only assumptions into general library contracts.
- Credit identifiable sources near substantially derived implementations when practical.
- Update compatibility notes whenever public APIs or stored formats change. Version 0.x
  may introduce deliberate breaking changes; document them rather than silently migrating data.
- Build through Cake using restored net20 reference assemblies. Debug remains available
  for local development; the Windows GitHub workflow builds and tests Release only.
  Verify CLR 2.0 assembly metadata as the workflow does.
- Unit tests target .NET Framework 4.8.1 and follow the same C# formatting rules.
  Run them through Cake with `--target=Test`; the default package path also runs tests.
  Tests execute on CLR 4 and do not prove CLR 2.0 compatibility. Keep the net20
  reference-assembly/metadata checks and separate legacy-runtime smoke verification.
  SQL CE integration tests deploy the matching private native runtime and run through
  the same Test task. This does not establish SQL CE operation on Framework 2.0-only machines.
- Before submitting generated code, check every touched control-flow body for braces
  and every executable block for multiline formatting. Editor settings do not replace review.

See the [roadmap](roadmap.md) for remaining compatibility and database work.
