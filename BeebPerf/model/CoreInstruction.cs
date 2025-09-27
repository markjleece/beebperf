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

namespace BeebPerf
{
    public struct CoreInstruction : IEquatable<CoreInstruction>, IComparable<CoreInstruction>
    {
        public CoreInstruction(ref Instruction instruction)
        {
            OpcodeAddress = instruction.OpcodeAddress;
            Opcode = instruction.Opcode;
            Operand = instruction.Operand;
        }

        public bool Equals(CoreInstruction other)
        {
            return
                OpcodeAddress.Equals(other.OpcodeAddress) &&
                Opcode == other.Opcode &&
                Operand == other.Operand;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(OpcodeAddress, Opcode, Operand);
        }

        public int CompareTo(CoreInstruction other)
        {
            int result = OpcodeAddress.CompareTo(other.OpcodeAddress);
            if (result == 0)
            {
                result = Opcode.CompareTo(other.Opcode);
                if (result == 0)
                    result = Operand.CompareTo(other.Operand);
            }
            return result;
        }

        public readonly CanonicalAddress OpcodeAddress;
        public readonly byte Opcode;
        public readonly ushort Operand;
    }
}