namespace WebAppCA.Models
{
    // Classe locale pour représenter les informations de connexion
    // Cette classe est distincte de la classe générée par gRPC
    public class ConnectInfo
    {
        public string IPAddr { get; set; }
        public int Port { get; set; }
        public bool UseSSL { get; set; }
    }
}