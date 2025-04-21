
    using System;
    using System.Collections.Generic;

    namespace WebAppCA.SDK
    {
        public abstract class FunctionModule
        {
            protected IntPtr SdkContext { get; private set; }
            protected uint DeviceID { get; private set; }

            public FunctionModule(IntPtr sdkContext, uint deviceID)
            {
                SdkContext = sdkContext;
                DeviceID = deviceID;
            }

            /// <summary>
            /// Renvoie la liste des actions disponibles pour ce module.
            /// </summary>
            public abstract List<(string Name, Action Action)> GetFunctionList();
        }
    }


