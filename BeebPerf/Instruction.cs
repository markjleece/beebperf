using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BeebPerf
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
                return new CanonicalAddress(_ISRAddress, _ISRAddressPage);
            }
            set
            {
                Debug.Assert(IsInterrupt);
                _ISRAddress = value.Address;
                _ISRAddressPage = value.Page;
            }
        }

        public CanonicalAddress InterruptedAddress
        {
            get
            {
                Debug.Assert(IsInterrupt);
                return new CanonicalAddress(_InterruptedAddress, _InterruptedAddressPage);
            }
            set
            {
                Debug.Assert(IsInterrupt);
                _InterruptedAddress = value.Address;
                _InterruptedAddressPage = value.Page;
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
                return new CanonicalAddress(_OpcodeAddress, _OpcodeAddressPage);
            }
            set
            {
                Debug.Assert(IsInstruction);
                _OpcodeAddress = value.Address;
                _OpcodeAddressPage = value.Page;
            }
        }

        public CanonicalAddress DestinationAddress
        {
            get
            {
                Debug.Assert(IsInstruction);
                return new CanonicalAddress(_DestinationAddress, _DestinationAddressPage);
            }
            set
            {
                Debug.Assert(IsInstruction);
                _DestinationAddress = value.Address;
                _DestinationAddressPage = value.Page;
            }
        }

        public CanonicalAddress MemoryAddress
        {
            get
            {
                Debug.Assert(IsInstruction);
                return new CanonicalAddress(_MemoryAddress, _MemoryAddressPage);
            }
            set
            {
                Debug.Assert(IsInstruction);
                _MemoryAddress = value.Address;
                _MemoryAddressPage = value.Page;
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