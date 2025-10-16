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
using System.Diagnostics;

namespace BeebPerf.ux
{
    internal class RoutineGridView : DataGridView
    {
        private const int RoutineColumnIndex = 0;
        private const int SelfCPUColumnIndex = 1;
        private const int TotalCPUColumnIndex = 2;
        private const int ElapsedCPUColumnIndex = 3;
        private const int ExecutionCountColumnIndex = 4;

        public RoutineGridView() : base()
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

            AutoGenerateColumns = false;
            Columns.Add("Routine", "Routine");
            Columns.Add("SelfCPU", "Self CPU [#cycles, %]");
            Columns.Add("TotalCPU", "Total CPU [#cycles, %]");
            Columns.Add("ElapsedCPU", "Elapsed CPU [#cycles, %]");
            Columns.Add("ExecutionCount", "Execution count");

            var cellTemplate = new CellTemplate();
            foreach (DataGridViewColumn column in Columns)
                column.CellTemplate = cellTemplate;

            Columns[SelfCPUColumnIndex]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            Columns[TotalCPUColumnIndex]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            Columns[ElapsedCPUColumnIndex]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            Columns[ExecutionCountColumnIndex]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            Sort(Columns[SelfCPUColumnIndex]!, System.ComponentModel.ListSortDirection.Descending);
        }

        public void Clear()
        {
            Rows.Clear();
        }

        public void AddRoutine(Routine routine)
        {
            Rows.Add(routine, routine, routine, routine, routine);
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

        private void SelectionChangedFunc(object? sender, EventArgs e)
        {
            BeebPerfForm form = (BeebPerfForm)GetParentForm();
            if (SelectedRows.Count == 1)
            {
                var routine = (Routine)SelectedRows[0].Cells[0].Value!;
                form.SetSelectedRoutine(routine, callStack: null);
            }
            else
            {
                form.ClearSelectedRoutine();
            }
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
                var routineGridView = (RoutineGridView)DataGridView!;
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
                RoutineGridView routineGridView, 
                Graphics graphics, 
                Rectangle cellBounds, 
                DataGridViewElementStates cellState)
            {
                double ratio = ColumnIndex switch
                {
                    SelfCPUColumnIndex => (double)routine.AggregateMetrics.SelfCycleCount / routineGridView.TotalCycleCount,
                    TotalCPUColumnIndex => (double)routine.AggregateMetrics.InclusiveCycleCount / routineGridView.TotalCycleCount,
                    ElapsedCPUColumnIndex => (double)routine.AggregateMetrics.ElapsedCycleCount / routineGridView.TotalCycleCount,
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
    }
}
