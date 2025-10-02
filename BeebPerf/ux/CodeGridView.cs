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
    internal class CodeGridView : DataGridView
    {
        public CodeGridView() : base()
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

            Columns.Add("Address", "Address");
            Columns.Add("Label", "Label");
            Columns.Add("Instruction", "Instruction");
            Columns.Add("TotalCPU", "Total CPU [#cycles]");
            Columns.Add("ExecutionCount", "Execution count");

            var cellRenderer = new CallTreeCellRenderer();
            foreach (DataGridViewColumn column in Columns)
            {
                column.CellTemplate = cellRenderer;
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
            }

            DefaultCellStyle.SelectionBackColor = DefaultCellStyle.BackColor;
            DefaultCellStyle.SelectionForeColor = DefaultCellStyle.ForeColor;

            _InstructionStyle = new InstructionColors()
            {
                MnemonicColor = Color.DarkGreen,
                AddressColor = Color.CadetBlue,
                LabelColor = Color.Black,
                PunctuationColor = Color.DarkGray
            };
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

        public void SetCode(
            Routine routine,
            List<InstructionMetrics> instructionMetrics,
            Dictionary<CanonicalAddress, Routine> routinesByAddress,
            Dictionary<ushort, string> labels,
            InstructionSet instructionSet)
        {
            _Routine = routine;
            _Labels = labels;
            _InstructionSet = instructionSet;

            Rows.Clear();
            if (instructionMetrics.Count > 0)
            {
                var ellipses = new Ellipses();
                var fallThrough = new FallThrough();

                CanonicalAddress nextAddress = instructionMetrics[0].Instruction.OpcodeAddress;
                foreach (var obj in instructionMetrics)
                {
                    // add ellipses
                    if (obj.Instruction.OpcodeAddress.CompareTo(nextAddress) > 0)
                        Rows.Add(ellipses, ellipses, ellipses, ellipses, ellipses);

                    // add fall-through
                    if (routinesByAddress.TryGetValue(obj.Instruction.OpcodeAddress, out var routine_))
                        if (routine_ != routine)
                            Rows.Add(fallThrough, fallThrough, fallThrough, fallThrough, fallThrough);

                    // add instruction metrics
                    Rows.Add(obj, obj, obj, obj, obj);
                    nextAddress = obj.Instruction.OpcodeAddress.Offset(_InstructionSet.Size(obj.Instruction.Opcode));
                }
            }
        }

        public void Clear()
        {
            _Labels = null;
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

            var instructionMetrics = (InstructionMetrics)e.Value;
            switch (Columns[e.ColumnIndex].Name)
            {
                case "Address":
                    e.Value = instructionMetrics.Instruction.OpcodeAddress.ToString();
                    e.FormattingApplied = true;
                    break;

                case "Label":
                    var label = string.Empty;
                    _Labels!.TryGetValue(instructionMetrics.Instruction.OpcodeAddress.Address, out label);
                    e.Value = label;
                    e.FormattingApplied = true;
                    break;

                case "Instruction":
                    e.Value = FormatInstruction(instructionMetrics.Instruction);
                    e.FormattingApplied = true;
                    break;

                case "TotalCPU":
                    e.Value = $"{instructionMetrics.InclusiveCycleCount:N0}";
                    e.FormattingApplied = true;
                    break;

                case "ExecutionCount":
                    e.Value = $"{instructionMetrics.ExecutionCount:N0}";
                    e.FormattingApplied = true;
                    break;

                default:
                    break;
            }
        }

        private string FormatInstruction(CoreInstruction instruction)
        {
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

            string mnemonic = instructionSet.Mnemonic(opcode);
            switch (addressMode)
            {
                case InstructionSet.AddressMode.Implied:
                    return $"{mnemonic}";

                case InstructionSet.AddressMode.Accumulator:
                    return $"{mnemonic} A";

                case InstructionSet.AddressMode.Immediate:
                    return $"{mnemonic} #{formattedOperand}";

                case InstructionSet.AddressMode.ZeroPage:
                case InstructionSet.AddressMode.Relative:
                case InstructionSet.AddressMode.Absolute:
                    return $"{mnemonic} {formattedOperand}";

                case InstructionSet.AddressMode.ZeroPageX:
                case InstructionSet.AddressMode.AbsoluteX:
                    return $"{mnemonic} {formattedOperand},X";

                case InstructionSet.AddressMode.ZeroPageY:
                case InstructionSet.AddressMode.AbsoluteY:
                    return $"{mnemonic} {formattedOperand},Y";

                case InstructionSet.AddressMode.Indirect:
                    return $"{mnemonic} ({formattedOperand})";

                case InstructionSet.AddressMode.IndirectX:
                    return $"{mnemonic} ({formattedOperand},X)";

                case InstructionSet.AddressMode.IndirectY:
                    return $"{mnemonic} ({formattedOperand}),Y";

                default:
                    return "???";
            }
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
                var codeGridView = (CodeGridView)DataGridView!;
                bool isInstructionColumn = (codeGridView.Columns[ColumnIndex].Name == "Instruction");

                base.Paint(graphics, clipBounds, cellBounds, rowIndex, cellState,
                            !isInstructionColumn ? value : null,
                            !isInstructionColumn ? formattedValue : null,
                            !isInstructionColumn ? errorText : null,
                            cellStyle, advancedBorderStyle, paintParts);

                if (value! is InstructionMetrics)
                {
                    if (isInstructionColumn)
                        PaintInstruction(graphics, cellBounds, value, cellStyle);
                }
                else if (value! is Ellipses)
                {
                    if (isInstructionColumn)
                        PaintEllipses(graphics, cellBounds, cellStyle);
                }
                else if (value! is FallThrough)
                {
                    PaintFallThrough(graphics, rowIndex, cellStyle);
                }
            }

            private void PaintInstruction(
                Graphics graphics,
                Rectangle cellBounds,
                object? value,
                DataGridViewCellStyle cellStyle)
            {
                var codeGridView = (CodeGridView)DataGridView!;
                var instructionSet = codeGridView._InstructionSet!;

                var instructionMetrics = (InstructionMetrics)value!;
                byte opcode = instructionMetrics.Instruction.Opcode;
                ushort operand = instructionMetrics.Instruction.Operand;
                bool codeModified = instructionMetrics.CodeModified;
                string mnemonic = instructionSet.Mnemonic(opcode);
                int opSize = instructionSet.Size(opcode);
                InstructionSet.AddressMode addressMode = instructionSet.AddressingMode(opcode);

                if (addressMode == InstructionSet.AddressMode.Relative)
                {
                    int branchAddress = instructionMetrics.Instruction.OpcodeAddress.Address;
                    operand = (ushort)unchecked(branchAddress + 2 + (sbyte)operand);
                }

                var format = new StringFormat { LineAlignment = StringAlignment.Center };

                List<Segment> segments = new();
                var colors = codeGridView._InstructionStyle;

                segments.Add(new Segment() { Text = mnemonic.PadRight(5), Color = colors.MnemonicColor });

                string hexOperand = string.Empty;
                if (opSize > 1)
                {
                    if (opSize == 2)
                        hexOperand = $"&{operand:X2}";
                    else if (opSize == 3)
                        hexOperand = $"&{operand:X4}";

                    segments.Add(new Segment() { Text = hexOperand, Color = colors.AddressColor });

                    if (addressMode != InstructionSet.AddressMode.Immediate &&
                        codeGridView!._Labels!.TryGetValue(operand, out var label))
                    {
                        segments.Insert(1, new Segment() { Text = label, Color = colors.LabelColor });
                        segments.Insert(2, new Segment() { Text = " (", Color = colors.PunctuationColor });
                        segments.Add(new Segment() { Text = ")", Color = colors.PunctuationColor });
                    }
                }

                switch (addressMode)
                {
                    case InstructionSet.AddressMode.Accumulator:
                        segments.Add(new Segment() { Text = "A", Color = colors.MnemonicColor });
                        break;

                    case InstructionSet.AddressMode.Immediate:
                        segments.Insert(1, new Segment() { Text = "#", Color = colors.PunctuationColor });
                        break;

                    case InstructionSet.AddressMode.ZeroPageX:
                    case InstructionSet.AddressMode.AbsoluteX:
                        segments.Add(new Segment() { Text = ",X", Color = colors.PunctuationColor });
                        break;

                    case InstructionSet.AddressMode.ZeroPageY:
                    case InstructionSet.AddressMode.AbsoluteY:
                        segments.Add(new Segment() { Text = ",X", Color = colors.PunctuationColor });
                        break;

                    case InstructionSet.AddressMode.Indirect:
                        segments.Insert(1, new Segment() { Text = "(", Color = colors.PunctuationColor });
                        segments.Add(new Segment() { Text = ")", Color = colors.PunctuationColor });
                        break;

                    case InstructionSet.AddressMode.IndirectX:
                        segments.Insert(1, new Segment() { Text = "(", Color = colors.PunctuationColor });
                        segments.Add(new Segment() { Text = ",X)", Color = colors.PunctuationColor });
                        break;

                    case InstructionSet.AddressMode.IndirectY:
                        segments.Insert(1, new Segment() { Text = "(", Color = colors.PunctuationColor });
                        segments.Add(new Segment() { Text = "),Y", Color = colors.PunctuationColor });
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

                if (codeModified)
                {
                    using var pen = new Pen(cellStyle.ForeColor);
                    graphics.DrawLine(pen, cellBounds.Left, cellBounds.Top, cellBounds.Left, cellBounds.Bottom);
                }
            }
            private void PaintEllipses(Graphics graphics, Rectangle cellBounds, DataGridViewCellStyle cellStyle)
            {
                graphics.DrawString("...", cellStyle.Font, new SolidBrush(cellStyle.ForeColor), cellBounds);
            }

            private void PaintFallThrough(Graphics graphics, int rowIndex, DataGridViewCellStyle cellStyle)
            {
                var codeGridView = (CodeGridView)DataGridView!;

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
        }

        private class InstructionColors
        {
            public Color MnemonicColor;
            public Color AddressColor;
            public Color LabelColor;
            public Color PunctuationColor;
        }

        private Routine? _Routine;
        private Dictionary<ushort, string>? _Labels;
        private InstructionSet? _InstructionSet;
        private InstructionColors _InstructionStyle;
    }
}
