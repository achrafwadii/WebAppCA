using System;

namespace WebAppCA.Models
{
    public class ConnectDeviceRequest
    {
        public string IpAddress { get; set; }
        public ushort? Port { get; set; }
    }

    public class SetTimeRequest
    {
        public DateTime? DateTime { get; set; }
    }
}