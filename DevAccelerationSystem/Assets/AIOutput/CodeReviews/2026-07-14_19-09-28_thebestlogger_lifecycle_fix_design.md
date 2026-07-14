# XUUnity TheBestLogger Lifecycle Fix Design

## Metadata

- Date: 2026-07-14 19:09:28 -03
- Repository: DevAccelerationSystem
- Resolved Unity project: DevAccelerationSystem/DevAccelerationSystem
- Branch: master
- Design baseline: 056a0d1086fbe8dfb75990e7d3718973c000ce93
- Source review: Assets/AIOutput/CodeReviews/2026-07-14_18-45-34_thebestlogger_lifecycle_cancellation_reinitialize_review.md
- Design scope:
  - Assets/TheBestLogger/Runtime/Core/LogManager.cs
  - Assets/TheBestLogger/Runtime/Core/LogManager.Public.cs
  - Assets/TheBestLogger/Tests/Editor/LogManagerLifecycleTests.cs
- Design type: architecture-only; no runtime or test implementation was changed
- Risk class: high
- Current release posture: not ready for production until this design is implemented and validated

## Outcome

Use one small per-initialization LifecycleGeneration object as the disposal authority. Its reference identity is the generation identity. Keep Unity-facing resource construction and physical cleanup serialized on the Unity main thread. A lifetime-token callback may only atomically request disposal and enqueue one main-thread drain; it must never dispose the manager, its registration, or consumer resources directly.

This is the minimum design that closes every review finding. It deliberately does not add:

- a numeric generation counter;
- a global lifecycle lock;
- a linked CancellationTokenSource;
- a hidden GameObject or custom PlayerLoop;
- SynchronizationContext.Send;
- Task.Wait, Result, or any synchronous update-task join;
- a full migration of every LogManager field into a new runtime session;
- public or conditional test hooks.

The design keeps public signatures unchanged and adds no allocation or synchronization to CoreLogger.Log or the target logging hot path.

## Selected XUUnity Stack

- Routers:
  - repository Agents.md
  - target workspace Agents.md
  - target Unity project Agents.md
- Session entrypoint: AIRoot/Modules/XUUnity/tasks/start_session.md
- Primary role: role/architect.md
- Supporting role: role/senior_unity_developer.md
- Primary task: tasks/architecture_plan.md
- Validation planning task: tasks/validation_plan.md
- Architecture guidance:
  - skills/architecture/routing.md
  - skills/architecture/state_management.md
  - skills/architecture/event_driven_design.md
  - skills/architecture/dependency_boundaries.md
- Async/lifecycle guidance:
  - skills/async/base_async_rules.md
  - skills/async/cancellation.md
  - skills/async/main_thread.md
  - skills/async/exception_handling.md
- Test guidance:
  - skills/tests/testing_doctrine.md
  - skills/tests/unit_tests.md
  - skills/tests/integration_tests.md
  - skills/tests/playmode_tests.md
  - skills/tests/runtime_service_testability.md
  - skills/tests/unity_test_runner_workflow.md
- Style and core reasoning:
  - skills/code_style/csharp.md
  - skills/code_style/unity.md
  - all routed skills/core guidance
- Project truth: all Markdown memory under Assets/AIOutput/ProjectMemory/
- Host-local overlay: none present for this target workspace

## Execution Contract

- resolved_project: DevAccelerationSystem/DevAccelerationSystem
- primary_task: tasks/architecture_plan.md
- overlay_tasks: lifecycle state design, async ownership design, regression-test design, and validation planning
- matched_skills: architecture routing/state/dependency/event guidance; C# and Unity style; cancellation, main-thread, exception handling; testing doctrine, unit, integration, PlayMode, runtime-service testability, and Unity Test Runner workflow
- matched_policy_packs: none
- matched_private_packs: none
- private_pack_report_references: none
- trigger_reasons: static lifecycle ownership, synchronous cancellation callbacks, off-main cancellation, registration publication race, reinitialize overlap, scheduled-task ownership, cleanup exception containment, and domain-reload-disabled state
- risk_class: high
- root_cause_chain_checked: Initialize -> lifetime registration -> partial resource acquisition -> active publication -> token callback -> logical disposal request -> main-thread physical cleanup -> registration release -> scheduled-loop termination -> replacement Initialize
- patch_shape: state_orchestration_fix
- pre_patch_blockers: implementation was not requested; architecture acceptance is required before runtime changes
- primary_validation_lane: interactive_mcp
- secondary_validation_lane: scenario
- lane_selection_reason: correctness depends on the real UnitySynchronizationContext, Play Mode lifecycle, domain reload behavior, worker cancellation, and Unity Test Runner orchestration
- expected_evidence_class: deterministic Editor and PlayMode lifecycle regressions, complete assembly results, compiler health, ordered scenario evidence, and tracked-consumer compile evidence
- validation_contract: Unity 2022.3.62f3; TheBestLogger.EditorTests; TheBestLogger.PlayModeTests; XUUnity MCP wrapper only; no direct Unity CLI
- why_not_local_fix: a local registration guard cannot establish generation authority, main-thread cleanup, update-loop ownership, complete rollback, and non-overlapping reinitialize as one invariant
- validation_gaps: implementation does not exist; no deterministic worker-cancel/main-drain evidence, no callback-before-registration-publication test, no update-loop completion proof, no domain-reload-disabled scenario, no player/IL2CPP result, and no new tracked-consumer result
- required_validation: focused lifecycle tests, full Editor and PlayMode assemblies, isolated domain-reload-disabled scenario, final compiler/console health, tracked consumer validation, and device/IL2CPP evidence before broad package release
- required_self_review: reference-identity authority, registration handoff, callback boundedness, no cleanup off-main, no self-registration disposal, no lock/wait cycle, no generation overlap, update-task termination, detach-first cleanup, per-resource exception containment, public API compatibility, and no hot-path cost

