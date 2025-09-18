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
    public enum CallType
    {
        ISR,
        JSR,
        TailCall,
        FallThrough,
        Unknown
    }

    public class CallStack : IEquatable<CallStack>
    {
        public CallStack()
        {
            Routine = new Routine();
        }

        public CallStack(Routine routine, CallType type, StackFrame? parent)
        {
            Type = type;
            Parent = parent;
            Routine = routine;
            CanonicalAddress = routine.StartAddress;
        }

        public bool Equals(CallStack? other)
        {
            if (other is null) return false;

            var self = this;
            var peer = other;

            while (self is not null && peer is not null)
            {
                if (!self.CanonicalAddress.Equals(peer.CanonicalAddress))
                    return false;

                if (self.Type == CallType.ISR)
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
                hashCode = unchecked(hashCode * 31 + callStack.CanonicalAddress.GetHashCode());
                if (callStack.Type == CallType.ISR)
                    break;
            }
            return hashCode;
        }

        public CallType Type;
        public readonly Routine Routine;
        public readonly CanonicalAddress CanonicalAddress;
        public StackFrame? Parent;
    }
}