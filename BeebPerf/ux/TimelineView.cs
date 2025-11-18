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

using static BeebPerf.VideoAnalysis;

namespace BeebPerf.ux
{
    internal class TimelineView : Panel
    {
        public static int MinHeight = 7; // seven characters high

        public TimelineView() : base()
        {
            DoubleBuffered = true;
            _ScrollBar = new HScrollBar();
            _ScrollBar.Dock = DockStyle.Bottom;
            _ScrollBar.Scroll += ScrollBar_Scroll;
            _ScrollBar.Enabled = false;
            Controls.Add(_ScrollBar);
            Layout += LayoutFunc;
        }

        public List<FrameBitmap> FrameBitmaps
        {
            get => _FrameBitmaps;
            set
            {
                _FrameBitmaps = value;
                Invalidate();
            }
        }

        public void SelectRange(int analysisFrom, int analysisTo)
        {
            using var token = _ReentrancyGuard.TryEnter();
            if (token == null) return;

            SelectRangeInternal(analysisFrom, analysisTo);
        }

        public int Duration
        {
            get => _RecordingDuration;
            set => SetDurationInternal(value);
        }

        public bool CanZoomIn()
        {
            return (_AnalysisFrom != 0.0 || _AnalysisTo != _RecordingDuration);
        }

        public void ZoomIn()
        {
            using var token = _ReentrancyGuard.TryEnter();
            if (token == null) return;

            ZoomInInternal();
        }

        public bool CanZoomOut()
        {
            return _ScrollBar.Enabled;
        }

        public void ZoomOut()
        {
            using var token = _ReentrancyGuard.TryEnter();
            if (token == null) return;

            ZoomOutInternal();
        }

        public bool CanSelectAll()
        {
            return (_AnalysisFrom != 0.0 || _AnalysisTo != _RecordingDuration);
        }

        public void SelectAll()
        {
            using var token = _ReentrancyGuard.TryEnter();
            if (token == null) return;

            SelectAllInternal();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (_ScrollBar.Enabled)
            {
                _DisplayTo = _DisplayFrom + MulDiv(_DisplayTo - _DisplayFrom, Width, _TimelineRect.Width);
                UpdateTimeline();
            }
            else
            {
                ZoomOutInternal();
            }
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
                int cycles = PixelsToCycles(e.X);
                if (cycles >= 0 && cycles <= _RecordingDuration)
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
                    int value = Math.Clamp(e.X + _DragOffset, CyclesToPixels(0), CyclesToPixels(_AnalysisTo) - 1);
                    SetLeftHandleRect(value);
                    break;

                case DragMode.RightHandle:
                    value = Math.Clamp(e.X - _DragOffset, CyclesToPixels(_AnalysisFrom) + 1, CyclesToPixels(_RecordingDuration));
                    SetRightHandleRect(value);
                    break;

                case DragMode.Range:
                    int min = CyclesToPixels(0);
                    int max = CyclesToPixels(_RecordingDuration);

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
                _AnalysisFrom = PixelsToCycles(_LeftHandleRect.Right - 1);

            if (_DragMode == DragMode.RightHandle || _DragMode == DragMode.Range)
                _AnalysisTo = PixelsToCycles(_RightHandleRect.Left);

            _AnalysisFrom = Math.Clamp(_AnalysisFrom, 0, _RecordingDuration - 1);
            _AnalysisTo = Math.Clamp(_AnalysisTo, 1, _RecordingDuration);

            if (_AnalysisFrom >= _AnalysisTo)
                _AnalysisTo = _AnalysisFrom + 1;

            _DragMode = DragMode.None;

            OnRangeChange();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics graphics = e.Graphics;

            if (_RecordingDuration == 0)
            {
                PaintDefaultRuler(e);
                return;
            }

            // draw ruler background
            int left = CyclesToPixels(0);
            int right = CyclesToPixels(_RecordingDuration);

            if (left < 0) left = 0;
            if (right > Width) right = Width;

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

            // draw top and bottom lines
            using var pen = new Pen(Blend(ForeColor, BackColor, 0.5));
            using var brush = new SolidBrush(ForeColor);

            graphics.DrawLine(pen, 0, _TimelineRect.Bottom - 1, Width, _TimelineRect.Bottom - 1);

            // draw handles
            using var handleBrush = new SolidBrush(ForeColor);
            graphics.FillRectangle(handleBrush, _LeftHandleRect);
            graphics.FillRectangle(handleBrush, _RightHandleRect);

            // draw ruler ticks and text
            var state = graphics.Save();
            graphics.ExcludeClip(centerRect);
            DrawRulerForeground(graphics, pen, brush);
            graphics.Restore(state);

            state = graphics.Save();
            graphics.IntersectClip(centerRect);
            using var highlightPen = new Pen(Blend(SystemColors.Highlight, BackColor, 0.5));
            using var highlightBrush = new SolidBrush(BackColor);
            DrawRulerForeground(graphics, highlightPen, highlightBrush);
            graphics.Restore(state);

            // draw thumbnails (pixels in src image are 1/2 width)
            DrawThumbnails(graphics);
        }

