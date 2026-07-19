# DCL movement convergence zero-hit checkpoint

The read-only probe at trace-updater convergence `0xD575143` installed with the expected bytes, the
game remained stable, several AI units moved, and Ramza completed a long manual route. The hook
emitted zero hits. The captured battle therefore did not execute this trace copy of the movement
updater.

The live log and its strict zero-event failure report are:

- `1784338189-dcl-movement-convergence-live.log`
- `1784338189-dcl-movement-convergence-live-analysis.md`

Offline control-flow evidence already identifies the equivalent native idle-only boundary at
`0x1FE793`. It follows the native state-zero check at `0x1FE786` and reads route length `actor+0xA8`
before comparing and consuming the next route byte. The next gate observes both `0x1FE793` and
`0xD575143`; this distinguishes the active implementation without guessing and preserves coverage
if different movement classes use different copies.
