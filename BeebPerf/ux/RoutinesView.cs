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
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;

namespace BeebPerf.ux
{
    internal class RoutinesView : DataGridView
    {
        private const int RoutineColumnIndex = 0;
        private const int SelfCPUColumnIndex = 1;
        private const int TotalCPUColumnIndex = 2;
        private const int ElapsedCPUColumnIndex = 3;
        private const int InterruptsColumnIndex = 4;
        private const int ExecutionCountColumnIndex = 5;

        public RoutinesView() : base()
        {
            AllowUserToAddRows = false;
            AllowUserToDeleteRows = false;
            AllowUserToResizeColumns = false;
            AllowUserToResizeRows = false;
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            BackgroundColor = DefaultCellStyle.BackColor;
            CellBorderStyle = DataGridViewCellBorderStyle.None;
            CellFormatting += CellFormattingFunc;
            MultiSelect = false;
            ReadOnly = true;
            RowHeadersVisible = false;
            RowTemplate.DefaultCellStyle.NullValue = null;
            SelectionChanged += SelectionChangedFunc;
            SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            SortCompare += SortCompareFunc;
            Sorted += SortedFunc;

            AutoGenerateColumns = false;
            Columns.Add("Routine", "Routine");
            Columns.Add("SelfCPU", "Self CPU [#cycles, %]");
            Columns.Add("TotalCPU", "Total CPU [#cycles, %]");
            Columns.Add("ElapsedCPU", "Elapsed CPU [#cycles, %]");
            Columns.Add("Interrupts", "Interrupts [#cycles, %]");
            Columns.Add("ExecutionCount", "Execution count");

            var cellTemplate = new CellTemplate();
            foreach (DataGridViewColumn column in Columns)
                column.CellTemplate = cellTemplate;

            SetColumnAlignment(SelfCPUColumnIndex, DataGridViewContentAlignment.MiddleRight);
            SetColumnAlignment(TotalCPUColumnIndex, DataGridViewContentAlignment.MiddleRight);
            SetColumnAlignment(ElapsedCPUColumnIndex, DataGridViewContentAlignment.MiddleRight);
            SetColumnAlignment(InterruptsColumnIndex, DataGridViewContentAlignment.MiddleRight);
            SetColumnAlignment(ExecutionCountColumnIndex, DataGridViewContentAlignment.MiddleRight);

            SetColumnHeaderToolTip(RoutineColumnIndex, "Routine address and label");
            SetColumnHeaderToolTip(SelfCPUColumnIndex,"Cycles used just by this routine");
            SetColumnHeaderToolTip(TotalCPUColumnIndex,"Total cycles used by this routine and all routines it calls.");
            SetColumnHeaderToolTip(ElapsedCPUColumnIndex, "Total cycles elapsed during execution of routine, including cycles in interrupts");
            SetColumnHeaderToolTip(InterruptsColumnIndex, "Total cycles spent servicing interrupts during execution of routine");
            SetColumnHeaderToolTip(ExecutionCountColumnIndex, "Number of times this routine was executed");

            Sort(Columns[SelfCPUColumnIndex]!, ListSortDirection.Descending);
        }

        private void SetColumnAlignment(int columnIndex, DataGridViewContentAlignment alignment)
        {
            Columns[columnIndex]!.DefaultCellStyle.Alignment = alignment;
        }

        private void SetColumnHeaderToolTip(int columnIndex, string text)
        {
            Columns[columnIndex].HeaderCell.ToolTipText = text;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            // ensure selection is correctly reflected when window is created
            _SuppressSelectionChangedEvent++;
            base.OnHandleCreated(e);

            BeginInvoke(new MethodInvoker(() =>
            {
                if (_SelectedRoutine != null)
                    SelectRoutine(_SelectedRoutine);
                else
                    ClearSelection();
                _SuppressSelectionChangedEvent--;
            }));
        }

