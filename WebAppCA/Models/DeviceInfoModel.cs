namespace WebAppCA.Models
{
    public class DeviceInfoModel
    {
        public uint DeviceID { get; set; }
        public string DeviceName { get; set; }
        public string IPAddress { get; set; }
        public ushort Port { get; set; } = 51211;
        public string ConnectionStatus { get; set; } = "Déconnecté";
    }
}