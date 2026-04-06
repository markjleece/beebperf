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

        public override string ToString()
        {
            return "".PadLeft((FullDepth - 1) * 2, ' ') + $"Type: {CallType}, Start: {StartAddress}{Routine.Label}, Return: {ReturnAddress}, ReturnSP: {ReturnStackPointer}";
        }

        public static void ComputeEffectiveInstructionIndices(StackFrame frame, int instructionCount)
        {
            int first = frame.FirstInstructionIndex == -1 ? instructionCount - 1 : frame.FirstInstructionIndex;
            int last = frame.LastInstructionIndex == -1 ? 0 : frame.LastInstructionIndex;

            foreach (var child in frame.Children)
            {
                ComputeEffectiveInstructionIndices(child, instructionCount);

                first = Math.Min(first, child.FirstEffectiveInstructionIndex);
                last = Math.Max(last, child.LastEffectiveInstructionIndex);
            }

            frame.FirstEffectiveInstructionIndex = first;
            frame.LastEffectiveInstructionIndex = last;
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
            // first instruction
            if (frame.FirstInstructionIndex != -1)
            {
                ref var firstInstruction = ref instructions[frame.FirstInstructionIndex];
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
                var lastInstructionIndex = frame.LastEffectiveInstructionIndex;
                if (lastInstructionIndex != -1 && lastInstructionIndex < instructions.Length - 2)
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
                Debug.Assert(firstFrame.FirstInstructionIndex < frame.FirstInstructionIndex);
                Debug.Assert(firstFrame.LastInstructionIndex < frame.FirstInstructionIndex);
            }

            if (lastFrame != frame)
            {
                Debug.Assert(lastFrame.EndCycleCount == frame.EndCycleCount);
                Debug.Assert(lastFrame.FirstInstructionIndex > frame.LastInstructionIndex);
                Debug.Assert(lastFrame.LastInstructionIndex > frame.LastInstructionIndex);
            }

            // instruction indices
            if (frame.FirstInstructionIndex != -1)
                Debug.Assert(frame.FirstInstructionIndex >= 0 && frame.FirstInstructionIndex < instructions.Length);

            if (frame.LastInstructionIndex != -1)
                Debug.Assert(frame.LastInstructionIndex >= 0 && frame.LastInstructionIndex < instructions.Length);

            if (frame.FirstInstructionIndex != -1 && frame.LastInstructionIndex != -1)
                Debug.Assert(frame.FirstInstructionIndex <= frame.LastInstructionIndex);

            // children
            StackFrame? prevChildFrame = null;
            for (int i = 0; i < frame.Children.Count; i++)
            {
                var childFrame = frame.Children[i];

                Debug.Assert(childFrame.StartCycleCount >= frame.StartCycleCount);
                Debug.Assert(childFrame.EndCycleCount <= frame.EndCycleCount);

                var childFrameFirstInstructionIndex = childFrame.LastEffectiveInstructionIndex;
                var childFrameLastInstructionIndex = childFrame.GetFirstInstructionStackFrame().LastInstructionIndex;

                if (frame.FirstInstructionIndex != -1)
                    Debug.Assert(
                        childFrameFirstInstructionIndex == -1 ||
                        childFrameLastInstructionIndex == -1 ||
                        frame.FirstInstructionIndex < childFrameFirstInstructionIndex ||
                        frame.FirstInstructionIndex > childFrameLastInstructionIndex);                    

                if (frame.LastInstructionIndex != -1)
                    Debug.Assert(
                        childFrameFirstInstructionIndex == -1 ||
                        childFrameLastInstructionIndex == -1 ||
                        frame.LastInstructionIndex < childFrameFirstInstructionIndex ||
                        frame.LastInstructionIndex > childFrameLastInstructionIndex);

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
                if (firstChild.FirstInstructionIndex >= current.FirstInstructionIndex)
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
                if (lastChild.LastInstructionIndex <= current.LastInstructionIndex)
                    break;
                current = lastChild;
            }
            return current;
        }
#endif

        public int FirstInstructionIndex = -1;
        public int LastInstructionIndex = -1;
        public int FirstEffectiveInstructionIndex;
        public int LastEffectiveInstructionIndex;
        public int StartCycleCount;
        public int EndCycleCount;
        public ushort LowestAddress = ushort.MinValue;
        public ushort HighestAddress = ushort.MaxValue;
        public CPUMetrics CPUMetrics = new();
        public List<StackFrame> Children = new();
        public bool InsertedTailCall = false;
    }
}