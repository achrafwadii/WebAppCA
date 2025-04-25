namespace WebAppCA.Models
{
    public class PortsViewModel
    {
        public List<string> AvailablePorts { get; set; }
        public string CurrentPort { get; set; }
        public int BaudRate { get; set; }
        public int DataBits { get; set; }
        public string Parity { get; set; }
        public string StopBits { get; set; }
    }
}
