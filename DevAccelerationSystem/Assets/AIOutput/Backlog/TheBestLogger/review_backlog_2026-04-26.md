# TheBestLogger Review Backlog

## Scope
- Package: `TheBestLogger`
- Source root: `Assets/TheBestLogger/`
- Review date: `2026-04-26`
- Review mode: source inspection only

## Context
- This logger is actively used in production on large projects.
- `OpenSearchLogTargetConfiguration` has a real remote-config merge path through `Merge(...)`, so `OpenSearch` should be treated as a production-sensitive surface, not only a sample artifact.
- Because of that production context, public API correctness, threaded delivery correctness, and remote-target stability should be treated as backlog items with elevated priority.

Relevant evidence:
- [OpenSearchLogTargetConfiguration.cs](../../../../Assets/TheBestLogger/Runtime/Examples/LogTargets/OpenSearch/OpenSearchLogTargetConfiguration.cs)
- [OpenSearchLogTarget.cs](../../../../Assets/TheBestLogger/Runtime/Examples/LogTargets/OpenSearch/OpenSearchLogTarget.cs)

## Findings

### 1. Broken public generic `LogFormat` overloads
- Severity: `high`
- Files:
  - [ILogger.cs](../../../../Assets/TheBestLogger/Runtime/Core/ILogger.cs)
  - [CoreLogger.cs](../../../../Assets/TheBestLogger/Runtime/Core/CoreLogger.cs)
- Problem:
  - public convenience overloads for `LogFormat<T1, T2>` and `LogFormat<T1, T2, T3>` silently forward only `arg1`
  - callers using the public generic API can get wrong formatted output without obvious failure
- Evidence:
  - `ILogger` exposes these overloads as public contract
  - implementation in `CoreLogger` routes both overloads to `LogFormat(level, message, null, arg1)` and drops remaining arguments
- Production impact:
  - formatting bugs in operational logs
  - corrupted diagnostic payloads in high-value production investigations
  - hard-to-detect regressions because logs still appear, but with wrong content
- Recommended action:
  - fix forwarding so the 2-arg and 3-arg overloads pass every argument
  - add direct unit tests for these overloads through public `ILogger`

### 2. Batched log payload is not immutable across async main-thread dispatch
- Severity: `high`
- Files:
  - [LogTargetBatchLogsDecoration.cs](../../../../Assets/TheBestLogger/Runtime/Core/LogTarget/LogTargetDecorations/LogTargetBatchLogsDecoration.cs)
  - [LogTargetDispatchingLogsToMainThreadDecoration.cs](../../../../Assets/TheBestLogger/Runtime/Core/LogTarget/LogTargetDecorations/LogTargetDispatchingLogsToMainThreadDecoration.cs)
- Problem:
  - `LogTargetBatchLogsDecoration` builds a batch on top of reusable thread-local `List<LogEntry>`
  - it passes `batch.AsReadOnly()` to downstream consumers
  - if downstream dispatch posts that batch asynchronously to the Unity main thread, the underlying list can be cleared and reused before consumption
- Evidence:
  - thread-local cache is reused in `GetLogsBatch(...)`
  - `AsReadOnly()` is only a wrapper, not a copy
  - dispatch decoration uses `SynchronizationContext.Post(...)`
- Production impact:
  - dropped logs
  - reordered or corrupted batched payloads
  - highest risk when threaded sources, batching, and main-thread dispatch are enabled together
  - especially relevant if production remote config can enable or tune OpenSearch delivery behavior
- Recommended action:
  - snapshot batch contents before async dispatch
  - add a regression test that proves dispatched batches are immutable after posting

### 3. Apple exception logging path has broken fallback and null-safety bug
- Severity: `medium`
- File:
  - [AppleSystemLogTarget.cs](../../../../Assets/TheBestLogger/Runtime/Core/LogTarget/AppleSystemLogTarget/AppleSystemLogTarget.cs)
- Problem:
  - exception path uses `exception.Message + "\n" + logAttributes.StackTrace ?? exception.StackTrace`
  - because of operator precedence, `exception.StackTrace` is not a real fallback
  - if `logAttributes` is null, the logger can throw while trying to log the original exception
- Production impact:
  - broken exception emission on Apple targets
  - reduced confidence in stability and crash-adjacent logging
- Recommended action:
  - make fallback explicit and null-safe
  - add targeted Apple-target formatting test coverage

### 4. Dispose path currently double-disposes wrapped targets
- Severity: `medium`
- Files:
  - [LogManager.cs](../../../../Assets/TheBestLogger/Runtime/Core/LogManager.cs)
  - [LogTargetBatchLogsDecoration.cs](../../../../Assets/TheBestLogger/Runtime/Core/LogTarget/LogTargetDecorations/LogTargetBatchLogsDecoration.cs)
  - [LogTargetDispatchingLogsToMainThreadDecoration.cs](../../../../Assets/TheBestLogger/Runtime/Core/LogTarget/LogTargetDecorations/LogTargetDispatchingLogsToMainThreadDecoration.cs)
- Problem:
  - decorations dispose their inner targets
  - `LogManager.Dispose()` later disposes both `_originalLogTargets` and `_decoratedLogTargets`
  - this depends on accidental idempotency of every target
- Production impact:
  - cleanup fragility
  - elevated shutdown/reinit risk if target implementations become less forgiving
- Recommended action:
  - make ownership explicit: either outer decorators own disposal or `LogManager` owns the full graph, but not both
  - add dispose-idempotency coverage

## Coverage Gaps
- Current tests cover some decorators, but do not directly cover:
  - public generic `LogFormat` forwarding contract
  - immutable batch snapshot behavior across posted dispatch
  - Apple target exception formatting
  - dispose ownership across original plus decorated targets

## Priority Notes
- `OpenSearch` should stay under active review because remote config can change effective production behavior without a code redeploy.
- Findings 1 and 2 should be treated as priority candidates before further broadening package adoption in large projects.
- Finding 3 should rise in priority if Apple unified logging is part of production observability or incident workflows.

## Suggested Next Steps
1. Fix the broken `LogFormat` forwarding overloads.
2. Fix batch snapshot ownership before async dispatch.
3. Fix Apple exception formatting and null handling.
4. Add focused regression tests for all three paths.

## Verification Status
- `review-only`
