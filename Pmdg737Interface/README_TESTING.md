# PMDG B737 native plugin foundation

This folder is intended to be added to the repository root as:

`Pmdg737Interface/`

It does **not** replace or modify the existing `Pmdg737/PMDG.737-Profile.json`.

## Safety model

Version `0.1.0` is deliberately conservative:

- PMDG NG3 ClientData monitoring is active.
- Model/variant detection is active.
- Parking-brake, electrical, APU, light and door-annunciator monitoring is active.
- GSX stair-state monitoring is active.
- Door/loader/jetway/stair callback interfaces are implemented.
- `EnableExperimentalWrites` defaults to **false**.
- The manifest advertises `ManualNone` for doors, fuel, payload and ground equipment.
- The default native profile has `RunAutomationService=false`.

Therefore installing/building this foundation should not start native PMDG door automation by itself.

## Prerequisite

In `737NG3_Options.ini`:

```ini
[SDK]
EnableDataBroadcast=1
```

Fully restart MSFS after changing the option.

## First runtime validation

Keep experimental writes OFF.

Load the PMDG 737 and inspect the Any2GSX log. A successful connection should produce a line similar to:

```text
Receiving PMDG 737 NG3 ClientData - model 5 (737-800)
```

and then a monitor snapshot containing:

```text
PB=
GndAvail=
GndSw=
APUsel=
APUegt=
APUbleed=
POS=
BCN=
L1=
R1=
L2=
R2=
CgoF=
CgoA=
Airstair=
```

Validate in this order:

1. parking brake set/released;
2. beacon on/off;
3. position lights;
4. ground power available/selected;
5. L1 manually open/close;
6. L2 manually open/close;
7. R1/R2 service doors;
8. forward/aft cargo doors;
9. front/rear GSX stair states.

Only after these values are proven should `EnableExperimentalWrites` be enabled for controlled door-event tests.

## Build note

This project follows the current `Pmdg777Interface` target/package versions and references the existing SimConnect DLL from that sibling project.

A real .NET build has **not** been executed in the ChatGPT environment because the required .NET/Windows/SimConnect toolchain is unavailable there. JSON/XML/static checks were run when this package was generated.
