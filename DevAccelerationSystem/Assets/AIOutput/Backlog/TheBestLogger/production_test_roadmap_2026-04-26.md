# TheBestLogger Production Test Roadmap

Date: `2026-04-26`
Scope: `TheBestLogger`
Workspace: `DevAccelerationSystem/DevAccelerationSystem/`
Goal: production-hardening test design for a large-scale Unity title with strict requirements:
- `0 known crash regressions`
- `0 ANR / no main-thread stalls attributable to logger`
- `minimum allocations on hot paths`
- `minimum impact on Unity frame time`
- `no silent loss of critical logs`

## Current Status

Current automated evidence is useful, but not yet sufficient for a `1M DAU` production bar.

Current green evidence:
- `EditMode`: core formatter, tags registry, utility supplier, target filtering, decorator logic, generic formatting overloads, Apple exception message helper, OpenSearch config merge, dispose ownership
- `PlayMode`: basic runtime attribute flow, queued main-thread dispatch, batch priority ordering

Current verified strengths:
- public generic formatting overloads now have direct regression tests
- queued batch snapshots now have regression tests
- batch priority ordering now has play mode evidence
- dispose ownership has direct regression coverage
- remote-config merge for `OpenSearchLogTargetConfiguration` has direct coverage

Current high-risk gaps:
- `LogManager` lifecycle coverage is still shallow
- log-source capture paths are largely untested
- platform targets are not validated end-to-end on real runtime surfaces
- delivery targets are not stress-tested under failure conditions
- allocation and frame-time budgets are not enforced by tests
- no soak or endurance evidence exists yet
- no tracked consumer-project release gate exists yet

## Production Risk Model

For this package, the main production risks are:

1. Correctness risk
- wrong message content
- wrong category / tags / attributes
- dropped critical logs
- missing exception or stack-trace context

2. Stability risk
- crashes while logging
- recursive failure loops
- dispose / shutdown races
- target exceptions taking down the pipeline

3. Concurrency risk
- corrupted batches
- out-of-order priority handling
- background-thread logging against non-thread-safe targets
- config updates while logs are in flight

4. Performance risk
- GC allocations on hot paths
- frame spikes on flush or dispatch
- main-thread stalls during batching or delivery
- queue growth causing memory pressure

5. Integration risk
- consumer project wiring differences
- platform-specific behavior divergence
- remote-config merge mistakes
- network/file target behavior under real failures

## Required Test Layers

The package should be validated across six layers. Missing any one of these leaves a production blind spot.

### Layer 1: Deterministic Unit Coverage

Purpose:
- prove local correctness of pure logic

Current state:
- partially good

Must cover:
- `CoreLogger`
- `LogMessageFormatter`
- `LogAttributes`
- `UtilitySupplier`
- `ConcurrentTagsRegistry`
- `StackTraceFormatter`
- `LogTarget` base filtering and debug-mode overrides
- all configuration merge behavior

Still missing or under-covered:
- `LogManagerConfiguration` decision logic
- `LogTargetConfiguration.Merge`
- `LogAttributesExtensions`
- `LoggerExtensions`
- `TaskExtensions`
- `UnityLogExtension`
- `SubCategorizedLoggerDecorator`
- `FallbackLogger`
- `SafeThirdPartyLogTarget` safety contract

### Layer 2: Runtime Behavior Coverage

Purpose:
- prove behavior under Unity lifecycle and frame progression

Current state:
- minimal

Must cover:
- logger use across frames
- background-thread dispatch to main thread
- scheduled batch flush across frames
- runtime tags/props/stacktrace behavior
- scene reload and logger reuse
- app pause / resume relevant paths

Still missing:
- `LogManager.Initialize -> CreateLogger -> Use -> Dispose`
- multiple logger categories in one runtime session
- repeated initialization attempts
- shutdown while work is queued
- runtime config update during active logging

### Layer 3: Concurrency and Stress Coverage

Purpose:
- prove the package remains correct under sustained multithreaded pressure

Current state:
- almost absent

Must cover:
- `1 / 4 / 16 / 64` producer thread scenarios
- mixed importance logging under pressure
- concurrent category creation
- concurrent config updates while logging
- concurrent dispose while logging
- sustained queue drain under dispatch + batch decorations

Must measure:
- no deadlocks
- no corrupted payloads
- no critical-log loss
- bounded queue growth

