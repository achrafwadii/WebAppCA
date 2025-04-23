using System;

namespace WebAppCA.Models
{
    public class ConnectDeviceRequest
    {
        public uint DeviceId { get; set; }
        public string IpAddress { get; set; }
        public ushort Port { get; set; }
        public string ModelName { get; set; }
        public ushort FirmwareVersion { get; set; }
    }

    public class SetTimeRequest
    {
        public DateTime? DateTime { get; set; }
    }
}