namespace WebAppCA.Models
{
    public class DoorInfoModel
    {
        public uint DoorID { get; set; }
        public string Name { get; set; }
        public uint DeviceID { get; set; }
        public string DeviceName { get; set; } // Pour l'affichage
        public byte RelayPort { get; set; }
        public byte Mode { get; set; }
        public string Status { get; set; } 


    }
}