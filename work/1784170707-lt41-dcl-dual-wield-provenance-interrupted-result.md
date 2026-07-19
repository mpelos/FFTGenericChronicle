# LT41 DCL Dual Wield provenance — interrupted result

## Scope

LT41 was intended to correlate the two native Dual Wield strike calculations with the managed
pre-clamp callback. The game reached the battle and completed the distant Wenyld attack, but its
visual window then disappeared while `FFT_enhanced` remained alive without a window title. The
process produced no further runtime rows and was terminated only after two independent Computer
Use refreshes could not recover a controllable window.

## Evidence retained

- Raw capture: `work/1784170642-lt41-dcl-dual-wield-provenance-interrupted-live.log`
- SHA-256: `4FB2BEF725DDFF2FB3D3AB0EB6C30B50468124E24FDDFF1C0E634967967EAE3B`
- Size: `25,668` bytes
- Startup installed every requested DCL/provenance hook without a failure row.
- The confirmed Wenyld attack is `outer-sweep`, battle state `0x2A`, payload `89`, and the managed
  pre-clamp pipeline rewrites its native `108` HP debit to `1`.
- Its staged synthetic `442` request is rejected at `typed-family` with result `-2`, as expected for
  the distant geometry. No Choco Beak or Dual Wield transaction appears in the file.

## Interpretation boundary

This capture says nothing about the second Dual Wield strike. It neither confirms nor refutes any
action-context-cache hypothesis. LT40 remains the only live evidence for that gap until a bounded
two-strike capture reaches the required transaction.

The replacement gate is `work/1784171179-lt41b-dcl-dual-wield-fast-provenance-live-plan.md`. It uses
an audited CT-order fixture to remove the distant Wenyld turn from the intended path.

## Restoration

After archiving the raw log, the deployed DLL, PDB, runtime settings, Reloaded AppConfig, prior
battle log, and prior autosave were restored from the pre-test backup. Every restored destination
matched its recorded pre-test SHA-256 hash.
