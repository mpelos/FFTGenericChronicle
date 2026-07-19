# DCL movement convergence read-only live gate

## Purpose

Validate `0xD575143` as the complete per-tile route boundary after the earlier arrival-writer probe
captured cursors `2..N` but omitted cursor `1`.

The trace updater reaches this instruction only after actor movement state `+0x8B` is zero. It then
reads route length `+0xA8`, compares cursor `+0xA4`, and either consumes the next byte or finalizes
the route. The probe is read-only and changes no battle, coordinate, route, status, HP, MP, action,
data, or save state.

## Controlled sequence

1. Load Manual Save `05` through the standard runbook path.
2. Allow one AI unit to finish a multi-tile route.
3. Move Ramza along a multi-tile route.
4. Close the game without saving.

## Pass conditions

- The hook installs with the expected bytes and the game remains stable.
- Each route begins with exactly one idle initialization event at cursor `0`.
- Each completed tile then emits cursors `1..length` without gaps or duplicates.
- Successive current X/Y positions differ by exactly one cardinal tile.
- Arrival current X/Y/layer equals the completed target X/Y/layer.
- The final event has cursor equal to route length.

Any missing cursor, repeated cursor, non-idle event, discontinuous tile, or actor-read failure returns
the investigation to offline mapping.
