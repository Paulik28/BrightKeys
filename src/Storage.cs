using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

namespace BrightKeys
{
    internal static class AppInfo
    {
        public const string Name = "BrightKeys";
        public const string Version = "1.0";
        public const string WindowTitle = "BrightKeys.MainWindow";
        public static readonly string ExePath = Application.ExecutablePath;
        public static readonly string ExeDir = Path.GetDirectoryName(Application.ExecutablePath);
        public static int CommandMessage;
        public static bool IsHelperExe
        {
            get
            {
                string n = Path.GetFileNameWithoutExtension(Application.ExecutablePath).ToLowerInvariant();
                return n.EndsWith("down") || n.EndsWith("up") || n.EndsWith("next") || n.EndsWith("nightlight");
            }
        }
    }

    internal static class Storage
    {
        public static readonly string DataDir = ResolveDataDir();
        public static readonly string ConfigPath = Path.Combine(DataDir, "config.ini");
        public static readonly string StatePath = Path.Combine(DataDir, "state.ini");
        public static readonly string LogPath = Path.Combine(DataDir, "log.txt");

        private static string ResolveDataDir()
        {
            try
            {
                string custom = Environment.GetEnvironmentVariable("BRIGHTKEYS_DATA");
                if (!string.IsNullOrEmpty(custom)) return custom;
            }
            catch { }
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BrightKeys");
        }

        public static void EnsureDataDir()
        {
            try { Directory.CreateDirectory(DataDir); } catch { }
        }

        public static void Log(string message)
        {
            try
            {
                EnsureDataDir();
                File.AppendAllText(LogPath,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "  " + message + "\r\n");
            }
            catch { }
        }

        private static readonly object _configFileLock = new object();

        public static void EnsureDefaultConfig()
        {
            try
            {
                EnsureDataDir();
                if (!File.Exists(ConfigPath))
                    File.WriteAllText(ConfigPath, "Step=5\r\nLanguage=ro\r\n");
            }
            catch { }
        }

