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

using BeebPerf.model;
using System.Diagnostics.CodeAnalysis;
using static BeebPerf.model.DisplaySettings;

namespace BeebPerf.ux
{
    internal class FlameGraphView : Panel, IDataView, IEMFExporter
    {
        public FlameGraphView() : base() 
        {
            DoubleBuffered = true;
            VisibleChanged += OnVisibleChanged;

            InitializeScrollBars();
            InitializeToolTips();
            InitializeButtons();
        }

        public void AddCallTree(CallTreeNode treeNode)
        {
            using var token = _ReentrancyGuard.TryEnter();
            if (token == null) return;

            AddCallTreeInternal(treeNode);
        }

        public void Clear()
        {
            using var token = _ReentrancyGuard.TryEnter();
            if (token == null) return;

            ClearInternal();
        }

        public void SelectRoutine(Routine routine, CallStack callStack)
        {
            using var token = _ReentrancyGuard.TryEnter();
            if (token == null) return;

            SelectRoutineInternal(routine, callStack);
        }

        public void ClearSelection()
        {
            using var token = _ReentrancyGuard.TryEnter();
            if (token == null) return;

            ClearSelectionInternal();
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            (this as IDataView).UpdateButtons();
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
            Invalidate();

            _PrevWidth = Width;
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            using var token = _ReentrancyGuard.TryEnter();
            if (token == null) return;

            var form = FindForm() as BeebPerfForm;
            if (form is null) return;

            foreach (var routineCell in _RoutineCells)
            {
                if (!routineCell.Rectangle.Contains(PixelsToLayout(e.Location)))
                    continue;

                if (routineCell.Routine != _SelectedRoutine ||
                   !routineCell.CallStack.Equals(_SelectedCallStack))
                {
                    SelectRoutineInternal(routineCell.Routine, routineCell.CallStack);
                    form.SetSelectedRoutine(routineCell.Routine, routineCell.CallStack, memoryAccess: null);
                }
                return;
            }

            if (_SelectedRoutine != null)
            {
                ClearSelectionInternal();
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

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (_VScrollBar.Enabled)
            {
                int value = _VScrollBar.Value - (e.Delta * _VScrollBar.SmallChange / 120/*WHEEL_DELTA*/);
                _VScrollBar.Value = Math.Clamp(value, 0, _VScrollBar.Maximum - _VScrollBar.LargeChange);
                Invalidate();
            }
        }

        private void AddCallTreeInternal(CallTreeNode treeNode)
        {
            _CallTrees.Add(treeNode);
            _InvalidLayout = true;
            Invalidate();
        }

        private void ClearInternal()
        {
            _CallTrees.Clear();
            _InvalidLayout = true;
            Invalidate();
        }

        private void SelectRoutineInternal(Routine routine, CallStack callStack)
        {
            if (routine == _SelectedRoutine && callStack.Equals(_SelectedCallStack))
                return;

            _SelectedRoutine = routine;
            _SelectedCallStack = callStack;

            _InvalidLayout = true;
            Invalidate();
        }

        private void ClearSelectionInternal()
        {
            if (_SelectedRoutine == null && _SelectedCallStack == null)
                return;

            _SelectedRoutine = null;
            _SelectedCallStack = null;

            _InvalidLayout = true;
            Invalidate();
        }

        private void OnVisibleChanged(object? sender, EventArgs e)
        {
            if (!Visible)
                HideToolTip();
        }

        void IEMFExporter.Paint(Graphics graphics)
        {
            DoPaint(graphics, paintEMF: true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (_InvalidLayout)
            {
                LayoutRoutineCells();
                if (_SelectedRoutine != null)
                {
                    ScrollSelectedRoutineIntoView();
                }
                else
                {
                    SetVScrollValue(0);
                    SetHScrollValue(0);
                }

                _InvalidLayout = false;
            }

            DoPaint(e.Graphics, paintEMF: false);
        }

        private void DoPaint(Graphics graphics, bool paintEMF)
        {
            bool lightMode = (ForeColor.GetBrightness() < 0.5);
            Color lineColor = lightMode ? Color.DarkRed : _ColorLightRed;
            Color fillColor = lightMode ? _ColorLightRed : Color.DarkRed;

            using var colorPen = new Pen(lineColor);
            using var colorSelectedPen = new Pen(lineColor, 2);
            using var colorBrush = new SolidBrush(fillColor);
            using var colorFocusBrush = new SolidBrush(Blend(fillColor, ForeColor, 0.1));

            Color grayFillColor = lightMode ? Color.LightGray : Color.Gray;
            Color grayLineColor = Blend(lightMode ? Color.LightGray : Color.Gray, ForeColor, 0.1);

            using var grayPen = new Pen(grayLineColor);
            using var graySelectedPen = new Pen(grayLineColor, 2);
            using var grayBrush = new SolidBrush(grayFillColor);
            using var grayFocusBrush = new SolidBrush(Blend(grayFillColor, ForeColor, 0.1));

            using var textBrush = new SolidBrush(ForeColor);

            foreach (var routineCell in _RoutineCells)
            {
                var callType = routineCell.CallStack.CallType;
                bool isTailCallOrFallthrough = (callType == CallType.TailCall || callType == CallType.FallThrough);

                if (!_ShowCallTypes)
                    isTailCallOrFallthrough = false;

                PaintRoutineCell(
                    graphics,
                    paintEMF,
                    routineCell,
                    isTailCallOrFallthrough ? grayPen : colorPen,
                    isTailCallOrFallthrough ? graySelectedPen : colorSelectedPen,
                    isTailCallOrFallthrough ? grayBrush : colorBrush,
                    isTailCallOrFallthrough ? grayFocusBrush : colorFocusBrush,
                    textBrush);
            }
        }

        private void PaintRoutineCell(
            Graphics graphics,
            bool paintEMF,
            RoutineCell routineCell,
            Pen linePen,
            Pen selectedLinePen,
            Brush fillBrush, 
            Brush focusFillBrush,
            Brush textBrush)
        {
            var clientRect = LayoutToPixels(routineCell.Rectangle, ignoreVScrollValue: paintEMF);

            // paint background
            if (clientRect.Width > 1)
                graphics.FillRectangle((routineCell == _FocusRoutineCell) ? focusFillBrush : fillBrush, clientRect);

            // paint border
            Pen borderPen = linePen;
            var borderRect = clientRect;
            if (borderRect.Width > 0)
            {
                if (routineCell.Routine == _SelectedRoutine &&
                    routineCell.CallStack.Equals(_SelectedCallStack))
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
                    graphics.DrawString(FormatCell(routineCell), Font, textBrush, clientRect, textFormat);
                }
            }
        }

        private Rectangle LayoutToPixels(Rectangle rect, bool ignoreVScrollValue = false)
        {
            int x = rect.X - GetHScrollValue();

            int y = rect.Y;
            if (!_FlipView)
                y = GetClientExtents().Height - y - rect.Height;

            if (ignoreVScrollValue)
            {
                if (!_FlipView)
                {
                    if (_VScrollBar.Enabled)
                        y += (_VScrollBar.Maximum - _VScrollBar.LargeChange);
                    else
                        y += _LayoutExtents.Height - Height + _HScrollBar.Height;
                }
            }
            else
            {
                if (_FlipView)
                    y -= GetVScrollValue();
                else
                    y += GetVScrollValue();
            }

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
            var percentage = double.Min(100.0 * routineCell.CycleCount / routineCell.TotalCycleCount, 100);
            return $"{routineCell.CycleCount:N0} ({percentage:F2}%)";
        }

        private string FormatRoutine(RoutineCell routineCell)
        {
            var routine = routineCell.Routine;
            if (routine.Label.Length > 0)
                return $"{FormatAddress(routine.StartAddress)} {routine.Label}";
            else
                return FormatAddress(routine.StartAddress);
        }

        private string FormatCell(RoutineCell routineCell)
        {
            var routine = routineCell.Routine;
            var percentage = double.Min(100.0 * routineCell.CycleCount / routineCell.TotalCycleCount, 100);
            if (routine.Label.Length > 0)
                return $"{FormatAddress(routine.StartAddress)} {routine.Label} {percentage:F2}%";
            else
                return FormatAddress(routine.StartAddress);
        }

        private string FormatAddress(CanonicalAddress address)
        {
            var form = FindForm() as BeebPerfForm;
            if (form is null) return string.Empty;

            return form.DisplaySettings.Format(Setting.Address, address.Address);
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
                    int selectedRoutineCycleCount;
                    if (_SelectedCallStack != null)
                        selectedRoutineCycleCount = _SelectedRoutine.MetricsByStack.TryGetValue(_SelectedCallStack, out var metrics) ? metrics.InclusiveCycleCount : 0;
                    else
                        selectedRoutineCycleCount = _SelectedRoutine.AggregateMetrics.InclusiveCycleCount;

                    if (selectedRoutineCycleCount > 0) 
                        displayedCycleCount = selectedRoutineCycleCount;
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

            // calculate total cycle count
            int totalCycleCount = 0;
            if (_SiblingPercentages)
                foreach (var sibling in sortedSiblings)
                    totalCycleCount += sibling.CPUMetrics.InclusiveCycleCount;
            else 
                totalCycleCount = _TotalCycleCount;

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
                        CycleCount = sibling.CPUMetrics.InclusiveCycleCount,
                        TotalCycleCount = totalCycleCount
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
                   !routineCell.CallStack.Equals(_SelectedCallStack))
                    continue;

                int marginSize = Font.Height / 2;
                SetHScrollValue(routineCell.Rectangle.X - marginSize);
                SetVScrollValue(routineCell.Rectangle.Y - marginSize);
                break;
            }
        }

