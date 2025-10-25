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

namespace BeebPerf.ux
{
    internal class CodeView : DataGridView
    {
        private const int AddressColumnIndex = 0;
        private const int LabelColumnIndex = 1;
        private const int InstructionColumnIndex = 2;
        private const int TailCallColumnIndex = 3;
        private const int TotalCPUColumnIndex = 4;
        private const int BranchCountColumnIndex = 5;
        private const int ExecutionCountColumnIndex = 6;

        public CodeView() : base()
        {
            AllowUserToAddRows = false;
            AllowUserToDeleteRows = false;
            AllowUserToResizeColumns = false;
            AllowUserToResizeRows = false;
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            BackgroundColor = DefaultCellStyle.BackColor;
            CellBorderStyle = DataGridViewCellBorderStyle.None;
            CellEnter += CellEnterFunc;
            CellFormatting += CellFormattingFunc;
            MultiSelect = false;
            ReadOnly = true;
            RowHeadersVisible = false;
            RowTemplate.DefaultCellStyle.NullValue = null;
            SelectionChanged += SelectionChangedFunc;

            AutoGenerateColumns = false;
            Columns.Add("Address", "Address");
            Columns.Add("Label", "Label");
            Columns.Add("Instruction", "Instruction");
            Columns.Add("TailCall", "Tail call");
            Columns.Add("TotalCPU", "Total CPU [#cycles, %");
            Columns.Add("BranchCount", "Branch count [#, %]");
            Columns.Add("ExecutionCount", "Execution count [#, %]");

            SetColumnAlignment(TailCallColumnIndex, DataGridViewContentAlignment.MiddleCenter);
            SetColumnAlignment(BranchCountColumnIndex, DataGridViewContentAlignment.MiddleRight);
            SetColumnAlignment(TotalCPUColumnIndex, DataGridViewContentAlignment.MiddleRight);
            SetColumnAlignment(ExecutionCountColumnIndex, DataGridViewContentAlignment.MiddleRight);

            SetColumnHeaderToolTip(AddressColumnIndex, "Address");
            SetColumnHeaderToolTip(LabelColumnIndex, "Label");
            SetColumnHeaderToolTip(InstructionColumnIndex, "Instruction mnemonic and operand");
            SetColumnHeaderToolTip(TailCallColumnIndex, "Whether a jump or branch instruction executes a tail call");
            SetColumnHeaderToolTip(TotalCPUColumnIndex, "Total cycles used executing instruction and routines it calls");
            SetColumnHeaderToolTip(BranchCountColumnIndex, "Number of times the branch was taken");
            SetColumnHeaderToolTip(ExecutionCountColumnIndex, "Number of times the instruction was executed");

            var cellRenderer = new CallTreeCellRenderer();
            foreach (DataGridViewColumn column in Columns)
            {
                column.CellTemplate = cellRenderer;
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
            }

            _SelectionBackColor = DefaultCellStyle.SelectionBackColor;
            _InstructionStyle = new InstructionColors()
            {
                MnemonicColor = Color.DarkGreen,
                AddressColor = Color.DarkBlue,
                LabelColor = Color.Black,
                PunctuationColor = Color.DarkSlateGray
            };

            DefaultCellStyle.SelectionBackColor = DefaultCellStyle.BackColor;
            DefaultCellStyle.SelectionForeColor = DefaultCellStyle.ForeColor;
        }

        private void SetColumnAlignment(int columnIndex, DataGridViewContentAlignment alignment)
        {
            Columns[columnIndex]!.DefaultCellStyle.Alignment = alignment;
        }

        private void SetColumnHeaderToolTip(int columnIndex, string text)
        {
            Columns[columnIndex].HeaderCell.ToolTipText = text;
        }

        private void CellEnterFunc(object? sender, DataGridViewCellEventArgs e)
        {
            ClearSelection();
        }

        private void SelectionChangedFunc(object? sender, EventArgs e)
        {
            ClearSelection();
        }

        class Ellipses {};
        class FallThrough {};

        public void Initialize(
            Func<Routine, CallStack?, List<InstructionMetrics>>? calculateInstructionMetrics,
            Dictionary<CanonicalAddress, Routine> routinesByAddress,
            Dictionary<ushort, string>? labels,
            InstructionSet instructionSet)
        {
            _CalculateInstructionMetrics = calculateInstructionMetrics;
            _RoutinesByAddress = routinesByAddress;
            _Labels = labels;
            _InstructionSet = instructionSet;
        }

