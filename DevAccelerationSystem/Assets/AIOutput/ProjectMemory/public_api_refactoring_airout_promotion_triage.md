# Public API Refactoring AIRoot Promotion Triage

## Purpose
Record the `xuunity`-style intake review for promoting public API refactoring knowledge from this project into shared `AIRoot` guidance.

## Intake Verdict
- Promote only the generic public-SDK evolution patterns.
- Keep all `OpenSearch`, `JsonUtility`, timestamp, and local validation details project-local.

## Shared Candidate

### Candidate A: Opt-In Capability Pattern
- Reusable rule:
  - add new functionality for SDK consumers through an explicit opt-in interface or marker when the capability is optional or performance-sensitive
- Why it is a good `AIRoot` candidate:
  - applies beyond Unity
  - applies beyond logging
  - public-safe
  - useful in many SDK refactors

### Candidate B: Safe Base Class Over Raw Interface
- Reusable rule:
  - expose a consumer-friendly base class as the primary extension path and keep the raw interface as the lower-level capability contract
- Why it is a good `AIRoot` candidate:
  - stable public API design rule
  - useful across packages and SDKs
  - not tied to this project's domain

### Candidate C: Compatibility Fallback For Incremental Rollout
- Reusable rule:
  - when introducing an optimized or more specialized path, preserve an old-path fallback for non-migrated consumers
- Why it is a good `AIRoot` candidate:
  - cross-project SDK evolution rule
  - protects consumer adoption
  - valuable in performance refactors and serialization refactors

### Candidate D: Keep The Base Public Type Clean
- Reusable rule:
  - keep the baseline public DTO or public extension type simple and data-oriented
  - move performance-specific machinery into a separate optimized path owner
- Why it is a good `AIRoot` candidate:
  - general refactoring principle for public surfaces
  - does not depend on Unity or OpenSearch

### Candidate E: Narrow Hook Instead Of Full Override
- Reusable rule:
  - prefer a narrow extension hook for custom fields or custom behavior instead of asking consumers to reimplement the full document or payload contract
- Why it is a good `AIRoot` candidate:
  - directly reusable as public API and code-style guidance
  - reduces regressions in shared base behavior

### Candidate F: Test Both Fast Path And Compatibility Path
- Reusable rule:
  - when refactoring a public API into optimized and fallback branches, add tests for both branches and verify base fields as well as custom fields
- Why it is a good `AIRoot` candidate:
  - strong reusable review guidance
  - not domain-bound

## Project-Only Residue

### Keep Local To This Project
- `OpenSearchLogDTO`
- `IOpenSearchBatchJsonSerializable`
- `OpenSearchBatchCompatibleLogDTO`
- `OpenSearchObjectWriter`
- Unity `JsonUtility` fallback details
- `TimeUTC` formatting rules
- batch fallback timestamp regression details
- OpenSearch index mapping concerns such as `date` vs `date_nanos`
- demo-project PlayMode local validation setup

### Why These Stay Local
- they are implementation-specific
- they embed Unity or OpenSearch constraints
- they are examples of the pattern, not the shared pattern itself

## Promotion Recommendation

### Recommended AIRoot Shape
- promote as shared public-SDK guidance, not as OpenSearch guidance
- likely destination types:
  - codestyle or utilities guidance about public API evolution
  - review checklist guidance for compatibility-preserving refactors

### Not Recommended
- do not promote concrete class names from this project
- do not promote timestamp or OpenSearch-specific tradeoffs into generic `AIRoot` knowledge
- do not promote local validation harness details into public core

## Practical Extraction Summary
- Good shared abstraction:
  - opt-in capability
  - safe default base class
  - compatibility fallback
  - narrow extension hook
  - split optimized path from baseline public type
  - dual-path test coverage
- Keep local:
  - all concrete logging/OpenSearch/Unity serializer details
