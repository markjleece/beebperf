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

            SetColumnAlignment(AddressColumnIndex, DataGridViewContentAlignment.MiddleCenter);
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

                var codeView = (CodeView)DataGridView!;
                var obj = (object)codeView._DataRows[rowIndex];
                if (obj is InstructionMetrics &&
                    (ColumnIndex == AddressColumnIndex ||
                     ColumnIndex == LabelColumnIndex ||
                     ColumnIndex == InstructionColumnIndex))
                {
                    int measure = PaintCode(codeView, (InstructionMetrics)obj, graphics, cellBounds: null, cellStyle, rowIndex, measureOnly: true);
                    int padding = cellStyle.Font.Height;
                    size.Width = measure + padding + cellStyle.Padding.Horizontal;
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
                var codeView = (CodeView)DataGridView!;
                var obj = (object)codeView._DataRows[rowIndex];
                PaintBackground(codeView, graphics, cellBounds, rowIndex, cellState, obj);
                if (obj is InstructionMetrics)
                    if (ColumnIndex == AddressColumnIndex ||
                        ColumnIndex == LabelColumnIndex ||
                        ColumnIndex == InstructionColumnIndex)
                        PaintCode(codeView, (InstructionMetrics)obj, graphics, cellBounds, cellStyle, rowIndex, measureOnly: false);
                    else
                        base.Paint(
                            graphics, clipBounds, cellBounds, rowIndex, cellState, value, formattedValue, errorText,
                            cellStyle, advancedBorderStyle, paintParts & DataGridViewPaintParts.ContentForeground);
                else if (obj is Ellipses)
                    PaintEllipses(codeView, graphics, cellBounds, cellStyle);
                else if (obj is FallThrough)
                    PaintFallThrough(codeView, graphics, rowIndex, cellStyle);
            }

            private void PaintBackground(
                CodeView codeView,
                Graphics graphics,
                Rectangle cellBounds,
                int rowIndex,
                DataGridViewElementStates cellState,
                object value)
            {
                var backColor = codeView.DefaultCellStyle.BackColor;

                if (value is InstructionMetrics)
                {
                    var instructionMetrics = (InstructionMetrics)value!;
                    double hotness = Math.Clamp((double)instructionMetrics.InclusiveCycleCount / codeView._TotalCycleCount, 0.0, 1.0);
                    Color hotColor = backColor.GetBrightness() > 0.5 ? ColorLightRed : Color.DarkRed;
                    backColor = Blend(backColor, hotColor, hotness);
                }

                using var brush = new SolidBrush(backColor);
                graphics.FillRectangle(brush, cellBounds);

                if (value is InstructionMetrics)
                    DrawBar(rowIndex, graphics, cellBounds, backColor, cellState);
            }

            private int PaintCode(
                CodeView codeView,
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
                    var valueAbove = codeView._DataRows[rowIndex - 1];
                    if (valueAbove is InstructionMetrics)
                    {
                        var instructionMetricsAbove = (InstructionMetrics)valueAbove!;
                        if (instructionMetricsAbove.Instruction.OpcodeAddress.Equals(instructionMetrics.Instruction.OpcodeAddress))
                            return 0;
                    }
                }

                return ColumnIndex switch
                {
                    LabelColumnIndex => PaintLabel(codeView, instructionMetrics, graphics, cellBounds, cellStyle, measureOnly),
                    AddressColumnIndex => PaintAddress(codeView, instructionMetrics, graphics, cellBounds, cellStyle, measureOnly),
                    InstructionColumnIndex => PaintInstruction(codeView, instructionMetrics, graphics, cellBounds, cellStyle, measureOnly),
                    _ => 0
                };
            }

            private int PaintAddress(
                CodeView codeView,
                InstructionMetrics instructionMetrics,
                Graphics graphics,
                Rectangle? cellBounds,
                DataGridViewCellStyle cellStyle,
                bool measureOnly)
            {
                ushort address = instructionMetrics.Instruction.OpcodeAddress.Address;
                return PaintCodeElement(codeView, Setting.Address, address, indent: 0, graphics, cellBounds, cellStyle, measureOnly);
            }

            private int PaintLabel(
                CodeView codeView,
                InstructionMetrics instructionMetrics,
                Graphics graphics,
                Rectangle? cellBounds,
                DataGridViewCellStyle cellStyle,
                bool measureOnly)
            {
                ushort address = instructionMetrics.Instruction.OpcodeAddress.Address;
                string label = codeView.FormatLabel(address, withOffset: false);
                return PaintCodeElement(codeView, Setting.Label, label, indent: 0, graphics, cellBounds, cellStyle, measureOnly);
            }

            private int PaintInstruction(
                CodeView codeView,
                InstructionMetrics instructionMetrics,
                Graphics graphics,
                Rectangle? cellBounds,
                DataGridViewCellStyle cellStyle,
                bool measureOnly)
            {
                byte opcode = instructionMetrics.Instruction.Opcode;
                ushort operand = instructionMetrics.Instruction.Operand;

                var instructionSet = codeView._InstructionSet!;
                int opSize = instructionSet.Size(opcode);
                string mnemonic = instructionSet.Mnemonic(opcode);
                InstructionSet.AddressMode addressMode = instructionSet.AddressingMode(opcode);

                if (addressMode == InstructionSet.AddressMode.Relative)
                {
                    int branchAddress = instructionMetrics.Instruction.OpcodeAddress.Address;
                    operand = (ushort)unchecked(branchAddress + 2 + (sbyte)operand);
                }

                List<Segment> segments = new();
                segments.Add(new Segment { Type = Setting.Mnemonic, Value = mnemonic });

                if (opSize > 1)
                {
                    Object address = operand;
                    if (opSize == 2 && addressMode != InstructionSet.AddressMode.Relative)
                        address = (byte)operand;

                    var type = (addressMode == InstructionSet.AddressMode.Immediate) ? Setting.Literal : Setting.Address;
                    segments.Add(new Segment { Type = type, Value = address });

                    if (addressMode != InstructionSet.AddressMode.Immediate)
                    {
                        string label = codeView.FormatLabel(operand, withOffset: true);
                        if (label.Length > 0)
                        {
                            segments.Insert(1, new Segment { Type = Setting.Label, Value = label });
                            segments.Insert(2, new Segment { Type = Setting.Punctuation, Value = " (" });
                            segments.Add(new Segment { Type = Setting.Punctuation, Value = ")" });
                        }
                    }
                }

                switch (addressMode)
                {
                    case InstructionSet.AddressMode.Accumulator:
                        segments.Add(new Segment { Type = Setting.Mnemonic, Value = "A" });
                        break;

                    case InstructionSet.AddressMode.Immediate:
                        segments.Insert(1, new Segment { Type = Setting.Punctuation, Value = "#" });
                        break;

                    case InstructionSet.AddressMode.ZeroPageX:
                    case InstructionSet.AddressMode.AbsoluteX:
                        segments.Add(new Segment { Type = Setting.Punctuation, Value = ",X" });
                        break;

                    case InstructionSet.AddressMode.ZeroPageY:
                    case InstructionSet.AddressMode.AbsoluteY:
                        segments.Add(new Segment { Type = Setting.Punctuation, Value = ",Y",  });
                        break;

                    case InstructionSet.AddressMode.Indirect:
                        segments.Insert(1, new Segment { Type = Setting.Punctuation, Value = "("  });
                        segments.Add(new Segment { Type = Setting.Punctuation, Value = ")" });
                        break;

                    case InstructionSet.AddressMode.IndirectX:
                        segments.Insert(1, new Segment { Type = Setting.Punctuation, Value = "(" });
                        segments.Add(new Segment { Type = Setting.Punctuation, Value = ",X)" });
                        break;

                    case InstructionSet.AddressMode.IndirectY:
                        segments.Insert(1, new Segment { Type = Setting.Punctuation, Value = "(" });
                        segments.Add(new Segment { Type = Setting.Punctuation, Value = "),Y" });
                        break;
                    default:
                        break;
                }

                int measureWidth = 0;
                foreach (var segment in segments)
                    measureWidth += PaintCodeElement(codeView, segment.Type, segment.Value, indent: measureWidth, graphics, cellBounds, cellStyle, measureOnly);

                if (!measureOnly && instructionMetrics.CodeModified)
                {
                    Rectangle bounds = (Rectangle)cellBounds!;
                    using var pen = new Pen(cellStyle.ForeColor);
                    graphics.DrawLine(pen, bounds.Left, bounds.Top, bounds.Left, bounds.Bottom);
                }

                return measureWidth;
            }

            private int PaintCodeElement(
                CodeView codeView,
                Setting setting,
                Object value,
                int indent,
                Graphics graphics,
                Rectangle? cellBounds,
                DataGridViewCellStyle cellStyle,
                bool measureOnly)
            {
                var form = (BeebPerfForm)codeView.FindForm()!;
                var displaySettings = form.DisplaySettings;

                var text = displaySettings.Format(setting, value);
                var flags = TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;
                using var font = displaySettings.GetFont(setting, cellStyle.Font);
                Size measure = TextRenderer.MeasureText(text, font, new Size(int.MaxValue, int.MaxValue), flags);

                if (!measureOnly)
                {
                    Rectangle bounds = (Rectangle)cellBounds!;

                    StringFormat format = new()
                    {
                        FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.NoClip,
                        LineAlignment = StringAlignment.Center,
                        Trimming = StringTrimming.EllipsisCharacter,
                        Alignment = cellStyle.Alignment switch
                        {
                            DataGridViewContentAlignment.MiddleLeft => StringAlignment.Near,
                            DataGridViewContentAlignment.MiddleCenter => StringAlignment.Center,
                            DataGridViewContentAlignment.MiddleRight => StringAlignment.Far,
                            _ => StringAlignment.Near,
                        }
                    };

                    bounds.X += cellStyle.Padding.Left;
                    bounds.Y += cellStyle.Padding.Top;
                    bounds.Width -= cellStyle.Padding.Horizontal;
                    bounds.Height -= cellStyle.Padding.Vertical;

                    if (cellStyle.Alignment == DataGridViewContentAlignment.MiddleLeft)
                        bounds.X += indent;

                    if (cellStyle.Alignment != DataGridViewContentAlignment.MiddleCenter)
                        bounds.Width -= indent;

                    using var brush = new SolidBrush(displaySettings.GetColor(setting));
                    graphics.DrawString(text, font, brush, bounds, format);
                }

                return measure.Width;
            }

            private void PaintEllipses(
                CodeView codeView,
                Graphics graphics, 
                Rectangle cellBounds, 
                DataGridViewCellStyle cellStyle)
            {
                if (ColumnIndex == AddressColumnIndex || ColumnIndex == InstructionColumnIndex)
                    graphics.DrawString("...", cellStyle.Font, new SolidBrush(cellStyle.ForeColor), cellBounds);
            }

            private void PaintFallThrough(
                CodeView codeView,
                Graphics graphics, 
                int rowIndex, 
                DataGridViewCellStyle cellStyle)
            {
                int lastColumnIndex = codeView.Columns[^1].Index;
                var lastCellBounds = codeView.GetCellDisplayRectangle(lastColumnIndex, rowIndex, cutOverflow: false);
                var rowBounds = new Rectangle(0, lastCellBounds.Y, lastCellBounds.Right, lastCellBounds.Height);
                if (rowBounds.IsEmpty)
                    rowBounds = codeView.GetRowDisplayRectangle(rowIndex, cutOverflow: false);

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
                public DisplaySettings.Setting Type;
                public Object Value;
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