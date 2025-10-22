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

using BeebPerf.model;

namespace BeebPerf.ux
{
    internal class FlameGraphView : Panel
    {
        public FlameGraphView() : base() 
        {
            DoubleBuffered = true;
            VisibleChanged += OnVisibleChanged;

            _VScrollBar = new VScrollBar();
            _VScrollBar.Dock = DockStyle.Right;
            _VScrollBar.Scroll += ScrollBar_VScroll;
            _VScrollBar.Enabled = false;
            Controls.Add(_VScrollBar);

            _HScrollBar = new HScrollBar();
            _HScrollBar.Dock = DockStyle.Bottom;
            _HScrollBar.Scroll += ScrollBar_HScroll;
            _HScrollBar.Enabled = false;
            Controls.Add(_HScrollBar);

            _ToolTipTimer = new();
            _ToolTipTimer.Interval = 500;
            _ToolTipTimer.Tick += ToolTipTimer_Tick;
        }

        public void AddCallTree(CallTreeNode treeNode)
        {
            _CallTrees.Add(treeNode);
            _ResetView = true;
            Invalidate();
        }

        public void Clear()
        {
            _CallTrees.Clear();
            _ResetView = true;
            Invalidate();
        }

        public void SelectRoutine(Routine routine, CallStack callStack)
        {
            foreach (var routineCell in _RoutineCells)
            {
                if (routineCell.Routine != routine ||
                    routineCell.CallStack != callStack)
                    continue;

                _SelectedRoutine = routineCell.Routine;
                _SelectedCallStack = routineCell.CallStack;

                LayoutRoutineCells();
                ScrollSelectedRoutineIntoView();
                Invalidate();

                return;
            }

            _SelectedRoutine = null;
            _SelectedCallStack = null;

            LayoutRoutineCells();
            SetVScrollValue(0);
            Invalidate();
        }

        public void ClearSelectedRoutine()
        {
            _ResetView = true;
            Invalidate();
        }

        public void FlipView()
        {
            _FlipView = !_FlipView;
            SetVScrollValue(0);
            Invalidate();
        }

