using Any2GSX.PluginInterface;
using Any2GSX.PluginInterface.Interfaces;
using CFIT.AppLogger;
using CFIT.SimConnectLib.SimVars;
using System;
using System.Threading.Tasks;

namespace Pmdg737Interface
{
    public class Pmdg737Aircraft : AircraftBase
    {
        public override bool IsConnected => ReceivedDataValid;
        public virtual bool ReceivedDataValid => Module?.ReceivedDataValid == true;

        public virtual Pmdg737Plugin Plugin => AppPlugin.Instance as Pmdg737Plugin;
        public virtual Pmdg737Module Module => Plugin?.SimConnectModule as Pmdg737Module;
        public virtual PMDG_NG3_Data Data => Module?.Data ?? new();

        public virtual Pmdg737DoorManager DoorManager { get; }
        public virtual Pmdg737Diagnostics Diagnostics { get; }

        public virtual AutomationState CurrentAutomationState =>
            GsxController?.AutomationState ?? Any2GSX.PluginInterface.Interfaces.AutomationState.Unknown;

        // Kept under the original key for backward compatibility with v0.1 profiles.
        public virtual bool ExperimentalWritesEnabled => GetBoolSetting(
            Pmdg737Settings.EnableExperimentalWrites,
            false);

        public virtual bool StateLoggingEnabled => GetBoolSetting(
            Pmdg737Settings.StateLogging,
            true);

        public virtual bool DoorAutomationEnabled => GetBoolSetting(
            Pmdg737Settings.DoorAutomationEnabled,
            true);

        public virtual bool IntegratedAirstairAuto => GetBoolSetting(
            Pmdg737Settings.IntegratedAirstairAuto,
            false);

        public virtual bool RearDoorAuto => GetBoolSetting(
            Pmdg737Settings.RearDoorAuto,
            true);

        public virtual bool JetwayAutoRetractAirstair => GetBoolSetting(
            Pmdg737Settings.JetwayAutoRetractAirstair,
            true);

        public virtual bool CargoDoorsAuto => GetBoolSetting(
            Pmdg737Settings.CargoDoorsAuto,
            true);

        public virtual bool PushbackCloseDoors => GetBoolSetting(
            Pmdg737Settings.PushbackCloseDoors,
            true);

        public virtual bool AlwaysStairsAtJetway => GetBoolSetting(
            Pmdg737Settings.AlwaysStairsAtJetway,
            false);

        public virtual bool DiagnosticsRearStair => GetBoolSetting(
            Pmdg737Settings.DiagnosticsRearStair,
            true);

        public Pmdg737Aircraft(IAppResources appResources) : base(appResources)
        {
            RunIntervalMs = 1000;
            DoorManager = new(this);
            Diagnostics = new(this);
        }

        protected virtual bool GetBoolSetting(PluginSetting setting, bool fallback)
        {
            try
            {
                return ISettingProfile.GetSetting(setting);
            }
            catch
            {
                return fallback;
            }
        }

        protected override Task DoInit()
        {
            foreach (Pmdg737Door door in DoorManager.Doors.Values)
                SimStore.AddEvent(Pmdg737Sdk.GetEventName(door.EventCode));

            foreach (string variable in DoorManager.ProgressVariables)
                SimStore.AddVariable(variable, SimUnitType.Number);

            GsxController.GetService(GsxServiceType.Catering).OnStateChanged += OnCateringState;

            Logger.Information(
                ExperimentalWritesEnabled
                    ? "PMDG 737 native plugin initialized - native door control ENABLED"
                    : "PMDG 737 native plugin initialized - read-only/safe mode");

            return Task.CompletedTask;
        }

        protected override Task DoStop()
        {
            foreach (Pmdg737Door door in DoorManager.Doors.Values)
                SimStore.Remove(Pmdg737Sdk.GetEventName(door.EventCode));

            foreach (string variable in DoorManager.ProgressVariables)
                SimStore.Remove(variable);

            GsxController.GetService(GsxServiceType.Catering).OnStateChanged -= OnCateringState;

            return Task.CompletedTask;
        }

        public override Task CheckConnection()
        {
            return Task.CompletedTask;
        }

        public override Task RunInterval()
        {
            Diagnostics.LogIfChanged();
            return Task.CompletedTask;
        }

        public override Task<bool> GetSettingAdvAutomation()
        {
            // Same pattern as the PMDG 777 native plugin: aircraft-specific callbacks
            // are handled by this interface while Any2GSX owns the service state machine.
            return Task.FromResult(false);
        }

        public override async Task OnAutomationStateChange(AutomationState state)
        {
            if (StateLoggingEnabled)
                Logger.Debug($"PMDG 737 automation state -> {state}");

            if (state == AutomationState.Preparation
                || state == AutomationState.Arrival
                || state == AutomationState.TurnAround)
            {
                if (GsxController?.JetwayState == GsxServiceState.Active
                    && ISettingProfile.DoorPaxHandling)
                {
                    await DoorManager.OnJetwayStateChange(GsxServiceState.Active, true);
                }

                if (GsxController?.StairsState == GsxServiceState.Active
                    && ISettingProfile.DoorPaxHandling
                    && ISettingProfile.DoorStairHandling)
                {
                    await DoorManager.OnStairStateChange(GsxServiceState.Active, true);
                }
            }

            // Ported from the proven Ryanair V3 boundary: once Pushback starts,
            // stale stair callbacks are no longer allowed to reopen the passenger route.
            if (state == AutomationState.Pushback
                && PushbackCloseDoors
                && ExperimentalWritesEnabled)
            {
                await Task.Delay(300, Token);
                await DoorManager.CloseAllKnownDoors();
            }
        }


