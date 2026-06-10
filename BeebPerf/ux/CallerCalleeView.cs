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
using static BeebPerf.CPUAnalysis;
using static BeebPerf.model.DisplaySettings;

namespace BeebPerf.ux
{
    internal class CallerCalleeView : Panel, IDataView
    {
        public CallerCalleeView() : base() 
        {
            DoubleBuffered = true;
            VisibleChanged += OnVisibleChanged;
            _ToolTipTimer = new();
            _ToolTipTimer.Interval = 300; // ms before showing
            _ToolTipTimer.Tick += ToolTipTimer_Tick;

            InitializeButtons();
        }

        public void Initialize(int totalCycleCount)
        {
            using var token = _ReentrancyGuard.TryEnter();
            if (token == null) return;

            _TotalCycleCount = totalCycleCount;
        }

        public void SelectRoutine(Routine routine, List<RoutineMetrics> callerMetrics, List<RoutineMetrics> calleeMetrics)
        {
            _Routine = routine;
            _Callers = callerMetrics;
            _Callees = calleeMetrics;
            LayoutRoutineCells();
            (this as IDataView).UpdateButtons();
            Invalidate();
        }

        public void Clear()
        {
            _Routine = null;
            _Callers = new();
            _Callees = new();
            _RoutineCells.Clear();
            (this as IDataView).UpdateButtons();
            Invalidate();
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            using var token = _ReentrancyGuard.TryEnter();
            if (token == null) return;

            foreach (var routineCell in _RoutineCells)
            {
                if ((routineCell.CellType != RoutineCellType.Caller &&
                     routineCell.CellType != RoutineCellType.Callee) ||
                    !routineCell.Rectangle.Contains(e.X, e.Y))
                    continue;

                var form = FindForm() as BeebPerfForm;
                if (form is null) continue;

                form.SetSelectedRoutine(routineCell.Routine, callStack: null, memoryAccess : null);
                return;
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutRoutineCells();
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            foreach (var routineCell in _RoutineCells)
            {
                if (!routineCell.Rectangle.Contains(e.Location))
                    continue;

                ShowToolTip(routineCell, e.Location);
                if ((routineCell.CellType == RoutineCellType.Caller ||
                     routineCell.CellType == RoutineCellType.Callee) &&
                    routineCell != _FocusRoutineCell)
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

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using var brush = new SolidBrush(BackColor);
            e.Graphics.FillRectangle(brush, e.ClipRectangle);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            int borderSize = Font.Height / 2;
            int panelWidth = (Width - borderSize * 6) / 3;
            int panelHeight = Height - borderSize;

            PaintPanel("Calling Routines", new Rectangle(borderSize, 0, panelWidth, panelHeight), e);
            PaintPanel("Current Routine", new Rectangle(3 * borderSize + panelWidth, 0, panelWidth, panelHeight), e);
            PaintPanel("Called Routines", new Rectangle(5 * borderSize + 2 * panelWidth, 0, panelWidth, panelHeight), e);

            int arrowSize = 2 * borderSize;
            PaintArrow(new Rectangle(borderSize + panelWidth, panelHeight / 2, arrowSize, arrowSize), e);
            PaintArrow(new Rectangle(3 * borderSize + 2 * panelWidth, panelHeight / 2, arrowSize, arrowSize), e);

            foreach (var routineCell in _RoutineCells)
                PaintRoutineCell(routineCell, e);
        }

        private void PaintPanel(string caption, Rectangle panelRect, PaintEventArgs e)
        {
            using var font = new System.Drawing.Font(Font, FontStyle.Bold);
            using var brush = new SolidBrush(ForeColor);
            using var pen = new Pen(ForeColor);

            int captionHeight = Font.Height * 3 / 2;
            var captionRect = new Rectangle(panelRect.X, panelRect.Y, panelRect.Width, captionHeight);
            StringFormat textFormat = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap
            };
            e.Graphics.DrawString(caption, font, brush, captionRect, textFormat);

            var borderRect = new Rectangle(panelRect.X, panelRect.Y + captionHeight, panelRect.Width, panelRect.Height - captionHeight);
            e.Graphics.DrawRectangle(pen, borderRect);
        }

        private void PaintArrow(Rectangle bounds, PaintEventArgs e)
        {
            int offsetX = bounds.Width / 2;
            int offsetY = bounds.Height / 3;

            var points = new Point[]
            {
                new Point(bounds.Left, bounds.Top + offsetY),
                new Point(bounds.Left + offsetX, bounds.Top + offsetY),
                new Point(bounds.Left + offsetX, bounds.Top),
                new Point(bounds.Right, (bounds.Top + bounds.Bottom) / 2),
                new Point(bounds.Left + offsetX, bounds.Bottom),
                new Point(bounds.Left + offsetX, bounds.Bottom - offsetY),
                new Point(bounds.Left, bounds.Bottom - offsetY),
            };

            using var brush = new SolidBrush(Blend(ForeColor, BackColor, 0.25));
            e.Graphics.FillPolygon(brush, points);
        }

        private void PaintRoutineCell(RoutineCell routineCell, PaintEventArgs e)
        {
            var bounds = routineCell.Rectangle;

            // paint background
            Color fillColor = BackColor;

            if (routineCell.ShowHotness)
                fillColor = CalcHotnessColor(routineCell);

            if (routineCell == _FocusRoutineCell)
                fillColor = Blend(fillColor, ForeColor, 0.1);

            using var fillBrush = new SolidBrush(fillColor);
            e.Graphics.FillRectangle(fillBrush, bounds);

            // paint border
            Color borderColor = fillColor;
            if (routineCell.CellType != RoutineCellType.Self)
            {
                borderColor = ForeColor;
                if (routineCell.ShowHotness)
                    borderColor = BackColor.GetBrightness() > 0.5 ? Color.DarkRed : _ColorLightRed;
            }
            using var pen = new Pen(borderColor);
            e.Graphics.DrawRectangle(pen, bounds);

            // paint text
            int textHeight = 2 * Font.Height;
            int padding = textHeight / 4;
            var textRect = new Rectangle(bounds.X + padding, bounds.Y, bounds.Width - 2 * padding, textHeight);
            textRect.Intersect(bounds);
            if (textRect.Height >= Font.Height)
            {
                // paint metric (right aligned)
                string metrics = FormatMetrics(routineCell);
                Size metricsMeasure = TextRenderer.MeasureText(
                    metrics, Font,
                    new Size(int.MaxValue, int.MaxValue),
                    TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);

                StringFormat textFormat = new StringFormat
                {
                    Alignment = StringAlignment.Far,
                    LineAlignment = StringAlignment.Center,
                    FormatFlags = StringFormatFlags.NoWrap
                };

                using var brush = new SolidBrush(ForeColor);
                e.Graphics.DrawString(metrics, Font, brush, textRect, textFormat);

                // paint routine address/executionCountLabel (with ellipses)
                textRect.Width -= metricsMeasure.Width;

                textFormat = new StringFormat
                {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Center,
                    FormatFlags = StringFormatFlags.NoWrap,
                    Trimming = StringTrimming.EllipsisCharacter
                };

                string label = (routineCell.CellType == RoutineCellType.SelfBody) ? "Function Body" : FormatRoutine(routineCell);
                e.Graphics.DrawString(label, Font, brush, textRect, textFormat);
            }
        }

        private Color CalcHotnessColor(RoutineCell routineCell)
        {
            double hotness = Math.Clamp((double)routineCell.CycleCount / _Routine!.AggregateMetrics.InclusiveCycleCount, 0, 1);
            var hotColor = BackColor.GetBrightness() > 0.5 ? _ColorLightRed : Color.DarkRed;
            return Blend(BackColor, hotColor, hotness);
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

        private string FormatAddress(CanonicalAddress address)
        {
            var form = FindForm() as BeebPerfForm;
            if (form is null) return string.Empty;

            return form.DisplaySettings.Format(Setting.Address, address.Address);
        }

        private void LayoutRoutineCells()
        {
            _RoutineCells.Clear();

            int borderSize = Font.Height / 2;
            int captionHeight = Font.Height * 3 / 2;

            int panelTop = captionHeight + borderSize;
            int panelWidth = (Width - borderSize * 12) / 3;
            int panelHeight = Height - captionHeight - 3 * borderSize;

            if (panelWidth < 0 || panelHeight < 0)
                return;

            var selfBounds = new Rectangle(borderSize + panelWidth + 5 * borderSize, panelTop, panelWidth, panelHeight);
            LayoutSelf(selfBounds);

            var callersBounds = new Rectangle(2 * borderSize, panelTop, panelWidth, panelHeight);
            LayoutCallerCallees(callersBounds, _Callers, RoutineCellType.Caller, borderSize);

            var calleesBounds = new Rectangle(borderSize + 2 * panelWidth + 9 * borderSize, panelTop, panelWidth, panelHeight);
            LayoutCallerCallees(calleesBounds, _Callees, RoutineCellType.Callee, borderSize);
        }

        private void LayoutSelf(Rectangle bounds)
        {
            if (_Routine is null)
                return;

            int cellHeight = 2 * Font.Height;

            var rect = new Rectangle(bounds.X, bounds.Y, bounds.Width, cellHeight);
            rect.Intersect(bounds);
            if (!rect.IsEmpty)
            {
                _RoutineCells.Add(new RoutineCell
                {
                    Rectangle = rect,
                    Routine = _Routine!,
                    CycleCount = _Routine!.AggregateMetrics.InclusiveCycleCount,
                    TotalCycleCount = _TotalCycleCount,
                    ExecutionCount = _Routine!.AggregateMetrics.ExecutionCount,
                    CellType = RoutineCellType.Self
                });
            }

            rect = new Rectangle(bounds.X, bounds.Y + cellHeight + 1, bounds.Width, cellHeight);
            rect.Intersect(bounds);
            if (!rect.IsEmpty)
            {
                _RoutineCells.Add(new RoutineCell
                {
                    Rectangle = rect,
                    Routine = _Routine!,
                    CycleCount = _Routine!.AggregateMetrics.SelfCycleCount,
                    TotalCycleCount = _TotalCycleCount,
                    ExecutionCount = _Routine!.AggregateMetrics.ExecutionCount,
                    CellType = RoutineCellType.SelfBody,
                    ShowHotness = true
                });
            }
        }

        private void LayoutCallerCallees(Rectangle bounds, List<RoutineMetrics> routineMetrics, RoutineCellType cellType, int borderSize)
        {
            if (routineMetrics.Count == 0)
                return;

            int availableHeight = bounds.Height - (routineMetrics.Count - 1) * borderSize - 1;

            int totalInclusiveCycles = 0;
            foreach (var routineMetric in routineMetrics)
                totalInclusiveCycles += routineMetric.CPUMetrics.InclusiveCycleCount;

            double scaler = (double)availableHeight / totalInclusiveCycles;
            int cycleCount = 0;
            for (int i = 0; i < routineMetrics.Count; i++)
            {
                var routineMetric = routineMetrics[i];

                int borderOffset = i * borderSize;
                int top = (int)double.Round(scaler * cycleCount) + borderOffset;
                cycleCount += routineMetric.CPUMetrics.InclusiveCycleCount;
                int bottom = (int)double.Round(scaler * cycleCount) + borderOffset;

                var rect = new Rectangle(bounds.X, bounds.Y + top, bounds.Width, bottom - top + 1);
                rect.Intersect(bounds);
                if (!rect.IsEmpty)
                {
                    _RoutineCells.Add(new RoutineCell
                    {
                        Rectangle = rect,
                        Routine = routineMetric.Routine,
                        CycleCount = routineMetric.CPUMetrics.InclusiveCycleCount,
                        TotalCycleCount = _SiblingPercentages ? totalInclusiveCycles : _TotalCycleCount,
                        ExecutionCount = routineMetric.CPUMetrics.ExecutionCount,
                        ShowHotness = (cellType == RoutineCellType.Callee),
                        CellType = cellType
                    });
                }
            }
        }

        [MemberNotNull(nameof(_PercentageButton))]
        private void InitializeButtons()
        {
            _PercentageButton = new()
            {
                Name = "percentageButton",
                ImageResourceName = "percentageButton.Image",
                ToolTipText = "global/relative percentages",
                Parent = this
            };
            _PercentageButton.Click += percentageButton_Click;
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
            int width = Width;
            var vScrollBar = GetVScrollBar();
            if (vScrollBar != null && vScrollBar.Visible)
                width -= vScrollBar.Width;

            int padding = 2;

            SuspendLayout();
            _PercentageButton.Location = new Point(width - _PercentageButton.Width - padding, padding);
            _PercentageButton.Visible = (_Routine != null);
            ResumeLayout(performLayout: false);
        }

        private void percentageButton_Click(object? sender, EventArgs e)
        {
            _SiblingPercentages = !_SiblingPercentages;
            LayoutRoutineCells();
            Invalidate();
        }

        private void ShowToolTip(RoutineCell routineCell, Point mousePosition)
        {
            _ToolTipTimer.Stop();
            _ToolTipTimer.Start();

            var cpuLabel = routineCell.CellType switch
            {
                RoutineCellType.SelfBody => "Self CPU",
                RoutineCellType.Caller => "Total CPU (in callee)",
                _ => "Total CPU"
            };

            var executionCountLabel = routineCell.CellType switch
            {
                RoutineCellType.Caller => "Execution count (in callee)",
                _ => "Execution count"
            };

            _ToolTipText = $"Routine: {FormatRoutine(routineCell)}\n" +
                           $"{cpuLabel}: {FormatMetrics(routineCell)}\n" +
                           $"{executionCountLabel}: {routineCell.ExecutionCount:N0}";
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
                Invalidate(_FocusRoutineCell.Rectangle);

            Invalidate(routineCell.Rectangle);
            _FocusRoutineCell = routineCell;
            Update();
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

        private enum RoutineCellType
        {
            Caller,
            Self,
            SelfBody,
            Callee
        }

        private class RoutineCell
        {
            public required Routine Routine;
            public required Rectangle Rectangle;
            public required int CycleCount;
            public required int TotalCycleCount;
            public required int ExecutionCount;
            public required RoutineCellType CellType;
            public bool ShowHotness;
        }

        private RoutineCell? _FocusRoutineCell;
        private List<RoutineCell> _RoutineCells = new();
        private List<RoutineMetrics> _Callers = new();
        private List<RoutineMetrics> _Callees = new();
        private Routine? _Routine;
        private int _TotalCycleCount;
        private Color _ColorLightRed = Color.FromArgb(0xFF, 0x80, 0x80);
        private ReentrancyGuard _ReentrancyGuard = new();

        private ToolTip _ToolTip = new ToolTip();
        private string _ToolTipText = string.Empty;
        private Point _ToolTipLocation;
        private System.Windows.Forms.Timer _ToolTipTimer = new();
        private ButtonEx _PercentageButton;
        private bool _SiblingPercentages = false;
    }
}
