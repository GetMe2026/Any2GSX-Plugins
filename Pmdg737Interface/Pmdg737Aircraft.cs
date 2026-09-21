using Any2GSX.PluginInterface;
using Any2GSX.PluginInterface.Interfaces;
using CFIT.AppLogger;
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

        public virtual bool ExperimentalWritesEnabled => GetBoolSetting(
            Pmdg737Settings.EnableExperimentalWrites,
            false);

        public virtual bool StateLoggingEnabled => GetBoolSetting(
            Pmdg737Settings.StateLogging,
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
            foreach (var door in DoorManager.Doors.Values)
                SimStore.AddEvent(Pmdg737Sdk.GetEventName(door.EventCode));

            Logger.Information(
                ExperimentalWritesEnabled
                    ? "PMDG 737 native plugin initialized - EXPERIMENTAL WRITES ENABLED"
                    : "PMDG 737 native plugin initialized - read-only monitoring mode");

            return Task.CompletedTask;
        }

        protected override Task DoStop()
        {
            foreach (var door in DoorManager.Doors.Values)
                SimStore.Remove(Pmdg737Sdk.GetEventName(door.EventCode));

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
            // The foundation intentionally does not advertise advanced aircraft
            // automation until runtime behavior has been validated in MSFS.
            return Task.FromResult(false);
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
                return Data.ELEC_BusPowered_AC_TRANSFER_1 != 0 ||
                       Data.ELEC_BusPowered_AC_TRANSFER_2 != 0 ||
                       Data.ELEC_BusPowered_DC_BATT_BUS != 0;
            }

            return await base.GetAvionicPowered();
        }

        public override async Task<bool> GetExternalPowerAvailable()
        {
            if (IsConnected)
                return Data.ELEC_annunGRD_POWER_AVAILABLE != 0;

            return await base.GetExternalPowerAvailable();
        }

        public override async Task<bool> GetExternalPowerConnected()
        {
            if (IsConnected)
                return Data.ELEC_GrdPwrSw != 0;

            return await base.GetExternalPowerConnected();
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
            return DoorManager.SetCargoDoors(state);
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
