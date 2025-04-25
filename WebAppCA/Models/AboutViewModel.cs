namespace WebAppCA.Models
{
    public class AboutViewModel
    {
        public string SystemVersion { get; set; }
        public string FirmwareVersion { get; set; }
        public string SerialNumber { get; set; }
        public DateTime InstallDate { get; set; }
        public DateTime LastUpdate { get; set; }
    }
}
