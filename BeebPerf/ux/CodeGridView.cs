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
            Columns.Add(new DataGridViewColumn()
            {
                Name = "Instruction",
                HeaderText = "Instruction",
                CellTemplate = new CallTreeCellRenderer(),
            });
            Columns.Add("TotalCPU", "Total CPU [#cycles]");
            Columns.Add("ExecutionCount", "Execution count");

            foreach (DataGridViewColumn column in Columns)
            {
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

        public void SetCode(InstructionSet instructionSet, Routine routine, List<InstructionMetrics> instructionMetrics, Dictionary<ushort, string> labels)
        {
            var missing = new InstructionMetrics(new CoreInstruction(), ordinal:-1);

            _InstructionSet = instructionSet;
            _Routine = routine;
            _Labels = labels;
            Rows.Clear();
            if (instructionMetrics.Count > 0)
            {
                CanonicalAddress nextAddress = instructionMetrics[0].Instruction.OpcodeAddress;
                foreach (var obj in instructionMetrics)
                {
                    if (obj.Instruction.OpcodeAddress.CompareTo(nextAddress) > 0)
                        Rows.Add(missing, missing, missing, missing, missing);
                    Rows.Add(obj, obj, obj, obj, obj);
                    nextAddress = obj.Instruction.OpcodeAddress.Offset(_InstructionSet.Size(obj.Instruction.Opcode));
                }
            }
        }

        public void Clear()
        {
            _Routine = null;
            _Labels = null;
            Rows.Clear();
            Invalidate();
        }

        private void CellFormattingFunc(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.Value == null)
                return;

            var instructionMetrics = (InstructionMetrics)e.Value;
            if (instructionMetrics.Ordinal == -1)
            {
                e.Value = string.Empty;
                e.FormattingApplied = true;
            }
            else switch (Columns[e.ColumnIndex].Name)
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
                base.Paint(graphics, clipBounds, cellBounds, rowIndex, cellState,
                           value:null, formattedValue:null, errorText:null, 
                           cellStyle, advancedBorderStyle, paintParts);

                var instructionMetrics = (InstructionMetrics)value!;
                if (instructionMetrics.Ordinal == -1)
                {
                    graphics.DrawString("...", cellStyle.Font, new SolidBrush(cellStyle.ForeColor), cellBounds);
                    return;
                }

                var codeGridView = (CodeGridView)DataGridView!;
                var instructionSet = codeGridView._InstructionSet!;

                byte opcode = instructionMetrics.Instruction.Opcode;
                ushort operand = instructionMetrics.Instruction.Operand;
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
        private InstructionColors _InstructionStyle;
        private InstructionSet? _InstructionSet;

    }
}
