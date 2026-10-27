using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BrightKeys
{
    internal class OsdRow
    {
        public string Label = "";
        public int? Percent;
        public float CurrentPercent = -1f;
        public float TargetPercent = -1f;
        public bool Highlight;
        public bool Unavailable;
    }

    internal class OsdForm : Form
    {
        private string _title = "";
        private string _subtitle = "";
        private List<OsdRow> _rows = new List<OsdRow>();
        private readonly Timer _hideTimer;
        private readonly Timer _animTimer;
        private bool _autoClose;

        private const int WidthPx = 400;
        private const int RowHeight = 42;
        private const int HeaderHeight = 58;

        public OsdForm()
            : this("", "", new List<OsdRow>(), false)
        {
        }

        public OsdForm(string title, string subtitle, List<OsdRow> rows)
            : this(title, subtitle, rows, false)
        {
        }

        public OsdForm(string title, string subtitle, List<OsdRow> rows, bool autoClose)
        {
            _autoClose = autoClose;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            BackColor = Color.FromArgb(30, 30, 34);
            DoubleBuffered = true;
            AutoScaleMode = AutoScaleMode.None;

            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            UpdateStyles();

            _hideTimer = new Timer();
            _hideTimer.Interval = 1600;
            _hideTimer.Tick += delegate
            {
                _hideTimer.Stop();
                if (_autoClose) Close();
                else Hide();
            };

            _animTimer = new Timer();
            _animTimer.Interval = 16; // ~60 FPS smooth interpolation
            _animTimer.Tick += OnAnimTick;

            if (rows != null && (title.Length > 0 || rows.Count > 0))
                UpdateContent(title, subtitle, rows);
        }

        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= Native.WS_EX_NOACTIVATE | Native.WS_EX_TOOLWINDOW | Native.WS_EX_TOPMOST;
                return cp;
            }
        }

        private void OnAnimTick(object sender, EventArgs e)
        {
            bool stillAnimating = false;
            if (_rows != null)
            {
                for (int i = 0; i < _rows.Count; i++)
                {
                    OsdRow row = _rows[i];
                    if (row.Percent.HasValue && row.CurrentPercent >= 0)
                    {
                        float diff = row.TargetPercent - row.CurrentPercent;
                        if (Math.Abs(diff) > 0.15f)
                        {
                            row.CurrentPercent += diff * 0.35f;
                            stillAnimating = true;
                        }
                        else
                        {
                            row.CurrentPercent = row.TargetPercent;
                        }
                    }
                }
            }

            Invalidate();

            if (!stillAnimating)
            {
                _animTimer.Stop();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try { if (_hideTimer != null) { _hideTimer.Stop(); _hideTimer.Dispose(); } } catch { }
                try { if (_animTimer != null) { _animTimer.Stop(); _animTimer.Dispose(); } } catch { }
            }
            base.Dispose(disposing);
        }

        public void UpdateContent(string title, string subtitle, List<OsdRow> rows)
        {
            bool titleChanged = !string.Equals(_title, title, StringComparison.OrdinalIgnoreCase);
            _title = title ?? "";
            _subtitle = subtitle ?? "";

            if (rows != null)
            {
                for (int i = 0; i < rows.Count; i++)
                {
                    OsdRow newRow = rows[i];
                    if (newRow.Percent.HasValue)
                    {
                        newRow.TargetPercent = newRow.Percent.Value;

                        // Match by row label so transitions within the same menu smoothly animate,
                        // but if title changed (Night Light vs Brightness), don't inherit unrelated percentages.
                        OsdRow matchingPrev = null;
                        if (!titleChanged && _rows != null)
                        {
                            foreach (OsdRow prev in _rows)
                            {
                                if (string.Equals(prev.Label, newRow.Label, StringComparison.OrdinalIgnoreCase) && prev.CurrentPercent >= 0)
                                {
                                    matchingPrev = prev;
                                    break;
                                }
                            }
                        }

                        if (matchingPrev != null)
                        {
                            newRow.CurrentPercent = matchingPrev.CurrentPercent;
                        }
                        else
                        {
                            newRow.CurrentPercent = newRow.TargetPercent;
                        }
                    }
                }
                _rows = rows;
            }

            bool needsAnim = false;
            if (_rows != null)
            {
                foreach (OsdRow r in _rows)
                {
                    if (r.Percent.HasValue && Math.Abs(r.TargetPercent - r.CurrentPercent) > 0.15f)
                    {
                        needsAnim = true;
                        break;
                    }
                }
            }

            if (needsAnim && !_animTimer.Enabled)
            {
                _animTimer.Start();
            }

            int height = HeaderHeight + (_rows != null ? _rows.Count : 0) * RowHeight + 12;
            if (ClientSize.Height != height || ClientSize.Width != WidthPx)
            {
                ClientSize = new Size(WidthPx, height);
                if (Region != null)
                {
                    try { Region.Dispose(); } catch { }
                }
                Region = new Region(RoundedPath(new Rectangle(0, 0, WidthPx, height), 14));

                Rectangle area = Screen.PrimaryScreen.WorkingArea;
                try
                {
                    Native.POINT pt;
                    if (Native.GetCursorPos(out pt))
                    {
                        IntPtr hMon = Native.MonitorFromPoint(pt, Native.MONITOR_DEFAULTTONEAREST);
                        Native.MONITORINFOEX mi = new Native.MONITORINFOEX();
                        mi.cbSize = Marshal.SizeOf(typeof(Native.MONITORINFOEX));
                        if (Native.GetMonitorInfo(hMon, ref mi))
                            area = Rectangle.FromLTRB(mi.rcWork.Left, mi.rcWork.Top, mi.rcWork.Right, mi.rcWork.Bottom);
                    }
                }
                catch { }
                Location = new Point(area.Left + (area.Width - WidthPx) / 2, area.Bottom - height - 40);
            }

            Invalidate();
            if (!Visible)
            {
                Show();
                Native.SetWindowPos(Handle, (IntPtr)Native.HWND_TOPMOST, 0, 0, 0, 0,
                    Native.SWP_NOMOVE | Native.SWP_NOSIZE | Native.SWP_NOACTIVATE | Native.SWP_SHOWWINDOW);
            }

            _hideTimer.Stop();
            _hideTimer.Start();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(Color.FromArgb(30, 30, 34));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using (GraphicsPath borderPath = RoundedPath(new Rectangle(0, 0, Width - 1, Height - 1), 14))
            using (Pen border = new Pen(Color.FromArgb(60, 255, 255, 255), 1))
            {
                g.DrawPath(border, borderPath);
            }

            using (Font titleFont = new Font("Segoe UI", 11.5f, FontStyle.Bold, GraphicsUnit.Point))
            using (Font subFont = new Font("Segoe UI", 8.5f, FontStyle.Regular, GraphicsUnit.Point))
            using (Font rowFont = new Font("Segoe UI", 9.25f, FontStyle.Regular, GraphicsUnit.Point))
            using (Font pctFont = new Font("Segoe UI", 9.25f, FontStyle.Bold, GraphicsUnit.Point))
            {
                bool isNightLight = _title.IndexOf("noapte", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    _title.IndexOf("night", StringComparison.OrdinalIgnoreCase) >= 0;
                Color accent = isNightLight ? Color.FromArgb(255, 179, 71) : Color.FromArgb(79, 195, 247);
                Color dim = Color.FromArgb(170, 170, 180);

                TextRenderer.DrawText(g, _title, titleFont, new Point(16, 12), Color.White,
                    TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
                if (_subtitle.Length > 0)
                    TextRenderer.DrawText(g, _subtitle, subFont, new Point(17, 34), dim,
                        TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);

                int y = HeaderHeight - 6;
                foreach (OsdRow row in _rows)
                {
                    Color labelColor = row.Highlight ? Color.White : dim;

                    if (row.Highlight)
                    {
                        using (SolidBrush dot = new SolidBrush(accent))
                            g.FillEllipse(dot, 16, y + 5, 7, 7);
                    }

                    TextRenderer.DrawText(g, row.Label, rowFont, new Point(30, y), labelColor,
                        TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);

                    if (row.Percent.HasValue)
                    {
                        float curVal = row.CurrentPercent >= 0 ? row.CurrentPercent : row.Percent.Value;
                        int displayPct = (int)Math.Round(curVal);
                        string pct = displayPct + "%";
                        Size ps = TextRenderer.MeasureText(g, pct, pctFont, Size.Empty, TextFormatFlags.NoPadding);
                        TextRenderer.DrawText(g, pct, pctFont, new Point(Width - 16 - ps.Width, y), labelColor,
                            TextFormatFlags.NoPadding);

                        int barY = y + 20;
                        int barX = 30;
                        int barW = Width - 30 - 16;
                        using (GraphicsPath bgPath = RoundedPath(new Rectangle(barX, barY, barW, 6), 3))
                        using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(58, 58, 66)))
                            g.FillPath(bgBrush, bgPath);

                        int fillW = Math.Max(0, Math.Min(barW, (int)Math.Round(barW * curVal / 100f)));
                        if (fillW > 0)
                        {
                            using (GraphicsPath fillPath = RoundedPath(new Rectangle(barX, barY, fillW, 6), 3))
                            using (SolidBrush fillBrush = new SolidBrush(row.Highlight ? accent : Color.FromArgb(110, 110, 120)))
                                g.FillPath(fillBrush, fillPath);
                        }
                    }
                    else if (row.Unavailable)
                    {
                        TextRenderer.DrawText(g, "indisponibil", subFont, new Point(Width - 100, y + 2),
                            Color.FromArgb(160, 130, 130), TextFormatFlags.NoPadding);
                    }
                    y += RowHeight;
                }
            }
        }

        private static GraphicsPath RoundedPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;
            if (d <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
