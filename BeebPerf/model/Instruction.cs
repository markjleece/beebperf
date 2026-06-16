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
    // 
    // Represents an individual instruction, IRQ, NMI, or display
    // event read from the .perf file. Each instruction is stored
    // as a 12‑byte packed structure within a single large array
    // containing all recorded events.
    //
    public enum InstructionType
    {
        Instruction = 0,
        IRQ = 1,
        NMI = 2,
        FrameStart = 3
    }

    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public struct Instruction
    {
        public static void CheckSizeOf()
        {
            if (Marshal.SizeOf<Instruction>() != 16)
                throw new InvalidOperationException();
        }

        public bool IsInstruction => (Type == InstructionType.Instruction);
        public bool IsIRQ => (Type == InstructionType.IRQ);
        public bool IsNMI => (Type == InstructionType.NMI);
        public bool IsFrameStart => (Type == InstructionType.FrameStart);

        public InstructionType Type
        {
            get => (InstructionType)_Type;
            set => _Type = (byte)value;
        }

        public int CycleCount
        {
            get => _CycleCount;
            set => _CycleCount = (byte)value;
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

        public byte StackPointer
        {
            get
            {
                Debug.Assert(IsInstruction && (
                        Opcode == 0x00/*BRK*/ || Opcode == 0x20/*JSR*/ ||
                        Opcode == 0x40/*RTI*/ || Opcode == 0x60/*RTS*/ ||
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
                        Opcode == 0x00/*BRK*/ || Opcode == 0x20/*JSR*/ ||
                        Opcode == 0x40/*RTI*/ || Opcode == 0x60/*RTS*/ ||
                        Opcode == 0x48/*PHA*/ || Opcode == 0x68/*PLA*/ ||
                        Opcode == 0x08/*PHP*/ || Opcode == 0x28/*PLP*/ ||
                        Opcode == 0xDA/*PHX*/ || Opcode == 0xFA/*PLX*/ ||
                        Opcode == 0x5A/*PHY*/ || Opcode == 0x7A/*PLY*/ ||
                        Opcode == 0x9A/*TXS*/ || Opcode == 0x9B/*TAS*/));
                _StackPointer = value;
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

        public byte OffsetCycleCount
        {
            get
            {
                Debug.Assert(IsFrameStart);
                return _OffsetCycleCount;
            }
            set
            {
                Debug.Assert(IsFrameStart);
                _OffsetCycleCount = value;
            }
        }

        public ushort StartAddress
        {
            get
            {
                Debug.Assert(IsFrameStart);
                return _StartAddress;
            }
            set
            {
                Debug.Assert(IsFrameStart);
                _StartAddress = value;
            }
        }

        public ushort DisplayScanline
        {
            get
            {
                Debug.Assert(IsFrameStart);
                return _DisplayScanline;
            }
            set
            {
                Debug.Assert(IsFrameStart);
                _DisplayScanline = value;
            }
        }

        public byte DisplayFlags
        {
            get 
            {
                Debug.Assert(IsFrameStart);
                return _DisplayFlags; 
            }
            set
            {
                Debug.Assert(IsFrameStart);
                _DisplayFlags = value;
            }
        }

        public int DisplayField
        {
            get
            {
                Debug.Assert(IsFrameStart);
                return (_DisplayFlags & 0x1);
            }
        }

        public bool SplitScreen
        {
            get
            {
                Debug.Assert(IsFrameStart);
                return ((_DisplayFlags & 0x2) == 0x2);
            }
        }

        // common fields
        [FieldOffset(0)] private byte _Type;
        [FieldOffset(1)] private byte _CycleCount;

        // instruction fields
        [FieldOffset(2)] private ushort _OpcodeAddress;
        [FieldOffset(4)] private byte _OpcodeAddressPage;

        [FieldOffset(5)] private byte _Opcode;
        [FieldOffset(6)] private ushort _Operand;

        [FieldOffset(8)] private ushort _MemoryAddress;
        [FieldOffset(10)] private byte _MemoryAddressPage;

        [FieldOffset(12)] private byte _MemoryReadValue;
        [FieldOffset(13)] private byte _MemoryWriteValue;

        [FieldOffset(14)] private byte _StackPointer;

        // interrupt and instruction fields
        [FieldOffset(8)] private ushort _DestinationAddress;    // overrides _MemoryAddress
        [FieldOffset(10)] private byte _DestinationAddressPage; // overrides _MemoryAddressPage

        [FieldOffset(11)] private byte _ReturnAddressPage;
        [FieldOffset(12)] private ushort _ReturnAddress;        // overrides _MemoryReadValue & _MemoryWriteValue

        // CRTC vertical counter reset fields
        [FieldOffset(4)] private byte _OffsetCycleCount;        // overrides _OpcodeAddressPage
        [FieldOffset(5)] private byte _DisplayFlags;            // overrides _Opcode
        [FieldOffset(6)] private ushort _StartAddress;          // overrides _Operand 
        [FieldOffset(8)] private ushort _DisplayScanline;       // overrides _MemoryAddress

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
            else if (IsFrameStart)
            {
                return $"FRAME-START-EVENT";
            }
            else
            {
                return $"UNKNOWN INSTRUCTION TYPE";
            }
        }
    }
}