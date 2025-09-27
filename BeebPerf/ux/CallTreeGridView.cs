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
    internal class CallTreeGridView : DataGridView
    {
        public CallTreeGridView() : base()
        {
            AllowUserToAddRows = false;
            AllowUserToDeleteRows = false;
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            BackgroundColor = SystemColors.Control;
            CellFormatting += CellFormattingFunc;
            KeyDown += KeyDownFunc;
            MultiSelect = false;
            ReadOnly = true;
            RowHeadersVisible = false;
            RowTemplate.DefaultCellStyle.NullValue = null;
            SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            SortCompare += SortCompareFunc;
            SelectionChanged += SelectionChangedFunc;

            Columns.Add(new DataGridViewColumn()
            {
                Name = "Routine",
                HeaderText = "Routine",
                CellTemplate = new CallTreeCellRenderer(),
                SortMode = DataGridViewColumnSortMode.NotSortable
            });

            Columns.Add("SelfCPU", "Self CPU [#cycles, %]");
            Columns.Add("TotalCPU", "Total CPU [#cycles, %]");
            Columns.Add("ElapsedCPU", "Elapsed CPU [#cycles, %]");
            Columns.Add("ExecutionCount", "Execution count");

            Sort(Columns["TotalCPU"]!, System.ComponentModel.ListSortDirection.Descending);
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
                var selectedRow = (CallTreeNode)SelectedRows[0].Cells[0].Value!;
                form.SetSelectedRoutine(selectedRow.Routine, selectedRow.Context);
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
        }

        public int TotalCycleCount;

        private void CellFormattingFunc(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.Value == null)
                return;

            var treeNode = (CallTreeNode)e.Value;

            switch (Columns[e.ColumnIndex].Name)
            {
                case "Routine":
                    e.Value = $"{treeNode.Routine.StartAddress} {treeNode.Routine.Label}";
                    e.FormattingApplied = true;
                    int indent = treeNode.Context.Depth * Rows[e.RowIndex].Cells[0].Size.Height;
                    e.CellStyle.Padding = new Padding(e.CellStyle.Padding.Left + indent, e.CellStyle.Padding.Top, e.CellStyle.Padding.Right, e.CellStyle.Padding.Bottom);
                    break;

                case "SelfCPU":
                    treeNode = (CallTreeNode)e.Value;
                    RoutineMetrics cpuMetrics = treeNode.Routine.MetricsByStack[treeNode.Context];
                    e.Value = FormatCPUMetric(cpuMetrics.SelfCycleCount, treeNode.Depth);
                    e.FormattingApplied = true;
                    break;

                case "TotalCPU":
                    treeNode = (CallTreeNode)e.Value;
                    cpuMetrics = treeNode.Routine.MetricsByStack[treeNode.Context];
                    e.Value = FormatCPUMetric(cpuMetrics.InclusiveCycleCount, treeNode.Depth);
                    e.FormattingApplied = true;
                    break;

                case "ElapsedCPU":
                    treeNode = (CallTreeNode)e.Value;
                    cpuMetrics = treeNode.Routine.MetricsByStack[treeNode.Context];
                    e.Value = FormatCPUMetric(cpuMetrics.ElapsedCycleCount, treeNode.Depth);
                    e.FormattingApplied = true;
                    break;

                case "ExecutionCount":
                    treeNode = (CallTreeNode)e.Value;
                    cpuMetrics = treeNode.Routine.MetricsByStack[treeNode.Context];
                    e.Value = $"{"".PadLeft(2*treeNode.Depth)}{cpuMetrics.ExecutionCount:N0}";
                    e.FormattingApplied = true;
                    break;

                default:
                    break;
            }
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
                    CallTreeNode.SortField sortField = column.Name switch
                    {
                        "SelfCPU" => CallTreeNode.SortField.SelfCPU,
                        "TotalCPU" => CallTreeNode.SortField.InclusiveCPU,
                        "ElapsedCPU" => CallTreeNode.SortField.ElapsedCPU,
                        "ExecutionCount" => CallTreeNode.SortField.Count,
                        _ => 0
                    };
                    treeNode.Sort(sortField, sortOrder);
                    break;
                }
            }

            treeNode.Expansion = TreeNode<CallTreeNode>.ExpansionType.Open;
            AddChildRows(rowIndex + 1, treeNode);
        }

        private void CloseTreeNode(int rowIndex, CallTreeNode treeNode)
        {
            RemoveChildRows(rowIndex + 1, treeNode);
            treeNode.Expansion = TreeNode<CallTreeNode>.ExpansionType.Closed;
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

        private Stack<int> GetSortKey(string columnName, CallTreeNode treeNode)
        {
            var sortKey = new Stack<int>();

            for (var node = treeNode; node != null; node = node.Parent)
            {
                var cpuMetrics = node.Routine.MetricsByStack[node.Context];
                var metric = columnName switch
                {
                    "SelfCPU" => cpuMetrics.SelfCycleCount,
                    "TotalCPU" => cpuMetrics.InclusiveCycleCount,
                    "ElapsedCPU" => cpuMetrics.ElapsedCycleCount,
                    "ExecutionCount" => cpuMetrics.ExecutionCount,
                    _ => 0
                };
                sortKey.Push(metric);
            }

            return sortKey;
        }

        private void SortCompareFunc(object? sender, DataGridViewSortCompareEventArgs e)
        {
            var columnName = e.Column.Name;
            var treeNode1 = (CallTreeNode)e.CellValue1!;
            var treeNode2 = (CallTreeNode)e.CellValue2!;

            var sortKey1 = GetSortKey(columnName, treeNode1);
            var sortKey2 = GetSortKey(columnName, treeNode2);

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

                if (Columns[columnName]!.HeaderCell.SortGlyphDirection == SortOrder.Descending)
                    comparison *= -1;
            }

            e.SortResult = comparison;
            e.Handled = true;
        }

        public class CallTreeCellRenderer : DataGridViewTextBoxCell
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
                // default behavior
                base.Paint(graphics, clipBounds, cellBounds, rowIndex,
                           cellState, value, formattedValue, errorText,
                           cellStyle, advancedBorderStyle, paintParts);

                // draw triangle
                var treeNode = (CallTreeNode)value!;
                if (treeNode.HasChildren)
                {
                    int cellHeight = cellBounds.Height;
                    float centerX = cellBounds.Left + cellHeight / 2;
                    float centerY = cellBounds.Top + cellHeight / 2;

                    centerX += (treeNode.Depth * cellHeight);

                    bool selected = (cellState & DataGridViewElementStates.Selected) != 0;
                    var foreColor = selected ? cellStyle.SelectionForeColor : cellStyle.ForeColor;
                    var backColor = selected ? cellStyle.SelectionBackColor : cellStyle.BackColor;

                    if (treeNode.Expansion == TreeNode<CallTreeNode>.ExpansionType.Closed)
                    {
                        float halfHeight = cellHeight / 5;
                        float quarterHeight = halfHeight / 2;

                        var points = new[]
                        {
                            new PointF(centerX - quarterHeight, centerY - halfHeight),
                            new PointF(centerX + quarterHeight, centerY),
                            new PointF(centerX - quarterHeight, centerY + halfHeight),
                        };

                        graphics.FillPolygon(new SolidBrush(backColor), points);
                        graphics.DrawPolygon(new Pen(foreColor), points);
                    }
                    else if (treeNode.Expansion == TreeNode<CallTreeNode>.ExpansionType.Open)
                    {
                        float hypotenuse = cellHeight / 5 * (float)Math.Sqrt(2.0);
                        float halfHypotenuse = hypotenuse / 2;

                        var points = new[]
                        {
                            new PointF(centerX - halfHypotenuse, centerY + halfHypotenuse),
                            new PointF(centerX + halfHypotenuse, centerY - halfHypotenuse),
                            new PointF(centerX + halfHypotenuse, centerY + halfHypotenuse)
                        };

                        graphics.FillPolygon(new SolidBrush(foreColor), points);
                        graphics.DrawPolygon(new Pen(foreColor), points);
                    }
                }
            }
        }
    }
}