        private void DrawThumbnails(Graphics graphics)
        {
            if (_ThumbnailRect.Height == 0)
                return;

            StringFormat textFormat = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
            };

            using var blackBrush = new SolidBrush(Color.Black);
            using var greyPen = new Pen(Color.Gray);
            using var textBrush = new SolidBrush(ForeColor);

            int frameHeight = _ThumbnailRect.Height - Font.Height;
            int frameWidth = 4 * frameHeight / 3;
            int fromLeft = CyclesToPixels(0);
            int frameSkipCount = 0;

            for (int i = 0; i < FrameBitmaps.Count; i++)
            {
                FrameBitmap frameBitmap = FrameBitmaps[i];
                int pixels = CyclesToPixels(frameBitmap.StartCycleCount);
                if (pixels < fromLeft || pixels > Width)
                {
                    frameSkipCount++;
                    continue;
                }

                if (frameSkipCount > 0)
                {
                    frameSkipCount = 0;
                    frameBitmap = FrameBitmaps[--i];
                    pixels = CyclesToPixels(frameBitmap.StartCycleCount);
                }

                if (pixels < Width && pixels + frameWidth > 0)
                {
                    double aspectRatio = 0.5 * frameBitmap.Bitmap!.Width / frameBitmap.Bitmap!.Height;
                    int bitmapWidth = Math.Min((int)double.Round(aspectRatio * frameHeight), frameWidth);
                    int indent = (frameWidth - bitmapWidth) / 2;

                    var imageRect = new Rectangle(
                        pixels + indent,
                        _ThumbnailRect.Top,
                        bitmapWidth,
                        frameHeight);

                    var frameRect = new Rectangle(
                        pixels,
                        _ThumbnailRect.Top,
                        frameWidth,
                        frameHeight);

                    graphics.FillRectangle(blackBrush, frameRect);
                    graphics.DrawImage(frameBitmap.Bitmap, imageRect);
                    graphics.DrawRectangle(greyPen, frameRect);

                    var textRect = new Rectangle(
                        pixels,
                        _ThumbnailRect.Top + frameHeight,
                        frameWidth,
                        Font.Height);

                    graphics.DrawString($"{frameBitmap.FrameNumber}", Font, textBrush, textRect, textFormat);

                    frameRect = new Rectangle(
                        frameRect.Left,
                        frameRect.Top,
                        frameRect.Width + 1, 
                        frameHeight + 1);
                    graphics.ExcludeClip(frameRect);
                }

                fromLeft = pixels + frameWidth;
            }
        }

        private void DrawRulerForeground(Graphics graphics, Pen pen, Brush brush)
        {
            foreach (var tick in _Ticks)
            {
                int tickHeight = tick.Size switch
                {
                    TickSize.Major => _TimelineRect.Height,
                    TickSize.Medium => _TimelineRect.Height / 2,
                    TickSize.Minor => _TimelineRect.Height / 4,
                    _ => throw new NotImplementedException()
                };

                int xPos = SecondsToPixels(tick.Position);
                graphics.DrawLine(pen, xPos, _TimelineRect.Bottom - tickHeight, xPos, _TimelineRect.Bottom - 1);

                if (tick.Size == TickSize.Major)
                {
                    string text = FormatSeconds(tick.Position);
                    graphics.DrawString(text, Font, brush, xPos, _TimelineRect.Top);
                }
            }
        }

