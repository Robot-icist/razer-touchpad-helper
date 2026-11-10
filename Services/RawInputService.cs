using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using RazerTouchpadHelper.Models;
using NativeMethods = RazerTouchpadHelper.NativeMethods;

namespace RazerTouchpadHelper.Services
{
    public sealed class RawInputService
    {
        private readonly IntPtr _windowHandle;
        private readonly Action<List<Contact>> _onContactsUpdate;

        public RawInputService(IntPtr windowHandle, Action<List<Contact>> onContactsUpdate)
        {
            _windowHandle = windowHandle;
            _onContactsUpdate = onContactsUpdate;
            Register();
        }

        private void Register()
        {
            var devices = new[]
            {
                new NativeMethods.RAWINPUTDEVICE
                {
                    usUsagePage = 0x0D,
                    usUsage = 0x05,
                    dwFlags = NativeMethods.RIDEV_INPUTSINK,
                    hwndTarget = _windowHandle
                }
            };

            if (!NativeMethods.RegisterRawInputDevices(devices, (uint)devices.Length, (uint)Marshal.SizeOf<NativeMethods.RAWINPUTDEVICE>()))
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "Raw Input registration failed");
            }
        }

        public void ProcessMessage(IntPtr lParam)
        {
            uint size = 0;
            NativeMethods.GetRawInputData(lParam, NativeMethods.RID_INPUT, IntPtr.Zero, ref size, (uint)Marshal.SizeOf<NativeMethods.RAWINPUTHEADER>());

            var buffer = new byte[size];
            var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                NativeMethods.GetRawInputData(lParam, NativeMethods.RID_INPUT, handle.AddrOfPinnedObject(), ref size, (uint)Marshal.SizeOf<NativeMethods.RAWINPUTHEADER>());
                var rawInput = Marshal.PtrToStructure<NativeMethods.RAWINPUT>(handle.AddrOfPinnedObject());

                if (rawInput.header.dwType == NativeMethods.RIM_TYPEHID)
                {
                    int dataOffset = Marshal.SizeOf<NativeMethods.RAWINPUTHEADER>() + Marshal.SizeOf<NativeMethods.RAWHID>();
                    var hidData = new byte[rawInput.hid.dwSizeHid];
                    Array.Copy(buffer, dataOffset, hidData, 0, hidData.Length);
                    DecodeDigitizer(hidData);
                }
            }
            finally
            {
                handle.Free();
            }
        }

        private void DecodeDigitizer(byte[] b)
        {
            if (b.Length < 4) return;

            var contacts = new List<Contact>();

            for (int k = 0; k < TouchConstants.MaxContacts; k++)
            {
                int o = 4 + k * 5;
                if (o + 4 >= b.Length) break;

                int tip = (b[o] >> 1) & 1;
                int cid = (b[o] >> 4) & 0x0F;
                int x = b[o + 1] | (b[o + 2] << 8);
                int y = b[o + 3] | (b[o + 4] << 8);

                if (tip == 1)
                    contacts.Add(new Contact(cid, x, y));
            }

            _onContactsUpdate?.Invoke(contacts);
        }

        public bool GetPhysicalButtonState(byte[] b)
        {
            if (b.Length < 2) return false;
            return (b[1] & 1) != 0;
        }
    }
}