// --------------------------------------------------------------
// BeebPerf - A BBC Micro Profiler
//
// Copyright (C) 2026  Mark John Leece
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

namespace BeebPerf.model
{
    public class InstructionSet
    {
        //
        // Represents the 6502 and 65C02 instruction sets, providing
        // opcode‑based query methods such as instruction size,
        // branch classification, memory‑access characteristics,
        // addressing mode, and other related properties.
        //
        public InstructionSet(CPUType cpu)
        {
            CPU = cpu;

            _BranchTable = [
             // 0 1 2 3 4 5 6 7 8 9 a b c d e f
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 0
                1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 1
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 2
                1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 3
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 4
                1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 5
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 6
                1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 7
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 8
                1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 9
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // a
                1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // b
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // c
                1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // d
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // e
                1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0  // f
            ];

            _BranchOrJumpTable = [
             // 0 1 2 3 4 5 6 7 8 9 a b c d e f
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 0
                1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 1
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 2
                1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 3
                0,0,0,0,0,0,0,0,0,0,0,0,1,0,0,0, // 4
                1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 5
                0,0,0,0,0,0,0,0,0,0,0,0,1,0,0,0, // 6
                1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 7
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 8
                1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 9
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // a
                1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // b
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // c
                1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // d
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // e
                1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0];// f

            if (cpu == CPUType._65C02)
                _BranchOrJumpTable[0x7C] = 1; // add JMP (abs,X)

            byte[] sizeTable6502 = [
             // 0 1 2 3 4 5 6 7 8 9 a b c d e f
                1,2,1,2,2,2,2,2,1,2,1,2,3,3,3,3, // 0
                2,2,1,2,2,2,2,2,1,3,1,3,3,3,3,3, // 1
                3,2,1,2,2,2,2,2,1,2,1,2,3,3,3,3, // 2
                2,2,1,2,2,2,2,2,1,3,1,3,3,3,3,3, // 3
                1,2,1,2,2,2,2,2,1,2,1,2,3,3,3,3, // 4
                2,2,1,2,2,2,2,2,1,3,1,3,3,3,3,3, // 5
                1,2,1,2,2,2,2,2,1,2,1,2,3,3,3,3, // 6
                2,2,1,2,2,2,2,2,1,3,1,3,3,3,3,3, // 7
                2,2,2,2,2,2,2,2,1,2,1,2,3,3,3,3, // 8
                2,2,1,2,2,2,2,2,1,3,1,3,3,3,3,3, // 9
                2,2,2,2,2,2,2,2,1,2,1,2,3,3,3,3, // a
                2,2,1,2,2,2,2,2,1,3,1,3,3,3,3,3, // b
                2,2,2,2,2,2,2,2,1,2,1,2,3,3,3,3, // c
                2,2,1,2,2,2,2,2,1,3,1,3,3,3,3,3, // d
                2,2,2,2,2,2,2,2,1,2,1,2,3,3,3,3, // e
                2,2,1,2,2,2,2,2,1,3,1,3,3,3,3,3  // f
            ];

            byte[] sizeTable65C02 = [
             // 0 1 2 3 4 5 6 7 8 9 a b c d e f
                1,2,1,1,2,2,2,1,1,2,1,1,3,3,3,1, // 0
                2,2,2,1,2,2,2,1,1,3,1,1,3,3,3,1, // 1
                3,2,1,1,2,2,2,1,1,2,1,1,3,3,3,1, // 2
                2,2,2,1,2,2,2,1,1,3,1,1,3,3,3,1, // 3
                1,2,1,1,1,2,2,1,1,2,1,1,3,3,3,1, // 4
                2,2,2,1,1,2,2,1,1,3,1,1,1,3,3,1, // 5
                1,2,1,1,2,2,2,1,1,2,1,1,3,3,3,1, // 6
                2,2,2,1,2,2,2,1,1,3,1,1,3,3,3,1, // 7
                2,2,1,1,2,2,2,1,1,2,1,1,3,3,3,1, // 8
                2,2,2,1,2,2,2,1,1,3,1,1,3,3,3,1, // 9
                2,2,2,1,2,2,2,1,1,2,1,1,3,3,3,1, // a
                2,2,2,1,2,2,2,1,1,3,1,1,3,3,3,1, // b
                2,2,1,1,2,2,2,1,1,2,1,1,3,3,3,1, // c
                2,2,2,1,1,2,2,1,1,3,1,1,1,3,3,1, // d
                2,2,1,1,2,2,2,1,1,2,1,1,3,3,3,1, // e
                2,2,2,1,1,2,2,1,1,3,1,1,1,3,3,1  // f
            ];

            byte[] memoryAccessTable6502 = [
             // 0 1 2 3 4 5 6 7 8 9 a b c d e f
                0,1,0,3,0,1,3,3,0,0,0,0,0,1,3,3, // 0 none
                0,1,0,3,0,1,3,3,0,1,0,3,0,1,3,3, // 1 memory read
                0,1,0,3,1,1,3,3,0,0,0,0,1,1,3,3, // 2 memory write
                0,1,0,3,0,1,3,3,0,1,0,3,0,1,3,3, // 3 memory read & write
                0,1,0,3,0,1,3,3,0,0,0,0,0,1,3,3, // 4 
                0,1,0,3,0,1,3,3,0,1,0,3,0,1,3,3, // 5 
                0,1,0,3,0,1,3,3,0,0,0,0,0,1,3,3, // 6
                0,1,0,3,0,1,3,3,0,1,0,3,0,1,3,3, // 7
                0,2,0,2,2,2,2,2,0,0,0,0,2,2,2,2, // 8
                0,2,0,2,2,2,2,2,0,2,0,2,2,2,2,2, // 9
                0,1,0,1,1,1,1,1,0,0,0,0,1,1,1,1, // a
                0,1,0,1,1,1,1,1,0,1,0,1,1,1,1,1, // b
                0,1,0,3,1,1,3,3,0,0,0,0,1,1,3,3, // c
                0,1,0,3,0,1,3,3,0,1,0,3,0,1,3,3, // d
                0,1,0,3,1,1,3,3,0,0,0,0,1,1,3,3, // e
                0,1,0,3,0,1,3,3,0,1,0,3,0,1,3,3  // f
            ];

            byte[] memoryAccessTable65C02 = [
             // 0 1 2 3 4 5 6 7 8 9 a b c d e f
                0,1,0,0,3,1,3,0,0,0,0,0,3,1,3,0, // 0 none
                0,1,1,0,3,1,3,0,0,1,0,0,3,1,3,0, // 1 memory read
                0,1,0,0,1,1,3,0,0,0,0,0,1,1,3,0, // 2 memory write
                0,1,1,0,1,1,3,0,0,1,0,0,1,1,3,0, // 3 memory read & write
                0,1,0,0,0,1,3,0,0,0,0,0,0,1,3,0, // 4 
                0,1,1,0,0,1,3,0,0,1,0,0,0,1,3,0, // 5 
                0,1,0,0,2,1,3,0,0,0,0,0,0,1,3,0, // 6
                0,1,1,0,2,1,3,0,0,1,0,0,0,1,3,0, // 7
                0,2,0,0,2,2,2,0,0,0,0,0,2,2,2,0, // 8
                0,2,2,0,2,2,2,0,0,2,0,0,2,2,2,0, // 9
                0,1,0,0,1,1,1,0,0,0,0,0,1,1,1,0, // a
                0,1,1,0,1,1,1,0,0,1,0,0,1,1,1,0, // b
                0,1,0,0,1,1,3,0,0,0,0,0,1,1,3,0, // c
                0,1,1,0,0,1,3,0,0,1,0,0,0,1,3,0, // d
                0,1,0,0,1,1,3,0,0,0,0,0,1,1,3,0, // e
                0,1,1,0,0,1,3,0,0,1,0,0,0,1,3,0  // f
            ];

            byte[] addressModeTable6502 = [
             // 0 1 2 3 4 5 6 7 8 9 a b c d e f
                0,11,13,11,4,4,4,4,0,2,1,2,5,5,5,5,  // 0 Implied
                3,12,13,12,6,6,6,6,0,9,0,9,8,8,8,8,  // 1 Accumulator
                5,11,13,11,4,4,4,4,0,2,1,2,5,5,5,5,  // 2 Immediate
                3,12,13,12,6,6,6,6,0,9,0,9,8,8,8,8,  // 3 Relative
                0,11,13,11,4,4,4,4,0,2,1,2,5,5,5,5,  // 4 ZeroPage
                3,12,13,12,6,6,6,6,0,9,0,9,8,8,8,8,  // 5 Absolute
                0,11,13,11,4,4,4,4,0,2,1,2,10,5,5,5, // 6 ZeroPageX
                3,12,13,12,6,6,6,6,0,9,0,9,8,8,8,8,  // 7 ZeroPageY
                2,11,2,11,4,4,4,4,0,2,0,2,5,5,5,5,   // 8 AbsoluteX
                3,12,13,12,6,6,7,7,0,9,0,9,8,8,9,9,  // 9 AbsoluteY
                2,11,2,11,4,4,4,4,0,2,0,2,5,5,5,5,   // a Indirect 10
                3,12,13,12,6,6,7,7,0,9,0,9,8,8,9,9,  // b IndirectX 11
                2,11,2,11,4,4,4,4,0,2,0,2,5,5,5,5,   // c IndirectY 12
                3,12,13,12,6,6,6,6,0,9,0,9,8,8,8,8,  // d Invalid 13
                2,11,2,11,4,4,4,4,0,2,0,2,5,5,5,5,   // e
                3,12,13,12,6,6,6,6,0,9,0,9,8,8,8,8,  // f
            ];

            byte[] addressModeTable65C02 = [
             // 0 1 2 3 4 5 6 7 8 9 a b c d e f
                0,11,13,13,4,4,4,4,0,2,1,13,5,5,5,3,   // 0 Implied
                3,12,10,13,4,6,6,4,0,9,1,13,5,8,8,3,   // 1 Accumulator
                5,11,13,13,4,4,4,4,0,2,1,13,5,5,5,3,   // 2 Immediate
                3,12,10,13,6,6,6,4,0,9,1,13,8,8,8,3,   // 3 Relative
                0,11,13,13,13,4,4,4,0,2,1,13,5,5,5,3,  // 4 ZeroPage
                3,12,10,13,13,6,6,4,0,9,0,13,13,8,8,3, // 5 Absolute
                0,11,13,13,4,4,4,4,0,2,1,13,10,5,5,3,  // 6 ZeroPageX
                3,12,10,13,6,6,6,4,0,9,0,13,11,8,8,3,  // 7 ZeroPageY
                3,11,13,13,4,4,4,4,0,2,0,13,5,5,5,3,   // 8 AbsoluteX
                3,12,10,13,6,6,7,4,0,9,0,13,5,8,8,3,   // 9 AbsoluteY
                2,11,2,13,4,4,4,4,0,2,0,13,5,5,5,3,    // a Indirect 10
                3,12,10,13,6,6,7,4,0,9,0,13,8,8,9,3,   // b IndirectX 11
                2,11,13,13,4,4,4,4,0,2,0,0,5,5,5,3,    // c IndirectY 12
                3,12,10,13,13,6,6,4,0,9,0,0,13,8,8,3,  // d Invalid 13
                2,11,13,13,4,4,4,4,0,2,0,13,5,5,5,3,   // e
                3,12,10,13,13,6,6,4,0,9,0,13,13,8,8,3  // f
            ];

            byte[] loadOrStoreTable6502 = [
             // 0 1 2 3 4 5 6 7 8 9 a b c d e f
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 0 
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 1 LDA
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 2 LDX
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 3 LDY
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 4 STA
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 5 STX
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 6 STY
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 7
                0,4,0,0,6,4,5,0,0,0,0,0,6,4,5,0, // 8
                0,4,0,0,6,4,5,0,0,4,0,0,0,4,0,0, // 9
                3,1,2,0,3,1,2,0,0,1,0,0,3,1,2,0, // a
                0,1,0,0,3,1,2,0,0,1,0,0,3,1,2,0, // b
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // c
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // d
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // e
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0  // f
           ];

            byte[] loadOrStoreTable65C02 = [
             // 0 1 2 3 4 5 6 7 8 9 a b c d e f
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 0 
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 1 LDA
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 2 LDX
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 3 LDY
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 4 STA
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 5 STX
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 6 STY
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 7
                0,4,0,0,6,4,5,0,0,0,0,0,6,4,5,0, // 8
                0,4,4,0,6,4,5,0,0,4,0,0,0,4,0,0, // 9
                3,1,2,0,3,1,2,0,0,1,0,0,3,1,2,0, // a
                0,1,1,0,3,1,2,0,0,1,0,0,3,1,2,0, // b
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // c
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // d
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // e
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0  // f
            ];

            byte[] modifiesStackPointerTable6502 = [
             // 0 1 2 3 4 5 6 7 8 9 a b c d e f
                1,0,0,0,0,0,0,0,1,0,0,0,0,0,0,0, // 0 No
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 1 Yes
                1,0,0,0,0,0,0,0,1,0,0,0,0,0,0,0, // 2
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 3
                1,0,0,0,0,0,0,0,1,0,0,0,0,0,0,0, // 4
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 5
                1,0,0,0,0,0,0,0,1,0,0,0,0,0,0,0, // 6
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 7
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 8
                0,0,0,0,0,0,0,0,0,0,1,1,0,0,0,0, // 9
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // a
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // b
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // c
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // d
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // e
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0  // f
            ];

            byte[] modifiesStackPointerTable65C02 = [
             // 0 1 2 3 4 5 6 7 8 9 a b c d e f
                1,0,0,0,0,0,0,0,1,0,0,0,0,0,0,0, // 0 No
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 1 Yes
                1,0,0,0,0,0,0,0,1,0,0,0,0,0,0,0, // 2
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 3
                1,0,0,0,0,0,0,0,1,0,0,0,0,0,0,0, // 4
                0,0,0,0,0,0,0,0,0,0,1,0,0,0,0,0, // 5
                1,0,0,0,0,0,0,0,1,0,0,0,0,0,0,0, // 6
                0,0,0,0,0,0,0,0,0,0,1,0,0,0,0,0, // 7
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 8
                0,0,0,0,0,0,0,0,0,0,1,0,0,0,0,0, // 9
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // a
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // b
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // c
                0,0,0,0,0,0,0,0,0,0,1,0,0,0,0,0, // d
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // e
                0,0,0,0,0,0,0,0,0,0,1,0,0,0,0,0  // f
            ];

            string[] mnemonicTable6502 = [
                "BRK","ORA","JAM","SLO","NOP","ORA","ASL","SLO","PHP","ORA","ASL","ANC","NOP","ORA","ASL","SLO",
                "BPL","ORA","JAM","SLO","NOP","ORA","ASL","SLO","CLC","ORA","NOP","SLO","NOP","ORA","ASL","SLO",
                "JSR","AND","JAM","RLA","BIT","AND","ROL","RLA","PLP","AND","ROL","ANC","BIT","AND","ROL","RLA",
                "BMI","AND","JAM","RLA","NOP","AND","ROL","RLA","SEC","AND","NOP","RLA","NOP","AND","ROL","RLA",
                "RTI","EOR","JAM","SRE","NOP","EOR","LSR","SRE","PHA","EOR","LSR","ALR","JMP","EOR","LSR","SRE",
                "BVC","EOR","JAM","SRE","NOP","EOR","LSR","SRE","CLI","EOR","NOP","SRE","NOP","EOR","LSR","SRE",
                "RTS","ADC","JAM","RRA","NOP","ADC","ROR","RRA","PLA","ADC","ROR","ARR","JMP","ADC","ROR","RRA",
                "BVS","ADC","JAM","RRA","NOP","ADC","ROR","RRA","SEI","ADC","NOP","RRA","NOP","ADC","ROR","RRA",
                "NOP","STA","NOP","SAX","STY","STA","STX","SAX","DEY","NOP","TXA","ANE","STY","STA","STX","SAX",
                "BCC","STA","JAM","SHA","STY","STA","STX","SAX","TYA","STA","TXS","TAS","SHY","STA","SHX","SHA",
                "LDY","LDA","LDX","LAX","LDY","LDA","LDX","LAX","TAY","LDA","TAX","LXA","LDY","LDA","LDX","LAX",
                "BCS","LDA","JAM","LAX","LDY","LDA","LDX","LAX","CLV","LDA","TSX","LAS","LDY","LDA","LDX","LAX",
                "CPY","CMP","NOP","DCP","CPY","CMP","DEC","DCP","INY","CMP","DEX","SBX","CPY","CMP","DEC","DCP",
                "BNE","CMP","JAM","DCP","NOP","CMP","DEC","DCP","CLD","CMP","NOP","DCP","NOP","CMP","DEC","DCP",
                "CPX","SBC","NOP","ISC","CPX","SBC","INC","ISC","INX","SBC","NOP","USBC","CPX","SBC","INC","ISC",
                "BEQ","SBC","JAM","ISC","NOP","SBC","INC","ISC","SED","SBC","NOP","ISC","NOP","SBC","INC","ISC"
            ];

            string[] mnemonicTable65C02 = [
                "BRK","ORA","???","???","TSB","ORA","ASL","RMB0","PHP","ORA","ASL","???","TSB","ORA","ASL","BBR0",
                "BPL","ORA","ORA","???","TRB","ORA","ASL","RMB1","CLC","ORA","INC","???","TRB","ORA","ASL","BBR1",
                "JSR","AND","???","???","BIT","AND","ROL","RMB2","PLP","AND","ROL","???","BIT","AND","ROL","BBR2",
                "BMI","AND","AND","???","BIT","AND","ROL","RMB3","SEC","AND","DEC","???","BIT","AND","ROL","BBR3",
                "RTI","EOR","???","???","???","EOR","LSR","RMB4","PHA","EOR","LSR","???","JMP","EOR","LSR","BBR4",
                "BVC","EOR","EOR","???","???","EOR","LSR","RMB5","CLI","EOR","PHY","???","???","EOR","LSR","BBR5",
                "RTS","ADC","???","???","STZ","ADC","ROR","RMB6","PLA","ADC","ROR","???","JMP","ADC","ROR","BBR6",
                "BVS","ADC","ADC","???","STZ","ADC","ROR","RMB7","SEI","ADC","PLY","???","JMP","ADC","ROR","BBR7",
                "BRA","STA","???","???","STY","STA","STX","SMB0","DEY","BIT","TXA","???","STY","STA","STX","BBS0",
                "BCC","STA","STA","???","STY","STA","STX","SMB1","TYA","STA","TXS","???","STZ","STA","STZ","BBS1",
                "LDY","LDA","LDX","???","LDY","LDA","LDX","SMB2","TAY","LDA","TAX","???","LDY","LDA","LDX","BBS2",
                "BCS","LDA","LDA","???","LDY","LDA","LDX","SMB3","CLV","LDA","TSX","???","LDY","LDA","LDX","BBS3",
                "CPY","CMP","???","???","CPY","CMP","DEC","SMB4","INY","CMP","DEX","WAI","CPY","CMP","DEC","BBS4",
                "BNE","CMP","CMP","???","???","CMP","DEC","SMB5","CLD","CMP","PHX","STP","???","CMP","DEC","BBS5",
                "CPX","SBC","???","???","CPX","SBC","INC","SMB6","INX","SBC","NOP","???","CPX","SBC","INC","BBS6",
                "BEQ","SBC","SBC","???","???","SBC","INC","SMB7","SED","SBC","PLX","???","???","SBC","INC","BBS7"
            ];

            if (cpu == CPUType._6502)
            {
                _SizeTable = sizeTable6502;
                _MemoryAccessTable = memoryAccessTable6502;
                _AddressModeTable = addressModeTable6502;
                _LoadOrStoreTable = loadOrStoreTable6502;
                _ModifiesStackPointerTable = modifiesStackPointerTable6502;
                _MnemonicTable = mnemonicTable6502;
            }
            else
            {
                Debug.Assert(cpu == CPUType._65C02);
                _SizeTable = sizeTable65C02;
                _MemoryAccessTable = memoryAccessTable65C02;
                _AddressModeTable = addressModeTable65C02;
                _LoadOrStoreTable = loadOrStoreTable65C02;
                _ModifiesStackPointerTable = modifiesStackPointerTable65C02;
                _MnemonicTable = mnemonicTable65C02;
            }

            Debug.Assert(_BranchOrJumpTable.Length == 256);
            Debug.Assert(_BranchTable.Length == 256);
            Debug.Assert(_SizeTable.Length == 256);
            Debug.Assert(_MemoryAccessTable.Length == 256);
            Debug.Assert(_AddressModeTable.Length == 256);
            Debug.Assert(_LoadOrStoreTable.Length == 256);
            Debug.Assert(_MnemonicTable.Length == 256);
        }

