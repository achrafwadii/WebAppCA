using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebAppCA.Models
{
    public class PortsViewModel
    {
        // Ports du côté gauche
        [Display(Name = "ASP.NET Core HTTP")]
        public int HttpPort { get; set; } = 5090;

        [Display(Name = "Device TCP Port")]
        public int DeviceTcpPort { get; set; } = 51212;

        [Display(Name = "Port Base de données")]
        public int DatabasePort { get; set; } = 1433;

        [Display(Name = "IIS Express HTTP")]
        public int IisExpressHttpPort { get; set; } = 5664;

        [Display(Name = "gRPC Port (Gateway)")]
        public int GrpcPort { get; set; } = 4000;

        [Display(Name = "IIS Express HTTPS")]
        public int IisExpressHttpsPort { get; set; } = 44310;

        // Ports du côté droit
        [Display(Name = "ASP.NET Core HTTPS")]
        public int HttpsPort { get; set; } = 7211;

        [Display(Name = "Device TLS Port")]
        public int DeviceTlsPort { get; set; } = 51213;

        [Display(Name = "UDP Port (Device Discovery)")]
        public int DeviceUdpPort { get; set; } = 51212;

        [Display(Name = "Port Gateway Maître (non utilisé)")]
        public int MasterGatewayPort { get; set; } = 4010;

        // Propriétés pour les ports série (si nécessaires)
        public List<string> AvailablePorts { get; set; } = new List<string>();
        public string CurrentPort { get; set; }
        public int BaudRate { get; set; } = 9600;
        public int DataBits { get; set; } = 8;
        public string Parity { get; set; } = "None";
        public string StopBits { get; set; } = "One";
    }
}
