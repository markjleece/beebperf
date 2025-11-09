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
using System.Drawing.Drawing2D;
using static BeebPerf.MemoryAnalysis;
using static BeebPerf.model.DisplaySettings;

namespace BeebPerf.ux
{
    internal class CodeView : GridView<object>
    {
        private const int AddressColumnIndex = 0;
        private const int LabelColumnIndex = 1;
        private const int InstructionColumnIndex = 2;
        private const int MemoryReadCountColumnIndex = 3;
        private const int MemoryWriteCountColumnIndex = 4;
        private const int TailCallColumnIndex = 5;
        private const int TotalCPUColumnIndex = 6;
        private const int BranchCountColumnIndex = 7;
        private const int ExecutionCountColumnIndex = 8;

        public CodeView() : base(
            DataGridViewAutoSizeColumnsMode.AllCells, 
            System.Windows.Forms.SelectionMode.None)
        {
            var cellTemplate = new CellTemplate();
            AddColumn("Address", "Address", cellTemplate);
            AddColumn("Label", "Label", cellTemplate);
            AddColumn("Instruction", "Instruction", cellTemplate);
            AddColumn("MemoryReadCount", "Memory reads [#]", cellTemplate);
            AddColumn("MemoryWriteCount", "Memory writes [#]", cellTemplate);
            AddColumn("TailCall", "Tail call", cellTemplate);
            AddColumn("TotalCPU", "Total CPU [#cycles, %]", cellTemplate);
            AddColumn("BranchCount", "Branch count [#, %]", cellTemplate);
            AddColumn("ExecutionCount", "Execution count [#, %]", cellTemplate);

            foreach (DataGridViewColumn column in Columns)
                column.SortMode = DataGridViewColumnSortMode.NotSortable;

            SetColumnAlignment(MemoryReadCountColumnIndex, DataGridViewContentAlignment.MiddleRight);
            SetColumnAlignment(MemoryWriteCountColumnIndex, DataGridViewContentAlignment.MiddleRight);
            SetColumnAlignment(TailCallColumnIndex, DataGridViewContentAlignment.MiddleCenter);
            SetColumnAlignment(BranchCountColumnIndex, DataGridViewContentAlignment.MiddleRight);
            SetColumnAlignment(TotalCPUColumnIndex, DataGridViewContentAlignment.MiddleRight);
            SetColumnAlignment(ExecutionCountColumnIndex, DataGridViewContentAlignment.MiddleRight);
            SetColumnAlignment(ExecutionCountColumnIndex, DataGridViewContentAlignment.MiddleRight);
            SetColumnAlignment(ExecutionCountColumnIndex, DataGridViewContentAlignment.MiddleRight);

            SetColumnHeaderToolTip(AddressColumnIndex, "Address");
            SetColumnHeaderToolTip(LabelColumnIndex, "Label");
            SetColumnHeaderToolTip(InstructionColumnIndex, "Instruction mnemonic and operand");
            SetColumnHeaderToolTip(MemoryReadCountColumnIndex, "Selected memory address read count");
            SetColumnHeaderToolTip(MemoryWriteCountColumnIndex, "Selected memory address write count");
            SetColumnHeaderToolTip(TailCallColumnIndex, "Whether a jump or branch instruction executes a tail call");
            SetColumnHeaderToolTip(TotalCPUColumnIndex, "Total cycles used executing instruction and routines it calls");
            SetColumnHeaderToolTip(BranchCountColumnIndex, "Number of times the branch was taken");
            SetColumnHeaderToolTip(ExecutionCountColumnIndex, "Number of times the instruction was executed");

            SetColumnVisibility(MemoryReadCountColumnIndex, false);
            SetColumnVisibility(MemoryWriteCountColumnIndex, false);
        }