### Layer 4: Target Integration Coverage

Purpose:
- prove targets behave correctly under real delivery conditions

Current state:
- weak

Must cover:
- `UnityEditorConsoleLogTarget`
- `AndroidSystemLogTarget`
- `AppleSystemLogTarget`
- `OpenSearchLogTarget`
- `BackgroundFileAsyncWriter`

Must include:
- success path
- invalid config
- target-side exception
- network or IO failure
- repeated retries or repeated failures
- partial batch failure behavior

### Layer 5: Performance / Allocation Coverage

Purpose:
- enforce hot-path cost ceilings

Current state:
- absent

Must cover:
- simple `LogInfo` hot path
- formatted logging hot path
- exception logging path
- batch flush path
- dispatch-to-main-thread path
- stack trace extraction path
- OpenSearch JSON serialization path

Must include thresholds for:
- GC allocations per log
- allocations per flush
- max frame-time contribution on main thread
- queue memory growth under burst load

### Layer 6: Endurance / Consumer Validation

Purpose:
- prove the package survives real project conditions over time

Current state:
- absent

Must cover:
- tracked validation in `DevAccelerationSystem.DemoProject/`
- long-running soak sessions
- domain reload and editor/runtime reload scenarios
- scene changes and repeated bootstrap
- quit while target flush is pending
- remote config updates in realistic consumer wiring

## P0 Must-Have Before Production Rollout

These are blockers for calling the package production-ready on a large title.

### P0.1 LogManager Lifecycle Suite

Files under test:
- `Assets/TheBestLogger/Runtime/Core/LogManager.Public.cs`
- `Assets/TheBestLogger/Runtime/Core/LogManager.cs`

Add tests for:
- initialize with valid single target
- initialize with multiple targets
- create logger before initialize returns fallback safely
- create logger with empty category returns fallback safely
- initialize twice does not corrupt state
- dispose twice is safe
- create logger after dispose is safe
- update target configs after init
- debug-mode toggling across multiple targets
- log sources enabled/disabled according to config

Suggested tests:
- `LogManagerLifecycleTests`
- `LogManagerConfigurationApplicationTests`
- `LogManagerDebugModeTests`

Priority reason:
- `LogManager` is the package bootstrap and shutdown authority
- lifecycle bugs can cause crashes, leaks, deadlocks, or silent logging failure

### P0.2 Multithreaded Stress Suite

Files under test:
- `CoreLogger.cs`
- `LogTargetBatchLogsDecoration.cs`
- `LogTargetDispatchingLogsToMainThreadDecoration.cs`
- `UtilitySupplier.cs`

Add tests for:
- `64` threads writing logs into one logger
- mixed `Critical`, `Important`, `NiceToHave`
- dispatch-enabled non-thread-safe target
- thread-safe target without dispatch
- concurrent category logging
- concurrent tags registry updates while logging
- dispose while burst logging is active

Expected assertions:
- no exceptions
- no deadlocks
- no duplicated critical logs
- no dropped critical logs
- payload integrity preserved

Suggested tests:
- `ConcurrentLoggingStressTests`
- `ConcurrentDisposeAndLoggingTests`
- `ConcurrentConfigUpdateTests`

Priority reason:
- biggest real-world risk for a logger in live mobile production

### P0.3 Target Failure Isolation Suite

Files under test:
- `SafeThirdPartyLogTarget.cs`
- all target decorators
- `LogManager.cs`

Add tests for:
- target throws during `Log`
- target throws during `LogBatch`
- one target throws while another healthy target still receives logs
- repeated target failure does not crash app or poison pipeline

Suggested tests:
- `FaultIsolationTests`
- `SafeThirdPartyLogTargetTests`

Priority reason:
- logging must never take down the game

### P0.4 Allocation Budget Suite

Files under test:
- `CoreLogger.cs`
- `LogMessageFormatter.cs`
- `StringOperations.cs`
- `StringBuilderPool/*`
- decorator classes

Add tests with profiler/benchmark harness for:
- `LogInfo` without stack trace
- `LogFormat<T1>`
- `LogFormat<T1, T2>`
- `LogException`
- batch flush
- dispatch flush

Required evidence:
- allocation baselines per scenario
- regression thresholds committed into the suite