Missing project memory: none.

## Design Question

How can LogManager accept a CancellationToken that may cancel synchronously or on any thread, dispose exactly the manager generation that owns the registration, allow safe reinitialize, preserve Unity main-thread cleanup, and stop all owned background work without introducing a broad rewrite or hot-path synchronization?

## Current Failure Shape

The current implementation uses process-wide mutable fields as both resources and lifecycle state:

- _isInitialized and _wasDisposed are unsynchronized global booleans;
- _disposingTokenRegistration is global rather than generation-owned;
- Register invokes the complete static Dispose method;
- Dispose clears global resources before an old callback can be proven stale;
- _isRunningUpdates controls every scheduled loop across every generation;
- one disposal exception prevents the rest of cleanup;
- a pre-cancelled token is checked only after side effects;
- sequential tests do not exercise callback publication, worker dispatch, or replacement ownership.

The central defect is not the absence of another boolean. The defect is that a cancellation callback holds global authority instead of authority scoped to the initialization that created it.

## Candidate Before First-Principles Simplification

A conventional solution would introduce a full RuntimeSession, a numeric generation id, a global lifecycle gate, a dispatcher abstraction, a linked CTS, and a cleanup snapshot. That can be made correct, but most of those mechanisms solve problems created by the other mechanisms.

The first-principles pass below removes everything that is not required by an invariant.

## First-Principles Deconstruction

### Phase 1: Autopsy of Assumptions

1. Assumption: lifecycle must be represented by several static booleans.
   - Origin: inherited implementation convenience.
   - Problem: the booleans do not identify which initialization owns a callback or resource.

2. Assumption: storing and disposing CancellationTokenRegistration is enough.
   - Origin: a local leak fix.
   - Problem: it fixes the completed sequential path but not an already-running stale callback.

3. Assumption: a cancellation callback may directly call global Dispose.
   - Origin: common .NET convenience examples.
   - Problem: cancellation callbacks can be synchronous and can run on the Cancel caller's thread.

4. Assumption: Register with useSynchronizationContext true is the main-thread fix.
   - Origin: framework API naming.
   - Problem: it still invokes synchronously for an already-cancelled token and can use a blocking context handoff. UnitySynchronizationContext.Send waits for the main thread.

5. Assumption: a lifecycle lock is required because cancellation is concurrent.
   - Origin: fear-driven concurrency design.
   - Problem: only a tiny request flag is truly concurrent. Registration disposal and arbitrary target cleanup must not run under a lock.

6. Assumption: a numeric generation id is required.
   - Origin: traditional generation-counter patterns.
   - Problem: an object reference already provides unique, non-reusable authority for the process lifetime.

7. Assumption: all runtime fields must move into a full session object.
   - Origin: architectural purity.
   - Problem: Unity global state still requires old cleanup to complete before new initialization, so full overlap is neither useful nor safe.

8. Assumption: the external lifetime token should also control scheduled updates.
   - Origin: token reuse.
   - Problem: explicit Dispose cannot stop a loop whose external token remains alive, and a shared run flag lets a replacement revive it.

9. Assumption: domain reload always clears static state.
   - Origin: default Editor behavior.
   - Problem: Enter Play Mode can disable domain reload.

10. Assumption: green sequential tests prove lifecycle safety.
    - Origin: test availability.
    - Problem: they do not prove thread affinity, callback publication, stale authority, task termination, or cleanup completion after an exception.

11. Assumption: physical cleanup must execute inside the token callback to be prompt.
    - Origin: current synchronous behavior.
    - Problem: it creates self-unregister, arbitrary-thread teardown, and teardown reentrancy. Logical revocation can be immediate while physical cleanup is delegated to the main thread.

### Phase 2: Irreducible Truths

1. At most one generation may own LogManager's static runtime resources.
2. A replacement cannot acquire Unity/global resources until prior physical cleanup reaches its finally block.
3. A token callback may run synchronously during Register and may run on any Cancel caller thread.
4. Every cancellation request must carry the identity of the generation that registered it.
5. A stale identity must have no authority over the current generation.
6. Unity-facing source, target, diagnostics, and global logger cleanup must run on the Unity main thread.
7. The cancellation callback must not block, wait, call consumer code, or throw.
8. Public availability must be revoked before physical cleanup begins.
9. Registration ownership must be established even when the callback completes before Register returns.
10. Every acquired disposable must receive an owner immediately.
11. Cleanup must attempt every owned resource even when an earlier resource throws.
12. Each scheduled loop must have one owner, one stop signal, and one observable completion task.
13. Initialization is not successful until all resources are ready and the generation is still live.
14. Invalid input and a pre-cancelled lifetime must fail before ownership transfer or Unity side effects.
15. Public API signatures and the per-log hot path must not change.