        public void SetCode(Routine routine, CallStack? callStack)
        {
            Rows.Clear();
            var instructionMetrics = _CalculateInstructionMetrics!.Invoke(routine, callStack);
            if (instructionMetrics.Count > 0)
            {
                var ellipses = new Ellipses();
                var fallThrough = new FallThrough();

                CanonicalAddress nextAddress = instructionMetrics[0].Instruction.OpcodeAddress;
                foreach (var obj in instructionMetrics)
                {
                    // add ellipses
                    if (obj.Instruction.OpcodeAddress.CompareTo(nextAddress) > 0)
                        Rows.Add(ellipses, ellipses, ellipses, ellipses, ellipses, ellipses, ellipses);

                    // add fall-through
                    if (_RoutinesByAddress.TryGetValue(obj.Instruction.OpcodeAddress, out var routine_))
                        if (routine_ != routine)
                            Rows.Add(fallThrough, fallThrough, fallThrough, fallThrough, fallThrough, fallThrough, fallThrough);

                    // add instruction metrics
                    Rows.Add(obj, obj, obj, obj, obj, obj, obj);
                    nextAddress = obj.Instruction.OpcodeAddress.Offset(_InstructionSet!.Size(obj.Instruction.Opcode));

                    // set tool-tip text
                    Rows[^1].Cells[InstructionColumnIndex].ToolTipText = FormatToolTipText(obj.Instruction);
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
        }

        public void Clear()
        {
            Rows.Clear();
            Invalidate();
        }

        private void CellFormattingFunc(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.Value == null)
                return;

            if (e.Value is not InstructionMetrics)
            {
                e.Value = string.Empty;
                e.FormattingApplied = true;
                return;
            }

            var instructionMetrics = (InstructionMetrics)e.Value!;
            if (e.RowIndex > 0 && (e.ColumnIndex == AddressColumnIndex || e.ColumnIndex == LabelColumnIndex))
            {
                // is duplicate?
                var valueAbove = Rows[e.RowIndex - 1].Cells[e.ColumnIndex].Value;
                if (valueAbove is InstructionMetrics)
                {
                    var instructionMetricsAbove = (InstructionMetrics)valueAbove!;
                    if (instructionMetricsAbove.Instruction.OpcodeAddress.Equals(instructionMetrics.Instruction.OpcodeAddress))
                    {
                        e.Value = string.Empty;
                        e.FormattingApplied = true;
                        return;
                    }
                }
            }

            e.Value = e.ColumnIndex switch {
                AddressColumnIndex        => instructionMetrics.Instruction.OpcodeAddress.ToString(),
                LabelColumnIndex          => _Labels!.TryGetValue(instructionMetrics.Instruction.OpcodeAddress.Address, out var label) ? label : string.Empty,
                InstructionColumnIndex    => FormatInstruction(instructionMetrics),
                BranchCountColumnIndex    => FormatBranchCount(instructionMetrics),
                TailCallColumnIndex       => instructionMetrics.TailCall ? "yes" : string.Empty,
                TotalCPUColumnIndex       => FormatValue(instructionMetrics.InclusiveCycleCount, _TotalCycleCount),
                ExecutionCountColumnIndex => FormatValue(instructionMetrics.ExecutionCount, _MaxExecutionCount),
                _                         => string.Empty };
            e.FormattingApplied = true;
        }

        private string FormatBranchCount(InstructionMetrics instructionMetrics)
        {
            if (_InstructionSet!.IsBranch(instructionMetrics.Instruction.Opcode))
                return FormatValue(instructionMetrics.BranchCount, instructionMetrics.ExecutionCount);
            else
                return string.Empty;
        }

        private string FormatValue(int value, int range)
        {
            var percentage = double.Min(100.0 * value / range, 100);
            return $"{value:N0} ({percentage:F2}%)";
        }

        private string FormatInstruction(InstructionMetrics instructionMetrics)
        {
            var instruction = instructionMetrics.Instruction;
            byte opcode = instruction.Opcode;
            ushort operand = instruction.Operand;

            var instructionSet = _InstructionSet!;

            InstructionSet.AddressMode addressMode = instructionSet.AddressingMode(opcode);
            if (addressMode == InstructionSet.AddressMode.Relative)
            {
                int branchAddress = instruction.OpcodeAddress.Address;
                operand = (ushort)unchecked(branchAddress + 2 + (sbyte)operand);
            }

            string formattedOperand = string.Empty;
            int opSize = instructionSet.Size(opcode);
            if (opSize > 1)
            {
                if (opSize == 2)
                    formattedOperand = $"&{operand:X2}";
                else if (opSize == 3)
                    formattedOperand = $"&{operand:X4}";

                if (addressMode != InstructionSet.AddressMode.Immediate && _Labels!.TryGetValue(operand, out var label))
                    formattedOperand = $"{label} ({formattedOperand})";
            }

            string mnemonic = instructionSet.Mnemonic(opcode).PadRight(5);
            switch (addressMode)
            {
                case InstructionSet.AddressMode.Implied:
                    return $"{mnemonic}";

                case InstructionSet.AddressMode.Accumulator:
                    return $"{mnemonic}A";

                case InstructionSet.AddressMode.Immediate:
                    return $"{mnemonic}#{formattedOperand}";

                case InstructionSet.AddressMode.ZeroPage:
                case InstructionSet.AddressMode.Relative:
                case InstructionSet.AddressMode.Absolute:
                    return $"{mnemonic}{formattedOperand}";

                case InstructionSet.AddressMode.ZeroPageX:
                case InstructionSet.AddressMode.AbsoluteX:
                    return $"{mnemonic}{formattedOperand},X";

                case InstructionSet.AddressMode.ZeroPageY:
                case InstructionSet.AddressMode.AbsoluteY:
                    return $"{mnemonic}{formattedOperand},Y";

                case InstructionSet.AddressMode.Indirect:
                    return $"{mnemonic}({formattedOperand})";

                case InstructionSet.AddressMode.IndirectX:
                    return $"{mnemonic}({formattedOperand},X)";

                case InstructionSet.AddressMode.IndirectY:
                    return $"{mnemonic}({formattedOperand}),Y";

                default:
                    return "???";
            }
        }

        public class CallTreeCellRenderer : DataGridViewTextBoxCell
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
                PaintBackground(graphics, cellBounds, value);
                if (value! is InstructionMetrics)
                    PaintInstruction(
                        graphics, clipBounds, cellBounds, rowIndex, cellState, value, formattedValue, errorText,
                        cellStyle, advancedBorderStyle, paintParts);
                else if (value! is Ellipses)
                    PaintEllipses(graphics, cellBounds, cellStyle);
                else if (value! is FallThrough)
                    PaintFallThrough(graphics, rowIndex, cellStyle);
            }

