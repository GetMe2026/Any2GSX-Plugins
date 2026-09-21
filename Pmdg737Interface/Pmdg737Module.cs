using CFIT.AppLogger;
using CFIT.SimConnectLib;
using CFIT.SimConnectLib.Modules;
using Microsoft.FlightSimulator.SimConnect;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Pmdg737Interface
{
    public class Pmdg737Module(SimConnectManager manager, object moduleParams, bool wasRegisteredBefore)
        : SimConnectModule(manager, moduleParams, wasRegisteredBefore)
    {
        protected virtual bool ClientDataAreaCreated { get; set; } = false;
        public virtual bool ReceivedDataValid { get; protected set; } = false;
        protected virtual bool FirstReceive { get; set; } = true;
        public virtual PMDG_NG3_Data Data { get; protected set; } = new();

        protected override void SetModuleParams(object moduleParams)
        {
        }

        public override Task<int> CheckResources()
        {
            return Task.FromResult(0);
        }

        public override Task CheckState()
        {
            return Task.CompletedTask;
        }

        public override Task ClearUnusedResources(bool clearAll)
        {
            return Task.CompletedTask;
        }

        public override void RegisterModule()
        {
            Manager.OnOpen += OnOpen;
        }

        public override Task OnOpen(SIMCONNECT_RECV_OPEN evtData)
        {
            Manager.OnClientData += OnClientData;
            return CreateDataArea();
        }

        public override Task UnregisterModule(bool disconnect)
        {
            if (disconnect && Manager.IsReceiveRunning)
            {
                Manager.OnClientData -= OnClientData;
                return Call(sc => sc.RequestClientData(
                    PMDG_NG3_ID.PMDG_NG3_DATA_ID,
                    PMDG_NG3_ID.DATA_REQUEST,
                    PMDG_NG3_ID.PMDG_NG3_DATA_DEFINITION,
                    SIMCONNECT_CLIENT_DATA_PERIOD.NEVER,
                    SIMCONNECT_CLIENT_DATA_REQUEST_FLAG.DEFAULT,
                    0, 0, 0));
            }

            return Task.CompletedTask;
        }

        protected virtual async Task CreateDataArea()
        {
            int size = Marshal.SizeOf(typeof(PMDG_NG3_Data));
            if (size != Pmdg737Sdk.ExpectedDataSize)
            {
                Logger.Error($"PMDG 737 interop size mismatch: {size} != {Pmdg737Sdk.ExpectedDataSize}");
                return;
            }

            if (!ClientDataAreaCreated && !WasRegisteredBefore)
            {
                await Call(sc => sc.MapClientDataNameToID(
                    Pmdg737Sdk.DataName,
                    PMDG_NG3_ID.PMDG_NG3_DATA_ID));

                await Call(sc => sc.AddToClientDataDefinition(
                    PMDG_NG3_ID.PMDG_NG3_DATA_DEFINITION,
                    0,
                    (uint)size,
                    0,
                    0));
            }

            await Call(sc => sc.RegisterStruct<SIMCONNECT_RECV_CLIENT_DATA, PMDG_NG3_Data>(
                PMDG_NG3_ID.PMDG_NG3_DATA_DEFINITION));

            await Call(sc => sc.RequestClientData(
                PMDG_NG3_ID.PMDG_NG3_DATA_ID,
                PMDG_NG3_ID.DATA_REQUEST,
                PMDG_NG3_ID.PMDG_NG3_DATA_DEFINITION,
                SIMCONNECT_CLIENT_DATA_PERIOD.VISUAL_FRAME,
                SIMCONNECT_CLIENT_DATA_REQUEST_FLAG.CHANGED,
                0, 0, 0));

            ClientDataAreaCreated = true;
        }

        protected virtual Task OnClientData(SIMCONNECT_RECV_CLIENT_DATA evtData)
        {
            try
            {
                if (evtData.dwRequestID == (uint)PMDG_NG3_ID.DATA_REQUEST &&
                    evtData?.dwData?.Length > 0)
                {
                    Data = (PMDG_NG3_Data)evtData.dwData[0];
                    ReceivedDataValid = Data.AircraftModel > 0 && Data.AircraftModel < 100;

                    if (FirstReceive)
                    {
                        if (!ReceivedDataValid)
                        {
                            Logger.Warning("PMDG 737 ClientData received, but AircraftModel is not valid.");
                        }
                        else
                        {
                            Logger.Information(
                                $"Receiving PMDG 737 NG3 ClientData - model {Data.AircraftModel} ({Pmdg737Model.GetName(Data.AircraftModel)})");
                        }

                        FirstReceive = false;
                    }
                }
                else if (Manager.Config.VerboseLogging)
                {
                    Logger.Verbose(
                        $"PMDG 737: unknown ClientData event dwID={evtData?.dwID} define={evtData?.dwDefineID} request={evtData?.dwRequestID}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return Task.CompletedTask;
        }
    }
}
