using Any2GSX.PluginInterface;
using Any2GSX.PluginInterface.Interfaces;
using System;

namespace Pmdg737Interface
{
    public class Pmdg737Plugin(IAppResources appResources) : AppPlugin(appResources)
    {
        public override string Id => "PMDG.B737";

        public override PluginType Type => PluginType.BinaryV1;

        public override Type GetAircraftInterface(string aircraftString)
        {
            return typeof(Pmdg737Aircraft);
        }

        public override Type SimConnectModuleType => typeof(Pmdg737Module);
    }
}
