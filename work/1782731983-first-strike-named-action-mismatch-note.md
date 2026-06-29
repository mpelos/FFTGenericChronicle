# First Strike named-action capture mismatch

## Manual result

The intended live test was Cloud using a basic attack into Ninja with First Strike equipped. The reported outcome was that First Strike fired before the incoming attack, Cloud took two UI damage numbers, Cloud ended at 0 HP, and Ninja did not take damage.

## Log evidence

The stabilized log is `work/1782731913-first-strike-named-action-log-stabilized.txt`; the generated analyzer report is `work/1782731913-first-strike-named-action-report-stabilized.md`.

That log does not match the intended manual result:

- The final HP event targets `0x141855CE0/id=0x80`, whose max HP is 277; this matches Ninja-like HP, not Cloud.
- The HP event applies 182 damage: `277 -> 95`.
- The resolved caster/source is `0x141855EE0/id=0x32`, the Cloud-like unit with HP around 428.
- The resolved action id is `257`, identified by the analyzer as Braver.
- Target-cache register refs do show the incoming source unit (`id=0x32`) while target is `id=0x80`, but the refs are direct unit refs and not named actor refs at the target-cache hook.

## Interpretation

Do not use this capture as canonical First Strike proof. It is useful only as a mismatch sample showing that the log stream later captured a Cloud-to-Ninja Braver-like action, while the intended test/result was a First Strike interruption of a basic attack.

The next repeat should start from a fresh process/log, avoid any queued Cloud Limit, execute exactly one basic Attack into Ninja, and stop immediately after the reaction/result without taking another action.
