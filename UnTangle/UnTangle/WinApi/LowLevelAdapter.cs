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
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace UnTangle.WinApi
{
    public static partial class LowLevelAdapter
    {

        public static IntPtr SetHook(int type, HookProc callback)
        {
            var process = Process.GetCurrentProcess();
            var module = process.MainModule;
            var handle = LowLevelAdapter.GetModuleHandle(module.ModuleName);
            return LowLevelAdapter.SetWindowsHookEx(type, callback, handle, 0);
        }
        public static void ReleaseHook(IntPtr id)
        {
            LowLevelAdapter.UnhookWindowsHookEx(id);
        }
        public static IntPtr GetNextHook(int nCode, IntPtr wParam, IntPtr lParam)
        {
            return LowLevelAdapter.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        public static bool IsKeyPressed(Keys keyCode)
        {
            return (LowLevelAdapter.GetKeyState((int)keyCode) & 0x8000) == 0x8000;
        }

        private static IntPtr GetFocusedHandle()
        {
            var threadId = LowLevelAdapter.GetCurrentThreadId();
            var wndThreadId = GetWindowThreadProcessId(LowLevelAdapter.GetForegroundWindow(), IntPtr.Zero);

            if (threadId == wndThreadId)
            {
                return IntPtr.Zero;
            }

            LowLevelAdapter.AttachThreadInput(wndThreadId, threadId, true);
            IntPtr focusedHandle = LowLevelAdapter.GetFocus();
            LowLevelAdapter.AttachThreadInput(wndThreadId, threadId, false);
            return focusedHandle;
        }

        public static IntPtr GetCurrentLayout()
        {
            var wndThreadId = GetWindowThreadProcessId(LowLevelAdapter.GetForegroundWindow(), IntPtr.Zero);
            return LowLevelAdapter.GetKeyboardLayout(wndThreadId);
        }


        public static void SetNextKeyboardLayout()
        {
            IntPtr hWnd = IntPtr.Zero;
            var threadId = GetWindowThreadProcessId(LowLevelAdapter.GetForegroundWindow(), IntPtr.Zero);
            var info = new GUITHREADINFO();
            info.cbSize = Marshal.SizeOf(info);
            var success = LowLevelAdapter.GetGUIThreadInfo(threadId, ref info);

            // target = hwndCaret || hwndFocus || (AttachThreadInput + GetFocus) || hwndActive || GetForegroundWindow
            var focusedHandle = GetFocusedHandle();
            if (success)
            {
                if (info.hwndCaret != IntPtr.Zero)
                {
                    hWnd = info.hwndCaret;
                }
                else if (info.hwndFocus != IntPtr.Zero)
                {
                    hWnd = info.hwndFocus;
                }
                else if (focusedHandle != IntPtr.Zero)
                {
                    hWnd = focusedHandle;
                }
                else if (info.hwndActive != IntPtr.Zero)
                {
                    hWnd = info.hwndActive;
                }
            }
            else
            {
                hWnd = focusedHandle;
            }
            if(hWnd == IntPtr.Zero) { hWnd = LowLevelAdapter.GetForegroundWindow();  }

            //PostMessage(hWnd, WM_INPUTLANGCHANGEREQUEST, INPUTLANGCHANGE_FORWARD, HKL_NEXT);

            var shiftDown = LowLevelAdapter.MakeKeyInput(Keys.LShiftKey, true);
            var shiftUp = LowLevelAdapter.MakeKeyInput(Keys.LShiftKey, false);
            var ctrlDown = LowLevelAdapter.MakeKeyInput(Keys.LControlKey, true);
            var ctrlUp = LowLevelAdapter.MakeKeyInput(Keys.LControlKey, false);

            LowLevelAdapter.SendInput(2, new INPUT[2] { ctrlDown, shiftDown }, Marshal.SizeOf(typeof(INPUT)));
            Thread.Sleep(300);
            LowLevelAdapter.SendInput(2, new INPUT[2] { ctrlUp, shiftUp }, Marshal.SizeOf(typeof(INPUT)));
        }

        public static void SendCopy()
        {
            var ctrlDown = LowLevelAdapter.MakeKeyInput(Keys.LControlKey, true);
            var ctrlUp = LowLevelAdapter.MakeKeyInput(Keys.LControlKey, false);
            var cDown = LowLevelAdapter.MakeKeyInput(Keys.C, true);
            var cUp = LowLevelAdapter.MakeKeyInput(Keys.C, false);
            LowLevelAdapter.SendInput(2, new INPUT[2] { ctrlDown, cDown }, Marshal.SizeOf(typeof(INPUT)));
            Thread.Sleep(20);
            LowLevelAdapter.SendInput(2, new INPUT[2] { ctrlUp, cUp }, Marshal.SizeOf(typeof(INPUT)));
        }

        public static void SendKeyPress(Keys vkCode, bool shift = false)
        {
            var down = LowLevelAdapter.MakeKeyInput(vkCode, true);
            var up = LowLevelAdapter.MakeKeyInput(vkCode, false);

            if (shift)
            {
                var shiftDown = LowLevelAdapter.MakeKeyInput(Keys.ShiftKey, true);
                var shiftUp = LowLevelAdapter.MakeKeyInput(Keys.ShiftKey, false);
                LowLevelAdapter.SendInput(4, new INPUT[4] { shiftDown, down, up, shiftUp }, Marshal.SizeOf(typeof(INPUT)));
            }
            else
            {
                LowLevelAdapter.SendInput(2, new INPUT[2] { down, up }, Marshal.SizeOf(typeof(INPUT)));
            }

        }

        public static void ReleasePressedFnKeys()
        {
            // temp solution
            //ReleasePressedKey(Keys.LMenu, true),
            //ReleasePressedKey(Keys.RMenu, true),
            //ReleasePressedKey(Keys.LWin, true),
            //ReleasePressedKey(Keys.RWin, true),
            ReleasePressedKey(Keys.RControlKey, false);
            ReleasePressedKey(Keys.LControlKey, false);
            ReleasePressedKey(Keys.LShiftKey, false);
            ReleasePressedKey(Keys.RShiftKey, false);
        }

        public static bool ReleasePressedKey(Keys keyCode, bool releaseTwice)
        {
            if (!IsKeyPressed(keyCode)) { return false; }
            //Debug.WriteLine("{0} was down", keyCode);
            var keyUp = LowLevelAdapter.MakeKeyInput(keyCode, false);
            if (releaseTwice)
            {
                var secondDown = LowLevelAdapter.MakeKeyInput(keyCode, true);
                var secondUp = LowLevelAdapter.MakeKeyInput(keyCode, false);
                LowLevelAdapter.SendInput(3, new INPUT[3] { keyUp, secondDown, secondUp }, Marshal.SizeOf(typeof(INPUT)));
            }
            else
            {
                LowLevelAdapter.SendInput(1, new INPUT[1] { keyUp }, Marshal.SizeOf(typeof(INPUT)));
            }
            return true;
        }

        public static void SendShowSettingsMessage()
        {
            LowLevelAdapter.PostMessage((IntPtr)LowLevelAdapter.HWND_BROADCAST, LowLevelAdapter.WM_SHOW_SETTINGS, 0, 0);
        }


        private static string GetAutorunPath()
        {
            return System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                "dotSwitcher.lnk");
        }

        //static Dictionary<string, object> lBackup = new Dictionary<string, object>();
        //static IDataObject lDataObject = null;
        //static string[] lFormats = new string[] {};

        public static Keys ToKey(char ch)
        {
            var layout = GetCurrentLayout();

            short keyNumber = LowLevelAdapter.VkKeyScanEx(ch, layout);
            if (keyNumber == -1)
            {
                return System.Windows.Forms.Keys.None;
            }
            return (System.Windows.Forms.Keys)(((keyNumber & 0xFF00) << 8) | (keyNumber & 0xFF));
        }
        public static void BackupClipboard()
        {
            //lDataObject = Clipboard.GetDataObject();
            //if (lDataObject == null) 
            //{
            //    return;
            //}
            //lFormats = lDataObject.GetFormats(false);
            //lBackup = new Dictionary<string, object>();
            //foreach(var lFormat in lFormats)
            //{
            //  lBackup.Add(lFormat, lDataObject.GetData(lFormat, false));
            //}
            //Debug.WriteLine(lDataObject);
            //Debug.WriteLine(lFormats);
        }

        public static void RestoreClipboard()
        {
            //Debug.WriteLine(lDataObject);
            //Debug.WriteLine(lFormats);
            //if (lDataObject == null)
            //{
            //    return;
            //}
            //foreach (var lFormat in lFormats)
            //{
            //    lDataObject.SetData(lBackup[lFormat]);
            //}
            //Clipboard.SetDataObject(lDataObject);
        }

    }
}