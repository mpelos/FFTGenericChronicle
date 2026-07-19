# DCL Interrupt hook-install failure

## Live observation

The first Test A attempt used the Interrupt-only log profile and executed Rion's Potion on
Josephine while her Death action remained visible in timeline slot 7. The runtime recorded the
execution calculation as `ability=368`, `type=0x06`, `target=0x81`, `outcome=hit`, but emitted no
`DCL-INTERRUPT` line. The startup log also contained no outer-producer hook activation line.

Raw log: `work/1784269211-dcl-interrupt-test-a-hook-install-failure.log`.

## Root cause

`InstallStagedBundleProbeIfEnabled` correctly classified an enabled Interrupt rule as an
`interruptProducer`, built the shared outer-sweep callback, and then used this final guard:

```csharp
if (!bundleProbe && !numericWriter && !statusProducer)
    return;
```

That guard omitted `interruptProducer`. With Interrupt as the only outer-sweep consumer, startup
returned before `CreateAsmHook`, so the managed Interrupt callback could never execute.

## Correction

The guard now calls `ShouldInstallStagedBundleHook`, which retains the hook when any one of the four
consumers is active: bundle probe, numeric writer, status producer, or Interrupt producer. The
Interrupt smoketest now owns both the positive Interrupt-only case and the all-disabled negative
case.

## Next validation

Build and run the offline gates, deploy the corrected DLL while FFT is stopped, restore the immutable
pending-action snapshot, and repeat Test A. The repeated run must show the outer hook at startup and
exactly one `outcome=eligible-log-only` transaction before proceeding to the live-write branches.
