using Any2GSX.PluginInterface.Interfaces;
using CFIT.AppLogger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Pmdg737Interface
{
    public class Pmdg737DoorManager(Pmdg737Aircraft aircraft)
    {
        public const string VarFwdL = "L:FwdLeftCabinDoor";
        public const string VarAftL = "L:AftLeftCabinDoor";
        public const string VarFwdR = "L:FwdRightCabinDoor";
        public const string VarAftR = "L:AftRightCabinDoor";
        public const string VarCargoFwd = "L:FwdLwrCargoDoor";
        public const string VarCargoAft = "L:AftLwrCargoDoor";
        public const string VarEquipment = "L:EEDoor";
        public const string VarAirstair = "L:AirStairs";

        public virtual Pmdg737Aircraft Aircraft { get; } = aircraft;

        protected virtual SemaphoreSlim PassengerRouteLock { get; } = new(1, 1);
        protected virtual bool ClosingAll { get; set; } = false;

        public virtual Dictionary<Pmdg737DoorId, Pmdg737Door> Doors { get; } = new()
        {
            { Pmdg737DoorId.FwdL, new(aircraft, Pmdg737DoorId.FwdL, Pmdg737Sdk.EventCode.DoorFwdL, VarFwdL) },
            { Pmdg737DoorId.FwdR, new(aircraft, Pmdg737DoorId.FwdR, Pmdg737Sdk.EventCode.DoorFwdR, VarFwdR) },
            { Pmdg737DoorId.AftL, new(aircraft, Pmdg737DoorId.AftL, Pmdg737Sdk.EventCode.DoorAftL, VarAftL) },
            { Pmdg737DoorId.AftR, new(aircraft, Pmdg737DoorId.AftR, Pmdg737Sdk.EventCode.DoorAftR, VarAftR) },
            { Pmdg737DoorId.OverwingL, new(aircraft, Pmdg737DoorId.OverwingL, Pmdg737Sdk.EventCode.DoorOverwingL) },
            { Pmdg737DoorId.OverwingR, new(aircraft, Pmdg737DoorId.OverwingR, Pmdg737Sdk.EventCode.DoorOverwingR) },
            { Pmdg737DoorId.CargoFwd, new(aircraft, Pmdg737DoorId.CargoFwd, Pmdg737Sdk.EventCode.DoorCargoFwd, VarCargoFwd) },
            { Pmdg737DoorId.CargoAft, new(aircraft, Pmdg737DoorId.CargoAft, Pmdg737Sdk.EventCode.DoorCargoAft, VarCargoAft) },
            { Pmdg737DoorId.CargoMain, new(aircraft, Pmdg737DoorId.CargoMain, Pmdg737Sdk.EventCode.DoorCargoMain) },
            { Pmdg737DoorId.Equipment, new(aircraft, Pmdg737DoorId.Equipment, Pmdg737Sdk.EventCode.DoorEquipment, VarEquipment) },
            { Pmdg737DoorId.Airstair, new(aircraft, Pmdg737DoorId.Airstair, Pmdg737Sdk.EventCode.DoorAirstair, VarAirstair) },
        };

        public virtual Dictionary<GsxVehicleStair, GsxVehicleStairState> StairStates { get; } = new()
        {
            { GsxVehicleStair.Front, GsxVehicleStairState.Unknown },
            { GsxVehicleStair.Middle, GsxVehicleStairState.Unknown },
            { GsxVehicleStair.Rear, GsxVehicleStairState.Unknown },
        };

        public virtual IEnumerable<string> ProgressVariables =>
            Doors.Values
                .Select(d => d.ProgressVariable)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.InvariantCultureIgnoreCase);

        public virtual bool HasOpenDoors()
        {
            return Doors.Values.Any(d => d.IsOpenIndicated || d.IsMoving || d.IsMostlyOpen);
        }

        public virtual Pmdg737Door GetDoor(GsxDoor door)
        {
            return door switch
            {
                GsxDoor.PaxDoor1 => Doors[Pmdg737DoorId.FwdL],
                GsxDoor.PaxDoor2 => Doors[Pmdg737DoorId.AftL],
                GsxDoor.PaxDoor4 => Doors[Pmdg737DoorId.AftL],
                GsxDoor.ServiceDoor1 => Doors[Pmdg737DoorId.FwdR],
                GsxDoor.ServiceDoor2 => Doors[Pmdg737DoorId.AftR],
                GsxDoor.CargoDoor1 => Doors[Pmdg737DoorId.CargoFwd],
                GsxDoor.CargoDoor2 => Doors[Pmdg737DoorId.CargoAft],
                GsxDoor.CargoDoor3Main => Doors[Pmdg737DoorId.CargoMain],
                _ => null,
            };
        }

        public virtual bool PassengerRouteAllowed()
        {
            return Aircraft.CurrentAutomationState == AutomationState.SessionStart
                || Aircraft.CurrentAutomationState == AutomationState.Preparation
                || Aircraft.CurrentAutomationState == AutomationState.Departure
                || Aircraft.CurrentAutomationState == AutomationState.Arrival
                || Aircraft.CurrentAutomationState == AutomationState.TurnAround;
        }

        public virtual bool IsJetwayActive()
        {
            return Aircraft.GsxController?.JetwayState == GsxServiceState.Active;
        }

        protected virtual bool RearStairIsApproachingOrConnected()
        {
            GsxVehicleStairState state = StairStates[GsxVehicleStair.Rear];
            return state == GsxVehicleStairState.Approaching
                || state == GsxVehicleStairState.InPosition
                || state == GsxVehicleStairState.Extending
                || state == GsxVehicleStairState.WaitingDoor;
        }

        protected virtual bool FrontStairIsApproachingOrConnected()
        {
            GsxVehicleStairState state = StairStates[GsxVehicleStair.Front];
            return state == GsxVehicleStairState.Approaching
                || state == GsxVehicleStairState.InPosition
                || state == GsxVehicleStairState.Extending
                || state == GsxVehicleStairState.WaitingDoor;
        }

        public virtual async Task<bool> SetAirstair(bool extended)
        {
            if (!Aircraft.IntegratedAirstairAuto)
                return !extended || Doors[Pmdg737DoorId.Airstair].IsMostlyClosed;

            Pmdg737Door airstair = Doors[Pmdg737DoorId.Airstair];
            if ((extended && airstair.IsMostlyOpen) || (!extended && airstair.IsMostlyClosed))
                return true;

            // The existing Ryanair integration established the important PMDG rule:
            // 1L must be closed before the integrated airstair is moved.
            await Doors[Pmdg737DoorId.FwdL].SetOpen(false);
            await Task.Delay(350, Aircraft.Token);

            return await airstair.SetOpen(extended);
        }

        public virtual async Task PreparePassengerRoute(bool paxDoorAllowed = true)
        {
            if (!paxDoorAllowed
                || ClosingAll
                || !Aircraft.DoorAutomationEnabled
                || !PassengerRouteAllowed())
            {
                return;
            }

            try
            {
                await PassengerRouteLock.WaitAsync(Aircraft.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            try
            {
                bool plannedArrivalJetway =
                    !Aircraft.AlwaysStairsAtJetway
                    && Aircraft.CurrentAutomationState == AutomationState.Arrival
                    && Aircraft.GsxController?.HasGateJetway == true
                    && Aircraft.ISettingProfile.CallJetwayStairsOnArrival;

                bool useJetway = IsJetwayActive() || plannedArrivalJetway;

                if (useJetway)
                {
                    if (Aircraft.JetwayAutoRetractAirstair)
                        await SetAirstair(false);

                    await Doors[Pmdg737DoorId.FwdL].SetOpen(true);
                }
                else
                {
                    if (Aircraft.IntegratedAirstairAuto)
                    {
                        await SetAirstair(true);
                        await Doors[Pmdg737DoorId.FwdL].SetOpen(true);
                    }
                    else if (FrontStairIsApproachingOrConnected()
                        || Aircraft.GsxController?.StairsState == GsxServiceState.Requested
                        || Aircraft.GsxController?.StairsState == GsxServiceState.Active)
                    {
                        await Doors[Pmdg737DoorId.FwdL].SetOpen(true);
                    }
                }

                if (Aircraft.RearDoorAuto)
                    await EnsureRearDoorOpen("prepare-passenger-route");
            }
            finally
            {
                PassengerRouteLock.Release();
            }
        }

        public virtual async Task<bool> EnsureRearDoorOpen(string reason)
        {
            if (ClosingAll
                || !Aircraft.DoorAutomationEnabled
                || !Aircraft.RearDoorAuto
                || !PassengerRouteAllowed())
            {
                return false;
            }

            Pmdg737Door rear = Doors[Pmdg737DoorId.AftL];
            double before = rear.Progress;
            bool result = await rear.SetOpen(true);

            if (Aircraft.DiagnosticsRearStair)
            {
                Logger.Information(
                    $"PMDG 737 2L ensure-open [{reason}] " +
                    $"before={before:0.0} after={rear.Progress:0.0} result={result}");
            }

            return result;
        }

        public virtual async Task CloseAllKnownDoors()
        {
            if (ClosingAll || !Aircraft.ExperimentalWritesEnabled)
                return;

            ClosingAll = true;
            try
            {
                Logger.Information("PMDG 737 native: closing known doors for departure");

                // Match the proven Ryanair ordering: passenger/service doors first.
                await Doors[Pmdg737DoorId.AftL].SetOpen(false);
                await Doors[Pmdg737DoorId.FwdR].SetOpen(false);
                await Doors[Pmdg737DoorId.AftR].SetOpen(false);
                await Doors[Pmdg737DoorId.FwdL].SetOpen(false);

                await Doors[Pmdg737DoorId.CargoFwd].SetOpen(false);
                await Doors[Pmdg737DoorId.CargoAft].SetOpen(false);
                await Doors[Pmdg737DoorId.Equipment].SetOpen(false);

                // The NG3 SDK exposes a cargo-main event but no matching annunciator
                // in the published data structure. Do not guess its state or toggle it
                // blindly here. Cargo-main support remains event-ready for future proofing.

                await SetAirstair(false);
            }
            finally
            {
                ClosingAll = false;
            }
        }

        public virtual async Task SetCargoDoors(bool open, bool force = false)
        {
            if (!force && !Aircraft.CargoDoorsAuto)
                return;

            await Doors[Pmdg737DoorId.CargoFwd].SetOpen(open);
            await Doors[Pmdg737DoorId.CargoAft].SetOpen(open);

            // Do not operate the freighter main cargo door blindly: unlike FWD/AFT
            // cargo doors the published NG3 data does not provide a main-door state.
        }

        public virtual async Task OnDoorTrigger(GsxDoor door, bool trigger)
        {
            if (ClosingAll || !Aircraft.DoorAutomationEnabled || !trigger)
                return;

            Pmdg737Door mapped = GetDoor(door);
            if (mapped == null || mapped.IsMoving)
                return;

            if (door == GsxDoor.CargoDoor3Main)
            {
                if (Aircraft.StateLoggingEnabled)
                {
                    Logger.Warning(
                        "PMDG 737 cargo-main trigger ignored: NG3 ClientData does not expose " +
                        "a reliable cargo-main state for target-safe toggle control.");
                }
                return;
            }

            // GSX *_TOGGLE variables are pulse triggers. Mirror the established
            // PMDG 777 implementation: act only on the positive pulse and derive
            // the requested target from the current aircraft state.
            bool targetOpen = mapped.IsMostlyClosed;

            if (door == GsxDoor.PaxDoor1 && targetOpen
                && Aircraft.IntegratedAirstairAuto
                && !IsJetwayActive())
            {
                await SetAirstair(true);
            }

            await mapped.SetOpen(targetOpen);
        }

        public virtual Task OnLoaderAttached(GsxDoor door, bool attached)
        {
            if (!Aircraft.CargoDoorsAuto)
                return Task.CompletedTask;

            if (door == GsxDoor.CargoDoor3Main)
                return Task.CompletedTask;

            Pmdg737Door mapped = GetDoor(door);
            return mapped?.SetOpen(attached) ?? Task.CompletedTask;
        }

        public virtual async Task OnJetwayStateChange(GsxServiceState state, bool paxDoorAllowed)
        {
            if (!paxDoorAllowed || ClosingAll || !Aircraft.DoorAutomationEnabled)
                return;

            if (state == GsxServiceState.Active)
            {
                if (Aircraft.AlwaysStairsAtJetway && Aircraft.StateLoggingEnabled)
                {
                    Logger.Information(
                        "PMDG 737 native: jetway physically active; overriding stairs-at-jetway preference for safety");
                }

                if (Aircraft.JetwayAutoRetractAirstair)
                    await SetAirstair(false);

                await Doors[Pmdg737DoorId.FwdL].SetOpen(true);
            }
            // Intentionally do not force-close 1L on transitional GSX jetway states.
            // Final-loadsheet / pushback door closure owns that boundary.
        }

        public virtual Task OnStairStateChange(GsxServiceState state, bool paxDoorAllowed)
        {
            if (!paxDoorAllowed || ClosingAll)
                return Task.CompletedTask;

            if (state == GsxServiceState.Requested || state == GsxServiceState.Active)
                return PreparePassengerRoute(true);

            return Task.CompletedTask;
        }

        public virtual Task OnStairOperationChange(GsxServiceState state, bool paxDoorAllowed)
        {
            if (!paxDoorAllowed || ClosingAll)
                return Task.CompletedTask;

            if (state == GsxServiceState.Requested || state == GsxServiceState.Active)
                return PreparePassengerRoute(true);

            return Task.CompletedTask;
        }

        public virtual async Task OnStairVehicleChange(
            GsxVehicleStair stair,
            GsxVehicleStairState state,
            bool paxDoorAllowed)
        {
            StairStates[stair] = state;

            if (Aircraft.StateLoggingEnabled)
                Logger.Debug($"PMDG 737 stair monitor: {stair} -> {state}");

            if (!paxDoorAllowed || ClosingAll || !PassengerRouteAllowed())
                return;

            if (stair == GsxVehicleStair.Rear && RearStairIsApproachingOrConnected())
            {
                await EnsureRearDoorOpen($"rear-stair-state-{state}");

                if (state == GsxVehicleStairState.InPosition
                    && Aircraft.DiagnosticsRearStair
                    && !Doors[Pmdg737DoorId.AftL].IsMostlyOpen)
                {
                    Logger.Warning(
                        $"PMDG 737 rear stair InPosition while 2L is not fully open " +
                        $"(progress={Doors[Pmdg737DoorId.AftL].Progress:0.0})");
                }
            }
            else if (stair == GsxVehicleStair.Front
                && !Aircraft.IntegratedAirstairAuto
                && FrontStairIsApproachingOrConnected())
            {
                await Doors[Pmdg737DoorId.FwdL].SetOpen(true);
            }
        }
    }
}
