# Implementation notes

## Isolation

The new code lives entirely under `Pmdg737Interface/`.

No existing PMDG 737 generic profile is included or changed in this package.

## ClientData

The native data area is `PMDG_NG3_Data`.

The interop struct is intentionally a minimal `LayoutKind.Explicit` view with an explicit native size instead of a copy of the full PMDG SDK header. Only fields used by the initial monitoring layer are declared.

## Door events prepared but inactive by default

Prepared event offsets:

- FWD L: 14005
- FWD R: 14006
- AFT L: 14007
- AFT R: 14008
- overwing L/R: 14009/14010
- cargo FWD/AFT: 14013/14014
- cargo main: 14015
- equipment hatch: 14016
- airstair: 14017

The event code is converted to a SimConnect custom event using base `69632`.

These writes are guarded by `EnableExperimentalWrites=false`.

## Why door states are not treated like the 777

The 777 exposes a multi-state door array with explicit OPEN/CLOSED/ARMED/OPENING/CLOSING values.

The NG3 data used here exposes individual door annunciator booleans. The foundation therefore only reports an `open indicated / not open indicated` condition and deliberately avoids inventing an opening/closing state.

## Next phase after runtime validation

After read-only validation:

1. test one controlled L1 toggle;
2. test L2/rear-stair behavior;
3. validate GSX passenger-door slot mapping;
4. enable jetway/stair callbacks selectively;
5. validate cargo loader mapping;
6. only then advertise plugin door capabilities in the manifest;
7. create a copy of the Ryanair profile that uses `PluginId=PMDG.B737`.

The current Ryanair profile remains the fallback throughout.
