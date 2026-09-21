# Implementation notes — v0.2.0 beta

## Isolation and fallbacks

All native code remains under `Pmdg737Interface/`.

The existing repository files under `Pmdg737/` are intentionally not modified.
An installed `PMDG.738.RYR` Lua plugin/profile is also not replaced. This gives
users a clean fallback while the native plugin is beta-tested.

## ClientData

The native PMDG data area is `PMDG_NG3_Data`.

`Pmdg737Sdk.cs` keeps a minimal `LayoutKind.Explicit` view with native size 916
bytes. It declares only fields actually used by this plugin. The PMDG SDK header
itself is not redistributed.

## Door events

PMDG NG3 custom shortcut event offsets used:

- FWD L: 14005
- FWD R: 14006
- AFT L: 14007
- AFT R: 14008
- overwing L/R: 14009/14010
- cargo FWD/AFT: 14013/14014
- cargo main: 14015
- equipment hatch: 14016
- airstair: 14017

All are based at event ID 69632 and use the PMDG left-single parameter.

## Target-safe toggle strategy

PMDG door custom events are toggles. Sending the same event twice while GSX emits
overlapping callbacks can undo the first command.

v0.2 therefore serializes each door independently with `SemaphoreSlim`, checks
the current target state before writing, and waits for a stable target after one
event. The established PMDG door LVars from the existing Ryanair Lua plugin are
used only as progress guards (0..100) so moving doors are not reversed.

The authoritative aircraft status still comes from NG3 ClientData where the SDK
provides it.

## GSX callbacks

The native layer implements:

- `OnDoorTrigger`
- `OnLoaderAttached`
- `OnJetwayStateChange`
- `OnStairStateChange`
- `OnStairOperationChange`
- `OnStairVehicleChange`
- `DoorsAllClose`
- `SetCargoDoors`
- `OnAutomationStateChange`

GSX `*_TOGGLE` door variables are treated as pulse triggers, matching the PMDG
777 plugin: only the positive pulse is acted on, and the target is derived from
the current aircraft state.

## Ryanair logic migrated from PMDG.738.RYR v0.2.5

The native Ryanair profile retains the service/SOP choices while aircraft-specific
mechanics move into C#:

- integrated 1L airstair;
- rear GSX stair -> 2L;
- serialized rear-door commands;
- stairs-at-jetway preference;
- physical jetway safety override;
- late stair callbacks blocked after Departure;
- final/pushback door closure;
- existing V3 service/tug configuration.

## Deliberate limitation

The NG3 SDK exposes `EVT_DOOR_CARGO_MAIN`, but the published ClientData structure
does not expose a corresponding cargo-main state/annunciator. A toggle without a
known current state is not target-safe, so v0.2 does not automatically operate
the freighter main cargo door.

## Release state

This is feature-complete for the intended v0.2 door/stair/jetway scope, but still
beta until the runtime validation matrix in `README_TESTING.md` has been executed.

