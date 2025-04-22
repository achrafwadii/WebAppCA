using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using WebAppCA.Models;

namespace WebAppCA.Controllers
{
    public class DoorController : Controller
    {
        private readonly ILogger<DoorController> _logger;

        public DoorController(ILogger<DoorController> logger)
        {
            _logger = logger;
        }

        // === Fonctions SDK pour la gestion des portes ===
        [DllImport("BS_SDK_V2.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int BS2_GetDoor(IntPtr deviceContext, uint doorId, out IntPtr doorInfo);

        [DllImport("BS_SDK_V2.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int BS2_SetDoor(IntPtr deviceContext, IntPtr doorInfo);

        [DllImport("BS_SDK_V2.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int BS2_RemoveDoor(IntPtr deviceContext, uint doorId);

        [DllImport("BS_SDK_V2.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int BS2_ControlDoor(IntPtr deviceContext, uint doorId, byte controlCode);

        // Structure pour les informations de porte
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct BS2Door
        {
            public uint doorID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 48)]
            public string name;
            public uint deviceID;
            public byte relayPort;
            public byte mode;
            // Ajouter d'autres champs selon le SDK
        }

        // GET: /Door/Index
        public IActionResult Index()
        {
            // Récupérer la liste des portes (simulé pour l'exemple)
            var doors = new List<DoorInfoModel>();

            // Récupérer la liste des équipements pour le dropdown
            ViewBag.Devices = GetDevices(); // Implémentez cette méthode

            return View(doors);
        }

        // POST: /Door/AddDoor
        [HttpPost]
        public IActionResult AddDoor(string doorName, uint deviceID, int portNumber)
        {
            try
            {
                // Initialiser le SDK
                int result = DeviceController1.BS2_Initialize();
                if (result != 0) throw new Exception($"Erreur SDK: {result}");

                // Connecter à l'appareil
                IntPtr deviceContext;
                result = DeviceController1.BS2_ConnectDevice(IntPtr.Zero, deviceID, out deviceContext);
                if (result != 0) throw new Exception($"Erreur connexion: {result}");

                // Configurer la nouvelle porte
                BS2Door door = new BS2Door
                {
                    doorID = (uint)DateTime.Now.Ticks, // Générer un ID unique
                    name = doorName,
                    deviceID = deviceID,
                    relayPort = (byte)portNumber,
                    mode = 1 // Mode normal
                };

                // Allouer mémoire pour la structure
                IntPtr doorPtr = Marshal.AllocHGlobal(Marshal.SizeOf(door));
                Marshal.StructureToPtr(door, doorPtr, false);

                // Envoyer la configuration à l'appareil
                result = BS2_SetDoor(deviceContext, doorPtr);

                // Libérer la mémoire
                Marshal.FreeHGlobal(doorPtr);
                DeviceController1.BS2_Release();

                if (result == 0)
                {
                    TempData["Message"] = "Porte ajoutée avec succès";
                }
                else
                {
                    TempData["Error"] = $"Erreur lors de l'ajout. Code: {result}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'ajout de la porte");
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("Index");
        }

        // POST: /Door/ToggleDoor
        [HttpPost]
        public IActionResult ToggleDoor(uint doorID)
        {
            try
            {
                // TODO: Ajoutez la logique de bascule d'état de la porte
                TempData["Message"] = $"Porte {doorID} activée/désactivée";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors du changement d'état de la porte {doorID}");
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("Index");
        }

        // POST: /Door/DeleteDoor
        [HttpPost]
        public IActionResult DeleteDoor(uint doorID)
        {
            try
            {
                // TODO: Ajoutez la logique de suppression de la porte
                TempData["Message"] = $"Porte {doorID} supprimée";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la suppression de la porte {doorID}");
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("Index");
        }

        private List<DeviceInfoModel> GetDevices()
        {
            // TODO: Implémentez la récupération des équipements disponibles
            return new List<DeviceInfoModel>();
        }
    }
}