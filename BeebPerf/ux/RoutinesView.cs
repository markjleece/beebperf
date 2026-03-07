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
    internal class RoutinesView : GridView<Routine>
    {
        private const int RoutineColumnIndex = 0;
        private const int SelfCPUColumnIndex = 1;
        private const int TotalCPUColumnIndex = 2;
        private const int ElapsedCPUColumnIndex = 3;
        private const int InterruptsColumnIndex = 4;
        private const int ExecutionCountColumnIndex = 5;

        public int TotalCycleCount;

        public RoutinesView() : base(
            DataGridViewAutoSizeColumnsMode.DisplayedCells, 
            System.Windows.Forms.SelectionMode.One)
        {
            var cellTemplate = new CellTemplate();
            AddColumn("Routine", "Routine", cellTemplate);
            AddColumn("SelfCPU", "Self CPU [#cycles, %]", cellTemplate);
            AddColumn("TotalCPU", "Total CPU [#cycles, %]", cellTemplate);
            AddColumn("ElapsedCPU", "Elapsed CPU [#cycles, %]", cellTemplate);
            AddColumn("Interrupts", "Interrupts [#cycles, %]", cellTemplate);
            AddColumn("ExecutionCount", "Execution count", cellTemplate);

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
            SelectRow(routine);
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
            return columnIndex switch
            {
                RoutineColumnIndex => a.StartAddress.Address.CompareTo(b.StartAddress.Address),
                SelfCPUColumnIndex => a.AggregateMetrics.SelfCycleCount.CompareTo(b.AggregateMetrics.SelfCycleCount),
                TotalCPUColumnIndex => a.AggregateMetrics.InclusiveCycleCount.CompareTo(b.AggregateMetrics.InclusiveCycleCount),
                ElapsedCPUColumnIndex => a.AggregateMetrics.ElapsedCycleCount.CompareTo(b.AggregateMetrics.ElapsedCycleCount),
                InterruptsColumnIndex => (a.AggregateMetrics.ElapsedCycleCount - a.AggregateMetrics.InclusiveCycleCount).CompareTo(
                                         (b.AggregateMetrics.ElapsedCycleCount - b.AggregateMetrics.InclusiveCycleCount)),
                ExecutionCountColumnIndex => a.AggregateMetrics.ExecutionCount.CompareTo(b.AggregateMetrics.ExecutionCount),
                _ => 0
            };
        }

        protected override string OnFormatRowData(Routine routine, int columnIndex, int rowIndex)
        {
            return columnIndex switch
            {
                RoutineColumnIndex => $"{"".PadLeft(routine.HotRoutine ? 4 : 0)}{routine.StartAddress} {routine.Label}",
                ExecutionCountColumnIndex => $"{routine.AggregateMetrics.ExecutionCount:N0}",
                _ => FormatCountAndRange(routine, columnIndex),
            };
        }

        protected override (int value, int range) OnRowDataCountAndRange(Routine routine, int columnIndex)
        {
            return columnIndex switch
            {
                SelfCPUColumnIndex => (value: routine.AggregateMetrics.SelfCycleCount, range: TotalCycleCount),
                TotalCPUColumnIndex => (value: routine.AggregateMetrics.InclusiveCycleCount, range: TotalCycleCount),
                ElapsedCPUColumnIndex => (value: routine.AggregateMetrics.ElapsedCycleCount, range: TotalCycleCount),
                InterruptsColumnIndex => (value: routine.AggregateMetrics.ElapsedCycleCount - routine.AggregateMetrics.InclusiveCycleCount, range: routine.AggregateMetrics.ElapsedCycleCount),
                ExecutionCountColumnIndex => (value: routine.AggregateMetrics.ExecutionCount, range: _MaxExecutionCount),
                _ => (value: -1, range: 1)
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
