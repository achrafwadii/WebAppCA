namespace WebAppCA.Models
{
    public class DeviceInfoModel
    {
        public uint DeviceID { get; set; }
        public string DeviceName { get; set; }
        public string IPAddress { get; set; }
        public int Port { get; set; }  // Added Port property
        public string ConnectionStatus { get; set; }
    }

}
