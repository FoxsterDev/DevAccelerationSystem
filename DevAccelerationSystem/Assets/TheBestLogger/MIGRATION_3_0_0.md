# TheBestLogger Migration Guide 3.0.0

This guide is for projects upgrading an existing `TheBestLogger` integration to package version `3.0.0`.

The main breaking change is intentional:

- the logger no longer exposes public runtime configuration objects for read or write
- runtime config updates now go through raw JSON remote patches only

## Breaking API Changes

Removed public APIs:

- `LogManager.GetCurrentLogTargetConfigurations()`
- `LogManager.UpdateLogTargetConfiguration(...)`
- `LogManager.UpdateLogTargetsConfigurations(...)`

Replacement public APIs:

- `LogManager.TryApplyRemoteConfigurationPatch(string targetName, string rawJsonPatch, out string error)`
- `LogManager.TryApplyRemoteConfigurationDocument(IReadOnlyDictionary<string, string> rawJsonPatches, out string error)`

## Why This Changed

The old API shape let app code:

- read current runtime logger configs back out of `LogManager`
- mutate them as objects
- push them back into the logger

That shape made it too easy for external code to:

- depend on logger internals
- bypass a clean remote-config boundary
- create brittle merge logic in app code
- send semantically invalid config state back into the logger

`3.0.0` moves the contract to a smaller boundary:

- app code owns provider-specific remote-config parsing
- app code normalizes data into `targetName -> rawJsonPatch`
- `LogManager` validates and applies only that normalized patch form

## Required Migration Steps

### 1. Remove Getter-Based Runtime Config Reads

Before:

```csharp
var configurations = LogManager.GetCurrentLogTargetConfigurations();
var config = configurations[nameof(UnityEditorConsoleLogTargetConfiguration)];
config.MinLogLevel = LogLevel.Warning;
LogManager.UpdateLogTargetsConfigurations(configurations);
```

After:

```csharp
var applied = LogManager.TryApplyRemoteConfigurationPatch(
    nameof(UnityEditorConsoleLogTargetConfiguration),
    "{\"MinLogLevel\":2}",
    out var error);
```

Important:

- project code should no longer read current logger config objects from `LogManager`
- project code should own any desired patch state locally

### 2. Replace Typed Runtime Updates With Raw JSON Patches

Before:

```csharp
LogManager.UpdateLogTargetConfiguration(nameof(OpenSearchLogTargetConfiguration), configurationObject);
```

After:

```csharp
var rawJsonPatch = JsonUtility.ToJson(configurationObject);
var applied = LogManager.TryApplyRemoteConfigurationPatch(
    nameof(OpenSearchLogTargetConfiguration),
    rawJsonPatch,
    out var error);
```

### 3. Replace Multi-Target Runtime Updates With A Normalized Document

Before:

```csharp
LogManager.UpdateLogTargetsConfigurations(configurations);
```

After:

```csharp
var document = new Dictionary<string, string>
{
    [nameof(UnityEditorConsoleLogTargetConfiguration)] = "{\"MinLogLevel\":2}",
    [nameof(OpenSearchLogTargetConfiguration)] = "{\"Muted\":true}"
};

var applied = LogManager.TryApplyRemoteConfigurationDocument(document, out var error);
```

## New Behavioral Contract

### `TryApplyRemoteConfigurationPatch(...)`

- returns `true` when the patch is accepted and applied
- returns `false` when validation or apply fails
- returns a client-facing error string through `out string error`

### `TryApplyRemoteConfigurationDocument(...)`

- same success and error contract as the single-patch API
- applies atomically
- if any patch in the document is invalid, the whole document is rejected

Do not assume partial success for batch updates.

## Recommended Integration Shape

Split your app integration into two layers:

1. app-level logger facade
2. app-level remote-config adapter

The package includes generic examples for both:

- [GenericProjectLoggerExample](./Runtime/Examples/Integration/GenericProjectLoggerExample.cs)
- [GenericRemoteConfigAdapterExample](./Runtime/Examples/Integration/GenericRemoteConfigAdapterExample.cs)

Recommended ownership:

- logger facade:
  - category access
  - cached `ILogger` instances
  - persisted debug id
  - debug-mode enablement
- remote-config adapter:
  - provider-specific key lookup
  - provider-specific JSON payload extraction
  - mapping external keys to logger target configuration names
  - normalization into `targetName -> rawJsonPatch`

## Best-Practice Rules After Migration

- do not rebuild old getter-based merge flows around logger internals
- do not keep project logic coupled to runtime config object graphs owned by the logger
- handle `bool` + `error` from `Try*` APIs explicitly
- surface remote-config rejection reasons to your client logs or diagnostics UI
- keep category names stable and human-readable
- preserve real `Exception` objects when logging failures

## Useful Follow-Up Files

- [README](./README.md)
- [AI Upgrade And Log Review Prompt 3.0.0](./AI_Upgrade_And_Log_Review_Prompt_3_0_0.md)
