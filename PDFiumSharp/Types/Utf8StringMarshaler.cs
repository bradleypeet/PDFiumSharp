/* SDL2# - C# Wrapper for SDL2
 *
 * Copyright (c) 2013-2016 Ethan Lee.
 *
 * This software is provided 'as-is', without any express or implied warranty.
 * In no event will the authors be held liable for any damages arising from
 * the use of this software.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 *
 * 1. The origin of this software must not be misrepresented; you must not
 * claim that you wrote the original software. If you use this software in a
 * product, an acknowledgment in the product documentation would be
 * appreciated but is not required.
 *
 * 2. Altered source versions must be plainly marked as such, and must not be
 * misrepresented as being the original software.
 *
 * 3. This notice may not be removed or altered from any source distribution.
 *
 * Ethan "flibitijibibo" Lee <flibitijibibo@flibitijibibo.com>
 *
 */

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace PDFiumSharp.Types
{
    internal class Utf8StringMarshaler : ICustomMarshaler
    {
        public const string LeaveAllocated = "LeaveAllocated";

        private static readonly ICustomMarshaler leaveAllocatedInstance = new Utf8StringMarshaler(true);

        private static readonly ICustomMarshaler defaultInstance = new Utf8StringMarshaler(false);

        public static ICustomMarshaler GetInstance(string cookie)
        {
            switch (cookie)
            {
                case "LeaveAllocated":
                    return leaveAllocatedInstance;
                default:
                    return defaultInstance;
            }
        }

        private readonly bool leaveAllocated;

        public Utf8StringMarshaler(bool leaveAllocated) => this.leaveAllocated = leaveAllocated;

        public object MarshalNativeToManaged(IntPtr pNativeData)
        {
            if (pNativeData == IntPtr.Zero)
            {
                return null;
            }
            int len = 0;
            while (Marshal.ReadByte(pNativeData, len) != 0) ++len;
            byte[] buffer = new byte[len];
            Marshal.Copy(pNativeData, buffer, 0, buffer.Length);
            return Encoding.UTF8.GetString(buffer);
        }

        public IntPtr MarshalManagedToNative(object managedObj)
        {
            if (managedObj == null)
            {
                return IntPtr.Zero;
            }
            if (managedObj is string managedString)
            {
                int len = Encoding.UTF8.GetByteCount(managedString);
                byte[] buffer = new byte[len + 1];
                Encoding.UTF8.GetBytes(managedString, 0, managedString.Length, buffer, 0);
                IntPtr nativeUtf8 = Marshal.AllocHGlobal(buffer.Length);
                Marshal.Copy(buffer, 0, nativeUtf8, buffer.Length);
                return nativeUtf8;
            }
            else
            {
                throw new ArgumentException("ManagedObj must be a string.", "managedObj");
            }
        }

        public void CleanUpManagedData(object managedObj) { }

        public void CleanUpNativeData(IntPtr pNativeData)
        {
            if (!this.leaveAllocated)
            {
                Marshal.FreeHGlobal(pNativeData);
            }
        }

        public int GetNativeDataSize() => -1;
    }
}