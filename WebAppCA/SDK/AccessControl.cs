using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace WebAppCA.SDK
{
    public class AccessControl : FunctionModule
    {
        public AccessControl(IntPtr sdkContext, uint deviceID)
            : base(sdkContext, deviceID) { }

        public override List<(string Name, Action Action)> GetFunctionList()
        {
            return new List<(string, Action)> {
                ("Get all access groups", GetAllAccessGroups),
                ("Remove all access groups", RemoveAllAccessGroups),
                ("Set access groups", SetAccessGroups),
                // … etc.
            };
        }

        private void GetAllAccessGroups()
        {
            // initialise si besoin
            SupremaApi.BS2_Initialize();

            IntPtr listPtr;
            uint count;
            var code = (BS2ErrorCode)SupremaApi.BS2_GetAllAccessGroup(SdkContext, DeviceID, out listPtr, out count);
            if (code != BS2ErrorCode.BS_SDK_SUCCESS)
                throw new Exception($"Erreur BS2_GetAllAccessGroup: {code}");

            // décode les structures BS2AccessGroup en C#
            int structSize = Marshal.SizeOf<BS2AccessGroup>();
            for (int i = 0; i < count; i++)
            {
                var ptr = IntPtr.Add(listPtr, i * structSize);
                var group = Marshal.PtrToStructure<BS2AccessGroup>(ptr);
                // … ajoute dans un ViewModel, log, etc.
            }

            // libère si le SDK l’exige
            SupremaApi.BS2_Release();
        }

        private void RemoveAllAccessGroups()
        {
            var code = (BS2ErrorCode)SupremaApi.BS2_RemoveAllAccessGroup(SdkContext, DeviceID);
            if (code != BS2ErrorCode.BS_SDK_SUCCESS)
                throw new Exception($"Erreur BS2_RemoveAllAccessGroup: {code}");
        }

        private void SetAccessGroups()
        {
            // prépare une liste de BS2AccessGroup à partir de ton UI / modèle
            var groups = new List<BS2AccessGroup> { /* … */ };
            int size = Marshal.SizeOf<BS2AccessGroup>();
            IntPtr buffer = Marshal.AllocHGlobal(size * groups.Count);
            for (int i = 0; i < groups.Count; i++)
            {
                Marshal.StructureToPtr(groups[i], IntPtr.Add(buffer, i * size), false);
            }

            var code = (BS2ErrorCode)SupremaApi.BS2_SetAccessGroup(SdkContext, DeviceID, buffer, (uint)groups.Count);
            Marshal.FreeHGlobal(buffer);

            if (code != BS2ErrorCode.BS_SDK_SUCCESS)
                throw new Exception($"Erreur BS2_SetAccessGroup: {code}");
        }

        // … ajoute les autres méthodes (AccessLevel, Schedule, HolidayGroup…)
    }
}
