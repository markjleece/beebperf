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
    public class StackFrame : CallStack
    {
        public StackFrame() : base() 
        {
        }

        public StackFrame(Routine routine, CallType type, StackFrame? parent) : base(routine, type, parent) 
        {
            routine.StackFrames.Add(this);
        }

        public StackFrame GetFirstInstructionStackFrame()
        {
            var current = this;
            while (current.Children.Count > 0)
            {
                var firstChild = current.Children[0];
                if (firstChild.FirstInstructionIndex >= current.FirstInstructionIndex)
                    break;
                current = firstChild;
            }
            return current;
        }

        public StackFrame GetLastInstructionStackFrame()
        {
            var current = this;
            while (current.Children.Count > 0)
            {
                var lastChild = current.Children[^1];
                if (lastChild.LastInstructionIndex <= current.LastInstructionIndex)
                    break;
                current = lastChild;
            }
            return current;
        }

        public List<StackFrame> Children = new();
        public int FirstInstructionIndex;
        public int LastInstructionIndex;
        public int StartCycleCount;
        public int EndCycleCount;
    }
}