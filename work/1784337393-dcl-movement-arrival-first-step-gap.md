# DCL movement arrival-writer probe correction

The read-only live log `1784336577-dcl-movement-route-live.log` contains 16 arrival-writer hits.
Three are zero-length terminal same-tile echoes. The 13 real transitions cover three actors, but
every actor starts at route cursor `2`; no route contains cursor `1`.

The corrected analyzer therefore reports **FAIL**, not PASS. Reaching route length at the last event
does not prove completeness when the first captured cursor is missing.

Offline control-flow analysis shows that the trace updater converges at `0xD575143` only after
movement state is zero and before the next route-byte decision. That boundary structurally includes
the initial cursor `0`, every completed arrival cursor `1..N`, and the final cursor/length equality.
The next live gate observes that convergence point instead of another coordinate writer.
