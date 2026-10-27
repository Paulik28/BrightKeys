using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace BrightKeys
{
    internal enum Target
    {
        Both = 0,
        Monitor1 = 1,
        Monitor2 = 2,
        NightLight = 3
    }

    internal class Display
    {
        public int Index;
        public IntPtr Monitor;
        public string DeviceName = "";
        public string MonitorKey = "";
        public Native.RECT Rect;
        public string Label = "";

        public override string ToString()
        {
            return Label;
        }
    }

    internal class BrightnessState
    {
        public Display Display;
        public bool Supported;
        public int Percent;
        public string Error = "";
        public uint RawMin;
        public uint RawMax;
        public uint RawCur;
    }

    internal class DisplayBrightness
    {
        public Display Display;
        public int Percent = 100;
        public uint RawMin = 0;
        public uint RawMax = 100;
        public bool Supported = true;
        public bool Initialized = false;
        public string Error = "";
    }

    internal static class BrightnessService
    {
        private static readonly object _stateLock = new object();
        private static List<Display> _cachedDisplays = null;
        private static readonly Dictionary<string, DisplayBrightness> _displayStates =
            new Dictionary<string, DisplayBrightness>(StringComparer.OrdinalIgnoreCase);

        private static readonly object _workerLock = new object();
        private static readonly AutoResetEvent _workEvent = new AutoResetEvent(false);
        private static readonly Dictionary<string, int> _pendingWrites =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private static Thread _workerThread;
        private static volatile bool _workerRunning = false;

        public static void Initialize()
        {
            RefreshDisplays();
            StartWorker();
            ThreadPool.QueueUserWorkItem(delegate { WarmUpInitialBrightness(); });
        }

        public static void ShutdownWorker()
        {
            _workerRunning = false;
            try { _workEvent.Set(); } catch { }
        }

        private static void StartWorker()
        {
            if (_workerRunning) return;
            _workerRunning = true;
            _workerThread = new Thread(WorkerLoop);
            _workerThread.IsBackground = true;
            _workerThread.Name = "BrightKeys.DdcCiWorker";
            _workerThread.Start();
        }

        private static void WorkerLoop()
        {
            while (_workerRunning)
            {
                _workEvent.WaitOne();
                if (!_workerRunning) break;

                Dictionary<string, int> batch;
                lock (_workerLock)
                {
                    batch = new Dictionary<string, int>(_pendingWrites, StringComparer.OrdinalIgnoreCase);
                    _pendingWrites.Clear();
                }

                if (batch.Count == 0) continue;

                foreach (KeyValuePair<string, int> item in batch)
                {
                    if (!_workerRunning) break;
                    ApplyHardwareBrightness(item.Key, item.Value);
                }
            }
        }

        private static void ApplyHardwareBrightness(string deviceName, int targetPercent)
        {
            try
            {
                DisplayBrightness db = null;
                lock (_stateLock)
                {
                    _displayStates.TryGetValue(deviceName, out db);
                }

                IntPtr targetHMonitor = IntPtr.Zero;
                Native.MonitorEnumProc callback = delegate(IntPtr hMonitor, IntPtr hdc, IntPtr lprc, IntPtr data)
                {
                    Native.MONITORINFOEX mi = new Native.MONITORINFOEX();
                    mi.cbSize = Marshal.SizeOf(typeof(Native.MONITORINFOEX));
                    if (Native.GetMonitorInfo(hMonitor, ref mi))
                    {
                        if (string.Equals(mi.szDevice, deviceName, StringComparison.OrdinalIgnoreCase))
                        {
                            targetHMonitor = hMonitor;
                            return false;
                        }
                    }
                    return true;
                };
                Native.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero);
                GC.KeepAlive(callback);

                if (targetHMonitor == IntPtr.Zero)
                {
                    Storage.Log("ApplyHardwareBrightness: Monitor not found for " + deviceName);
                    return;
                }

                uint count = 0;
                if (!Native.GetNumberOfPhysicalMonitorsFromHMONITOR(targetHMonitor, out count) || count == 0)
                {
                    Storage.Log("ApplyHardwareBrightness: No physical monitors for " + deviceName);
                    return;
                }

                Native.PHYSICAL_MONITOR[] pms = new Native.PHYSICAL_MONITOR[count];
                if (!Native.GetPhysicalMonitorsFromHMONITOR(targetHMonitor, count, pms))
                {
                    Storage.Log("ApplyHardwareBrightness: GetPhysicalMonitors failed for " + deviceName);
                    return;
                }

                foreach (Native.PHYSICAL_MONITOR pm in pms)
                {
                    uint min = 0, cur = 0, max = 100;
                    if (db != null && db.RawMax > db.RawMin)
                    {
                        min = db.RawMin;
                        max = db.RawMax;
                    }
                    else
                    {
                        if (TryGetBrightness(pm.hPhysicalMonitor, out min, out cur, out max))
                        {
                            if (db != null)
                            {
                                db.RawMin = min;
                                db.RawMax = max;
                            }
                        }
                        else
                        {
                            min = 0;
                            max = 100;
                        }
                    }

                    uint raw = FromPercent(min, max, targetPercent);
                    bool ok = TrySetBrightness(pm.hPhysicalMonitor, raw);
                    Native.DestroyPhysicalMonitor(pm.hPhysicalMonitor);

                    if (ok)
                    {
                        if (db != null) db.Error = "";
                        Storage.Log("DDC/CI Set OK: " + deviceName + " -> " + targetPercent + "% (raw=" + raw + ")");
                    }
                    else
                    {
                        if (db != null) db.Error = "Nu pot seta luminozitatea (DDC/CI)";
                        Storage.Log("DDC/CI Set FAILED: " + deviceName + " -> " + targetPercent + "%");
                    }
                }
            }
            catch (Exception ex)
            {
                Storage.Log("ApplyHardwareBrightness error: " + ex.Message);
            }
        }

        private static void WarmUpInitialBrightness()
        {
            try
            {
                List<Display> displays = GetDisplays();
                foreach (Display d in displays)
                {
                    IntPtr hMon = d.Monitor;
                    uint count = 0;
                    if (!Native.GetNumberOfPhysicalMonitorsFromHMONITOR(hMon, out count) || count == 0)
                        continue;

                    Native.PHYSICAL_MONITOR[] pms = new Native.PHYSICAL_MONITOR[count];
                    if (!Native.GetPhysicalMonitorsFromHMONITOR(hMon, count, pms))
                        continue;

                    foreach (Native.PHYSICAL_MONITOR pm in pms)
                    {
                        uint min, cur, max;
                        if (TryGetBrightness(pm.hPhysicalMonitor, out min, out cur, out max))
                        {
                            lock (_stateLock)
                            {
                                DisplayBrightness db;
                                if (!_displayStates.TryGetValue(d.DeviceName, out db))
                                {
                                    db = new DisplayBrightness { Display = d };
                                    _displayStates[d.DeviceName] = db;
                                }
                                db.RawMin = min;
                                db.RawMax = max;
                                db.Percent = ToPercent(min, cur, max);
                                db.Supported = true;
                                db.Initialized = true;
                            }
                        }
                        Native.DestroyPhysicalMonitor(pm.hPhysicalMonitor);
                    }
                }
                Storage.Log("WarmUp initial brightness: " + SummaryState());
            }
            catch (Exception ex)
            {
                Storage.Log("WarmUp error: " + ex.Message);
            }
        }

        public static List<Display> GetDisplays()
        {
            lock (_stateLock)
            {
                if (_cachedDisplays != null && _cachedDisplays.Count > 0)
                    return new List<Display>(_cachedDisplays);
            }
            return RefreshDisplays();
        }

        public static List<Display> RefreshDisplays()
        {
            List<Display> list = QueryDisplays();
            lock (_stateLock)
            {
                _cachedDisplays = list;
                foreach (Display d in list)
                {
                    if (!_displayStates.ContainsKey(d.DeviceName))
                    {
                        _displayStates[d.DeviceName] = new DisplayBrightness
                        {
                            Display = d,
                            Percent = 100,
                            Supported = true
                        };
                    }
                    else
                    {
                        _displayStates[d.DeviceName].Display = d;
                    }
                }
            }
            return list;
        }

        private static List<Display> QueryDisplays()
        {
            List<Display> list = new List<Display>();
            Native.MonitorEnumProc callback = delegate(IntPtr hMonitor, IntPtr hdc, IntPtr lprc, IntPtr data)
            {
                Native.MONITORINFOEX mi = new Native.MONITORINFOEX();
                mi.cbSize = Marshal.SizeOf(typeof(Native.MONITORINFOEX));
                if (Native.GetMonitorInfo(hMonitor, ref mi))
                {
                    Display d = new Display();
                    d.Monitor = hMonitor;
                    d.DeviceName = mi.szDevice ?? "";
                    d.Rect = mi.rcMonitor;
                    d.MonitorKey = GetMonitorKey(d.DeviceName);
                    list.Add(d);
                }
                return true;
            };
            Native.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero);
            GC.KeepAlive(callback);

            list.Sort(delegate(Display a, Display b)
            {
                int c = a.Rect.Left.CompareTo(b.Rect.Left);
                if (c != 0) return c;
                c = a.Rect.Top.CompareTo(b.Rect.Top);
                if (c != 0) return c;
                return a.DeviceName.CompareTo(b.DeviceName);
            });

            for (int i = 0; i < list.Count; i++)
            {
                list[i].Index = i + 1;
                list[i].Label = "Monitor " + (i + 1);
                if (!string.IsNullOrEmpty(list[i].MonitorKey))
                    list[i].Label += " (" + list[i].MonitorKey + ")";
            }
            return list;
        }

        private static string GetMonitorKey(string deviceName)
        {
            try
            {
                Native.DISPLAY_DEVICE dd = new Native.DISPLAY_DEVICE();
                dd.cb = Marshal.SizeOf(typeof(Native.DISPLAY_DEVICE));
                if (Native.EnumDisplayDevices(deviceName, 0, ref dd, 0))
                {
                    string id = dd.DeviceID ?? "";
                    string[] parts = id.Split('\\');
                    if (parts.Length >= 2 && parts[0].ToUpperInvariant().StartsWith("MONITOR"))
                        return parts[1];
                }
            }
            catch { }
            return "";
        }

        // Fast, asynchronous adjust: updates in-memory percentage immediately (0 ms UI lag)
        // and enqueues hardware DDC/CI write to background worker.
        public static void AdjustAsync(Target target, int deltaPercent, out string title, out string subtitle, out List<OsdRow> rows)
        {
            List<Display> displays = GetDisplays();
            rows = new List<OsdRow>();

            lock (_stateLock)
            {
                for (int i = 0; i < displays.Count; i++)
                {
                    Display d = displays[i];
                    DisplayBrightness db;
                    if (!_displayStates.TryGetValue(d.DeviceName, out db))
                    {
                        db = new DisplayBrightness { Display = d, Percent = 100, Supported = true };
                        _displayStates[d.DeviceName] = db;
                    }

                    bool isTargeted = IsTargeted(target, i);
                    if (isTargeted)
                    {
                        int newPct = Math.Max(0, Math.Min(100, db.Percent + deltaPercent));
                        db.Percent = newPct;
                        lock (_workerLock)
                        {
                            _pendingWrites[d.DeviceName] = newPct;
                        }
                        _workEvent.Set();
                    }

                    OsdRow row = new OsdRow
                    {
                        Label = d.Label,
                        Highlight = isTargeted,
                        Percent = db.Supported ? (int?)db.Percent : null,
                        Unavailable = !db.Supported
                    };
                    rows.Add(row);
                }
            }

            title = TargetName(target, displays);
            subtitle = I18n.T("BrightnessDelta", deltaPercent > 0 ? "+" + deltaPercent : deltaPercent.ToString());
        }

        public static List<OsdRow> SelectionRows(Target target, List<Display> displays, bool nightLightEnabled)
        {
            List<OsdRow> rows = new List<OsdRow>();
            rows.Add(new OsdRow { Label = I18n.T("BothMonitors"), Highlight = target == Target.Both });
            if (displays.Count > 0)
                rows.Add(new OsdRow { Label = displays[0].Label, Highlight = target == Target.Monitor1 });
            if (displays.Count > 1)
                rows.Add(new OsdRow { Label = displays[1].Label, Highlight = target == Target.Monitor2 });
            if (nightLightEnabled)
                rows.Add(new OsdRow { Label = I18n.T("NightLight"), Highlight = target == Target.NightLight });
            return rows;
        }

        public static List<OsdRow> SelectionRows(Target target, List<Display> displays)
        {
            return SelectionRows(target, displays, false);
        }

        public static List<BrightnessState> ReadAll()
        {
            List<Display> displays = GetDisplays();
            List<BrightnessState> list = new List<BrightnessState>();
            lock (_stateLock)
            {
                foreach (Display d in displays)
                {
                    DisplayBrightness db;
                    if (_displayStates.TryGetValue(d.DeviceName, out db))
                    {
                        list.Add(new BrightnessState
                        {
                            Display = d,
                            Percent = db.Percent,
                            Supported = db.Supported,
                            Error = db.Error,
                            RawMin = db.RawMin,
                            RawMax = db.RawMax,
                            RawCur = FromPercent(db.RawMin, db.RawMax, db.Percent)
                        });
                    }
                    else
                    {
                        list.Add(new BrightnessState
                        {
                            Display = d,
                            Percent = 100,
                            Supported = true
                        });
                    }
                }
            }
            return list;
        }

        public static string SummaryState()
        {
            List<string> parts = new List<string>();
            lock (_stateLock)
            {
                foreach (KeyValuePair<string, DisplayBrightness> kvp in _displayStates)
                {
                    Display d = kvp.Value.Display;
                    string label = d != null ? d.Label : kvp.Key;
                    parts.Add(label + "=" + kvp.Value.Percent + "%");
                }
            }
            return string.Join("; ", parts.ToArray());
        }

        // Synchronous adjust for fallback / CLI helper
        public static List<BrightnessState> Adjust(Target target, int deltaPercent)
        {
            string title, subtitle;
            List<OsdRow> rows;
            AdjustAsync(target, deltaPercent, out title, out subtitle, out rows);
            return ReadAll();
        }

        private static bool TryGetBrightness(IntPtr hPhysicalMonitor, out uint min, out uint cur, out uint max)
        {
            min = 0;
            cur = 0;
            max = 0;
            for (int attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    if (Native.GetMonitorBrightness(hPhysicalMonitor, out min, out cur, out max))
                        return true;
                }
                catch { }
                Thread.Sleep(50);
            }
            return false;
        }

        private static bool TrySetBrightness(IntPtr hPhysicalMonitor, uint value)
        {
            for (int attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    if (Native.SetMonitorBrightness(hPhysicalMonitor, value))
                        return true;
                }
                catch { }
                Thread.Sleep(50);
            }
            return false;
        }

        private static int ToPercent(uint min, uint cur, uint max)
        {
            if (max <= min) return 0;
            return (int)Math.Round((cur - min) * 100.0 / (max - min));
        }

        private static uint FromPercent(uint min, uint max, double percent)
        {
            if (max <= min) return min;
            double raw = min + percent * (max - min) / 100.0;
            if (raw < min) raw = min;
            if (raw > max) raw = max;
            return (uint)Math.Round(raw);
        }

        public static string TargetName(Target target, List<Display> displays)
        {
            if (target == Target.NightLight) return I18n.T("NightLight");
            if (target == Target.Both) return I18n.T("BothMonitors");
            int idx = target == Target.Monitor1 ? 0 : 1;
            if (displays != null && displays.Count > idx) return displays[idx].Label;
            return target == Target.Monitor1 ? I18n.T("Monitor1") : I18n.T("Monitor2");
        }

        public static Target NextTarget(Target current, int monitorCount, bool nightLightEnabled)
        {
            if (!nightLightEnabled)
            {
                if (monitorCount <= 1) return Target.Both;
                switch (current)
                {
                    case Target.Both: return Target.Monitor1;
                    case Target.Monitor1: return Target.Monitor2;
                    default: return Target.Both;
                }
            }
            else
            {
                if (monitorCount <= 1)
                {
                    switch (current)
                    {
                        case Target.Both: return Target.NightLight;
                        default: return Target.Both;
                    }
                }
                switch (current)
                {
                    case Target.Both: return Target.Monitor1;
                    case Target.Monitor1: return Target.Monitor2;
                    case Target.Monitor2: return Target.NightLight;
                    default: return Target.Both;
                }
            }
        }

        public static Target NextTarget(Target current, int monitorCount)
        {
            return NextTarget(current, monitorCount, false);
        }

        public static bool IsTargeted(Target target, int index)
        {
            if (target == Target.Both) return true;
            if (target == Target.Monitor1) return index == 0;
            return index == 1;
        }

        public static List<Display> DisplaysFromStates(List<BrightnessState> states)
        {
            List<Display> list = new List<Display>();
            foreach (BrightnessState st in states) list.Add(st.Display);
            return list;
        }

        public static string Summary(List<BrightnessState> states)
        {
            List<string> parts = new List<string>();
            foreach (BrightnessState st in states)
            {
                string label = st.Display != null ? st.Display.Label : "?";
                string value = st.Supported ? st.Percent + "%" : "indisponibil";
                if (!string.IsNullOrEmpty(st.Error)) value += " (" + st.Error + ")";
                parts.Add(label + "=" + value);
            }
            return string.Join("; ", parts.ToArray());
        }
    }
}
