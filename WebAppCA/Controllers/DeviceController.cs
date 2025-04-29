using Microsoft.AspNetCore.Mvc;
using WebAppCA.Models;
using WebAppCA.Services;
using System.Collections.Generic;
using System.IO;

namespace WebAppCA.Controllers
{
    public class DeviceController : Controller
    {
        private const string CaCertPath = "certs/gateway_ca.pem";
        private const string GatewayAddress = "127.0.0.1";
        private const int GatewayPort = 50051;

        [HttpGet]
        public IActionResult Index()
        {
            var gateway = new GatewayClient();
            gateway.Connect(CaCertPath, GatewayAddress, GatewayPort);

            var connectSvc = new ConnectService(gateway.Channel);
            var grpcDevices = connectSvc.GetDevices();

            var model = new List<DeviceInfoModel>();

            foreach (var d in grpcDevices)
            {
                model.Add(new DeviceInfoModel
                {
                    DeviceName = d.DeviceID.ToString(),
                    DeviceID = d.DeviceID,
                    IPAddress = d.IPAddr,
                    ConnectionStatus = "Non connecté" // actualiser selon besoin
                });
            }

            gateway.Close();
            return View("Index", model);
        }

        [HttpPost]
        public IActionResult Connect(string ip, int port)
        {
            var gateway = new GatewayClient();
            gateway.Connect(CaCertPath, GatewayAddress, GatewayPort);

            var connectSvc = new ConnectService(gateway.Channel);
            var deviceId = connectSvc.ConnectToDevice(ip, port);

            TempData["Message"] = $"Équipement connecté avec succès (ID: {deviceId})";
            gateway.Close();

            return RedirectToAction("Index");
        }
    }
}
