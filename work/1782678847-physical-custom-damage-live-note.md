# Physical Custom Damage Live Note

Evidence snapshot:

- `work/1782678308-battleprobe-log-physical-custom-agrias-beowulf-success.txt`

Controlled setup:

- Runtime profile: `work/battle-runtime-settings.preclamp-plan-immediate-basic-demo.json`
- Data mod disabled.
- Test action: Agrias basic Attack into Beowulf.
- Formula: `max(1, a.pa * 10 - t.faith)`

Observed player-facing result:

- Preview damage: `151`
- Floating damage UI: `45`
- Beowulf HP: `314 -> 269`
- No critical, reaction, evade, block, parry, or status effect reported.

Log-confirmed runtime path:

- Immediate source resolver selected Agrias: `source=immediate-action`, `id=0x1E`.
- Native pre-clamp target was Beowulf: `id=0x1F`.
- Formula runtime produced `final=45`.
- Native staged debit was rewritten from `oldDebit=151` to `forcedDebit=45`.
- HP event confirmed `314 -> 269 = 45`.

Durable implication:

- For pre-clamp rewrites, the resolved floating damage number and final HP follow the rewritten
  staged debit, while the forecast preview can remain vanilla/placeholder unless a preview-specific
  hook rewrites it.
