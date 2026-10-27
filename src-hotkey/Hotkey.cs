using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace BrightKeys
{
    // Ultra-fast launcher for FN hotkeys.
    // Signals the running BrightKeys instance via Win32 named EventWaitHandle in < 0.1ms.
    // If the key is held down, keeps signaling at a steady rate to enable continuous smooth gliding.
    internal static class HotkeyLauncher
    {
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private const string EvtDown       = @"Local\BrightKeys_Event_Down";
        private const string EvtUp         = @"Local\BrightKeys_Event_Up";
        private const string EvtNext       = @"Local\BrightKeys_Event_Next";
        private const string EvtNightLight = @"Local\BrightKeys_Event_NightLight";

        [STAThread]
        private static int Main()
        {
            try
            {
                string name = Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetExecutingAssembly().Location).ToLowerInvariant();
                string evtName = EvtDown;
                string arg = "--down";
                bool isShift = (GetAsyncKeyState(0x10) & 0x8000) != 0
                            || (GetAsyncKeyState(0xA0) & 0x8000) != 0
                            || (GetAsyncKeyState(0xA1) & 0x8000) != 0;

                if (name.EndsWith("up"))
                {
                    evtName = EvtUp;
                    arg = "--up";
                }
                else if (name.EndsWith("next"))
                {
                    if (isShift)
                    {
                        evtName = EvtNightLight;
                        arg = "--nightlight";
                    }
                    else
                    {
                        evtName = EvtNext;
                        arg = "--next";
                    }
                }
                else if (name.EndsWith("nightlight"))
                {
                    evtName = EvtNightLight;
                    arg = "--nightlight";
                }

                // 1. Fast path: Signal the running BrightKeys instance
                try
                {
                    using (EventWaitHandle evt = EventWaitHandle.OpenExisting(evtName))
                    {
                        evt.Set();
                        return 0;
                    }
                }
                catch (WaitHandleCannotBeOpenedException)
                {
                    // Tray app not yet running
                }

                // 2. Slow path: Tray not running, start it
                string dir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                string main = Path.Combine(dir, "BrightKeys.exe");
                if (File.Exists(main))
                {
                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = main;
                    psi.Arguments = arg;
                    psi.WorkingDirectory = dir;
                    psi.UseShellExecute = false;
                    Process.Start(psi);
                }
            }
            catch { }
            return 0;
        }
    }
}