        public static string ReadConfigValue(string key, string defaultValue)
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    foreach (string line in File.ReadAllLines(ConfigPath))
                    {
                        string[] kv = line.Split(new char[] { '=' }, 2);
                        if (kv.Length == 2 && kv[0].Trim().Equals(key, StringComparison.OrdinalIgnoreCase))
                            return kv[1].Trim();
                    }
                }
            }
            catch { }
            return defaultValue;
        }

        public static void WriteConfigValue(string key, string value)
        {
            try
            {
                EnsureDataDir();
                lock (_configFileLock)
                {
                    System.Collections.Generic.Dictionary<string, string> dict =
                        new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    if (File.Exists(ConfigPath))
                    {
                        foreach (string line in File.ReadAllLines(ConfigPath))
                        {
                            string[] kv = line.Split(new char[] { '=' }, 2);
                            if (kv.Length == 2)
                                dict[kv[0].Trim()] = kv[1].Trim();
                        }
                    }
                    dict[key] = value;
                    System.Collections.Generic.List<string> lines = new System.Collections.Generic.List<string>();
                    foreach (System.Collections.Generic.KeyValuePair<string, string> kv in dict)
                        lines.Add(kv.Key + "=" + kv.Value);
                    File.WriteAllLines(ConfigPath, lines.ToArray());
                }
            }
            catch { }
        }

        public static int LoadStep()
        {
            string val = ReadConfigValue("Step", "5");
            int v;
            if (int.TryParse(val, out v)) return Math.Max(1, Math.Min(50, v));
            return 5;
        }

        public static void SaveStep(int step)
        {
            WriteConfigValue("Step", step.ToString());
        }

        public static string LoadLanguage()
        {
            string val = ReadConfigValue("Language", "ro").ToLowerInvariant();
            return val == "en" ? "en" : "ro";
        }

        public static void SaveLanguage(string lang)
        {
            WriteConfigValue("Language", lang);
        }

        public static void LoadCustomHotkeys(out bool enabled, out string down, out string up, out string next, out string nl)
        {
            string enVal = ReadConfigValue("CustomHotkeysEnabled", "0");
            enabled = enVal == "1" || enVal.Equals("true", StringComparison.OrdinalIgnoreCase);
            down = ReadConfigValue("HotkeyDown", "Ctrl+Alt+F9");
            up = ReadConfigValue("HotkeyUp", "Ctrl+Alt+F10");
            next = ReadConfigValue("HotkeyNext", "Ctrl+Alt+F12");
            nl = ReadConfigValue("HotkeyNightLight", "Ctrl+Alt+N");
        }

        public static void SaveCustomHotkeys(bool enabled, string down, string up, string next, string nl)
        {
            WriteConfigValue("CustomHotkeysEnabled", enabled ? "1" : "0");
            WriteConfigValue("HotkeyDown", down ?? "Ctrl+Alt+F9");
            WriteConfigValue("HotkeyUp", up ?? "Ctrl+Alt+F10");
            WriteConfigValue("HotkeyNext", next ?? "Ctrl+Alt+F12");
            WriteConfigValue("HotkeyNightLight", nl ?? "Ctrl+Alt+N");
        }

        private static readonly object _stateFileLock = new object();

        private static string ReadStateValue(string key, string defaultValue)
        {
            try
            {
                if (File.Exists(StatePath))
                {
                    foreach (string line in File.ReadAllLines(StatePath))
                    {
                        string[] kv = line.Split(new char[] { '=' }, 2);
                        if (kv.Length == 2 && kv[0].Trim().Equals(key, StringComparison.OrdinalIgnoreCase))
                            return kv[1].Trim();
                    }
                }
            }
            catch { }
            return defaultValue;
        }

        private static void WriteStateValue(string key, string value)
        {
            try
            {
                EnsureDataDir();
                lock (_stateFileLock)
                {
                    System.Collections.Generic.Dictionary<string, string> dict =
                        new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    if (File.Exists(StatePath))
                    {
                        foreach (string line in File.ReadAllLines(StatePath))
                        {
                            string[] kv = line.Split(new char[] { '=' }, 2);
                            if (kv.Length == 2)
                                dict[kv[0].Trim()] = kv[1].Trim();
                        }
                    }
                    dict[key] = value;
                    System.Collections.Generic.List<string> lines = new System.Collections.Generic.List<string>();
                    foreach (System.Collections.Generic.KeyValuePair<string, string> kv in dict)
                        lines.Add(kv.Key + "=" + kv.Value);
                    File.WriteAllLines(StatePath, lines.ToArray());
                }
            }
            catch { }
        }

        public static Target LoadTarget()
        {
            string v = ReadStateValue("Target", "Both").ToLowerInvariant();
            if (v == "1" || v == "monitor1") return Target.Monitor1;
            if (v == "2" || v == "monitor2") return Target.Monitor2;
            if (v == "3" || v == "nightlight") return Target.NightLight;
            return Target.Both;
        }

        public static void SaveTarget(Target target)
        {
            try
            {
                string value = target == Target.Monitor1 ? "1"
                             : target == Target.Monitor2 ? "2"
                             : target == Target.NightLight ? "NightLight"
                             : "Both";
                WriteStateValue("Target", value);
            }
            catch { }
        }

        public static bool LoadNightLightEnabled()
        {
            string val = ReadStateValue("NightLightEnabled", "0");
            return val == "1" || val.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        public static void SaveNightLightEnabled(bool enabled)
        {
            WriteStateValue("NightLightEnabled", enabled ? "1" : "0");
        }

        public static int LoadNightLightIntensity()
        {
            return LoadNightLightIntensity1();
        }

        public static int LoadNightLightIntensity1()
        {
            string val = ReadStateValue("NightLightIntensity1", "");
            if (string.IsNullOrEmpty(val))
                val = ReadStateValue("NightLightIntensity", "50");
            int v;
            if (int.TryParse(val, out v))
                return Math.Max(0, Math.Min(100, v));
            return 50;
        }

        public static int LoadNightLightIntensity2()
        {
            string val = ReadStateValue("NightLightIntensity2", "");
            if (string.IsNullOrEmpty(val))
                val = ReadStateValue("NightLightIntensity", "50");
            int v;
            if (int.TryParse(val, out v))
                return Math.Max(0, Math.Min(100, v));
            return 50;
        }

        public static void SaveNightLightIntensity(int intensity)
        {
            SaveNightLightIntensities(intensity, intensity);
        }

        public static void SaveNightLightIntensities(int intensity1, int intensity2)
        {
            WriteStateValue("NightLightIntensity1", intensity1.ToString());
            WriteStateValue("NightLightIntensity2", intensity2.ToString());
            WriteStateValue("NightLightIntensity", intensity1.ToString());
        }
    }

    internal static class SystemSetup
    {
        public const string AppKeyBase = @"Software\Microsoft\Windows\CurrentVersion\Explorer\AppKey";
        public const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        public const string RunValueName = "BrightKeys";

        public static readonly string[] HelperNames = { "BrightKeysDown.exe", "BrightKeysUp.exe", "BrightKeysNext.exe", "BrightKeysNightLight.exe" };
        public static readonly string[] AppKeyIds = { "15", "7", "18" };

        public static string DownPath { get { return Path.Combine(AppInfo.ExeDir, HelperNames[0]); } }
        public static string UpPath { get { return Path.Combine(AppInfo.ExeDir, HelperNames[1]); } }
        public static string NextPath { get { return Path.Combine(AppInfo.ExeDir, HelperNames[2]); } }

        public static void EnsureHelperCopies()
        {
            if (AppInfo.IsHelperExe) return;
            try
            {
                foreach (string name in HelperNames)
                {
                    string target = Path.Combine(AppInfo.ExeDir, name);
                    if (!File.Exists(target))
                        File.Copy(AppInfo.ExePath, target, true);
                }
            }
            catch (Exception ex) { Storage.Log("EnsureHelperCopies: " + ex.Message); }
        }

        private static string GetAppKeyValue(string id)
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(AppKeyBase + "\\" + id))
                {
                    if (k == null) return "";
                    object v = k.GetValue("ShellExecute");
                    return v == null ? "" : v.ToString();
                }
            }
            catch { return ""; }
        }

        public static bool HotkeysMatch()
        {
            return string.Equals(GetAppKeyValue("15"), DownPath, StringComparison.OrdinalIgnoreCase)
                && string.Equals(GetAppKeyValue("7"), UpPath, StringComparison.OrdinalIgnoreCase)
                && string.Equals(GetAppKeyValue("18"), NextPath, StringComparison.OrdinalIgnoreCase)
                && File.Exists(DownPath) && File.Exists(UpPath) && File.Exists(NextPath);
        }

        public static string GetHotkeysStatus()
        {
            return "Fn+F9  -> " + GetAppKeyValue("15") + "\r\n"
                 + "Fn+F10 -> " + GetAppKeyValue("7") + "\r\n"
                 + "Fn+F12 -> " + GetAppKeyValue("18");
        }

        public static bool InstallHotkeys(out string error)
        {
            error = "";
            try
            {
                EnsureHelperCopies();
                using (RegistryKey baseKey = Registry.CurrentUser.CreateSubKey(AppKeyBase))
                {
                    if (baseKey == null)
                    {
                        error = "Nu pot deschide registrul AppKey.";
                        return false;
                    }
                    WriteAppKey(baseKey, "15", DownPath);
                    WriteAppKey(baseKey, "7", UpPath);
                    WriteAppKey(baseKey, "18", NextPath);

                    // remove the temporary test mappings used while diagnosing the keyboard
                    foreach (string id in new string[] { "16", "17" })
                    {
                        try
                        {
                            using (RegistryKey k = baseKey.OpenSubKey(id))
                            {
                                if (k != null)
                                {
                                    object v = k.GetValue("ShellExecute");
                                    string s = v == null ? "" : v.ToString();
                                    if (s.IndexOf("\\probe\\", StringComparison.OrdinalIgnoreCase) >= 0)
                                        baseKey.DeleteSubKeyTree(id, false);
                                }
                            }
                        }
                        catch { }
                    }
                }
                Storage.Log("InstallHotkeys OK: down=" + DownPath + "; up=" + UpPath + "; next=" + NextPath);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                Storage.Log("InstallHotkeys FAILED: " + ex.Message);
                return false;
            }
        }

        private static void WriteAppKey(RegistryKey baseKey, string id, string value)
        {
            using (RegistryKey k = baseKey.CreateSubKey(id))
            {
                if (k == null) throw new IOException("Nu pot crea cheia AppKey\\" + id);
                k.SetValue("ShellExecute", value, RegistryValueKind.String);
            }
        }

        public static bool IsStartupEnabled()
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(RunKey))
                {
                    if (k == null) return false;
                    object v = k.GetValue(RunValueName);
                    if (v == null) return false;
                    string s = v.ToString().Trim('"');
                    return string.Equals(s, AppInfo.ExePath, StringComparison.OrdinalIgnoreCase);
                }
            }
            catch { return false; }
        }

        public static bool SetStartup(bool enabled, out string error)
        {
            error = "";
            try
            {
                using (RegistryKey k = Registry.CurrentUser.CreateSubKey(RunKey))
                {
                    if (k == null)
                    {
                        error = "Nu pot deschide cheia Run.";
                        return false;
                    }
                    if (enabled)
                        k.SetValue(RunValueName, "\"" + AppInfo.ExePath + "\"", RegistryValueKind.String);
                    else
                        k.DeleteValue(RunValueName, false);
                }
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static void OpenMonitorian()
        {
            string[] candidates = new string[]
            {
                @"D:\PROGRAME\Monitorian\Monitorian.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Monitorian", "Monitorian.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Monitorian", "Monitorian.exe")
            };
            foreach (string c in candidates)
            {
                try
                {
                    if (File.Exists(c))
                    {
                        Process.Start(c);
                        return;
                    }
                }
                catch { }
            }
            try
            {
                string lnk = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
                    @"Programs\Monitorian.lnk");
                if (File.Exists(lnk))
                {
                    Process.Start(new ProcessStartInfo(lnk) { UseShellExecute = true });
                    return;
                }
            }
            catch { }
            MessageBox.Show("Nu am găsit Monitorian. Îl poți deschide manual din meniul Start.",
                AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
