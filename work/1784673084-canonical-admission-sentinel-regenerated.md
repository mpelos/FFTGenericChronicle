# Canonical admission sentinel regenerated with policy templates

## Context

The existing canonical admission sentinel fixture in `work/1784545101-*` was emitted before policy-ticket templates were wired into the canonical runtime. Its settings enabled canonical admission but did not set `DclCanonicalPolicyTicketTemplatesPath`, so the next live run would prove only retention/waiting unless an external ticket was injected.

## Action

Regenerated the sentinel fixture with prefix `1784673033` using the updated smoke-test emitter:

```powershell
codemod\fftivc.generic.chronicle.codemod.smoketests\bin\AdmissionSentinelPolicyTemplates2\net9.0-windows\fftivc.generic.chronicle.codemod.smoketests.exe --test-dcl-canonical-runtime --emit-canonical-admission-sentinel work 1784673033
```

## Produced files

- `work/1784673033-battle-runtime-settings.canonical-admission-sentinel.json`
- `work/1784673033-dcl-actions.json`
- `work/1784673033-dcl-items.json`
- `work/1784673033-dcl-bindings.json`
- `work/1784673033-dcl-reaction-bindings.json`
- `work/1784673033-dcl-policy-ticket-templates.json`

## Validation

The regenerated settings points `DclCanonicalPolicyTicketTemplatesPath` at `work/1784673033-dcl-policy-ticket-templates.json`. The template bundle contains a DirectNumeric template for Fire ability `16`. The canonical admission live runbook now points at the regenerated fixture and expects template build plus same-ActionInstance bridge settlement.

## Result

The prepared live probe is now aligned with the admission-side template-producer path. The next live test can prove the guarded callback, complete admission capture, template build, ticket publication, and bridge settlement together instead of only proving admission retention.
