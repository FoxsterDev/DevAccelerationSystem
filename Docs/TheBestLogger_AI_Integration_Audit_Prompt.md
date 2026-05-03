# TheBestLogger AI Integration Audit Prompt

Use this prompt when `TheBestLogger` is already integrated into a Unity project and you want an AI review focused on:

- correctness of integration
- runtime safety
- log-message quality
- threading and batching safety
- logging overhead and performance discipline

This prompt is designed for the `xuunity` protocol used in this monorepo, but it can also be adapted for another code-review agent.

## Ready-To-Run Prompt

```text
xuunity review the existing TheBestLogger integration in this project.

Scope:
- inspect bootstrap and initialization flow
- inspect whether any logger is created before `LogManager.Initialize(...)`
- inspect constructor, static-field, and static-property logger creation
- inspect Resources-backed LogManagerConfiguration usage
- inspect current log target set and each target configuration
- inspect log source enablement
- inspect logger category usage and log-message patterns
- inspect background-thread logging behavior
- inspect batching, dispatch-to-main-thread, stack-trace, and debug-mode settings
- inspect StabilityHub usage if present
- inspect OpenSearch integration and config-update path if present
- inspect direct Unity Debug usage that should probably go through TheBestLogger instead
- inspect hot paths that may create avoidable allocations, main-thread work, or noisy logs

Review goals:
- confirm whether the integration is correct
- find production risks
- find performance risks
- find observability gaps
- find places where log messages are too noisy, too weak, or not structured enough
- identify missing validation evidence

Constraints:
- do not redesign public APIs unless it is unavoidable
- treat existing production behavior as sensitive
- distinguish clearly between:
  - proven by source
  - proven by tests
  - inferred risk

Expected output:
- findings first, ordered by severity
- include file references
- call out logger bootstrap path
- call out target-by-target assessment
- call out whether thread-safety and dispatch settings are coherent
- call out whether stack traces and min log levels are appropriate
- call out whether high-frequency logs are too expensive
- call out whether consumer validation or device proof is still missing
- after findings, provide:
  - integration health verdict
  - performance verdict
  - top 5 fixes
  - recommended validation plan

Focus especially on:
- early `LogManager.CreateLogger(...)` calls that force `FallbackLogger`
- startup systems that create loggers in constructors or static initialization
- incorrect or duplicate source capture
- non-thread-safe targets used from worker threads without dispatch
- thread-safe targets unnecessarily dispatched to main thread
- remote targets without batching or with overly verbose levels
- stack traces enabled on hot paths
- exception paths that can fail while trying to log
- shutdown or dispose paths that can deadlock
- direct Debug.Log usage in systems that should use structured categories
- weak category naming or unstable category construction
- missing consumer or device-facing validation
```

## What A Good Review Should Answer

A useful review should answer these questions explicitly:

1. Is logger bootstrap correct and early enough?
2. Are any loggers created before initialization or from startup constructors/static initializers?
3. Is the target set coherent for the project stage and platform mix?
4. Are `BatchLogs` and `DispatchingLogsToMainThread` configured in a way that matches target thread safety?
5. Are remote sinks protected from noisy low-value traffic?
6. Are stack traces enabled only where they justify their cost?
7. Are important exceptions and crash-path events actually captured?
8. Are the hottest log paths likely to allocate or block unnecessarily?
9. Is there enough evidence to trust the current integration in production?

## What To Give Another AI

When using this prompt in another Unity project, give the AI enough project-local context to review the integration instead of only the package docs:

- the logger bootstrap entrypoint
- the `LogManager.Initialize(...)` call site
- any startup steps that can call `LogManager.CreateLogger(...)`
- the `Resources` folder or explicit config assets used by `LogManager`
- any wrapper types like `GameLogger`, `Logger`, or project-local logger factories
- example high-frequency systems that emit logs during startup, gameplay, and shutdown
- any consumer validation or device-validation evidence already available

If you can attach two docs from this repo, attach:

- `Docs/TheBestLogger_Integration_Best_Practices.md`
- `Docs/TheBestLogger_AI_Integration_Audit_Prompt.md`

## Optional Follow-Up Prompts

### Fix-Only Follow-Up

```text
xuunity fix only the P0 and P1 production risks found in the TheBestLogger integration review. Do not widen public APIs unless required. Keep findings traceable to tests where possible.
```

### Performance Follow-Up

```text
xuunity design and implement a targeted TheBestLogger performance validation pass for this project. Focus on hot-path allocations, main-thread dispatch cost, remote-target batching, and frame-time impact.
```

### Consumer Validation Follow-Up

```text
xuunity add or refresh consumer validation for the current TheBestLogger integration in the representative demo or sample project, then report what this consumer validation proves and what it still does not prove.
```

## Related Docs

- [TheBestLogger package README](../DevAccelerationSystem/Assets/TheBestLogger/README.md)
- [TheBestLogger Integration Best Practices](./TheBestLogger_Integration_Best_Practices.md)
