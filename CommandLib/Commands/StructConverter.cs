using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;

namespace Cmd
{
    public static class StructConverter
    {
        /// <summary>
        /// From structure convert to byte[]
        /// </summary>
        public static byte[] StructureToByte<T>(T structure)
        {
            int size = 0;
            byte[] buffer = null;
            IntPtr bufferIntPtr = IntPtr.Zero;
            try
            {
                size = Marshal.SizeOf(typeof(T));
                buffer = new byte[size];
                bufferIntPtr = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(structure, bufferIntPtr, true);
                Marshal.Copy(bufferIntPtr, buffer, 0, size);
            }
            finally
            {
                if (bufferIntPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(bufferIntPtr);
                }
            }
            return buffer;
        }

        /// <summary>
        /// From byte[] convert to structure
        /// </summary>
        public static T ByteToStructure<T>(byte[] dataBuffer)
        {
            object structure = null;
            int size = 0;
            IntPtr allocIntPtr = IntPtr.Zero;
            try
            {
                size = Marshal.SizeOf(typeof(T));
                allocIntPtr = Marshal.AllocHGlobal(size);
                Marshal.Copy(dataBuffer, 0, allocIntPtr, size);
                structure = Marshal.PtrToStructure(allocIntPtr, typeof(T));
            }
            finally
            {
                if (allocIntPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(allocIntPtr);
                }
            }
            return (T)structure;
        }

    }
}
