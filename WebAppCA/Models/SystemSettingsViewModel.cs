using WebAppCA.Controllers;

namespace WebAppCA.Models
{
    public class SystemSettingsViewModel
    {
        public string DeviceName { get; set; }
        public string TimeZone { get; set; }
        public string Language { get; set; }
        public int AutoLockTimeout { get; set; }
    }
}
