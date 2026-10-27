using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace BrightKeys
{
    internal class HotkeyListener
    {
        private EventWaitHandle _evtDown;
        private EventWaitHandle _evtUp;
        private EventWaitHandle _evtNext;
        private EventWaitHandle _evtNightLight;
        private EventWaitHandle _evtStatus;
        private Thread _thread;
        private volatile bool _running = true;
        private readonly Control _syncControl;

        public event Action<int> Command;

        public HotkeyListener(Control syncControl)
        {
            _syncControl = syncControl;
        }

        public void Start()
        {
            _evtDown       = new EventWaitHandle(false, EventResetMode.AutoReset, @"Local\BrightKeys_Event_Down");
            _evtUp         = new EventWaitHandle(false, EventResetMode.AutoReset, @"Local\BrightKeys_Event_Up");
            _evtNext       = new EventWaitHandle(false, EventResetMode.AutoReset, @"Local\BrightKeys_Event_Next");
            _evtNightLight = new EventWaitHandle(false, EventResetMode.AutoReset, @"Local\BrightKeys_Event_NightLight");
            _evtStatus     = new EventWaitHandle(false, EventResetMode.AutoReset, @"Local\BrightKeys_Event_Status");

            _thread = new Thread(delegate()
            {
                WaitHandle[] handles = new WaitHandle[] { _evtDown, _evtUp, _evtNext, _evtNightLight, _evtStatus };
                while (_running)
                {
                    try
                    {
                        int index = WaitHandle.WaitAny(handles, 1000);
                        if (!_running) break;
                        if (index >= 0 && index < 5)
                        {
                            int action;
                            if (index == 0) action = 1;      // Down
                            else if (index == 1) action = 2; // Up
                            else if (index == 2) action = 3; // Next
                            else if (index == 3) action = 6; // NightLight
                            else action = 5;                 // Status

                            if (_syncControl != null && !_syncControl.IsDisposed && _syncControl.IsHandleCreated)
                            {
                                _syncControl.BeginInvoke(new Action(delegate
                                {
                                    Action<int> handler = Command;
                                    if (handler != null) handler(action);
                                }));
                            }
                        }
                    }
                    catch { }
                }
            });
            _thread.IsBackground = true;
            _thread.Name = "BrightKeys.HotkeyListener";
            _thread.Start();
        }

        public void Stop()
        {
            _running = false;
            try { if (_evtDown != null) _evtDown.Set(); } catch { }
            try { if (_evtDown != null) _evtDown.Close(); } catch { }
            try { if (_evtUp != null) _evtUp.Close(); } catch { }
            try { if (_evtNext != null) _evtNext.Close(); } catch { }
            try { if (_evtNightLight != null) _evtNightLight.Close(); } catch { }
            try { if (_evtStatus != null) _evtStatus.Close(); } catch { }
        }
    }

    internal class CommandWindow : NativeWindow
    {
        public event Action<int> Command;
        public event Action DisplayChanged;
        public event Action ToggleNightLightRequested;

        private const int HOTKEY_F11_ID = 1111;
        private const int HOTKEY_CUSTOM_DOWN = 2001;
        private const int HOTKEY_CUSTOM_UP = 2002;
        private const int HOTKEY_CUSTOM_NEXT = 2003;
        private const int HOTKEY_CUSTOM_NIGHTLIGHT = 2004;

        public CommandWindow()
        {
            CreateParams cp = new CreateParams();
            cp.Caption = AppInfo.WindowTitle;
            cp.X = 0;
            cp.Y = 0;
            cp.Width = 0;
            cp.Height = 0;
            cp.ExStyle = Native.WS_EX_TOOLWINDOW;
            CreateHandle(cp);

            RegisterDefaultHotkeys();
            RegisterCustomHotkeys();
        }

        private void RegisterDefaultHotkeys()
        {
            try
            {
                bool ok = Native.RegisterHotKey(Handle, HOTKEY_F11_ID, Native.MOD_NOREPEAT, (uint)Keys.F11);
                if (!ok)
                {
                    ok = Native.RegisterHotKey(Handle, HOTKEY_F11_ID, 0, (uint)Keys.F11);
                }
                Storage.Log("RegisterHotKey F11: " + ok);
            }
            catch (Exception ex)
            {
                Storage.Log("RegisterHotKey F11 error: " + ex.Message);
            }
        }

        public void RegisterCustomHotkeys()
        {
            UnregisterCustomHotkeys();
            bool enabled;
            string down, up, next, nl;
            Storage.LoadCustomHotkeys(out enabled, out down, out up, out next, out nl);
            if (!enabled) return;

            RegisterSingleShortcut(HOTKEY_CUSTOM_DOWN, down);
            RegisterSingleShortcut(HOTKEY_CUSTOM_UP, up);
            RegisterSingleShortcut(HOTKEY_CUSTOM_NEXT, next);
            RegisterSingleShortcut(HOTKEY_CUSTOM_NIGHTLIGHT, nl);
        }

        private void RegisterSingleShortcut(int id, string shortcut)
        {
            uint mod, vk;
            if (ParseShortcut(shortcut, out mod, out vk))
            {
                try
                {
                    Native.RegisterHotKey(Handle, id, mod, vk);
                }
                catch { }
            }
        }

        public void UnregisterCustomHotkeys()
        {
            try { Native.UnregisterHotKey(Handle, HOTKEY_CUSTOM_DOWN); } catch { }
            try { Native.UnregisterHotKey(Handle, HOTKEY_CUSTOM_UP); } catch { }
            try { Native.UnregisterHotKey(Handle, HOTKEY_CUSTOM_NEXT); } catch { }
            try { Native.UnregisterHotKey(Handle, HOTKEY_CUSTOM_NIGHTLIGHT); } catch { }
        }

        public void UnregisterHotkeys()
        {
            try { Native.UnregisterHotKey(Handle, HOTKEY_F11_ID); } catch { }
            UnregisterCustomHotkeys();
        }

        private static bool ParseShortcut(string text, out uint modifiers, out uint vk)
        {
            modifiers = Native.MOD_NOREPEAT;
            vk = 0;
            if (string.IsNullOrEmpty(text)) return false;
            string[] parts = text.Split(new char[] { '+' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return false;
            for (int i = 0; i < parts.Length - 1; i++)
            {
                string m = parts[i].Trim().ToLowerInvariant();
                if (m == "ctrl" || m == "control") modifiers |= Native.MOD_CONTROL;
                else if (m == "alt") modifiers |= Native.MOD_ALT;
                else if (m == "shift") modifiers |= Native.MOD_SHIFT;
                else if (m == "win") modifiers |= Native.MOD_WIN;
            }
            string keyStr = parts[parts.Length - 1].Trim();
            try
            {
                Keys k = (Keys)Enum.Parse(typeof(Keys), keyStr, true);
                vk = (uint)k;
                return true;
            }
            catch
            {
                return false;
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Native.WM_HOTKEY)
            {
                int hotkeyId = (int)m.WParam.ToInt64();
                if (hotkeyId == HOTKEY_F11_ID || hotkeyId == HOTKEY_CUSTOM_NIGHTLIGHT)
                {
                    Action handler = ToggleNightLightRequested;
                    if (handler != null) handler();
                    return;
                }
                if (hotkeyId == HOTKEY_CUSTOM_DOWN)
                {
                    Action<int> handler = Command;
                    if (handler != null) handler(1);
                    return;
                }
                if (hotkeyId == HOTKEY_CUSTOM_UP)
                {
                    Action<int> handler = Command;
                    if (handler != null) handler(2);
                    return;
                }
                if (hotkeyId == HOTKEY_CUSTOM_NEXT)
                {
                    Action<int> handler = Command;
                    if (handler != null) handler(3);
                    return;
                }
            }
            if (m.Msg == AppInfo.CommandMessage)
            {
                Action<int> handler = Command;
                if (handler != null) handler((int)m.WParam.ToInt64());
                return;
            }
            if (m.Msg == Native.WM_DISPLAYCHANGE)
            {
                Action handler = DisplayChanged;
                if (handler != null) handler();
            }
            base.WndProc(ref m);
        }
    }

    internal class TrayApp : ApplicationContext
    {
        private readonly NotifyIcon _icon;
        private readonly ContextMenuStrip _menu;
        private readonly CommandWindow _window;
        private readonly Control _syncControl;
        private readonly HotkeyListener _listener;
        private ToolStripMenuItem _miBoth;
        private ToolStripMenuItem _miM1;
        private ToolStripMenuItem _miM2;
        private ToolStripMenuItem _miTargetNightLight;
        private ToolStripMenuItem _miDown;
        private ToolStripMenuItem _miUp;
        private ToolStripMenuItem _miNext;
        private ToolStripMenuItem _miNightLight;
        private ToolStripMenuItem _miSettings;
        private ToolStripMenuItem _miLang;
        private ToolStripMenuItem _miLangRo;
        private ToolStripMenuItem _miLangEn;
        private ToolStripMenuItem _miHotkeys;
        private ToolStripMenuItem _miStartup;
        private ToolStripMenuItem _miAbout;
        private ToolStripMenuItem _miExit;

        private Target _target = Target.Both;
        private int _step = 5;
        private OsdForm _osd;
        private SettingsForm _settingsForm;
        private bool _iconDisposed;

        private readonly System.Windows.Forms.Timer _holdDelayTimer;
        private readonly System.Windows.Forms.Timer _glideTimer;
        private int _glideDir = 0;
        private bool _isHolding = false;

        public TrayApp()
        {
            Storage.EnsureDefaultConfig();
            I18n.Initialize();
            _step = Storage.LoadStep();
            _target = Storage.LoadTarget();

            BrightnessService.Initialize();
            NightLightService.Initialize();

            SystemSetup.EnsureHelperCopies();
            string err;
            if (!SystemSetup.HotkeysMatch())
                SystemSetup.InstallHotkeys(out err);

            _window = new CommandWindow();
            _window.Command += OnCommand;
            _window.ToggleNightLightRequested += delegate { ToggleNightLight(); };
            _window.DisplayChanged += delegate
            {
                BrightnessService.RefreshDisplays();
                if (NightLightService.Enabled)
                {
                    NightLightService.ApplyRamp();
                }
            };

            _syncControl = new Control();
            _syncControl.CreateControl();
            IntPtr forceHandle = _syncControl.Handle;

            _listener = new HotkeyListener(_syncControl);
            _listener.Command += OnCommand;
            _listener.Start();

            _holdDelayTimer = new System.Windows.Forms.Timer();
            _holdDelayTimer.Interval = 250;
            _holdDelayTimer.Tick += delegate
            {
                _holdDelayTimer.Stop();
                if (_glideDir != 0 && IsAdjustKeyPhysicallyPressed(_glideDir))
                {
                    _isHolding = true;
                    _glideTimer.Start();
                }
                else
                {
                    StopGlide();
                }
            };

            _glideTimer = new System.Windows.Forms.Timer();
            _glideTimer.Interval = 35; // ~28 smooth ticks/sec for butter-smooth sliding
            _glideTimer.Tick += delegate
            {
                if (_isHolding && _glideDir != 0)
                {
                    if (IsAdjustKeyPhysicallyPressed(_glideDir))
                    {
                        DoAdjustStep(_glideDir * 1, true);
                    }
                    else
                    {
                        StopGlide();
                    }
                }
                else
                {
                    StopGlide();
                }
            };

            _menu = BuildMenu();

            _icon = new NotifyIcon();
            _icon.Icon = LoadIcon();
            _icon.Text = I18n.T("TrayTooltip");
            _icon.ContextMenuStrip = _menu;
            _icon.DoubleClick += delegate { ShowStatusBalloon(); };
            _icon.Visible = true;
            _icon.ShowBalloonTip(6000, I18n.T("StartupBalloonTitle"), I18n.T("StartupBalloonText"), ToolTipIcon.Info);

            I18n.LanguageChanged += delegate
            {
                UpdateMenu();
                UpdateTrayTexts();
            };

            Storage.Log("Tray started. step=" + _step + ", lang=" + I18n.CurrentLanguage + ", hotkeysOk=" + SystemSetup.HotkeysMatch());
        }

        public void DisposeIcon()
        {
            if (_iconDisposed) return;
            _iconDisposed = true;
            if (_holdDelayTimer != null) { try { _holdDelayTimer.Stop(); _holdDelayTimer.Dispose(); } catch { } }
            if (_glideTimer != null) { try { _glideTimer.Stop(); _glideTimer.Dispose(); } catch { } }
            if (_listener != null) _listener.Stop();
            BrightnessService.ShutdownWorker();
            NightLightService.Shutdown();
            NightLightService.RestoreDaylight();
            if (_osd != null && !_osd.IsDisposed)
            {
                try { _osd.Close(); _osd.Dispose(); } catch { }
                _osd = null;
            }
            if (_settingsForm != null && !_settingsForm.IsDisposed)
            {
                try { _settingsForm.Close(); _settingsForm.Dispose(); } catch { }
                _settingsForm = null;
            }
            try { _icon.Visible = false; _icon.Dispose(); } catch { }
            try { if (_window != null) { _window.UnregisterHotkeys(); _window.DestroyHandle(); } } catch { }
            try { if (_syncControl != null) _syncControl.Dispose(); } catch { }
        }

        private static Icon LoadIcon()
        {
            try
            {
                System.IO.Stream stream = typeof(TrayApp).Assembly.GetManifestResourceStream("app.ico");
                if (stream != null) return new Icon(stream);
            }
            catch { }
            return SystemIcons.Application;
        }

        private ContextMenuStrip BuildMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip();

            _miBoth = new ToolStripMenuItem();
            _miBoth.Click += delegate { SetTarget(Target.Both); };

            _miM1 = new ToolStripMenuItem();
            _miM1.Click += delegate { SetTarget(Target.Monitor1); };

            _miM2 = new ToolStripMenuItem();
            _miM2.Click += delegate { SetTarget(Target.Monitor2); };

            _miTargetNightLight = new ToolStripMenuItem();
            _miTargetNightLight.Click += delegate { SetTarget(Target.NightLight); };

            _miDown = new ToolStripMenuItem();
            _miDown.ShortcutKeyDisplayString = "Fn+F9";
            _miDown.Click += delegate { HandleAdjustCommand(-1); };

            _miUp = new ToolStripMenuItem();
            _miUp.ShortcutKeyDisplayString = "Fn+F10";
            _miUp.Click += delegate { HandleAdjustCommand(1); };

            _miNext = new ToolStripMenuItem();
            _miNext.ShortcutKeyDisplayString = "Fn+F12";
            _miNext.Click += delegate { DoNext(); };

            _miNightLight = new ToolStripMenuItem();
            _miNightLight.ShortcutKeyDisplayString = "Shift+Fn+F12";
            _miNightLight.Click += delegate { ToggleNightLight(); };

            _miSettings = new ToolStripMenuItem();
            _miSettings.Click += delegate { OpenSettings(); };

            _miLang = new ToolStripMenuItem();
            _miLangRo = new ToolStripMenuItem("Română");
            _miLangRo.Click += delegate { I18n.CurrentLanguage = "ro"; };
            _miLangEn = new ToolStripMenuItem("English");
            _miLangEn.Click += delegate { I18n.CurrentLanguage = "en"; };
            _miLang.DropDownItems.Add(_miLangRo);
            _miLang.DropDownItems.Add(_miLangEn);

            _miHotkeys = new ToolStripMenuItem();
            _miHotkeys.Click += delegate { ConfigureHotkeys(); };

            _miStartup = new ToolStripMenuItem();
            _miStartup.CheckOnClick = true;
            _miStartup.Click += delegate { ToggleStartup(); };

            _miAbout = new ToolStripMenuItem();
            _miAbout.Click += delegate { ShowAbout(); };

            _miExit = new ToolStripMenuItem();
            _miExit.Click += delegate { Quit(); };

            menu.Items.Add(_miBoth);
            menu.Items.Add(_miM1);
            menu.Items.Add(_miM2);
            menu.Items.Add(_miTargetNightLight);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(_miDown);
            menu.Items.Add(_miUp);
            menu.Items.Add(_miNext);
            menu.Items.Add(_miNightLight);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(_miSettings);
            menu.Items.Add(_miLang);
            menu.Items.Add(_miHotkeys);
            menu.Items.Add(_miStartup);
            menu.Items.Add(_miAbout);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(_miExit);

            menu.Opening += delegate { UpdateMenu(); };
            UpdateMenu();
            return menu;
        }

        private void UpdateMenu()
        {
            List<Display> displays = BrightnessService.GetDisplays();
            _miBoth.Text = I18n.T("BothMonitors");
            _miM1.Text = displays.Count > 0 ? displays[0].Label : I18n.T("Monitor1");
            _miM2.Text = displays.Count > 1 ? displays[1].Label : I18n.T("Monitor2");
            _miM2.Enabled = displays.Count > 1;

            _miBoth.Checked = _target == Target.Both;
            _miM1.Checked = _target == Target.Monitor1;
            _miM2.Checked = _target == Target.Monitor2;
            _miTargetNightLight.Checked = _target == Target.NightLight;
            _miTargetNightLight.Text = I18n.T("NightLight");
            _miTargetNightLight.Visible = NightLightService.Enabled;

            if (_target == Target.NightLight)
            {
                _miDown.Text = I18n.T("IntensityMinusStep", _step);
                _miUp.Text = I18n.T("IntensityPlusStep", _step);
            }
            else
            {
                _miDown.Text = I18n.T("BrightnessMinusStep", _step);
                _miUp.Text = I18n.T("BrightnessPlusStep", _step);
            }

            _miNext.Text = I18n.T("SelectDisplay");
            _miNightLight.Checked = NightLightService.Enabled;
            _miNightLight.Text = NightLightService.Enabled
                ? I18n.T("NightLightEnabled")
                : I18n.T("NightLightDisabled");

            _miSettings.Text = I18n.T("MenuSettings");
            _miLang.Text = I18n.T("MenuLanguage");
            _miLangRo.Checked = I18n.CurrentLanguage == "ro";
            _miLangEn.Checked = I18n.CurrentLanguage == "en";
            _miHotkeys.Text = I18n.T("MenuSetupHotkeys");
            _miStartup.Text = I18n.T("MenuStartup");
            _miStartup.Checked = SystemSetup.IsStartupEnabled();
            _miAbout.Text = I18n.T("MenuAbout");
            _miExit.Text = I18n.T("MenuExit");
        }

        private void UpdateTrayTexts()
        {
            try
            {
                _icon.Text = I18n.T("TrayTooltip");
            }
            catch { }
        }

        private void OpenSettings()
        {
            if (_settingsForm != null && !_settingsForm.IsDisposed)
            {
                _settingsForm.BringToFront();
                _settingsForm.Activate();
                return;
            }

            _settingsForm = new SettingsForm(delegate
            {
                _step = Storage.LoadStep();
                if (_window != null) _window.RegisterCustomHotkeys();
                UpdateMenu();
                UpdateTrayTexts();
            });
            _settingsForm.Show();
        }

        private void OnCommand(int command)
        {
            switch (command)
            {
                case 1: HandleAdjustCommand(-1); break;
                case 2: HandleAdjustCommand(1); break;
                case 3:
                    StopGlide();
                    DoNext();
                    break;
                case 4:
                    StopGlide();
                    Quit();
                    break;
                case 5:
                    StopGlide();
                    ShowStatusBalloon();
                    break;
                case 6:
                    StopGlide();
                    ToggleNightLight();
                    break;
            }
        }

        private void StopGlide()
        {
            if (_holdDelayTimer != null) _holdDelayTimer.Stop();
            if (_glideTimer != null) _glideTimer.Stop();
            _isHolding = false;
            _glideDir = 0;
        }

        private void HandleAdjustCommand(int dir)
        {
            if (_isHolding && _glideDir == dir && IsAdjustKeyPhysicallyPressed(dir))
            {
                return;
            }

            StopGlide();

            DoAdjustStep(dir * _step, false);

            if (IsAdjustKeyPhysicallyPressed(dir))
            {
                _glideDir = dir;
                _holdDelayTimer.Stop();
                _holdDelayTimer.Start();
            }
        }

        private static bool IsAdjustKeyPhysicallyPressed(int dir)
        {
            int k1 = dir < 0 ? 0xB4 : 0xAC; // LaunchMail (15) or BrowserHome (7)
            int k2 = dir < 0 ? 0x78 : 0x79; // F9 or F10
            return ((Native.GetAsyncKeyState(k1) & 0x8000) != 0) ||
                   ((Native.GetAsyncKeyState(k2) & 0x8000) != 0);
        }

        private void SetTarget(Target target)
        {
            StopGlide();
            _target = target;
            Storage.SaveTarget(_target);
            Storage.Log("target set to " + _target);
            List<Display> displays = BrightnessService.GetDisplays();
            ShowOsd(I18n.T("SelectDisplay"), I18n.T("TargetForHotkeys"), BrightnessService.SelectionRows(_target, displays, NightLightService.Enabled));
        }

        private void DoAdjustStep(int delta, bool isContinuous)
        {
            try
            {
                bool isShift = (Native.GetAsyncKeyState(0x10) & 0x8000) != 0
                            || (Native.GetAsyncKeyState(0xA0) & 0x8000) != 0
                            || (Native.GetAsyncKeyState(0xA1) & 0x8000) != 0
                            || (Control.ModifierKeys & Keys.Shift) != 0;

                string title;
                string subtitle;
                List<OsdRow> rows;

                if (isShift || _target == Target.NightLight)
                {
                    if (!NightLightService.Enabled)
                    {
                        title = I18n.T("NightLight");
                        subtitle = I18n.T("Disabled");
                        rows = new List<OsdRow>();
                        rows.Add(new OsdRow
                        {
                            Label = I18n.T("PressToEnableNightLight"),
                            Highlight = false
                        });
                        ShowOsd(title, subtitle, rows);
                        return;
                    }

                    int val1, val2;
                    NightLightService.AdjustIntensity(_target, delta, out val1, out val2);

                    title = I18n.T("NightLight");
                    subtitle = isContinuous
                        ? (delta < 0 ? I18n.T("IntensityDown") : I18n.T("IntensityUp"))
                        : I18n.T("IntensityDelta", (delta > 0 ? "+" + delta : delta.ToString()));

                    rows = new List<OsdRow>();
                    List<Display> displays = BrightnessService.GetDisplays();
                    bool targetAll = (_target == Target.Both || _target == Target.NightLight);

                    string label1 = (displays != null && displays.Count > 0) ? displays[0].Label : I18n.T("Monitor1");
                    rows.Add(new OsdRow
                    {
                        Label = label1,
                        Highlight = targetAll || _target == Target.Monitor1,
                        Percent = val1
                    });

                    if (displays != null && displays.Count > 1)
                    {
                        string label2 = displays[1].Label;
                        rows.Add(new OsdRow
                        {
                            Label = label2,
                            Highlight = targetAll || _target == Target.Monitor2,
                            Percent = val2
                        });
                    }

                    ShowOsd(title, subtitle, rows);
                    if (!isContinuous)
                    {
                        Storage.Log("adjust night light delta=" + delta + " target=" + _target + " m1=" + val1 + " m2=" + val2);
                    }
                    return;
                }

                BrightnessService.AdjustAsync(_target, delta, out title, out subtitle, out rows);
                if (isContinuous)
                {
                    subtitle = delta < 0 ? I18n.T("BrightnessDown") : I18n.T("BrightnessUp");
                }
                ShowOsd(title, subtitle, rows);
                if (!isContinuous)
                {
                    Storage.Log("adjust target=" + _target + " delta=" + delta);
                }
            }
            catch (Exception ex)
            {
                Storage.Log("adjust error: " + ex);
                ShowBalloon(I18n.T("Error"), ex.Message, ToolTipIcon.Warning);
            }
        }

        private void DoNext()
        {
            try
            {
                bool isShift = (Native.GetAsyncKeyState(0x10) & 0x8000) != 0
                            || (Native.GetAsyncKeyState(0xA0) & 0x8000) != 0
                            || (Native.GetAsyncKeyState(0xA1) & 0x8000) != 0
                            || (Control.ModifierKeys & Keys.Shift) != 0;
                if (isShift)
                {
                    ToggleNightLight();
                    return;
                }

                List<Display> displays = BrightnessService.GetDisplays();
                _target = BrightnessService.NextTarget(_target, displays.Count, false);
                Storage.SaveTarget(_target);
                ShowOsd(I18n.T("SelectDisplay"), I18n.T("CycleTargetHint"), BrightnessService.SelectionRows(_target, displays, false));
                Storage.Log("next target=" + _target + " monitors=" + displays.Count);
            }
            catch (Exception ex)
            {
                Storage.Log("next error: " + ex);
                ShowBalloon(I18n.T("Error"), ex.Message, ToolTipIcon.Warning);
            }
        }

        private void ToggleNightLight()
        {
            StopGlide();
            bool enabled = NightLightService.Toggle();
            if (!enabled && _target == Target.NightLight)
            {
                _target = Target.Both;
                Storage.SaveTarget(_target);
            }

            if (enabled)
            {
                List<OsdRow> rows = new List<OsdRow>();
                List<Display> displays = BrightnessService.GetDisplays();
                bool targetAll = (_target == Target.Both || _target == Target.NightLight);

                string label1 = (displays != null && displays.Count > 0) ? displays[0].Label : I18n.T("Monitor1");
                rows.Add(new OsdRow
                {
                    Label = label1,
                    Highlight = targetAll || _target == Target.Monitor1,
                    Percent = NightLightService.GetIntensity(0)
                });

                if (displays != null && displays.Count > 1)
                {
                    string label2 = displays[1].Label;
                    rows.Add(new OsdRow
                    {
                        Label = label2,
                        Highlight = targetAll || _target == Target.Monitor2,
                        Percent = NightLightService.GetIntensity(1)
                    });
                }

                ShowOsd(I18n.T("NightLight"), I18n.T("Enabled") + " (Shift+Fn+F12)", rows);
                Storage.Log("Night light enabled. m1=" + NightLightService.GetIntensity(0) + " m2=" + NightLightService.GetIntensity(1));
            }
            else
            {
                List<OsdRow> rows = new List<OsdRow>();
                rows.Add(new OsdRow
                {
                    Label = I18n.T("NightLightTurnedOff"),
                    Highlight = false
                });
                ShowOsd(I18n.T("NightLight"), I18n.T("Disabled") + " (Shift+Fn+F12)", rows);
                Storage.Log("Night light disabled.");
            }
        }

        private void ShowOsd(string title, string subtitle, List<OsdRow> rows)
        {
            try
            {
                if (_osd == null || _osd.IsDisposed)
                {
                    _osd = new OsdForm(title, subtitle, rows);
                    _osd.Show();
                }
                else
                {
                    _osd.UpdateContent(title, subtitle, rows);
                    if (!_osd.Visible) _osd.Show();
                }
            }
            catch (Exception ex)
            {
                Storage.Log("OSD error: " + ex.Message);
            }
        }

        private void ConfigureHotkeys()
        {
            string err;
            if (SystemSetup.InstallHotkeys(out err))
            {
                MessageBox.Show(
                    I18n.T("AboutDialogText"),
                    "BrightKeys", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Nu am putut configura tastele FN:\r\n" + err,
                    "BrightKeys", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ToggleStartup()
        {
            string err;
            if (!SystemSetup.SetStartup(_miStartup.Checked, out err))
            {
                MessageBox.Show("Nu am putut modifica pornirea automată:\r\n" + err,
                    "BrightKeys", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _miStartup.Checked = SystemSetup.IsStartupEnabled();
            }
        }

        private void ShowStatusBalloon()
        {
            try
            {
                List<Display> displays = BrightnessService.GetDisplays();
                string targetName = BrightnessService.TargetName(_target, displays);
                string nlStatus = NightLightService.Enabled
                    ? string.Format("{0} (M1: {1}%, M2: {2}%)", I18n.T("Enabled"), NightLightService.GetIntensity(0), NightLightService.GetIntensity(1))
                    : I18n.T("Disabled");
                string text = string.Format(I18n.T("StatusBalloonTarget"), targetName, BrightnessService.SummaryState(), nlStatus);
                _icon.ShowBalloonTip(5000, "BrightKeys", text, ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                Storage.Log("status error: " + ex.Message);
            }
        }

        private void ShowBalloon(string title, string text, ToolTipIcon icon)
        {
            try { _icon.ShowBalloonTip(5000, title, text, icon); } catch { }
        }

        private void ShowAbout()
        {
            MessageBox.Show(
                I18n.T("AboutDialogText"),
                I18n.T("AboutDialogTitle"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void Quit()
        {
            StopGlide();
            DisposeIcon();
            Application.Exit();
        }
    }
}
