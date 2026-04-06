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
    internal class MemoryView : GridView<MemoryAccess>, IGridExporter
    {
        private const int PageColumnIndex = 0;
        private const int AddressColumnIndex = 1;
        private const int LabelColumnIndex = 2;
        private const int ReadWriteCountColumnIndex = 3;
        private const int ReadCountColumnIndex = 4;
        private const int WriteCountColumnIndex = 5;

        private const int ExportPageColumnIndex = 0;
        private const int ExportAddressColumnIndex = 1;
        private const int ExportLabelColumnIndex = 2;
        private const int ExportReadWriteCountColumnIndex = 3;
        private const int ExportReadWritePercentageColumnIndex = 4;
        private const int ExportReadCountColumnIndex = 5;
        private const int ExportReadPercentageColumnIndex = 6;
        private const int ExportWriteCountColumnIndex = 7;
        private const int ExportWritePercentageColumnIndex = 8;
        private const int ExportColumnCount = 9;

        public MemoryView() : base(System.Windows.Forms.SelectionMode.One, (ButtonType)(ButtonType.Copy | ButtonType.Export))
        {
            AddColumn("Page", "Page", cellTemplate: null);
            AddColumn("Address", "Address", cellTemplate: null);
            AddColumn("Label", "Label", cellTemplate: null);
            AddColumn("ReadWriteCount", "Reads/Writes [#, %]", cellTemplate: null);
            AddColumn("ReadCount", "Reads [#, %]", cellTemplate: null);
            AddColumn("WriteCount", "Writes [#, %]", cellTemplate: null);

            SetColumnAlignment(AddressColumnIndex, DataGridViewContentAlignment.MiddleCenter);
            SetColumnAlignment(ReadWriteCountColumnIndex, DataGridViewContentAlignment.MiddleRight);
            SetColumnAlignment(ReadCountColumnIndex, DataGridViewContentAlignment.MiddleRight);
            SetColumnAlignment(WriteCountColumnIndex, DataGridViewContentAlignment.MiddleRight);

            SetColumnHeaderToolTip(PageColumnIndex, "Page");
            SetColumnHeaderToolTip(AddressColumnIndex, "Address");
            SetColumnHeaderToolTip(LabelColumnIndex, "Label");
            SetColumnHeaderToolTip(ReadWriteCountColumnIndex, "Total number of memory reads or writes");
            SetColumnHeaderToolTip(ReadCountColumnIndex, "Total number of memory reads");
            SetColumnHeaderToolTip(WriteCountColumnIndex, "Total number of memory writes");

            SetColumnSortMode(PageColumnIndex, DataGridViewColumnSortMode.NotSortable);
            SetColumnSortMode(AddressColumnIndex, DataGridViewColumnSortMode.Programmatic);
            SetColumnSortMode(LabelColumnIndex, DataGridViewColumnSortMode.Programmatic);
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
            var result = columnIndex switch
            {
                AddressColumnIndex => a.Address.CompareTo(b.Address),
                LabelColumnIndex => a.Label.Trim('.').CompareTo(b.Label.Trim('.')),
                ReadWriteCountColumnIndex => (a.ReadCount + a.WriteCount).CompareTo(b.ReadCount + b.WriteCount),
                ReadCountColumnIndex => a.ReadCount.CompareTo(b.ReadCount),
                WriteCountColumnIndex => a.WriteCount.CompareTo(b.WriteCount),
                _ => 0
            };

            if (result == 0 && columnIndex != AddressColumnIndex)
                result = a.Address.CompareTo(b.Address);

            return result;
        }

        protected override string OnFormatRowData(MemoryAccess memoryAccess, int columnIndex, int rowIndex)
        {
            return columnIndex switch
            {
                PageColumnIndex => memoryAccess.Address.Page.ToString(),
                AddressColumnIndex => FormatAddress(memoryAccess.Address),
                LabelColumnIndex => _LabelResolver.ResolveWithOffset(memoryAccess.Address.Address),
                _ => FormatCountAndRange(memoryAccess, columnIndex),
            };
        }

        protected override (int value, int range, bool clamp) OnRowDataCountAndRange(MemoryAccess memoryAccess, int columnIndex)
        {
            return columnIndex switch
            {
                ReadWriteCountColumnIndex => (value: memoryAccess.ReadCount + memoryAccess.WriteCount, range: _TotalReadCount + _TotalWriteCount, clamp: true),
                ReadCountColumnIndex => (value: memoryAccess.ReadCount, range: _TotalReadCount, clamp: true),
                WriteCountColumnIndex => (value: memoryAccess.WriteCount, range: _TotalWriteCount, clamp: true),
                _ => (value: -1, range: 1, clamp: false)
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
                "Memory page",
                "Memory address",
                "Label",
                "Reads/writes [#]",
                "Reads/writes [%]",
                "Reads [#]",
                "Reads [%]",
                "Writes [#]",
                "Writes [%]"
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

        private string FormatExportCell(MemoryAccess memoryAccess, int columnIndex)
        {
            return columnIndex switch
            {
                ExportPageColumnIndex => memoryAccess.Address.Page.ToString(),
                ExportAddressColumnIndex => FormatAddress(memoryAccess.Address),
                ExportLabelColumnIndex => memoryAccess.Label,
                ExportReadWriteCountColumnIndex => (memoryAccess.ReadCount + memoryAccess.WriteCount).ToString(),
                ExportReadWritePercentageColumnIndex => FormatExportPercentage(memoryAccess.ReadCount + memoryAccess.WriteCount, _TotalReadCount + _TotalWriteCount),
                ExportReadCountColumnIndex => memoryAccess.ReadCount.ToString(),
                ExportReadPercentageColumnIndex => FormatExportPercentage(memoryAccess.ReadCount, _TotalReadCount),
                ExportWriteCountColumnIndex => memoryAccess.WriteCount.ToString(),
                ExportWritePercentageColumnIndex => FormatExportPercentage(memoryAccess.WriteCount, _TotalWriteCount),
                _ => string.Empty
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
                    SelectRow(memoryAccess, scrollIntoView: true);
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
