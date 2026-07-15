# LT35 AI-scoring live result

## Question

Does protected enemy utility consume the normalized per-target staged bundle after `0x281F12`, or
does it rank targets from an earlier aggregate that the DCL cannot safely rewrite?

## Controlled fixture

- Exact autosave snapshot: `work/1784087502-fft-autoenhanced-snapshot.png`
  (`SHA256 73DB59E2250507414FB61B5DE54EBA65DE009DA9122E0CB7271B9E3797D7A5B4`).
- Ramza began at 569/569 HP and Rion at 26/277 HP. Both were visible, in range, and evaluated by the
  first enemy (`turnOwner=3`, `sourceIdx=3`) for the same action (`abilityId=265`).
- Baseline and forced runs restored the same snapshot. The forced profile changed only Ramza's
  normalized post-calc bundle to kind 0, HP debit 4095, result flag `0x80`.

## Evidence

Baseline log: `work/1784087707-lt35-ai-score-visible-dual-target-baseline-live.log`
(`SHA256 FD7AAD6F48BD1769E6399AC927DBB1E06A9F6DF2E3425F3B035075BA10ABBF5D`).

- Candidate 16 / Ramza: `stagedDmg=122`, `resFlag=0x80`.
- Candidate 17 / Rion: `stagedDmg=79`, `resFlag=0x80`.
- Forecast and state-`0x2A` execution both selected target 17 / Rion.

Forced log: `work/1784088085-lt35-ai-score-visible-dual-target-forced-live.log`
(`SHA256 D1DA6FC03D9F71EE33D099C4F301187EB5B0E92B5861E9B77777088BBA3EC6D9`).

- Hook declaration: `forceChar=1 kind=0 dmg=4095 resFlag=128`.
- Candidate 16 / Ramza: `stagedDmg=4095`, `resFlag=0x80`.
- Candidate 17 / Rion remained `stagedDmg=79`, `resFlag=0x80`.
- Forecast and state-`0x2A` execution both selected target 16 / Ramza.
- Native delivery reduced Ramza from 569 HP to zero, confirming the selected forced bundle reached
  execution as well as AI choice.

The patched probe logs the consumer-visible bundle after optional mutation. The code-mod build,
smoke suite, and forced-profile validator passed before deployment.

## Conclusion

**PROVEN:** the normalized staged bundle exposed after `0x281F12` participates directly in enemy
target ranking. The causal A/B changed only Ramza's candidate bundle and switched the same enemy's
forecast and execution target from Rion to Ramza while both candidates remained legal.

This closes the indispensable LT35 gate. The permanent DCL compute-point writer belongs at this
normalized post-calc boundary and must publish one cached action/target result for AI scoring,
forecast, and execution. The comparison proves numeric target ranking; action selection across
different abilities, healing/status utility weights, and move planning still require representative
regression rather than inference.

## Cleanup

- FFT and Reloaded-II were closed without saving.
- The decisive autosave snapshot was restored and its hash reverified.
- Installed runtime settings were restored from the stable backup and reverified as
  `SHA256 BD6857DC2219BAAC3A9769C5F4C040B1F762081FAE66AA192D8B8755964CC624`.
- The patched probe DLL remains deployed because no verified pre-test DLL backup exists; it changes
  LT35 logging order and does not enable the force profile under stable settings.
