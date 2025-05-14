namespace WebAppCA.Services
{
    public class GrpcSettings
    {
        public string Address { get; set; }
        public int Port { get; set; }
        public string CaCertPath { get; set; }
        public bool UseSSL { get; set; }
    }
}