### Phase 3: Rebuild From Zero

#### Approach A: Main-thread lifecycle actor

All lifecycle commands enter a permanent main-thread mailbox. Worker cancellation enqueues DisposeGeneration. Initialize and explicit Dispose execute inline only when called by the actor's main thread.

This satisfies the truths, but it requires a permanent pump, queue ownership, shutdown draining, and a compatibility policy for synchronous Initialize. It is correct but larger than the problem.

#### Approach B: Reference-scoped lifecycle generation over serialized static resources

Create one LifecycleGeneration for each accepted Initialize. Its object reference is the authority. The Unity main thread remains the only writer of static resources. Concurrent callbacks can only set that generation's request flag and Post one drain. Main-thread lifecycle entrypoints compare reference identity before acting.

This satisfies every truth with one owner object, atomic request fields, the existing UnitySynchronizationContext, and a per-generation update-loop owner. It preserves the current static facade and avoids hot-path changes.

This is the selected approach.

#### Approach C: Full immutable RuntimeSession facade

Move every logger/configuration/source/target field into an instance and atomically swap the active session. Cleanup remains main-thread serialized because Unity global state cannot overlap.

This is also correct and gives stronger whole-manager encapsulation, but it mechanically changes roughly every LogManager helper and public method. It adds migration risk without closing any review finding that Approach B leaves open.

### Phase 4: Assumptions Versus Truths

| Starting assumption | Replacing truth | Design consequence |
|---|---|---|
| Global Dispose is valid callback authority | Authority belongs to one initialization | Callback captures LifecycleGeneration, never LogManager.Dispose |
| More flags will fix lifecycle | Identity and ordering are required | One current-generation reference plus a small phase |
| A generation counter is needed | Reference identity is already unique | No counter in correctness logic |
| A global lock is needed | Only request publication is concurrent | Interlocked/Volatile fields; no lifecycle lock |
| Context capture solves dispatch | Unity teardown needs explicit non-blocking handoff | Register without context capture, then explicit Post |
| Token callback must perform cleanup | Logical revocation and physical cleanup are separate | Callback revokes immediately; main thread drains |
| External token owns every async task | Manager owns its internal work | Private ScheduledUpdateLoop CTS/task |
| Clearing fields equals cleanup | Ownership survives exceptions | Detach to an ownership snapshot and dispose each boundary |
| Full session rewrite is necessary | Generations cannot overlap Unity globals anyway | Keep existing resource fields, serialize generations |
| Domain reload clears everything | Static state can survive Play Mode | SubsystemRegistration cleanup/reset |
| Internal snapshots prove behavior | Users observe delivery, fallback, and cleanup | Public-behavior Editor/PlayMode regressions |

### Phase 5: The Aristotelian Move

Replace global disposal authority with a scoped capability:

> The cancellation registration captures the LifecycleGeneration that created it, and every cleanup path must present that exact object reference before it may mutate LogManager state.

This single move makes a stale callback harmless and allows the lock, numeric generation, linked CTS, and full-session rewrite to disappear.

## Final Simplified Architecture

### 1. LifecycleGeneration

Introduce one internal production type, nested in LogManager or in the same runtime assembly:

~~~csharp
internal enum LifecyclePhase
{
    Initializing,
    Active,
    Disposing,
    Disposed
}

internal sealed class LifecycleGeneration
{
    internal readonly int MainThreadId;
    internal readonly SynchronizationContext MainContext;

    // Written with Interlocked or Volatile.
    internal int DisposeRequested;
    internal int CleanupPostQueued;
    internal int Phase;

    // Unity-main-thread-owned fields.
    internal CancellationTokenRegistration LifetimeRegistration;
    internal bool HasLifetimeRegistration;
    internal ScheduledUpdateLoop UpdateLoop;

    // Incremental ownership ledgers for rollback and final cleanup.
    internal readonly List<TargetOwnership> Targets;
    internal readonly List<ILogSource> Sources;
}

private static LifecycleGeneration _currentGeneration;
~~~

The actual implementation should use named methods rather than exposing mutable fields broadly. The shape above documents ownership.

The active predicate is:

~~~text
ReferenceEquals(Volatile.Read(ref _currentGeneration), generation)
and generation.Phase == Active
and generation.DisposeRequested == 0
~~~

This predicate replaces lifecycle use of _isInitialized. Null current generation means uninitialized.

### 2. No Global Lifecycle Lock

The no-lock design is safe because:

- Initialize and physical cleanup are enforced on the captured Unity main thread;
- only that thread writes _currentGeneration, phase, registrations, and static resources;
- token callbacks and off-main Dispose only write per-generation integer request fields through Interlocked;
- posted cleanup validates reference identity again on the main thread;
- registration disposal and arbitrary consumer code therefore cannot be inside a lifecycle lock.

If implementation work discovers a second legitimate writer of static lifecycle resources, that is a violated invariant to remove, not a reason to wrap consumer cleanup in a lock.

### 3. State and Availability

