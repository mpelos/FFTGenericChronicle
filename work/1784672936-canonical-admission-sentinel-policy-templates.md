# Canonical admission sentinel policy templates

## Context

The canonical admission sentinel emitter produced authoring, item metadata, ability bindings, Reaction bindings, and runtime settings. After policy-ticket templates became the normal admission-side producer path, the emitted settings still lacked `DclCanonicalPolicyTicketTemplatesPath`, which would make the live sentinel retain admissions while waiting for external tickets.

## Hypothesis

The sentinel emitter should write a strict policy-ticket template bundle and point generated runtime settings at it. This keeps the prepared probe aligned with the template-producer bridge rather than the external-ticket test route.

## Validation

- Updated the canonical admission sentinel emitter to write `*-dcl-policy-ticket-templates.json`.
- Updated generated settings to set `DclCanonicalPolicyTicketTemplatesPath`.
- Extended settings validation smoke coverage so a templated canonical admission profile no longer emits the missing-template-path warning.
- Ran a dry sentinel emission and confirmed the output directory contains `probe-dcl-policy-ticket-templates.json` and the generated settings references it.

## Result

The canonical admission sentinel now emits all five canonical runtime artifacts plus settings for the template-producer path. Live proof is still gated, but the prepared probe no longer starts from a configuration that intentionally waits for externally supplied tickets.
