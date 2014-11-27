using System;

namespace OutsideConnectionsSchema
{
    public class DeviceConnection
    {

        public int StartDeviceId { get; private set; }

        public string StartPinName { get; private set; }

        public int EndDeviceId { get; private set; }

        public string EndPinName { get; private set; }

        public int CableId { get; private set; }

        public string Signal { get; private set; }

        public DeviceConnection(int startDeviceId, string startPinName, int endDeviceId, string endPinName, int cableId, string signal)
        {
            StartDeviceId = startDeviceId;
            StartPinName = String.Intern(startPinName);
            EndDeviceId = endDeviceId;
            EndPinName = String.Intern(endPinName);
            CableId = cableId;
            Signal = signal;
        }
    }
}
