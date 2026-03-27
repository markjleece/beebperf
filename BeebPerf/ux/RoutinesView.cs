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
using System.Diagnostics;
using static BeebPerf.MemoryAnalysis;

namespace BeebPerf.ux
{
    internal class RoutinesView : GridView<Routine>, IGridExporter
    {
        private const int RoutineColumnIndex = 0;
        private const int TotalCPUColumnIndex = 1;
        private const int SelfCPUColumnIndex = 2;
        private const int ElapsedCPUColumnIndex = 3;
        private const int InterruptsColumnIndex = 4;
        private const int ExecutionCountColumnIndex = 5;

        private const int ExportRoutineAddressColumnIndex = 0;
        private const int ExportRoutinePageColumnIndex = 1;
        private const int ExportRoutineLabelColumnIndex = 2;
        private const int ExportTotalCPUColumnIndex = 3;
        private const int ExportTotalCPUPercentageIndex = 4;
        private const int ExportSelfCPUColumnIndex = 5;
        private const int ExportSelfCPUPercentageIndex = 6;
        private const int ExportElapsedCPUColumnIndex = 7;
        private const int ExportElapsedCPUPercentageIndex = 8;
        private const int ExportInterruptsColumnIndex = 9;
        private const int ExportInterruptsPercentageIndex = 10;
        private const int ExportExecutionCountColumnIndex = 11;
        private const int ExportExecutionPercentageColumnIndex = 12;
        private const int ExportColumnCount = 13;

        public int TotalCycleCount;

        public RoutinesView() : base(System.Windows.Forms.SelectionMode.One, (ButtonType)(ButtonType.Copy | ButtonType.Export))
        {
            var cellTemplate = new CellTemplate();
            AddColumn("Routine", "Routine", cellTemplate);
            AddColumn("TotalCPU", "Total CPU [#cycles, %]", cellTemplate);
            AddColumn("SelfCPU", "Self CPU [#cycles, %]", cellTemplate);
            AddColumn("ElapsedCPU", "Elapsed CPU [#cycles, %]", cellTemplate);
            AddColumn("Interrupts", "Interrupts [#cycles, %]", cellTemplate);
            AddColumn("ExecutionCount", "Execution count [#, %]", cellTemplate);

            SetColumnAlignment(TotalCPUColumnIndex, DataGridViewContentAlignment.MiddleRight);
            SetColumnAlignment(SelfCPUColumnIndex, DataGridViewContentAlignment.MiddleRight);
            SetColumnAlignment(ElapsedCPUColumnIndex, DataGridViewContentAlignment.MiddleRight);
            SetColumnAlignment(InterruptsColumnIndex, DataGridViewContentAlignment.MiddleRight);
            SetColumnAlignment(ExecutionCountColumnIndex, DataGridViewContentAlignment.MiddleRight);

            SetColumnHeaderToolTip(RoutineColumnIndex, "Routine address and label");
            SetColumnHeaderToolTip(TotalCPUColumnIndex, "Total cycles used by this routine and all routines it calls.");
            SetColumnHeaderToolTip(SelfCPUColumnIndex,"Cycles used just by this routine");
            SetColumnHeaderToolTip(ElapsedCPUColumnIndex, "Total cycles elapsed during execution of routine, including cycles in interrupts");
            SetColumnHeaderToolTip(InterruptsColumnIndex, "Total cycles spent servicing interrupts during execution of routine");
            SetColumnHeaderToolTip(ExecutionCountColumnIndex, "Number of times this routine was executed");
        }

        public void SetRoutines(List<Routine> routines)
        {
            using var token = _ReentrancyGuard.TryEnter();
            if (token == null) return;

            SetRoutinesInternal(routines);
        }

        public void SelectRoutine(Routine routine)
        {
            using var token = _ReentrancyGuard.TryEnter();
            if (token == null) return;

            SelectRoutineInternal(routine);
        }

        public void ShowHotRoutines()
        {
            using var token = _ReentrancyGuard.TryEnter();
            if (token == null) return;

            ShowHotRoutinesInternal();
        }

        private void SetRoutinesInternal(List<Routine> routines)
        {
            SetRowsData(routines);

            _MaxExecutionCount = 0;
            foreach (var routine in routines)
                if (_MaxExecutionCount < routine.AggregateMetrics.ExecutionCount)
                    _MaxExecutionCount = routine.AggregateMetrics.ExecutionCount;

            SortColumn(SelfCPUColumnIndex, SortOrder.Descending);
        }

        private void SelectRoutineInternal(Routine routine)
        {
            SelectRow(routine, scrollIntoView: true);
        }

        private void ShowHotRoutinesInternal()
        {
            SortColumn(SelfCPUColumnIndex, SortOrder.Descending);
            ScrollToTop();
        }

        protected override void OnSelectionChange(object? sender, Routine? routine)
        {
            using var token = _ReentrancyGuard.TryEnter();
            if (token == null) return;

            var form = FindForm() as BeebPerfForm;
            if (form is null) return;

            if (routine != null)
                form.SetSelectedRoutine(routine, callStack: null, memoryAccess: null);
            else
                form.ClearSelectedRoutine();
        }

        protected override int OnSortCompare(Routine a, Routine b, int columnIndex)
        {
            var metrics_a = a.AggregateMetrics;
            var metrics_b = b.AggregateMetrics;

            return columnIndex switch
            {
                RoutineColumnIndex => a.StartAddress.Address.CompareTo(b.StartAddress.Address),
                SelfCPUColumnIndex => metrics_a.SelfCycleCount.CompareTo(metrics_b.SelfCycleCount),
                TotalCPUColumnIndex => metrics_a.InclusiveCycleCount.CompareTo(metrics_b.InclusiveCycleCount),
                ElapsedCPUColumnIndex => metrics_a.ElapsedCycleCount.CompareTo(metrics_b.ElapsedCycleCount),
                InterruptsColumnIndex => (metrics_a.ElapsedCycleCount - metrics_a.InclusiveCycleCount).CompareTo(
                                         (metrics_b.ElapsedCycleCount - metrics_b.InclusiveCycleCount)),
                ExecutionCountColumnIndex => metrics_a.ExecutionCount.CompareTo(metrics_b.ExecutionCount),
                _ => 0
            };
        }

