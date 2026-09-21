using Any2GSX.PluginInterface.Interfaces;
using CFIT.AppLogger;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pmdg737Interface
{
    public class Pmdg737DoorManager(Pmdg737Aircraft aircraft)
    {
        public virtual Pmdg737Aircraft Aircraft { get; } = aircraft;

        public virtual Dictionary<Pmdg737DoorId, Pmdg737Door> Doors { get; } = new()
        {
            { Pmdg737DoorId.FwdL, new(aircraft, Pmdg737DoorId.FwdL, Pmdg737Sdk.EventCode.DoorFwdL) },
            { Pmdg737DoorId.FwdR, new(aircraft, Pmdg737DoorId.FwdR, Pmdg737Sdk.EventCode.DoorFwdR) },
            { Pmdg737DoorId.AftL, new(aircraft, Pmdg737DoorId.AftL, Pmdg737Sdk.EventCode.DoorAftL) },
            { Pmdg737DoorId.AftR, new(aircraft, Pmdg737DoorId.AftR, Pmdg737Sdk.EventCode.DoorAftR) },
            { Pmdg737DoorId.OverwingL, new(aircraft, Pmdg737DoorId.OverwingL, Pmdg737Sdk.EventCode.DoorOverwingL) },
            { Pmdg737DoorId.OverwingR, new(aircraft, Pmdg737DoorId.OverwingR, Pmdg737Sdk.EventCode.DoorOverwingR) },
            { Pmdg737DoorId.CargoFwd, new(aircraft, Pmdg737DoorId.CargoFwd, Pmdg737Sdk.EventCode.DoorCargoFwd) },
            { Pmdg737DoorId.CargoAft, new(aircraft, Pmdg737DoorId.CargoAft, Pmdg737Sdk.EventCode.DoorCargoAft) },
            { Pmdg737DoorId.CargoMain, new(aircraft, Pmdg737DoorId.CargoMain, Pmdg737Sdk.EventCode.DoorCargoMain) },
            { Pmdg737DoorId.Equipment, new(aircraft, Pmdg737DoorId.Equipment, Pmdg737Sdk.EventCode.DoorEquipment) },
            { Pmdg737DoorId.Airstair, new(aircraft, Pmdg737DoorId.Airstair, Pmdg737Sdk.EventCode.DoorAirstair) },
        };

        public virtual Dictionary<GsxVehicleStair, GsxVehicleStairState> StairStates { get; } = new()
        {
            { GsxVehicleStair.Front, GsxVehicleStairState.Unknown },
            { GsxVehicleStair.Middle, GsxVehicleStairState.Unknown },
            { GsxVehicleStair.Rear, GsxVehicleStairState.Unknown },
        };

        public virtual bool HasOpenDoors()
        {
            return Doors.Values.Any(d => d.IsOpenIndicated);
        }

        public virtual Pmdg737Door GetDoor(GsxDoor door)
        {
            return door switch
            {
                GsxDoor.PaxDoor1 => Doors[Pmdg737DoorId.FwdL],

                // GSX aircraft profiles can represent the aft passenger door using
                // different passenger-door slots. Both are intentionally mapped
                // to the 737 aft-left entry door for the experimental layer.
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

        public virtual async Task CloseAllKnownDoors()
        {
            foreach (var door in Doors.Values)
            {
                if (door.Id == Pmdg737DoorId.CargoMain && !Pmdg737Model.IsCargo(Aircraft.Data.AircraftModel))
                    continue;

                if (door.IsOpenIndicated)
                    await door.SetOpen(false);
            }
        }

        public virtual async Task SetCargoDoors(bool open)
        {
            await Doors[Pmdg737DoorId.CargoFwd].SetOpen(open);
            await Doors[Pmdg737DoorId.CargoAft].SetOpen(open);

            if (Pmdg737Model.IsCargo(Aircraft.Data.AircraftModel))
                await Doors[Pmdg737DoorId.CargoMain].SetOpen(open);
        }

        public virtual Task OnDoorTrigger(GsxDoor door, bool trigger)
        {
            if (!trigger)
                return Task.CompletedTask;

            var mapped = GetDoor(door);
            if (mapped == null)
                return Task.CompletedTask;

            return mapped.SetOpen(!mapped.IsOpenIndicated);
        }

        public virtual Task OnLoaderAttached(GsxDoor door, bool attached)
        {
            var mapped = GetDoor(door);
            if (mapped == null)
                return Task.CompletedTask;

            return mapped.SetOpen(attached);
        }

        public virtual Task OnJetwayStateChange(GsxServiceState state, bool paxDoorAllowed)
        {
            if (!paxDoorAllowed)
                return Task.CompletedTask;

            return Doors[Pmdg737DoorId.FwdL].SetOpen(state == GsxServiceState.Active);
        }

        public virtual async Task OnStairStateChange(GsxServiceState state, bool paxDoorAllowed)
        {
            if (!paxDoorAllowed)
                return;

            bool open = state == GsxServiceState.Active;

            if (open)
            {
                if (StairStates[GsxVehicleStair.Front] >= GsxVehicleStairState.Approaching &&
                    !Aircraft.GsxController.HasGateJetway)
                {
                    await Doors[Pmdg737DoorId.FwdL].SetOpen(true);
                }

                if (StairStates[GsxVehicleStair.Rear] >= GsxVehicleStairState.Approaching)
                    await Doors[Pmdg737DoorId.AftL].SetOpen(true);
            }
            else
            {
                await Doors[Pmdg737DoorId.FwdL].SetOpen(false);
                await Doors[Pmdg737DoorId.AftL].SetOpen(false);
            }
        }

        public virtual Task OnStairVehicleChange(
            GsxVehicleStair stair,
            GsxVehicleStairState state,
            bool paxDoorAllowed)
        {
            StairStates[stair] = state;

            if (Aircraft.StateLoggingEnabled)
                Logger.Debug($"PMDG 737 stair monitor: {stair} -> {state}");

            return Task.CompletedTask;
        }
    }
}
