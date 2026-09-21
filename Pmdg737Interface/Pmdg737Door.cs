using CFIT.AppLogger;
using System.Threading.Tasks;

namespace Pmdg737Interface
{
    public enum Pmdg737DoorId
    {
        FwdL,
        FwdR,
        AftL,
        AftR,
        OverwingL,
        OverwingR,
        CargoFwd,
        CargoAft,
        CargoMain,
        Equipment,
        Airstair,
    }

    public class Pmdg737Door(Pmdg737Aircraft aircraft, Pmdg737DoorId id, int eventCode)
    {
        public virtual Pmdg737Aircraft Aircraft { get; } = aircraft;
        public virtual Pmdg737DoorId Id { get; } = id;
        public virtual int EventCode { get; } = eventCode;

        public virtual bool IsOpenIndicated => Id switch
        {
            Pmdg737DoorId.FwdL => Aircraft.Data.DOOR_annunFWD_ENTRY != 0,
            Pmdg737DoorId.FwdR => Aircraft.Data.DOOR_annunFWD_SERVICE != 0,
            Pmdg737DoorId.AftL => Aircraft.Data.DOOR_annunAFT_ENTRY != 0,
            Pmdg737DoorId.AftR => Aircraft.Data.DOOR_annunAFT_SERVICE != 0,
            Pmdg737DoorId.OverwingL =>
                Aircraft.Data.DOOR_annunLEFT_FWD_OVERWING != 0 ||
                Aircraft.Data.DOOR_annunLEFT_AFT_OVERWING != 0,
            Pmdg737DoorId.OverwingR =>
                Aircraft.Data.DOOR_annunRIGHT_FWD_OVERWING != 0 ||
                Aircraft.Data.DOOR_annunRIGHT_AFT_OVERWING != 0,
            Pmdg737DoorId.CargoFwd => Aircraft.Data.DOOR_annunFWD_CARGO != 0,
            Pmdg737DoorId.CargoAft => Aircraft.Data.DOOR_annunAFT_CARGO != 0,
            Pmdg737DoorId.Equipment => Aircraft.Data.DOOR_annunEQUIP != 0,
            Pmdg737DoorId.Airstair => Aircraft.Data.DOOR_annunAIRSTAIR != 0,
            _ => false,
        };

        public virtual Task SetOpen(bool targetOpen)
        {
            if (!Aircraft.ExperimentalWritesEnabled)
            {
                Logger.Debug($"PMDG 737 write suppressed: {Id} targetOpen={targetOpen}");
                return Task.CompletedTask;
            }

            // NG3 annunciators are currently treated only as an open/not-open indication.
            // We intentionally do not infer OPENING/CLOSING. One toggle is sent only when
            // the indicated state differs from the requested target.
            if (IsOpenIndicated == targetOpen)
                return Task.CompletedTask;

            return Toggle();
        }

        public virtual Task Toggle()
        {
            if (!Aircraft.ExperimentalWritesEnabled)
                return Task.CompletedTask;

            string evt = Pmdg737Sdk.GetEventName(EventCode);
            Logger.Information(
                $"EXPERIMENTAL PMDG 737 door event: {Id} -> {Pmdg737Sdk.GetEventId(EventCode)}");

            return Aircraft.SimStore[evt]?.WriteValue(Pmdg737Sdk.MouseLeftSingle)
                ?? Task.CompletedTask;
        }
    }
}