        protected override string OnFormatRowData(Routine routine, int columnIndex, int rowIndex)
        {
            if (columnIndex == RoutineColumnIndex)
                return $"{"".PadLeft(routine.HotRoutine ? 4 : 0)}{FormatAddress(routine.StartAddress)} {routine.Label}";
            else
                return FormatCountAndRange(routine, columnIndex);
        }

        protected override (int value, int range) OnRowDataCountAndRange(Routine routine, int columnIndex)
        {
            var metrics = routine.AggregateMetrics;
            return columnIndex switch
            {
                SelfCPUColumnIndex => (value: metrics.SelfCycleCount, range: TotalCycleCount),
                TotalCPUColumnIndex => (value: metrics.InclusiveCycleCount, range: TotalCycleCount),
                ElapsedCPUColumnIndex => (value: metrics.ElapsedCycleCount, range: TotalCycleCount),
                InterruptsColumnIndex => (value: metrics.ElapsedCycleCount - metrics.InclusiveCycleCount, range: TotalCycleCount),
                ExecutionCountColumnIndex => (value: metrics.ExecutionCount, range: _MaxExecutionCount),
                _ => (value: -1, range: 1)
            };
        }

        override protected void OnCopyButtonClick()
        {
            var form = FindForm() as BeebPerfForm;
            if (form == null) return;

            Exporter.CopyToClipboard(form, this);
        }

        override protected void OnExportButtonClick()
        {
            var form = FindForm() as BeebPerfForm;
            if (form == null) return;

            Exporter.ExportCSVFile(form, this);
        }

        string[] IGridExporter.GetHeaders()
        {
            string[] headers = [
                "Routine address",
                "Routine page",
                "Routine label",
                "Total CPU [#]",
                "Total CPU [%]",
                "Self CPU [#]",
                "Self CPU [%]",
                "Elapsed CPU [#]",
                "Elapsed CPU [%]",
                "Interrupts [#]",
                "Interrupts [%]",
                "Execution count [#]",
                "Execution count [%]"
            ];

            Debug.Assert(headers.Length == ExportColumnCount);
            return headers;
        }

        int IGridExporter.GetRowCount()
        {
            return _DataRows.Count;
        }

        string[] IGridExporter.GetRowValues(int rowIndex)
        {
            List<string> rowValues = new();

            var rowData = _DataRows[rowIndex];

            for (int columnIndex = 0; columnIndex < ExportColumnCount; columnIndex++)
                rowValues.Add(FormatExportCell(rowData, columnIndex));

            return rowValues.ToArray();
        }

        private string FormatExportCell(Routine routine, int columnIndex)
        {
            var metrics = routine.AggregateMetrics;
            return columnIndex switch
            {
                ExportRoutineAddressColumnIndex => FormatAddress(routine.StartAddress),
                ExportRoutinePageColumnIndex => routine.StartAddress.Page.ToString(),
                ExportRoutineLabelColumnIndex => routine.Label,
                ExportTotalCPUColumnIndex => metrics.InclusiveCycleCount.ToString(),
                ExportTotalCPUPercentageIndex => FormatExportPercentage(metrics.InclusiveCycleCount, TotalCycleCount),
                ExportSelfCPUColumnIndex => metrics.SelfCycleCount.ToString(),
                ExportSelfCPUPercentageIndex => FormatExportPercentage(metrics.SelfCycleCount, TotalCycleCount),
                ExportElapsedCPUColumnIndex => metrics.ElapsedCycleCount.ToString(),
                ExportElapsedCPUPercentageIndex => FormatExportPercentage(metrics.ElapsedCycleCount, TotalCycleCount),
                ExportInterruptsColumnIndex => (metrics.ElapsedCycleCount - metrics.InclusiveCycleCount).ToString(),
                ExportInterruptsPercentageIndex => FormatExportPercentage(metrics.ElapsedCycleCount - metrics.InclusiveCycleCount, TotalCycleCount),
                ExportExecutionCountColumnIndex => metrics.ExecutionCount.ToString(),
                ExportExecutionPercentageColumnIndex => FormatExportPercentage(metrics.ExecutionCount, _MaxExecutionCount),
                _ => string.Empty
            };
        }

        public class CellTemplate : GridViewCellTemplate
        {
            protected override void Paint(
                Graphics graphics,
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
                // paint default
                base.Paint(
                    graphics, clipBounds, cellBounds, rowIndex, cellState,
                    value, formattedValue, errorText,
                    cellStyle, advancedBorderStyle, paintParts);

                // paint flame icon
                var routinesView = (RoutinesView)DataGridView!;
                var routine = routinesView._DataRows[rowIndex];
                if (ColumnIndex == RoutineColumnIndex && routine.HotRoutine)
                {
                    int size = cellBounds.Height / 2;
                    int inset = cellBounds.Height / 4;
                    var rect = new Rectangle(cellBounds.Left + inset / 2, cellBounds.Top + inset, size, size);

                    var form = routinesView.FindForm() as BeebPerfForm;
                    if (form is null) return;
                    
                    graphics.DrawImage(form.FlameImage, rect);
                }
            }
        }

        private int _MaxExecutionCount;
        private ReentrancyGuard _ReentrancyGuard = new();
    }
}