        protected override void OnResize(EventArgs eventArgs)
        {
            int vScrollPos = GetVScrollValue();
            double hScrollPos = GetHScrollUnitValue();

            base.OnResize(eventArgs);

            if (Width != _PrevWidth)
                LayoutRoutineCells();
            else
                UpdateScrollBars();

            SetVScrollValue(vScrollPos);
            SetHScrollUnitValue(hScrollPos);

            _PrevWidth = Width;
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            BeebPerfForm form = (BeebPerfForm)GetParentForm();

            foreach (var routineCell in _RoutineCells)
            {
                if (!routineCell.Rectangle.Contains(PixelsToLayout(e.Location)))
                    continue;

                if (_SelectedRoutine != routineCell.Routine ||
                    _SelectedCallStack != routineCell.CallStack)
                {
                    _SelectedRoutine = routineCell.Routine;
                    _SelectedCallStack = routineCell.CallStack;

                    LayoutRoutineCells();
                    ScrollSelectedRoutineIntoView();
                    Invalidate();

                    form.SetSelectedRoutine(routineCell.Routine, routineCell.CallStack);
                }
                return;
            }

            if (_SelectedRoutine != null)
            {
                _SelectedRoutine = null;
                _SelectedCallStack = null;

                LayoutRoutineCells();
                SetVScrollValue(0);
                Invalidate();

                form.ClearSelectedRoutine();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            foreach (var routineCell in _RoutineCells)
            {
                if (!routineCell.Rectangle.Contains(PixelsToLayout(e.Location)))
                    continue;

                ShowToolTip(routineCell, e.Location);
                if (routineCell != _FocusRoutineCell)
                    SetFocus(routineCell);
                return;
            }

            HideToolTip();
            RemoveFocus();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            HideToolTip();
            RemoveFocus();
        }

        private void OnVisibleChanged(object? sender, EventArgs e)
        {
            if (!Visible)
                HideToolTip();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (_ResetView)
            {
                _SelectedRoutine = null;
                _SelectedCallStack = null;
                LayoutRoutineCells();
                SetHScrollValue(0);
                SetVScrollValue(0);
                _ResetView = false;
            }

            bool lightMode = (ForeColor.GetBrightness() < 0.5);
            Color lineColor = lightMode ? Color.DarkRed : _ColorLightRed;
            Color fillColor = lightMode ? _ColorLightRed : Color.DarkRed;

            using var linePen = new Pen(lineColor);
            using var selectedLinePen = new Pen(lineColor, 2);
            using var fillBrush = new SolidBrush(fillColor);
            using var focusFillBrush = new SolidBrush(Blend(fillColor, ForeColor, 0.1));
            using var textBrush = new SolidBrush(ForeColor);

            foreach (var routineCell in _RoutineCells)
                PaintRoutineCell(routineCell, e, linePen, selectedLinePen, fillBrush, focusFillBrush, textBrush);
        }

        private void PaintRoutineCell(
            RoutineCell routineCell,
            PaintEventArgs e,
            Pen linePen,
            Pen selectedLinePen,
            Brush fillBrush, 
            Brush focusFillBrush,
            Brush textBrush)
        {
            Graphics graphics = e.Graphics;

            var clientRect = LayoutToPixels(routineCell.Rectangle);

            // paint background
            if (clientRect.Width > 1)
                graphics.FillRectangle((routineCell == _FocusRoutineCell) ? focusFillBrush : fillBrush, clientRect);

            // paint border
            Pen borderPen = linePen;
            var borderRect = clientRect;
            if (borderRect.Width > 0)
            {
                if (routineCell.Routine == _SelectedRoutine &&
                    routineCell.CallStack == _SelectedCallStack)
                {
                    // inset two-pixel border
                    borderPen = selectedLinePen;
                    borderRect.X += 1;
                    borderRect.Y += 1;
                    borderRect.Width -= 1;
                    borderRect.Height -= 1;
                }
                graphics.DrawRectangle(borderPen, borderRect);
            }
            else
                graphics.DrawLine(borderPen, borderRect.X, borderRect.Y, borderRect.X, borderRect.Y + borderRect.Height);

            // paint text
            if (clientRect.Width > FontHeight / 2)
            {
                // clip left and right ends
                var clientExtents = GetClientExtents();

                bool clipLeft = (clientRect.X < 0);
                bool clipRight = (clientRect.Right >= clientExtents.Width);

                if (clipLeft)
                {
                    clientRect.Width = clientRect.Right;
                    clientRect.X = 0;
                }

                if (clipRight)
                {
                    clientRect.Width = clientExtents.Width - clientRect.X;
                }

                int charWidth = Font.Height / 2;

                StringFormat textFormat = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    FormatFlags = StringFormatFlags.NoWrap,
                };

                // paint '<'?
                if (clipLeft && clientRect.Width > charWidth)
                {
                    textFormat.Alignment = StringAlignment.Near;
                    graphics.DrawString("<", Font, textBrush, clientRect, textFormat);
                    clientRect.X += charWidth;
                    clientRect.Width -= charWidth;
                }

                // paint '>'?
                if (clipRight && clientRect.Width > charWidth)
                {
                    textFormat.Alignment = StringAlignment.Far;
                    graphics.DrawString(">", Font, textBrush, clientRect, textFormat);
                    clientRect.Width -= charWidth;
                }

                // paint routine address/label?
                if (clientRect.Width > charWidth)
                {
                    textFormat.Trimming = StringTrimming.EllipsisCharacter;
                    textFormat.Alignment = StringAlignment.Center;
                    graphics.DrawString(FormatRoutine(routineCell), Font, textBrush, clientRect, textFormat);
                }
            }
        }

