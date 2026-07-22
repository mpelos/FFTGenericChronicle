# Composed native dispatch bridge

Scope: offline DCL contract work for admitted native-family request composition.

Conclusion: the confirmed execution dispatcher can now consume the retained
`DclCanonicalNativeComposedExecution` wrapper emitted by native-family composition. The dispatcher
re-resolves the ability's runtime family and rejects a wrapper whose retained family diverges before
execution RNG, state mutation, or native publication.

This closes a small typed-bridge gap between the composition boundary and the confirmed dispatcher.
The family-specific request remains the exact deterministic input selected by the classified
composition entry; the wrapper simply keeps AbilityId, family, and request identity together until
dispatch.

Validation target:

- `DclCanonicalNativeConfirmedRequestComposer.Compose`
- `DclCanonicalConfirmedExecutionDispatcher.ResolveAndPublish`
- smoke-test sentinels in `TestDclCanonicalRuntime`
- `docs/modding/06-code-mod-runtime-dsl.md`
- `docs/modding/08-dcl-information-requirements.md`
- `tools/report_dcl_implementation_coverage.py`

Remaining work: live-prove the guarded admission callback and connect live family-policy input
providers to this already typed bridge.
