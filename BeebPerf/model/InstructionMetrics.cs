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

namespace BeebPerf.model
{
    public class InstructionMetrics : IComparable<InstructionMetrics>, IEquatable<InstructionMetrics>
    {
        public InstructionMetrics(CoreInstruction instruction, int ordinal)
        {
            Instruction = instruction;
            Ordinal = ordinal;
        }

        public int CompareTo(InstructionMetrics? other)
        {
            int result = Instruction.OpcodeAddress.CompareTo(other!.Instruction.OpcodeAddress);
            if (result != 0) return result;

            result = Ordinal.CompareTo(other.Ordinal);
            if (result != 0) return result;

            result = Instruction.Opcode.CompareTo(other!.Instruction.Opcode);
            if (result != 0) return result;

            return Instruction.Operand.CompareTo(other!.Instruction.Operand);
        }

        public bool Equals(InstructionMetrics? other)
        {
            return (Instruction.Equals(other!.Instruction) &&
                Ordinal.Equals(other!.Ordinal));
        }

        public CoreInstruction Instruction;
        public int Ordinal;
        public int ExecutionCount;
        public int BranchCount;
        public int InclusiveCycleCount;
        public bool CodeModified;
        public bool TailCall;
    }
}