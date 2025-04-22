namespace WebAppCA.Models
{
    public class DoorInfoModel
    {
        public uint DoorID { get; set; }
        public string Name { get; set; }
        public uint DeviceID { get; set; }
        public byte RelayPort { get; set; }
        public byte Mode { get; set; }
        // Add any other properties you need
    }
}