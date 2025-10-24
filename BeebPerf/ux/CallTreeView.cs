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
using System.ComponentModel;
using System.Diagnostics;

namespace BeebPerf.ux
{
    internal class CallTreeView : DataGridView
    {
        private const int RoutineColumnIndex = 0;
        private const int SelfCPUColumnIndex = 1;
        private const int TotalCPUColumnIndex = 2;
        private const int ElapsedCPUColumnIndex = 3;
        private const int ExecutionCountColumnIndex = 4;

        public CallTreeView() : base()
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
            Sorted += SortedFunc;

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
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            // ensure selection is correctly reflected when window is created
            _SuppressSelectionChangedEvent++;
            base.OnHandleCreated(e);

            BeginInvoke(new MethodInvoker(() =>
            {
                if (_SelectedTreeNode != null)
                    SelectRoutine(_SelectedTreeNode.Routine, _SelectedTreeNode.CallStack);
                else
                    ClearSelection();
                _SuppressSelectionChangedEvent--;
            }));
        }

        public void SelectRoutine(Routine routine, CallStack callStack)
        {
            // find tree node
            var treeNode = FindTreeNode(routine, callStack);
            if (treeNode == null)
            {
                ClearSelection();
                return;
            }

            // create tip-to-root stack of tree nodes 
            Stack<CallTreeNode> treeNodeStack = new();
            while (treeNode != null)
            {
                treeNodeStack.Push(treeNode);
                treeNode = treeNode.Parent;
            }

            // expand tree nodes root-to-tip, selecting the tip
            CollapseTree();

            int rowIndex = 0;
            treeNode = treeNodeStack.Pop();
            while (treeNode != null && rowIndex < Rows.Count)
            {
                if (treeNode == Rows[rowIndex].Cells[0].Value!)
                {
                    if (treeNodeStack.Count == 0)
                    {
                        _SelectedTreeNode = treeNode;

                        _SuppressSelectionChangedEvent++;
                        Rows[rowIndex].Selected = true;
                        _SuppressSelectionChangedEvent--;

                        FirstDisplayedScrollingRowIndex = Math.Clamp(rowIndex - DisplayedRowCount(false) + 1, 0, Rows.Count - 1);
                        return;
                    }

                    if (treeNode.Expansion == TreeNode<CallTreeNode>.ExpansionType.Closed)
                        OpenTreeNode(rowIndex, treeNode);
                    treeNode = treeNodeStack.Pop();
                }
                rowIndex++;
            }
        }

        public new void ClearSelection()
        {
            _SelectedTreeNode = null;

            _SuppressSelectionChangedEvent++;
            base.ClearSelection();
            _SuppressSelectionChangedEvent--;
        }

        private void SelectionChangedFunc(object? sender, EventArgs e)
        {
            if (_SuppressSelectionChangedEvent > 0)
                return;

            BeebPerfForm form = (BeebPerfForm)GetParentForm();
            if (SelectedRows.Count == 1)
            {
                var treeNode = (CallTreeNode)SelectedRows[0].Cells[0].Value!;
                _SelectedTreeNode = treeNode;
                form.SetSelectedRoutine(sender: this, treeNode.Routine, treeNode.CallStack);
            }
            else
            {
                _SelectedTreeNode = null;
                form.ClearSelectedRoutine(sender: this);
            }
        }

        protected override void OnColumnHeaderMouseClick(DataGridViewCellMouseEventArgs e)
        {
            _SuppressSelectionChangedEvent++;
            base.OnColumnHeaderMouseClick(e);
            _SuppressSelectionChangedEvent--;
        }

        private void SortedFunc(Object? sender, EventArgs args)
        {
            // ensure selection is correctly reflected after a sort
            if (Rows.Count == 0)
                return;

            if (_SelectedTreeNode != null)
            {
                foreach (DataGridViewRow row in Rows)
                {
                    if (_SelectedTreeNode != (CallTreeNode)row.Cells[0].Value!)
                        continue;

                    FirstDisplayedScrollingRowIndex = Math.Clamp(row.Index - DisplayedRowCount(false) + 1, 0, Rows.Count - 1);
                    break;
                }
            }
            else
            {
                ClearSelection();
                FirstDisplayedScrollingRowIndex = 0;
            }
        }

