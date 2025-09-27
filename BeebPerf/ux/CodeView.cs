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
using System.Diagnostics.Eventing.Reader;
using System.Runtime.InteropServices;
using static BeebPerf.ux.CodeView;

namespace BeebPerf.ux
{
    internal class CodeView : Panel
    {
        public CodeView() : base()
        {
            _Font = new Font("Console", 9F);
            _RowHeight = _Font.Height;
        }

        public void SetCode(Routine routine, List<InstructionMetrics> instructionMetrics, Dictionary<ushort, string> labels)
        {
            _Routine = routine;
            _Labels = labels;
            _Rows.Clear();
            foreach (var instructionMetric in instructionMetrics)
                _Rows.Add(new Row(instructionMetric, _Rows.Count));
            Invalidate();
        }

        void Clear()
        {
            _Routine = null;
            _Labels = null;
            _Rows = [];
            Invalidate();
        }

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            CalculateColumnWidths();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.FillRectangle(new SolidBrush(BackColor), e.ClipRectangle);

            for (int rowIndex = 0; rowIndex < _Rows.Count; rowIndex++)
            {
                var row = _Rows[rowIndex];
                DrawRow(row, e);
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.FillRectangle(new SolidBrush(BackColor), e.ClipRectangle);
            base.OnPaint(e);
        }

        private void DrawRow(Row row, PaintEventArgs e)
        {
            int columnIndex = 0;
            int x = 0;
            int y = row.Index * _RowHeight;
            DrawInstructionAddress(row, new Rectangle(x, y, _ColumnWidths[columnIndex], _RowHeight), e);

            x += _ColumnWidths[columnIndex++];
            DrawLabel(row, new Rectangle(x, y, _ColumnWidths[columnIndex], _RowHeight), e);

            x += _ColumnWidths[columnIndex++];
            DrawInstruction(row, new Rectangle(x, y, _ColumnWidths[columnIndex], _RowHeight), e);

            x += _ColumnWidths[columnIndex++];
            DrawCycleCount(row, new Rectangle(x, y, _ColumnWidths[columnIndex], _RowHeight), e);

            x += _ColumnWidths[columnIndex++];
            DrawExecutionCount(row, new Rectangle(x, y, _ColumnWidths[columnIndex], _RowHeight), e);
        }

        private void DrawInstructionAddress(Row row, Rectangle rect, PaintEventArgs e)
        {
            string text = row.InstructionMetrics.Instruction.OpcodeAddress.ToString();
            var format = new StringFormat { LineAlignment = StringAlignment.Center };
            e.Graphics.DrawString(text, _Font!, Brushes.Black, rect, format);
        }

        private void DrawLabel(Row row, Rectangle rect, PaintEventArgs e)
        {
            if (_Labels!.TryGetValue(row.InstructionMetrics.Instruction.OpcodeAddress.Address, out var label))
            {
                var format = new StringFormat { LineAlignment = StringAlignment.Center };
                e.Graphics.DrawString(label, _Font!, Brushes.Black, rect, format);
            }
        }

        private struct Segment
        {
            public string Text;
            public Color Color;
        };

