# PMDG B737 native plugin — v0.2.0 beta

Native PMDG 737 NG3 integration for Any2GSX.

This project deliberately leaves the existing `Pmdg737/PMDG.737-Profile.json`
and any installed `PMDG.738.RYR` Lua profile untouched. They remain fallback options.

## Architecture

The plugin follows the same separation used by the native PMDG 777 integration:

- `Pmdg737Module` — PMDG NG3 ClientData transport.
- `Pmdg737Aircraft` — Any2GSX aircraft interface and state mapping.
- `Pmdg737DoorManager` — GSX door/stair/jetway/loader orchestration.
- `Pmdg737Door` — serialized target-safe PMDG door event handling.
- `Profiles/PMDG.737.Native-Profile-IMPORT.json` — general PMDG 737 profile.
- `Profiles/PMDG.738.Ryanair.Native-Profile-IMPORT.json` — Ryanair-specific SOP profile.

The aircraft plugin owns PMDG-specific mechanics. Profiles own service/SOP choices.

## Prerequisite

In `737NG3_Options.ini`:

```ini
[SDK]
EnableDataBroadcast=1
```

Fully restart MSFS after changing this option.

## v0.2 door/state integration

Implemented:

- PMDG NG3 ClientData model detection and state monitoring.
- Parking brake, power buses, ground-power availability, APU bleed, position lights and beacon.
- PMDG L1 / R1 / L2 / R2 target-safe door control.
- Forward/aft lower cargo doors.
- Equipment hatch closure during departure cleanup.
- PMDG integrated 1L airstair.
- GSX door trigger callbacks.
- GSX cargo-loader callbacks.
- GSX jetway callbacks.
- GSX stair service and operation callbacks.
- Per-vehicle front/rear stair callbacks.
- Serialized door writes to prevent duplicate PMDG toggle events.
- Door progress guard using the same PMDG LVars proven by the existing Ryanair Lua integration.
- Pushback boundary that prevents stale rear-stair callbacks from reopening 2L.
- Ryanair mode: integrated front airstair + rear GSX stair, including jetway-stand preference.
- Active-jetway safety: integrated airstair retracts before 1L is used with the jetway.

## Important beta limitation

The published NG3 ClientData structure exposes a custom event for the freighter main
cargo door, but no corresponding cargo-main state/annunciator. v0.2 therefore does
**not** blindly toggle that door. Forward/aft lower cargo doors are fully handled.

Fuel/payload synchronization is also intentionally not advertised yet; this version
targets the aircraft-state, door, stair, jetway and loader integration first.

## Profiles

### PMDG.737.Native

General PMDG 737 profile:

- native aircraft plugin;
- normal jetway behavior;
- native passenger/service/cargo door handling;
- native rear-stair/2L synchronization;
- no forced Ryanair stairs-at-jetway behavior.

### PMDG.738.Ryanair.Native

Based directly on the established `PMDG.738.Ryanair.V3` service flow.

It keeps the proven Ryanair choices while moving aircraft mechanics into the native plugin:

- PMDG integrated front airstair;
- GSX rear stair with 2L synchronization;
- stairs preferred at jetway-equipped stands;
- jetway physical-connection safety override;
- pushback-safe closure boundary;
- existing V3 departure-services / tug logic retained.

## Validation matrix before calling 1.0 stable

1. 737-800 cold & dark, remote stand.
2. 737-800 turnaround, remote stand.
3. 737-800 jetway stand.
4. L1/L2 rear boarding.
5. R1/R2 catering/service-door callbacks.
6. forward/aft cargo loader attach/detach.
7. final-loadsheet close-all.
8. automatic pushback with tug already attached.
9. arrival/deboarding and turnaround.
10. at least one additional PMDG 737 passenger variant.

A successful compile is not a substitute for those runtime checks.
