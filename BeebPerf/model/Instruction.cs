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
using System.Runtime.InteropServices;

namespace BeebPerf.model
{
    public enum InstructionType
    {
        Instruction = 0x00,
        IRQ = 0x10,
        NMI = 0x20,
        BeginDisplayEvent = 0x30
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct Instruction
    {
        static Instruction()
        {
            if (Marshal.SizeOf<Instruction>() != 12) throw new InvalidOperationException();
        }

        public bool IsInstruction => (Type == InstructionType.Instruction);
        public bool IsIRQ => (Type == InstructionType.IRQ);
        public bool IsNMI => (Type == InstructionType.NMI);
        public bool IsBeginDisplayEvent => (Type == InstructionType.BeginDisplayEvent);

        public InstructionType Type
        {
            get => (InstructionType)(_TypeAndCycleCount & 0xF0);
            set => _TypeAndCycleCount = (byte)(((byte)value & 0xF0) | (_TypeAndCycleCount & 0x0F));
        }

        public int CycleCount
        {
            get => (_TypeAndCycleCount & 0x0F);
            set => _TypeAndCycleCount = (byte)((_TypeAndCycleCount & 0xF0) | (value & 0x0F));
        }

        public CanonicalAddress DestinationAddress
        {
            get
            {
                Debug.Assert(IsNMI || IsIRQ || (IsInstruction &&
                     (Opcode == 0x00/*BRK*/ || Opcode == 0x60/*RTS*/ || 
                      Opcode == 0x40/*RTI*/ || Opcode == 0x20/*JSR*/ || 
                      Opcode == 0x4C/*JMP abs*/ ||
                      Opcode == 0x6C/*JMP (abs)*/ || Opcode == 0x7C/*JMP (abs,X)*/ ||
                      (Opcode & 0x1F) == 0x10)/*B??*/));
                return new CanonicalAddress(_DestinationAddress, (MemoryPage)_DestinationAddressPage);
            }
            set
            {
                Debug.Assert(IsNMI || IsIRQ || (IsInstruction &&
                     (Opcode == 0x00/*BRK*/ || Opcode == 0x60/*RTS*/ || Opcode == 0x40/*RTI*/ ||
                      Opcode == 0x20/*JSR abs*/ || Opcode == 0x4C/*JMP abs*/ ||
                      Opcode == 0x6C/*JMP (abs)*/ || Opcode == 0x7C/*JMP (abs,X)*/ ||
                      (Opcode & 0x1F) == 0x10)/*B??*/));
                _DestinationAddress = value.Address;
                _DestinationAddressPage = (byte)value.Page;
            }
        }

        public CanonicalAddress ReturnAddress
        {
            get
            {
                Debug.Assert(IsNMI || IsIRQ || (IsInstruction && (Opcode == 0x00/*BRK*/ || Opcode == 0x20/*JSR*/)));
                return new CanonicalAddress(_ReturnAddress, (MemoryPage)_ReturnAddressPage);
            }
            set
            {
                Debug.Assert(IsNMI || IsIRQ || (IsInstruction && (Opcode == 0x00/*BRK*/ || Opcode == 0x20/*JSR*/)));
                _ReturnAddress = value.Address;
                _ReturnAddressPage = (byte)value.Page;
            }
        }

        public byte StackValue
        {
            get
            {
                Debug.Assert(IsInstruction &&
                    (Opcode == 0x48/*PHA*/ || Opcode == 0x68/*PLA*/ ||
                     Opcode == 0xDA/*PHX*/ || Opcode == 0xFA/*PLX*/ ||
                     Opcode == 0x5A/*PHY*/ || Opcode == 0x7A/*PLY*/));
                return _StackValue;
            }
            set
            {
                Debug.Assert(IsInstruction &&
                    (Opcode == 0x48/*PHA*/ || Opcode == 0x68/*PLA*/ ||
                     Opcode == 0xDA/*PHX*/ || Opcode == 0xFA/*PLX*/ ||
                     Opcode == 0x5A/*PHY*/ || Opcode == 0x7A/*PLY*/));
                _StackValue = value;
            }
        }

        public byte StackPointer
        {
            get
            {
                Debug.Assert(IsInstruction && (
                        Opcode == 0x48/*PHA*/ || Opcode == 0x68/*PLA*/ ||
                        Opcode == 0x08/*PHP*/ || Opcode == 0x28/*PLP*/ ||
                        Opcode == 0xDA/*PHX*/ || Opcode == 0xFA/*PLX*/ ||
                        Opcode == 0x5A/*PHY*/ || Opcode == 0x7A/*PLY*/ ||
                        Opcode == 0x9A/*TXS*/ || Opcode == 0x9B/*TAS*/));
                 return _StackPointer;
            }
            set
            {
                Debug.Assert(IsInstruction && (
                        Opcode == 0x48/*PHA*/ || Opcode == 0x68/*PLA*/ ||
                        Opcode == 0x08/*PHP*/ || Opcode == 0x28/*PLP*/ ||
                        Opcode == 0xDA/*PHX*/ || Opcode == 0xFA/*PLX*/ ||
                        Opcode == 0x5A/*PHY*/ || Opcode == 0x7A/*PLY*/ ||
                        Opcode == 0x9A/*TXS*/ || Opcode == 0x9B/*TAS*/));
                _StackPointer = value;
            }
        }

        public int DisplayField
        {
            get
            {
                Debug.Assert(IsBeginDisplayEvent);
                return _DisplayField;
            }
            set
            {
                Debug.Assert(IsBeginDisplayEvent && (value == 0 || value == 1));
                _DisplayField = (byte)value;
            }
        }

        public byte Opcode
        {
            get
            {
                Debug.Assert(IsInstruction);
                return _Opcode;
            }
            set
            {
                Debug.Assert(IsInstruction);
                _Opcode = value;
            }
        }

        public ushort Operand
        {
            get
            {
                Debug.Assert(IsInstruction);
                return _Operand;
            }
            set
            {
                Debug.Assert(IsInstruction);
                _Operand = value;
            }
        }

        public CanonicalAddress OpcodeAddress
        {
            get
            {
                Debug.Assert(IsInstruction);
                return new CanonicalAddress(_OpcodeAddress, (MemoryPage)_OpcodeAddressPage);
            }
            set
            {
                Debug.Assert(IsInstruction);
                _OpcodeAddress = value.Address;
                _OpcodeAddressPage = (byte)value.Page;
            }
        }

        public CanonicalAddress MemoryAddress
        {
            get
            {
                Debug.Assert(IsInstruction);
                return new CanonicalAddress(_MemoryAddress, (MemoryPage)_MemoryAddressPage);
            }
            set
            {
                Debug.Assert(IsInstruction);
                _MemoryAddress = value.Address;
                _MemoryAddressPage = (byte)value.Page;
            }
        }

        public byte MemoryReadValue
        {
            get
            {
                Debug.Assert(IsInstruction);
                return _MemoryReadValue;
            }
            set
            {
                Debug.Assert(IsInstruction);
                _MemoryReadValue = value;
            }
        }

        public byte MemoryWriteValue
        {
            get
            {
                Debug.Assert(IsInstruction);
                return _MemoryWriteValue;
            }
            set
            {
                Debug.Assert(IsInstruction);
                _MemoryWriteValue = value;
            }
        }

        // common fields
        [FieldOffset(0)] private byte _TypeAndCycleCount;

        // display event fields
        [FieldOffset(1)] private byte _DisplayField;

        // instruction fields
        [FieldOffset(1)] private byte _OpcodeAddressPage;
        [FieldOffset(2)] private ushort _OpcodeAddress;

        [FieldOffset(6)] private byte _Opcode;
        [FieldOffset(4)] private ushort _Operand;

        [FieldOffset(7)] private byte _MemoryAddressPage;
        [FieldOffset(8)] private ushort _MemoryAddress;

        [FieldOffset(10)] private byte _MemoryReadValue;
        [FieldOffset(11)] private byte _MemoryWriteValue;

        [FieldOffset(10)] private byte _StackValue;            // overrides _MemoryReadValue
        [FieldOffset(11)] private byte _StackPointer;          // overrides _MemoryWriteValue

        // interrupt and instruction fields
        [FieldOffset(7)] private byte _DestinationAddressPage; // overrides _MemoryAddressPage
        [FieldOffset(8)] private ushort _DestinationAddress;   // overrides _MemoryAddress

        [FieldOffset(1)] private byte _ReturnAddressPage;      // overrides _OpcodeAddressPage
        [FieldOffset(10)] private ushort _ReturnAddress;       // overrides _MemoryReadValue & _MemoryWriteValue

        public string ToString(InstructionSet instructionSet)
        {
            if (IsInstruction)
            {
                string mnemonic = instructionSet.Mnemonic(Opcode);
                string operand = (instructionSet.Size(Opcode) == 3) ? $"&{Operand:X4}" : $"&{Operand:X2}";
                string address = OpcodeAddress.ToString();
                InstructionSet.AddressingModeType addressMode = instructionSet.AddressingMode(Opcode);
                return addressMode switch
                {
                    InstructionSet.AddressingModeType.Accumulator => $"{address} {mnemonic} A",
                    InstructionSet.AddressingModeType.Implied => $"{address} {mnemonic}",
                    InstructionSet.AddressingModeType.ZeroPage => $"{address} {mnemonic} {operand}",
                    InstructionSet.AddressingModeType.Absolute => $"{address} {mnemonic} {operand}",
                    InstructionSet.AddressingModeType.Immediate => $"{address} {mnemonic} #{operand}",
                    InstructionSet.AddressingModeType.Relative => $"{address} {mnemonic} {operand}",
                    InstructionSet.AddressingModeType.ZeroPageX => $"{address} {mnemonic} {operand}, X",
                    InstructionSet.AddressingModeType.AbsoluteX => $"{address} {mnemonic} {operand}, X",
                    InstructionSet.AddressingModeType.ZeroPageY => $"{address} {mnemonic} {operand}, Y",
                    InstructionSet.AddressingModeType.AbsoluteY => $"{address} {mnemonic} {operand}, Y",
                    InstructionSet.AddressingModeType.Indirect => $"{address} {mnemonic} ({operand}), Y",
                    InstructionSet.AddressingModeType.IndirectX => $"{address} {mnemonic} ({operand}, X)",
                    InstructionSet.AddressingModeType.IndirectY => $"{address} {mnemonic} ({operand}), Y",
                    _ => string.Empty
                };
            }
            else if (IsNMI)
            {
                return $"MASKABLE INTERRUPT to {DestinationAddress}";
            }
            else if (IsIRQ)
            {
                return $"NON-MASKABLE INTERRUPT to {DestinationAddress}";
            }
            else if (IsBeginDisplayEvent)
            {
                return $"BEGIN DISPLAY EVENT";
            }
            else
            {
                return $"UNKNOWN INSTRUCTION TYPE";
            }
        }
    }
}