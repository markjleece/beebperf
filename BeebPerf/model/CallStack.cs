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
    // 
    // Represents a call-stack where an instance chains to its 
    // parent call-stack.  Each call-stack instance has an
    // associated call-type (how it was invoked), routine,
    // start and return addresses.
    //
    // The identity of a call-stack is defined by the chain of
    // start addresses, up until an IRQ, NMI, BRK, or the root
    // is encountered.
    //
    public class CallStack : IEquatable<CallStack>
    {
        public CallStack()
        {
            Routine = new Routine();
        }

        public CallStack(Routine routine, CanonicalAddress returnAddress, byte returnStackPointer, CallType callType, StackFrame? parent)
        {
            CallType = callType;
            Parent = parent;
            Routine = routine;
            StartAddress = routine.StartAddress;
            ReturnAddress = returnAddress;
            ReturnStackPointer = returnStackPointer;
            Depth = (parent == null || callType == CallType.IRQ || callType == CallType.NMI || callType == CallType.BRK) ? 0 : parent.Depth + 1;
            FullDepth = (parent == null) ? 0 : parent.FullDepth + 1;
            CalcHash();
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
            return _HashCode;
        }

        protected void CalcHash()
        {
            int hashCode = StartAddress.GetHashCode();
            if (Parent != null && CallType != CallType.IRQ && CallType != CallType.NMI && CallType != CallType.BRK)
                hashCode = unchecked(Parent._HashCode * 31 + hashCode);
            _HashCode = hashCode;
        }

        public CallType CallType { get; private set; }
        public StackFrame? Parent { get; private set; }
        public int Depth { get; private set; }
        public int FullDepth { get; private set; }
        public Routine Routine;
        public CanonicalAddress StartAddress;
        public CanonicalAddress ReturnAddress;
        public byte ReturnStackPointer;

        private int _HashCode;
    }
}