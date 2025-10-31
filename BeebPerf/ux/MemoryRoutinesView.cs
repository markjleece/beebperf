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
using static BeebPerf.MemoryAnalysis;

namespace BeebPerf.ux
{
    internal class MemoryRoutinesView : GridView<RoutineMemoryAccess>
    {
        private const int RoutineColumnIndex = 0;
        private const int ReadWriteCountColumnIndex = 1;
        private const int ReadCountColumnIndex = 2;
        private const int WriteCountColumnIndex = 3;

        public MemoryRoutinesView() : base(
            DataGridViewAutoSizeColumnsMode.DisplayedCells,
            System.Windows.Forms.SelectionMode.One)
        {
            Columns.Add("Routine", "Routine");
            Columns.Add("ReadWriteCount", "Reads/Writes [#, %]");
            Columns.Add("ReadCount", "Reads [#, %]");
            Columns.Add("WriteCount", "Writes [#, %]");

            SetCellTemplate(new GridViewCellTemplate());

            SetColumnAlignment(ReadWriteCountColumnIndex, DataGridViewContentAlignment.MiddleRight);
            SetColumnAlignment(ReadCountColumnIndex, DataGridViewContentAlignment.MiddleRight);
            SetColumnAlignment(WriteCountColumnIndex, DataGridViewContentAlignment.MiddleRight);

            SetColumnHeaderToolTip(RoutineColumnIndex, "Routine");
            SetColumnHeaderToolTip(ReadWriteCountColumnIndex, "Total number of memory reads or writes");
            SetColumnHeaderToolTip(ReadCountColumnIndex, "Total number of memory reads");
            SetColumnHeaderToolTip(WriteCountColumnIndex, "Total number of memory writes");

            SetColumnSortMode(RoutineColumnIndex, DataGridViewColumnSortMode.NotSortable);
            SetColumnSortMode(ReadWriteCountColumnIndex, DataGridViewColumnSortMode.Automatic);
            SetColumnSortMode(ReadCountColumnIndex, DataGridViewColumnSortMode.Automatic);
            SetColumnSortMode(WriteCountColumnIndex, DataGridViewColumnSortMode.Automatic);
        }

        public void SetMemoryAccesses(List<RoutineMemoryAccess> memoryAccesses)
        {
            SetRowsData(memoryAccesses);

            _TotalReadCount = 0;
            _TotalWriteCount = 0;
            foreach (var memoryAccess in memoryAccesses)
            {
                _TotalReadCount += memoryAccess.ReadCount;
                _TotalWriteCount += memoryAccess.WriteCount;
            }

            SortColumn(ReadWriteCountColumnIndex, SortOrder.Descending);
        }

        public void SelectRoutine(Routine routine)
        {
            using var token = _ReentrancyGuard.TryEnter();
            if (token == null) return;

            SelectRoutineInternal(routine);
        }

        protected override void OnSelectionChange(object? sender, RoutineMemoryAccess? selection)
        {
            using var token = _ReentrancyGuard.TryEnter();
            if (token == null) return;

            var form = (BeebPerfForm)GetParentForm();
            if (selection != null)
                form.SetSelectedRoutine(selection.Routine, callStack: null, selection);
            else
                form.ClearSelectedRoutine();
        }

        protected override int OnSortCompare(RoutineMemoryAccess a, RoutineMemoryAccess b, int columnIndex)
        {
            return columnIndex switch
            {
                ReadWriteCountColumnIndex => (a.ReadCount + a.WriteCount).CompareTo(b.ReadCount + b.WriteCount),
                ReadCountColumnIndex => a.ReadCount.CompareTo(b.ReadCount),
                WriteCountColumnIndex => a.WriteCount.CompareTo(b.WriteCount),
                _ => 0
            };
        }

        protected override string OnFormatRowData(RoutineMemoryAccess memoryAccess, int columnIndex, int rowIndex)
        {
            if (columnIndex == RoutineColumnIndex)
                return $"{memoryAccess.Routine.StartAddress} {memoryAccess.Routine.Label}";
            else
                return FormatCountAndRange(memoryAccess, columnIndex);
        }

        protected override (int value, int range) OnRowDataCountAndRange(RoutineMemoryAccess memoryAccess, int columnIndex)
        {
            return columnIndex switch
            {
                ReadWriteCountColumnIndex => (value: memoryAccess.ReadCount + memoryAccess.WriteCount, range: _TotalReadCount + _TotalWriteCount),
                ReadCountColumnIndex => (value: memoryAccess.ReadCount, range: _TotalReadCount),
                WriteCountColumnIndex => (value: memoryAccess.WriteCount, range: _TotalWriteCount),
                _ => (value: -1, range: 1)
            };
        }
        private void SelectRoutineInternal(Routine routine)
        {
            foreach (var memoryAccess in _DataRows)
            {
                if (!routine.StartAddress.Equals(memoryAccess.Routine.StartAddress))
                    continue;

                SelectRow(memoryAccess);
                return;
            }

            ClearSelection();
        }

        private int _TotalReadCount;
        private int _TotalWriteCount;
        private ReentrancyGuard _ReentrancyGuard = new();
    }
}