        public void ShowHotRoutines()
        {
            _SuppressSelectionChangedEvent++;
            Sort(Columns[SelfCPUColumnIndex], ListSortDirection.Descending);
            _SuppressSelectionChangedEvent--;

            ScrollToTop();
            Invalidate();
        }

        public void SelectRoutine(Routine routine)
        {
            foreach (DataGridViewRow row in Rows)
            {
                Routine rowRoutine = (Routine)row.Cells[0].Value!;
                if (routine != rowRoutine)
                    continue;

                _SelectedRoutine = rowRoutine;

                _SuppressSelectionChangedEvent++;
                row.Selected = true;
                _SuppressSelectionChangedEvent--;

                FirstDisplayedScrollingRowIndex = Math.Clamp(row.Index - DisplayedRowCount(false) + 1, 0, Rows.Count - 1);
                return;
            }

            ClearSelection();
        }

        public new void ClearSelection()
        {
            _SelectedRoutine = null;

            _SuppressSelectionChangedEvent++;
            base.ClearSelection();
            _SuppressSelectionChangedEvent--;
        }

        private void SelectionChangedFunc(object? sender, EventArgs e)
        {
            if (_SuppressSelectionChangedEvent > 0)
                return;

            BeebPerfForm form = (BeebPerfForm)GetParentForm();
            if (SelectedRows.Count == 1)
            {
                _SelectedRoutine = (Routine)SelectedRows[0].Cells[0].Value!;
                form.SetSelectedRoutine(sender: this, _SelectedRoutine, callStack: null, memoryAccess: null);
            }
            else
            {
                _SelectedRoutine = null;
                form.ClearSelectedRoutine(sender: this);
            }
        }

        protected override void OnColumnHeaderMouseClick(DataGridViewCellMouseEventArgs e)
        {
            _SuppressSelectionChangedEvent++;
            base.OnColumnHeaderMouseClick(e);
            _SuppressSelectionChangedEvent--;
        }

        private void SortedFunc(Object? sender, EventArgs args)
        {
            // ensure selection is correctly reflected after a sort
            if (Rows.Count == 0)
                return;

            if (_SelectedRoutine != null)
            {
                foreach (DataGridViewRow row in Rows)
                {
                    if (_SelectedRoutine != (Routine)row.Cells[0].Value!)
                        continue;

                    FirstDisplayedScrollingRowIndex = Math.Clamp(row.Index - DisplayedRowCount(false) + 1, 0, Rows.Count - 1);
                    break;
                }
            }
            else
            {
                ClearSelection();
                ScrollToTop();
            }
        }

        public void AddRoutine(Routine routine)
        {
            _SuppressSelectionChangedEvent++;
            Rows.Add(routine, routine, routine, routine, routine, routine);
            _SuppressSelectionChangedEvent--;

            ClearSelection();
        }

        public void Clear()
        {
            _SelectedRoutine = null;
            Rows.Clear();
        }

        public int TotalCycleCount;
        public int MaxExecutionCount;

        private Form GetParentForm()
        {
            Control control = this;
            while (control is not Form)
                control = control!.Parent!;
            return (Form)control;
        }

        private void CellFormattingFunc(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.Value == null)
                return;

            var dataGrid = (DataGridView)sender!;
            var routine = (Routine)e.Value!;

            switch (e.ColumnIndex)
            {
                case RoutineColumnIndex:
                    int padding = routine.HotRoutine ? 4 : 0;
                    e.Value = $"{"".PadLeft(padding)}{routine.StartAddress} {routine.Label}";
                    break;

                case SelfCPUColumnIndex:
                    e.Value = FormatCPUMetric(routine.AggregateMetrics.SelfCycleCount);
                    e.FormattingApplied = true;
                    break;

                case TotalCPUColumnIndex:
                    e.Value = FormatCPUMetric(routine.AggregateMetrics.InclusiveCycleCount);
                    e.FormattingApplied = true;
                    break;

                case ElapsedCPUColumnIndex:
                    e.Value = FormatCPUMetric(routine.AggregateMetrics.ElapsedCycleCount);
                    e.FormattingApplied = true;
                    break;

                case InterruptsColumnIndex:
                    e.Value = FormatInterruptsMetric(routine.AggregateMetrics.ElapsedCycleCount, routine.AggregateMetrics.InclusiveCycleCount);
                    e.FormattingApplied = true;
                    break;

                case ExecutionCountColumnIndex:
                    e.Value = $"{routine.AggregateMetrics.ExecutionCount:N0}";
                    e.FormattingApplied = true;
                    break;

                default:
                    break;
            }
        }