        public void Initialize(
            Func<Routine, CallStack?, List<InstructionMetrics>>? calculateInstructionMetrics,
            Dictionary<CanonicalAddress, Routine> routinesByAddress,
            Dictionary<ushort, string> labels,
            InstructionSet instructionSet)
        {
            _CalculateInstructionMetrics = calculateInstructionMetrics;
            _RoutinesByAddress = routinesByAddress;
            _InstructionSet = instructionSet;
            Labels = labels;
        }

        public void SetCode(Routine routine, CallStack? callStack, RoutineMemoryAccess? memoryAccess)
        {
            // memory access
            _MemoryAccess = memoryAccess;
            if (memoryAccess != null)
            {
                SetColumnHeaderText(MemoryReadCountColumnIndex, $"Reads from {memoryAccess.Address} [#, %]");
                SetColumnHeaderText(MemoryWriteCountColumnIndex, $"Writes to {memoryAccess.Address} [#, %]");
            }

            SetColumnVisibility(MemoryReadCountColumnIndex, memoryAccess != null);
            SetColumnVisibility(MemoryWriteCountColumnIndex, memoryAccess != null);

            // get instruction metrics
            var instructionMetrics = _CalculateInstructionMetrics!.Invoke(routine, callStack);

            // populate view data
            var dataRows = new List<Object>(instructionMetrics.Count);
            if (instructionMetrics.Count > 0)
            {
                var ellipses = new Ellipses();
                var fallThrough = new FallThrough();

                CanonicalAddress nextAddress = instructionMetrics[0].Instruction.OpcodeAddress;
                foreach (var obj in instructionMetrics)
                {
                    // add ellipses
                    if (obj.Instruction.OpcodeAddress.CompareTo(nextAddress) > 0)
                        dataRows.Add(ellipses);

                    // add fall-through
                    if (_RoutinesByAddress.TryGetValue(obj.Instruction.OpcodeAddress, out var routine_))
                        if (routine_ != routine)
                            dataRows.Add(fallThrough);

                    // add instruction metrics
                    dataRows.Add(obj);

                    nextAddress = obj.Instruction.OpcodeAddress.Offset(_InstructionSet!.Size(obj.Instruction.Opcode));
                }
            }

            // sum cycles and find max
            int totalCycles = 0;
            int maxExecutionCount = 0;
            foreach (var instructionMetric in instructionMetrics)
            {
                totalCycles += instructionMetric.InclusiveCycleCount;
                maxExecutionCount = Math.Max(maxExecutionCount, instructionMetric.ExecutionCount);
            }
            _TotalCycleCount = totalCycles;
            _MaxExecutionCount = maxExecutionCount;

            SetRowsData(dataRows);
            SetToolTips(dataRows);
            ResizeColumns();
        }

        public new void Clear()
        {
            SetColumnVisibility(MemoryReadCountColumnIndex, false);
            SetColumnVisibility(MemoryWriteCountColumnIndex, false);
            _MemoryAccess = null;
            base.Clear();
            ResizeColumns();
        }

        private void ResizeColumns()
        {
            foreach (DataGridViewColumn column in Columns)
                column.MinimumWidth = 2;
            AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
            Invalidate();
        }

        protected override string OnFormatRowData(object obj, int columnIndex, int rowIndex)
        {
            if (obj is not InstructionMetrics)
                return string.Empty;

            return FormatCountAndRange(obj, columnIndex);
        }

        protected override (int value, int range) OnRowDataCountAndRange(object obj, int columnIndex)
        {
            if (obj is not InstructionMetrics)
                return (value: -1, range: 1);

            var instructionMetrics = (InstructionMetrics)obj;
            return columnIndex switch
            {
                TotalCPUColumnIndex => (value: instructionMetrics.InclusiveCycleCount, range: _TotalCycleCount),
                ExecutionCountColumnIndex => (value: instructionMetrics.ExecutionCount, range: _MaxExecutionCount),
                BranchCountColumnIndex => (value: GetInstructionBranchCount(instructionMetrics), range: instructionMetrics.ExecutionCount),
                MemoryReadCountColumnIndex => (value: GetMemoryReadCount(instructionMetrics.Instruction), range: instructionMetrics.ExecutionCount),
                MemoryWriteCountColumnIndex => (value: GetMemoryWriteCount(instructionMetrics.Instruction), range: instructionMetrics.ExecutionCount),
                _ => (value: -1, range: 1)
            };
        }

