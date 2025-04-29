using System;
using System.Runtime.InteropServices;

namespace WebAppCA.SDK
{
    public static class API
    {
        // Chemin vers la DLL
        private const string DllPath = "BS_SDK_V2.dll";

        // Import des fonctions Suprema SDK
        [DllImport("BS_SDK_V2.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int BS2_Initialize(ref IntPtr context);


        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        public static extern int BS2_ReleaseContext(IntPtr context);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        public static extern int BS2_GetDeviceInfoEx(IntPtr context, uint deviceId, out BS2SimpleDeviceInfo deviceInfo, out BS2SimpleDeviceInfoEx deviceInfoEx);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        public static extern int BS2_GetDeviceTime(IntPtr context, uint deviceId, out uint timestamp);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        public static extern int BS2_SetDeviceTime(IntPtr context, uint deviceId, uint timestamp);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        public static extern int BS2_FactoryReset(IntPtr context, uint deviceId);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        public static extern int BS2_RebootDevice(IntPtr context, uint deviceId);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        public static extern int BS2_LockDevice(IntPtr context, uint deviceId);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        public static extern int BS2_UnlockDevice(IntPtr context, uint deviceId);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        public static extern int BS2_GetWiegandMultiConfig(IntPtr context, uint deviceId, out BS2WiegandMultiConfig config);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        public static extern int BS2_SetWiegandMultiConfig(IntPtr context, uint deviceId, ref BS2WiegandMultiConfig config);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        public static extern int BS2_GetConfig(IntPtr context, uint deviceId, ref BS2Configs configs);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        public static extern int BS2_SearchDevices(IntPtr context);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        public static extern int BS2_ConnectDevice(IntPtr context, uint deviceId);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        public static extern int BS2_DisconnectDevice(IntPtr context, uint deviceId);

        // Définissez d'autres importations selon vos besoins
    }
}