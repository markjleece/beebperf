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

namespace BeebPerf.model
{
    public enum CallType
    {
        None = 0,
        JSR = 1,
        IRQ = 2,
        NMI = 3,
        BRK = 4,
        TailCall = 5,
        FallThrough = 6,
    }

    public class CallStack : IEquatable<CallStack>
    {
        public CallStack()
        {
            Routine = new Routine();
        }

        public CallStack(Routine routine, CanonicalAddress returnAddress, byte stackPointer, CallType type, StackFrame? parent)
        {
            CallType = type;
            Parent = parent;
            Routine = routine;
            StartAddress = routine.StartAddress;
            ReturnAddress = returnAddress;
            StackPointer = stackPointer;
        }

        public bool Equals(CallStack? other)
        {
            if (other is null) return false;

            var self = this;
            var peer = other;

            while (self is not null && peer is not null)
            {
                if (!self.StartAddress.Equals(peer.StartAddress))
                    return false;

                if (self.CallType == CallType.IRQ || self.CallType == CallType.NMI || self.CallType == CallType.BRK)
                    return true;

                self = self.Parent;
                peer = peer.Parent;
            }

            return true;
        }

        public override int GetHashCode()
        {
            int hashCode = 0;
            for (var callStack = this; callStack != null; callStack = callStack.Parent)
            {
                hashCode = unchecked(hashCode * 31 + callStack.StartAddress.GetHashCode());
                if (callStack.CallType == CallType.IRQ || callStack.CallType == CallType.NMI || callStack.CallType == CallType.BRK)
                    break;
            }
            return hashCode;
        }

        public int Depth
        {
            get
            {
                int depth = 0;
                for (var callStack = this; callStack != null; callStack = callStack.Parent)
                {
                    depth++;
                    if (callStack.CallType == CallType.IRQ || callStack.CallType == CallType.NMI || callStack.CallType == CallType.BRK)
                        break;
                }
                return depth;
            }
        }

        public int FullDepth
        {
            get
            {
                int depth = 0;
                for (var callStack = this; callStack != null; callStack = callStack.Parent)
                    depth++;
                return depth;
            }
        }

        public CallType CallType;
        public Routine Routine;
        public CanonicalAddress StartAddress;
        public CanonicalAddress ReturnAddress;
        public byte StackPointer;
        public StackFrame? Parent;
    }
}