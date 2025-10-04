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
    internal class CallTreeGridView : DataGridView
    {
        private const int RoutineColumnIndex = 0;
        private const int SelfCPUColumnIndex = 1;
        private const int TotalCPUColumnIndex = 2;
        private const int ElapsedCPUColumnIndex = 3;
        private const int ExecutionCountColumnIndex = 4;

        public CallTreeGridView() : base()
        {
            AllowUserToAddRows = false;
            AllowUserToDeleteRows = false;
            AllowUserToResizeColumns = false;
            AllowUserToResizeRows = false;
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            BackgroundColor = DefaultCellStyle.BackColor;
            CellBorderStyle = DataGridViewCellBorderStyle.None;
            CellFormatting += CellFormattingFunc;
            KeyDown += KeyDownFunc;
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

            Columns[RoutineColumnIndex]!.SortMode = DataGridViewColumnSortMode.NotSortable;
            Sort(Columns[TotalCPUColumnIndex]!, System.ComponentModel.ListSortDirection.Descending);
        }

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
                var treeNode = (CallTreeNode)SelectedRows[0].Cells[0].Value!;
                form.SetSelectedRoutine(treeNode.Routine, treeNode.Context);
            }
            else
            {
                form.ClearSelectedRoutine();
            }
        }

        public void Clear()
        {
            Rows.Clear();
        }

        public void AddCallTree(CallTreeNode treeNode)
        {
            Rows.Add(treeNode, treeNode, treeNode, treeNode, treeNode);
            OpenHotPaths(Rows.Count - 1, treeNode);
        }

        public void OpenHotPaths(int rowIndex, CallTreeNode treeNode)
        {
            if (treeNode.HotPath && treeNode.HasChildren)
            {
                bool hotChild = false;
                foreach (var childNode in treeNode.Children)
                    hotChild |= childNode.HotPath;

                if (hotChild)
                {
                    if (treeNode.Expansion == TreeNode<CallTreeNode>.ExpansionType.Closed)
                        OpenTreeNode(rowIndex, treeNode);

                    int childRowIndex = rowIndex + 1;
                    foreach (var childNode in treeNode.Children)
                    {
                        while (Rows[childRowIndex].Cells[0].Value != childNode)
                            childRowIndex++;
                        OpenHotPaths(childRowIndex, childNode);
                    }
                }
            }

            RefreshExecutionCounts();
        }

        public int TotalCycleCount;
        public int MaxExecutionCount;

        private void CellFormattingFunc(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.Value == null)
                return;

            var treeNode = (CallTreeNode)e.Value;

            switch (e.ColumnIndex)
            {
                case RoutineColumnIndex:
                    int padding = treeNode.HotPath ? 4 : 0;
                    e.Value = $"{"".PadLeft(padding)}{treeNode.Routine.StartAddress} {treeNode.Routine.Label}";
                    e.FormattingApplied = true;
                    int indent = treeNode.Context.Depth * Rows[e.RowIndex].Cells[0].Size.Height;
                    e.CellStyle.Padding = new Padding(e.CellStyle.Padding.Left + indent, e.CellStyle.Padding.Top, e.CellStyle.Padding.Right, e.CellStyle.Padding.Bottom);
                    break;

                case SelfCPUColumnIndex:
                    e.Value = FormatCPUMetric(treeNode.CPUMetrics.SelfCycleCount, treeNode.Depth);
                    e.FormattingApplied = true;
                    break;

                case TotalCPUColumnIndex:
                    e.Value = FormatCPUMetric(treeNode.CPUMetrics.InclusiveCycleCount, treeNode.Depth);
                    e.FormattingApplied = true;
                    break;

                case ElapsedCPUColumnIndex:
                    e.Value = FormatCPUMetric(treeNode.CPUMetrics.ElapsedCycleCount, treeNode.Depth);
                    e.FormattingApplied = true;
                    break;

                case ExecutionCountColumnIndex:
                    e.Value = $"{"".PadLeft(2*treeNode.Depth)}{treeNode.CPUMetrics.ExecutionCount:N0}";
                    e.FormattingApplied = true;
                    break;

                default:
                    break;
            }
        }

        private void RefreshExecutionCounts()
        {
            int maxExecutionCount = 0;
            foreach (DataGridViewRow row in Rows)
            {
                var treeNode = (CallTreeNode)row.Cells[ExecutionCountColumnIndex].Value!;
                if (maxExecutionCount < treeNode.CPUMetrics.ExecutionCount)
                    maxExecutionCount = treeNode.CPUMetrics.ExecutionCount;
            }
            MaxExecutionCount = maxExecutionCount;
            InvalidateColumn(ExecutionCountColumnIndex);
        }

        private string FormatCPUMetric(int value, int indent)
        {
            var percentage = (double)value * 100.0 / TotalCycleCount;
            return $"{"".PadLeft(2*indent)}{value:N0} ({percentage:F2}%)";
        }

        private void KeyDownFunc(object? sender, KeyEventArgs e)
        {
            if (SelectedRows.Count != 1)
                return;
            if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Right ||
                e.KeyCode == Keys.Add || e.KeyCode == Keys.Subtract || 
                e.KeyCode == Keys.Space)
            {
                var cell = SelectedRows[0].Cells[0];
                var treeNode = (CallTreeNode)cell.Value!;
                if (treeNode.HasChildren)
                {
                    if ((e.KeyCode == Keys.Right || e.KeyCode == Keys.Add || e.KeyCode == Keys.Space) &&
                        treeNode.Expansion == TreeNode<CallTreeNode>.ExpansionType.Closed)
                    {
                        OpenTreeNode(cell.RowIndex, treeNode);
                    }
                    else if ((e.KeyCode == Keys.Left || e.KeyCode == Keys.Subtract || e.KeyCode == Keys.Space) &&
                        treeNode.Expansion == TreeNode<CallTreeNode>.ExpansionType.Open)
                    {
                        CloseTreeNode(cell.RowIndex, treeNode);
                    }
                }
                e.Handled = true;
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            HitTestInfo hti = HitTest(e.X, e.Y);
            if (e.Button == MouseButtons.Left &&
                hti.Type == DataGridViewHitTestType.Cell &&
                hti.ColumnIndex == 0 &&
                hti.RowIndex >= 0)
            {
                var cell = Rows[hti.RowIndex].Cells[hti.ColumnIndex];
                var treeNode = (CallTreeNode)cell.Value!;
                if (treeNode.HasChildren)
                {
                    int cellX = e.X - hti.ColumnX;
                    if ((cellX >= treeNode.Depth * cell.Size.Height) &&
                        (cellX <= (treeNode.Depth + 1) * cell.Size.Height))
                    {
                        if (treeNode.Expansion == TreeNode<CallTreeNode>.ExpansionType.Closed)
                        {
                            OpenTreeNode(hti.RowIndex, treeNode);
                        }
                        else if (treeNode.Expansion == TreeNode<CallTreeNode>.ExpansionType.Open)
                        {
                            CloseTreeNode(hti.RowIndex, treeNode);
                        }
                        return; // we don't want the selection to change)
                    }
                }
            }
            base.OnMouseDown(e);
        }

        private void OpenTreeNode(int rowIndex, CallTreeNode treeNode)
        {
            foreach (DataGridViewColumn column in Columns)
            {
                SortOrder sortOrder = column.HeaderCell.SortGlyphDirection;
                if (sortOrder != SortOrder.None)
                {
                    CallTreeNode.SortField sortField = column.Index switch
                    {
                        SelfCPUColumnIndex => CallTreeNode.SortField.SelfCPU,
                        TotalCPUColumnIndex => CallTreeNode.SortField.InclusiveCPU,
                        ElapsedCPUColumnIndex => CallTreeNode.SortField.ElapsedCPU,
                        ExecutionCountColumnIndex => CallTreeNode.SortField.Count,
                        _ => 0
                    };
                    treeNode.Sort(sortField, sortOrder);
                    break;
                }
            }

            treeNode.Expansion = TreeNode<CallTreeNode>.ExpansionType.Open;
            AddChildRows(rowIndex + 1, treeNode);
            RefreshExecutionCounts();
        }

        private void CloseTreeNode(int rowIndex, CallTreeNode treeNode)
        {
            RemoveChildRows(rowIndex + 1, treeNode);
            treeNode.Expansion = TreeNode<CallTreeNode>.ExpansionType.Closed;
            RefreshExecutionCounts();
        }

        private int AddChildRows(int index, CallTreeNode treeNode)
        {
            if (treeNode.HasChildren && treeNode.Expansion == TreeNode<CallTreeNode>.ExpansionType.Open)
            {
                foreach (var childNode in treeNode.Children)
                {
                    Rows.Insert(index++, childNode, childNode, childNode, childNode, childNode);
                    index = AddChildRows(index, childNode);
                }
            }
            return index;
        }

        private void RemoveChildRows(int index, CallTreeNode treeNode)
        {
            if (treeNode.HasChildren && treeNode.Expansion == TreeNode<CallTreeNode>.ExpansionType.Open)
            {
                foreach (var childNode in treeNode.Children)
                {
                    Rows.RemoveAt(index);
                    RemoveChildRows(index, childNode);
                }
            }
        }

        private Stack<int> GetSortKey(int columnIndex, CallTreeNode treeNode)
        {
            var sortKey = new Stack<int>();

            for (var node = treeNode; node != null; node = node.Parent)
            {
                var cpuMetrics = treeNode.CPUMetrics;
                var metric = columnIndex switch
                {
                    SelfCPUColumnIndex => cpuMetrics.SelfCycleCount,
                    TotalCPUColumnIndex => cpuMetrics.InclusiveCycleCount,
                    ElapsedCPUColumnIndex => cpuMetrics.ElapsedCycleCount,
                    ExecutionCountColumnIndex => cpuMetrics.ExecutionCount,
                    _ => 0
                };
                sortKey.Push(metric);
            }

            return sortKey;
        }

        private void SortCompareFunc(object? sender, DataGridViewSortCompareEventArgs e)
        {
            var columnIndex = e.Column.Index;
            var treeNode1 = (CallTreeNode)e.CellValue1!;
            var treeNode2 = (CallTreeNode)e.CellValue2!;

            var sortKey1 = GetSortKey(columnIndex, treeNode1);
            var sortKey2 = GetSortKey(columnIndex, treeNode2);

            int comparison = 0;
            while (comparison == 0 && sortKey1.Count > 0 && sortKey2.Count > 0)
            {
                comparison = sortKey1.Pop() - sortKey2.Pop();
            }

            if (comparison == 0)
            {
                if (sortKey1.Count > 0)
                    comparison = 1;
                else if (sortKey2.Count > 0)
                    comparison = -1;

                if (Columns[columnIndex]!.HeaderCell.SortGlyphDirection == SortOrder.Descending)
                    comparison *= -1;
            }

            e.SortResult = comparison;
            e.Handled = true;
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
                var callTreeDateView = (CallTreeGridView)DataGridView!;
                var treeNode = (CallTreeNode)value!;

                int columnIndex = callTreeDateView.Columns[ColumnIndex].Index;
                if (columnIndex == RoutineColumnIndex)
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
                        columnIndex == SelfCPUColumnIndex ||
                        columnIndex == TotalCPUColumnIndex ||
                        columnIndex == ElapsedCPUColumnIndex ||
                        columnIndex == ExecutionCountColumnIndex);

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
                    (int num, int den) ratio = columnIndex switch
                    {
                        SelfCPUColumnIndex => (treeNode.CPUMetrics.SelfCycleCount, callTreeDateView.TotalCycleCount),
                        TotalCPUColumnIndex => (treeNode.CPUMetrics.InclusiveCycleCount, callTreeDateView.TotalCycleCount),
                        ElapsedCPUColumnIndex => (treeNode.CPUMetrics.ElapsedCycleCount, callTreeDateView.TotalCycleCount),
                        ExecutionCountColumnIndex => (treeNode.CPUMetrics.ExecutionCount, callTreeDateView.MaxExecutionCount),
                        _ => (0, 0)
                    };

                    int width = 0;
                    int margin = cellBounds.Height / 8;

                    if (ratio.num > 0 && ratio.den > 0)
                    {
                        int maxWidth = cellBounds.Width - (margin * 2);
                        width = (int)double.Ceiling((double)ratio.num * maxWidth / ratio.den);
                    }

                    var rect = new Rectangle(
                        cellBounds.Right - margin - width,
                        cellBounds.Y + margin,
                        width,
                        cellBounds.Height - margin * 2);

                    bool selected = (cellState & DataGridViewElementStates.Selected) != 0;

                    var color = Blend(
                        callTreeDateView.DefaultCellStyle.BackColor,
                        callTreeDateView.DefaultCellStyle.SelectionBackColor,
                        selected ? 0.75 : 0.25);

                    using var brush = new SolidBrush(color);
                    graphics.FillRectangle(brush, rect);

                    // draw default foreground
                    var forePaintParts = paintParts & DataGridViewPaintParts.ContentForeground;
                    base.Paint(
                        graphics, clipBounds, cellBounds, rowIndex, cellState,
                        value, formattedValue, errorText,
                        cellStyle, advancedBorderStyle, forePaintParts);
                }

                if (columnIndex == RoutineColumnIndex)
                {
                    // paint expand/close triangle
                    int cellHeight = cellBounds.Height;
                    float centerX = cellBounds.Left + cellHeight / 2;
                    centerX += (treeNode.Depth * cellHeight);

                    if (treeNode.HasChildren)
                    {
                        float centerY = cellBounds.Top + cellHeight / 2;

                        bool selected = (cellState & DataGridViewElementStates.Selected) != 0;
                        var foreColor = selected ? cellStyle.SelectionForeColor : cellStyle.ForeColor;
                        var backColor = selected ? cellStyle.SelectionBackColor : cellStyle.BackColor;

                        PointF[]? points = null;

                        if (treeNode.Expansion == TreeNode<CallTreeNode>.ExpansionType.Closed)
                        {
                            float halfHeight = cellHeight / 5;
                            float quarterHeight = halfHeight / 2;

                            points = new[]
                            {
                                new PointF(centerX - quarterHeight, centerY - halfHeight),
                                new PointF(centerX + quarterHeight, centerY),
                                new PointF(centerX - quarterHeight, centerY + halfHeight),
                            };
                        }
                        else if (treeNode.Expansion == TreeNode<CallTreeNode>.ExpansionType.Open)
                        {
                            float hypotenuse = cellHeight / 5 * (float)Math.Sqrt(2.0);
                            float halfHypotenuse = hypotenuse / 2;

                            points = new[]
                            {
                                new PointF(centerX - halfHypotenuse, centerY + halfHypotenuse),
                                new PointF(centerX + halfHypotenuse, centerY - halfHypotenuse),
                                new PointF(centerX + halfHypotenuse, centerY + halfHypotenuse)
                            };

                            backColor = foreColor;
                        }

                        if (points != null)
                        {
                            using var brush = new SolidBrush(backColor);
                            graphics.FillPolygon(brush, points);

                            using var pen = new Pen(foreColor);
                            graphics.DrawPolygon(pen, points);
                        }
                    }

                    // paint flame icon
                    if (treeNode.HotPath)
                    {
                        int size = cellBounds.Height / 2;
                        int inset = cellBounds.Height / 4;
                        int indent = (int)centerX + cellHeight / 2;
                        var rect = new Rectangle(indent + inset / 2, cellBounds.Top + inset, size, size);

                        BeebPerfForm form = (BeebPerfForm)callTreeDateView.GetParentForm();
                        graphics.DrawImage(form.FlameImage, rect);
                    }
                }
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