~~~mermaid
stateDiagram-v2
    [*] --> Initializing: accepted Initialize
    Initializing --> Active: complete build and live commit
    Initializing --> Disposing: cancellation or build failure
    Active --> Disposing: explicit or token disposal request
    Disposing --> [*]: cleanup finally clears current
    Active --> Active: duplicate Initialize rejected
~~~

DisposeRequested is an atomic side flag rather than a fifth phase. Setting it immediately makes Active evaluate false without allowing a worker to mutate the main-thread-owned phase.

Rules:

- duplicate Initialize while Active is a warning/no-op and does not register or own the second token;
- Initialize while Initializing or Disposing is a reentrant call and is rejected;
- main-thread Initialize that finds a requested Active generation synchronously drains it, then starts the replacement;
- _currentGeneration remains the old object through all cleanup and is cleared only in finally;
- repeated Dispose and repeated cancellation are idempotent;
- a stale posted drain is a no-op after reference comparison.

### 4. Initialization Transaction

Both public overloads must share one preflight so the Resources-path overload does not load an asset before checking the token.

Order:

1. Verify the documented Unity-main-thread call contract and capture a non-null SynchronizationContext and thread id.
2. Inspect the current generation.
   - Active and not requested: warn and return.
   - Active and requested: drain it synchronously on main, then continue.
   - Initializing or Disposing: reject reentrant initialization.
3. Validate cheap arguments that require no ownership:
   - token not already cancelled;
   - targets non-null and non-empty;
   - explicit configuration non-null, or load the Resources configuration only after the token check.
4. Create and publish generation N in Initializing.
5. Register the lifetime token before resource acquisition:

~~~csharp
var registration = disposingToken.Register(
    LifetimeCancellationRequested,
    generation,
    useSynchronizationContext: false);
~~~

Use a cached method-group callback and state overload; do not capture a closure.

6. Attach the returned registration on the main thread.
7. Re-read DisposeRequested.
   - If the token cancelled before or during Register, rollback N after Register has returned.
   - The callback never touches the registration, so callback-before-return cannot tear or dispose an unpublished struct.
8. Transfer target ownership and build resources incrementally.
9. Check DisposeRequested after each acquisition boundary that can call external code:
   - configuration application;
   - each target decoration;
   - default logger construction;
   - each log-source construction/subscription;
   - debug-state application;
   - before active publication.
10. Create the update-loop owner but do not let it update before the live commit.
11. Volatile-write phase Active only after all required resources exist.
12. Start the update loop with generation-local dependencies.
13. Re-check DisposeRequested before the success diagnostic. A request that won before commit produces rollback and no false successful initialization.

Precondition failure versus rollback is explicit:

- invalid input or an already-cancelled token transfers no ownership; the caller retains targets;
- once generation N is published and target ownership is recorded, every later failure rolls back all acquired resources exactly once.

An internal CreateLoggerCore(generation, ...) should create the default logger during Initializing. Public CreateLogger must not require a temporarily published active manager.

### 5. Cancellation Callback

The callback contract is intentionally smaller than Dispose:

~~~text
LifetimeCancellationRequested(generation N)
  if N is not the current reference: return
  Interlocked set N.DisposeRequested
  if this is the first request:
      Interlocked claim N.CleanupPostQueued
      N.MainContext.Post(MainThreadCleanup, N)
      if Post throws:
          release N.CleanupPostQueued
          record dispatch failure without Unity logging
  contain every exception
  return
~~~

It must not:

- call Dispose or any target/source/logger method;
- dispose CancellationTokenRegistration;
- cancel or dispose the update CTS;
- use SynchronizationContext.Send;
- wait for the main thread;
- log through Unity;
- acquire a lifecycle lock;
- throw back through CancellationTokenSource.Cancel.

Consequences:

- logical disposal is immediate on any thread because the active predicate observes DisposeRequested;
- worker Cancel is bounded and non-blocking;
- physical cleanup is eventual on the Unity main thread;
- a main-thread cancellation is also posted, avoiding teardown reentrancy and registration self-disposal;
- a synchronous callback inside Register completes before initialization rollback touches the returned registration.

Releasing CleanupPostQueued after a failed Post allows a later request to retry. The generation remains logically inactive, and the next main-thread Initialize, Dispose, terminal lifecycle drain, or SubsystemRegistration hook must synchronously drain it. A failed Post must never fall back to off-main physical cleanup.

Public documentation should state this distinction:

- LogManager.Dispose called on the Unity main thread performs physical cleanup synchronously;
- lifetime-token cancellation and off-main Dispose revoke the manager immediately and complete physical cleanup on the next main-thread drain.

For terminal flush reliability, own one Application.quitting main-thread drain for the active manager. It calls the normal synchronous main-thread Dispose path, is subscribed/unsubscribed as a generation-owned resource, and is idempotent with the token request. This preserves Application.exitCancellationToken use without putting arbitrary teardown inside its callback.

The terminal drain must be validated in both normal and domain-reload-disabled Play Mode. It must not become a second lifecycle authority: it only requests cleanup of the current reference.

### 6. CancellationTokenRegistration Safety

The preferred design never calls CancellationTokenRegistration.Dispose from its own cancellation callback.

Normal cleanup can still race an executing callback:

1. main thread detaches the registration handle from generation N;
2. it calls registration.Dispose outside any lock;
3. Dispose may wait for an already-running callback;
4. that callback only performs Interlocked operations and non-blocking Post and never waits for the main thread;
5. therefore the wait is bounded by the tiny callback and has no lock cycle.

This avoids depending on the runtime's self-unregister special case. The .NET contract documents that registration Dispose normally waits for an executing callback, except when the callback unregisters itself. The design remains compatible with that contract but does not require the exception for correctness.

### 7. Main-Thread Physical Cleanup

MainThreadCleanup(generation N) performs:

1. Assert or verify the captured main-thread id.
2. Compare N with the volatile current reference; stale N returns.
3. If phase is Disposing or Disposed, return.
4. Set DisposeRequested and phase Disposing.
5. Keep _currentGeneration equal to N so reentrant Initialize cannot start B.
6. Detach the registration, update loop, sources, loggers, target-ownership ledger, and all static resource references into local ownership snapshots.
7. Reset public-facing static references to canonical empty/null values.
8. Dispose the detached registration outside any lock.
9. Request stop on N's update loop; never synchronously wait on its Task from the Unity main thread.
10. Dispose every source independently.
11. Dispose every logger independently.
12. Dispose each target ownership root independently and exactly once.
13. Restore/clear diagnostics and Unity-global state last.
14. Aggregate failures for one contained fallback diagnostic.
15. In finally:
    - force every static field to its canonical empty state;
    - set N phase Disposed;
    - clear _currentGeneration only if it still references N.

Cleanup ordering goals:

- stop new public use first;
- stop scheduled production before target teardown;
- unsubscribe sources before logger and target destruction;
- retain diagnostics long enough to report cleanup failures;
- never allow one consumer exception to skip later owners;
- never propagate a target/source exception through token Cancel.

CreateLogger can race a worker cancellation before physical cleanup. It should capture the current generation and its logger dictionary, add through that captured dictionary, then re-check the active predicate. If authority was revoked, it must TryRemove the exact newly created logger and dispose only when it won removal. Main cleanup should also drain the ConcurrentDictionary through TryRemove rather than enumerate-then-clear. This prevents a logger added at the disposal boundary from escaping cleanup without adding synchronization to CoreLogger.Log.

Other manager mutation entrypoints must capture one generation and re-check that same reference around external target calls. This design does not make arbitrary target implementations thread-safe; it ensures that an off-main token callback itself performs no target or Unity mutation.

### 8. Incremental Target and Source Ownership

The current decorated-list/original-list branch is not sufficient for partial construction. Use a small ownership ledger:

~~~csharp
internal sealed class TargetOwnership
{
    internal readonly LogTarget Original;
    internal ILogTarget OwnedRoot;
}
~~~

For each input target:

1. create an entry with OwnedRoot equal to Original before reading target configuration;
2. after constructing each wrapper, replace OwnedRoot with the outermost successfully constructed wrapper;
3. cleanup disposes only OwnedRoot.

This makes original, partially decorated, and fully decorated targets one exactly-once rule. A wrapper constructor that subscribes before returning must remain internally exception-safe because no caller can own an object that was never returned.

Build the source list directly in generation N and append each successfully constructed source immediately. Do not keep a source only in a temporary local until the whole list succeeds.

### 9. ScheduledUpdateLoop

Extract a small internal production owner used by LogManager:

~~~text
ScheduledUpdateLoop
  owns one private CancellationTokenSource
  owns one Task Completion
  captures immutable target list, period, initial time, utility supplier, and generation
  Start once
  RequestStop idempotently
  catches expected cancellation and all unexpected faults
  disposes its CTS after its async body finishes
~~~

RunUpdates must no longer read:

- _isRunningUpdates;
- the external lifetime token;
- a replacement generation's _utilitySupplier;
- a mutable static target-update list.

After every await and before touching a target, require:

~~~text
internal token is not cancelled
and generation.DisposeRequested is zero
and current generation reference equals generation
and generation phase is Active
~~~

Cleanup uses the XUUnity cancellation ownership pattern: atomically take the owned CTS when more than one stop path can reach it, then cancel with ObjectDisposedException containment. It does not join the task on the Unity main thread, because the continuation is scheduled to that same context. The async body owns final CTS disposal.

Do not clear a list that a suspended old task captured. Capture an immutable/read-only list and make the generation check stop the old continuation.

### 10. Domain Reload and Static Reset

Add a private RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration) hook.

On the Unity main thread it:

- force-drains a surviving current generation best-effort;
- invalidates queued callbacks through reference identity;
- resets every static field to the same canonical state used by cleanup;
- refreshes the main-thread/context baseline for the new play session.

With normal domain reload the AppDomain reset remains the primary reset. With domain reload disabled, SubsystemRegistration becomes the deterministic safety net.

Do not make an Editor-only reset hook the production owner. Existing test reset helpers must not mutate lifecycle authority.

## Finding-to-Fix Map