        [MemberNotNull(nameof(_VScrollBar), nameof(_HScrollBar))]
        private void InitializeScrollBars()
        {
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

        [MemberNotNull(nameof(_ToolTipTimer))]
        private void InitializeToolTips()
        {
            _ToolTipTimer = new();
            _ToolTipTimer.Interval = 500;
            _ToolTipTimer.Tick += ToolTipTimer_Tick;
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
            _ToolTipTimer.Stop();
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

        [MemberNotNull(nameof(_PercentageButton), nameof(_CopyButton), nameof(_CallTypesButton), nameof(_FlipViewButton))]
        private void InitializeButtons()
        {
            _CopyButton = new()
            {
                Name = "copyButton",
                ImageResourceName = "copyButton.Image",
                ToolTipText = "Copy",
                Parent = this
            };
            _CopyButton.Click += copyButton_Click;

            _PercentageButton = new()
            {
                Name = "percentageButton",
                ImageResourceName = "percentageButton.Image",
                ToolTipText = "global/sibling percentages",
                Parent = this
            };
            _PercentageButton.Click += percentageButton_Click;

            _CallTypesButton = new()
            {
                Name = "callTypesButton",
                ImageResourceName = "callTypesButton.Image",
                ToolTipText = "Call types",
                Parent = this
            };
            _CallTypesButton.Click += callTypesButton_Click;

            _FlipViewButton = new()
            {
                Name = "flipViewButton",
                ImageResourceName = "flipViewButton.Image",
                ToolTipText = "Flip view",
                Parent = this
            };
            _FlipViewButton.Click += flipViewButton_Click;
        }

        private VScrollBar? GetVScrollBar()
        {
            foreach (Control control in Controls)
            {
                if (control is VScrollBar vScroll)
                    return vScroll;
            }
            return null;
        }

        void IDataView.UpdateButtons()
        {
            if (_CopyButton == null || _PercentageButton == null || _CallTypesButton == null || _FlipViewButton == null)
                return;

            int width = Width;
            var vScrollBar = GetVScrollBar();
            if (vScrollBar != null && vScrollBar.Visible)
                width -= vScrollBar.Width;

            int padding = 2;

            SuspendLayout();

            int left = width - _CopyButton.Width - padding;
            _CopyButton.Location = new Point(left, padding);
            _CopyButton.Visible = true;

            left -= _PercentageButton.Width;
            _PercentageButton.Location = new Point(left, padding);
            _PercentageButton.Visible = true;

            left -= _CallTypesButton.Width;
            _CallTypesButton.Location = new Point(left, padding);
            _CallTypesButton.Visible = true;

            left -= _FlipViewButton.Width;
            _FlipViewButton.Location = new Point(left, padding);
            _FlipViewButton.Visible = true;

            ResumeLayout(performLayout: false);
        }

        private void percentageButton_Click(object? sender, EventArgs e)
        {
            _SiblingPercentages = !_SiblingPercentages;
            LayoutRoutineCells();
            Invalidate();
        }

        private void copyButton_Click(object? sender, EventArgs e)
        {
            var form = FindForm() as BeebPerfForm;
            if (form == null) return;

            Exporter.CopyToClipboard(form, _LayoutExtents, this);
        }

        private void callTypesButton_Click(object? sender, EventArgs e)
        {
            _ShowCallTypes = !_ShowCallTypes;
            Invalidate();
        }

        public void flipViewButton_Click(object? sender, EventArgs e)
        {
            _FlipView = !_FlipView;
            SetVScrollValue(0);
            Invalidate();
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

        private class RoutineCell
        {
            public required Routine Routine;
            public required CallStack CallStack;
            public required Rectangle Rectangle;
            public required int CycleCount;
            public required int TotalCycleCount;
        }

        private Routine? _SelectedRoutine = null;
        private CallStack? _SelectedCallStack = null;
        private RoutineCell? _FocusRoutineCell;
        private List<RoutineCell> _RoutineCells = new();
        private List<CallTreeNode> _CallTrees = new();
        private int _TotalCycleCount;
        private Color _ColorLightRed = Color.FromArgb(0xFF, 0x80, 0x80);
        private bool _InvalidLayout;
        private ReentrancyGuard _ReentrancyGuard = new();

        private ToolTip _ToolTip = new ToolTip();
        private string _ToolTipText = string.Empty;
        private Point _ToolTipLocation;
        private System.Windows.Forms.Timer _ToolTipTimer = new();

        private Size _LayoutExtents = new();
        private int _PrevWidth = 0;
        private VScrollBar _VScrollBar;
        private HScrollBar _HScrollBar;
        private ButtonEx _CopyButton;
        private ButtonEx _PercentageButton;
        private ButtonEx _CallTypesButton;
        private ButtonEx _FlipViewButton;
        private bool _SiblingPercentages = false;
        private bool _ShowCallTypes = false;
        private bool _FlipView = false;
    }
}
