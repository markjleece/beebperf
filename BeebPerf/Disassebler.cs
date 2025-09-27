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

using System.Diagnostics;

namespace BeebPerf
{
    public class Disassembler
    {
        public void Disassemble(Routine routine, List<InstructionMetrics> instructionMetrics, Dictionary<ushort, string> labels)
        {
            foreach (var instructionMetric in instructionMetrics)
            {
                Debug.WriteLine(
                    $"{instructionMetric.Instruction.OpcodeAddress}, " +
                    $"0x{instructionMetric.Instruction.Opcode:X2}, " +
                    $"0x{instructionMetric.Instruction.Operand:X4}, " +
                    $"Total CPU: {instructionMetric.InclusiveCycleCount:N0}, " +
                    $"Execution ExecutionCount: {instructionMetric.ExecutionCount:N0}, " +
                    $"Ordinal: {instructionMetric.Ordinal}");
            }

            // build address -> instruction count dictionary
            var overlappingInstructions = new SortedDictionary<CanonicalAddress, int>();
            foreach (var instructionMetric in instructionMetrics)
            {
                int instructionSize = GetOpcodeSize(instructionMetric.Instruction.Opcode);

                // get available space
                int maxDepth = 0;
                CanonicalAddress address = instructionMetric.Instruction.OpcodeAddress;
                for (int i = 0; i < instructionSize; i++)
                {
                    int depth = 0;
                    overlappingInstructions.TryGetValue(address, out depth);
                    if (depth > maxDepth) maxDepth = depth;
                    address = address.Offset(1);
                }

                // set alternate index
                instructionMetric.AlternateIndex = maxDepth;

                // populate space
                maxDepth++;
                address = instructionMetric.Instruction.OpcodeAddress;
                for (int i = 0; i < instructionSize; i++)
                {
                    overlappingInstructions[address] = maxDepth;
                    address = address.Offset(1);
                }
            }

            var disassemblyLines = new List<DisassemblyLine>(instructionMetrics.Count);
            foreach (var instructionMetric in instructionMetrics)
            {
                ref CoreInstruction instruction = ref instructionMetric.Instruction;
                disassemblyLines.Add(new DisassemblyLine()
                {
                    OpcodeAddress = instruction.OpcodeAddress.ToString(),
                    Label = labels.TryGetValue(instruction.OpcodeAddress.Address, out var label) ? label : string.Empty,
                    OpcodeMnemonic = GetOpcodeMnemonic(instruction.Opcode),
                    Operand = FormatOperand(instruction.OpcodeAddress, instruction.Opcode, instruction.Operand, labels),
                    ExecutionCount = $"{instructionMetric.ExecutionCount:N0}",
                    InclusiveCycleCount = $"{instructionMetric.InclusiveCycleCount:N0}"
                });
            }
        }

        public class DisassemblyLine
        {
            public string OpcodeAddress = string.Empty;
            public string Label = string.Empty;
            public string OpcodeMnemonic = string.Empty;
            public string Operand = string.Empty;
            public string ExecutionCount = string.Empty;
            public string InclusiveCycleCount = string.Empty;
        }

        private string FormatOperand(CanonicalAddress opcodeAddress, byte opcode, ushort operand, Dictionary<ushort, string> labels)
        {
            var addressMode = GetOpcodeAddressMode(opcode);
            if (addressMode == AddressMode.Relative)
            {
                operand = (ushort)unchecked(opcodeAddress.Address + 2 + (sbyte)operand);
            }

            string value = string.Empty;
            int size = GetOpcodeSize(opcode);
            if (size > 1)
            {
                if (size == 2)
                    value = "&{opcode:X2}";
                else if (size == 3)
                    value = "&{opcode:X4}";

                if (labels.TryGetValue(operand, out var label))
                    value = "{label} ({value})";
            }

            return addressMode switch
            {
                AddressMode.Implied => $"A",
                AddressMode.Immediate => $"#{value}",
                AddressMode.Relative => $"{value}",
                AddressMode.Absolute => $"{value}",
                AddressMode.ZeroPage => $"{value}",
                AddressMode.ZeroPageX => $"{value},X",
                AddressMode.ZeroPageY => $"{value},Y",
                AddressMode.AbsoluteX => $"{value},X",
                AddressMode.AbsoluteY => $"{value},Y",
                AddressMode.Indirect => $"({value})",
                AddressMode.IndexedIndirectX => $"({value},X)",
                AddressMode.IndirectIndexedY => $"({value}),Y",
                AddressMode.Illegal => "???",
                _ => "???"
            };
        }

        private string GetOpcodeMnemonic(byte opcode)
        {
            return _MnemonicUpperCaseTbl6502[opcode];
        }