| Severity and finding | Evidence location | Final fix | Required proof |
|---|---|---|---|
| High: token A can dispose manager B | LogManager.Public.cs:44-51, 97, 158-164; LogManager.cs:794-884 | callback captures generation A; every drain compares object identity; B cannot start until A cleanup finally | stale token A after B; queued stale drain after B; in-progress cancel versus replacement |
| High: cancellation tears Unity state down off-main | LogManager.cs:803, 819-887 | callback only Interlocked plus Post; all physical cleanup verifies main thread | worker Cancel returns while main is blocked; target records main-thread Dispose |
| Medium: old scheduled loop is revived by B | LogManager.Public.cs:143-146; LogManager.cs:667-724, 833 | private ScheduledUpdateLoop per generation; internal CTS/task; immutable dependencies; identity checks | A completion terminates; A never ticks after B; B continues |
| Medium: one Dispose exception loses later cleanup | LogManager.cs:821-887 | detach-first ownership snapshot; per-resource try/catch; terminal finally | first target throws; later source/logger/target still disposes; retry succeeds |
| Medium: tests prove only sequential behavior | LogManagerLifecycleTests.cs:1457-1508 | focused Editor and PlayMode lifecycle fixtures plus internal production-owner tests | public delivery/fallback, thread id, exact counts, bounded gates, no reflection/hooks |
| Low: pre-cancel observed after side effects | LogManager.Public.cs:53-158; LogManager.cs:794-803 | shared early preflight before Resources.Load and ownership; registration-before-acquisition closes TOCTOU | no target/config/global mutation; no success; same target can be used by a later live init |

## Critical Lifecycle Flows That Must Never Regress

1. Pre-cancelled token: no ownership, no target/config/source side effect, no success publication.
2. Cancellation during Register: callback may complete before Register returns; returned registration is still released exactly once.
3. Cancellation during initialization: no Active publication; every already-acquired owner rolls back.
4. Active token cancellation: public availability is revoked immediately and physical cleanup runs once on main.
5. Worker cancellation: Cancel returns without waiting for Unity main; no Unity API or consumer Dispose runs on worker.
6. Old token A after completed A cleanup: no effect on active B.
7. Old queued A drain after B starts: identity check makes it a no-op.
8. Initialize B after A is requested: main thread completes A cleanup before B acquires anything.
9. Duplicate Initialize while A is healthy: B token is not registered and B targets are not owned.
10. Explicit Dispose, Cancel, Dispose, and repeated orderings: exactly one physical cleanup.
11. Throwing source/logger/target: all later owners still receive cleanup and a fresh generation can initialize.
12. Explicit Dispose with an uncancelled external token: A's scheduled update loop still terminates.
13. Old update continuation after B starts: no old target update and no access to B dependencies.
14. Reentrant Initialize from cleanup: rejected until A's finally clears current.
15. Domain reload disabled: no generation, callback, event subscription, or update loop survives into the next play session.
16. Terminal application/play exit: active targets receive the synchronous main-thread terminal drain even though token callbacks themselves remain request-only.
17. Public signatures remain source-compatible and CoreLogger.Log receives no new lifecycle branch, allocation, lock, or volatile read.

## Regression Test Design

### Fixture Strategy

Do not extend the existing roughly 2,000-line asset-backed lifecycle fixture for all concurrency scenarios.

Add a focused Editor fixture and a focused PlayMode fixture using:

- the real public LogManager.Initialize and LogManager.Dispose paths;
- in-memory ScriptableObject configuration;
- all unrelated log sources disabled;
- a real LifecycleTrackingTarget boundary spy, not a mocked manager;
- ConcurrentQueue for delivered entries;
- Interlocked counters for Apply, Log, Dispose, and log-after-dispose;
- captured Dispose thread id;
- optional bounded ManualResetEventSlim gates;
- bounded waits and joins released in finally.

The current RunOnWorkerThread helper blocks the Unity main thread until work completes and is unsuitable for a design that posts cleanup. The current TrackingLogTarget uses a non-thread-safe List and cannot prove thread affinity.

### Mandatory Editor Tests

1. Initialize_WithPreCancelledLifetime_DoesNotAcquireOrMutateTargets
   - snapshot target apply count and relevant Unity logger state;
   - cancel first, call public Initialize;
   - prove no apply/dispose/global mutation and public fallback behavior;
   - initialize the same target with a live token and deliver a real log.

2. Initialize_WhileActive_SecondTokenCannotReplaceLifetimeOwnership
   - initialize A/token A;
   - attempt B/token B;
   - cancel B and prove A still receives a unique message and B was never owned;
   - cancel A, pump cleanup, and prove A disposed once.

3. OldToken_AfterCompletedDisposeAndReinitialize_CannotAffectB
   - complete A disposal;
   - initialize B;
   - cancel token A from a worker;
   - prove messages before/after reach B and B remains undisposed.

4. FailedInitialize_ReleasesAcquiredOwnershipAndOldTokenCannotAffectRetry
   - fail after ownership transfer through a real target boundary, such as a throwing configuration-name getter;
   - prove cleanup and public fallback;
   - initialize B, cancel token A, and prove B still delivers;
   - cancel token B and prove B cleanup.

5. Cancellation_WhenOneTargetDisposeThrows_ContinuesCleanupAndAllowsRetry
   - target one records then throws;
   - target two records normal cleanup;
   - Cancel must not throw;
   - both targets receive exactly one cleanup attempt;
   - a fresh generation delivers.

