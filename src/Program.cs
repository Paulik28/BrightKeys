using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;

namespace BrightKeys
{
    internal static class Program
    {
        private static Mutex _instanceMutex;

        [STAThread]
        private static void Main(string[] args)
        {
            Storage.EnsureDefaultConfig();
            I18n.Initialize();
            Storage.Log("Main started. args: " + string.Join(" ", args));

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += delegate(object s, ThreadExceptionEventArgs e) {
                Storage.Log("ThreadException: " + e.Exception);
            };
            AppDomain.CurrentDomain.UnhandledException += delegate(object s, UnhandledExceptionEventArgs e) {
                Storage.Log("UnhandledException: " + e.ExceptionObject);
            };
            AppInfo.CommandMessage = Native.RegisterWindowMessage("BrightKeys.Command.2026");
            try { Native.ChangeWindowMessageFilter(AppInfo.CommandMessage, Native.MSGFLT_ADD); } catch { }

            bool noOsd = false;
            bool quit = false;
            bool hotkeys = false;
            int action = 0;
            bool helper = false;

            foreach (string arg in args)
            {
                string a = arg.Trim().ToLowerInvariant();
                if (a == "--down") { action = 1; helper = true; }
                else if (a == "--up") { action = 2; helper = true; }
                else if (a == "--next") { action = 3; helper = true; }
                else if (a == "--nightlight" || a == "--toggle-nightlight") { action = 6; helper = true; }
                else if (a == "--no-osd") noOsd = true;
                else if (a == "--quit") quit = true;
                else if (a == "--hotkeys") hotkeys = true;
            }

            if (!helper && AppInfo.IsHelperExe)
            {
                helper = true;
                string name = System.IO.Path.GetFileNameWithoutExtension(Application.ExecutablePath).ToLowerInvariant();
                if (name.EndsWith("down")) action = 1;
                else if (name.EndsWith("up")) action = 2;
                else if (name.EndsWith("next")) action = 3;
                else if (name.EndsWith("nightlight")) action = 6;
            }

            Storage.EnsureDefaultConfig();

            if (helper)
            {
                Helper.Run(action, !noOsd);
                return;
            }
            if (quit)
            {
                Helper.Quit();
                return;
            }
            if (hotkeys)
            {
                string err;
                string text = SystemSetup.InstallHotkeys(out err)
                    ? "Tastele FN au fost configurate cu succes."
                    : "Nu am putut configura tastele FN:\r\n" + err;
                MessageBox.Show(text, "BrightKeys", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            RunTray();
        }

        private static void RunTray()
        {
            Storage.Log("RunTray acquiring mutex...");
            bool created;
            _instanceMutex = new Mutex(true, "Local\\BrightKeys_SingleInstance_2026", out created);
            Storage.Log("RunTray mutex created=" + created);
            if (!created)
            {
                IntPtr hwnd = Native.FindWindow(null, AppInfo.WindowTitle);
                Storage.Log("RunTray existing hwnd=" + hwnd);
                if (hwnd != IntPtr.Zero)
                    Native.PostMessage(hwnd, AppInfo.CommandMessage, (IntPtr)5, IntPtr.Zero);
                return;
            }

            TrayApp app = null;
            try
            {
                Storage.Log("RunTray creating TrayApp...");
                app = new TrayApp();
                Storage.Log("RunTray starting Application.Run...");
                Application.Run(app);
                Storage.Log("RunTray Application.Run finished.");
            }
            catch (Exception ex)
            {
                Storage.Log("fatal tray error: " + ex);
                MessageBox.Show("BrightKeys a întâmpinat o eroare:\r\n" + ex.Message,
                    "BrightKeys", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Storage.Log("RunTray cleanup.");
                if (app != null) app.DisposeIcon();
                try { _instanceMutex.ReleaseMutex(); } catch { }
                try { _instanceMutex.Dispose(); } catch { }
            }
        }
    }

    internal static class Helper
    {
        public static void Quit()
        {
            IntPtr hwnd = Native.FindWindow(null, AppInfo.WindowTitle);
            if (hwnd != IntPtr.Zero)
                Native.PostMessage(hwnd, AppInfo.CommandMessage, (IntPtr)4, IntPtr.Zero);
        }

        public static void Run(int action, bool showOsd)
        {
            try
            {
                IntPtr hwnd = Native.FindWindow(null, AppInfo.WindowTitle);
                if (hwnd != IntPtr.Zero)
                {
                    Native.PostMessage(hwnd, AppInfo.CommandMessage, (IntPtr)action, IntPtr.Zero);
                    return;
                }

                Storage.EnsureDefaultConfig();
                Target target = Storage.LoadTarget();
                int step = Storage.LoadStep();
                string title;
                string subtitle;
                List<OsdRow> rows = new List<OsdRow>();

                if (action == 6)
                {
                    NightLightService.Initialize();
                    bool enabled = NightLightService.Toggle();
                    title = "Lumină de noapte";
                    subtitle = enabled ? "Activat (F11)" : "Dezactivat (F11)";
                    rows.Add(new OsdRow
                    {
                        Label = enabled ? "Intensitate lumină caldă" : "Lumină de noapte dezactivată",
                        Highlight = enabled,
                        Percent = enabled ? (int?)NightLightService.Intensity : null
                    });
                    Storage.Log("helper toggle night light enabled=" + enabled);
                }
                else if (action == 3)
                {
                    List<Display> displays = BrightnessService.GetDisplays();
                    bool nl = Storage.LoadNightLightEnabled();
                    target = BrightnessService.NextTarget(target, displays.Count, nl);
                    Storage.SaveTarget(target);
                    title = "Alege monitorul";
                    subtitle = "Fn+F12 comută ținta";
                    rows = BrightnessService.SelectionRows(target, displays, nl);
                    Storage.Log("helper next target=" + target + " monitors=" + displays.Count);
                }
                else
                {
                    int delta = action == 1 ? -step : step;
                    if (target == Target.NightLight)
                    {
                        NightLightService.Initialize();
                        int newIntensity = NightLightService.AdjustIntensity(delta);
                        title = "Lumină de noapte";
                        subtitle = delta > 0 ? "Intensitate +" + delta + "%" : "Intensitate " + delta + "%";
                        rows.Add(new OsdRow
                        {
                            Label = "Intensitate lumină caldă",
                            Highlight = true,
                            Percent = newIntensity
                        });
                        Storage.Log("helper adjust night light delta=" + delta);
                    }
                    else
                    {
                        BrightnessService.AdjustAsync(target, delta, out title, out subtitle, out rows);
                        Storage.Log("helper adjust target=" + target + " delta=" + delta);
                    }
                }

                if (showOsd)
                {
                    using (OsdForm osd = new OsdForm(title, subtitle, rows, true))
                        Application.Run(osd);
                }
            }
            catch (Exception ex)
            {
                Storage.Log("helper error: " + ex);
            }
        }
    }
}