        public enum AddressingModeType : byte
        {
            Implied = 0,
            Accumulator = 1,
            Immediate = 2,
            Relative = 3,
            ZeroPage = 4,
            Absolute = 5,

            IndexedOrIndirect = 6,
            ZeroPageX = 6,
            ZeroPageY = 7,
            AbsoluteX = 8,
            AbsoluteY = 9,
            Indirect = 10,
            IndirectX = 11,
            IndirectY = 12,
            Invalid = 13
        }

        public enum MemoryAccessType : byte
        {
            None = 0,
            Read = 0x1,
            Write = 0x2,
            ReadWrite = 0x3
        }

        public enum LoadOrStoreType : byte
        {
            Neither = 0,
            LDA = 1,
            LDX = 2,
            LDY = 3,
            STA = 4,
            STX = 5,
            STY = 6
        }

        public int Size(byte opcode)
        {
            return _SizeTable[opcode];
        }

        public bool IsBranch(byte opcode)
        {
            return _BranchTable[opcode] != 0;
        }

        public bool IsBranchOrJump(byte opcode)
        {
            return _BranchOrJumpTable[opcode] != 0;
        }

        public MemoryAccessType MemoryAccess(byte opcode)
        {
            return (MemoryAccessType)_MemoryAccessTable[opcode];
        }

        public LoadOrStoreType LoadOrStore(byte opcode)
        {
            return (LoadOrStoreType)_LoadOrStoreTable[opcode];
        }

        public AddressingModeType AddressingMode(byte opcode)
        {
            return (AddressingModeType)_AddressModeTable[opcode];
        }

        public bool ModifiesStackPointer(byte opcode)
        {
            return _ModifiesStackPointerTable[opcode] != 0;
        }

        public string Mnemonic(byte opcode)
        {
            return _MnemonicTable[opcode];
        }

        public readonly CPUType CPU;

        private readonly byte[] _BranchOrJumpTable;
        private readonly byte[] _BranchTable;
        private readonly byte[] _SizeTable;
        private readonly byte[] _MemoryAccessTable;
        private readonly byte[] _AddressModeTable;
        private readonly byte[] _LoadOrStoreTable;
        private readonly byte[] _ModifiesStackPointerTable;
        private readonly string[] _MnemonicTable;
    }
}
