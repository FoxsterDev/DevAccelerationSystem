# Platform Constraints

## Purpose
Record durable engine and package constraints for `DevAccelerationSystem`.

## Engine Baseline
- `Assets/TheBestLogger/package.json` declares Unity baseline `2022.3`.
- Project packages also include:
  - `com.unity.mobile.android-logcat`
  - `com.unity.textmeshpro`
  - `com.unity.test-framework`
  - `com.unity.test-framework.performance`
  - local `com.cysharp.zstring`

## Source-Surface Constraints
- `Assets/TheBestLogger/` behaves like a package-style reusable runtime surface.
- `Assets/DevAccelerationSystem/` contains editor and tooling responsibilities that should not be merged casually into logger runtime ownership.

## Constraint Rules
- Do not assume editor-test success alone proves runtime or consumer integration health.
- Do not assume editor or playmode proof alone is equal to physical-device proof for Android or Apple native log targets.
- When a change touches package/runtime behavior, pair source-level validation with consumer validation planning.
- When a change touches tooling or shell-script-driven workflow, verify the owning editor/tooling surface first before widening the patch.
- Performance claims should prefer the dedicated performance-test surface over ad-hoc stopwatch-only assertions.

## Consumer Constraints
- `DevAccelerationSystem.DemoProject/` is the default tracked consumer validation target.
- `DAS.LocalProject/` is useful local evidence but is ignored by the nested repo's `.gitignore`.
