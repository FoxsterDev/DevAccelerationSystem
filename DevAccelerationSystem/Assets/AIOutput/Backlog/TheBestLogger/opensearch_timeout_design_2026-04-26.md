# OpenSearch Timeout Design

Date: `2026-04-26`
Scope: `TheBestLogger`
Component: `OpenSearchLogTarget`
Status: `design only`

## Problem

`OpenSearchLogTarget` is used in production.
A hardcoded low timeout such as `2s` is not an acceptable production default:
- mobile networks can exceed it under normal conditions
- aggressive timeout values can silently increase log loss
- the right timeout is environment-specific
- this surface already has remote-config usage, so policy should not be compiled into runtime code

## Current State

Current automated coverage proves:
- payload serialization
- batch payload shape
- invalid host recovery
- `4xx` recovery
- `5xx` recovery
- API key update via runtime reconfiguration

Current uncovered policy area:
- request timeout behavior as a supported production contract

## Recommended Design

Recommended approach:
1. add a timeout field to `OpenSearchLogTargetConfiguration`
2. allow it to merge from remote config
3. validate the value before applying it to `UnityWebRequest.timeout`
4. keep a conservative fallback only when config is absent or invalid

Recommended config field:
- `RequestTimeoutSeconds`

Recommended validation:
- reject `<= 0`
- clamp to a sane minimum and maximum
- log diagnostics when invalid remote values are ignored

Recommended runtime behavior:
- if config timeout is valid, assign it to `UnityWebRequest.timeout`
- if config timeout is invalid or missing, use a documented fallback
- do not silently switch policy between editor and player

## Suggested Policy

Do not hardcode `2s` as production default.

Suggested starting point for discussion:
- default fallback: `10s`
- allowed range: `3s .. 60s`

Reasoning:
- `10s` is materially safer for mobile networks than `2s`
- `3s` prevents obviously broken hyper-aggressive configs
- `60s` prevents unbounded hangs from bad remote values

This is still a policy proposal, not a measured final value.
Final thresholds should come from backend SLA plus mobile field evidence.

## Required Tests After Implementation

Add or update tests for:
- timeout field merge from remote config
- valid timeout applied to outgoing request
- invalid timeout ignored and replaced by fallback
- very low timeout values clamped or rejected
- recovery after timeout still allows later requests

Suggested suites:
- `OpenSearchLogTargetConfigurationTests`
- `OpenSearchLogTargetDeliveryTests`
- `OpenSearchRemoteConfigRuntimeTests`

## Release Guidance

Until timeout becomes configurable:
- treat `P0.5` as partially closed
- do not claim timeout behavior is production-hardened
- keep device/network validation pending for real mobile environments
