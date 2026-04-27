<!-- Managed by AIRoot/scripts/init_ai_project.sh project-memory-baseline -->
# Release Rules

## Purpose
Define the durable release-facing rules for `DevAccelerationSystem.DemoProject`.

## Fill In
- package or product identity if applicable
- release gates that must be satisfied before trusting rollout readiness
- tracked consumer validation targets
- evidence that does not count as release proof

## Rules
- Keep release-relevant validation and routing expectations here.
- Update this file when package identity, release gates, or representative validation targets change.
- For meaningful `TheBestLoggerSample` UI changes, release-facing confidence requires opening `LoggerSampleScene` in the Unity Editor and confirming the scene-authored layout is readable and intact.
- Code diff review does not count as sufficient release proof for sample-scene layout, spacing, or action-surface usability changes.
- If a sample-UI change touches logger controls, managed exception controls, or native crash controls, release-facing validation must confirm those affordances still exist on the scene surface and remain callable from the UI.
