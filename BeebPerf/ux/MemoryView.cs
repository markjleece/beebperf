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
    internal class MemoryView : DataGridView
    {
        private const int AddressColumnIndex = 0;
        private const int PageColumnIndex = 1;
        private const int LabelColumnIndex = 2;
        private const int ReadWriteCountColumnIndex = 3;
        private const int ReadCountColumnIndex = 4;
        private const int WriteCountColumnIndex = 5;

        public MemoryView() : base()
        {
            AllowUserToAddRows = false;
            AllowUserToDeleteRows = false;
            AllowUserToResizeColumns = false;
            AllowUserToResizeRows = false;
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
            BackgroundColor = DefaultCellStyle.BackColor;
            CellBorderStyle = DataGridViewCellBorderStyle.None;
            CellValueNeeded += CellValueNeededFunc;

            MultiSelect = false;
            ReadOnly = true;
            RowHeadersVisible = false;
            RowTemplate.DefaultCellStyle.NullValue = null;
            SelectionChanged += SelectionChangedFunc;
            SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            SortCompare += SortCompareFunc;
            Sorted += SortedFunc;
            VirtualMode = true;

            AutoGenerateColumns = false;
            Columns.Add("Address", "Address");
            Columns.Add("Page", "Page");
            Columns.Add("Label", "Label");
            Columns.Add("ReadWriteCount", "Reads/Writes [#, %]");
            Columns.Add("ReadCount", "Reads [#, %]");
            Columns.Add("WriteCount", "Writes [#, %]");

            var cellTemplate = new CellTemplate();
            foreach (DataGridViewColumn column in Columns)
                column.CellTemplate = cellTemplate;

            SetColumnAlignment(ReadWriteCountColumnIndex, DataGridViewContentAlignment.MiddleRight);
            SetColumnAlignment(ReadCountColumnIndex, DataGridViewContentAlignment.MiddleRight);
            SetColumnAlignment(WriteCountColumnIndex, DataGridViewContentAlignment.MiddleRight);

            SetColumnHeaderToolTip(AddressColumnIndex, "Address");
            SetColumnHeaderToolTip(PageColumnIndex, "Page");
            SetColumnHeaderToolTip(LabelColumnIndex, "Label");
            SetColumnHeaderToolTip(ReadWriteCountColumnIndex, "Total number of memory reads or writes");
            SetColumnHeaderToolTip(ReadCountColumnIndex, "Total number of memory reads");
            SetColumnHeaderToolTip(WriteCountColumnIndex, "Total number of memory writes");

            SetColumnSortMode(AddressColumnIndex, DataGridViewColumnSortMode.Automatic);
            SetColumnSortMode(PageColumnIndex, DataGridViewColumnSortMode.NotSortable);
            SetColumnSortMode(LabelColumnIndex, DataGridViewColumnSortMode.NotSortable);
            SetColumnSortMode(ReadWriteCountColumnIndex, DataGridViewColumnSortMode.Automatic);
            SetColumnSortMode(ReadCountColumnIndex, DataGridViewColumnSortMode.Automatic);
            SetColumnSortMode(WriteCountColumnIndex, DataGridViewColumnSortMode.Automatic);
        }

        public Dictionary<ushort, string> Labels
        {
            get
            {
                return _Labels;
            }
            set
            {
                _Labels = value;
                _LabelAddresses = value.Keys.ToList();
                _LabelAddresses.Sort();
            }
        }

        private void SetColumnAlignment(int columnIndex, DataGridViewContentAlignment alignment)
        {
            Columns[columnIndex]!.DefaultCellStyle.Alignment = alignment;
        }

        private void SetColumnSortMode(int columnIndex, DataGridViewColumnSortMode sortMode)
        {
            Columns[columnIndex].SortMode = sortMode;
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
                if (_SelectedMemoryAccess != null)
                    SelectAddress(((MemoryAccess)_SelectedMemoryAccess).Address);
                else
                    ClearSelection();
                _SuppressSelectionChangedEvent--;
            }));
        }

        public void SelectAddress(CanonicalAddress address)
        {
            foreach (DataGridViewRow row in Rows)
            {
                var memoryAccess = (MemoryAccess)row.Cells[0].Value!;
                if (!address.Equals(memoryAccess.Address))
                    continue;

                _SelectedMemoryAccess = memoryAccess;

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
            _SelectedMemoryAccess = null;

            _SuppressSelectionChangedEvent++;
            base.ClearSelection();
            _SuppressSelectionChangedEvent--;
        }

        private Form GetParentForm()
        {
            Control control = this;
            while (control is not Form)
                control = control!.Parent!;
            return (Form)control;
        }

        private void SelectionChangedFunc(object? sender, EventArgs e)
        {
            if (_SuppressSelectionChangedEvent > 0)
                return;

            var form = (BeebPerfForm)GetParentForm();
            if (SelectedRows.Count == 1)
            {
                var memoryAccess = _MemoryAccesses[SelectedRows[0].Index];
                _SelectedMemoryAccess = memoryAccess;
                form.SetSelectedMemoryAddress(this, memoryAccess.Address);
            }
            else
            {
                _SelectedMemoryAccess = null;
                form.ClearSelectedMemoryAddress(this);
            }
        }

        protected override void OnColumnHeaderMouseClick(DataGridViewCellMouseEventArgs e)
        {
            base.OnColumnHeaderMouseClick(e);

            if (Columns[e.ColumnIndex].SortMode == DataGridViewColumnSortMode.NotSortable)
                return;

            // sort data
            bool ascending = Columns[e.ColumnIndex].HeaderCell.SortGlyphDirection != SortOrder.Ascending;
            switch (e.ColumnIndex)
            {
                case AddressColumnIndex:
                    if (ascending)
                        _MemoryAccesses.Sort((a, b) => a.Address.CompareTo(b.Address));
                    else
                        _MemoryAccesses.Sort((a, b) => b.Address.CompareTo(a.Address));
                    break;

                case ReadWriteCountColumnIndex:
                    if (ascending)
                        _MemoryAccesses.Sort((a, b) => (a.ReadCount + a.WriteCount).CompareTo(b.ReadCount + b.WriteCount));
                    else
                        _MemoryAccesses.Sort((a, b) => (b.ReadCount + b.WriteCount).CompareTo(a.ReadCount + a.WriteCount));
                    break;

                case ReadCountColumnIndex:
                    if (ascending)
                        _MemoryAccesses.Sort((a, b) => a.ReadCount.CompareTo(b.ReadCount));
                    else
                        _MemoryAccesses.Sort((a, b) => b.ReadCount.CompareTo(a.ReadCount));
                    break;

                case WriteCountColumnIndex:
                    if (ascending)
                        _MemoryAccesses.Sort((a, b) => a.WriteCount.CompareTo(b.WriteCount));
                    else
                        _MemoryAccesses.Sort((a, b) => b.WriteCount.CompareTo(a.WriteCount));
                    break;

                default:
                    break;
            }

            // set glyphs
            int[] sortColumnIndices = [AddressColumnIndex, ReadWriteCountColumnIndex, ReadCountColumnIndex, WriteCountColumnIndex];
            foreach (int columnIndex in sortColumnIndices)
            {
                if (columnIndex == e.ColumnIndex)
                    Columns[columnIndex].HeaderCell.SortGlyphDirection = ascending ? SortOrder.Ascending : SortOrder.Descending;
                else
                    Columns[columnIndex].HeaderCell.SortGlyphDirection = SortOrder.None;
            }

            ScrollToTop();
            Invalidate();
        }

        private void SortedFunc(Object? sender, EventArgs args)
        {
            // ensure selection is correctly reflected after a sort
            if (Rows.Count == 0)
                return;

            if (_SelectedMemoryAccess != null)
            {
                foreach (DataGridViewRow row in Rows)
                {
                    var memoryAccess = (MemoryAccess)row.Cells[0].Value!;
                    if (!_SelectedMemoryAccess.Equals(memoryAccess.Address))
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

        public void SetMemoryAccesses(List<MemoryAccess> memoryAccesses)
        {
            RowCount = 0;

            _MemoryAccesses = memoryAccesses;

            int totalReadCount = 0;
            int totalWriteCount = 0;

            foreach (var memoryAccess in memoryAccesses)
            {
                totalReadCount += memoryAccess.ReadCount;
                totalWriteCount += memoryAccess.WriteCount;
            }

            _TotalReadCount = totalReadCount;
            _TotalWriteCount = totalWriteCount;

            _MemoryAccesses.Sort((a, b) => (b.ReadCount + b.WriteCount).CompareTo(a.ReadCount + a.WriteCount));
            Columns[ReadWriteCountColumnIndex].HeaderCell.SortGlyphDirection = SortOrder.Descending;

            _SuppressSelectionChangedEvent++;
            RowCount = memoryAccesses.Count;
            _SuppressSelectionChangedEvent--;

            ClearSelection();
            Invalidate();
        }

        private void CellValueNeededFunc(object? sender, DataGridViewCellValueEventArgs e)
        {
            if (e.RowIndex >= _MemoryAccesses.Count)
                return;

            var memoryAccess = _MemoryAccesses[e.RowIndex];

            switch (e.ColumnIndex)
            {
                case AddressColumnIndex:
                    e.Value = memoryAccess.Address.ToString();
                    break;

                case PageColumnIndex:
                    e.Value = memoryAccess.Address.Page.ToString();
                    break;

                case LabelColumnIndex:
                    e.Value = FormatLabel(memoryAccess.Address.Address);
                    break;

                case ReadWriteCountColumnIndex:
                    e.Value = FormatCount(memoryAccess.ReadCount + memoryAccess.WriteCount, _TotalReadCount + _TotalWriteCount);
                    break;

                case ReadCountColumnIndex:
                    e.Value = FormatCount(memoryAccess.ReadCount, _TotalReadCount);
                    break;

                case WriteCountColumnIndex:
                    e.Value = FormatCount(memoryAccess.WriteCount, _TotalWriteCount);
                    break;

                default:
                    break;
            }
        }

        public void Clear()
        {
            _MemoryAccesses = [];
            _SelectedMemoryAccess = null;
            RowCount = 0;
            Invalidate();
        }

        private void SortCompareFunc(object? sender, DataGridViewSortCompareEventArgs e)
        {
            var a = (MemoryAccess)e.CellValue1!;
            var b = (MemoryAccess)e.CellValue2!;

            switch (e.Column.Index)
            {
                case AddressColumnIndex:
                    e.SortResult = a.Address.CompareTo(b.Address);
                    e.Handled = true;
                    break;

                case ReadWriteCountColumnIndex:
                    e.SortResult = (a.ReadCount + a.WriteCount) - (b.ReadCount + b.WriteCount);
                    e.Handled = true;
                    break;

                case ReadCountColumnIndex:
                    e.SortResult = a.ReadCount - b.ReadCount;
                    e.Handled = true;
                    break;

                case WriteCountColumnIndex:
                    e.SortResult = a.WriteCount - b.WriteCount;
                    e.Handled = true;
                    break;

                default:
                    break;
            }
        }

        private string FormatCount(int value, int range)
        {
            var percentage = double.Min(100.0 * value / range, 100.0);
            return $"{value:N0} ({percentage:F2}%)";
        }

        private string FormatLabel(ushort address)
        {
            int index = _LabelAddresses.BinarySearch(address);
            if (index >= 0)
                return _Labels[address];
            index = ~index - 1;
            if (index >= 0)
            {
                ushort lowerAddress = _LabelAddresses[index];
                int offset = address - lowerAddress;
                if (offset < 0x100)
                    return $"{_Labels[lowerAddress]}+{offset}";
            }

            return string.Empty;
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
                if (rowIndex >= 0 && (ColumnIndex == ReadWriteCountColumnIndex ||
                                      ColumnIndex == ReadCountColumnIndex ||
                                      ColumnIndex == WriteCountColumnIndex))
                {
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
                    DrawBar(rowIndex, graphics, cellBounds, cellState);

                    // draw default foreground
                    var forePaintParts = paintParts & DataGridViewPaintParts.ContentForeground;
                    base.Paint(
                        graphics, clipBounds, cellBounds, rowIndex, cellState,
                        value, formattedValue, errorText,
                        cellStyle, advancedBorderStyle, forePaintParts);
                }
                else
                {
                    // paint default, minus focus rect
                    base.Paint(
                        graphics, clipBounds, cellBounds, rowIndex, cellState,
                        value, formattedValue, errorText,
                        cellStyle, advancedBorderStyle, paintParts & ~DataGridViewPaintParts.Focus);
                }
            }

            private void DrawBar(
                int rowIndex,
                Graphics graphics, 
                Rectangle cellBounds, 
                DataGridViewElementStates cellState)
            {
                var memoryView = (MemoryView)DataGridView!;
                var memoryAccess = memoryView._MemoryAccesses[rowIndex];

                double ratio = ColumnIndex switch
                {
                    ReadWriteCountColumnIndex => (double)(memoryAccess.ReadCount + memoryAccess.WriteCount) / (memoryView._TotalReadCount + memoryView._TotalWriteCount),
                    ReadCountColumnIndex => (double)memoryAccess.ReadCount / memoryView._TotalReadCount,
                    WriteCountColumnIndex => (double)memoryAccess.WriteCount / memoryView._TotalWriteCount,
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
                    memoryView.DefaultCellStyle.BackColor,
                    memoryView.DefaultCellStyle.SelectionBackColor,
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

        private MemoryAccess? _SelectedMemoryAccess;
        private int _SuppressSelectionChangedEvent;
        List<MemoryAccess> _MemoryAccesses = [];
        private int _TotalReadCount;
        private int _TotalWriteCount;
        private Dictionary<ushort, string> _Labels = new();
        private List<ushort> _LabelAddresses = new();
    }
}