        private Rectangle LayoutToPixels(Rectangle rect)
        {
            int x = rect.X - GetHScrollValue();

            int y = rect.Y;
            if (!_FlipView)
                y = GetClientExtents().Height - y - rect.Height;

            if (_FlipView)
                y -= GetVScrollValue();
            else
                y += GetVScrollValue();

            return new Rectangle(x, y, rect.Width, rect.Height);
        }

        private Point PixelsToLayout(Point point)
        {
            int x = point.X + GetHScrollValue();

            int y = point.Y;
            if (_FlipView)
                y += GetVScrollValue();
            else
                y -= GetVScrollValue();

            if (!_FlipView)
                y = GetClientExtents().Height - y;

            return new Point(x, y);
        }

        private string FormatMetrics(RoutineCell routineCell)
        {
            var percentage = double.Min(100.0 * routineCell.CycleCount / _TotalCycleCount, 100);
            return $"{routineCell.CycleCount:N0} ({percentage:F2}%)";
        }

        private string FormatRoutine(RoutineCell routineCell)
        {
            var routine = routineCell.Routine;
            if (routine.Label.Length > 0)
                return $"{routine.Label} ({routine.StartAddress})";
            else
                return routine.StartAddress.ToString();
        }

        private void LayoutRoutineCells()
        {
            _RoutineCells.Clear();
            _FocusRoutineCell = null;

            // calc total cycle count
            int totalCycleCount = 0;
            foreach (var treeNode in _CallTrees)
                totalCycleCount += treeNode.CPUMetrics.InclusiveCycleCount;
            _TotalCycleCount = totalCycleCount;

            if (totalCycleCount > 0)
            {
                // calc displayed cycle count
                int displayedCycleCount = totalCycleCount;
                if (_SelectedRoutine != null)
                {
                    if (_SelectedCallStack != null)
                        displayedCycleCount = _SelectedRoutine.MetricsByStack[_SelectedCallStack].InclusiveCycleCount;
                    else
                        displayedCycleCount = _SelectedRoutine.AggregateMetrics.InclusiveCycleCount;
                }

                // calc scaler
                int marginSize = Font.Height / 2;
                double scaler = (double)(GetClientExtents().Width - 2 * marginSize) / displayedCycleCount;

                // calc layout extents
                int cellHeight = Font.Height;
                int maxTreeDepth = MaxDepth(_CallTrees, 1);

                _LayoutExtents.Width = (int)double.Round(scaler * totalCycleCount) + (2 * marginSize);
                _LayoutExtents.Height = (maxTreeDepth * cellHeight) + (2 * marginSize);

                // layout siblings, and their descendants
                LayoutSiblings(_CallTrees, left: marginSize, top: marginSize, scaler, cellHeight, new HashSet<Rectangle>());
            }
            else
            {
                _LayoutExtents.Width = 0;
                _LayoutExtents.Height = 0;
            }

            UpdateScrollBars();
        }

        private void LayoutSiblings(IReadOnlyList<CallTreeNode> siblings, int left, int top, double scaler, int cellHeight, HashSet<Rectangle> rectSet)
        {
            // sort siblings by their inclusive cycle count
            var sortedSiblings = siblings.ToList();
            sortedSiblings.Sort((a, b) => b.CPUMetrics.InclusiveCycleCount.CompareTo(a.CPUMetrics.InclusiveCycleCount));

            // layout siblings
            int cycleCount = 0;
            foreach (var sibling in sortedSiblings)
            {
                int siblingLeft = left + (int)double.Round(scaler * cycleCount);
                cycleCount += sibling.CPUMetrics.InclusiveCycleCount;
                int siblingRight = left + (int)double.Round(scaler * cycleCount);
                var rect = new Rectangle(siblingLeft, top, siblingRight - siblingLeft, cellHeight);

                if (!rectSet.Contains(rect)) // duplicate rect?
                {
                    rectSet.Add(rect);

                    _RoutineCells.Add(new RoutineCell
                    {
                        Routine = sibling.Routine,
                        CallStack = sibling.CallStack,
                        Rectangle = rect,
                        CycleCount = sibling.CPUMetrics.InclusiveCycleCount
                    });
                }

                // recursively layout children
                if (sibling.HasChildren)
                {
                    LayoutSiblings(sibling.Children, siblingLeft, top + cellHeight, scaler, cellHeight, rectSet);
                }
            }
        }