        private int GetInstructionBranchCount(InstructionMetrics instructionMetrics)
        {
            if (_InstructionSet!.IsBranch(instructionMetrics.Instruction.Opcode))
                return instructionMetrics.BranchCount;
            else
                return -1;
        }

        private void SetToolTips(List<Object> lines)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i] is InstructionMetrics)
                {
                    var metrics = (InstructionMetrics)lines[i];
                    Rows[i].Cells[InstructionColumnIndex].ToolTipText = FormatToolTip(metrics.Instruction);
                }
            }
        }

        private string FormatToolTip(CoreInstruction instruction)
        {
            byte opcode = instruction.Opcode;

            if (_InstructionSet!.AddressingMode(opcode) != InstructionSet.AddressMode.Immediate)
                return string.Empty;

            ushort operand = instruction.Operand;
            int size = _InstructionSet!.Size(opcode);
            string hex = (size == 2) ? $"{operand:X2}" : $"{operand:X4}";
            string octal = Convert.ToString(operand, 8).PadLeft(size == 2 ? 3 : 6, '0');
            string binary = Convert.ToString(operand, 2).PadLeft(size == 2 ? 8 : 16, '0');
            return $"Hex: &{hex}\nDec: {operand}\nOct: {octal}\nBin: {binary}";
        }

        public class CellTemplate : GridViewCellTemplate
        {
            protected override Size GetPreferredSize(Graphics graphics, DataGridViewCellStyle cellStyle, int rowIndex, Size constraintSize)
            {
                var size = base.GetPreferredSize(graphics, cellStyle, rowIndex, constraintSize);

                var codeGridView = (CodeView)DataGridView!;
                var obj = (object)codeGridView._DataRows[rowIndex];
                if (obj is InstructionMetrics)
                    if (ColumnIndex == AddressColumnIndex ||
                        ColumnIndex == LabelColumnIndex ||
                        ColumnIndex == InstructionColumnIndex)
                        size.Width = cellStyle.Padding.Horizontal + PaintCode((InstructionMetrics)obj, graphics, cellBounds: null, cellStyle, rowIndex, measureOnly: true);

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
                var codeGridView = (CodeView)DataGridView!;
                var obj = (object)codeGridView._DataRows[rowIndex];
                PaintBackground(graphics, cellBounds, rowIndex, cellState, obj);
                if (obj is InstructionMetrics)
                    if (ColumnIndex == AddressColumnIndex ||
                        ColumnIndex == LabelColumnIndex ||
                        ColumnIndex == InstructionColumnIndex)
                        PaintCode((InstructionMetrics)obj, graphics, cellBounds, cellStyle, rowIndex, measureOnly: false);
                    else
                        base.Paint(
                            graphics, clipBounds, cellBounds, rowIndex, cellState, value, formattedValue, errorText,
                            cellStyle, advancedBorderStyle, paintParts & DataGridViewPaintParts.ContentForeground);
                else if (obj is Ellipses)
                    PaintEllipses(graphics, cellBounds, cellStyle);
                else if (obj is FallThrough)
                    PaintFallThrough(graphics, rowIndex, cellStyle);
            }

            private void PaintBackground(
                Graphics graphics,
                Rectangle cellBounds,
                int rowIndex,
                DataGridViewElementStates cellState,
                object value)
            {
                var codeGridView = (CodeView)DataGridView!;
                var backColor = codeGridView.DefaultCellStyle.BackColor;

                if (value is InstructionMetrics)
                {
                    var instructionMetrics = (InstructionMetrics)value!;
                    double hotness = Math.Clamp((double)instructionMetrics.InclusiveCycleCount / codeGridView._TotalCycleCount, 0.0, 1.0);
                    Color hotColor = backColor.GetBrightness() > 0.5 ? ColorLightRed : Color.DarkRed;
                    backColor = Blend(backColor, hotColor, hotness);
                }

                using var brush = new SolidBrush(backColor);
                graphics.FillRectangle(brush, cellBounds);

                if (value is InstructionMetrics)
                    DrawBar(rowIndex, graphics, cellBounds, backColor, cellState);
            }

            private int PaintCode(
                InstructionMetrics instructionMetrics,
                Graphics graphics,
                Rectangle? cellBounds,
                DataGridViewCellStyle cellStyle,
                int rowIndex,
                bool measureOnly)
            {
                if (rowIndex > 0 && (ColumnIndex == AddressColumnIndex || ColumnIndex == LabelColumnIndex))
                {
                    // is duplicate?
                    var codeGridView = (CodeView)DataGridView!;
                    var valueAbove = codeGridView._DataRows[rowIndex - 1];
                    if (valueAbove is InstructionMetrics)
                    {
                        var instructionMetricsAbove = (InstructionMetrics)valueAbove!;
                        if (instructionMetricsAbove.Instruction.OpcodeAddress.Equals(instructionMetrics.Instruction.OpcodeAddress))
                            return 0;
                    }
                }

                if (ColumnIndex == AddressColumnIndex)
                    return PaintAddress(instructionMetrics, graphics, cellBounds, cellStyle, measureOnly);
                else if (ColumnIndex == LabelColumnIndex)
                    return PaintLabel(instructionMetrics, graphics, cellBounds, cellStyle, measureOnly);
                else if (ColumnIndex == InstructionColumnIndex)
                    return PaintInstruction(instructionMetrics, graphics, cellBounds, cellStyle, measureOnly);
                return 0;
            }

            private int PaintAddress(
                InstructionMetrics instructionMetrics,
                Graphics graphics,
                Rectangle? cellBounds,
                DataGridViewCellStyle cellStyle,
                bool measureOnly)
            {
                ushort address = instructionMetrics.Instruction.OpcodeAddress.Address;
                return PaintCodeElement(Setting.Address, address, indent: 0, includePadding: true, graphics, cellBounds, cellStyle, measureOnly);
            }

            private int PaintLabel(
                InstructionMetrics instructionMetrics,
                Graphics graphics,
                Rectangle? cellBounds,
                DataGridViewCellStyle cellStyle,
                bool measureOnly)
            {
                var codeGridView = (CodeView)DataGridView!;
                ushort address = instructionMetrics.Instruction.OpcodeAddress.Address;
                string label = codeGridView.FormatLabel(address, withOffset: false);
                return PaintCodeElement(Setting.Label, label, indent: 0, includePadding: true, graphics, cellBounds, cellStyle, measureOnly);
            }

            private int PaintInstruction(
                InstructionMetrics instructionMetrics,
                Graphics graphics,
                Rectangle? cellBounds,
                DataGridViewCellStyle cellStyle,
                bool measureOnly)
            {
                var codeGridView = (CodeView)DataGridView!;
                byte opcode = instructionMetrics.Instruction.Opcode;
                ushort operand = instructionMetrics.Instruction.Operand;

                var instructionSet = codeGridView._InstructionSet!;
                int opSize = instructionSet.Size(opcode);
                string mnemonic = instructionSet.Mnemonic(opcode);
                InstructionSet.AddressMode addressMode = instructionSet.AddressingMode(opcode);

                if (addressMode == InstructionSet.AddressMode.Relative)
                {
                    int branchAddress = instructionMetrics.Instruction.OpcodeAddress.Address;
                    operand = (ushort)unchecked(branchAddress + 2 + (sbyte)operand);
                }

                List<Segment> segments = new();
                segments.Add(new Segment { Value = mnemonic, Type = Setting.Mnemonic });

                if (opSize > 1)
                {
                    Object address = operand;
                    if (opSize == 2 && addressMode != InstructionSet.AddressMode.Relative)
                        address = (byte)operand;

                    var type = (addressMode == InstructionSet.AddressMode.Immediate) ? Setting.Literal : Setting.Address;
                    segments.Add(new Segment { Value = address, Type = type });

                    if (addressMode != InstructionSet.AddressMode.Immediate)
                    {
                        string label = codeGridView.FormatLabel(operand, withOffset: true);
                        if (label.Length > 0)
                        {
                            segments.Insert(1, new Segment { Value = label, Type = Setting.Label });
                            segments.Insert(2, new Segment { Value = " (", Type = Setting.Punctuation });
                            segments.Add(new Segment { Value = ")", Type = Setting.Punctuation });
                        }
                    }
                }

                switch (addressMode)
                {
                    case InstructionSet.AddressMode.Accumulator:
                        segments.Add(new Segment { Value = "A", Type = Setting.Mnemonic });
                        break;

                    case InstructionSet.AddressMode.Immediate:
                        segments.Insert(1, new Segment { Value = "#", Type = Setting.Punctuation });
                        break;

                    case InstructionSet.AddressMode.ZeroPageX:
                    case InstructionSet.AddressMode.AbsoluteX:
                        segments.Add(new Segment { Value = ",X", Type = Setting.Punctuation });
                        break;

                    case InstructionSet.AddressMode.ZeroPageY:
                    case InstructionSet.AddressMode.AbsoluteY:
                        segments.Add(new Segment { Value = ",Y", Type = Setting.Punctuation });
                        break;

                    case InstructionSet.AddressMode.Indirect:
                        segments.Insert(1, new Segment { Value = "(", Type = Setting.Punctuation });
                        segments.Add(new Segment { Value = ")", Type = Setting.Punctuation });
                        break;

                    case InstructionSet.AddressMode.IndirectX:
                        segments.Insert(1, new Segment { Value = "(", Type = Setting.Punctuation });
                        segments.Add(new Segment { Value = ",X)", Type = Setting.Punctuation });
                        break;

                    case InstructionSet.AddressMode.IndirectY:
                        segments.Insert(1, new Segment { Value = "(", Type = Setting.Punctuation });
                        segments.Add(new Segment { Value = "),Y", Type = Setting.Punctuation });
                        break;
                    default:
                        break;
                }

                int measureWidth = 0;
                for (var i = 0; i < segments.Count; i++)
                {
                    var segment = segments[i];
                    bool includePadding = (i == segments.Count - 1);
                    measureWidth += PaintCodeElement(segment.Type, segment.Value, indent: measureWidth, includePadding, graphics, cellBounds, cellStyle, measureOnly);
                }

                if (!measureOnly && instructionMetrics.CodeModified)
                {
                    Rectangle bounds = (Rectangle)cellBounds!;
                    using var pen = new Pen(cellStyle.ForeColor);
                    graphics.DrawLine(pen, bounds.Left, bounds.Top, bounds.Left, bounds.Bottom);
                }

                return measureWidth;
            }

            private int PaintCodeElement(
                Setting setting,
                Object value,
                int indent,
                bool includePadding,
                Graphics graphics,
                Rectangle? cellBounds,
                DataGridViewCellStyle cellStyle,
                bool measureOnly)
            {
                var codeGridView = (CodeView)DataGridView!;
                var form = (BeebPerfForm)codeGridView.FindForm()!;
                var displaySettings = form.DisplaySettings;

                var text = displaySettings.Format(setting, value);
                using var font = displaySettings.GetFont(setting, cellStyle.Font);

                TextFormatFlags flags = TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix;
                if (!includePadding) 
                    flags |= TextFormatFlags.NoPadding;
                Size measure = TextRenderer.MeasureText(text, font, new Size(int.MaxValue, int.MaxValue), flags);

                if (!measureOnly)
                {
                    Rectangle bounds = (Rectangle)cellBounds!;
                    using var brush = new SolidBrush(displaySettings.GetColor(setting));
                    int heightAdjust = (bounds.Height - measure.Height) / 2;
                    StringFormat format = new()
                    {
                        Alignment = StringAlignment.Near,
                        Trimming = StringTrimming.None,
                        FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.NoClip
                    };

                    graphics.DrawString(text, font, brush, bounds.X + indent, bounds.Y + heightAdjust, format);
                }

                return measure.Width;
            }


            private void PaintEllipses(Graphics graphics, Rectangle cellBounds, DataGridViewCellStyle cellStyle)
            {
                if (ColumnIndex == AddressColumnIndex || ColumnIndex == InstructionColumnIndex)
                    graphics.DrawString("...", cellStyle.Font, new SolidBrush(cellStyle.ForeColor), cellBounds);
            }

            private void PaintFallThrough(Graphics graphics, int rowIndex, DataGridViewCellStyle cellStyle)
            {
                var codeGridView = (CodeView)DataGridView!;

                int lastColumnIndex = codeGridView.Columns[^1].Index;
                var lastCellBounds = codeGridView.GetCellDisplayRectangle(lastColumnIndex, rowIndex, cutOverflow: false);
                var rowBounds = new Rectangle(0, lastCellBounds.Y, lastCellBounds.Right, lastCellBounds.Height);
                if (rowBounds.IsEmpty)
                    rowBounds = codeGridView.GetRowDisplayRectangle(rowIndex, cutOverflow: false);

                using var font = new Font(cellStyle.Font, FontStyle.Italic);
                var text = "fall-through";
                Size measure = TextRenderer.MeasureText(
                    text, font,
                    new Size(int.MaxValue, int.MaxValue),
                    TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);

                StringFormat textFormat = new StringFormat
                {
                    Alignment = StringAlignment.Center,    // Horizontal
                    LineAlignment = StringAlignment.Center // Vertical
                };
                using var brush = new SolidBrush(cellStyle.ForeColor);
                graphics.DrawString(text, font, brush, rowBounds, textFormat);

                int padding = rowBounds.Height / 4;
                int textWidth = measure.Width + (2 * padding);
                rowBounds.Inflate(-padding, 0);

                int lineLength = (rowBounds.Width - textWidth) / 2;
                if (lineLength > 0)
                {
                    using Pen dashedPen = new Pen(cellStyle.ForeColor);
                    dashedPen.DashStyle = DashStyle.Custom;
                    dashedPen.DashPattern = new float[] { 6, 3 };

                    int y = (rowBounds.Top + rowBounds.Bottom) / 2;
                    graphics.DrawLine(dashedPen, rowBounds.Left, y, rowBounds.Left + lineLength, y);
                    graphics.DrawLine(dashedPen, rowBounds.Right - lineLength, y, rowBounds.Right, y);
                }
            }

            private struct Segment
            {
                public Object Value;
                public DisplaySettings.Setting Type;
            };

            private Color ColorLightRed = Color.FromArgb(0xFF, 0x80, 0x80);
        }

        private int GetMemoryReadCount(CoreInstruction instruction)
        {
            if (_MemoryAccess != null && _MemoryAccess.InstructionReadCounts.TryGetValue(instruction, out var count))
                return count;
            else
                return -1;
        }

        private int GetMemoryWriteCount(CoreInstruction instruction)
        {
            if (_MemoryAccess != null && _MemoryAccess.InstructionWriteCounts.TryGetValue(instruction, out var count))
                return count;
            else
                return -1;
        }

        class Ellipses 
        {
        };

        class FallThrough 
        { 
        };

        private InstructionSet? _InstructionSet;
        private int _MaxExecutionCount;
        private int _TotalCycleCount;
        private Func<Routine, CallStack?, List<InstructionMetrics>>? _CalculateInstructionMetrics;
        private Dictionary<CanonicalAddress, Routine> _RoutinesByAddress = new();
        private RoutineMemoryAccess? _MemoryAccess;
    }
}