using System.Runtime.InteropServices;

namespace WebAppCA.Services
{
    public class BioStarService
    {
        [DllImport("BS_SDK.dll")]
        public static extern int BS_Init(int port, string ip);

        [DllImport("BS_SDK.dll")]
        public static extern int BS_Connect(int deviceId);

        // Ajoutez d'autres méthodes du SDK selon vos besoins

        public bool ConnectToDevice(string ip, int port)
        {
            try
            {
                int result = BS_Init(port, ip);
                return result == 0; // 0 = succès
            }
            catch
            {
                return false;
            }
        }
    }
}
