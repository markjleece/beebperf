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
    internal class CallTreeView : GridView<CallTreeNode>
    {
        private const int RoutineColumnIndex = 0;
        private const int SelfCPUColumnIndex = 1;
        private const int TotalCPUColumnIndex = 2;
        private const int ElapsedCPUColumnIndex = 3;
        private const int InterruptsColumnIndex = 4;
        private const int ExecutionCountColumnIndex = 5;

        public CallTreeView() : base(
            DataGridViewAutoSizeColumnsMode.DisplayedCells, 
            System.Windows.Forms.SelectionMode.One)
        {
            KeyDown += KeyDownFunc;

            AutoGenerateColumns = false;
            Columns.Add("Routine", "Routine");
            Columns.Add("SelfCPU", "Self CPU [#cycles, %]");
            Columns.Add("TotalCPU", "Total CPU [#cycles, %]");
            Columns.Add("ElapsedCPU", "Elapsed CPU [#cycles, %]");
            Columns.Add("Interrupts", "Interrupts [#cycles, %]");
            Columns.Add("ExecutionCount", "Execution count");

            SetCellTemplate(new CellTemplate());

            SetColumnAlignment(SelfCPUColumnIndex, DataGridViewContentAlignment.MiddleRight);
            SetColumnAlignment(TotalCPUColumnIndex, DataGridViewContentAlignment.MiddleRight);
            SetColumnAlignment(ElapsedCPUColumnIndex, DataGridViewContentAlignment.MiddleRight);
            SetColumnAlignment(InterruptsColumnIndex, DataGridViewContentAlignment.MiddleRight);
            SetColumnAlignment(ExecutionCountColumnIndex, DataGridViewContentAlignment.MiddleRight);

            SetColumnHeaderToolTip(RoutineColumnIndex, "Routine address and label");
            SetColumnHeaderToolTip(SelfCPUColumnIndex, "Cycles used just by this routine");
            SetColumnHeaderToolTip(TotalCPUColumnIndex, "Total cycles used by this routine and all routines it calls.");
            SetColumnHeaderToolTip(ElapsedCPUColumnIndex, "Total cycles elapsed during execution of routine, including cycles in interrupts");
            SetColumnHeaderToolTip(InterruptsColumnIndex, "Total cycles spent servicing interrupts during execution of routine");
            SetColumnHeaderToolTip(ExecutionCountColumnIndex, "Number of times this routine was executed");

            SetColumnSortMode(RoutineColumnIndex, DataGridViewColumnSortMode.NotSortable);
        }

        public void ShowHotPaths()
        {
            CollapseTree();
            SortColumn(TotalCPUColumnIndex, SortOrder.Descending);
            for (int index = 0; index < _DataRows.Count; index++)
            {
                var treeNode = _DataRows[index];
                bool hotChild = false;
                foreach (var childNode in treeNode.Children)
                    hotChild |= childNode.HotPath;
                if (hotChild)
                    OpenTreeNode(index, treeNode);
            }
            RefreshExecutionCounts();
            ScrollToTop();
        }

        public void SetCallTrees(CallTreeNode?[] callTrees)
        {
            var callTreeList = new List<CallTreeNode>();
            foreach (var callTree in callTrees)
                if (callTree != null)
                    callTreeList.Add(callTree);
            SetRowsData(callTreeList);
            RefreshExecutionCounts();
        }

        protected override void OnSelectionChange(object? sender, CallTreeNode? treeNode)
        {
            var form = (BeebPerfForm)GetParentForm();
            if (treeNode != null)
                form.SetSelectedRoutine(sender: this, treeNode.Routine, treeNode.CallStack, memoryAccess: null);
            else
                form.ClearSelectedRoutine(sender: this);
        }

        protected override int OnSortCompare(CallTreeNode a, CallTreeNode b, int columnIndex)
        {
            var sortKey1 = GetSortKey(columnIndex, a);
            var sortKey2 = GetSortKey(columnIndex, b);

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

                if (Columns[columnIndex]!.HeaderCell.SortGlyphDirection == SortOrder.Ascending)
                    comparison *= -1;
            }

            return comparison;
        }

        protected override string OnFormatRowData(CallTreeNode treeNode, int columnIndex, int rowIndex)
        {
            return columnIndex switch
            {
               RoutineColumnIndex =>$"{"".PadLeft(treeNode.HotPath ? 4 : 0)}{treeNode.Routine.StartAddress} {treeNode.Routine.Label}",
                _ => FormatCountAndRange(treeNode, columnIndex),
            };
        }

        protected override (int value, int range) OnRowDataCountAndRange(CallTreeNode treeNode, int columnIndex)
        {
            return columnIndex switch
            {
                SelfCPUColumnIndex => (value: treeNode.Routine.AggregateMetrics.SelfCycleCount, range: TotalCycleCount),
                TotalCPUColumnIndex => (value: treeNode.Routine.AggregateMetrics.InclusiveCycleCount, range: TotalCycleCount),
                ElapsedCPUColumnIndex => (value: treeNode.Routine.AggregateMetrics.ElapsedCycleCount, range: TotalCycleCount),
                InterruptsColumnIndex => (value: treeNode.Routine.AggregateMetrics.ElapsedCycleCount - treeNode.Routine.AggregateMetrics.InclusiveCycleCount, range: treeNode.Routine.AggregateMetrics.ElapsedCycleCount),
                ExecutionCountColumnIndex => (value: treeNode.Routine.AggregateMetrics.ExecutionCount, range: MaxExecutionCount),
                _ => (value: -1, range: 1)
            };
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
            while (treeNode != null && rowIndex < _DataRows.Count)
            {
                if (treeNode == _DataRows[rowIndex])
                {
                    if (treeNodeStack.Count == 0)
                    {
                        SelectRow(treeNode);
                        return;
                    }

                    if (treeNode.Expansion == TreeNode<CallTreeNode>.ExpansionType.Closed)
                        OpenTreeNode(rowIndex, treeNode);
                    treeNode = treeNodeStack.Pop();
                }
                rowIndex++;
            }
        }

        public void CollapseTree()
        {
            for (int rowIndex = _DataRows.Count - 1; rowIndex >= 0; rowIndex--)
            {
                var treeNode = _DataRows[rowIndex];
                if (treeNode == _SelectedDataRow)
                    ClearSelection();
                if (treeNode.Expansion == TreeNode<CallTreeNode>.ExpansionType.Open)
                    CloseTreeNode(rowIndex, treeNode);
            }
        }

        private CallTreeNode? FindTreeNode(Routine routine, CallStack callStack)
        {
            foreach (var treeNode in _DataRows)
            {
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

        private void RefreshExecutionCounts()
        {
            int maxExecutionCount = 0;
            foreach (var treeNode in _DataRows)
                if (maxExecutionCount < treeNode.CPUMetrics.ExecutionCount)
                    maxExecutionCount = treeNode.CPUMetrics.ExecutionCount;
            MaxExecutionCount = maxExecutionCount;
            InvalidateColumn(ExecutionCountColumnIndex);
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
                var treeNode = _DataRows[cell.RowIndex];
                if (treeNode.HasChildren)
                {
                    if ((e.KeyCode == Keys.Right || e.KeyCode == Keys.Add || e.KeyCode == Keys.Space) &&
                        treeNode.Expansion == TreeNode<CallTreeNode>.ExpansionType.Closed)
                        OpenTreeNode(cell.RowIndex, treeNode);
                    else if ((e.KeyCode == Keys.Left || e.KeyCode == Keys.Subtract || e.KeyCode == Keys.Space) &&
                        treeNode.Expansion == TreeNode<CallTreeNode>.ExpansionType.Open)
                        CloseTreeNode(cell.RowIndex, treeNode);
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
                var treeNode = _DataRows[hti.RowIndex];
                var cell = Rows[hti.RowIndex].Cells[hti.ColumnIndex];
                if (treeNode.HasChildren)
                {
                    int cellX = e.X - hti.ColumnX;
                    if ((cellX >= treeNode.Depth * cell.Size.Height) &&
                        (cellX <= (treeNode.Depth + 1) * cell.Size.Height))
                    {
                        if (treeNode.Expansion == TreeNode<CallTreeNode>.ExpansionType.Closed)
                            OpenTreeNode(hti.RowIndex, treeNode);
                        else if (treeNode.Expansion == TreeNode<CallTreeNode>.ExpansionType.Open)
                            CloseTreeNode(hti.RowIndex, treeNode);
                        return; // we don't want the selection to change
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
                        InterruptsColumnIndex => CallTreeNode.SortField.Interrupts,
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
            AutoGrowColumns();
        }

        private void CloseTreeNode(int rowIndex, CallTreeNode treeNode)
        {
            RemoveChildRows(rowIndex + 1, treeNode);
            treeNode.Expansion = TreeNode<CallTreeNode>.ExpansionType.Closed;
            RefreshExecutionCounts();
            AutoGrowColumns();
        }

        private int AddChildRows(int index, CallTreeNode treeNode)
        {
            if (treeNode.HasChildren && treeNode.Expansion == TreeNode<CallTreeNode>.ExpansionType.Open)
            {
                foreach (var childNode in treeNode.Children)
                {
                    _DataRows.Insert(index++, childNode);
                    IncrementRowCount();
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
                    _DataRows.RemoveAt(index);
                    DecrementRowCount();
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
                    InterruptsColumnIndex => cpuMetrics.ElapsedCycleCount - cpuMetrics.InclusiveCycleCount,
                    ExecutionCountColumnIndex => cpuMetrics.ExecutionCount,
                    _ => 0
                };
                sortKey.Push(metric);
            }

            return sortKey;
        }

        public class CellTemplate : GridViewCellTemplate
        {
            protected override Size GetPreferredSize(Graphics graphics, DataGridViewCellStyle cellStyle, int rowIndex, Size constraintSize)
            {
                var size = base.GetPreferredSize(graphics, cellStyle, rowIndex, constraintSize);
                if (ColumnIndex == RoutineColumnIndex)
                {
                    var callTreeDateView = (CallTreeView)DataGridView!;
                    var treeNode = callTreeDateView._DataRows[rowIndex];
                    size.Width += RoutineColumnIndent(treeNode.Depth);
                }
                return size;
            }

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
                var backPaintParts = paintParts &
                    (DataGridViewPaintParts.Border |
                     DataGridViewPaintParts.Background |
                     DataGridViewPaintParts.ContentBackground |
                     DataGridViewPaintParts.SelectionBackground);
                base.Paint(
                    graphics, clipBounds, cellBounds, rowIndex, cellState,
                    value, formattedValue, errorText,
                    cellStyle, advancedBorderStyle, backPaintParts);

                var callTreeView = (CallTreeView)DataGridView!;
                if (ColumnIndex == RoutineColumnIndex)
                {
                    var treeNode = callTreeView._DataRows[rowIndex];

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

                var forePaintParts = paintParts & DataGridViewPaintParts.ContentForeground;
                base.Paint(
                        graphics, clipBounds, cellBounds, rowIndex, cellState,
                        value, formattedValue, errorText,
                        cellStyle, advancedBorderStyle, forePaintParts);
            }

            private void PaintOpenCloseTriangle(
                CallTreeNode treeNode,
                Graphics graphics,
                Rectangle cellBounds,
                DataGridViewElementStates cellState,
                DataGridViewCellStyle cellStyle)
            {
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
        }
    }
}
