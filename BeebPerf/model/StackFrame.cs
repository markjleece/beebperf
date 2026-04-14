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

namespace BeebPerf.model
{
    //
    // Represents a stack-frame, including its start and end cycle
    // counts, first and last instruction indices, lowest and highest
    // instruction addresses, associated CPU metrics, and the
    // collection of child stack frames.
    //
    // A stack-frame also has an associated call-stack, which is 
    // inherited for implementation efficiency.
    //
    public class StackFrame : CallStack
    {
        public StackFrame() : base() 
        {
        }

        public StackFrame(Routine routine, CanonicalAddress returnAddress, byte returnStackPointer, CallType type, StackFrame? parent) : 
            base(routine, returnAddress, returnStackPointer, type, parent) 
        {
            routine.StackFrames.Add(this);
        }

        public void ClearMetrics()
        {
            CPUMetrics.Clear();
            foreach (var child in Children)
                child.ClearMetrics();
        }

        public bool IsEmpty()
        {
            return (FirstSelfInstructionIndex == -1 && LastSelfInstructionIndex == -1 && Children.Count == 0);
        }

        public override string ToString()
        {
            return "".PadLeft((FullDepth - 1) * 2, ' ') + $"Type: {CallType}, Start: {StartAddress}{Routine.Label}, Return: {ReturnAddress}, ReturnSP: {ReturnStackPointer}";
        }

        public static void ComputeInstructionIndices(StackFrame frame, int instructionCount)
        {
            int first = frame.FirstSelfInstructionIndex == -1 ? instructionCount - 1 : frame.FirstSelfInstructionIndex;
            int last = frame.LastSelfInstructionIndex == -1 ? 0 : frame.LastSelfInstructionIndex;

            foreach (var child in frame.Children)
            {
                ComputeInstructionIndices(child, instructionCount);

                first = Math.Min(first, child.FirstInstructionIndex);
                last = Math.Max(last, child.LastInstructionIndex);
            }

            frame.FirstInstructionIndex = first;
            frame.LastInstructionIndex = last;
        }

#if DEBUG && STACKFRAME_INVARIANT
        public static void AssertInvariant(StackFrame rootStackFrame, Instruction[] instructions, InstructionSet instructionSet)
        {
            int totalCycles = 0;
            for (int i = 0; i < instructions.Length; i++)
                totalCycles += instructions[i].CycleCount;

            AssertInvariant(rootStackFrame, instructions, instructionSet, totalCycles);
        }