        private void PaintDefaultRuler(PaintEventArgs e)
        {
            Graphics graphics = e.Graphics;

            // draw header text
            using var textBrush = new SolidBrush(ForeColor);
            graphics.DrawString("Duration:", Font, textBrush, new PointF(0, 0));

            // draw default ruler
            using var pen = new Pen(Blend(ForeColor, BackColor, 0.5));
            using var brush = new SolidBrush(ForeColor);

            graphics.DrawLine(pen, 0, _TimelineRect.Top, Width, _TimelineRect.Top);
            graphics.DrawLine(pen, 0, _TimelineRect.Bottom - 1, Width, _TimelineRect.Bottom - 1);

            int origin = Font.Height * 4;
            double maxSeconds = 4.0 * (Width - origin) / graphics.DpiX;
            for (double seconds = 0.0; seconds < maxSeconds; seconds += 1.0)
            {
                int xPos = origin + (int)(seconds * (Width - origin) / maxSeconds);
                if ((int)Math.Round(seconds) % 5 == 0)
                {
                    graphics.DrawLine(pen, xPos, _TimelineRect.Top, xPos, _TimelineRect.Bottom - 1);
                    graphics.DrawString(FormatSeconds(seconds), Font, brush, xPos, _TimelineRect.Top);
                }
                else
                {
                    graphics.DrawLine(pen, xPos, _TimelineRect.Bottom - 1, xPos, _TimelineRect.Bottom - _TimelineRect.Height / 4);
                }
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
                int cycles = PixelsToCycles(mousePos.X);
                if (cycles >= 0.0 && cycles <= _RecordingDuration)
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

        private double CyclesToSeconds(int value)
        {
            return (double)value / 2_000_000;
        }

        private int SecondsToCycles(double value)
        {
            return (int)double.Round(value * 2_000_000);
        }

        private int CyclesToPixels(int value)
        {
            return MulDiv(Width, value - _DisplayFrom, _DisplayTo - _DisplayFrom);
        }

        private int PixelsToCycles(int value)
        {
            return _DisplayFrom + MulDiv(value, _DisplayTo - _DisplayFrom, Width);
        }

        private int SecondsToPixels(double value)
        {
            return MulDiv(Width, SecondsToCycles(value) - _DisplayFrom, _DisplayTo - _DisplayFrom);
        }

        private void UpdateDurationText()
        {
            int cycles = _DragMode switch
            {
                DragMode.None => _AnalysisTo - _AnalysisFrom,
                DragMode.LeftHandle => _AnalysisTo - PixelsToCycles(_LeftHandleRect.Right - 1),
                DragMode.RightHandle => PixelsToCycles(_RightHandleRect.Left) - _AnalysisFrom,
                DragMode.Range => PixelsToCycles(_RightHandleRect.Left) - PixelsToCycles(_LeftHandleRect.Right - 1),
                _ => throw new NotImplementedException(),
            };

            string durationText = $"Duration: {FormatCycles(_RecordingDuration)}";
            if (cycles != _RecordingDuration)
                durationText += $" ({FormatCycles(cycles)} selected)";

            var form = (BeebPerfForm)GetParentForm();
            form.StatusText = durationText;
        }

        private string FormatCycles(int value)
        {
            if (value == 0)
                return "0";
            else if (value < 1000)
                return $"{value} cycles";
            else
                return FormatSeconds(CyclesToSeconds(value));
        }

        private string FormatSeconds(double value)
        {
            if (value == 0.0)
                return "0";
            else if (value >= 1.0)
                return $"{(value):#.###}s";
            else if (value >= 1e-3)
                return $"{(value * 1e3):#.###}ms";
            else
                return $"{(value * 1e6):#.#}µs";
        }

        private void SelectRangeInternal(int analysisFrom, int analysisTo)
        {
            _AnalysisFrom = Math.Max(0, analysisFrom);
            _AnalysisTo = Math.Min(_RecordingDuration, analysisTo);
            ZoomOutInternal();
            OnRangeChange();
        }

        private void SetDurationInternal(int recordingDuration)
        {
            _RecordingDuration = recordingDuration;
            _AnalysisFrom = 0;
            _AnalysisTo = recordingDuration;
            ZoomOutInternal();
        }

        private void ScrollBar_Scroll(object? sender, ScrollEventArgs e)
        {
            int deltaCycles = PixelsToCycles(e.NewValue) - PixelsToCycles(e.OldValue);
            _DisplayFrom += deltaCycles;
            _DisplayTo += deltaCycles;

            SetLeftHandleRect(CyclesToPixels(_AnalysisFrom));
            SetRightHandleRect(CyclesToPixels(_AnalysisTo));

            UpdateDurationText();
            UpdateTimeline();
        }

        private void ZoomInInternal()
        { 
            int margin = Font.Height * 4;
            int range = Width - margin * 2;
            int analysisSize = _AnalysisTo - _AnalysisFrom;
            int extends = MulDiv(analysisSize, margin, range);

            int minDisplayRange = 4 * Width;
            int minExtends = (minDisplayRange - analysisSize) / 2;
            if (minExtends > extends)
                extends = minExtends;

            _DisplayFrom = _AnalysisFrom - extends;
            _DisplayTo = _AnalysisTo + extends;

            SetLeftHandleRect(CyclesToPixels(_AnalysisFrom));
            SetRightHandleRect(CyclesToPixels(_AnalysisTo));

            UpdateDurationText();
            UpdateTimeline();

            _ScrollBar.Minimum = CyclesToPixels(-extends);
            _ScrollBar.Maximum = CyclesToPixels(_RecordingDuration + extends);
            _ScrollBar.LargeChange = CyclesToPixels(_DisplayFrom + (_AnalysisTo - _AnalysisFrom) + extends * 2);
            _ScrollBar.SmallChange = Width / DeviceDpi;
            _ScrollBar.Value = 0;
            _ScrollBar.Enabled = true;
        }

        private void ZoomOutInternal()
        { 
            int margin = Font.Height * 4;
            int range = Width - margin * 2;
            int extends = MulDiv(_RecordingDuration, margin, range);

            _DisplayFrom = -extends;
            _DisplayTo = _RecordingDuration + extends;

            SetLeftHandleRect(CyclesToPixels(_AnalysisFrom));
            SetRightHandleRect(CyclesToPixels(_AnalysisTo));

            UpdateDurationText();
            UpdateTimeline();

            _ScrollBar.Enabled = false;
        }

        private void SelectAllInternal()
        {
            _AnalysisFrom = 0;
            _AnalysisTo = _RecordingDuration;
            ZoomOutInternal();
            OnRangeChange();
        }

        private void LayoutFunc(object? sender, LayoutEventArgs e)
        {
            SetLeftHandleRect(CyclesToPixels(_AnalysisFrom));
            SetRightHandleRect(CyclesToPixels(_AnalysisTo));
            UpdateDurationText();
            UpdateTimeline();
        }

        private void UpdateTimeline()
        {
            int dividerHeight = Math.Max(2, Font.Height / 8);
            int rulerHeight = 3 * Font.Height / 2;
            int thumbnailHeight = Height - _ScrollBar.Height - rulerHeight - dividerHeight;

            if (thumbnailHeight < 0)
                thumbnailHeight = 0;

            _TimelineRect = new Rectangle(0, 0, Width, rulerHeight);
            _ThumbnailRect = new Rectangle(0, rulerHeight + dividerHeight, Width, thumbnailHeight);

            ComputeTicks();
            Invalidate();
        }

        private void ComputeTicks()
        {
            _Ticks.Clear();

            double start = CyclesToSeconds(_DisplayFrom);
            double end = CyclesToSeconds(_DisplayTo);
            double range = end - start;
            if (range <= 0) 
                return;

            using Graphics g = Graphics.FromHwnd(IntPtr.Zero);
            double maxTickCount = Width / g.DpiX;
            double rawStep = range / maxTickCount;
            double magnitude = Math.Pow(10, Math.Floor(Math.Log10(rawStep)));

            Divisions[] options = [
                new Divisions { Major = 1, Medium = 2, Minor = 5 },
                new Divisions { Major = 2, Medium = 4, Minor = 2 },
                new Divisions { Major = 5, Medium = 5, Minor = 2 },
                new Divisions { Major = 10, Medium = 2, Minor = 5 } ];

            Divisions divisions = Array.Find(options, d => magnitude * d.Major >= rawStep);
            double minorStep = magnitude * divisions.Major / divisions.Medium / divisions.Minor;

            int firstIndex = (int)Math.Ceiling(start / minorStep);
            int lastIndex = (int)Math.Floor(end / minorStep);

            for (int i = firstIndex; i <= lastIndex; i++)
            {
                TickSize size;
                if (i % (divisions.Medium * divisions.Minor) == 0)
                    size = TickSize.Major;
                else if (i % divisions.Minor == 0)
                    size = TickSize.Medium;
                else
                    size = TickSize.Minor;
                _Ticks.Add(new Tick { Position = minorStep * i, Size = size });
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
                control = control!.Parent!;
            return (Form)control;
        }

        private void OnRangeChange()
        {
            using var token = _ReentrancyGuard.TryEnter();
            if (token == null) return;

            var form = (BeebPerfForm)GetParentForm();
            form.SetAnalysisRange(_AnalysisFrom, _AnalysisTo);

            Invalidate(_ThumbnailRect);
        }

        private int MulDiv(int a, int b, int c)
        {
            return (int)double.Round((double)a * b / c);
        }

        private enum DragMode
        {
            None = 0,
            LeftHandle = 1,
            RightHandle = 2,
            Range = 3
        }

        private enum TickSize { Major, Medium, Minor }

        private struct Tick
        {
            public double Position;
            public TickSize Size;
        }

        private struct Divisions
        {
            public int Major;
            public int Medium;
            public int Minor;
        }

        private int _RecordingDuration;
        private int _DisplayFrom;
        private int _DisplayTo;
        private int _AnalysisFrom;
        private int _AnalysisTo;
        private Rectangle _TimelineRect;
        private Rectangle _ThumbnailRect;
        private Rectangle _LeftHandleRect;
        private Rectangle _RightHandleRect;
        private DragMode _DragMode;
        private int _DragOffset;
        private int _DragOrigin;
        private List<Tick> _Ticks = [];
        private HScrollBar _ScrollBar;
        private ReentrancyGuard _ReentrancyGuard = new();
        private List<FrameBitmap> _FrameBitmaps = [];
    }
}
