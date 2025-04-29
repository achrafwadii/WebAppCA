using Grpc.Core;
using System;
using System.Collections.Generic;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using System.Threading.Channels;
using WebAppCA.Protos;

namespace WebAppCA.Services
{
    public class ConnectService
    {
        private readonly Connect.ConnectClient _client;

        public ConnectService(Channel channel)
        {
            _client = new Connect.ConnectClient(channel);
        }

        public List<Connect.DeviceInfo> GetDevices()
        {
            var response = _client.GetDeviceList(new GetDeviceListRequest());
            return new List<Connect.DeviceInfo>(response.DeviceInfos);
        }

        public uint ConnectToDevice(string ip, int port)
        {
            var req = new ConnectRequest
            {
                ConnectInfo = new ConnectInfo
                {
                    IPAddr = ip,
                    Port = port,
                    UseSSL = true
                }
            };

            var response = _client.Connect(req);
            return response.DeviceID;
        }
    }
}
