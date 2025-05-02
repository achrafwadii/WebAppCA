using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebAppCA.Models;
using WebAppCA.Data;

namespace WebAppCA.Services
{
    public class DeviceDbService
    {
        private readonly ApplicationDbContext _context;

        public DeviceDbService(ApplicationDbContext context)
        {
            _context = context;
        }

        public DeviceInfo GetDeviceById(int deviceId)
        {
            return _context.Devices.FirstOrDefault(d => d.DeviceId == deviceId);
        }

        public IEnumerable<DeviceInfo> GetAllDevices()
        {
            return _context.Devices.ToList();
        }

        public int AddDevice(DeviceInfo device)
        {
            // Ensure required properties aren't null
            if (string.IsNullOrEmpty(device.IPAddress))
            {
                device.IPAddress = "unknown";
            }

            _context.Devices.Add(device);
            _context.SaveChanges();
            return device.DeviceId;
        }

        public bool UpdateDevice(DeviceInfo device)
        {
            try
            {
                // Ensure required properties aren't null
                if (string.IsNullOrEmpty(device.IPAddress))
                {
                    device.IPAddress = "unknown";
                }

                _context.Entry(device).State = EntityState.Modified;
                _context.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }


        public bool DeleteDevice(int deviceId)
        {
            var device = _context.Devices.Find(deviceId);
            if (device == null)
                return false;

            _context.Devices.Remove(device);
            _context.SaveChanges();
            return true;
        }
    }
}