        private void DrawInstruction(Row row, Rectangle rect, PaintEventArgs e)
        {
            byte opcode = row.InstructionMetrics.Instruction.Opcode;
            ushort operand = row.InstructionMetrics.Instruction.Operand;
            string mnemonic = _MnemonicTable[opcode];
            byte opSize = _SizeTable[opcode];
            AddressMode addressMode = (AddressMode)_AddressModeTable[opcode];

            if (addressMode == AddressMode.Relative)
            {
                int branchAddress = row.InstructionMetrics.Instruction.OpcodeAddress.Address;
                operand = (ushort)unchecked(branchAddress + 2 + (sbyte)operand);
            }

            var format = new StringFormat { LineAlignment = StringAlignment.Center };

            List<Segment> segments = new();
            segments.Add(new Segment() { Text = mnemonic.PadRight(2), Color = Color.Black });

            string hexOperand = string.Empty;
            if (opSize > 1)
            {
                if (opSize == 2)
                    hexOperand = $"&{operand:X2}";
                else if (opSize == 3)
                    hexOperand = $"&{operand:X4}";

                segments.Add(new Segment() { Text = hexOperand, Color = Color.Black });

                if (addressMode != AddressMode.Immediate && 
                    _Labels!.TryGetValue(operand, out var label))
                {
                    segments.Insert(1, new Segment() { Text = label, Color = Color.Black });
                    segments.Insert(2, new Segment() { Text = "(", Color = Color.Black });
                    segments.Add(new Segment() { Text = ")", Color = Color.Black });
                }
            }

            switch (addressMode)
            {
                case AddressMode.Accumulator:
                    segments.Add(new Segment() { Text = "A", Color = Color.Black });
                    break;

                case AddressMode.Immediate:
                    segments.Insert(1, new Segment() { Text = "#", Color = Color.Black });
                    break;

                case AddressMode.ZeroPageX:
                case AddressMode.AbsoluteX:
                    segments.Add(new Segment() { Text = ",X", Color = Color.Black });
                    break;

                case AddressMode.ZeroPageY:
                case AddressMode.AbsoluteY:
                    segments.Add(new Segment() { Text = ",X", Color = Color.Black });
                    break;

                case AddressMode.Indirect:
                    segments.Insert(1, new Segment() { Text = "(", Color = Color.Black });
                    segments.Add(new Segment() { Text = ")", Color = Color.Black });
                    break;

                case AddressMode.IndirectX:
                    segments.Insert(1, new Segment() { Text = "(", Color = Color.Black });
                    segments.Add(new Segment() { Text = ",X)", Color = Color.Black });
                    break;

                case AddressMode.IndirectY:
                    segments.Insert(1, new Segment() { Text = "(", Color = Color.Black });
                    segments.Add(new Segment() { Text = "),Y", Color = Color.Black });
                    break;
                default:
                    break;
            }

            float x = rect.X;
            float y = rect.Y;
            foreach (var segment in segments)
            {
                SizeF size = e.Graphics.MeasureString(segment.Text, _Font!);
                using (Brush brush = new SolidBrush(segment.Color))
                {
                    e.Graphics.DrawString(segment.Text, _Font!, brush, x, y);
                }

                x += size.Width;
            }
        }

        private void DrawCycleCount(Row row, Rectangle rect, PaintEventArgs e)
        {
            string text = $"{row.InstructionMetrics.InclusiveCycleCount:N0}";
            var format = new StringFormat { LineAlignment = StringAlignment.Center };
            e.Graphics.DrawString(text, _Font!, Brushes.Black, rect, format);
        }

        private void DrawExecutionCount(Row row, Rectangle rect, PaintEventArgs e)
        {
            string text = $"{row.InstructionMetrics.ExecutionCount:N0}";
            var format = new StringFormat { LineAlignment = StringAlignment.Center };
            e.Graphics.DrawString(text, _Font!, Brushes.Black, rect, format);
        }

        private void CalculateColumnWidths()
        {
            int columnWidth = Width / _ColumnWidths.Length;
            for (int i = 0; i < _ColumnWidths.Length; i++)
                _ColumnWidths[i] = columnWidth;
        }

        public struct Row
        {
            public Row(InstructionMetrics instructionMetrics, int index)
            {
                InstructionMetrics = instructionMetrics;
                Index = index;
            }
            public readonly InstructionMetrics InstructionMetrics;
            public readonly int Index;
        }

        private List<Row> _Rows = new();

        private int[] _ColumnWidths = new int[_ColumnCount];
        private int _RowHeight;
        private Font? _Font;

        private const int _ColumnCount = 5;
        private Routine? _Routine;
        private Dictionary<ushort, string>? _Labels;

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
            Jam = 13        
        }

        private static readonly byte[] _AddressModeTable = [
            0,11,13,11,3,3,3,3,0,2,1,2,7,7,7,7,
            6,12,13,12,4,4,4,4,0,2,1,2,8,8,8,8,
            0,11,13,11,3,3,3,3,0,2,1,2,7,7,7,7,
            6,12,13,12,4,4,4,4,0,2,1,2,8,8,8,8,
            0,11,13,11,3,3,3,3,0,2,1,2,7,7,7,7,
            6,12,13,12,4,4,4,4,0,2,1,2,8,8,8,8,
            0,11,13,11,3,3,3,3,0,2,1,2,7,7,7,7,
            6,12,13,12,4,4,4,4,0,2,1,2,8,8,8,8,
            0,11,13,11,3,3,3,3,0,2,1,2,7,7,7,7,
            6,12,13,12,4,4,4,4,0,2,1,2,8,8,8,8,
            0,11,13,11,3,3,3,3,0,2,1,2,7,7,7,7,
            6,12,13,12,4,4,4,4,0,2,1,2,8,8,8,8,
            0,11,13,11,3,3,3,3,0,2,1,2,7,7,7,7,
            6,12,13,12,4,4,4,4,0,2,1,2,8,8,8,8,
            0,11,13,11,3,3,3,3,0,2,1,2,7,7,7,7,
            6,12,13,12,4,4,4,4,0,2,1,2,8,8,8,8 ];

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