6. CancelAndRepeatedDispose_AreIdempotent
   - cover Cancel -> Dispose -> Dispose and Dispose -> Cancel -> Dispose;
   - exactly one physical target cleanup per generation.

7. Internal LifecycleGeneration registration handshake
   - use the production internal abstraction already consumed by LogManager;
   - pre-cancelled Register invokes callback before attachment returns;
   - attach/rollback releases one registration;
   - stale generation requests cannot acquire cleanup authority;
   - a Barrier-based concurrent attach/cancel stress is supplementary, not sole evidence.

The direct internal tests are permitted because TheBestLogger.Core already grants InternalsVisibleTo to both test assemblies. They test a production abstraction, not conditional test plumbing.

### Mandatory PlayMode Tests

8. Cancel_FromWorker_ReturnsBeforeMainPump_ThenDisposesOnMain
   - capture main thread id;
   - start worker Cancel;
   - block main only for a bounded worker-completion assertion;
   - prove worker returned and target is not physically disposed on worker;
   - yield to a bounded deadline;
   - prove Dispose thread id equals main, count is one, and public logging uses fallback.

9. Initialize_AfterWorkerCancellation_DrainsAThenActivatesB
   - initialize A;
   - worker requests cancellation and returns before main pump;
   - call Initialize B on main;
   - prove A cleanup completed before B target/config acquisition;
   - prove B public delivery and token ownership.

10. OldQueuedDrain_CannotDisposeNewGeneration
    - arrange an A request whose Post remains queued;
    - drain A synchronously through Initialize B;
    - allow the queued A callback to run;
    - prove B still delivers and remains undisposed.

11. DisposeAndImmediateReinitialize_UpdatesOnlyCurrentGeneration
    - A scheduled target observes at least one update;
    - dispose A without cancelling its external token;
    - initialize B;
    - after bounded periods, A count is fixed and B advances.

12. ScheduledUpdateLoop_StopAStartB_ACompletionTerminates
    - directly test the real internal ScheduledUpdateLoop;
    - no sleeps as synchronization; use bounded frame/time deadlines and completion;
    - A completion finishes and A never ticks after stop; B ticks.

13. ApplicationExitLifetime_PerformsTerminalMainThreadCleanup
    - initialize with Application.exitCancellationToken;
    - exit Play Mode through the MCP-owned scenario;
    - prove target cleanup/flush occurred on main before the next session.

14. DomainReloadDisabled_EnterExitTwice_HasNoStaleGeneration
    - isolated category;
    - save/restore Enter Play Mode settings in finally;
    - disable domain reload;
    - enter, initialize A with exit token, exit and prove cleanup;
    - enter again, initialize B and prove public delivery; exit and prove cleanup.

### Assertion Policy

Primary public evidence:

- active means a unique public log reaches the intended real target exactly once;
- inactive means the documented fallback warning occurs and no target receives the message;
- cleanup means exact target counts, thread id, and post-dispose behavior;
- ordering means recorded target/source acquisition and disposal sequence.

Do not make these the primary proof:

- concrete CoreLogger or FallbackLogger type checks;
- GetCurrentLogTargetConfigurationsSnapshot alone;
- reflection over private lifecycle fields;
- test-only public methods;
- mock-only assertions;
- unbounded waits;
- sleep-based race timing;
- GC/WeakReference token-leak tests as release gates.

## Test-Quality Verdict

Current test-quality verdict remains keep and improve, 67/100, medium confidence:

- the three sequential regressions are useful;
- they use real manager orchestration and no new public hooks;
- they do not prove either High finding, update-task ownership, exception-complete cleanup, or domain reload.

Expected verdict after the mandatory suite is implemented:

- public behavior coverage: strong;
- concurrency determinism: strong where bounded gates or internal production abstractions are used;
- Unity integration confidence: strong after PlayMode and isolated domain-reload runs;
- remaining confidence limiter: player/IL2CPP/device backend behavior.

## Compatibility and Cost

### Public API

- no public signature changes;
- duplicate Initialize remains a warning/no-op;
- explicit main-thread Dispose remains synchronous and idempotent;
- token cancellation becomes logically synchronous and physically main-thread-asynchronous;
- off-main direct Dispose uses the same request/post behavior;
- invalid/pre-cancelled Initialize explicitly does not take ownership of caller targets.

The last two semantic clarifications must be documented in XML comments and release notes.

### Allocations

Cold path per successful generation:

- one LifecycleGeneration;
- one cancellation registration only when CanBeCanceled;
- one ownership entry per target;
- one ScheduledUpdateLoop and CTS/task only when scheduled updates exist;
- at most one posted cleanup work item;
- existing logger/source/decorator allocations.

No new allocation, lock, generation check, or cancellation check is added to CoreLogger.Log.

Manager-level CreateLogger and configuration entrypoints may perform a volatile active-generation read; these are not the per-message logging hot path.

### Unity 2022.3 Compatibility

- CancellationToken.Register without context capture is available on the target profile;
- an already-cancelled token invokes its callback synchronously, which the handshake handles;
- UnitySynchronizationContext.Post queues non-blocking work for main-thread execution;
- UnitySynchronizationContext.Send can wait for the main thread and is prohibited here;
- Interlocked and Volatile are supported by Mono and IL2CPP;
- no Task blocking or unsupported modern API is required.

