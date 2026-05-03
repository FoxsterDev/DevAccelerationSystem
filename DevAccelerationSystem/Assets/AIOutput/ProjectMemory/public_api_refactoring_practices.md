# Public API Refactoring Practices

## Purpose
Capture durable practices for evolving `TheBestLogger` public SDK surface without forcing breakage onto existing consumers.

## Current Public Extensibility Pattern

### Opt-In Interface For New Capability
- When a new capability is performance-sensitive or behaviorally optional, prefer an explicit opt-in interface instead of widening the baseline contract for every consumer.
- Current source example:
  - `OpenSearchLogDTO` stays a plain serializable DTO.
  - `IOpenSearchBatchJsonSerializable` marks DTOs that support the custom low-allocation batch serializer.
  - `OpenSearchBatchCompatibleLogDTO` is the consumer-friendly base class that implements the interface and owns the manual batch-writing contract.

### Safe Consumer Path vs Raw Power Path
- Prefer exposing a safe, guided base class for consumers instead of pushing the lowest-level interface as the primary extension point.
- Current source example:
  - public consumers should usually derive from `OpenSearchBatchCompatibleLogDTO`
  - they should not need to implement `IOpenSearchBatchJsonSerializable` directly unless they intentionally own the whole batch JSON contract

### Compatibility Fallback
- New extensibility should not silently break old DTOs.
- If the consumer does not opt in to the new interface-based path, preserve old behavior through a compatibility branch.
- Current source example:
  - single `Log(...)` uses `JsonUtility`
  - batch `LogBatch(...)` uses manual JSON only for DTOs that implement `IOpenSearchBatchJsonSerializable`
  - otherwise batch falls back to `JsonUtility`

## Public Surface Design Rules

### Keep Base Data Types Clean
- Keep the most common public type data-oriented and simple.
- Do not move specialized performance plumbing into the baseline DTO if most consumers do not need it.
- Current source example:
  - `OpenSearchLogDTO` contains public payload data and `PrepareForJsonSerialization()`
  - manual field-name constants and custom JSON writing live in `OpenSearchBatchCompatibleLogDTO`, not in the base DTO

### Prefer Narrow Hooks Over Full Overrides
- Give consumers a narrow hook for the exact part they need to extend, instead of forcing them to reimplement the whole payload contract.
- Current source example:
  - `OpenSearchBatchCompatibleLogDTO.WriteAdditionalFields(ref OpenSearchObjectWriter writer)`
  - base fields remain owned by package code
  - consumer code only writes custom fields

### Preserve Existing Semantics First
- When refactoring for performance, keep the compatibility path behaviorally identical before optimizing the fast path further.
- Current source example:
  - fallback DTOs still serialize through Unity `JsonUtility`
  - manual batch path is additive, not a behavioral rewrite for all DTOs

### Do Not Force Hidden Runtime Coupling
- Avoid public APIs that require consumers to know internal serializer state or batch-builder state.
- Current source example:
  - consumer-facing `OpenSearchObjectWriter` gives `WriteStringField(...)`, `WriteBooleanField(...)`, and related methods
  - consumer code does not manipulate raw payload builders directly

## Consumer Guidance Rules

### If A Consumer Needs Only Extra Serializable Fields
- Derive from the simple base DTO:
  - `OpenSearchLogDTO`
- Use Unity serialization fields only.
- Rely on the compatibility path.

### If A Consumer Needs Extra Fields And Batch-Optimized Serialization
- Derive from:
  - `OpenSearchBatchCompatibleLogDTO`
- Implement:
  - `WriteAdditionalFields(ref OpenSearchObjectWriter writer)`
- Optionally implement `ISerializationCallbackReceiver` when runtime values must be populated right before serialization.

### Show The Pattern With Real Examples
- Public extensibility is easier to adopt when source includes both:
  - an optimized example
  - a compatibility-only example
- Current source examples:
  - `GameSessionOpenSearchLogDTOExample`
  - `GameSessionOpenSearchLogDTOJsonUtilityFallbackExample`

## Refactoring Rules

### Separate Baseline API From Optimized Path
- First split the simple baseline type from the optimized opt-in path.
- Then move constants, manual writer code, and specialized helpers into the optimized path owner.
- This reduces surface noise for most users while keeping the fast path available.

### Prefer Additive Refactors For Public SDKs
- Introduce new interface/base-class paths alongside the old path first.
- Only remove the old path after real consumer migration evidence exists.

### Performance Changes Must Respect Public Contracts
- Internal optimization is allowed to add compatibility shims when needed.
- Current source example:
  - fallback DTOs require a serialized `TimeUTC` string before `JsonUtility`
  - manual batch DTOs can use raw `TimeUtcValue` and format directly into the payload writer

## Testing Expectations For Public API Changes
- Add deterministic editor tests for:
  - opt-in interface path
  - compatibility fallback path
  - consumer example DTOs
- Validate not only custom fields, but also base fields that can regress differently between fast path and fallback path.
- Current concrete regression to remember:
  - batch fallback DTOs lost `TimeUTC` until the fallback branch explicitly populated the serialized timestamp before `JsonUtility`
