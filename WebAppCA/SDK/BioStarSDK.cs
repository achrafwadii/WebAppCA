
    using System;
    using System.Runtime.InteropServices;

    namespace WebAppCA.SDK
    {
        public enum BS2ErrorCode : int
        {
            BS_SDK_SUCCESS = 1,
            // … ajoute d’autres codes d’erreur si nécessaire
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct BS2AccessGroup
        {
            public uint id;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string name;
            // etc. selon la doc, adapte la taille des tableaux
        }

        public static class SupremaApi
        {
            private const string DllName = "BS_SDK_V2.dll";

            [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
            public static extern int BS2_Initialize();

            [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
            public static extern int BS2_Release();

            [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
            public static extern int BS2_GetAllAccessGroup(
                IntPtr sdkContext, uint deviceID,
                out IntPtr groupList, out uint groupCount);

            [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
            public static extern int BS2_RemoveAllAccessGroup(
                IntPtr sdkContext, uint deviceID);

            [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
            public static extern int BS2_SetAccessGroup(
                IntPtr sdkContext, uint deviceID,
                IntPtr groupList, uint groupCount);

            // … déclare ici BS2_GetAccessLevel, BS2_SetAccessLevel, etc.
        }
    }