Reference evidence:

- Microsoft CancellationToken.Register documentation:
  https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken.register
- Microsoft CancellationTokenRegistration.Dispose documentation:
  https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtokenregistration.dispose
- Unity 2022.3 UnitySynchronizationContext source:
  https://github.com/Unity-Technologies/UnityCsReference/blob/2022.3/Runtime/Export/Scripting/UnitySynchronizationContext.cs
- Unity Application.exitCancellationToken documentation:
  https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Application-exitCancellationToken.html

## Implementation Boundaries

Likely runtime changes:

- LogManager.cs:
  - add LifecycleGeneration, TargetOwnership, and ScheduledUpdateLoop production owners;
  - replace lifecycle booleans/registration/global run flag;
  - implement request/post and main-thread cleanup;
  - implement detach-first best-effort cleanup;
  - add SubsystemRegistration reset and terminal drain ownership.
- LogManager.Public.cs:
  - shared preflight before Resources.Load;
  - transactional Initialize and active-generation predicate;
  - internal initialization logger creation;
  - XML documentation for logical versus physical disposal.
- Tests:
  - keep useful sequential tests;
  - move new concurrency coverage into focused Editor and PlayMode fixtures;
  - strengthen assertions to public delivery/fallback/thread/cleanup behavior.

Fields and methods to remove from lifecycle ownership:

- _wasDisposed;
- _isInitialized;
- _isRunningUpdates;
- global _targetUpdates;
- _disposingToken;
- global _disposingTokenRegistration;
- RegisterDisposingToken;
- ReleaseDisposingTokenRegistration;
- FireAndForget ownership of RunUpdates.

Do not fold unrelated logger configuration, debug rollout, or delivery behavior into this patch.

## Validation Plan

All Unity execution must use the XUUnity MCP wrapper, never direct Unity CLI.

Recommended order after implementation:

1. ensure-ready for DevAccelerationSystem/DevAccelerationSystem using Unity 2022.3.62f3;
2. project refresh and compile;
3. focused internal lifecycle/update-owner Editor tests;
4. focused public Editor lifecycle tests;
5. focused public PlayMode worker/main-thread tests;
6. full TheBestLogger.EditorTests;
7. full TheBestLogger.PlayModeTests;
8. isolated DomainReloadLifecycle category with settings restored in finally;
9. console scan for unhandled exceptions, OperationCanceledException, thread-affinity violations, leaked task warnings, and duplicate cleanup;
10. final compiler/status evidence;
11. tracked consumer validation in DevAccelerationSystem.DemoProject;
12. Mono player plus IL2CPP/device lane before broad package publication;
13. restore editor state if the wrapper opened Unity.

Performance validation:

- static audit confirms no CoreLogger.Log change;
- compare lifecycle allocations only as a cold-path sanity check;
- run existing logger performance tests if implementation touches logger/target delivery code beyond the design boundary.

## Validation Evidence for This Design

- The complete prior focused review was loaded and used as the finding source.
- Current runtime and lifecycle-test source was re-read at commit 056a0d1086fbe8dfb75990e7d3718973c000ce93.
- The three scoped runtime/test files had no local delta during design.
- Prior XUUnity MCP evidence remains:
  - TheBestLogger.EditorTests: 269/269 passed, request a5b5c8c9-85ea-4c8d-a60f-b1b770cb8c1e;
  - TheBestLogger.PlayModeTests: 14/14 passed, request f96349f6-048b-4b3a-9693-1c784d96cb01;
  - compiler errors: zero.
- Primary framework behavior was checked against Microsoft documentation and Unity 2022.3 reference source.
- No Unity execution was run for this architecture-only turn because no runtime or test implementation changed.

The prior green suites prove only the existing sequential behavior. They do not validate this unimplemented design.

## Remaining Gaps

- exact implementation and self-review;
- real UnitySynchronizationContext request/drain behavior in the new tests;
- terminal flush ordering between Application.exitCancellationToken, Application.quitting, and Play Mode exit;
- deterministic registration callback-before-attachment unit evidence;
- update-loop completion and retention evidence;
- domain-reload-disabled two-session result;
- throwing consumer cleanup behavior;
- tracked consumer compile/runtime result;
- Mono player and IL2CPP/device behavior;
- release-note confirmation of asynchronous physical cleanup after token cancellation.

## Release Recommendation

Architecture recommendation: approve Approach B for implementation.

Release recommendation for the current code: do not release or publish the local 4.4.2 package state.

Release can proceed only after:

1. both High findings are closed by generation-scoped authority and main-thread cleanup;
2. the scheduled loop is generation-owned and observably terminates;
3. cleanup is detach-first and exception-complete;
4. pre-cancel and failed-init ownership semantics are tested;
5. focused and full Editor/PlayMode MCP lanes pass;
6. domain-reload-disabled and terminal-exit scenarios pass;
7. tracked consumer validation passes;
8. player/IL2CPP risk is either executed or explicitly accepted by the release owner.

Recommended next XUUnity task: implementation planning followed by a narrow lifecycle refactor; do not implement this as another local boolean/registration patch.
