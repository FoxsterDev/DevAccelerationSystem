# TheBestLogger Device Performance Validation Plan

Date: `2026-04-26`
Scope: `TheBestLogger`
Workspace: `DevAccelerationSystem/DevAccelerationSystem/`
Goal: extend current editor-based performance evidence into device-facing release gates for Android and iOS/macOS runtime validation.

## Current Baseline

Current performance evidence already exists through `Unity Performance Testing` package:
- package installed: `com.unity.test-framework.performance@3.1.0`
- editmode perf suite:
  - `CoreLogger.LogInfo`
  - `CoreLogger.LogFormat`
  - `Dispatch.FlushQueuedBurst`
  - `Batch.FlushBurst`
- playmode perf suite:
  - `PlayMode.CoreLogger.LogInfo.FrameTime`
  - `PlayMode.BatchDispatch.MixedPressure.FrameTime`

Current editor batch-mode baseline on Unity `2022.3.62f3`:
- `CoreLogger.LogInfo`
  - average time: about `0.48 ms` per `500` iterations
  - average GC sample: about `7.01`
- `CoreLogger.LogFormat`
  - average time: about `0.57 ms` per `500` iterations
  - average GC sample: about `8.01`
- `Dispatch.FlushQueuedBurst`
  - average flush time: about `0.036 ms`
  - average GC sample: about `0.3`
- `Batch.FlushBurst`
  - average flush time: about `0.160 ms`
  - average GC sample: about `57.8`
- `PlayMode.CoreLogger.LogInfo.FrameTime`
  - average frame contribution: about `0.027 ms`
  - max observed sample: about `0.259 ms`
- `PlayMode.BatchDispatch.MixedPressure.FrameTime`
  - average frame contribution: about `0.673 ms`
  - max observed sample: about `14.44 ms`

Interpretation:
- these are useful baselines for regression comparison
- they are editor-only evidence
- they are not safe to use as final production thresholds for mobile devices

## Why Device Validation Is Still Required

Editor batch-mode evidence does not prove:
- mobile thread scheduling behavior
- IL2CPP runtime behavior
- native stack-trace extraction cost on device
- actual Android/iOS main-thread frame impact
- platform log target behavior under sustained pressure
- thermal or long-session degradation

For the stated production goal:
- `0 ANR`
- `0 logger-attributable freezes`
- `minimum allocations`

device evidence is mandatory.

## Device Matrix

Minimum Android matrix:
- low/mid Android device
  - `6-8 GB RAM`
  - mid-tier CPU/GPU
- high-end Android device
  - flagship-tier CPU/GPU

Minimum Apple matrix:
- one modern iPhone
- one iPad or one additional iPhone class if iPad is not a target
- one Apple Silicon macOS editor/player runtime for Apple target validation if macOS is supported

Build requirements:
- `IL2CPP`
- development build for profiling runs
- non-development sanity run to validate no debug-only behavior masks issues

## Required Device Scenarios

### D1 Hot Path Logging

Measure on device:
- `LogInfo`
- `LogFormat<T1>`
- `LogFormat<T1,T2>`
- `LogException`

For each scenario:
- main-thread only
- background-thread only
- mixed main/background

Capture:
- average time
- median
- p95
- p99
- GC allocations

### D2 Batch and Dispatch Pressure

Measure:
- dispatch-only target under worker bursts
- batch-only target under sustained load
- batch + dispatch combined
- mixed `Critical`, `Important`, `NiceToHave`

Capture:
- flush duration
- frame-time spikes during flush
- total logs delivered
- duplicates
- drops

### D3 Long Session Soak

Run:
- `30 min` smoke soak
- `2 h` standard soak
- `8 h` nightly soak

During soak:
- periodic main-thread logging
- periodic background-thread logging
- periodic configuration updates
- scene transitions if consumer project exists

Capture:
- memory growth
- GC trend
- dropped logs
- duplicate logs
- frame spike distribution
- thermal slowdown notes

### D4 Platform Target Validation

Android:
- `AndroidSystemLogTarget`
- any OpenSearch or remote-config path used by Android builds

Apple:
- `AppleSystemLogTarget`
- exception path with and without stacktrace

Capture:
- per-level mapping correctness
- exception behavior
- runtime cost under repeated logging
- no platform-only crash or stall

## Suggested Release Thresholds

These should start as provisional thresholds and become stricter after 2-3 device baselines are collected.

### Android provisional thresholds

- `LogInfo` hot path:
  - no unexpected frame spikes attributable to logger
  - average cost should stay within `20%` of established baseline on same device class
- dispatch flush burst:
  - no single flush should exceed `2 ms` on high-end devices
  - no single flush should exceed `4 ms` on mid-tier devices
- mixed batch+dispatch frame pressure:
  - no sustained p95 frame contribution above `1 ms`
  - no p99 spike above `4 ms` without explicit investigation
- soak:
  - no ANR
  - no unbounded memory growth
  - no duplicate or dropped critical logs

### Apple provisional thresholds

- same relative thresholds as Android
- additional gate:
  - Apple exception target path must not increase exception logging frame cost by more than `25%` over generic path in repeated runs

## Required Artifacts Per Release

Store under:
- `DevAccelerationSystem/Assets/AIOutput/ValidationReports/`

Per release attach:
- `device_perf_summary_<date>.md`
- raw performance test outputs
- device list with OS versions
- build config used
- noted spikes and investigation outcome

## Immediate Next Actions

1. Add consumer-facing perf scene or harness in `DevAccelerationSystem.DemoProject`.
2. Add Android/iOS-targeted runtime scenarios that mirror current package perf tests.
3. Start collecting first two Android baselines and first Apple baseline.
4. Convert provisional thresholds into committed per-device-class release thresholds.

## Current Assessment

Current state is materially better than before:
- package-based performance measurements are now present
- regression hooks exist in editmode and playmode

But current state is still not enough to claim:
- `0 ANR risk`
- `production-safe mobile perf certainty`

That claim still requires device-facing evidence.
