# LT25 pre-selector hook launch check

## Result

The LT25 observe-only profile loaded through Reloaded-II and activated its hook:

```text
[DCL-REACTION-PRESELECT-HOOK] rva=0x2063A9 addr=0x1402063A9 maxLogs=128 expected=48 8D 4D D2 E8 86 CA 07 00 (observe-only)
```

The runtime settings line confirmed that the pre-selector probe was enabled while the commit probe
and action replacement were disabled. The game remained responsive and exited cleanly through
`CloseMainWindow`. No menu input, save load, battle, or candidate event occurred.

## Interpretation boundary

- **Proven for this build:** the AOB guard passes and Reloaded's assembler accepts/activates the
  snapshot shim.
- **Not tested:** pre-selector fire cadence, candidate contents, source direction, forecast silence,
  or coexistence with status data.
- The full behavior protocol remains
  `work/1783994716-lt25-dcl-reaction-preselector-live-plan.md`.

After this launch-only check, the deployed settings were restored to LT23 and source/installed DLL
hashes matched `8222FB5A40911395D8C1EBC43583FBB5E58109B11CECA881AEAFB7130F7D8B54`.
