using CFIT.AppLogger;

namespace Pmdg737Interface
{
    public class Pmdg737Diagnostics(Pmdg737Aircraft aircraft)
    {
        public virtual Pmdg737Aircraft Aircraft { get; } = aircraft;
        protected virtual string LastState { get; set; } = "";

        public virtual void LogIfChanged()
        {
            if (!Aircraft.StateLoggingEnabled || !Aircraft.IsConnected)
                return;

            string state = BuildState();
            if (state == LastState)
                return;

            if (string.IsNullOrEmpty(LastState))
                Logger.Information($"PMDG 737 monitor connected: {state}");
            else
                Logger.Debug($"PMDG 737 state changed: {state}");

            LastState = state;
        }

        public virtual string BuildState()
        {
            var d = Aircraft.Data;

            return
                $"Model={d.AircraftModel}:{Pmdg737Model.GetName(d.AircraftModel)} " +
                $"Cargo={(Pmdg737Model.IsCargo(d.AircraftModel) ? 1 : 0)} " +
                $"PB={d.PED_annunParkingBrake} " +
                $"GndAvail={d.ELEC_annunGRD_POWER_AVAILABLE} " +
                $"GndSw={d.ELEC_GrdPwrSw} " +
                $"GroundConn={d.GroundConnAvailable} " +
                $"AC1={d.ELEC_BusPowered_AC_TRANSFER_1} " +
                $"AC2={d.ELEC_BusPowered_AC_TRANSFER_2} " +
                $"APUsel={d.APU_Selector} " +
                $"APUegt={d.APU_EGTNeedle:0.0} " +
                $"APUbleed={d.AIR_APUBleedAirSwitch} " +
                $"POS={d.LTS_PositionSw} " +
                $"BCN={d.LTS_AntiCollisionSw} " +
                $"L1={d.DOOR_annunFWD_ENTRY} " +
                $"R1={d.DOOR_annunFWD_SERVICE} " +
                $"L2={d.DOOR_annunAFT_ENTRY} " +
                $"R2={d.DOOR_annunAFT_SERVICE} " +
                $"CgoF={d.DOOR_annunFWD_CARGO} " +
                $"CgoA={d.DOOR_annunAFT_CARGO} " +
                $"Airstair={d.DOOR_annunAIRSTAIR}";
        }
    }
}
