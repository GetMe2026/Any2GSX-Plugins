using System.Runtime.InteropServices;

namespace Pmdg737Interface
{
    public enum PMDG_NG3_ID
    {
        DATA_REQUEST = 10,
        PMDG_NG3_DATA_ID = 0x4E473331,
        PMDG_NG3_DATA_DEFINITION = 0x4E473332,
        PMDG_NG3_CONTROL_ID = 0x4E473333,
        PMDG_NG3_CONTROL_DEFINITION = 0x4E473334,
    }

    public static class Pmdg737Sdk
    {
        public const string DataName = "PMDG_NG3_Data";
        public const string ControlName = "PMDG_NG3_Control";

        public const int ExpectedDataSize = 916;
        public const int EventBase = 69632;
        public const uint MouseLeftSingle = 0x20000000;

        public static class EventCode
        {
            public const int DoorFwdL = 14005;
            public const int DoorFwdR = 14006;
            public const int DoorAftL = 14007;
            public const int DoorAftR = 14008;
            public const int DoorOverwingL = 14009;
            public const int DoorOverwingR = 14010;
            public const int DoorCargoFwd = 14013;
            public const int DoorCargoAft = 14014;
            public const int DoorCargoMain = 14015;
            public const int DoorEquipment = 14016;
            public const int DoorAirstair = 14017;
        }

        public static int GetEventId(int eventCode) => EventBase + eventCode;

        public static string GetEventName(int eventCode) => $"#{GetEventId(eventCode)}";
    }

    // Deliberately minimal read-only view into PMDG_NG3_Data.
    // Offsets were derived from the supplied NG3 SDK layout using the native
    // 1-byte bool/char sizes and 4-byte maximum field alignment.
    // Explicit Size keeps the SimConnect ClientData definition equal to the
    // complete native structure without copying the full SDK header into this repo.
    [StructLayout(LayoutKind.Explicit, Size = Pmdg737Sdk.ExpectedDataSize, Pack = 4)]
    public struct PMDG_NG3_Data
    {
        [FieldOffset(116)] public float FUEL_QtyCenter;
        [FieldOffset(120)] public float FUEL_QtyLeft;
        [FieldOffset(124)] public float FUEL_QtyRight;

        [FieldOffset(142)] public byte ELEC_annunGRD_POWER_AVAILABLE;
        [FieldOffset(143)] public byte ELEC_GrdPwrSw;

        // ELEC_BusPowered[16], expanded to blittable scalar fields.
        [FieldOffset(182)] public byte ELEC_BusPowered_DC_HOT_BATT;
        [FieldOffset(183)] public byte ELEC_BusPowered_DC_HOT_BATT_SWITCHED;
        [FieldOffset(184)] public byte ELEC_BusPowered_DC_BATT_BUS;
        [FieldOffset(185)] public byte ELEC_BusPowered_DC_STANDBY_BUS;
        [FieldOffset(186)] public byte ELEC_BusPowered_DC_BUS_1;
        [FieldOffset(187)] public byte ELEC_BusPowered_DC_BUS_2;
        [FieldOffset(188)] public byte ELEC_BusPowered_DC_GROUND_SVC;
        [FieldOffset(189)] public byte ELEC_BusPowered_AC_TRANSFER_1;
        [FieldOffset(190)] public byte ELEC_BusPowered_AC_TRANSFER_2;
        [FieldOffset(191)] public byte ELEC_BusPowered_AC_GROUND_SVC_1;
        [FieldOffset(192)] public byte ELEC_BusPowered_AC_GROUND_SVC_2;
        [FieldOffset(193)] public byte ELEC_BusPowered_AC_MAIN_1;
        [FieldOffset(194)] public byte ELEC_BusPowered_AC_MAIN_2;
        [FieldOffset(195)] public byte ELEC_BusPowered_AC_GALLEY_1;
        [FieldOffset(196)] public byte ELEC_BusPowered_AC_GALLEY_2;
        [FieldOffset(197)] public byte ELEC_BusPowered_AC_STANDBY;

        [FieldOffset(200)] public float APU_EGTNeedle;
        [FieldOffset(284)] public byte AIR_APUBleedAirSwitch;

        [FieldOffset(344)] public byte DOOR_annunFWD_ENTRY;
        [FieldOffset(345)] public byte DOOR_annunFWD_SERVICE;
        [FieldOffset(346)] public byte DOOR_annunAIRSTAIR;
        [FieldOffset(347)] public byte DOOR_annunLEFT_FWD_OVERWING;
        [FieldOffset(348)] public byte DOOR_annunRIGHT_FWD_OVERWING;
        [FieldOffset(349)] public byte DOOR_annunFWD_CARGO;
        [FieldOffset(350)] public byte DOOR_annunEQUIP;
        [FieldOffset(351)] public byte DOOR_annunLEFT_AFT_OVERWING;
        [FieldOffset(352)] public byte DOOR_annunRIGHT_AFT_OVERWING;
        [FieldOffset(353)] public byte DOOR_annunAFT_CARGO;
        [FieldOffset(354)] public byte DOOR_annunAFT_ENTRY;
        [FieldOffset(355)] public byte DOOR_annunAFT_SERVICE;

        [FieldOffset(379)] public byte APU_Selector;
        [FieldOffset(380)] public byte ENG_StartSelector_1;
        [FieldOffset(381)] public byte ENG_StartSelector_2;
        [FieldOffset(383)] public byte LTS_LogoSw;
        [FieldOffset(384)] public byte LTS_PositionSw;
        [FieldOffset(385)] public byte LTS_AntiCollisionSw;

        [FieldOffset(574)] public byte PED_annunParkingBrake;

        [FieldOffset(620)] public byte FMC_TakeoffFlaps;
        [FieldOffset(634)] public byte FMC_PerfInputComplete;
        [FieldOffset(636)] public float FMC_DistanceToTOD;
        [FieldOffset(640)] public float FMC_DistanceToDest;

        [FieldOffset(654)] public ushort AircraftModel;
        [FieldOffset(656)] public byte WeightInKg;
        [FieldOffset(657)] public byte GPWS_V1CallEnabled;
        [FieldOffset(658)] public byte GroundConnAvailable;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct PMDG_NG3_Control
    {
        public uint Event;
        public uint Parameter;
    }
}
