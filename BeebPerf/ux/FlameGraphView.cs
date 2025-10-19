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

//
// TODO:
// - Sorting behavior
// - Flip view
//
using BeebPerf.model;
using System.Drawing.Printing;

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
            _ScrollTopLeft = new();
            _LayoutInvalid = true;
            Invalidate();
        }

        public void Clear()
        {
            _ScrollTopLeft = new();
            _SelectedRoutine = null;
            _SelectedCallStack = null;
            _LayoutInvalid = true;
            Invalidate();
        }

        public void FlipView()
        {
            _FlipView = !_FlipView;
            if (_VScrollBar.Enabled)
            {
                if (_FlipView)
                    _ScrollTopLeft.Y = _VScrollBar.Maximum - _VScrollBar.LargeChange;
                else
                    _ScrollTopLeft.Y = _VScrollBar.Minimum;
            }
            else
            {
                _ScrollTopLeft.Y = 0;
            }

            _LayoutInvalid = true;
            Invalidate();
        }

        protected override void OnResize(EventArgs eventArgs)
        {
            base.OnResize(eventArgs);
            LayoutRoutineCells();
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            BeebPerfForm form = (BeebPerfForm)GetParentForm();

            var pos = new Point(e.X + _ScrollTopLeft.X, e.Y + _ScrollTopLeft.Y);
            foreach (var routineCell in _RoutineCells)
            {
                if (!routineCell.Rectangle.Contains(pos))
                    continue;

                if (_SelectedRoutine != routineCell.Routine ||
                    _SelectedCallStack != routineCell.CallStack)
                {
                    _SelectedRoutine = routineCell.Routine;
                    _SelectedCallStack = routineCell.CallStack;

                    _LayoutInvalid = true;
                    Invalidate();

                    form.SetSelectedRoutine(routineCell.Routine, routineCell.CallStack);
                }
                return;
            }

            if (_SelectedRoutine != null)
            {
                _SelectedRoutine = null;
                _SelectedCallStack = null;

                _LayoutInvalid = true;
                Invalidate();

                form.ClearSelectedRoutine();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            var pos = new Point(e.X + _ScrollTopLeft.X, e.Y + _ScrollTopLeft.Y);
            foreach (var routineCell in _RoutineCells)
            {
                if (!routineCell.Rectangle.Contains(pos))
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

            if (_LayoutInvalid)
            {
                LayoutRoutineCells();
                _LayoutInvalid = false;
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
                if (routineCell != _FocusRoutineCell)
                    PaintRoutineCell(routineCell, e, linePen, selectedLinePen, fillBrush, focusFillBrush, textBrush);

            if (_FocusRoutineCell != null)
                PaintRoutineCell(_FocusRoutineCell, e, linePen, selectedLinePen, fillBrush, focusFillBrush, textBrush);
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
            var bounds = routineCell.Rectangle;
            bounds.X -= _ScrollTopLeft.X;
            bounds.Y -= _ScrollTopLeft.Y;

            // determine if the bounds extend left and/or right, clipping them if they do
            bool extendsLeft = (bounds.X < 0);
            bool extendsRight = (bounds.Right > (Width - _VScrollBar.Width));

            if (extendsLeft)
            {
                bounds.Width += bounds.X + 1;
                bounds.X = -1;
            }

            if (extendsRight)
            {
                bounds.Width = (Width - _VScrollBar.Width) - bounds.X;
            }

            // paint background
            if (bounds.Width > 1)
                e.Graphics.FillRectangle((routineCell == _FocusRoutineCell) ? focusFillBrush : fillBrush, bounds);

            // paint border
            if (bounds.Width > 0)
            {
                Pen pen = linePen;
                var rect = bounds;

                if (routineCell.Routine == _SelectedRoutine &&
                    routineCell.CallStack == _SelectedCallStack)
                {
                    // inset two-pixel border
                    pen = selectedLinePen;
                    rect.X += 1;
                    rect.Y += 1;
                    rect.Width -= 1;
                    rect.Height -= 1;
                }

                e.Graphics.DrawRectangle(pen, rect);
            }
            else
                e.Graphics.DrawLine(linePen, bounds.X, bounds.Y, bounds.X, bounds.Y + bounds.Height);

            // paint text
            int padding = Font.Height / 2;
            int textWidth = bounds.Width - 2 * padding;
            var textRect = new Rectangle(bounds.X + padding, bounds.Y, textWidth, bounds.Height);

            if (textWidth > 0)
            {
                StringFormat textFormat = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center,
                    FormatFlags = StringFormatFlags.NoWrap,
                    Trimming = StringTrimming.EllipsisCharacter
                };

                e.Graphics.DrawString(FormatRoutine(routineCell), Font, textBrush, textRect, textFormat);

                if (extendsLeft)
                {
                    textFormat.Trimming = StringTrimming.None;
                    textFormat.Alignment = StringAlignment.Near;
                    e.Graphics.DrawString("<", Font, textBrush, bounds, textFormat);
                }

                if (extendsRight)
                {
                    textFormat.Trimming = StringTrimming.None;
                    textFormat.Alignment = StringAlignment.Far;
                    e.Graphics.DrawString(">", Font, textBrush, bounds, textFormat);
                }
            }
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

            int cellHeight = Font.Height;
            int marginSize = Font.Height / 2;
            int maxTreeDepth = MaxDepth(_CallTrees, 1);

            // calc displayed cycle count
            int totalCycleCount = 0;
            foreach (var treeNode in _CallTrees)
                totalCycleCount += treeNode.CPUMetrics.InclusiveCycleCount;
            _TotalCycleCount = totalCycleCount;

            int displayedCycleCount = totalCycleCount;
            if (_SelectedRoutine != null)
            {
                if (_SelectedCallStack != null)
                    displayedCycleCount = _SelectedRoutine.MetricsByStack[_SelectedCallStack].InclusiveCycleCount;
                else
                    displayedCycleCount = _SelectedRoutine.AggregateMetrics.InclusiveCycleCount;
            }

            // calc scaler
            double scaler = (double)(Width - _VScrollBar.Width - 2 * marginSize) / displayedCycleCount;

            // layout
            var hashSet = new HashSet<Rectangle>();

            int top = marginSize;
            if (_FlipView)
            {
                int contentHeight = (maxTreeDepth * cellHeight) + (2 * marginSize);
                if (contentHeight <= Height - _HScrollBar.Height)
                    top = Height - _HScrollBar.Height - marginSize - cellHeight;
                else
                    top = marginSize + (maxTreeDepth - 1) * cellHeight;
                cellHeight = -cellHeight;
            }
            LayoutSiblings(_CallTrees, marginSize, top, scaler, cellHeight, hashSet);

            UpdateScrollbars(marginSize, Math.Abs(cellHeight), maxTreeDepth);
        }

        private void LayoutSiblings(IReadOnlyList<CallTreeNode> siblings, int left, int top, double scaler, int cellHeight, HashSet<Rectangle> hashSet)
        {
            // sort siblings by their InclusiveCycleCount
            var sortedSiblings = siblings.ToList();
            sortedSiblings.Sort((a, b) => b.CPUMetrics.InclusiveCycleCount.CompareTo(a.CPUMetrics.InclusiveCycleCount));

            // layout siblings
            int cycleCount = 0;
            foreach (var sibling in sortedSiblings)
            {
                int siblingLeft = left + (int)double.Round(scaler * cycleCount);
                cycleCount += sibling.CPUMetrics.InclusiveCycleCount;
                int siblingRight = left + (int)double.Round(scaler * cycleCount);
                var rect = new Rectangle(siblingLeft, top, siblingRight - siblingLeft, Math.Abs(cellHeight));
                if (!hashSet.Contains(rect))
                {
                    hashSet.Add(rect);
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
                    LayoutSiblings(sibling.Children, siblingLeft, top + cellHeight, scaler, cellHeight, hashSet);
                }
            }
        }

        private void UpdateScrollbars(int marginSize, int cellHeight, int maxTreeDepth)
        {
            // horizontal scroll bar
            _ScrollTopLeft.X = 0;
            int contentWidth = 0;
            if (_SelectedRoutine != null)
            {
                foreach (var routineCell in _RoutineCells)
                {
                    if (routineCell.Routine == _SelectedRoutine &&
                        routineCell.CallStack == _SelectedCallStack)
                    {
                        _ScrollTopLeft.X = routineCell.Rectangle.X - marginSize;
                    }

                    if (contentWidth < routineCell.Rectangle.Right)
                        contentWidth = routineCell.Rectangle.Right;
                }
            }

            _HScrollBar.Enabled = (Width - _VScrollBar.Width < contentWidth);
            if (_HScrollBar.Enabled)
            {
                _HScrollBar.Minimum = 0;
                _HScrollBar.Maximum = contentWidth + marginSize;
                _HScrollBar.LargeChange = Width - _VScrollBar.Width;
                _HScrollBar.SmallChange = DeviceDpi / 2;
                _HScrollBar.Value = _ScrollTopLeft.X;
            }

            // vertical scroll bar
            int contentHeight = (maxTreeDepth * cellHeight) + (2 * marginSize);
            _VScrollBar.Enabled = (Height - _HScrollBar.Height < contentHeight);
            if (_VScrollBar.Enabled)
            {
                _VScrollBar.Minimum = 0;
                _VScrollBar.Maximum = contentHeight;
                _VScrollBar.LargeChange = Height - _HScrollBar.Height;
                _VScrollBar.SmallChange = DeviceDpi / 2;
                _VScrollBar.Value = _ScrollTopLeft.Y;
            }
        }

        private void ScrollBar_VScroll(object? sender, ScrollEventArgs e)
        {
            if (_ScrollTopLeft.Y != e.NewValue)
            {
                _ScrollTopLeft.Y = e.NewValue;
                Invalidate();
            }
        }

        private void ScrollBar_HScroll(object? sender, ScrollEventArgs e)
        {
            if (_ScrollTopLeft.X != e.NewValue)
            {
                _ScrollTopLeft.X = e.NewValue;
                Invalidate();
            }
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
                Invalidate(_FocusRoutineCell.Rectangle);

            Invalidate(routineCell.Rectangle);
            _FocusRoutineCell = routineCell;
            Update();
        }

        private new void Invalidate(Rectangle rect)
        {
            rect.X -= _ScrollTopLeft.X;
            rect.Y -= _ScrollTopLeft.Y;
            base.Invalidate(rect);
        }

        private void RemoveFocus()
        {
            if (_FocusRoutineCell != null)
            {
                Invalidate(_FocusRoutineCell.Rectangle);
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
        private Dictionary<ushort, string> _Labels = new();
        private Color _ColorLightRed = Color.FromArgb(0xFF, 0x80, 0x80);
        private bool _LayoutInvalid;

        private ToolTip _ToolTip = new ToolTip();
        private string _ToolTipText = string.Empty;
        private Point _ToolTipLocation;
        private System.Windows.Forms.Timer _ToolTipTimer = new();

        private Point _ScrollTopLeft = new();
        private VScrollBar _VScrollBar;
        private HScrollBar _HScrollBar;
    }
}