        protected virtual async Task OnCateringState(IGsxService cateringService)
        {
            if (!ISettingProfile.DoorServiceHandling
                || cateringService.IsRunning
                || !IsConnected
                || !ExperimentalWritesEnabled)
            {
                return;
            }

            await DoorManager.Doors[Pmdg737DoorId.FwdR].SetOpen(false);
            await DoorManager.Doors[Pmdg737DoorId.AftR].SetOpen(false);
        }

        public override async Task<bool> GetIsCargo()
        {
            if (IsConnected)
                return Pmdg737Model.IsCargo(Data.AircraftModel);

            return await base.GetIsCargo();
        }

        public override Task<DisplayUnit> GetAircraftUnits()
        {
            if (IsConnected)
                return Task.FromResult(Data.WeightInKg != 0 ? DisplayUnit.KG : DisplayUnit.LB);

            return base.GetAircraftUnits();
        }

        public override async Task<bool> GetAvionicPowered()
        {
            if (IsConnected)
            {
                return Data.ELEC_BusPowered_AC_TRANSFER_1 != 0
                    || Data.ELEC_BusPowered_AC_TRANSFER_2 != 0
                    || Data.ELEC_BusPowered_DC_BATT_BUS != 0;
            }

            return await base.GetAvionicPowered();
        }

        public override async Task<bool> GetExternalPowerAvailable()
        {
            if (IsConnected)
                return Data.ELEC_annunGRD_POWER_AVAILABLE != 0;

            return await base.GetExternalPowerAvailable();
        }

        public override Task<bool> GetExternalPowerConnected()
        {
            // The NG3 SDK exposes the physical/momentary GRD PWR switch but no proven
            // direct "connected" boolean. Keep the existing generic PMDG source here.
            return base.GetExternalPowerConnected();
        }

        public override async Task<bool> GetApuBleedOn()
        {
            if (IsConnected)
                return Data.AIR_APUBleedAirSwitch != 0 && await GetApuRunning();

            return await base.GetApuBleedOn();
        }

        public override async Task<bool> GetBrakeSet()
        {
            if (IsConnected)
                return Data.PED_annunParkingBrake != 0;

            return await base.GetBrakeSet();
        }

        public override async Task<bool> GetLightNav()
        {
            if (IsConnected)
            {
                // NG3 SDK: 0=STEADY, 1=OFF, 2=STROBE&STEADY.
                bool navOn = Data.LTS_PositionSw != 1;
                return navOn && await GetAvionicPowered();
            }

            return await base.GetLightNav();
        }

        public override async Task<bool> GetLightBeacon()
        {
            if (IsConnected)
                return Data.LTS_AntiCollisionSw != 0 && await GetAvionicPowered();

            return await base.GetLightBeacon();
        }

        public override Task<bool> GetHasOpenDoors()
        {
            return Task.FromResult(IsConnected && DoorManager.HasOpenDoors());
        }

        public override Task<bool> GetHasAirStairForward()
        {
            // Preserve the proven Ryanair behavior:
            // report an integrated front stair at jetway stands only when the profile
            // explicitly prefers stairs over the available jetway. This prevents the
            // automatic jetway call while keeping the GSX rear stair service usable.
            return Task.FromResult(
                IntegratedAirstairAuto
                && AlwaysStairsAtJetway
                && GsxController?.HasGateJetway == true);
        }

        public override Task<bool> GetHasAirStairAft()
        {
            return Task.FromResult(false);
        }

        public override Task<bool> GetHasFuelSync()
        {
            return Task.FromResult(false);
        }

        public override Task<bool> GetCanSetPayload()
        {
            return Task.FromResult(false);
        }

        public override Task<bool> GetHasFobSaveRestore()
        {
            return Task.FromResult(false);
        }

        public override Task SetCargoDoors(bool state, bool force = false)
        {
            return DoorManager.SetCargoDoors(state, force);
        }

        public override Task DoorsAllClose()
        {
            if (!ExperimentalWritesEnabled)
                return Task.CompletedTask;

            return DoorManager.CloseAllKnownDoors();
        }

        public override Task OnDoorTrigger(GsxDoor door, bool trigger)
        {
            return DoorManager.OnDoorTrigger(door, trigger);
        }

        public override Task OnLoaderAttached(GsxDoor door, bool attached)
        {
            return DoorManager.OnLoaderAttached(door, attached);
        }

        public override Task OnJetwayStateChange(GsxServiceState state, bool paxDoorAllowed)
        {
            return DoorManager.OnJetwayStateChange(state, paxDoorAllowed);
        }

        public override Task OnStairStateChange(GsxServiceState state, bool paxDoorAllowed)
        {
            return DoorManager.OnStairStateChange(state, paxDoorAllowed);
        }

        public override Task OnStairOperationChange(GsxServiceState state, bool paxDoorAllowed)
        {
            return DoorManager.OnStairOperationChange(state, paxDoorAllowed);
        }

        public override Task OnStairVehicleChange(
            GsxVehicleStair stair,
            GsxVehicleStairState state,
            bool paxDoorAllowed)
        {
            return DoorManager.OnStairVehicleChange(stair, state, paxDoorAllowed);
        }

        public override Task PushStateChange(GsxServiceState state)
        {
            if (StateLoggingEnabled)
                Logger.Debug($"PMDG 737 pushback monitor: GSX state -> {state}");

            return Task.CompletedTask;
        }

        public override Task PushOperationChange(int status)
        {
            if (StateLoggingEnabled)
                Logger.Debug($"PMDG 737 pushback monitor: operation -> {status}");

            return Task.CompletedTask;
        }
    }
}