        private int GetOpcodeSize(byte opcode)
        {
            return _SizeTbl6502[opcode];
        }

        private AddressMode GetOpcodeAddressMode(byte opcode)
        {
            return (AddressMode)_AddressModeTbl6502[opcode];
        }

        public enum AddressMode : byte
        {
            Implied = 0,
            Immediate = 1,
            Relative = 2,
            Absolute = 3,
            ZeroPage = 4,
            ZeroPageX = 5,
            ZeroPageY = 6,
            AbsoluteX = 7,
            AbsoluteY = 8,
            Indirect = 9,
            IndexedIndirectX = 10,
            IndirectIndexedY = 11,
            Illegal = 12
        }

        private static readonly byte[] _SizeTbl6502 = new byte[256] {
            1,2,1,2,2,2,2,2,1,2,1,2,3,3,3,3,
            2,2,1,2,2,2,2,2,1,3,1,3,3,3,3,3,
            3,2,1,2,2,2,2,2,1,2,1,2,3,3,3,3,
            2,2,1,2,2,2,2,2,1,3,1,3,3,3,3,3,
            1,2,1,2,2,2,2,2,1,2,1,2,3,3,3,3,
            2,2,1,2,2,2,2,2,1,3,1,3,3,3,3,3,
            1,2,1,2,2,2,2,2,1,2,1,2,3,3,3,3,
            2,2,1,2,2,2,2,2,1,3,1,3,3,3,3,3,
            2,2,2,2,2,2,2,2,1,2,1,2,3,3,3,3,
            2,2,2,2,2,2,2,2,1,3,1,3,3,3,3,3,
            2,2,2,2,2,2,2,2,1,2,1,2,3,3,3,3,
            2,2,2,2,2,2,2,2,1,3,1,3,3,3,3,3,
            2,2,2,2,2,2,2,2,1,2,1,2,3,3,3,3,
            2,2,2,2,2,2,2,2,1,3,1,3,3,3,3,3,
            2,2,2,2,2,2,2,2,1,2,1,2,3,3,3,3,
            2,2,2,2,2,2,2,2,1,3,1,3,3,3,3,3 };

        private static readonly byte[] _AddressModeTbl6502 = new byte[256] {
            0,10,12,10,4,4,4,4,0,1,0,1,3,3,3,3,
            2,11,12,11,5,5,5,5,0,8,0,8,7,7,7,7,
            3,10,12,10,4,4,4,4,0,1,0,1,3,3,3,3,
            2,11,12,11,5,5,5,5,0,8,0,8,7,7,7,7,
            0,10,12,10,4,4,4,4,0,1,0,1,9,3,3,3,
            2,11,12,11,5,5,5,5,0,8,0,8,9,7,7,7,
            0,10,12,10,4,4,4,4,0,1,0,1,9,3,3,3,
            2,11,12,11,5,5,5,5,0,8,0,8,9,7,7,7,
            1,10,1,10,4,4,4,4,0,1,0,1,3,3,3,3,
            2,11,12,11,5,5,5,5,0,8,0,8,3,3,3,3,
            1,10,1,10,4,4,4,4,0,1,0,1,3,3,3,3,
            2,11,12,11,5,5,5,5,0,8,0,8,3,3,3,3,
            1,10,1,10,4,4,4,4,0,1,0,1,3,3,3,3,
            2,11,12,11,5,5,5,5,0,8,0,8,3,3,3,3,
            1,10,1,10,4,4,4,4,0,1,0,1,3,3,3,3,
            2,11,12,11,5,5,5,5,0,8,0,8,3,3,3,3 };

        private static readonly string[] _MnemonicUpperCaseTbl6502 = {
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
            "BEQ","SBC","KIL","ISB","NOP","SBC","INC","ISB","SED","SBC","NOP","ISB","NOP","SBC","INC","ISB" };
    }

    /*
    order CoreInstructions
    identify overlapping instructions


    generate text
    generate column based layout
    format text
    generate hot regions
    questions
        how to we detect overlapping instructions?
            - we look for instructions that share the same space, and put them in different columns (larger instructions on right)
            - we could show the alternative instructions in different columns, with their respective metrics
        how do we represent alterative instructions?
            - animate through alternates (animation is good)
            - how do we deal with metrics? let's just add them all together
        how do we represent execution count
            - indentation?
            - separate column?
            - colors?
        how do we denote tail-calls
            - show <-- (tail call) - clicking navigates
        how do we denote interrupts
            - show <-- (interrupt) - clicking navigates
        how do we denote fall through
            - show all metrics with 'fall-through to XXXX'

    Metrics can be absolute, or averages (we can toggle these)

    Columns:
        total CPU
        execution count
        cycle count

        address
        label
        opcode+operand *
    */
}
