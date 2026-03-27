// --------------------------------------------------------------
// BeebPerf - A BBC Micro Profiler
//
// Copyright (C) 2026  Mark John Leece
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public
// License along with this program; if not, write to the Free
// Software Foundation, Inc., 51 Franklin Street, Fifth Floor,
// Boston, MA  02110-1301, USA.
// --------------------------------------------------------------

namespace BeebPerf.ux
{
    public class TabControlEx : Panel
    {
        public TabControlEx() : base()
        {
            ControlAdded += ControlAddedFunc;
            ControlRemoved += ControlRemovedFunc;
            MouseClick += MouseClickFunc;
        }

        public event EventHandler? SelectedIndexChanged;
        public event EventHandler? SelectedTabChanged;

        public int SelectedIndex
        {
            get
            {
                return _SelectedIndex;
            }
            set
            {
                if (value < -1 || value >= Controls.Count)
                    throw new ArgumentOutOfRangeException("SelectedIndex");

                if (value != _SelectedIndex)
                {
                    _SelectedIndex = value;
                    _SelectedTab = (value >= 0) ? (Panel)Controls[value] : null;
                    SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
                    SelectedTabChanged?.Invoke(this, EventArgs.Empty);
                    UpdateTabs();
                    if (_SelectedTab != null)
                        UpdateGridViewButtons(_SelectedTab);
                }
            }
        }

        public Panel? SelectedTab
        {
            get
            {
                return _SelectedTab;
            }
            set
            {
                int index = Controls.IndexOf(value);
                if (value != null && index == -1)
                    throw new ArgumentOutOfRangeException("SelectedTab");

                if (value != _SelectedTab)
                {
                    _SelectedTab = value;
                    _SelectedIndex = index;
                    SelectedTabChanged?.Invoke(this, EventArgs.Empty);
                    SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
                    UpdateTabs();
                    if (_SelectedTab != null)
                        UpdateGridViewButtons(_SelectedTab);
                }
            }
        }

        private void UpdateGridViewButtons(Control control)
        {
            var dataView = control as IDataView;
            if (dataView != null)
                dataView.UpdateButtons();

            foreach (Control child in control.Controls)
                UpdateGridViewButtons(child);
        }

        private void MouseClickFunc(object? sender, MouseEventArgs e)
        {
            for (int tabIndex = 0; tabIndex < Controls.Count; tabIndex++)
            {
                var tabBounds = GetTabBounds(tabIndex);
                if (tabBounds.Contains(e.Location))
                {
                    SelectedIndex = tabIndex;
                    break;
                }
            }
        }

        private void ControlAddedFunc(object? sender, ControlEventArgs e)
        {
            UpdateTabs();
        }

        private void ControlRemovedFunc(object? sender, ControlEventArgs e)
        {
            UpdateTabs();
        }

        private void UpdateTabs()
        {
            foreach (Control control in Controls)
            {
                bool visible = (control == SelectedTab);
                if (visible != control.Visible)
                    control.Visible = visible;
            }

            Invalidate();
        }

        private Rectangle GetTabBounds(int tabIndex)
        {
            if (tabIndex < 0 || tabIndex >= Controls.Count)
                throw new ArgumentOutOfRangeException("tabIndex");

            int x = 0;
            for (int index = 0; index < tabIndex; index++)
            {
                x += MeasureTab(index).Width;
            }

            var tabSize = MeasureTab(tabIndex);
            return new Rectangle(x, 0, tabSize.Width, tabSize.Height);
        }

        private Size MeasureTab(int tabIndex)
        {
            if (tabIndex < 0 || tabIndex >= Controls.Count)
                throw new ArgumentOutOfRangeException("tabIndex");

            var tabPage = (Panel)Controls[tabIndex];
            var measure = TextRenderer.MeasureText(
                tabPage.Text,
                Font,
                new Size(int.MaxValue, int.MaxValue),
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);

            int padding = Font.Height;
            return new Size(measure.Width + 2 * padding, TabHeight);
        }

        private int TabHeight => Font.Height * 2;

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);

            foreach (Control child in Controls)
            {
                child.Location = new Point(0, TabHeight);
                child.Size = new Size(Width, Height - TabHeight);
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics graphics = e.Graphics;

            using var pen = new Pen(SystemColors.ControlText);

            int x = 0;
            for (int tabIndex = 0; tabIndex < Controls.Count; tabIndex++)
            {
                var tabPage = (Panel)Controls[tabIndex];
                var tabBounds = GetTabBounds(tabIndex);
                bool isSelected = (tabIndex == SelectedIndex);

                Color backColor = isSelected ? SystemColors.Highlight : SystemColors.Window;
                Color foreColor = isSelected ? SystemColors.HighlightText : SystemColors.ControlText;

                using var backgroundBrush = new SolidBrush(backColor);
                using var textBrush = new SolidBrush(foreColor);

                graphics.FillRectangle(backgroundBrush, tabBounds);
                var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                var font = new Font(Font, FontStyle.Bold);
                graphics.DrawString(tabPage.Text, font, textBrush, tabBounds, format);

                graphics.DrawLine(pen, tabBounds.Left, 0, tabBounds.Right - 1, 0);
                graphics.DrawLine(pen, tabBounds.Right - 1, 0, tabBounds.Right - 1, TabHeight);

                x = tabBounds.Right;
            }

            graphics.DrawLine(pen, 0, TabHeight - 1, Width, TabHeight - 1);
        }

        private Panel? _SelectedTab = null;
        private int _SelectedIndex = -1;
    }
}