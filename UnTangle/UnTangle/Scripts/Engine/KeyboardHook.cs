/**
 *   DotSwitcher: a simple keyboard layout switcher
 *   Copyright (C) 2014-2019 Kirill Mokhovtsev / kurumpa
 *   Contact: kiev.programmer@gmail.com
 *
 *   This program is free software: you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation, either version 3 of the License, or
 *   (at your option) any later version.
 *
 *   This program is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *   GNU General Public License for more details.
 *
 *   You should have received a copy of the GNU General Public License
 *   along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using UnTangle.WinApi;

namespace UnTangle.Scripts.Engine
{
    public sealed class KeyboardHook : IDisposable
    {
        public event EventHandler<KeyboardEventArgs> KeyboardEvent;
        private IntPtr hookId = IntPtr.Zero;
        private HookProc keyboardEventHook;

        public KeyboardHook()
        {
            // prevents the hook method from being GCed: do not remove this member.
            keyboardEventHook = KeyboardEventHook;
        }
        public bool IsStarted()
        {
            return hookId != IntPtr.Zero;
        }
        public void Start()
        {
            if (IsStarted()) { return; }
            hookId = LowLevelAdapter.SetHook(LowLevelAdapter.WH_KEYBOARD_LL, keyboardEventHook);
        }
        public void Stop()
        {
            if (!IsStarted()) { return; }
            LowLevelAdapter.ReleaseHook(hookId);
            hookId = IntPtr.Zero;
        }
        private void OnKeyboardEvent(KeyboardEventArgs e)
        {
            if (KeyboardEvent != null)
            {
                KeyboardEvent(this, e);
            }
        }


        private IntPtr KeyboardEventHook(int nCode, IntPtr wParam, IntPtr lParam)
        {
            bool isHandled;
            ProcessKeyboardEvent(nCode, wParam, lParam, out isHandled);
            return isHandled?
                new IntPtr(1) :
                LowLevelAdapter.GetNextHook(nCode, wParam, lParam);
        }

        // returns true if event is handled
        private void ProcessKeyboardEvent(int nCode, IntPtr wParam, IntPtr lParam, out bool isHandled)
        {
            isHandled = false;
            try
            {

                if (nCode < 0)
                    return;

                bool isKeyDownEvent = false;
                switch (wParam.ToInt32())
                {
                    case LowLevelAdapter.WM_KEYDOWN:
                    case LowLevelAdapter.WM_SYSKEYDOWN:
                        isKeyDownEvent = true;
                        goto case LowLevelAdapter.WM_KEYUP;

                    case LowLevelAdapter.WM_KEYUP:
                    case LowLevelAdapter.WM_SYSKEYUP:

                        var keybdinput = (KEYBDINPUT)Marshal.PtrToStructure(lParam, typeof(KEYBDINPUT));
                        var keyData = (Keys)keybdinput.Vk;

                        keyData |= LowLevelAdapter.IsKeyPressed(Keys.ControlKey) ? Keys.Control : 0;
                        keyData |= LowLevelAdapter.IsKeyPressed(Keys.Menu) ? Keys.Alt : 0;
                        keyData |= LowLevelAdapter.IsKeyPressed(Keys.ShiftKey) ? Keys.Shift : 0;

                        var winPressed = LowLevelAdapter.IsKeyPressed(Keys.LWin) || LowLevelAdapter.IsKeyPressed(Keys.RWin);

                        var args = new KeyboardEventArgs(keyData, winPressed, isKeyDownEvent ? KeyboardEventType.KeyDown : KeyboardEventType.KeyUp);
                        OnKeyboardEvent(args);

                        isHandled = args.Handled;
                        break;
                }

            }
            catch { }
        }


        public void Dispose()
        {
            Stop();
        }
    }
}
