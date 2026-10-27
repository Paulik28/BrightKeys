using System;
using System.Collections.Generic;
using System.Threading;

namespace BrightKeys
{
    internal static class NightLightService
    {
        private static bool _enabled = false;
        private static int _intensity1 = 50;
        private static int _intensity2 = 50;
        private static readonly object _lock = new object();

        private static readonly AutoResetEvent _rampEvent = new AutoResetEvent(false);
        private static Thread _workerThread;
        private static volatile bool _workerRunning = false;
        private static volatile int _pendingIntensity1 = -1;
        private static volatile int _pendingIntensity2 = -1;
        private static volatile int _lastSavedIntensity1 = -1;
        private static volatile int _lastSavedIntensity2 = -1;

        public static bool Enabled
        {
            get { lock (_lock) { return _enabled; } }
        }

        public static int Intensity
        {
            get { lock (_lock) { return _intensity1; } }
        }

        public static int GetIntensity(int monitorIndex)
        {
            lock (_lock)
            {
                return monitorIndex == 0 ? _intensity1 : _intensity2;
            }
        }

        public static void SetIntensity(int monitorIndex, int value)
        {
            lock (_lock)
            {
                int clamped = Math.Max(0, Math.Min(100, value));
                if (monitorIndex == 0) _intensity1 = clamped;
                else _intensity2 = clamped;

                ApplyRampAsync();
            }
        }

        public static void Initialize()
        {
            lock (_lock)
            {
                _enabled = Storage.LoadNightLightEnabled();
                _intensity1 = Storage.LoadNightLightIntensity1();
                _intensity2 = Storage.LoadNightLightIntensity2();
                _lastSavedIntensity1 = _intensity1;
                _lastSavedIntensity2 = _intensity2;
                StartWorker();
                if (_enabled)
                {
                    ApplyRampAsync();
                }
            }
        }

        public static void Shutdown()
        {
            _workerRunning = false;
            try { _rampEvent.Set(); } catch { }
        }

        private static void StartWorker()
        {
            if (_workerRunning) return;
            _workerRunning = true;
            _workerThread = new Thread(WorkerLoop);
            _workerThread.IsBackground = true;
            _workerThread.Name = "BrightKeys.NightLightWorker";
            _workerThread.Start();
        }

        private static void WorkerLoop()
        {
            while (_workerRunning)
            {
                _rampEvent.WaitOne(200);
                if (!_workerRunning) break;

                int targetInt1 = -1;
                int targetInt2 = -1;
                bool isEnabled = false;
                lock (_lock)
                {
                    targetInt1 = _pendingIntensity1;
                    targetInt2 = _pendingIntensity2;
                    isEnabled = _enabled;
                    _pendingIntensity1 = -1;
                    _pendingIntensity2 = -1;
                }

                if (targetInt1 >= 0 || targetInt2 >= 0)
                {
                    int i1 = targetInt1 >= 0 ? targetInt1 : _intensity1;
                    int i2 = targetInt2 >= 0 ? targetInt2 : _intensity2;

                    if (isEnabled)
                    {
                        ApplyRampInternal(i1, i2);
                    }

                    if (i1 != _lastSavedIntensity1 || i2 != _lastSavedIntensity2)
                    {
                        Storage.SaveNightLightIntensities(i1, i2);
                        _lastSavedIntensity1 = i1;
                        _lastSavedIntensity2 = i2;
                    }
                }
            }
        }

        public static bool Toggle()
        {
            lock (_lock)
            {
                _enabled = !_enabled;
                Storage.SaveNightLightEnabled(_enabled);
                if (_enabled)
                {
                    ApplyRampAsync();
                }
                else
                {
                    RestoreDaylight();
                }
                return _enabled;
            }
        }

        public static void SetEnabled(bool enabled)
        {
            lock (_lock)
            {
                if (_enabled == enabled) return;
                _enabled = enabled;
                Storage.SaveNightLightEnabled(_enabled);
                if (_enabled)
                {
                    ApplyRampAsync();
                }
                else
                {
                    RestoreDaylight();
                }
            }
        }

