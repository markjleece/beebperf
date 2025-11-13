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
using System.Runtime.InteropServices;

namespace BeebPerf.model
{
    [StructLayout(LayoutKind.Explicit)]
    public struct Instruction
    {
        static Instruction()
        {
            if (Marshal.SizeOf<Instruction>() != 12) throw new InvalidOperationException();
        }

        public bool IsInterrupt
        {
            get => (_FlagsAndCycleCount & 0x80) != 0;
            set => _FlagsAndCycleCount = (byte)((value ? 0x80 : 0x00) | (_FlagsAndCycleCount & 0x0F));
        }

        public bool IsInstruction
        {
            get => (_FlagsAndCycleCount & 0x40) != 0;
            set => _FlagsAndCycleCount = (byte)((value ? 0x40 : 0x00) | (_FlagsAndCycleCount & 0x0F));
        }

        public bool IsBeginDisplayEvent
        {
            get => (_FlagsAndCycleCount & 0x20) != 0;
            set => _FlagsAndCycleCount = (byte)((value ? 0x20 : 0x00) | (_FlagsAndCycleCount & 0x0F));
        }

        public int CycleCount
        {
            get => (_FlagsAndCycleCount & 0x0F);
            set => _FlagsAndCycleCount = (byte)((_FlagsAndCycleCount & 0xF0) | (value & 0x0F));
        }

        public byte StackPointer
        {
            get => _StackPointer;
            set => _StackPointer = value;
        }

        public bool NonMaskableInterrupt
        {
            get
            {
                Debug.Assert(IsInterrupt);
                return (_NonMaskableInterrupt != 0);
            }
            set
            {
                Debug.Assert(IsInterrupt);
                _NonMaskableInterrupt = (byte)(value ? 1 : 0);
            }
        }

        public CanonicalAddress ISRAddress
        {
            get
            {
                Debug.Assert(IsInterrupt);
                return new CanonicalAddress(_ISRAddress, (MemoryPage)_ISRAddressPage);
            }
            set
            {
                Debug.Assert(IsInterrupt);
                _ISRAddress = value.Address;
                _ISRAddressPage = (byte)value.Page;
            }
        }

        public CanonicalAddress InterruptedAddress
        {
            get
            {
                Debug.Assert(IsInterrupt);
                return new CanonicalAddress(_InterruptedAddress, (MemoryPage)_InterruptedAddressPage);
            }
            set
            {
                Debug.Assert(IsInterrupt);
                _InterruptedAddress = value.Address;
                _InterruptedAddressPage = (byte)value.Page;
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

        public CanonicalAddress DestinationAddress
        {
            get
            {
                Debug.Assert(IsInstruction);
                return new CanonicalAddress(_DestinationAddress, (MemoryPage)_DestinationAddressPage);
            }
            set
            {
                Debug.Assert(IsInstruction);
                _DestinationAddress = value.Address;
                _DestinationAddressPage = (byte)value.Page;
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
        [FieldOffset(0)] private byte _FlagsAndCycleCount;
        [FieldOffset(1)] private byte _StackPointer;

        // interrupt fields
        [FieldOffset(2)] private byte _NonMaskableInterrupt;

        [FieldOffset(8)] private ushort _ISRAddress;
        [FieldOffset(3)] private byte _ISRAddressPage;

        [FieldOffset(10)] private ushort _InterruptedAddress;
        [FieldOffset(4)] private byte _InterruptedAddressPage;

        // instruction fields
        [FieldOffset(2)] private byte _Opcode;
        [FieldOffset(6)] private ushort _Operand;

        [FieldOffset(8)] private ushort _OpcodeAddress;
        [FieldOffset(3)] private byte _OpcodeAddressPage;

        [FieldOffset(10)] private ushort _DestinationAddress;
        [FieldOffset(4)] private byte _DestinationAddressPage;

        [FieldOffset(10)] private ushort _MemoryAddress;
        [FieldOffset(4)] private byte _MemoryAddressPage;

        [FieldOffset(1)] private byte _MemoryReadValue;
        [FieldOffset(5)] private byte _MemoryWriteValue;
    }
}