        private void ScrollSelectedRoutineIntoView()
        {
            if (_SelectedRoutine == null)
                return;

            foreach (var routineCell in _RoutineCells)
            {
                if (routineCell.Routine != _SelectedRoutine ||
                    routineCell.CallStack != _SelectedCallStack)
                    continue;

                int marginSize = Font.Height / 2;
                SetHScrollValue(routineCell.Rectangle.X - marginSize);
                break;
            }
        }

        private void UpdateScrollBars()
        {
            Size clientExtents = GetClientExtents();
            _HScrollBar.Enabled = (_LayoutExtents.Width > clientExtents.Width);
            if (_HScrollBar.Enabled)
            {
                _HScrollBar.Minimum = 0;
                _HScrollBar.Maximum = _LayoutExtents.Width;
                _HScrollBar.LargeChange = Math.Max(clientExtents.Width, 1);
                _HScrollBar.SmallChange = DeviceDpi / 2;

                int maxValue = _HScrollBar.Maximum - _HScrollBar.LargeChange;
                _HScrollBar.Value = Math.Clamp(_HScrollBar.Value, 0, maxValue);
            }

            _VScrollBar.Enabled = (_LayoutExtents.Height > clientExtents.Height);
            if (_VScrollBar.Enabled)
            {
                _VScrollBar.Minimum = 0;
                _VScrollBar.Maximum = _LayoutExtents.Height;
                _VScrollBar.LargeChange = Math.Max(clientExtents.Height, 1);
                _VScrollBar.SmallChange = DeviceDpi / 2;

                int maxValue = _VScrollBar.Maximum - _VScrollBar.LargeChange;
                _VScrollBar.Value = Math.Clamp(_VScrollBar.Value, 0, maxValue);
            }
        }

        private int GetVScrollValue()
        {
            if (!_VScrollBar.Enabled)
                return 0;

            int maxValue = _VScrollBar.Maximum - _VScrollBar.LargeChange;
            return Math.Clamp(_FlipView ? _VScrollBar.Value : maxValue - _VScrollBar.Value, 0, maxValue);
        }

        private void SetVScrollValue(int value)
        {
            if (!_VScrollBar.Enabled)
                return;

            int maxValue = _VScrollBar.Maximum - _VScrollBar.LargeChange;
            value = Math.Clamp(_FlipView ? value : maxValue - value, 0, maxValue);

            _VScrollBar.Value = value;
        }

        private int GetHScrollValue()
        {
            if (!_HScrollBar.Enabled)
                return 0;

            int maxValue = _HScrollBar.Maximum - _HScrollBar.LargeChange;
            return Math.Clamp(_HScrollBar.Value, 0, maxValue);
        }

        private void SetHScrollValue(int value)
        {
            if (!_HScrollBar.Enabled)
                return;

            int maxValue = _HScrollBar.Maximum - _HScrollBar.LargeChange;
            _HScrollBar.Value = Math.Clamp(value, 0, maxValue);
        }

        private double GetHScrollUnitValue()
        {
            if (!_HScrollBar.Enabled)
                return 0;

            int maxValue = _HScrollBar.Maximum - _HScrollBar.LargeChange;
            double halfLargeChange = 0.5 * _HScrollBar.LargeChange;
            return double.Clamp(((double)_HScrollBar.Value + halfLargeChange) / maxValue, 0, 1);
        }