        public void AddCallTree(CallTreeNode treeNode)
        {
            _SuppressSelectionChangedEvent++;
            Rows.Add(treeNode, treeNode, treeNode, treeNode, treeNode);
            _SuppressSelectionChangedEvent--;
            
            ClearSelection();
            RefreshExecutionCounts();
        }

        public void Clear()
        {
            _SelectedTreeNode = null;
            Rows.Clear();
        }

        private Form GetParentForm()
        {
            Control control = this;
            while (control is not Form)
                control = control!.Parent!;
            return (Form)control;
        }

        public void CollapseTree()
        {
            for (int rowIndex = Rows.Count - 1; rowIndex >= 0; rowIndex--)
            {
                var treeNode = (CallTreeNode)Rows[rowIndex].Cells[0].Value!;
                if (treeNode == _SelectedTreeNode)
                    ClearSelection();
                if (treeNode.Expansion == TreeNode<CallTreeNode>.ExpansionType.Open)
                    CloseTreeNode(rowIndex, treeNode);
            }
        }

        public void ShowHotPaths()
        {
            CollapseTree();
            Sort(Columns[TotalCPUColumnIndex], ListSortDirection.Descending);
            for (int rowIndex = 0; rowIndex < Rows.Count; rowIndex++)
            {
                var treeNode = (CallTreeNode)Rows[rowIndex].Cells[0].Value!;
                bool hotChild = false;
                foreach (var childNode in treeNode.Children)
                    hotChild |= childNode.HotPath;
                if (hotChild)
                    OpenTreeNode(rowIndex, treeNode);
            }
            RefreshExecutionCounts();
            FirstDisplayedScrollingRowIndex = 0;
            Invalidate();
        }

        private CallTreeNode? FindTreeNode(Routine routine, CallStack callStack)
        {
            foreach (DataGridViewRow row in Rows)
            {
                var treeNode = (CallTreeNode)row.Cells[0].Value!;
                if (treeNode.Parent == null)
                {
                    var found = FindTreeNode(treeNode, routine, callStack);
                    if (found != null)
                        return found;
                }
            }
            return null;
        }

