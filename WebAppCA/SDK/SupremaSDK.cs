using System;
using System.Runtime.InteropServices;

namespace WebAppCA.SDK
{
    public static class SupremaSDK
    {
        // Chemin vers la DLL - ajustez selon votre environnement
        private const string DllPath = "B2_SDK_V2.dll";

        // Initialisation
        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        public static extern int BS2_Initialize(out IntPtr context);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        public static extern int BS2_ReleaseContext(IntPtr context);

        // Device Management
        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        public static extern int BS2_GetDeviceInfoEx(IntPtr context, UInt32 deviceId, out BS2SimpleDeviceInfo deviceInfo, out BS2SimpleDeviceInfoEx deviceInfoEx);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        public static extern int BS2_GetDeviceTime(IntPtr context, UInt32 deviceId, out UInt32 timestamp);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        public static extern int BS2_SetDeviceTime(IntPtr context, UInt32 deviceId, UInt32 timestamp);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        public static extern int BS2_RebootDevice(IntPtr context, UInt32 deviceId);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        public static extern int BS2_FactoryReset(IntPtr context, UInt32 deviceId);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        public static extern int BS2_LockDevice(IntPtr context, UInt32 deviceId);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        public static extern int BS2_UnlockDevice(IntPtr context, UInt32 deviceId);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        public static extern int BS2_GetWiegandMultiConfig(IntPtr context, UInt32 deviceId, out BS2WiegandMultiConfig config);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        public static extern int BS2_SetWiegandMultiConfig(IntPtr context, UInt32 deviceId, ref BS2WiegandMultiConfig config);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        public static extern int BS2_GetConfig(IntPtr context, UInt32 deviceId, ref BS2Configs configs);

        // Connexion au périphérique
        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        public static extern int BS2_ConnectDevice(IntPtr context, string ipAddr, UInt16 port, out UInt32 deviceId);

        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        public static extern int BS2_DisconnectDevice(IntPtr context, UInt32 deviceId);

        // Recherche des périphériques
        [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
        public static extern int BS2_SearchDevices(IntPtr context);
    }

    // Structures requises (ajoutez toutes les structures nécessaires ici)
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct BS2SimpleDeviceInfo
    {
        internal readonly string ipAddress;
        public UInt32 deviceId;
        public UInt16 type;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] macAddr;
        public UInt16 connectionMode;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] ipAddr;
        public UInt16 port;
        public byte maxConnections;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 48)]
        public byte[] reserved;

        public string modelName { get; internal set; }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct BS2SimpleDeviceInfoEx
    {
        internal readonly ushort firmwareVersion;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] deviceName;
        public byte enableSSL;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] serverAddr;
        public UInt16 serverPort;
        public byte useServerMode;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] reserved;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct BS2WiegandFormat
    {
        public UInt32 length;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        public byte[] idFields;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        public byte[] parityFields;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] parityType;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] parityPos;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct BS2WiegandMultiFormat
    {
        public UInt32 formatID;
        public BS2WiegandFormat format;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct BS2WiegandMultiConfig
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
        public BS2WiegandMultiFormat[] formats;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct BS2Configs
    {
        public UInt32 configMask;
        // Ajoutez tous les champs nécessaires pour la structure BS2Configs
        // Ceci est une version simplifiée
        public BS2FactoryConfig factoryConfig;
        // Autres champs selon la documentation du SDK
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct BS2FactoryConfig
    {
        public UInt32 deviceID;
        // Autres champs selon la documentation du SDK
    }

    // Codes d'erreur
    public enum BS2ErrorCode
    {
        BS_SDK_SUCCESS = 0,
        // Ajoutez les autres codes d'erreur selon la documentation
    }

    // Masques de configuration
    
}