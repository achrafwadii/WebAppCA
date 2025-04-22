using System;
using System.Runtime.InteropServices;

namespace WebAppCA.SDK
{
    public static class Util
    {
        // Conversion d'un timestamp Unix en DateTime
        public static DateTime ConvertFromUnixTimestamp(double timestamp)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return origin.AddSeconds(timestamp).ToLocalTime();
        }

        // Conversion d'un DateTime en timestamp Unix
        public static double ConvertToUnixTimestamp(DateTime date)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan diff = date.ToUniversalTime() - origin;
            return Math.Floor(diff.TotalSeconds);
        }

        // Allocation d'une structure
        public static T AllocateStructure<T>() where T : struct
        {
            int size = Marshal.SizeOf(typeof(T));
            IntPtr ptr = Marshal.AllocHGlobal(size);

            try
            {
                T result = default(T);
                Marshal.StructureToPtr(result, ptr, false);
                result = (T)Marshal.PtrToStructure(ptr, typeof(T));
                return result;
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }
    }
}