        private void SortCompareFunc(object? sender, DataGridViewSortCompareEventArgs e)
        {
            var a = (Routine)e.CellValue1!;
            var b = (Routine)e.CellValue2!;

            switch (e.Column.Index)
            {
                case RoutineColumnIndex:
                    e.SortResult = a.StartAddress.Address - b.StartAddress.Address;
                    e.Handled = true;
                    break;

                case SelfCPUColumnIndex:
                    e.SortResult = a.AggregateMetrics.SelfCycleCount - b.AggregateMetrics.SelfCycleCount;
                    e.Handled = true;
                    break;

                case TotalCPUColumnIndex:
                    e.SortResult = a.AggregateMetrics.InclusiveCycleCount - b.AggregateMetrics.InclusiveCycleCount;
                    e.Handled = true;
                    break;

                case ElapsedCPUColumnIndex:
                    e.SortResult = a.AggregateMetrics.ElapsedCycleCount - b.AggregateMetrics.ElapsedCycleCount;
                    e.Handled = true;
                    break;

                case InterruptsColumnIndex:
                    e.SortResult = 
                        (a.AggregateMetrics.ElapsedCycleCount - a.AggregateMetrics.InclusiveCycleCount) - 
                        (b.AggregateMetrics.ElapsedCycleCount - b.AggregateMetrics.InclusiveCycleCount);
                    e.Handled = true;
                    break;

                case ExecutionCountColumnIndex:
                    e.SortResult = a.AggregateMetrics.ExecutionCount - b.AggregateMetrics.ExecutionCount;
                    e.Handled = true;
                    break;

                default:
                    break;
            }
        }

        private string FormatCPUMetric(int value)
        {
            var percentage = double.Min(100.0 * value / TotalCycleCount, 100.0);
            return $"{value:N0} ({percentage:F2}%)";
        }

        private string FormatInterruptsMetric(int elapsedCycleCount, int inclusiveCycleCount)
        {
            int interruptsCycleCount = elapsedCycleCount - inclusiveCycleCount;
            var percentage = double.Min(100.0 * interruptsCycleCount / elapsedCycleCount, 100.0);
            return $"{interruptsCycleCount:N0} ({percentage:F2}%)";
        }

        private void ScrollToTop()
        {
            if (Rows.Count > 0)
                FirstDisplayedScrollingRowIndex = 0;
        }