        private void SetHScrollUnitValue(double value)
        {
            if (!_HScrollBar.Enabled)
                return;

            int maxValue = _HScrollBar.Maximum - _HScrollBar.LargeChange;
            double halfLargeChange = 0.5 * _HScrollBar.LargeChange;
            _HScrollBar.Value = int.Clamp((int)double.Round((value * maxValue - halfLargeChange)), 0, maxValue);
        }

        private void ScrollBar_VScroll(object? sender, ScrollEventArgs e)
        {
            Invalidate();
        }

        private void ScrollBar_HScroll(object? sender, ScrollEventArgs e)
        {
            Invalidate();
        }

        private int MaxDepth(IReadOnlyList<CallTreeNode> siblings, int depth)
        {
            int maxDepth = depth;
            foreach (var sibling in siblings)
                if (sibling.HasChildren)
                    maxDepth = Math.Max(MaxDepth(sibling.Children, depth + 1), maxDepth);
            return maxDepth;
        }

        private void ShowToolTip(RoutineCell routineCell, Point mousePosition)
        {
            _ToolTipTimer.Stop();
            _ToolTipTimer.Start();

            _ToolTipText = $"Routine: {FormatRoutine(routineCell)}\nTotal CPU: {FormatMetrics(routineCell)}";
            _ToolTipLocation = mousePosition;
            _ToolTipLocation.Offset(10, 10);
        }

        private void HideToolTip()
        {
            _ToolTip.Hide(this);
        }

        private void ToolTipTimer_Tick(object? sender, EventArgs e)
        {
            _ToolTipTimer.Stop();
            _ToolTip.Show(_ToolTipText, this, _ToolTipLocation);
        }

        private void SetFocus(RoutineCell routineCell)
        {
            if (_FocusRoutineCell != null)
                Invalidate(LayoutToPixels(_FocusRoutineCell.Rectangle));

            Invalidate(LayoutToPixels(routineCell.Rectangle));
            _FocusRoutineCell = routineCell;
            Update();
        }

        private void RemoveFocus()
        {
            if (_FocusRoutineCell != null)
            {
                Invalidate(LayoutToPixels(_FocusRoutineCell.Rectangle));
                _FocusRoutineCell = null;
                Update();
            }
        }

        private Color Blend(Color first, Color second, double ratio)
        {
            int r = (int)(first.R * (1 - ratio) + second.R * ratio);
            int g = (int)(first.G * (1 - ratio) + second.G * ratio);
            int b = (int)(first.B * (1 - ratio) + second.B * ratio);
            return Color.FromArgb(r, g, b);
        }

        private Size GetClientExtents()
        {
            return new Size(
                Math.Max(Width - _VScrollBar.Width, 0),
                Math.Max(Height - _HScrollBar.Height, 0));
        }

        private Form GetParentForm()
        {
            Control control = this;
            while (control is not Form)
                control = control!.Parent!;
            return (Form)control;
        }

        private class RoutineCell
        {
            public required Routine Routine;
            public required CallStack CallStack;
            public required Rectangle Rectangle;
            public required int CycleCount;
        }

        private Routine? _SelectedRoutine = null;
        private CallStack? _SelectedCallStack = null;
        private bool _FlipView;
        private RoutineCell? _FocusRoutineCell;
        private List<RoutineCell> _RoutineCells = new();
        private List<CallTreeNode> _CallTrees = new();
        private int _TotalCycleCount;
        private Color _ColorLightRed = Color.FromArgb(0xFF, 0x80, 0x80);
        private bool _ResetView;

        private ToolTip _ToolTip = new ToolTip();
        private string _ToolTipText = string.Empty;
        private Point _ToolTipLocation;
        private System.Windows.Forms.Timer _ToolTipTimer = new();

        private Size _LayoutExtents = new();
        private int _PrevWidth = 0;
        private VScrollBar _VScrollBar;
        private HScrollBar _HScrollBar;
    }
}