        private CallTreeNode? FindTreeNode(CallTreeNode treeNode, Routine routine, CallStack callStack)
        {
            if (treeNode.Routine == routine && callStack == treeNode.CallStack)
                return treeNode;
            foreach (var child in treeNode.Children)
            {
                var found = FindTreeNode(child, routine, callStack);
                if (found != null)
                    return found;
            }
            return null;
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
            var percentage = double.Min(100.0 * value / TotalCycleCount, 100.0);
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
                    _SuppressSelectionChangedEvent++;
                    Rows.Insert(index++, childNode, childNode, childNode, childNode, childNode);
                    _SuppressSelectionChangedEvent--;

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
                    _SuppressSelectionChangedEvent++;
                    Rows.RemoveAt(index);
                    _SuppressSelectionChangedEvent--;

                    RemoveChildRows(index, childNode);
                }
            }
        }

        private Stack<int> GetSortKey(int columnIndex, CallTreeNode treeNode)
        {
            var sortKey = new Stack<int>();

            for (var node = treeNode; node != null; node = node.Parent)
            {
                var cpuMetrics = node.CPUMetrics;
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
            protected override Size GetPreferredSize(Graphics graphics, DataGridViewCellStyle cellStyle, int rowIndex, Size constraintSize)
            {
                var size = base.GetPreferredSize(graphics, cellStyle, rowIndex, constraintSize);
                if (ColumnIndex == RoutineColumnIndex)
                {
                    var callTreeDateView = (CallTreeView)DataGridView!;
                    var treeNode = (CallTreeNode)callTreeDateView.Rows[rowIndex]!.Cells[ColumnIndex].Value!;
                    size.Width += RoutineColumnIndent(treeNode.Depth);
                }
                return size;
            }

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
                var backPaintParts = paintParts &
                    (DataGridViewPaintParts.Border |
                     DataGridViewPaintParts.Background |
                     DataGridViewPaintParts.ContentBackground |
                     DataGridViewPaintParts.SelectionBackground);
                base.Paint(
                    graphics, clipBounds, cellBounds, rowIndex, cellState,
                    value, formattedValue, errorText,
                    cellStyle, advancedBorderStyle, backPaintParts);

                var treeNode = (CallTreeNode)value!;

                if (ColumnIndex == RoutineColumnIndex)
                {
                    if (treeNode.HasChildren)
                        PaintOpenCloseTriangle(treeNode, graphics, cellBounds, cellState, cellStyle);

                    if (treeNode.HotPath)
                        PaintHotImage(treeNode, graphics, cellBounds);

                    int textIndent = RoutineColumnIndent(treeNode.Depth);
                    cellBounds = new Rectangle(
                        cellBounds.X + textIndent,
                        cellBounds.Y,
                        cellBounds.Width - textIndent,
                        cellBounds.Height);
                }
                else 
                {
                    Debug.Assert(
                        ColumnIndex == SelfCPUColumnIndex ||
                        ColumnIndex == TotalCPUColumnIndex ||
                        ColumnIndex == ElapsedCPUColumnIndex ||
                        ColumnIndex == ExecutionCountColumnIndex);
                    PaintBar(treeNode, graphics, cellBounds, cellState);
                }

                var forePaintParts = paintParts & DataGridViewPaintParts.ContentForeground;
                base.Paint(
                        graphics, clipBounds, cellBounds, rowIndex, cellState,
                        value, formattedValue, errorText,
                        cellStyle, advancedBorderStyle, forePaintParts);
            }

            private void PaintBar(
                CallTreeNode treeNode, 
                Graphics graphics, 
                Rectangle cellBounds, 
                DataGridViewElementStates cellState)
            {
                var callTreeDateView = (CallTreeView)DataGridView!;
                (int num, int den) ratio = ColumnIndex switch
                {
                    SelfCPUColumnIndex => (treeNode.CPUMetrics.SelfCycleCount, callTreeDateView.TotalCycleCount),
                    TotalCPUColumnIndex => (treeNode.CPUMetrics.InclusiveCycleCount, callTreeDateView.TotalCycleCount),
                    ElapsedCPUColumnIndex => (treeNode.CPUMetrics.ElapsedCycleCount, callTreeDateView.TotalCycleCount),
                    ExecutionCountColumnIndex => (treeNode.CPUMetrics.ExecutionCount, callTreeDateView.MaxExecutionCount),
                    _ => (0, 0)
                };

                int width = 0;
                int margin = cellBounds.Height / 8;

                if (ratio.num > ratio.den)
                    ratio.num = ratio.den;

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
            }

            private void PaintOpenCloseTriangle(
                CallTreeNode treeNode,
                Graphics graphics,
                Rectangle cellBounds,
                DataGridViewElementStates cellState,
                DataGridViewCellStyle cellStyle)
            {
                Debug.Assert(treeNode.HasChildren);
                
                int cellHeight = cellBounds.Height;
                int centerX = cellBounds.Left + RoutineColumnIndent(treeNode.Depth) - cellHeight / 2;
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

            private void PaintHotImage(
                CallTreeNode treeNode,
                Graphics graphics,
                Rectangle cellBounds)
            {
                int cellHeight = cellBounds.Height;

                int inset = cellHeight / 4;
                int size = cellHeight / 2;

                var rect = new Rectangle(
                    cellBounds.Left + RoutineColumnIndent(treeNode.Depth) + inset / 2, 
                    cellBounds.Top + inset,
                    size, size);

                var callTreeDateView = (CallTreeView)DataGridView!;
                BeebPerfForm form = (BeebPerfForm)callTreeDateView.GetParentForm();
                graphics.DrawImage(form.FlameImage, rect);
            }

            private int RoutineColumnIndent(int nodeDepth)
            {
                var callTreeDateView = (CallTreeView)DataGridView!;
                var cellHeight = callTreeDateView.Rows[0].Height;
                return cellHeight + (nodeDepth * cellHeight);
            }

            private Color Blend(Color first, Color second, double ratio)
            {
                int r = (int)(first.R * (1 - ratio) + second.R * ratio);
                int g = (int)(first.G * (1 - ratio) + second.G * ratio);
                int b = (int)(first.B * (1 - ratio) + second.B * ratio);
                return Color.FromArgb(r, g, b);
            }
        }

        private CallTreeNode? _SelectedTreeNode;
        private int _SuppressSelectionChangedEvent;
    }
}
