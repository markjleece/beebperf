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
using System.Reflection.Emit;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;

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

            InstructionStyle = new InstructionColors()
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

        public void SetCode(Routine routine, List<InstructionMetrics> instructionMetrics, Dictionary<ushort, string> labels)
        {
            var unknown = new InstructionMetrics(new CoreInstruction(), ordinal:-1);

            _Routine = routine;
            _Labels = labels;
            Rows.Clear();
            if (instructionMetrics.Count > 0)
            {
                CanonicalAddress nextAddress = instructionMetrics[0].Instruction.OpcodeAddress;
                foreach (var obj in instructionMetrics)
                {
                    if (obj.Instruction.OpcodeAddress.CompareTo(nextAddress) > 0)
                        Rows.Add(unknown, unknown, unknown, unknown, unknown);
                    Rows.Add(obj, obj, obj, obj, obj);
                    nextAddress = obj.Instruction.OpcodeAddress.Offset(_SizeTable[obj.Instruction.Opcode]);
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
                    e.Value = InstructionFormatString(instructionMetrics.Instruction);
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

        private string InstructionFormatString(CoreInstruction instruction)
        {
            byte opcode = instruction.Opcode;
            ushort operand = instruction.Operand;

            AddressMode addressMode = (AddressMode)_AddressModeTable[opcode];
            if (addressMode == AddressMode.Relative)
            {
                int branchAddress = instruction.OpcodeAddress.Address;
                operand = (ushort)unchecked(branchAddress + 2 + (sbyte)operand);
            }

            string formattedOperand= string.Empty;
            byte opSize = _SizeTable[opcode];
            if (opSize > 1)
            {
                if (opSize == 2)
                    formattedOperand = $"&{operand:X2}";
                else if (opSize == 3)
                    formattedOperand = $"&{operand:X4}";

                if (addressMode != AddressMode.Immediate && _Labels!.TryGetValue(operand, out var label))
                    formattedOperand = $"{label} ({formattedOperand})";
            }

            string mnemonic = _MnemonicTable[opcode];

            switch (addressMode)
            {
                case AddressMode.Implied:
                    return $"{mnemonic}";

                case AddressMode.Accumulator:
                    return $"{mnemonic} A";

                case AddressMode.Immediate:
                    return $"{mnemonic} #{formattedOperand}";

                case AddressMode.ZeroPage:
                case AddressMode.Relative:
                case AddressMode.Absolute:
                    return $"{mnemonic} {formattedOperand}";

                case AddressMode.ZeroPageX:
                case AddressMode.AbsoluteX:
                    return $"{mnemonic} {formattedOperand},X";

                case AddressMode.ZeroPageY:
                case AddressMode.AbsoluteY:
                    return $"{mnemonic} {formattedOperand},Y";

                case AddressMode.Indirect:
                    return $"{mnemonic} ({formattedOperand})";

                case AddressMode.IndirectX:
                    return $"{mnemonic} ({formattedOperand},X)";

                case AddressMode.IndirectY:
                    return $"{mnemonic} ({formattedOperand}),Y";

                default:
                    return "???";
            }
        }

        private Routine? _Routine;
        private Dictionary<ushort, string>? _Labels;

        public class InstructionColors
        {
            public Color MnemonicColor;
            public Color AddressColor;
            public Color LabelColor;
            public Color PunctuationColor;
        }

        private InstructionColors InstructionStyle;

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

                byte opcode = instructionMetrics.Instruction.Opcode;
                ushort operand = instructionMetrics.Instruction.Operand;
                string mnemonic = _MnemonicTable[opcode];
                byte opSize = _SizeTable[opcode];
                AddressMode addressMode = (AddressMode)_AddressModeTable[opcode];

                if (addressMode == AddressMode.Relative)
                {
                    int branchAddress = instructionMetrics.Instruction.OpcodeAddress.Address;
                    operand = (ushort)unchecked(branchAddress + 2 + (sbyte)operand);
                }

                var format = new StringFormat { LineAlignment = StringAlignment.Center };

                var codeGridView = (CodeGridView)DataGridView!;

                List<Segment> segments = new();
                var colors = codeGridView.InstructionStyle;

                segments.Add(new Segment() { Text = mnemonic.PadRight(5), Color = colors.MnemonicColor });

                string hexOperand = string.Empty;
                if (opSize > 1)
                {
                    if (opSize == 2)
                        hexOperand = $"&{operand:X2}";
                    else if (opSize == 3)
                        hexOperand = $"&{operand:X4}";

                    segments.Add(new Segment() { Text = hexOperand, Color = colors.AddressColor });

                    if (addressMode != AddressMode.Immediate &&
                        codeGridView!._Labels!.TryGetValue(operand, out var label))
                    {
                        segments.Insert(1, new Segment() { Text = label, Color = colors.LabelColor });
                        segments.Insert(2, new Segment() { Text = " (", Color = colors.PunctuationColor });
                        segments.Add(new Segment() { Text = ")", Color = colors.PunctuationColor });
                    }
                }

                switch (addressMode)
                {
                    case AddressMode.Accumulator:
                        segments.Add(new Segment() { Text = "A", Color = colors.MnemonicColor });
                        break;

                    case AddressMode.Immediate:
                        segments.Insert(1, new Segment() { Text = "#", Color = colors.PunctuationColor });
                        break;

                    case AddressMode.ZeroPageX:
                    case AddressMode.AbsoluteX:
                        segments.Add(new Segment() { Text = ",X", Color = colors.PunctuationColor });
                        break;

                    case AddressMode.ZeroPageY:
                    case AddressMode.AbsoluteY:
                        segments.Add(new Segment() { Text = ",X", Color = colors.PunctuationColor });
                        break;

                    case AddressMode.Indirect:
                        segments.Insert(1, new Segment() { Text = "(", Color = colors.PunctuationColor });
                        segments.Add(new Segment() { Text = ")", Color = colors.PunctuationColor });
                        break;

                    case AddressMode.IndirectX:
                        segments.Insert(1, new Segment() { Text = "(", Color = colors.PunctuationColor });
                        segments.Add(new Segment() { Text = ",X)", Color = colors.PunctuationColor });
                        break;

                    case AddressMode.IndirectY:
                        segments.Insert(1, new Segment() { Text = "(", Color = colors.PunctuationColor });
                        segments.Add(new Segment() { Text = "),Y", Color = colors.PunctuationColor });
                        break;
                    default:
                        break;
                }

                int x = cellBounds.X;
                int y = cellBounds.Y;

                foreach (var segment in segments)
                {
                    format = StringFormat.GenericTypographic;

                    Size size = TextRenderer.MeasureText(segment.Text, 
                        cellStyle.Font,
                        new Size(int.MaxValue, int.MaxValue),
                        TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
                    using (Brush brush = new SolidBrush(segment.Color))
                    {
                        int heightAdjust = (cellBounds.Height - size.Height) / 2;
                        graphics.DrawString(segment.Text, cellStyle.Font, brush, x, y + heightAdjust);
                    }

                    x += size.Width;
                }
            }
            private struct Segment
            {
                public string Text;
                public Color Color;
            };
        }

        public enum AddressMode : byte
        {
            Implied = 0,
            Accumulator = 1,
            Immediate = 2,
            ZeroPage = 3,
            ZeroPageX = 4,
            ZeroPageY = 5,
            Relative = 6,
            Absolute = 7,
            AbsoluteX = 8,
            AbsoluteY = 9,
            Indirect = 10,
            IndirectX = 11,
            IndirectY = 12,
            Invalid = 13
        }

        private static readonly byte[] _AddressModeTable = [
            0,11,13,11,3,3,3,3,0,2,1,2,7,7,7,7,
            6,12,13,12,4,4,4,4,0,9,0,9,8,8,8,8,
            7,11,13,11,3,3,3,3,0,2,1,2,7,7,7,7,
            6,12,13,12,4,4,4,4,0,9,0,9,8,8,8,8,
            0,11,13,11,3,3,3,3,0,2,1,2,7,7,7,7,
            6,12,13,12,4,4,4,4,0,9,0,9,8,8,8,8,
            0,11,13,11,3,3,3,3,0,2,1,2,10,7,7,7,
            6,12,13,12,4,4,4,4,0,9,0,9,10,8,8,8,
            2,11,2,11,3,3,3,3,0,2,1,2,7,7,7,7, 
            6,12,13,12,4,4,4,4,0,9,0,9,8,8,8,8,
            2,11,2,11,3,3,3,3,0,2,1,2,7,7,7,7, 
            6,12,13,12,4,4,4,4,0,9,0,9,8,8,8,8,
            2,11,2,11,3,3,3,3,0,2,1,2,7,7,7,7, 
            6,12,13,12,4,4,4,4,0,9,0,9,8,8,8,8,
            2,11,2,11,3,3,3,3,0,2,1,2,7,7,7,7, 
            6,12,13,12,4,4,4,4,0,9,0,9,8,8,8,8 ];

        private static readonly byte[] _SizeTable = [
            1,2,1,2,2,2,2,2,1,2,1,2,3,3,3,3,
            2,2,1,2,2,2,2,2,1,3,1,3,3,3,3,3,
            3,2,1,2,2,2,2,2,1,2,1,2,3,3,3,3,
            2,2,1,2,2,2,2,2,1,3,1,3,3,3,3,3,
            1,2,1,2,2,2,2,2,1,2,1,2,3,3,3,3,
            2,2,1,2,2,2,2,2,1,3,1,3,3,3,3,3,
            1,2,1,2,2,2,2,2,1,2,1,2,3,3,3,3,
            2,2,1,2,2,2,2,2,1,3,1,3,3,3,3,3,
            2,2,2,2,2,2,2,2,1,2,1,2,3,3,3,3,
            2,2,1,2,2,2,2,2,1,3,1,3,3,3,3,3,
            2,2,2,2,2,2,2,2,1,2,1,2,3,3,3,3,
            2,2,1,2,2,2,2,2,1,3,1,3,3,3,3,3,
            2,2,2,2,2,2,2,2,1,2,1,2,3,3,3,3,
            2,2,1,2,2,2,2,2,1,3,1,3,3,3,3,3,
            2,2,2,2,2,2,2,2,1,2,1,2,3,3,3,3,
            2,2,1,2,2,2,2,2,1,3,1,3,3,3,3,3 ];

        private static readonly string[] _MnemonicTable = [
            "BRK","ORA","KIL","SLO","NOP","ORA","ASL","SLO","PHP","ORA","ASL","ANC","NOP","ORA","ASL","SLO",
            "BPL","ORA","KIL","SLO","NOP","ORA","ASL","SLO","CLC","ORA","NOP","SLO","NOP","ORA","ASL","SLO",
            "JSR","AND","KIL","RLA","BIT","AND","ROL","RLA","PLP","AND","ROL","ANC","BIT","AND","ROL","RLA",
            "BMI","AND","KIL","RLA","NOP","AND","ROL","RLA","SEC","AND","NOP","RLA","NOP","AND","ROL","RLA",
            "RTI","EOR","KIL","SRE","NOP","EOR","LSR","SRE","PHA","EOR","LSR","ALR","JMP","EOR","LSR","SRE",
            "BVC","EOR","KIL","SRE","NOP","EOR","LSR","SRE","CLI","EOR","NOP","SRE","NOP","EOR","LSR","SRE",
            "RTS","ADC","KIL","RRA","NOP","ADC","ROR","RRA","PLA","ADC","ROR","ARR","JMP","ADC","ROR","RRA",
            "BVS","ADC","KIL","RRA","NOP","ADC","ROR","RRA","SEI","ADC","NOP","RRA","NOP","ADC","ROR","RRA",
            "NOP","STA","NOP","SAX","STY","STA","STX","SAX","DEY","NOP","TXA","XAA","STY","STA","STX","SAX",
            "BCC","STA","KIL","AHX","STY","STA","STX","SAX","TYA","STA","TXS","TAS","SHY","STA","SHX","AHX",
            "LDY","LDA","LDX","LAX","LDY","LDA","LDX","LAX","TAY","LDA","TAX","LAX","LDY","LDA","LDX","LAX",
            "BCS","LDA","KIL","LAX","LDY","LDA","LDX","LAX","CLV","LDA","TSX","LAS","LDY","LDA","LDX","LAX",
            "CPY","CMP","NOP","DCP","CPY","CMP","DEC","DCP","INY","CMP","DEX","AXS","CPY","CMP","DEC","DCP",
            "BNE","CMP","KIL","DCP","NOP","CMP","DEC","DCP","CLD","CMP","NOP","DCP","NOP","CMP","DEC","DCP",
            "CPX","SBC","NOP","ISB","CPX","SBC","INC","ISB","INX","SBC","NOP","SBC","CPX","SBC","INC","ISB",
            "BEQ","SBC","KIL","ISB","NOP","SBC","INC","ISB","SED","SBC","NOP","ISB","NOP","SBC","INC","ISB" ];
    }
}
