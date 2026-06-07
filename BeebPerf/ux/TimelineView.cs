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

using static BeebPerf.FrameAnalysis;

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

            _EditSelectionButton = new () {
                Name = "editSelectionButton",
                ImageResourceName = "editSelectionButton.Image",
                ToolTipText = "Edit selection",
                Parent = this
            };
            _EditSelectionButton.Click += editSelectionButton_Click;

            _CopyFrameButton = new()
            {
                Name = "copyFrameButton",
                ImageResourceName = "copyButton.Image",
                ToolTipText = "Copy bitmap",
                Parent = this
            };
            _CopyFrameButton.Click += copyFrameButton_Click;

            _ShowCopyFrameButtonTimer = new();
            _ShowCopyFrameButtonTimer.Interval = 500;
            _ShowCopyFrameButtonTimer.Tick += ShowCopyFrameButtonTimer_Tick;
        }

        private void editSelectionButton_Click(object? sender, EventArgs e)
        {
            var form = FindForm() as BeebPerfForm;
            if (form is null) return;

            var dialog = new EditSelectionDialog(_AnalysisFrom, _AnalysisTo, form);
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                int analysisFrom = Math.Clamp(dialog.AnalysisFrom, 0, _RecordingDuration);
                int analysisTo = Math.Clamp(dialog.AnalysisTo, 0, _RecordingDuration);
                form.SetAnalysisRange(analysisFrom, analysisTo);
            }
        }

        private void copyFrameButton_Click(object? sender, EventArgs e)
        {
            var form = FindForm() as BeebPerfForm;
            if (form != null && _FocusFrame != null)
            {
                var bitmap = _FocusFrame.Value.Bitmap;
                Exporter.CopyToClipboard(form, bitmap.Bitmap!, bitmap.AspectRatio);
            }
        }

        public List<DisplayFrame> FrameBitmaps
        {
            get => _FrameBitmaps;
            set
            {
                _FrameBitmaps = value;
                ComputeFrames();
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

        public bool CanSelectAll()
        {
            if (_RecordingDuration == 0)
                return false;

            return (_AnalysisFrom != 0.0 || _AnalysisTo != _RecordingDuration);
        }

        public void SelectAll()
        {
            using var token = _ReentrancyGuard.TryEnter();
            if (token == null) return;

            SelectAllInternal();
        }

        public bool CanZoomIn()
        {
            if (_RecordingDuration == 0)
                return false;

            int newDisplayWidth = (_DisplayTo - _DisplayFrom) / 2;
            int minDisplayRange = 4 * Width;
            return (newDisplayWidth > minDisplayRange);
        }

        public void ZoomIn()
        {
            using var token = _ReentrancyGuard.TryEnter();
            if (token == null) return;

            ZoomInternal(0.5, Width / 2);
        }

        public bool CanZoomOut()
        {
            if (_RecordingDuration == 0)
                return false;

            return _ScrollBar.Enabled;
        }

        public void ZoomOut()
        {
            using var token = _ReentrancyGuard.TryEnter();
            if (token == null) return;

            ZoomInternal(2.0, Width / 2);
        }

        public bool CanFitSelection()
        {
            return (_RecordingDuration > 0);
        }

        public void FitSelection()
        {
            using var token = _ReentrancyGuard.TryEnter();
            if (token == null) return;

            FitSelectionInternal();
        }

        public bool CanFitFrames()
        {
            return (_RecordingDuration > 0);
        }
            
        public void FitFrames()
        {
            using var token = _ReentrancyGuard.TryEnter();
            if (token == null) return;

            FitFramesInternal();
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
                FitSelectionInternal();
            }

            if (_EditSelectionButton != null)
            {
                SuspendLayout();
                int padding = (_TimelineRect.Height - _EditSelectionButton.Height) / 2;
                _EditSelectionButton.Location = new Point(Width - _EditSelectionButton.Width - padding, padding);
                ResumeLayout(performLayout: false);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (_RecordingDuration == 0 || e.Button != MouseButtons.Left)
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

            if (_RecordingDuration == 0)
                return;

            switch (_DragMode)
            {
                case DragMode.None:
                    UpdateCopyFrameButton(e.X, e.Y);
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

        private void UpdateCopyFrameButton(int mouseX, int mouseY)
        {
            bool found = false;

            foreach (var frame in _Frames)
            {
                if (frame.Rect.Contains(mouseX, mouseY))
                {
                    found = true;
                    ShowCopyFrameButton(frame);
                    break;
                }
            }

            if (!found)
                HideCopyFrameButton();
        }

        private void ShowCopyFrameButton(Frame frame)
        {
            if (!frame.Equals(_FocusFrame))
            {
                _FocusFrame = frame;
                _ShowCopyFrameButtonTimer.Stop();
                _ShowCopyFrameButtonTimer.Start();
            }
        }

        private void HideCopyFrameButton()
        {
            _FocusFrame = null;
            _ShowCopyFrameButtonTimer.Stop();
            if (_CopyFrameButton != null)
            {
                SuspendLayout();
                _CopyFrameButton.Visible = false;
                ResumeLayout(performLayout: false);
            }
        }

        private void ShowCopyFrameButtonTimer_Tick(object? sender, EventArgs e)
        {
            if (_FocusFrame == null)
                return;

            SuspendLayout();
            var frame = (Frame)_FocusFrame;
            _CopyFrameButton.Location = new()
            {
                X = frame.Rect.Right - _CopyFrameButton.Width,
                Y = frame.Rect.Top,
            };
            _CopyFrameButton.Visible = true;
            ResumeLayout(performLayout: false);
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

            // draw ruler line
            using var pen = new Pen(Blend(ForeColor, BackColor, 0.5));
            graphics.DrawLine(pen, 0, _TimelineRect.Bottom - 1, Width, _TimelineRect.Bottom - 1);

            // draw handles
            using var handleBrush = new SolidBrush(ForeColor);
            graphics.FillRectangle(handleBrush, _LeftHandleRect);
            graphics.FillRectangle(handleBrush, _RightHandleRect);

            // draw ruler ticks and text
            var state = graphics.Save();
            graphics.ExcludeClip(centerRect);
            using var brush = new SolidBrush(ForeColor);
            DrawRulerForeground(graphics, pen, brush);
            graphics.Restore(state);

            state = graphics.Save();
            graphics.IntersectClip(centerRect);
            using var highlightPen = new Pen(Blend(SystemColors.Highlight, BackColor, 0.5));
            using var highlightBrush = new SolidBrush(BackColor);
            DrawRulerForeground(graphics, highlightPen, highlightBrush);
            graphics.Restore(state);

            // draw frames
            DrawFrames(graphics);
        }

        private void DrawFrames(Graphics graphics)
        {
            if (_FramesRect.Height == 0)
                return;

            StringFormat textFormat = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
            };

            using var blackBrush = new SolidBrush(Color.Black);
            using var greyPen = new Pen(Color.Gray);
            using var textBrush = new SolidBrush(ForeColor);

            using var arrowThinPen = new Pen(ForeColor, 1);
            using var arrowThickPen = new Pen(ForeColor, 3);
            using var arrowBrush = new SolidBrush(ForeColor);

            foreach (var frame in _Frames)
            {
                // draw display span arrow
                if (frame.Start < frame.Rect.Left || frame.End > frame.Rect.Right)
                {
                    // measure
                    int arrowHeadLength = Font.Height / 2;
                    int arrowHeadHalfHeight = Font.Height / 3;

                    int arrowCenterY = frame.Rect.Top + frame.Rect.Height / 2;
                    int arrowTop = arrowCenterY - arrowHeadHalfHeight;
                    int arrowBottom = arrowCenterY + arrowHeadHalfHeight;

                    int arrowLeft = frame.Start;
                    int arrowRight = frame.End;

                    // paint arrow line
                    graphics.DrawLine(arrowThickPen,
                        arrowLeft + arrowHeadLength, arrowCenterY,
                        arrowRight - arrowHeadLength, arrowCenterY);

                    // paint left arrow head
                    Point[] leftArrowHeadPoints = [
                        new() { X = arrowLeft, Y = arrowCenterY },
                        new() { X = arrowLeft + arrowHeadLength, Y = arrowTop },
                        new() { X = arrowLeft + arrowHeadLength, Y = arrowBottom },
                    ];

                    graphics.FillPolygon(arrowBrush, leftArrowHeadPoints);
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    graphics.DrawPolygon(arrowThinPen, leftArrowHeadPoints);
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;

                    // paint right arrow head
                    Point[] rightArrowHeadPoints = [
                        new() { X = arrowRight, Y = arrowCenterY },
                        new() { X = arrowRight - arrowHeadLength, Y = arrowTop },
                        new() { X = arrowRight - arrowHeadLength, Y = arrowBottom },
                    ];

                    graphics.FillPolygon(arrowBrush, rightArrowHeadPoints);
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    graphics.DrawPolygon(arrowThinPen, rightArrowHeadPoints);
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
                }

                // paint frame background and border
                var frameBitmap = frame.Bitmap;
                var frameRect = frame.Rect;

                int frameWidth = frameRect.Width;
                var frameHeight = frameRect.Height;

                graphics.FillRectangle(blackBrush, frameRect);
                graphics.DrawRectangle(greyPen, frameRect);

                // paint thumbnail image
                double aspectRatio = frameBitmap.AspectRatio * frameBitmap.Bitmap!.Width / frameBitmap.Bitmap!.Height;
                int bitmapWidth = Math.Min((int)double.Round(aspectRatio * frameHeight), frameWidth);
                int indent = (frameWidth - bitmapWidth) / 2;
                var imageRect = new Rectangle(
                    frameRect.Left + indent + 2,
                    _FramesRect.Top + 2,
                    bitmapWidth - 3,
                    frameHeight - 3);

                graphics.DrawImage(frameBitmap.Bitmap, imageRect);

                // paint frame number
                var textRect = new Rectangle(
                    frameRect.Left,
                    _FramesRect.Top + frameHeight,
                    frameWidth,
                    Font.Height);

                graphics.DrawString($"{frameBitmap.FrameNumber}", Font, textBrush, textRect, textFormat);

                // exclude thumbnail from clipping area
                frameRect = new Rectangle(
                    frameRect.Left,
                    frameRect.Top,
                    frameRect.Width + 1, 
                    frameHeight + Font.Height + 1);

                graphics.ExcludeClip(frameRect);
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
                    _ => throw new ArgumentOutOfRangeException()
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
            if (_RecordingDuration == 0)
            {
                Cursor = Cursors.Default;
                return;
            }

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
                    Cursor = Cursors.IBeam;
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
                _ => throw new ArgumentOutOfRangeException()
            };

            int cyclesFrom = _DragMode switch
            {
                DragMode.None => _AnalysisFrom,
                DragMode.LeftHandle => PixelsToCycles(_LeftHandleRect.Right - 1),
                DragMode.RightHandle => _AnalysisFrom,
                DragMode.Range => Math.Min(PixelsToCycles(_LeftHandleRect.Right - 1), PixelsToCycles(_RightHandleRect.Left)),
                _ => throw new ArgumentOutOfRangeException()
            };

            int cyclesTo = _DragMode switch
            {
                DragMode.None => _AnalysisTo,
                DragMode.LeftHandle => _AnalysisTo,
                DragMode.RightHandle => PixelsToCycles(_RightHandleRect.Left),
                DragMode.Range => Math.Max(PixelsToCycles(_LeftHandleRect.Right - 1), PixelsToCycles(_RightHandleRect.Left)),
                _ => throw new ArgumentOutOfRangeException()
            };

            string durationText = $"Duration: {FormatCycles(_RecordingDuration)}";
            if (cycles != _RecordingDuration)
                durationText += $" ({FormatCycles(cycles)} selected from {FormatCycles(cyclesFrom)} to {FormatCycles(cyclesTo)})";

            var form = FindForm() as BeebPerfForm;
            if (form is null) return;

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
            FitSelectionInternal();
            OnRangeChange();
        }

        private void SetDurationInternal(int recordingDuration)
        {
            _RecordingDuration = recordingDuration;
            _AnalysisFrom = 0;
            _AnalysisTo = recordingDuration;
            _EditSelectionButton.Visible = recordingDuration > 0;
            FitSelectionInternal();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            bool controlKeyModifier = (Control.ModifierKeys & Keys.Control) != 0;
            if (controlKeyModifier)
            {
                // zoom timeline
                double zoomFactor = 1.0 - (0.2 * (double)e.Delta / 120.0/*WHEEL_DELTA*/);
                ZoomInternal(zoomFactor, e.X);
            }
            else if (_ScrollBar.Enabled)
            {
                // scroll timeline
                int oldValue = _ScrollBar.Value;
                int newValue = _ScrollBar.Value - (e.Delta * _ScrollBar.SmallChange / 120/*WHEEL_DELTA*/);
                _ScrollBar.Value = Math.Clamp(newValue, _ScrollBar.Minimum, _ScrollBar.Maximum - _ScrollBar.LargeChange);
                ScrollTimelime(_ScrollBar.Value, oldValue);
            }
        }

        private void ScrollBar_Scroll(object? sender, ScrollEventArgs e)
        {
            ScrollTimelime(e.NewValue, e.OldValue);
        }

        private void ScrollTimelime(int newValue, int oldValue)
        {
            int deltaCycles = PixelsToCycles(newValue) - PixelsToCycles(oldValue);
            _DisplayFrom += deltaCycles;
            _DisplayTo += deltaCycles;

            SetLeftHandleRect(CyclesToPixels(_AnalysisFrom));
            SetRightHandleRect(CyclesToPixels(_AnalysisTo));

            UpdateDurationText();
            UpdateTimeline();
        }

        private void SelectAllInternal()
        {
            _AnalysisFrom = 0;
            _AnalysisTo = _RecordingDuration;
            FitSelection();
            FitSelectionInternal();
        }

        private void ZoomInternal(double zoomFactor, int origin)
        {
            bool leftStop = false;
            bool rightStop = false;

            int margin = Font.Height * 4;

            int displayWidth = _DisplayTo - _DisplayFrom;
            int newDisplayWidth = (int)double.Round(zoomFactor * displayWidth);

            // limit zoom depth
            int minDisplayRange = 4 * Width;
            if (newDisplayWidth < minDisplayRange && newDisplayWidth < displayWidth)
                newDisplayWidth = minDisplayRange;

            // ensure scale origin is not in margins
            int start = CyclesToPixels(0);
            int end = CyclesToPixels(_RecordingDuration);
            if (origin < start)
                origin = start;
            else if (origin > end)
                origin = end;

            // calc deltas
            int deltaDisplayFrom = MulDiv(origin, displayWidth - newDisplayWidth, Width);
            int deltaDisplayTo = MulDiv(Width - origin, displayWidth - newDisplayWidth, Width);

            // constrain margins, so zoom-out doesn't enlarge them
            int range = Width - margin * 2;
            int extends = MulDiv(_RecordingDuration, margin, range);

            int extendsScaled = MulDiv(extends, newDisplayWidth, _RecordingDuration + extends * 2);
            if (extends > extendsScaled)
                extends = extendsScaled;

            int minDisplayFrom = -extends;
            int maxDisplayTo = _RecordingDuration + extends;

            leftStop = _DisplayFrom + deltaDisplayFrom < minDisplayFrom;
            rightStop = _DisplayTo - deltaDisplayTo > maxDisplayTo;

            if (leftStop && rightStop)
            {
                _DisplayFrom = minDisplayFrom;
                _DisplayTo = maxDisplayTo;
            }
            else if (leftStop)
            {
                _DisplayFrom = minDisplayFrom;
                _DisplayTo = _DisplayFrom + newDisplayWidth;
            }
            else if (rightStop)
            {
                _DisplayTo = maxDisplayTo;
                _DisplayFrom = _DisplayTo - newDisplayWidth;
            }
            else
            {
                _DisplayFrom += deltaDisplayFrom;
                _DisplayTo -= deltaDisplayTo;
            }

            newDisplayWidth = _DisplayTo - _DisplayFrom;

            SetLeftHandleRect(CyclesToPixels(_AnalysisFrom));
            SetRightHandleRect(CyclesToPixels(_AnalysisTo));

            UpdateDurationText();
            UpdateTimeline();

            _ScrollBar.Minimum = -margin;
            _ScrollBar.Maximum = MulDiv(Width, _RecordingDuration, newDisplayWidth) + margin;
            _ScrollBar.LargeChange = Width;
            _ScrollBar.SmallChange = DeviceDpi;
            _ScrollBar.Value = Math.Clamp(MulDiv(Width, _DisplayFrom, newDisplayWidth), _ScrollBar.Minimum, _ScrollBar.Maximum);
            _ScrollBar.Enabled = !(leftStop && rightStop);
        }

        private void FitSelectionInternal()
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
            _ScrollBar.SmallChange = DeviceDpi;
            _ScrollBar.Value = Math.Clamp(CyclesToPixels(_DisplayFrom), _ScrollBar.Minimum, _ScrollBar.Maximum);
            _ScrollBar.Enabled = (_AnalysisFrom != 0) || (_AnalysisTo != _RecordingDuration);
        }

        private void FitFramesInternal()
        {
            int frameHeight = _FramesRect.Height - Font.Height;
            int frameWidth = 4 * frameHeight / 3;
            int newDisplayWidth = MulDiv(40000, Width, frameWidth);

            int displayWidth = _DisplayTo - _DisplayFrom;
            int delta = (displayWidth - newDisplayWidth) / 2;
            _DisplayFrom += delta;
            _DisplayTo -= delta;

            SetLeftHandleRect(CyclesToPixels(_AnalysisFrom));
            SetRightHandleRect(CyclesToPixels(_AnalysisTo));

            UpdateDurationText();
            UpdateTimeline();

            int margin = Font.Height * 4;

            _ScrollBar.Minimum = -margin;
            _ScrollBar.Maximum = MulDiv(Width, _RecordingDuration, newDisplayWidth) + margin;
            _ScrollBar.LargeChange = Width;
            _ScrollBar.SmallChange = frameWidth;
            _ScrollBar.Value = Math.Clamp(MulDiv(Width, _DisplayFrom, newDisplayWidth), _ScrollBar.Minimum, _ScrollBar.Maximum);
            _ScrollBar.Enabled = true;
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
            int framesHeight = Height - _ScrollBar.Height - rulerHeight - dividerHeight;

            if (framesHeight < 0)
                framesHeight = 0;

            _TimelineRect = new Rectangle(0, 0, Width, rulerHeight);
            _FramesRect = new Rectangle(0, rulerHeight + dividerHeight, Width, framesHeight);

            ComputeTicks();
            ComputeFrames();
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

        private void ComputeFrames()
        {
            _Frames = [];

            int top = _FramesRect.Top;
            int height = _FramesRect.Height - Font.Height;
            int width = 4 * height / 3;
            
            int leftEdge = CyclesToPixels(0);
            bool overlappingFrames = false;

            int lastFrameIndex = FrameBitmaps.Count - 1;
            for (int frameIndex = 0; frameIndex <= lastFrameIndex; frameIndex++)
            {
                var frameBitmap = FrameBitmaps[frameIndex];

                int left = CyclesToPixels(frameBitmap.StartCycleCount);
                if (left > Width + width)
                    break;

                if (frameIndex < lastFrameIndex && left < leftEdge)
                {
                    overlappingFrames = true;
                    continue;
                }

                if (overlappingFrames)
                {
                    overlappingFrames = false;

                    frameBitmap = FrameBitmaps[--frameIndex];
                    left = CyclesToPixels(frameBitmap.StartCycleCount);
                }

                int right = CyclesToPixels(frameBitmap.EndCycleCount);

                if (left < Width && (left + width > 0 || right > 0))
                {
                    var rect = new Rectangle(left, top, width, height);
                    int span = right - left + 1;
                    if (span > width)
                        rect.Offset((span - width) / 2, 0);

                    _Frames.Add(new Frame() {
                        Bitmap = frameBitmap, 
                        Rect = rect,
                        Start = left,
                        End = right,
                    });
                }

                leftEdge = left + width;
            }

            HideCopyFrameButton();
        }

        private Color Blend(Color first, Color second, double ratio)
        {
            int r = (int)(first.R * (1 - ratio) + second.R * ratio);
            int g = (int)(first.G * (1 - ratio) + second.G * ratio);
            int b = (int)(first.B * (1 - ratio) + second.B * ratio);
            return Color.FromArgb(r, g, b);
        }

        private void OnRangeChange()
        {
            using var token = _ReentrancyGuard.TryEnter();
            if (token == null) return;

            var form = FindForm() as BeebPerfForm;
            if (form is null) return;

            form.SetAnalysisRange(_AnalysisFrom, _AnalysisTo);

            Invalidate(_FramesRect);
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

        private struct Frame
        {
            public DisplayFrame Bitmap;
            public Rectangle Rect;
            public int Start;
            public int End;
        }

        private int _RecordingDuration;
        private int _DisplayFrom;
        private int _DisplayTo;
        private int _AnalysisFrom;
        private int _AnalysisTo;
        private Rectangle _TimelineRect;
        private Rectangle _FramesRect;
        private Rectangle _LeftHandleRect;
        private Rectangle _RightHandleRect;
        private DragMode _DragMode;
        private int _DragOffset;
        private int _DragOrigin;
        private List<Tick> _Ticks = [];
        private List<Frame> _Frames = [];
        private HScrollBar _ScrollBar;
        private ReentrancyGuard _ReentrancyGuard = new();
        private List<DisplayFrame> _FrameBitmaps = [];
        private Frame? _FocusFrame = null;
        private ButtonEx _EditSelectionButton;
        private ButtonEx _CopyFrameButton;
        private System.Windows.Forms.Timer _ShowCopyFrameButtonTimer = new();
    }
}
