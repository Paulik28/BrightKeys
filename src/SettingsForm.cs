using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BrightKeys
{
    internal class SettingsForm : Form
    {
        private readonly Action _onSettingsSaved;

        // Top Navigation Bar
        private Panel _topBar;
        private Button _btnTabGeneral;
        private Button _btnTabNightLight;
        private Button _btnTabHotkeys;
        private int _currentTab = 0;

        // Content Panels
        private Panel _contentPanel;
        private Panel _panelGeneral;
        private Panel _panelNightLight;
        private Panel _panelHotkeys;

        // Bottom Bar
        private Panel _bottomBar;
        private Button _btnSave;
        private Button _btnCancel;

        // General Controls
        private Label _lblLang;
        private ComboBox _cbLang;
        private Label _lblStep;
        private ComboBox _cbStep;
        private CheckBox _chkStartup;

        // Night Light Controls
        private CheckBox _chkNightLight;
        private Label _lblNightLightDesc;
        private Label _lblWarmth1;
        private TrackBar _tbWarmth1;
        private Label _lblWarmthVal1;
        private Label _lblWarmth2;
        private TrackBar _tbWarmth2;
        private Label _lblWarmthVal2;

        // Hotkeys Controls
        private Label _lblAppKeyTitle;
        private Label _lblAppKeyDesc;
        private Button _btnReinstallAppKeys;
        private Label _lblCustomTitle;
        private CheckBox _chkEnableCustom;
        private Label _lblDown;
        private TextBox _txtDown;
        private Label _lblUp;
        private TextBox _txtUp;
        private Label _lblNext;
        private TextBox _txtNext;
        private Label _lblNightLightKey;
        private TextBox _txtNightLightKey;
        private Button _btnResetHotkeys;

        private readonly Color _bgDark = Color.FromArgb(28, 28, 32);
        private readonly Color _bgCard = Color.FromArgb(36, 36, 42);
        private readonly Color _accent = Color.FromArgb(79, 195, 247);
        private readonly Color _textDim = Color.FromArgb(170, 170, 180);

        public SettingsForm(Action onSettingsSaved)
        {
            _onSettingsSaved = onSettingsSaved;
            InitializeComponent();
            LoadCurrentValues();
            ApplyTranslations();
        }

        private void InitializeComponent()
        {
            Text = I18n.T("SettingsTitle");
            Size = new Size(540, 520);
            MinimumSize = new Size(540, 520);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = true;
            BackColor = _bgDark;
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point);

            try
            {
                System.IO.Stream stream = typeof(SettingsForm).Assembly.GetManifestResourceStream("app.ico");
                if (stream != null) Icon = new Icon(stream);
            }
            catch { }

            // 1. Top Bar
            _topBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.FromArgb(24, 24, 28)
            };

            _btnTabGeneral = CreateTabButton("General", 0);
            _btnTabNightLight = CreateTabButton("Lumină de noapte", 1);
            _btnTabHotkeys = CreateTabButton("Taste & Scurtături", 2);

            _topBar.Controls.Add(_btnTabGeneral);
            _topBar.Controls.Add(_btnTabNightLight);
            _topBar.Controls.Add(_btnTabHotkeys);
            Controls.Add(_topBar);

            // 2. Bottom Bar
            _bottomBar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 54,
                BackColor = Color.FromArgb(24, 24, 28)
            };

            _btnSave = new Button
            {
                Text = I18n.T("BtnSave"),
                DialogResult = DialogResult.OK,
                Size = new Size(100, 32),
                Location = new Point(Width - 235, 11),
                BackColor = _accent,
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnSave.FlatAppearance.BorderSize = 0;
            _btnSave.Click += delegate { SaveSettings(); };

            _btnCancel = new Button
            {
                Text = I18n.T("BtnCancel"),
                DialogResult = DialogResult.Cancel,
                Size = new Size(95, 32),
                Location = new Point(Width - 125, 11),
                BackColor = Color.FromArgb(50, 50, 58),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnCancel.FlatAppearance.BorderSize = 0;

            _bottomBar.Controls.Add(_btnSave);
            _bottomBar.Controls.Add(_btnCancel);
            Controls.Add(_bottomBar);

            // 3. Content Panel container
            _contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = _bgDark,
                Padding = new Padding(15)
            };
            Controls.Add(_contentPanel);

            // Panel: General
            _panelGeneral = new Panel { Dock = DockStyle.Fill, BackColor = _bgDark, AutoScroll = true };
            InitGeneralPanel();
            _contentPanel.Controls.Add(_panelGeneral);

            // Panel: Night Light
            _panelNightLight = new Panel { Dock = DockStyle.Fill, BackColor = _bgDark, AutoScroll = true };
            InitNightLightPanel();
            _contentPanel.Controls.Add(_panelNightLight);

            // Panel: Hotkeys
            _panelHotkeys = new Panel { Dock = DockStyle.Fill, BackColor = _bgDark, AutoScroll = true };
            InitHotkeysPanel();
            _contentPanel.Controls.Add(_panelHotkeys);

            SelectTab(0);
        }

        private Button CreateTabButton(string text, int index)
        {
            Button btn = new Button
            {
                Text = text,
                Width = 150,
                Height = 44,
                Left = index * 150,
                Top = 0,
                FlatStyle = FlatStyle.Flat,
                ForeColor = _textDim,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9.25f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += delegate { SelectTab(index); };
            return btn;
        }

        private void SelectTab(int index)
        {
            _currentTab = index;
            _btnTabGeneral.ForeColor = index == 0 ? Color.White : _textDim;
            _btnTabGeneral.BackColor = index == 0 ? _bgDark : Color.Transparent;

            _btnTabNightLight.ForeColor = index == 1 ? Color.White : _textDim;
            _btnTabNightLight.BackColor = index == 1 ? _bgDark : Color.Transparent;

            _btnTabHotkeys.ForeColor = index == 2 ? Color.White : _textDim;
            _btnTabHotkeys.BackColor = index == 2 ? _bgDark : Color.Transparent;

            _panelGeneral.Visible = index == 0;
            _panelNightLight.Visible = index == 1;
            _panelHotkeys.Visible = index == 2;
        }

        private void InitGeneralPanel()
        {
            int y = 15;

            _lblLang = new Label { Text = I18n.T("LanguageLabel"), Location = new Point(20, y), AutoSize = true, ForeColor = Color.White };
            _panelGeneral.Controls.Add(_lblLang);
            y += 24;

            _cbLang = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(20, y),
                Width = 220,
                BackColor = _bgCard,
                ForeColor = Color.White
            };
            _cbLang.Items.Add("Română");
            _cbLang.Items.Add("English");
            _cbLang.SelectedIndexChanged += delegate
            {
                string sel = _cbLang.SelectedIndex == 1 ? "en" : "ro";
                if (I18n.CurrentLanguage != sel)
                {
                    I18n.CurrentLanguage = sel;
                    ApplyTranslations();
                }
            };
            _panelGeneral.Controls.Add(_cbLang);
            y += 45;

            _lblStep = new Label { Text = I18n.T("StepLabel"), Location = new Point(20, y), AutoSize = true, ForeColor = Color.White };
            _panelGeneral.Controls.Add(_lblStep);
            y += 24;

            _cbStep = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(20, y),
                Width = 220,
                BackColor = _bgCard,
                ForeColor = Color.White
            };
            _cbStep.Items.AddRange(new object[] { "1%", "2%", "5%", "10%", "15%", "20%" });
            _panelGeneral.Controls.Add(_cbStep);
            y += 45;

            _chkStartup = new CheckBox
            {
                Text = I18n.T("StartupLabel"),
                Location = new Point(20, y),
                AutoSize = true,
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            _panelGeneral.Controls.Add(_chkStartup);
        }

        private void InitNightLightPanel()
        {
            int y = 15;

            _chkNightLight = new CheckBox
            {
                Text = I18n.T("NightLightEnableLabel"),
                Location = new Point(20, y),
                AutoSize = true,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _chkNightLight.CheckedChanged += delegate
            {
                bool en = _chkNightLight.Checked;
                _tbWarmth1.Enabled = en;
                _tbWarmth2.Enabled = en;
            };
            _panelNightLight.Controls.Add(_chkNightLight);
            y += 32;

            _lblNightLightDesc = new Label
            {
                Text = I18n.T("NightLightDesc"),
                Location = new Point(20, y),
                Size = new Size(470, 38),
                ForeColor = _textDim
            };
            _panelNightLight.Controls.Add(_lblNightLightDesc);
            y += 45;

            // Monitor 1 Warmth
            _lblWarmth1 = new Label { Text = I18n.T("Monitor1Warmth"), Location = new Point(20, y), AutoSize = true, ForeColor = Color.White };
            _panelNightLight.Controls.Add(_lblWarmth1);

            _lblWarmthVal1 = new Label { Text = "50%", Location = new Point(440, y), AutoSize = true, ForeColor = _accent };
            _panelNightLight.Controls.Add(_lblWarmthVal1);
            y += 22;

            _tbWarmth1 = new TrackBar
            {
                Location = new Point(15, y),
                Width = 470,
                Minimum = 0,
                Maximum = 100,
                TickFrequency = 10,
                SmallChange = 5,
                LargeChange = 10
            };
            _tbWarmth1.ValueChanged += delegate
            {
                _lblWarmthVal1.Text = _tbWarmth1.Value + "%";
            };
            _panelNightLight.Controls.Add(_tbWarmth1);
            y += 50;

            // Monitor 2 Warmth
            _lblWarmth2 = new Label { Text = I18n.T("Monitor2Warmth"), Location = new Point(20, y), AutoSize = true, ForeColor = Color.White };
            _panelNightLight.Controls.Add(_lblWarmth2);

            _lblWarmthVal2 = new Label { Text = "50%", Location = new Point(440, y), AutoSize = true, ForeColor = _accent };
            _panelNightLight.Controls.Add(_lblWarmthVal2);
            y += 22;

            _tbWarmth2 = new TrackBar
            {
                Location = new Point(15, y),
                Width = 470,
                Minimum = 0,
                Maximum = 100,
                TickFrequency = 10,
                SmallChange = 5,
                LargeChange = 10
            };
            _tbWarmth2.ValueChanged += delegate
            {
                _lblWarmthVal2.Text = _tbWarmth2.Value + "%";
            };
            _panelNightLight.Controls.Add(_tbWarmth2);
        }

        private void InitHotkeysPanel()
        {
            int y = 10;

            // Hardware FN Keys Section
            _lblAppKeyTitle = new Label
            {
                Text = I18n.T("HotkeysDescAppKey"),
                Location = new Point(20, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.White
            };
            _panelHotkeys.Controls.Add(_lblAppKeyTitle);
            y += 22;

            _lblAppKeyDesc = new Label
            {
                Text = I18n.T("HotkeysDescAppKeyInfo"),
                Location = new Point(20, y),
                Size = new Size(470, 75),
                ForeColor = _textDim
            };
            _panelHotkeys.Controls.Add(_lblAppKeyDesc);
            y += 80;

            _btnReinstallAppKeys = new Button
            {
                Text = I18n.T("BtnReinstallAppKeys"),
                Location = new Point(20, y),
                Size = new Size(240, 28),
                BackColor = Color.FromArgb(50, 50, 58),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnReinstallAppKeys.FlatAppearance.BorderSize = 0;
            _btnReinstallAppKeys.Click += delegate
            {
                string err;
                if (SystemSetup.InstallHotkeys(out err))
                {
                    MessageBox.Show(this, "Tastele FN implicite au fost reînregistrate cu succes!", "BrightKeys",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(this, "Eroare: " + err, "BrightKeys", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };
            _panelHotkeys.Controls.Add(_btnReinstallAppKeys);
            y += 40;

            // Custom Shortcuts Section
            _lblCustomTitle = new Label
            {
                Text = I18n.T("HotkeysCustomTitle"),
                Location = new Point(20, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.White
            };
            _panelHotkeys.Controls.Add(_lblCustomTitle);
            y += 22;

            _chkEnableCustom = new CheckBox
            {
                Text = I18n.T("EnableCustomHotkeys"),
                Location = new Point(20, y),
                AutoSize = true,
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            _chkEnableCustom.CheckedChanged += delegate
            {
                bool en = _chkEnableCustom.Checked;
                _txtDown.Enabled = en;
                _txtUp.Enabled = en;
                _txtNext.Enabled = en;
                _txtNightLightKey.Enabled = en;
            };
            _panelHotkeys.Controls.Add(_chkEnableCustom);
            y += 28;

            _lblDown = new Label { Text = I18n.T("KeyBrightnessDown"), Location = new Point(20, y), AutoSize = true, ForeColor = _textDim };
            _panelHotkeys.Controls.Add(_lblDown);
            _txtDown = CreateKeyTextBox("Ctrl+Alt+F9", 220, y - 2);
            _panelHotkeys.Controls.Add(_txtDown);
            y += 28;

            _lblUp = new Label { Text = I18n.T("KeyBrightnessUp"), Location = new Point(20, y), AutoSize = true, ForeColor = _textDim };
            _panelHotkeys.Controls.Add(_lblUp);
            _txtUp = CreateKeyTextBox("Ctrl+Alt+F10", 220, y - 2);
            _panelHotkeys.Controls.Add(_txtUp);
            y += 28;

            _lblNext = new Label { Text = I18n.T("KeyNextDisplay"), Location = new Point(20, y), AutoSize = true, ForeColor = _textDim };
            _panelHotkeys.Controls.Add(_lblNext);
            _txtNext = CreateKeyTextBox("Ctrl+Alt+F12", 220, y - 2);
            _panelHotkeys.Controls.Add(_txtNext);
            y += 28;

            _lblNightLightKey = new Label { Text = I18n.T("KeyNightLight"), Location = new Point(20, y), AutoSize = true, ForeColor = _textDim };
            _panelHotkeys.Controls.Add(_lblNightLightKey);
            _txtNightLightKey = CreateKeyTextBox("Ctrl+Alt+N", 220, y - 2);
            _panelHotkeys.Controls.Add(_txtNightLightKey);
            y += 32;

            _btnResetHotkeys = new Button
            {
                Text = I18n.T("BtnResetDefaults"),
                Location = new Point(220, y),
                Size = new Size(180, 26),
                BackColor = Color.FromArgb(44, 44, 52),
                ForeColor = _textDim,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnResetHotkeys.FlatAppearance.BorderSize = 0;
            _btnResetHotkeys.Click += delegate
            {
                _txtDown.Text = "Ctrl+Alt+F9";
                _txtUp.Text = "Ctrl+Alt+F10";
                _txtNext.Text = "Ctrl+Alt+F12";
                _txtNightLightKey.Text = "Ctrl+Alt+N";
            };
            _panelHotkeys.Controls.Add(_btnResetHotkeys);
        }

        private TextBox CreateKeyTextBox(string defaultKey, int x, int y)
        {
            TextBox tb = new TextBox
            {
                Text = defaultKey,
                Location = new Point(x, y),
                Width = 180,
                BackColor = _bgCard,
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true,
                Cursor = Cursors.Hand
            };

            tb.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.ShiftKey ||
                    e.KeyCode == Keys.Menu || e.KeyCode == Keys.LWin || e.KeyCode == Keys.RWin)
                {
                    return;
                }

                string shortcut = "";
                if (e.Control) shortcut += "Ctrl+";
                if (e.Alt) shortcut += "Alt+";
                if (e.Shift) shortcut += "Shift+";
                shortcut += e.KeyCode.ToString();

                tb.Text = shortcut;
                e.SuppressKeyPress = true;
                e.Handled = true;
            };

            return tb;
        }

        private void LoadCurrentValues()
        {
            // General
            _cbLang.SelectedIndex = I18n.CurrentLanguage == "en" ? 1 : 0;
            int step = Storage.LoadStep();
            string stepStr = step + "%";
            int idx = _cbStep.Items.IndexOf(stepStr);
            _cbStep.SelectedIndex = idx >= 0 ? idx : 2; // Default 5%
            _chkStartup.Checked = SystemSetup.IsStartupEnabled();

            // Night Light
            _chkNightLight.Checked = NightLightService.Enabled;
            _tbWarmth1.Value = NightLightService.GetIntensity(0);
            _lblWarmthVal1.Text = _tbWarmth1.Value + "%";
            _tbWarmth2.Value = NightLightService.GetIntensity(1);
            _lblWarmthVal2.Text = _tbWarmth2.Value + "%";
            _tbWarmth1.Enabled = _chkNightLight.Checked;
            _tbWarmth2.Enabled = _chkNightLight.Checked;

            // Custom Hotkeys
            bool customEnabled;
            string down, up, next, nl;
            Storage.LoadCustomHotkeys(out customEnabled, out down, out up, out next, out nl);
            _chkEnableCustom.Checked = customEnabled;
            _txtDown.Text = string.IsNullOrEmpty(down) ? "Ctrl+Alt+F9" : down;
            _txtUp.Text = string.IsNullOrEmpty(up) ? "Ctrl+Alt+F10" : up;
            _txtNext.Text = string.IsNullOrEmpty(next) ? "Ctrl+Alt+F12" : next;
            _txtNightLightKey.Text = string.IsNullOrEmpty(nl) ? "Ctrl+Alt+N" : nl;

            _txtDown.Enabled = customEnabled;
            _txtUp.Enabled = customEnabled;
            _txtNext.Enabled = customEnabled;
            _txtNightLightKey.Enabled = customEnabled;
        }

        private void ApplyTranslations()
        {
            Text = I18n.T("SettingsTitle");
            _btnTabGeneral.Text = I18n.T("TabGeneral");
            _btnTabNightLight.Text = I18n.T("TabNightLight");
            _btnTabHotkeys.Text = I18n.T("TabHotkeys");

            _lblLang.Text = I18n.T("LanguageLabel");
            _lblStep.Text = I18n.T("StepLabel");
            _chkStartup.Text = I18n.T("StartupLabel");

            _chkNightLight.Text = I18n.T("NightLightEnableLabel");
            _lblNightLightDesc.Text = I18n.T("NightLightDesc");
            _lblWarmth1.Text = I18n.T("Monitor1Warmth");
            _lblWarmth2.Text = I18n.T("Monitor2Warmth");

            _lblAppKeyTitle.Text = I18n.T("HotkeysDescAppKey");
            _lblAppKeyDesc.Text = I18n.T("HotkeysDescAppKeyInfo");
            _btnReinstallAppKeys.Text = I18n.T("BtnReinstallAppKeys");

            _lblCustomTitle.Text = I18n.T("HotkeysCustomTitle");
            _chkEnableCustom.Text = I18n.T("EnableCustomHotkeys");
            _lblDown.Text = I18n.T("KeyBrightnessDown");
            _lblUp.Text = I18n.T("KeyBrightnessUp");
            _lblNext.Text = I18n.T("KeyNextDisplay");
            _lblNightLightKey.Text = I18n.T("KeyNightLight");
            _btnResetHotkeys.Text = I18n.T("BtnResetDefaults");

            _btnSave.Text = I18n.T("BtnSave");
            _btnCancel.Text = I18n.T("BtnCancel");
        }

        private void SaveSettings()
        {
            try
            {
                // 1. Language
                string lang = _cbLang.SelectedIndex == 1 ? "en" : "ro";
                I18n.CurrentLanguage = lang;

                // 2. Step
                int step = 5;
                if (_cbStep.SelectedItem != null)
                {
                    string s = _cbStep.SelectedItem.ToString().Replace("%", "").Trim();
                    int.TryParse(s, out step);
                }
                Storage.SaveStep(step);

                // 3. Startup
                string err;
                SystemSetup.SetStartup(_chkStartup.Checked, out err);

                // 4. Night Light
                NightLightService.SetEnabled(_chkNightLight.Checked);
                NightLightService.SetIntensity(0, _tbWarmth1.Value);
                NightLightService.SetIntensity(1, _tbWarmth2.Value);
                Storage.SaveNightLightIntensities(_tbWarmth1.Value, _tbWarmth2.Value);

                // 5. Custom Hotkeys
                Storage.SaveCustomHotkeys(
                    _chkEnableCustom.Checked,
                    _txtDown.Text.Trim(),
                    _txtUp.Text.Trim(),
                    _txtNext.Text.Trim(),
                    _txtNightLightKey.Text.Trim()
                );

                if (_onSettingsSaved != null)
                {
                    _onSettingsSaved();
                }

                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Eroare la salvare: " + ex.Message, "BrightKeys", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