Suggested tests:
- `LoggerPerformanceTests`
- `BatchFlushAllocationTests`
- `StackTraceAllocationTests`

Priority reason:
- if allocation is not gated, regressions will arrive quietly and show up as frame spikes in production

### P0.5 OpenSearch Delivery Contract Suite

Files under test:
- `Assets/TheBestLogger/Runtime/Examples/LogTargets/OpenSearch/OpenSearchLogTarget.cs`
- `OpenSearchLogTargetConfiguration.cs`

Add tests for:
- single log payload shape
- batch payload shape
- timestamp/category/tags/props serialization
- invalid host
- timeout
- server `4xx`
- server `5xx`
- API key update via remote config merge

Current note after initial implementation:
- payload shape, batch shape, invalid host, `4xx`, `5xx`, and API key rotation now have direct automated coverage
- timeout policy is still open at product/runtime level because the production target does not yet expose a configurable request timeout
- do not accept a hardcoded low timeout just to satisfy tests; timeout behavior should be controlled via config and remote config

Suggested tests:
- `OpenSearchPayloadTests`
- `OpenSearchFailureHandlingTests`
- `OpenSearchRemoteConfigRuntimeTests`

Priority reason:
- user stated this is real production surface with remote config

### P0.6 Consumer Validation Gate

Project:
- `DevAccelerationSystem.DemoProject/`

Add tracked validation scenarios:
- bootstrap logger in real demo scene
- main-thread logs
- background-thread logs
- exception log
- config update simulation
- scene transition while logs are queued

Artifacts required per release:
- `ValidationReports/<date>_thebestlogger_runtime_validation.md`

Current note after initial implementation:
- `DevAccelerationSystem.DemoProject` now has a dedicated playmode consumer gate in `TheBestLogger.ConsumerValidation.PlayModeTests`
- tracked evidence was captured in `Assets/AIOutput/ValidationReports/integration_validation_2026-04-26.md`
- the current gate proves bootstrap, main-thread logs, background-thread logs, exception logging, config update simulation, and queued delivery across scene transition

Priority reason:
- package-level green tests are not enough without consumer proof

## P1 Required Before Wide Rollout

These are not immediate blockers for a narrow rollout, but they are required before calling the logger hardened across the full product surface.

### P1.1 Platform Runtime Suites

Add runtime validation for:
- `AndroidSystemLogTarget`
- `AppleSystemLogTarget`
- `UnityEditorConsoleLogTarget`

Must prove:
- valid message mapping by level
- exception path behavior
- no platform-only crash
- acceptable runtime overhead

Suggested suites:
- `AndroidSystemLogTargetRuntimeTests`
- `AppleSystemLogTargetRuntimeTests`
- `UnityEditorConsoleIntegrationTests`

Current note after initial implementation:
- `UnityEditorConsoleIntegrationTests` is now the first landed slice of `P1.1`
- it verifies level mapping, context propagation, exception forwarding, batch formatting, and constructor handler caching
- `AppleSystemLogTargetTests` now also verify log-level mapping, exception-message construction, and batch forwarding/null-safety
- `AndroidSystemLogTargetTests` now verify log-level mapping, message payload formatting, exception payload formatting, and batch forwarding/null-safety
- `PlatformTargetRuntimePlayModeTests` now verify the real `Log()` and `LogBatch()` execution paths for `UnityEditorConsoleLogTarget`, `AppleSystemLogTarget`, and `AndroidSystemLogTarget` through player-runnable playmode tests
- `AppleSystemLogTarget` and `AndroidSystemLogTarget` now expose internal native-bridge seams so runtime paths can be proven in tests without changing the public API
- this is stronger runtime evidence than helper-only editor tests, but it is still not the same as physical device native-log observability proof

### P1.2 Log Source Capture Suites

Files under test:
- `Runtime/Core/LogSources/*`

Add tests for:
- `UnityDebugLogSource`
- `UnityApplicationLogSource`
- `UnityApplicationLogSourceThreaded`
- `UnobservedTaskExceptionLogSource`
- `UnobservedUniTaskExceptionLogSource`
- `CurrentDomainUnhandledExceptionLogSource`
- `SystemDiagnosticsConsoleLogSource`
- `SystemDiagnosticsDebugLogSource`

Must prove:
- source subscription and unsubscription
- event capture correctness
- no recursion loops
- no duplicate events

