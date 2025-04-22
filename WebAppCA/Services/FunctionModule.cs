using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using WebAppCA.SDK;

namespace WebAppCA.Services
{
    // Cette classe abstraite sert de base pour tous les modules fonctionnels liés au SDK Suprema
    public abstract class FunctionModule
    {
        protected BS2SimpleDeviceInfo deviceInfo;
        protected BS2SimpleDeviceInfoEx deviceInfoEx;

        // Cette méthode abstraite doit être implémentée par les classes dérivées
        // Elle retourne la liste des fonctions disponibles pour ce module
        protected abstract List<KeyValuePair<string, Func<IntPtr, UInt32, bool, Task<object>>>> GetFunctionList(IntPtr sdkContext, UInt32 deviceID, bool isMasterDevice);

        // Cette méthode initialise les informations de l'appareil
        public async Task<bool> InitializeDeviceInfoAsync(IntPtr sdkContext, UInt32 deviceID)
        {
            BS2ErrorCode result = (BS2ErrorCode)API.BS2_GetDeviceInfoEx(sdkContext, deviceID, out deviceInfo, out deviceInfoEx);
            if (result != BS2ErrorCode.BS_SDK_SUCCESS)
            {
                return false;
            }
            return true;
        }

        // Cette méthode retourne la liste des fonctions disponibles sous forme de dictionnaire
        // pour une utilisation facile dans l'API web
        public async Task<Dictionary<string, Func<IntPtr, UInt32, bool, Task<object>>>> GetAvailableFunctions(IntPtr sdkContext, UInt32 deviceID, bool isMasterDevice)
        {
            var functionList = GetFunctionList(sdkContext, deviceID, isMasterDevice);
            var functionDict = new Dictionary<string, Func<IntPtr, UInt32, bool, Task<object>>>();

            foreach (var func in functionList)
            {
                functionDict[func.Key] = func.Value;
            }

            return functionDict;
        }
    }
}