            private void PaintBackground(
                Graphics graphics,
                Rectangle cellBounds,
                object? value)
            {
                var codeGridView = (CodeView)DataGridView!;
                var backColor = codeGridView.DefaultCellStyle.BackColor;

                if (value! is InstructionMetrics)
                {
                    var instructionMetrics = (InstructionMetrics)value!;
                    double hotness = Math.Clamp((double)instructionMetrics.InclusiveCycleCount / codeGridView._TotalCycleCount, 0.0, 1.0);
                    Color hotColor = backColor.GetBrightness() > 0.5 ? ColorLightRed : Color.DarkRed;
                    backColor = Blend(backColor, hotColor, hotness);
                }

                using var brush = new SolidBrush(backColor);
                graphics.FillRectangle(brush, cellBounds);

                if (value! is InstructionMetrics)
                {
                    var instructionMetrics = (InstructionMetrics)value!;
                    DrawBar(instructionMetrics, codeGridView, graphics, cellBounds, backColor);
                }
            }

            private void DrawBar(
                InstructionMetrics instructionMetrics,
                CodeView codeGridView,
                Graphics graphics,
                Rectangle cellBounds,
                Color backColor)
            {
                double ratio = ColumnIndex switch
                {
                    TotalCPUColumnIndex => (double)instructionMetrics.InclusiveCycleCount / codeGridView._TotalCycleCount,
                    ExecutionCountColumnIndex => (double)instructionMetrics.ExecutionCount / codeGridView._MaxExecutionCount,
                    BranchCountColumnIndex => (double)instructionMetrics.BranchCount / instructionMetrics.ExecutionCount,
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

                var color = Blend(backColor, codeGridView._SelectionBackColor, 0.25);
                using var brush = new SolidBrush(color);
                graphics.FillRectangle(brush, rect);
            }

            private void PaintInstruction(
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
                if (ColumnIndex != InstructionColumnIndex)
                {
                    base.Paint(
                        graphics, clipBounds, cellBounds, rowIndex, cellState, value, formattedValue, errorText,
                        cellStyle, advancedBorderStyle, paintParts & DataGridViewPaintParts.ContentForeground);
                    return;
                }

                var codeGridView = (CodeView)DataGridView!;

                var instructionMetrics = (InstructionMetrics)value!;
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
                var colors = codeGridView._InstructionStyle;

                segments.Add(new Segment { Text = mnemonic.PadRight(5), Color = colors.MnemonicColor });

                string hexOperand = string.Empty;
                if (opSize > 1)
                {
                    if (opSize == 2)
                        hexOperand = $"&{operand:X2}";
                    else if (opSize == 3)
                        hexOperand = $"&{operand:X4}";

                    segments.Add(new Segment { Text = hexOperand, Color = colors.AddressColor });

                    if (addressMode != InstructionSet.AddressMode.Immediate &&
                        codeGridView!._Labels!.TryGetValue(operand, out var label))
                    {
                        segments.Insert(1, new Segment { Text = label, Color = colors.LabelColor });
                        segments.Insert(2, new Segment { Text = " (", Color = colors.PunctuationColor });
                        segments.Add(new Segment { Text = ")", Color = colors.PunctuationColor });
                    }
                }

                switch (addressMode)
                {
                    case InstructionSet.AddressMode.Accumulator:
                        segments.Add(new Segment { Text = "A", Color = colors.MnemonicColor });
                        break;

                    case InstructionSet.AddressMode.Immediate:
                        segments.Insert(1, new Segment { Text = "#", Color = colors.PunctuationColor });
                        break;

                    case InstructionSet.AddressMode.ZeroPageX:
                    case InstructionSet.AddressMode.AbsoluteX:
                        segments.Add(new Segment { Text = ",X", Color = colors.PunctuationColor });
                        break;

                    case InstructionSet.AddressMode.ZeroPageY:
                    case InstructionSet.AddressMode.AbsoluteY:
                        segments.Add(new Segment { Text = ",Y", Color = colors.PunctuationColor });
                        break;

                    case InstructionSet.AddressMode.Indirect:
                        segments.Insert(1, new Segment { Text = "(", Color = colors.PunctuationColor });
                        segments.Add(new Segment { Text = ")", Color = colors.PunctuationColor });
                        break;

                    case InstructionSet.AddressMode.IndirectX:
                        segments.Insert(1, new Segment { Text = "(", Color = colors.PunctuationColor });
                        segments.Add(new Segment { Text = ",X)", Color = colors.PunctuationColor });
                        break;

                    case InstructionSet.AddressMode.IndirectY:
                        segments.Insert(1, new Segment { Text = "(", Color = colors.PunctuationColor });
                        segments.Add(new Segment { Text = "),Y", Color = colors.PunctuationColor });
                        break;
                    default:
                        break;
                }

                int xPos = cellBounds.X;
                int yPos = cellBounds.Y;

                foreach (var segment in segments)
                {
                    Size measure = TextRenderer.MeasureText(
                        segment.Text,
                        cellStyle.Font,
                        new Size(int.MaxValue, int.MaxValue),
                        TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);

                    using var brush = new SolidBrush(segment.Color);
                    int heightAdjust = (cellBounds.Height - measure.Height) / 2;
                    graphics.DrawString(segment.Text, cellStyle.Font, brush, xPos, yPos + heightAdjust);

                    xPos += measure.Width;
                }

                if (instructionMetrics.CodeModified)
                {
                    using var pen = new Pen(cellStyle.ForeColor);
                    graphics.DrawLine(pen, cellBounds.Left, cellBounds.Top, cellBounds.Left, cellBounds.Bottom);
                }
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
                public string Text;
                public Color Color;
            };

            private Color Blend(Color first, Color second, double ratio)
            {
                int r = (int)(first.R * (1 - ratio) + second.R * ratio);
                int g = (int)(first.G * (1 - ratio) + second.G * ratio);
                int b = (int)(first.B * (1 - ratio) + second.B * ratio);
                return Color.FromArgb(r, g, b);
            }

            private Color ColorLightRed = Color.FromArgb(0xFF, 0x80, 0x80);
        }

        private string FormatToolTipText(CoreInstruction instruction)
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

        private class InstructionColors
        {
            public Color MnemonicColor;
            public Color AddressColor;
            public Color LabelColor;
            public Color PunctuationColor;
        }

        private Dictionary<ushort, string>? _Labels;
        private InstructionSet? _InstructionSet;
        private InstructionColors _InstructionStyle;
        private Color _SelectionBackColor;
        private int _MaxExecutionCount;
        private int _TotalCycleCount;
        private Func<Routine, CallStack?, List<InstructionMetrics>>? _CalculateInstructionMetrics;
        private Dictionary<CanonicalAddress, Routine> _RoutinesByAddress = new();
    }
}
