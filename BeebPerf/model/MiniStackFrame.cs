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
    public class MiniStackFrame // Same as model.StackFrame but with only the information we need for routine identification
    {
        public CanonicalAddress StartAddress { get; }
        public CanonicalAddress ReturnAddress { get; }
        public byte StackPointer { get; }
        CallType CallType { get; }

        public MiniStackFrame(CallType callType, CanonicalAddress startAddress, CanonicalAddress returnAddress, byte stackPointer)
        {
            CallType = callType;
            StartAddress = startAddress;
            ReturnAddress = returnAddress;
            StackPointer = stackPointer;
        }

        public override string ToString()
        {
            return $"CallType: {CallType}, Start: {StartAddress}, Return: {ReturnAddress}, SP: {StackPointer}";
        }
    }
}