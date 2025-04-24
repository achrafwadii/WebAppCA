using System;

namespace WebAppCA.Models
{
    public class DeviceInfoModel
    {
        public uint DeviceID { get; set; }
        public string DeviceName { get; set; }
        public string IPAddress { get; set; }
        public int Port { get; set; }
        public string ConnectionStatus { get; set; }
        public DateTime LastConnection { get; set; }
        public string FirmwareVersion { get; set; }
        public string ModelName { get; set; }
    }
}