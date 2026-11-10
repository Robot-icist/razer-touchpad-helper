using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using RazerTouchpadHelper;
using NativeMethods = RazerTouchpadHelper.NativeMethods;

namespace RazerTouchpadHelper.Services
{
    public sealed class InputInjectionService
    {
        public int InjectedClickCount { get; private set; }
        public int InjectedWheelCount { get; private set; }
        public bool LastClickScreenValid { get; private set; }
        public System.Drawing.Point LastClickScreenPosition { get; private set; }

        public void InjectClick(bool right, bool forceReleaseBeforeClick = true, int clickDelayMs = 0)
        {
            if (forceReleaseBeforeClick)
            {
                InjectMouseButton(NativeMethods.MOUSEEVENTF_LEFTUP);
                InjectMouseButton(NativeMethods.MOUSEEVENTF_RIGHTUP);
            }

            uint down = right ? NativeMethods.MOUSEEVENTF_RIGHTDOWN : NativeMethods.MOUSEEVENTF_LEFTDOWN;
            uint up = right ? NativeMethods.MOUSEEVENTF_RIGHTUP : NativeMethods.MOUSEEVENTF_LEFTUP;

            var inputs = new[]
            {
                CreateMouseInput(down),
                CreateMouseInput(up)
            };

            uint sent = NativeMethods.SendInput(2, inputs, Marshal.SizeOf<NativeMethods.INPUT>());
            if (sent == 2)
            {
                InjectedClickCount++;
                LastClickScreenValid = NativeMethods.GetCursorPos(out var p);
                if (LastClickScreenValid)
                {
                    LastClickScreenPosition = new System.Drawing.Point(p.X, p.Y);
                }
            }

            if (clickDelayMs > 0)
            {
                System.Threading.Thread.Sleep(clickDelayMs);
            }
        }

        public void InjectMouseButton(uint flags)
        {
            var input = CreateMouseInput(flags);
            NativeMethods.SendInput(1, new[] { input }, Marshal.SizeOf<NativeMethods.INPUT>());
        }

        public void InjectWheelDelta(int delta, bool horizontal)
        {
            if (delta == 0) return;

            var mi = new NativeMethods.MOUSEINPUT
            {
                mouseData = (uint)delta,
                dwFlags = horizontal ? NativeMethods.MOUSEEVENTF_HWHEEL : NativeMethods.MOUSEEVENTF_WHEEL
            };

            var input = new NativeMethods.INPUT
            {
                type = NativeMethods.INPUT_MOUSE,
                u = new NativeMethods.INPUTUNION { mi = mi }
            };

            NativeMethods.SendInput(1, new[] { input }, Marshal.SizeOf<NativeMethods.INPUT>());
            InjectedWheelCount++;
        }

        public void SendHotkey(params ushort[] keys)
        {
            var inputs = new List<NativeMethods.INPUT>();

            foreach (var k in keys)
            {
                inputs.Add(new NativeMethods.INPUT
                {
                    type = NativeMethods.INPUT_KEYBOARD,
                    u = new NativeMethods.INPUTUNION
                    {
                        ki = new NativeMethods.KEYBDINPUT { wVk = k }
                    }
                });
            }

            for (int i = keys.Length - 1; i >= 0; i--)
            {
                inputs.Add(new NativeMethods.INPUT
                {
                    type = NativeMethods.INPUT_KEYBOARD,
                    u = new NativeMethods.INPUTUNION
                    {
                        ki = new NativeMethods.KEYBDINPUT { wVk = keys[i], dwFlags = NativeMethods.KEYEVENTF_KEYUP }
                    }
                });
            }

            NativeMethods.SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf<NativeMethods.INPUT>());
        }

        public void SendHotkey((ushort, ushort) k) => SendHotkey(k.Item1, k.Item2);
        public void SendHotkey((ushort, ushort, ushort) k) => SendHotkey(k.Item1, k.Item2, k.Item3);

        private static NativeMethods.INPUT CreateMouseInput(uint flags)
        {
            return new NativeMethods.INPUT
            {
                type = NativeMethods.INPUT_MOUSE,
                u = new NativeMethods.INPUTUNION
                {
                    mi = new NativeMethods.MOUSEINPUT { dwFlags = flags }
                }
            };
        }
    }
}