        private static void AssertInvariant(StackFrame frame, Instruction[] instructions, InstructionSet instructionSet, int totalCycles)
        {
            // first and last self instruction indices
            if (frame.FirstSelfInstructionIndex == -1 || frame.LastSelfInstructionIndex == -1)
            {
                Debug.Assert(frame.FirstSelfInstructionIndex == -1);
                Debug.Assert(frame.LastSelfInstructionIndex == -1);
            }
            
            // first and last instruction indices
            if (frame.FirstSelfInstructionIndex != -1)
                Debug.Assert(frame.FirstInstructionIndex <= frame.FirstSelfInstructionIndex);

            if (frame.LastSelfInstructionIndex != -1)
                Debug.Assert(frame.LastInstructionIndex >= frame.LastSelfInstructionIndex);

            Debug.Assert(frame.FirstInstructionIndex >= 0 && frame.FirstInstructionIndex <= instructions.Length - 1);
            Debug.Assert(frame.LastInstructionIndex >= 0 && frame.LastInstructionIndex <= instructions.Length - 1);

            // first instruction
            if (frame.FirstSelfInstructionIndex != -1)
            {
                ref var firstInstruction = ref instructions[frame.FirstSelfInstructionIndex];
                switch (frame.CallType)
                {
                    case CallType.IRQ:
                        Debug.Assert(firstInstruction.IsIRQ);
                        break;

                    case CallType.NMI:
                        Debug.Assert(firstInstruction.IsNMI);
                        break;

                    case CallType.JSR:
                    case CallType.TailCall:
                    case CallType.FallThrough:
                        Debug.Assert(firstInstruction.IsInstruction);
                        break;
                }
            }

            // last instruction
            var lastFrame = frame.GetLastInstructionStackFrame();
            if (!lastFrame.InsertedTailCall)
            {
                var lastInstructionIndex = frame.LastInstructionIndex;
                if (lastInstructionIndex < instructions.Length - 1)
                {
                    // included situations where stack pointer is manipulated
                    ref var lastInstruction = ref instructions[lastInstructionIndex];
                    ref var afterLastInstruction = ref instructions[lastInstructionIndex + 1];

                    Debug.Assert(lastInstruction.IsInstruction);
                    Debug.Assert(
                        lastInstruction.Opcode == 0x60/*RTS*/ ||
                        lastInstruction.Opcode == 0x40/*RTI*/ ||
                        lastInstruction.Opcode == 0x00/*BRK*/ ||
                        lastInstruction.Opcode == 0x20/*JSR*/ ||
                        instructionSet.IsBranchOrJump(lastInstruction.Opcode) ||
                        afterLastInstruction.IsIRQ ||
                        afterLastInstruction.IsNMI);
                }
            }

            // cycles
            Debug.Assert(frame.StartCycleCount < frame.EndCycleCount);
            Debug.Assert(frame.StartCycleCount >= 0 && frame.EndCycleCount <= totalCycles);

            // first and last stack frames
            var firstFrame = frame.GetFirstInstructionStackFrame();
            if (firstFrame != frame)
            {
                Debug.Assert(firstFrame.StartCycleCount == frame.StartCycleCount);
                Debug.Assert(firstFrame.FirstSelfInstructionIndex < frame.FirstSelfInstructionIndex);
                Debug.Assert(firstFrame.LastSelfInstructionIndex < frame.FirstSelfInstructionIndex);
            }

            if (lastFrame != frame)
            {
                Debug.Assert(lastFrame.EndCycleCount == frame.EndCycleCount);
                Debug.Assert(lastFrame.FirstSelfInstructionIndex > frame.LastSelfInstructionIndex);
                Debug.Assert(lastFrame.LastSelfInstructionIndex > frame.LastSelfInstructionIndex);
            }

            // instruction indices
            if (frame.FirstSelfInstructionIndex != -1)
                Debug.Assert(frame.FirstSelfInstructionIndex >= 0 && frame.FirstSelfInstructionIndex < instructions.Length);

            if (frame.LastSelfInstructionIndex != -1)
                Debug.Assert(frame.LastSelfInstructionIndex >= 0 && frame.LastSelfInstructionIndex < instructions.Length);

            if (frame.FirstSelfInstructionIndex != -1 && frame.LastSelfInstructionIndex != -1)
                Debug.Assert(frame.FirstSelfInstructionIndex <= frame.LastSelfInstructionIndex);

            // children
            StackFrame? prevChildFrame = null;
            for (int i = 0; i < frame.Children.Count; i++)
            {
                var childFrame = frame.Children[i];

                Debug.Assert(childFrame.StartCycleCount >= frame.StartCycleCount);
                Debug.Assert(childFrame.EndCycleCount <= frame.EndCycleCount);

                var childFrameFirstInstructionIndex = childFrame.LastInstructionIndex;
                var childFrameLastInstructionIndex = childFrame.GetFirstInstructionStackFrame().LastSelfInstructionIndex;

                if (frame.FirstSelfInstructionIndex != -1)
                    Debug.Assert(
                        childFrameFirstInstructionIndex == -1 ||
                        childFrameLastInstructionIndex == -1 ||
                        frame.FirstSelfInstructionIndex < childFrameFirstInstructionIndex ||
                        frame.FirstSelfInstructionIndex > childFrameLastInstructionIndex);                    

                if (frame.LastSelfInstructionIndex != -1)
                    Debug.Assert(
                        childFrameFirstInstructionIndex == -1 ||
                        childFrameLastInstructionIndex == -1 ||
                        frame.LastSelfInstructionIndex < childFrameFirstInstructionIndex ||
                        frame.LastSelfInstructionIndex > childFrameLastInstructionIndex);

                if (i == 0 && frame != firstFrame)
                    Debug.Assert(childFrame.StartCycleCount == frame.StartCycleCount);

                if (i == frame.Children.Count - 1 && frame != lastFrame)
                    Debug.Assert(childFrame.EndCycleCount == frame.EndCycleCount);

                if (prevChildFrame != null)
                    Debug.Assert(childFrame.StartCycleCount >= prevChildFrame.EndCycleCount);

                prevChildFrame = childFrame;
            }

            // recurse children
            foreach (var childFrame in frame.Children)
                AssertInvariant(childFrame, instructions, instructionSet, totalCycles);
        }

        private StackFrame GetFirstInstructionStackFrame()
        {
            var current = this;
            while (current.Children.Count > 0)
            {
                var firstChild = current.Children[0];
                if (firstChild.FirstSelfInstructionIndex >= current.FirstSelfInstructionIndex)
                    break;
                current = firstChild;
            }
            return current;
        }

        private StackFrame GetLastInstructionStackFrame()
        {
            var current = this;
            while (current.Children.Count > 0)
            {
                var lastChild = current.Children[^1];
                if (lastChild.LastSelfInstructionIndex <= current.LastSelfInstructionIndex)
                    break;
                current = lastChild;
            }
            return current;
        }
#endif

        public int FirstSelfInstructionIndex = -1;
        public int LastSelfInstructionIndex = -1;
        public int FirstInstructionIndex;
        public int LastInstructionIndex;
        public int StartCycleCount;
        public int EndCycleCount;
        public ushort LowestAddress = ushort.MinValue;
        public ushort HighestAddress = ushort.MaxValue;
        public CPUMetrics CPUMetrics = new();
        public List<StackFrame> Children = new();
        public bool InsertedTailCall = false;
    }
}