        // Fast asynchronous intensity adjust: updates in-memory value in 0.001ms (0 ms UI lag)
        // and enqueues gamma ramp write + disk save to background worker thread.
        public static void AdjustIntensity(Target target, int delta, out int val1, out int val2)
        {
            lock (_lock)
            {
                if (target == Target.Both || target == Target.NightLight)
                {
                    _intensity1 = Math.Max(0, Math.Min(100, _intensity1 + delta));
                    _intensity2 = Math.Max(0, Math.Min(100, _intensity2 + delta));
                }
                else if (target == Target.Monitor1)
                {
                    _intensity1 = Math.Max(0, Math.Min(100, _intensity1 + delta));
                }
                else if (target == Target.Monitor2)
                {
                    _intensity2 = Math.Max(0, Math.Min(100, _intensity2 + delta));
                }

                val1 = _intensity1;
                val2 = _intensity2;
                _pendingIntensity1 = _intensity1;
                _pendingIntensity2 = _intensity2;
                _rampEvent.Set();
            }
        }

        public static int AdjustIntensity(int delta)
        {
            int v1, v2;
            AdjustIntensity(Target.Both, delta, out v1, out v2);
            return v1;
        }

        public static void ApplyRamp()
        {
            lock (_lock)
            {
                ApplyRampAsync();
            }
        }

        private static void ApplyRampAsync()
        {
            _pendingIntensity1 = _intensity1;
            _pendingIntensity2 = _intensity2;
            _rampEvent.Set();
        }

        private static Native.RAMP BuildRamp(int intensity)
        {
            double f = intensity / 100.0;
            double r = 1.0;
            double g = 1.0 - (0.18 * f);
            double b = 1.0 - (0.65 * f);

            Native.RAMP ramp = new Native.RAMP
            {
                Red = new ushort[256],
                Green = new ushort[256],
                Blue = new ushort[256]
            };

            for (int i = 0; i < 256; i++)
            {
                ramp.Red[i] = (ushort)Math.Min(65535, Math.Round(i * 256.0 * r));
                ramp.Green[i] = (ushort)Math.Min(65535, Math.Round(i * 256.0 * g));
                ramp.Blue[i] = (ushort)Math.Min(65535, Math.Round(i * 256.0 * b));
            }
            return ramp;
        }

        private static void ApplyRampInternal(int int1, int int2)
        {
            try
            {
                Native.RAMP ramp1 = BuildRamp(int1);
                Native.RAMP ramp2 = BuildRamp(int2);

                List<Display> displays = BrightnessService.GetDisplays();
                if (displays != null && displays.Count > 0)
                {
                    for (int i = 0; i < displays.Count; i++)
                    {
                        Display d = displays[i];
                        if (string.IsNullOrEmpty(d.DeviceName)) continue;

                        IntPtr hdc = Native.CreateDC(d.DeviceName, d.DeviceName, null, IntPtr.Zero);
                        if (hdc != IntPtr.Zero)
                        {
                            if (i == 0) Native.SetDeviceGammaRamp(hdc, ref ramp1);
                            else Native.SetDeviceGammaRamp(hdc, ref ramp2);
                            Native.DeleteDC(hdc);
                        }
                    }
                }
                else
                {
                    IntPtr hdc = Native.CreateDC("DISPLAY", null, null, IntPtr.Zero);
                    if (hdc != IntPtr.Zero)
                    {
                        Native.SetDeviceGammaRamp(hdc, ref ramp1);
                        Native.DeleteDC(hdc);
                    }
                }
            }
            catch (Exception ex)
            {
                Storage.Log("ApplyRampInternal error: " + ex.Message);
            }
        }

        public static void RestoreDaylight()
        {
            Native.RAMP ramp = new Native.RAMP
            {
                Red = new ushort[256],
                Green = new ushort[256],
                Blue = new ushort[256]
            };

            for (int i = 0; i < 256; i++)
            {
                ushort val = (ushort)(i * 256);
                ramp.Red[i] = val;
                ramp.Green[i] = val;
                ramp.Blue[i] = val;
            }

            try
            {
                List<Display> displays = BrightnessService.GetDisplays();
                if (displays != null && displays.Count > 0)
                {
                    foreach (Display d in displays)
                    {
                        if (string.IsNullOrEmpty(d.DeviceName)) continue;
                        IntPtr hdc = Native.CreateDC(d.DeviceName, d.DeviceName, null, IntPtr.Zero);
                        if (hdc != IntPtr.Zero)
                        {
                            Native.SetDeviceGammaRamp(hdc, ref ramp);
                            Native.DeleteDC(hdc);
                        }
                    }
                }
                else
                {
                    IntPtr hdc = Native.CreateDC("DISPLAY", null, null, IntPtr.Zero);
                    if (hdc != IntPtr.Zero)
                    {
                        Native.SetDeviceGammaRamp(hdc, ref ramp);
                        Native.DeleteDC(hdc);
                    }
                }
            }
            catch (Exception ex)
            {
                Storage.Log("RestoreDaylight error: " + ex.Message);
            }
        }
    }
}
