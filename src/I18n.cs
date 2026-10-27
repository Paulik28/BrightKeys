using System;
using System.Collections.Generic;

namespace BrightKeys
{
    internal static class I18n
    {
        private static string _currentLang = "ro";
        private static readonly object _lock = new object();

        public static event Action LanguageChanged;

        private static readonly Dictionary<string, string> Ro = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "BothMonitors", "Ambele monitoare" },
            { "Monitor1", "Monitor 1" },
            { "Monitor2", "Monitor 2" },
            { "NightLight", "Lumină de noapte" },
            { "NightLightWarmth", "Intensitate lumină caldă" },
            { "SelectDisplay", "Alege monitorul" },
            { "TargetForHotkeys", "Ținta pentru Fn+F9 / Fn+F10" },
            { "CycleTargetHint", "Fn+F12 comută ținta" },
            { "Brightness", "Luminozitate" },
            { "BrightnessDown", "Luminozitate -" },
            { "BrightnessUp", "Luminozitate +" },
            { "BrightnessDelta", "Luminozitate {0}%" },
            { "Intensity", "Intensitate" },
            { "IntensityDown", "Intensitate -" },
            { "IntensityUp", "Intensitate +" },
            { "IntensityDelta", "Intensitate {0}%" },
            { "NightLightEnabled", "Lumină de noapte (Activat)" },
            { "NightLightDisabled", "Lumină de noapte (Dezactivat)" },
            { "Enabled", "Activat" },
            { "Disabled", "Dezactivat" },
            { "PressToEnableNightLight", "Apasă Shift+Fn+F12 pentru a activa" },
            { "NightLightTurnedOff", "Lumină de noapte dezactivată" },
            { "BrightnessMinusStep", "Luminozitate -{0}%" },
            { "BrightnessPlusStep", "Luminozitate +{0}%" },
            { "IntensityMinusStep", "Intensitate -{0}%" },
            { "IntensityPlusStep", "Intensitate +{0}%" },
            { "MenuSettings", "Setări..." },
            { "MenuLanguage", "Limbă / Language" },
            { "MenuSetupHotkeys", "Configurare taste FN" },
            { "MenuStartup", "Pornește la logon" },
            { "MenuAbout", "Despre / Ajutor" },
            { "MenuExit", "Ieșire" },
            { "TrayTooltip", "BrightKeys - Fn+F9 / Fn+F10 / Fn+F12 (Shift=NightLight)" },
            { "StartupBalloonTitle", "BrightKeys" },
            { "StartupBalloonText", "Fn+F9/F10 = Luminozitate   Fn+F12 = Schimbă monitorul\r\nShift+Fn+F12 = Night Light On/Off   Shift+Fn+F9/F10 = Intensitate Night Light" },
            { "StatusBalloonTarget", "Țintă: {0}\r\n{1}\r\nLumină de noapte: {2}" },
            { "AboutDialogTitle", "Despre BrightKeys" },
            { "AboutDialogText", "BrightKeys 1.0\r\nControl luminozitate cu tastele FN pentru ambele monitoare.\r\n\r\nTaste luminozitate monitoare:\r\n  Fn+F9  = scade luminozitatea\r\n  Fn+F10 = crește luminozitatea\r\n  Fn+F12 = comută monitorul (Ambele → Monitor 1 → Monitor 2)\r\n\r\nLumină de noapte (Night Light):\r\n  Shift + Fn+F12 = activează / dezactivează Lumina de noapte\r\n  Shift + Fn+F9  = scade intensitatea luminii de noapte\r\n  Shift + Fn+F10 = crește intensitatea luminii de noapte\r\n\r\nȚine apăsat pe Fn+F9 sau Fn+F10 pentru o glisare fluidă și continuă (60 FPS).\r\nCând ții Shift reglează Lumina de noapte; când dai drumul la Shift reglează luminozitatea!" },
            { "SettingsTitle", "BrightKeys - Setări" },
            { "TabGeneral", "General" },
            { "TabNightLight", "Lumină de noapte" },
            { "TabHotkeys", "Taste & Scurtături" },
            { "LanguageLabel", "Limbă interfață:" },
            { "StepLabel", "Pas de reglare (% la fiecare apăsare):" },
            { "StartupLabel", "Pornește automat la autentificare în Windows" },
            { "NightLightEnableLabel", "Activează funcția Lumină de noapte" },
            { "NightLightDesc", "Intensitatea luminii calde poate fi reglată individual pentru fiecare monitor sau simultan prin Fn+F12." },
            { "Monitor1Warmth", "Căldură Monitor 1:" },
            { "Monitor2Warmth", "Căldură Monitor 2:" },
            { "HotkeysDescAppKey", "Taste FN Multimedia (Laptop / Hardware AppKey):" },
            { "HotkeysDescAppKeyInfo", "Fn+F9 = Scade luminozitatea\r\nFn+F10 = Crește luminozitatea\r\nFn+F12 = Schimbă monitorul (Ambele → Mon 1 → Mon 2)\r\nShift+Fn+F12 = On/Off Lumină noapte\r\nShift+Fn+F9/F10 = Intensitate Lumină noapte" },
            { "BtnReinstallAppKeys", "Reînregistrează tastele FN implicite" },
            { "HotkeysCustomTitle", "Scurtături globale personalizate (Universal):" },
            { "HotkeysCustomInfo", "Funcționează pe orice tastatură (fără conflicte hardware FN):" },
            { "EnableCustomHotkeys", "Activează tastele rapide globale personalizate" },
            { "KeyBrightnessDown", "Scade luminozitatea:" },
            { "KeyBrightnessUp", "Crește luminozitatea:" },
            { "KeyNextDisplay", "Comută monitorul:" },
            { "KeyNightLight", "Comută Lumină de noapte:" },
            { "BtnResetDefaults", "Resetează la implicite" },
            { "BtnSave", "Salvează" },
            { "BtnCancel", "Anulează" },
            { "SavedSuccess", "Setările au fost salvate cu succes!" },
            { "Error", "Eroare" }
        };

        private static readonly Dictionary<string, string> En = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "BothMonitors", "Both Monitors" },
            { "Monitor1", "Monitor 1" },
            { "Monitor2", "Monitor 2" },
            { "NightLight", "Night Light" },
            { "NightLightWarmth", "Warmth intensity" },
            { "SelectDisplay", "Select Display" },
            { "TargetForHotkeys", "Target for Fn+F9 / Fn+F10" },
            { "CycleTargetHint", "Fn+F12 cycles display" },
            { "Brightness", "Brightness" },
            { "BrightnessDown", "Brightness -" },
            { "BrightnessUp", "Brightness +" },
            { "BrightnessDelta", "Brightness {0}%" },
            { "Intensity", "Intensity" },
            { "IntensityDown", "Intensity -" },
            { "IntensityUp", "Intensity +" },
            { "IntensityDelta", "Intensity {0}%" },
            { "NightLightEnabled", "Night Light (Enabled)" },
            { "NightLightDisabled", "Night Light (Disabled)" },
            { "Enabled", "Enabled" },
            { "Disabled", "Disabled" },
            { "PressToEnableNightLight", "Press Shift+Fn+F12 to enable" },
            { "NightLightTurnedOff", "Night Light disabled" },
            { "BrightnessMinusStep", "Brightness -{0}%" },
            { "BrightnessPlusStep", "Brightness +{0}%" },
            { "IntensityMinusStep", "Intensity -{0}%" },
            { "IntensityPlusStep", "Intensity +{0}%" },
            { "MenuSettings", "Settings..." },
            { "MenuLanguage", "Language / Limbă" },
            { "MenuSetupHotkeys", "Setup FN Hotkeys" },
            { "MenuStartup", "Start with Windows" },
            { "MenuAbout", "About / Help" },
            { "MenuExit", "Exit" },
            { "TrayTooltip", "BrightKeys - Fn+F9 / Fn+F10 / Fn+F12 (Shift=NightLight)" },
            { "StartupBalloonTitle", "BrightKeys" },
            { "StartupBalloonText", "Fn+F9/F10 = Brightness   Fn+F12 = Switch display\r\nShift+Fn+F12 = Night Light On/Off   Shift+Fn+F9/F10 = Night Light Warmth" },
            { "StatusBalloonTarget", "Target: {0}\r\n{1}\r\nNight Light: {2}" },
            { "AboutDialogTitle", "About BrightKeys" },
            { "AboutDialogText", "BrightKeys 1.0\r\nDual-monitor brightness control using FN keys.\r\n\r\nBrightness Hotkeys:\r\n  Fn+F9  = Decrease brightness\r\n  Fn+F10 = Increase brightness\r\n  Fn+F12 = Switch monitor (Both → Monitor 1 → Monitor 2)\r\n\r\nNight Light Warmth:\r\n  Shift + Fn+F12 = Toggle Night Light\r\n  Shift + Fn+F9  = Decrease warmth\r\n  Shift + Fn+F10 = Increase warmth\r\n\r\nHold Fn+F9 / Fn+F10 for smooth continuous gliding (60 FPS).\r\nHold Shift to adjust Night Light, release Shift to adjust Brightness." },
            { "SettingsTitle", "BrightKeys - Settings" },
            { "TabGeneral", "General" },
            { "TabNightLight", "Night Light" },
            { "TabHotkeys", "Keys & Shortcuts" },
            { "LanguageLabel", "Interface language:" },
            { "StepLabel", "Adjustment step (% per keypress):" },
            { "StartupLabel", "Start automatically on Windows logon" },
            { "NightLightEnableLabel", "Enable Night Light feature" },
            { "NightLightDesc", "Warmth intensity can be adjusted independently per monitor or simultaneously via Fn+F12." },
            { "Monitor1Warmth", "Monitor 1 Warmth:" },
            { "Monitor2Warmth", "Monitor 2 Warmth:" },
            { "HotkeysDescAppKey", "Physical FN Keys (Laptop / Hardware AppKey):" },
            { "HotkeysDescAppKeyInfo", "Fn+F9 = Brightness Down\r\nFn+F10 = Brightness Up\r\nFn+F12 = Switch display (Both → Mon 1 → Mon 2)\r\nShift+Fn+F12 = Toggle Night Light\r\nShift+Fn+F9/F10 = Adjust Night Light Warmth" },
            { "BtnReinstallAppKeys", "Re-register default FN keys" },
            { "HotkeysCustomTitle", "Custom Global Shortcuts (Universal):" },
            { "HotkeysCustomInfo", "Works on any keyboard (no FN hardware conflicts):" },
            { "EnableCustomHotkeys", "Enable custom global shortcuts" },
            { "KeyBrightnessDown", "Brightness Down:" },
            { "KeyBrightnessUp", "Brightness Up:" },
            { "KeyNextDisplay", "Switch Display:" },
            { "KeyNightLight", "Toggle Night Light:" },
            { "BtnResetDefaults", "Reset to defaults" },
            { "BtnSave", "Save" },
            { "BtnCancel", "Cancel" },
            { "SavedSuccess", "Settings saved successfully!" },
            { "Error", "Error" }
        };

        public static string CurrentLanguage
        {
            get { lock (_lock) { return _currentLang; } }
            set
            {
                lock (_lock)
                {
                    string lang = string.Equals(value, "en", StringComparison.OrdinalIgnoreCase) ? "en" : "ro";
                    if (_currentLang == lang) return;
                    _currentLang = lang;
                    Storage.SaveLanguage(_currentLang);
                }
                Action handler = LanguageChanged;
                if (handler != null) handler();
            }
        }

        public static void Initialize()
        {
            lock (_lock)
            {
                _currentLang = Storage.LoadLanguage();
            }
        }

        public static string T(string key)
        {
            lock (_lock)
            {
                Dictionary<string, string> dict = _currentLang == "en" ? En : Ro;
                string val;
                if (dict.TryGetValue(key, out val)) return val;
                if (Ro.TryGetValue(key, out val)) return val;
                return key;
            }
        }

        public static string T(string key, params object[] args)
        {
            string fmt = T(key);
            try
            {
                return string.Format(fmt, args);
            }
            catch
            {
                return fmt;
            }
        }
    }
}
