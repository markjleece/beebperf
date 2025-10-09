// --------------------------------------------------------------
// BeebPerf - A BBC Micro Profiler
//
// Copyright (C) 2025  Mark John Leece
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

using System.Diagnostics;
using System.DirectoryServices;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BeebPerf.ux
{
    internal class TimelineView : Panel
    {
        public TimelineView() : base()
        {
            _ScrollBar = new HScrollBar();
            _ScrollBar.Dock = DockStyle.Bottom;
            _ScrollBar.Scroll += ScrollBar_Scroll;
            _ScrollBar.Enabled = false;
            Controls.Add(_ScrollBar);

            _SelectionChangeTimer = new System.Windows.Forms.Timer();
            _SelectionChangeTimer.Interval = 500;
            _SelectionChangeTimer.Tick += SelectionChangeTimer_Tick;
        }

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            UpdateTimeline();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button != MouseButtons.Left)
                return;

            var mousePos = new Point(e.X, e.Y);
            if (_LeftHandleRect.Contains(mousePos))
            {
                _DragMode = DragMode.LeftHandle;
                _DragOffset = _LeftHandleRect.Right - e.X;
            }
            else if (_RightHandleRect.Contains(mousePos))
            {
                _DragMode = DragMode.RightHandle;
                _DragOffset = e.X - _RightHandleRect.Left;
            }
            else if (_TimelineRect.Contains(mousePos))
            {
                double seconds = PixelsToSeconds(e.X);
                if (seconds >= 0.0 && seconds <= _RecordingDuration)
                {
                    _DragMode = DragMode.Range;
                    _DragOrigin = e.X;
                    SetLeftHandleRect(e.X);
                    SetRightHandleRect(e.X + 1);
                    UpdateDurationText();
                    Update();
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            SetCursor(new Point(e.X, e.Y));

            switch (_DragMode)
            {
                case DragMode.None:
                    return;

                case DragMode.LeftHandle:
                    int value = Math.Clamp(e.X + _DragOffset, SecondsToPixels(0), SecondsToPixels(_AnalysisTo) - 1);
                    SetLeftHandleRect(value);
                    break;

                case DragMode.RightHandle:
                    value = Math.Clamp(e.X - _DragOffset, SecondsToPixels(_AnalysisFrom) + 1, SecondsToPixels(_RecordingDuration));
                    SetRightHandleRect(value);
                    break;

                case DragMode.Range:
                    int min = SecondsToPixels(0);
                    int max = SecondsToPixels(_RecordingDuration);

                    value = Math.Clamp((e.X > _DragOrigin) ? _DragOrigin : e.X, min, max);
                    SetLeftHandleRect(value);

                    value = Math.Clamp((e.X <= _DragOrigin) ? _DragOrigin + 1 : e.X + 1, min, max);
                    SetRightHandleRect(value);
                    break;
            }

            UpdateDurationText();
            Update();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (_DragMode == DragMode.None)
                return;

            if (_DragMode == DragMode.LeftHandle || _DragMode == DragMode.Range)
                _AnalysisFrom = PixelsToSeconds(_LeftHandleRect.Right - 1);
            
            if (_DragMode == DragMode.RightHandle || _DragMode == DragMode.Range)
                _AnalysisTo = PixelsToSeconds(_RightHandleRect.Left);

            if (_AnalysisFrom < 0) _AnalysisFrom = 0;
            if (_AnalysisTo > _RecordingDuration) _AnalysisTo = _RecordingDuration;
            Debug.Assert(_AnalysisFrom < _AnalysisTo);

            _DragMode = DragMode.None;

            _SelectionChangeTimer.Stop();
            _SelectionChangeTimer.Start();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // do nothing
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics graphics = e.Graphics;

            int displayFrom = _TimelineRect.Left;
            int displayTo = _TimelineRect.Right;

            // draw handles
            using var handleBrush = new SolidBrush(ForeColor);
            graphics.FillRectangle(handleBrush, _LeftHandleRect);
            graphics.FillRectangle(handleBrush, _RightHandleRect);

            graphics.ExcludeClip(_LeftHandleRect);
            graphics.ExcludeClip(_RightHandleRect);

            // draw background
            int left = SecondsToPixels(0);
            int right = SecondsToPixels(_RecordingDuration);

            using var exclusionBrush = new SolidBrush(Blend(BackColor, ForeColor, 0.2));
            var leftExclusionRect = new Rectangle(left, _TimelineRect.Top, _LeftHandleRect.Left - left, _TimelineRect.Height);
            if (graphics.IsVisible(leftExclusionRect))
                graphics.FillRectangle(exclusionBrush, leftExclusionRect);

            var rightExclusionRect = new Rectangle(_RightHandleRect.Right, _TimelineRect.Top, right - _RightHandleRect.Right, _TimelineRect.Height);
            if (graphics.IsVisible(rightExclusionRect))
                graphics.FillRectangle(exclusionBrush, rightExclusionRect);

            using var centerBrush = new SolidBrush(SystemColors.Highlight);
            var centerRect = new Rectangle(_LeftHandleRect.Right, _TimelineRect.Top, _RightHandleRect.Left - _LeftHandleRect.Right, _TimelineRect.Height);
            if (graphics.IsVisible(centerRect))
                graphics.FillRectangle(centerBrush, centerRect);

            GraphicsState state;
            var exclusionRect = new Rectangle(left, _TimelineRect.Top, right - left, _TimelineRect.Height);
            if (graphics.IsVisible(exclusionRect))
            {
                state = graphics.Save();
                graphics.ExcludeClip(exclusionRect);

                using var windowBrush = new SolidBrush(BackColor);
                graphics.FillRectangle(windowBrush, e.ClipRectangle);
                graphics.Restore(state);
            }

            // draw header text
            using var textBrush = new SolidBrush(ForeColor);
            graphics.DrawString(_DurationText, Font, textBrush, new PointF(0, 0));

            // draw top and bottom lines
            using var pen = new Pen(Blend(ForeColor, BackColor, 0.5));
            using var brush = new SolidBrush(ForeColor);

            graphics.DrawLine(pen, 0, _TimelineRect.Top, Width, _TimelineRect.Top);
            graphics.DrawLine(pen, 0, _TimelineRect.Bottom - 1, Width, _TimelineRect.Bottom - 1);

            // draw ruler ticks and text
            state = graphics.Save();
            graphics.ExcludeClip(centerRect);
            DrawRulerForeground(graphics, pen, brush);
            graphics.Restore(state);

            graphics.IntersectClip(centerRect);
            using var highlightPen = new Pen(Blend(SystemColors.Highlight, BackColor, 0.5));
            using var highlightBrush = new SolidBrush(BackColor);
            DrawRulerForeground(graphics, highlightPen, highlightBrush);
        }

        private void DrawRulerForeground(Graphics graphics, Pen pen, Brush brush)
        {
            // draw ticks
            foreach (var tick in _MajorTicks)
            {
                int xPos = SecondsToPixels(tick);
                graphics.DrawLine(pen, xPos, _TimelineRect.Top, xPos, _TimelineRect.Bottom - 1);
                string text = FormatSeconds(tick);
                graphics.DrawString(text, Font, brush, xPos, _TimelineRect.Top);
            }

            foreach (var tick in _MinorTicks)
            {
                int xPos = SecondsToPixels(tick);
                graphics.DrawLine(pen, xPos, _TimelineRect.Bottom - 1, xPos, _TimelineRect.Bottom - _TimelineRect.Height / 4);
            }
        }

        private void SetCursor(Point mousePos)
        {
            if (_DragMode != DragMode.None)
            {
                Cursor = Cursors.SizeWE;
                return;
            }

            if (_LeftHandleRect.Contains(mousePos) || _RightHandleRect.Contains(mousePos))
            {
                Cursor = Cursors.SizeWE;
                return;
            }

            if (_TimelineRect.Contains(mousePos))
            {
                double seconds = PixelsToSeconds(mousePos.X);
                if (seconds >= 0.0 && seconds <= _RecordingDuration)
                {
                    Cursor = Cursors.VSplit;
                    return;
                }
            }

            Cursor = Cursors.Default;
        }

        private Rectangle Union(Rectangle rect1, Rectangle rect2)
        {
            int minLeft = int.Min(rect1.Left, rect2.Left);
            int maxRight = int.Max(rect1.Right, rect2.Right);
            int minTop = int.Min(rect1.Top, rect2.Top);
            int maxBottom = int.Max(rect1.Bottom, rect2.Bottom);
            return new Rectangle(minLeft, minTop, maxRight - minLeft, maxBottom - minTop);
        }

        private void SetLeftHandleRect(int value)
        {
            int handleWidth = _TimelineRect.Height / 4;
            var rect = new Rectangle(
                value - handleWidth + 1,
                _TimelineRect.Top,
                handleWidth,
                _TimelineRect.Height);

            if (rect != _LeftHandleRect)
            {
                Invalidate(Union(rect, _LeftHandleRect));
                _LeftHandleRect = rect;
            }
        }

        private void SetRightHandleRect(int value)
        {
            int handleWidth = _TimelineRect.Height / 4;
            var rect = new Rectangle(
                value,
                _TimelineRect.Top,
                handleWidth,
                _TimelineRect.Height);

            if (rect != _RightHandleRect)
            {
                Invalidate(Union(rect, _RightHandleRect));
                _RightHandleRect = rect;
            }
        }

        private int SecondsToPixels(double value)
        {
            return (int)((value - _DisplayFrom) / (_DisplayTo - _DisplayFrom) * (double)Width);
        }

        private double PixelsToSeconds(int value)
        {
            return _DisplayFrom + ((double)value * (_DisplayTo - _DisplayFrom) / (double)Width);
        }

        private void UpdateDurationText()
        {
            double duration = _DragMode switch
            {
                DragMode.None => _AnalysisTo - _AnalysisFrom,
                DragMode.LeftHandle => _AnalysisTo - PixelsToSeconds(_LeftHandleRect.Right - 1),
                DragMode.RightHandle => PixelsToSeconds(_RightHandleRect.Left) - _AnalysisFrom,
                DragMode.Range => PixelsToSeconds(_RightHandleRect.Left) - PixelsToSeconds(_LeftHandleRect.Right - 1),
                _ => throw new NotImplementedException(),
            };

            string newText = $"Duration: {FormatSeconds(_RecordingDuration)}";
            if (duration != _RecordingDuration)
                newText += $" ({FormatSeconds(duration)} selected)";

            if (newText == _DurationText)
                return;

            var maxExtents = new Size(int.MaxValue, int.MaxValue);
            var previousSize = TextRenderer.MeasureText(_DurationText, Font, maxExtents, TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
            var newSize = TextRenderer.MeasureText(newText, Font, maxExtents, TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);

            var origin = new Point(0, 0);
            var previousRect = new Rectangle(origin, previousSize);
            var newRect = new Rectangle(origin, newSize);
            if (previousRect != newRect)
                Invalidate(Union(previousRect, newRect));

            _DurationText = newText;
        }

        private string FormatSeconds(double value)
        {
            if (value == 0)
                return "0";
            if (value >= 1.0)
                return $"{value:#.###}s";
            else if (value >= 1e-3)
                return $"{(value * 1e3):#.###}ms";
            else
                return $"{(value * 1e6):#.#}µs";
        }

        public void SetDuration(int recordingDuration)
        {
            _RecordingDuration = (double)recordingDuration / 2_000_000;
            _AnalysisFrom = 0;
            _AnalysisTo = _RecordingDuration;
            ZoomOut();
        }

        private void ScrollBar_Scroll(object? sender, ScrollEventArgs e)
        {
            double deltaSeconds = PixelsToSeconds(e.NewValue) - PixelsToSeconds(e.OldValue);
            _DisplayFrom += deltaSeconds;
            _DisplayTo += deltaSeconds;

            SetLeftHandleRect(SecondsToPixels(_AnalysisFrom));
            SetRightHandleRect(SecondsToPixels(_AnalysisTo));

            UpdateDurationText();
            UpdateTimeline();
        }

        public bool CanZoomIn()
        {
            return (_AnalysisFrom != 0.0 || _AnalysisTo != _RecordingDuration);
        }

        public void ZoomIn()
        {
            int margin = Font.Height * 4;
            int range = Width - margin * 2;
            double extends = (_AnalysisTo - _AnalysisFrom) * margin / range;
            _DisplayFrom = _AnalysisFrom - extends;
            _DisplayTo = _AnalysisTo + extends;

            SetLeftHandleRect(SecondsToPixels(_AnalysisFrom));
            SetRightHandleRect(SecondsToPixels(_AnalysisTo));

            UpdateDurationText();
            UpdateTimeline();

            _ScrollBar.Minimum = SecondsToPixels(-extends);
            _ScrollBar.Maximum = SecondsToPixels(_RecordingDuration + extends);
            _ScrollBar.LargeChange = SecondsToPixels(_DisplayFrom + (_AnalysisTo - _AnalysisFrom) + extends * 2);
            _ScrollBar.SmallChange = Width / DeviceDpi;
            _ScrollBar.Value = 0;
            _ScrollBar.Enabled = true;
        }

        public bool CanZoomOut()
        {
            return _ScrollBar.Enabled;
        }

        public void ZoomOut()
        {
            int margin = Font.Height * 4;
            int range = Width - margin * 2;
            double extends = _RecordingDuration * margin / range;

            _DisplayFrom = -extends;
            _DisplayTo = _RecordingDuration + extends;

            SetLeftHandleRect(SecondsToPixels(_AnalysisFrom));
            SetRightHandleRect(SecondsToPixels(_AnalysisTo));

            UpdateDurationText();
            UpdateTimeline();

            _ScrollBar.Enabled = false;
        }

        public bool CanSelectAll()
        {
            return (_AnalysisFrom != 0.0 || _AnalysisTo != _RecordingDuration);
        }

        public void SelectAll()
        {
            _AnalysisFrom = 0;
            _AnalysisTo = _RecordingDuration;

            ZoomOut();

            _SelectionChangeTimer.Stop();
            _SelectionChangeTimer.Start();
        }

        private int TickCount()
        {
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
            {
                return (int)float.Round((float)Width / g.DpiX);
            }
        }

        private void UpdateTimeline()
        {
            int margin = Font.Height;
            _TimelineRect = new Rectangle(
                0,
                margin,
                Width,
                Height - margin - _ScrollBar.Height);

            ComputeTicks();
            Invalidate();
        }

        private void ComputeTicks()
        {
            if (_DisplayTo <= _DisplayFrom) return;

            double range = (double)(_DisplayTo - _DisplayFrom);
            double rawSpacing = range / TickCount();
            double magnitude = Math.Pow(10, Math.Floor(Math.Log10(rawSpacing)));
            double residual = rawSpacing / magnitude;

            double majorSpacing;
            if (residual <= 1)
                majorSpacing = 1 * magnitude;
            else if (residual <= 2)
                majorSpacing = 2 * magnitude;
            else if (residual <= 5)
                majorSpacing = 5 * magnitude;
            else
                majorSpacing = 10 * magnitude;

            double minorDivisions;
            if (residual <= 1.0) 
                minorDivisions = 5;
            else if (residual <= 2.0) 
                minorDivisions = 4;
            else if (residual <= 3.5) 
                minorDivisions = 5;
            else if (residual <= 7.5) 
                minorDivisions = 4;
            else 
                minorDivisions = 2;

            double minorSpacing = majorSpacing / (minorDivisions + 1);
            double firstMinorTick = Math.Ceiling(_DisplayFrom / minorSpacing) * minorSpacing;
            double lastMinorTick = Math.Floor(_DisplayTo / minorSpacing) * minorSpacing;

            _MinorTicks.Clear();
            _MajorTicks.Clear();

            for (double minorTick = firstMinorTick; minorTick <= lastMinorTick + 1e-10; minorTick += minorSpacing)
            {
                int index = (int)Math.Round(minorTick / minorSpacing);
                if (index % (minorDivisions + 1) == 0)
                    _MajorTicks.Add(Math.Round(minorTick, 10));
                else
                    _MinorTicks.Add(Math.Round(minorTick, 10));
            }
        }

        private Color Blend(Color first, Color second, double ratio)
        {
            int r = (int)(first.R * (1 - ratio) + second.R * ratio);
            int g = (int)(first.G * (1 - ratio) + second.G * ratio);
            int b = (int)(first.B * (1 - ratio) + second.B * ratio);
            return Color.FromArgb(r, g, b);
        }

        private Form GetParentForm()
        {
            Control control = this;
            while (control is not Form)
            {
                control = control!.Parent!;
            }
            return (Form)control;
        }

        private void SelectionChangeTimer_Tick(object? sender, EventArgs e)
        {
            _SelectionChangeTimer.Stop();

            var form = (BeebPerfForm)GetParentForm();
            int cyclesFrom = (int)(_AnalysisFrom * 2_000_000.0);
            int cyclesTo = (int)(_AnalysisTo * 2_000_000.0);
            form.DynamicAnalysis(cyclesFrom, cyclesTo);
        }

        private enum DragMode
        {
            None = 0,
            LeftHandle = 1,
            RightHandle = 2,
            Range = 3
        }

        private double _RecordingDuration;
        private double _DisplayFrom;
        private double _DisplayTo;
        private double _AnalysisFrom;
        private double _AnalysisTo;
        private string _DurationText = string.Empty;
        private Rectangle _TimelineRect;
        private Rectangle _LeftHandleRect;
        private Rectangle _RightHandleRect;
        private DragMode _DragMode;
        private int _DragOffset;
        private int _DragOrigin;
        private List<double> _MajorTicks = [];
        private List<double> _MinorTicks = [];
        private HScrollBar _ScrollBar;
        private System.Windows.Forms.Timer _SelectionChangeTimer;
    }
}