Suggested suites:
- `UnityLogSourceIntegrationTests`
- `AsyncExceptionLogSourceTests`
- `SystemDiagnosticsLogSourceTests`

Current note after initial implementation:
- `UnityLogSourceIntegrationTests` now verify `UnityDebugLogSource`, `UnityApplicationLogSource`, and `UnityApplicationLogSourceThreaded`
- `AsyncExceptionLogSourceTests` now verify `UnobservedTaskExceptionLogSource`, `UnobservedUniTaskExceptionLogSource`, and `CurrentDomainUnhandledExceptionLogSource`
- `SystemDiagnosticsLogSourceTests` now verify `SystemDiagnosticsConsoleLogSource`, `SystemDiagnosticsConsoleRedirector`, and `SystemDiagnosticsDebugLogSource`
- current targeted editor evidence for these `P1.2` suites is green
- `CurrentDomainUnhandledExceptionLogSource` was hardened so non-`Exception` payloads no longer cause an invalid cast during crash-path logging

### P1.3 BackgroundFileAsyncWriter Stress Suite

Files under test:
- `BackgroundFileAsyncWriter.cs`

Must cover:
- high-volume writes
- flush on shutdown
- invalid path
- disk write failure
- partial write
- repeated reopen

Suggested suites:
- `BackgroundFileAsyncWriterTests`
- `BackgroundFileAsyncWriterStressTests`

Current note after initial implementation:
- `BackgroundFileAsyncWriter` was simplified to synchronous file writes on its dedicated background task and now wakes the wait handle during disposal
- a first `BackgroundFileAsyncWriterTests` suite was added for flush, high-volume writes, reopen, double-dispose, and invalid-path behavior
- the original Unity batch-runner stall was caused by `DisposeAsync()` awaiting `_logTask` while capturing the Unity synchronization context, then tests blocking that same thread via `GetAwaiter().GetResult()`
- `DisposeAsync()` now awaits `_logTask.ConfigureAwait(false)`, which removes that deadlock path
- targeted Unity editor evidence for `BackgroundFileAsyncWriterTests` is now green

### P1.4 StabilityHub Runtime Suite

Files under test:
- `Runtime/StabilityHub/*`

Must cover:
- configuration wiring
- crash-reporter module activation
- previous-session retrieval path
- invalid config safety

Suggested suites:
- `StabilityHubRuntimeTests`
- `CrashReporterModuleTests`

Current note after initial implementation:
- `StabilityHubRuntimeTests` now verify service initialization, module activation wiring, previous-session retrieval forwarding, disposal, and invalid-config safety
- `StabilityHubService.Initialize` no longer throws when monitoring config is missing; it falls back to disabled state and logs that path
- internal test seams were added for monitoring-config loading and crash-reporter module creation so the runtime wiring can be proven without changing the public API
- this is editor-side wiring evidence; physical iOS crash-report retrieval behavior is still not proven on device

## P2 Hardening / Nightly / Pre-Launch

These suites should run nightly or in pre-release hardening.

### P2.1 Long Soak Sessions

Durations:
- `30 min`
- `2 hr`
- `8 hr`

Scenarios:
- sustained mixed logging
- periodic remote config updates
- scene changes
- logger recreation
- periodic exception bursts

Expected results:
- no crashes
- no deadlocks
- no monotonic memory growth beyond expected bounded buffers
- no throughput collapse

### P2.2 Device Farm Matrix

Run on:
- low-end Android
- mid-tier Android
- flagship Android
- iPhone current gen
- older supported iPhone
- macOS editor/runtime target if applicable

Focus:
- frame spikes
- platform target behavior
- lifecycle transitions
- background/foreground transitions

### P2.3 Chaos / Fault Injection

Inject:
- network failures
- malformed config
- empty config
- partial target failures
- exception storms
- rapid enable/disable of debug mode

Purpose:
- prove graceful degradation

## Missing Cases By Runtime Surface

### Core Runtime

Still missing:
- empty or null message behavior matrix
- extremely long message matrix
- malformed format strings under load
- stacktrace enabled/disabled matrix for every level
- category override matrix with debug-mode overrides

### Concurrency

Still missing:
- concurrent logger creation for same and different categories
- concurrent `TagsRegistry.GetAllTags` during mutations
- simultaneous `LogBatch` and `ApplyConfiguration`
- simultaneous `Dispose` and source callbacks

