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
    internal class RoutineGridView : DataGridView
    {
        public RoutineGridView() : base()
        {
            AllowUserToAddRows = false;
            AllowUserToDeleteRows = false;
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
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

            Columns["Routine"]!.CellTemplate = new CellTemplate();

            Sort(Columns["SelfCPU"]!, System.ComponentModel.ListSortDirection.Descending);
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

        private Form GetParentForm()
        {
            Control control = this;
            while (control is not Form)
            {
                control = control!.Parent!;
            }
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

            switch (dataGrid.Columns[e.ColumnIndex].Name)
            {
                case "Routine":
                    int padding = routine.HotRoutine ? 4 : 0;
                    e.Value = $"{"".PadLeft(padding)}{routine.StartAddress} {routine.Label}";
                    break;

                case "SelfCPU":
                    e.Value = FormatCPUMetric(routine.AggregateMetrics.SelfCycleCount);
                    e.FormattingApplied = true;
                    break;

                case "TotalCPU":
                    e.Value = FormatCPUMetric(routine.AggregateMetrics.InclusiveCycleCount);
                    e.FormattingApplied = true;
                    break;

                case "ElapsedCPU":
                    e.Value = FormatCPUMetric(routine.AggregateMetrics.ElapsedCycleCount);
                    e.FormattingApplied = true;
                    break;

                case "ExecutionCount":
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

            switch (e.Column.Name)
            {
                case "Routine":
                    e.SortResult = a.StartAddress.Address - b.StartAddress.Address;
                    e.Handled = true;
                    break;

                case "SelfCPU":
                    e.SortResult = a.AggregateMetrics.SelfCycleCount - b.AggregateMetrics.SelfCycleCount;
                    e.Handled = true;
                    break;

                case "TotalCPU":
                    e.SortResult = a.AggregateMetrics.InclusiveCycleCount - b.AggregateMetrics.InclusiveCycleCount;
                    e.Handled = true;
                    break;

                case "ElapsedCPU":
                    e.SortResult = a.AggregateMetrics.ElapsedCycleCount - b.AggregateMetrics.ElapsedCycleCount;
                    e.Handled = true;
                    break;

                case "ExecutionCount":
                    e.SortResult = a.AggregateMetrics.ExecutionCount - b.AggregateMetrics.ExecutionCount;
                    e.Handled = true;
                    break;

                default:
                    break;
            }

        }

        private string FormatCPUMetric(int value)
        {
            var percentage = (double)value * 100.0 / TotalCycleCount;
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
                base.Paint(graphics, clipBounds, cellBounds, rowIndex, cellState,
                            value, formattedValue, errorText,
                            cellStyle, advancedBorderStyle, paintParts);

                // paint flame icon
                var routineGridView = (RoutineGridView)DataGridView!;
                var routine = (Routine)value!;
                if (routine.HotRoutine)
                {
                    int size = cellBounds.Height / 2;
                    int inset = cellBounds.Height / 4;
                    var rect = new Rectangle(cellBounds.Left + inset / 2, cellBounds.Top + inset, size, size);
                    BeebPerfForm form = (BeebPerfForm)routineGridView.GetParentForm();
                    graphics.DrawImage(form.FlameImage, rect);
                }
            }
        }
    }
}