        public class CellTemplate : DataGridViewTextBoxCell
        {
            protected override void Paint(Graphics graphics,
                                          Rectangle clipBounds,
                                          Rectangle cellBounds,
                                          int rowIndex,
                                          DataGridViewElementStates cellState,
                                          object? value,
                                          object? formattedValue,
                                          string? errorText,
                                          DataGridViewCellStyle cellStyle,
                                          DataGridViewAdvancedBorderStyle advancedBorderStyle,
                                          DataGridViewPaintParts paintParts)
            {
                var routineGridView = (RoutinesView)DataGridView!;
                var routine = (Routine)value!;

                if (ColumnIndex == RoutineColumnIndex)
                {
                    // paint default, minus focus rect
                    base.Paint(
                        graphics, clipBounds, cellBounds, rowIndex, cellState,
                        value, formattedValue, errorText,
                        cellStyle, advancedBorderStyle, paintParts & ~DataGridViewPaintParts.Focus);
                }
                else
                {
                    Debug.Assert(
                        ColumnIndex == SelfCPUColumnIndex ||
                        ColumnIndex == TotalCPUColumnIndex ||
                        ColumnIndex == ElapsedCPUColumnIndex ||
                        ColumnIndex == InterruptsColumnIndex ||
                        ColumnIndex == ExecutionCountColumnIndex);

                    // paint default background
                    var backPaintParts = paintParts &
                        (DataGridViewPaintParts.Border |
                         DataGridViewPaintParts.Background |
                         DataGridViewPaintParts.ContentBackground |
                         DataGridViewPaintParts.SelectionBackground);

                    base.Paint(
                        graphics, clipBounds, cellBounds, rowIndex, cellState,
                        value, formattedValue, errorText,
                        cellStyle, advancedBorderStyle, backPaintParts);

                    // draw bar
                    DrawBar(routine, routineGridView, graphics, cellBounds, cellState);

                    // draw default foreground
                    var forePaintParts = paintParts & DataGridViewPaintParts.ContentForeground;
                    base.Paint(
                        graphics, clipBounds, cellBounds, rowIndex, cellState,
                        value, formattedValue, errorText,
                        cellStyle, advancedBorderStyle, forePaintParts);
                }

                // paint flame icon
                if (ColumnIndex == RoutineColumnIndex && routine.HotRoutine)
                {
                    int size = cellBounds.Height / 2;
                    int inset = cellBounds.Height / 4;
                    var rect = new Rectangle(cellBounds.Left + inset / 2, cellBounds.Top + inset, size, size);
                    BeebPerfForm form = (BeebPerfForm)routineGridView.GetParentForm();
                    graphics.DrawImage(form.FlameImage, rect);
                }
            }

            private void DrawBar(
                Routine routine, 
                RoutinesView routineGridView, 
                Graphics graphics, 
                Rectangle cellBounds, 
                DataGridViewElementStates cellState)
            {
                double ratio = ColumnIndex switch
                {
                    SelfCPUColumnIndex => (double)routine.AggregateMetrics.SelfCycleCount / routineGridView.TotalCycleCount,
                    TotalCPUColumnIndex => (double)routine.AggregateMetrics.InclusiveCycleCount / routineGridView.TotalCycleCount,
                    ElapsedCPUColumnIndex => (double)routine.AggregateMetrics.ElapsedCycleCount / routineGridView.TotalCycleCount,
                    InterruptsColumnIndex => (double)(routine.AggregateMetrics.ElapsedCycleCount - routine.AggregateMetrics.InclusiveCycleCount) / routine.AggregateMetrics.ElapsedCycleCount,
                    ExecutionCountColumnIndex => (double)routine.AggregateMetrics.ExecutionCount / routineGridView.MaxExecutionCount,
                    _ => -1
                };

                if (ratio < 0)
                    return;
                else if (ratio > 1.0)
                    ratio = 1.0;

                int margin = cellBounds.Height / 8;
                int maxWidth = cellBounds.Width - (margin * 2);
                int width = (int)double.Ceiling((double)ratio * maxWidth);

                var rect = new Rectangle(
                    cellBounds.Right - margin - width,
                    cellBounds.Y + margin,
                    width,
                    cellBounds.Height - margin * 2);

                bool selected = (cellState & DataGridViewElementStates.Selected) != 0;

                var color = Blend(
                    routineGridView.DefaultCellStyle.BackColor,
                    routineGridView.DefaultCellStyle.SelectionBackColor,
                    selected ? 0.75 : 0.25);

                using var brush = new SolidBrush(color);
                graphics.FillRectangle(brush, rect);
            }

            private Color Blend(Color first, Color second, double ratio)
            {
                int r = (int)(first.R * (1 - ratio) + second.R * ratio);
                int g = (int)(first.G * (1 - ratio) + second.G * ratio);
                int b = (int)(first.B * (1 - ratio) + second.B * ratio);
                return Color.FromArgb(r, g, b);
            }
        }

        private Routine? _SelectedRoutine;
        private int _SuppressSelectionChangedEvent;
    }
}
