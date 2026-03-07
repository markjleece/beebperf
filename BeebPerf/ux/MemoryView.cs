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
            AddColumn("Address", "Address", cellTemplate: null);
            AddColumn("Page", "Page", cellTemplate: null);
            AddColumn("Label", "Label", cellTemplate: null);
            AddColumn("ReadWriteCount", "Reads/Writes [#, %]", cellTemplate: null);
            AddColumn("ReadCount", "Reads [#, %]", cellTemplate: null);
            AddColumn("WriteCount", "Writes [#, %]", cellTemplate: null);

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

        public void SetMemoryAccesses(List<MemoryAccess> memoryAccesses, LabelResolver labelResolver)
        {
            _LabelResolver = labelResolver;

            using var token = _ReentrancyGuard.TryEnter();
            if (token == null) return;

            SetMemoryAccessesInternal(memoryAccesses);
        }

        public void SelectMemoryAddress(CanonicalAddress address, LabelResolver labelResolver)
        {
            _LabelResolver = labelResolver;

            using var token = _ReentrancyGuard.TryEnter();
            if (token == null) return;

            SelectMemoryAddressInternal(address);
        }

        protected override void OnSelectionChange(object? sender, MemoryAccess? selectedMemoryAccess)
        {
            using var token = _ReentrancyGuard.TryEnter();
            if (token == null) return;

            var form = FindForm() as BeebPerfForm;
            if (form is null) return;

            if (selectedMemoryAccess != null)
                form.SetSelectedMemoryAddress(selectedMemoryAccess.Address);
            else
                form.ClearSelectedMemoryAddress();
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
                LabelColumnIndex => _LabelResolver.ResolveWithOffset(memoryAccess.Address.Address),
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

        private void SetMemoryAccessesInternal(List<MemoryAccess> memoryAccesses)
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

        private void SelectMemoryAddressInternal(CanonicalAddress address)
        {
            foreach (var memoryAccess in _DataRows)
            {
                if (memoryAccess.Address.Equals(address))
                {
                    SelectRow(memoryAccess);
                    break;
                }
            }
        }

        private int _TotalReadCount;
        private int _TotalWriteCount;
        private ReentrancyGuard _ReentrancyGuard = new();
        private LabelResolver _LabelResolver = new();
    }
}