### Main Thread Safety

Still missing:
- dispatch queue depth thresholds
- main-thread flush budget per frame
- repeated flush under low frame rate
- starvation behavior when producer rate exceeds consumer rate

### Delivery Targets

Still missing:
- payload size ceiling behavior
- huge batch handling
- backpressure behavior
- malformed props/tags serialization
- target-specific latency spikes

### Crash / ANR Prevention

Still missing:
- logging during app quit
- logging during scene unload
- logging from failing async continuations under pressure
- logging while Unity synchronization context is unavailable or stale

## Recommended Test Class Backlog

### P0 Backlog

- `LogManagerLifecycleTests`
- `LogManagerConfigurationApplicationTests`
- `LogManagerDebugModeTests`
- `ConcurrentLoggingStressTests`
- `ConcurrentDisposeAndLoggingTests`
- `ConcurrentConfigUpdateTests`
- `FaultIsolationTests`
- `SafeThirdPartyLogTargetTests`
- `LoggerPerformanceTests`
- `BatchFlushAllocationTests`
- `StackTraceAllocationTests`
- `OpenSearchPayloadTests`
- `OpenSearchFailureHandlingTests`
- `OpenSearchRemoteConfigRuntimeTests`
- `DemoProjectLoggerValidationTests`

### P1 Backlog

- `AndroidSystemLogTargetRuntimeTests`
- `AppleSystemLogTargetRuntimeTests`
- `UnityEditorConsoleIntegrationTests`
- `UnityLogSourceIntegrationTests`
- `AsyncExceptionLogSourceTests`
- `SystemDiagnosticsLogSourceTests`
- `BackgroundFileAsyncWriterTests`
- `BackgroundFileAsyncWriterStressTests`
- `StabilityHubRuntimeTests`
- `CrashReporterModuleTests`

### P2 Backlog

- `LoggerSoakTests`
- `LoggerDeviceFarmMatrixPlan`
- `LoggerChaosScenarioValidation`

## Release Gates

The logger should not be declared production-ready for high-scale rollout unless all `P0` gates pass.

### Mandatory P0 Gates

- `EditMode` suite green
- `PlayMode` suite green
- `P0` stress suite green
- `P0` allocation thresholds green
- `P0` OpenSearch delivery suite green
- consumer validation green in `DevAccelerationSystem.DemoProject`

### Mandatory Wide-Rollout Gates

- all `P0` gates green
- `P1` platform runtime suites green
- `P1` log source suites green
- `P1` file/IO suites green
- at least one soak run green

### Mandatory Final Hardening Gates

- all `P0` and `P1` green
- nightly `P2` soak stable
- device matrix stable
- no unresolved crash/ANR/perf regressions

## Metrics To Track

Per release, capture:
- logs/sec throughput
- max queue depth
- dropped critical logs
- dropped non-critical logs
- alloc/log on hot paths
- alloc/flush
- worst frame-time delta caused by flush/dispatch
- exception logging overhead
- memory growth over soak session

## Recommended Execution Cadence

Per PR:
- deterministic `EditMode`
- targeted `PlayMode`
- changed-surface perf tests

Per merge to main:
- full `EditMode`
- full `PlayMode`
- core concurrency suite

Nightly:
- stress
- allocation/perf
- integration targets
- consumer validation

Pre-release:
- soak
- device matrix
- remote-config scenarios
- shutdown / startup / scene transition scenarios

## Exit Criteria

The logger can be described as production-hardened only when:
- all `P0` suites exist and are green
- `P1` platform and source coverage exists and is green
- performance thresholds are codified and enforced
- consumer-project validation is part of release evidence
- soak evidence exists
- there are no unresolved crash, ANR, queue-growth, or allocation-regression findings

## Direct Conclusion

Current coverage is not enough to claim `0 production problems`.

Current coverage is enough to say:
- several known correctness defects are now guarded
- core deterministic logic is better covered
- basic runtime dispatch and batching behavior has first-pass evidence

Current coverage is not enough to say:
- `0 crashes`
- `0 ANR`
- `minimum allocations`
- `no main-thread freeze risk`
- `safe at 1M DAU`

That claim requires the `P0` suite to be completed first, then the `P1` platform and source suites, and finally soak/perf evidence from `P2`.
