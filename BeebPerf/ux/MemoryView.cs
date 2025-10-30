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

using static BeebPerf.MemoryAnalysis;

namespace BeebPerf.ux
{
    internal class MemoryView : GridView<MemoryAccess>
    {
        private const int AddressColumnIndex = 0;
        private const int PageColumnIndex = 1;
        private const int LabelColumnIndex = 2;
        private const int ReadWriteCountColumnIndex = 3;
        private const int ReadCountColumnIndex = 4;
        private const int WriteCountColumnIndex = 5;

        public MemoryView() : base(
            DataGridViewAutoSizeColumnsMode.DisplayedCells, 
            System.Windows.Forms.SelectionMode.One)
        {
            Columns.Add("Address", "Address");
            Columns.Add("Page", "Page");
            Columns.Add("Label", "Label");
            Columns.Add("ReadWriteCount", "Reads/Writes [#, %]");
            Columns.Add("ReadCount", "Reads [#, %]");
            Columns.Add("WriteCount", "Writes [#, %]");

            SetCellTemplate(new GridViewCellTemplate());

            SetColumnAlignment(ReadWriteCountColumnIndex, DataGridViewContentAlignment.MiddleRight);
            SetColumnAlignment(ReadCountColumnIndex, DataGridViewContentAlignment.MiddleRight);
            SetColumnAlignment(WriteCountColumnIndex, DataGridViewContentAlignment.MiddleRight);

            SetColumnHeaderToolTip(AddressColumnIndex, "Address");
            SetColumnHeaderToolTip(PageColumnIndex, "Page");
            SetColumnHeaderToolTip(LabelColumnIndex, "Label");
            SetColumnHeaderToolTip(ReadWriteCountColumnIndex, "Total number of memory reads or writes");
            SetColumnHeaderToolTip(ReadCountColumnIndex, "Total number of memory reads");
            SetColumnHeaderToolTip(WriteCountColumnIndex, "Total number of memory writes");

            SetColumnSortMode(AddressColumnIndex, DataGridViewColumnSortMode.Programmatic);
            SetColumnSortMode(PageColumnIndex, DataGridViewColumnSortMode.NotSortable);
            SetColumnSortMode(LabelColumnIndex, DataGridViewColumnSortMode.NotSortable);
            SetColumnSortMode(ReadWriteCountColumnIndex, DataGridViewColumnSortMode.Programmatic);
            SetColumnSortMode(ReadCountColumnIndex, DataGridViewColumnSortMode.Programmatic);
            SetColumnSortMode(WriteCountColumnIndex, DataGridViewColumnSortMode.Programmatic);
        }

        public void SetMemoryAccesses(List<MemoryAccess> memoryAccesses)
        {
            SetRowsData(memoryAccesses);

            int totalReadCount = 0;
            int totalWriteCount = 0;

            foreach (var memoryAccess in memoryAccesses)
            {
                totalReadCount += memoryAccess.ReadCount;
                totalWriteCount += memoryAccess.WriteCount;
            }

            _TotalReadCount = totalReadCount;
            _TotalWriteCount = totalWriteCount;

            SortColumn(ReadWriteCountColumnIndex, SortOrder.Descending);
        }

        protected override void OnSelectionChange(object? sender, MemoryAccess? selectedMemoryAccess)
        {
            var form = (BeebPerfForm)GetParentForm();
            if (selectedMemoryAccess != null)
                form.SetSelectedMemoryAddress(this, selectedMemoryAccess.Address);
            else
                form.ClearSelectedMemoryAddress(this);
        }

        protected override int OnSortCompare(MemoryAccess a, MemoryAccess b, int columnIndex)
        {
            return columnIndex switch
            {
                AddressColumnIndex => a.Address.CompareTo(b.Address),
                ReadWriteCountColumnIndex => (a.ReadCount + a.WriteCount).CompareTo(b.ReadCount + b.WriteCount),
                ReadCountColumnIndex => a.ReadCount.CompareTo(b.ReadCount),
                WriteCountColumnIndex => a.WriteCount.CompareTo(b.WriteCount),
                _ => 0
            };
        }

        protected override string OnFormatRowData(MemoryAccess memoryAccess, int columnIndex, int rowIndex)
        {
            return columnIndex switch
            {
                AddressColumnIndex => memoryAccess.Address.ToString(),
                PageColumnIndex => memoryAccess.Address.Page.ToString(),
                LabelColumnIndex => FormatLabel(memoryAccess.Address.Address, withOffsets: true),
                _ => FormatCountAndRange(memoryAccess, columnIndex),
            };
        }

        protected override (int value, int range) OnRowDataCountAndRange(MemoryAccess memoryAccess, int columnIndex)
        {
            return columnIndex switch
            {
                ReadWriteCountColumnIndex => (value: memoryAccess.ReadCount + memoryAccess.WriteCount, range: _TotalReadCount + _TotalWriteCount),
                ReadCountColumnIndex => (value: memoryAccess.ReadCount, range: _TotalReadCount),
                WriteCountColumnIndex => (value: memoryAccess.WriteCount, range: _TotalWriteCount),
                _ => (value: -1, range: 1)
            };
        }

        private int _TotalReadCount;
        private int _TotalWriteCount;
    }
}
