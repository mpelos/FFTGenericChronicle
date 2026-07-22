# Canonical admission log schema alignment

Offline inspection found that the canonical-admission live analyzer required a completed admission
line with `strikes=<n>`, but the managed hook's completed-admission log omitted that field. A live
run could therefore have admitted the action and published the policy-ticket template correctly
while still failing the analyzer as if no matching completed admission existed.

The runtime completed-admission log now emits `strikes={completed.Admissions.Count}` beside
`targetCount`. The analyzer smoke test also checks the C# source for this field so the offline gate
fails before a live probe if the runtime/analyzer contract drifts again.

Verified gates:

- `python tools\test_dcl_canonical_admission_template_live.py`
- `python -m py_compile tools\test_dcl_canonical_admission_template_live.py tools\analyze_dcl_canonical_admission_template_live.py`
- `dotnet test codemod\fftivc.generic.chronicle.codemod.smoketests\fftivc.generic.chronicle.codemod.smoketests.csproj --no-restore`
