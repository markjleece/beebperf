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

using System.Diagnostics;

namespace BeebPerf.ux
{
    internal class GridView<ROW_DATA_TYPE> : DataGridView 
        where ROW_DATA_TYPE : class
    {
        public GridView(
            DataGridViewAutoSizeColumnsMode autoSizeMode, 
            SelectionMode selectionMode) : base()
        {
            AllowUserToAddRows = false;
            AllowUserToDeleteRows = false;
            AllowUserToResizeColumns = false;
            AllowUserToResizeRows = false;
            AutoGenerateColumns = false;
            AutoSizeColumnsMode = autoSizeMode;
            BackgroundColor = DefaultCellStyle.BackColor;
            CellBorderStyle = DataGridViewCellBorderStyle.None;
            CellValueNeeded += CellValueNeededFunc;
            CellEnter += CellEnterFunc;

            MultiSelect = false;
            ReadOnly = true;
            RowHeadersVisible = false;
            RowTemplate.DefaultCellStyle.NullValue = null;
            SelectionChanged += SelectionChangedFunc;
            SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            VirtualMode = true;

            _SelectionMode = selectionMode;
            _ScrollTimer = new System.Windows.Forms.Timer { Interval = 500 };
            _ScrollTimer.Tick += ScrollTimerTickFunc;
            Scroll += ScrollFunc;
        }

        public void SetRowsData(List<ROW_DATA_TYPE> rowsData)
        {
            RowCount = 0;

            _DataRows = rowsData;

            foreach (DataGridViewColumn column in Columns)
                column.MinimumWidth = 2;

            _SuppressSelectionChangedEvent++;
            RowCount = rowsData.Count;
            _SuppressSelectionChangedEvent--;

            ClearSelection();
            Invalidate();
        }

        public void Clear()
        {
            _DataRows = [];
            _SelectedDataRow = null;
            RowCount = 0;
            Invalidate();
        }

        public void SelectRow(ROW_DATA_TYPE rowData)
        {
            for (int index = 0; index < _DataRows.Count; index++)
            {
                if (!rowData.Equals(_DataRows[index]))
                    continue;

                _SuppressSelectionChangedEvent++;
                Rows[index].Selected = true;
                _SuppressSelectionChangedEvent--;

                FirstDisplayedScrollingRowIndex = Math.Clamp(index - DisplayedRowCount(false) + 1, 0, Rows.Count - 1);
                return;
            }

            ClearSelection();
        }

        public new void ClearSelection()
        {
            _SelectedDataRow = null;

            _SuppressSelectionChangedEvent++;
            base.ClearSelection();
            _SuppressSelectionChangedEvent--;
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

        protected virtual void OnSelectionChange(object? sender, ROW_DATA_TYPE? rowData)
        {
        }

        protected virtual int OnSortCompare(ROW_DATA_TYPE a, ROW_DATA_TYPE b, int columnIndex)
        {
            return 0;
        }

        protected virtual string OnFormatRowData(ROW_DATA_TYPE rowData, int columnIndex, int rowIndex)
        {
            return string.Empty;
        }

        protected virtual (int value, int range) OnRowDataCountAndRange(ROW_DATA_TYPE rowData, int columnIndex)
        {
            return (value: -1, range: 1);
        }

        protected void SetCellTemplate(GridViewCellTemplate cellTemplate)
        {
            Debug.Assert(Columns.Count > 0);
            foreach (DataGridViewColumn column in Columns)
                column.CellTemplate = cellTemplate;
        }

        protected void SetColumnAlignment(int columnIndex, DataGridViewContentAlignment alignment)
        {
            Columns[columnIndex]!.DefaultCellStyle.Alignment = alignment;
        }

        protected void SetColumnSortMode(int columnIndex, DataGridViewColumnSortMode sortMode)
        {
            Columns[columnIndex].SortMode = sortMode;
        }

        protected void SetColumnHeaderToolTip(int columnIndex, string text)
        {
            Columns[columnIndex].HeaderCell.ToolTipText = text;
        }

        protected void SetColumnVisibility(int columnIndex, bool visibility)
        {
            Columns[columnIndex].Visible = visibility;
        }

        protected void SetColumnHeaderText(int columnIndex, string text)
        {
            Columns[columnIndex].HeaderText = text;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            _SuppressSelectionChangedEvent++;
            base.OnHandleCreated(e);

            BeginInvoke(new MethodInvoker(() =>
            {
                if (_SelectedDataRow != null)
                    SelectRow(_SelectedDataRow);
                else
                    ClearSelection();
                _SuppressSelectionChangedEvent--;
            }));
        }

        private void SelectionChangedFunc(object? sender, EventArgs e)
        {
            if (_SuppressSelectionChangedEvent > 0)
                return;

            if (_SelectionMode == System.Windows.Forms.SelectionMode.None)
            {
                ClearSelection();
                return;
            }

            var form = (BeebPerfForm)GetParentForm();
            if (SelectedRows.Count == 1)
            {
                var value = _DataRows[SelectedRows[0].Index];
                _SelectedDataRow = value;
                OnSelectionChange(sender, value);
            }
            else
            {
                _SelectedDataRow = null;
                OnSelectionChange(sender, null);
            }
        }

        protected override void OnColumnHeaderMouseClick(DataGridViewCellMouseEventArgs e)
        {
            base.OnColumnHeaderMouseClick(e);

            bool descending = Columns[e.ColumnIndex].HeaderCell.SortGlyphDirection != SortOrder.Descending;
            SortColumn(e.ColumnIndex, descending ? SortOrder.Descending : SortOrder.Ascending);
        }

        protected Form GetParentForm()
        {
            Control control = this;
            while (control is not Form)
                control = control!.Parent!;
            return (Form)control;
        }

        protected void SortColumn(int columnIndex, SortOrder sortOrder)
        {
            if (Rows.Count == 0 || Columns[columnIndex].SortMode == DataGridViewColumnSortMode.NotSortable)
                return;

            if (sortOrder == SortOrder.Ascending)
                _DataRows.Sort((a, b) => OnSortCompare(a, b, columnIndex));
            else if (sortOrder == SortOrder.Descending)
                _DataRows.Sort((a, b) => OnSortCompare(b, a, columnIndex));

            foreach (DataGridViewColumn column in Columns)
                column.HeaderCell.SortGlyphDirection = (columnIndex == column.Index) ? sortOrder : SortOrder.None;

            if (_SelectedDataRow != null)
            {
                SelectRow(_SelectedDataRow);
            }
            else
            {
                ClearSelection();
                ScrollToTop();
            }

            AutoGrowColumns();
        }

        protected void AutoGrowColumns()
        {
            foreach (DataGridViewColumn column in Columns)
                column.MinimumWidth = column.Width;
            AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            Invalidate();
        }

        private void CellEnterFunc(object? sender, DataGridViewCellEventArgs e)
        {
            if (_SelectionMode == System.Windows.Forms.SelectionMode.None)
                ClearSelection();
        }

        private void ScrollFunc(object? sender, ScrollEventArgs e)
        {
            _ScrollTimer.Stop();
            _ScrollTimer.Start();
        }

        private void ScrollTimerTickFunc(object? sender, EventArgs e)
        {
            _ScrollTimer.Stop();
            foreach (DataGridViewColumn column in Columns)
                column.MinimumWidth = column.Width;
            AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        }

        private void CellValueNeededFunc(object? sender, DataGridViewCellValueEventArgs e)
        {
            if (e.RowIndex >= _DataRows.Count)
                return;

            e.Value = OnFormatRowData(_DataRows[e.RowIndex], e.ColumnIndex, e.RowIndex);
        }

        protected string FormatCountAndRange(int value, int range)
        {
            var percentage = double.Min(100.0 * value / range, 100.0);
            if (percentage >= 0)
                return $"{value:N0} ({percentage:F2}%)";
            else
                return string.Empty;
        }

        protected string FormatCountAndRange(ROW_DATA_TYPE rowData, int columnIndex)
        {
            var countAndRange = OnRowDataCountAndRange(rowData, columnIndex);
            return FormatCountAndRange(countAndRange.value, countAndRange.range);
        }

        protected string FormatLabel(ushort address, bool withOffsets)
        {
            int index = _LabelAddresses.BinarySearch(address);
            if (index >= 0)
                return _Labels[address];

            index = ~index - 1;
            if (index >= 0 && withOffsets)
            {
                ushort lowerAddress = _LabelAddresses[index];
                int offset = address - lowerAddress;
                if (offset < 0x100)
                    return $"{_Labels[lowerAddress]}+{offset}";
            }

            return string.Empty;
        }

        protected void ScrollToTop()
        {
            if (Rows.Count > 0)
                FirstDisplayedScrollingRowIndex = 0;
            Invalidate();
        }
        protected void IncrementRowCount()
        {
            _SuppressSelectionChangedEvent++;
            RowCount++;
            _SuppressSelectionChangedEvent--;
        }

        protected void DecrementRowCount()
        {
            _SuppressSelectionChangedEvent++;
            RowCount--;
            _SuppressSelectionChangedEvent--;
        }

        public class GridViewCellTemplate : DataGridViewTextBoxCell
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
                var gridView = (GridView<ROW_DATA_TYPE>)DataGridView!;

                // paint default background
                var backPaintParts = paintParts &
                    (DataGridViewPaintParts.Border |
                     DataGridViewPaintParts.Background |
                     DataGridViewPaintParts.SelectionBackground);
                if (backPaintParts != DataGridViewPaintParts.None)
                    base.Paint(
                        graphics, clipBounds, cellBounds, rowIndex, cellState,
                        value, formattedValue, errorText,
                        cellStyle, advancedBorderStyle, backPaintParts);

                // paint content background
                var contentBackgroundPaintParts = paintParts & DataGridViewPaintParts.ContentBackground;
                if (contentBackgroundPaintParts != DataGridViewPaintParts.None)
                    DrawBar(rowIndex, graphics, cellBounds, gridView.DefaultCellStyle.BackColor, cellState);

                // draw foreground
                var contentForegroundPaintParts = paintParts & DataGridViewPaintParts.ContentForeground;
                if (contentForegroundPaintParts != DataGridViewPaintParts.None)
                    base.Paint(
                        graphics, clipBounds, cellBounds, rowIndex, cellState,
                        value, formattedValue, errorText,
                        cellStyle, advancedBorderStyle, contentForegroundPaintParts);
            }

            protected void DrawBar(
                int rowIndex,
                Graphics graphics,
                Rectangle cellBounds,
                Color backColor,
                DataGridViewElementStates cellState)
            {
                var gridView = (GridView<ROW_DATA_TYPE>)DataGridView!;
                var dataRow = gridView._DataRows[rowIndex];

                var countAndRange = gridView.OnRowDataCountAndRange(dataRow, ColumnIndex);
                double ratio = (double)countAndRange.value / countAndRange.range;

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
                    backColor,
                    gridView.DefaultCellStyle.SelectionBackColor,
                    selected ? 0.75 : 0.25);

                using var brush = new SolidBrush(color);
                graphics.FillRectangle(brush, rect);
            }

            protected Color Blend(Color first, Color second, double ratio)
            {
                int r = (int)(first.R * (1 - ratio) + second.R * ratio);
                int g = (int)(first.G * (1 - ratio) + second.G * ratio);
                int b = (int)(first.B * (1 - ratio) + second.B * ratio);
                return Color.FromArgb(r, g, b);
            }
        }

        protected List<ROW_DATA_TYPE> _DataRows = [];
        protected ROW_DATA_TYPE? _SelectedDataRow;
        private int _SuppressSelectionChangedEvent;
        private Dictionary<ushort, string> _Labels = new();
        private List<ushort> _LabelAddresses = new();
        private System.Windows.Forms.Timer _ScrollTimer;
        private SelectionMode _SelectionMode;
    }
}
