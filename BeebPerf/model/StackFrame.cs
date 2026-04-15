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
            ClearMetrics(this);
        }

        private static void ClearMetrics(StackFrame stackFrame)
        { 
            var stack = new Stack<StackFrame>();
            stack.Push(stackFrame);

            while (stack.Count > 0)
            {
                stackFrame = stack.Pop();

                stackFrame.CPUMetrics.Clear();

                foreach (var childStackFrame in stackFrame.Children)
                    stack.Push(childStackFrame);
            }
        }

        public bool IsEmpty()
        {
            return (FirstSelfInstructionIndex == -1 && LastSelfInstructionIndex == -1 && Children.Count == 0);
        }

        public override string ToString()
        {
            return "".PadLeft((FullDepth - 1) * 2, ' ') + $"Type: {CallType}, Start: {StartAddress}{Routine.Label}, Return: {ReturnAddress}, ReturnSP: {ReturnStackPointer}";
        }

        public static void ComputeInstructionIndices(StackFrame stackFrame, int instructionCount)
        {
            /*
            int first = stackFrame.FirstSelfInstructionIndex == -1 ? instructionCount - 1 : stackFrame.FirstSelfInstructionIndex;
            int last = stackFrame.LastSelfInstructionIndex == -1 ? 0 : stackFrame.LastSelfInstructionIndex;
            
            foreach (var childStackFrame in stackFrame.Children)
            {
                ComputeInstructionIndices(childStackFrame, instructionCount);
            
                first = Math.Min(first, childStackFrame.FirstInstructionIndex);
                last = Math.Max(last, childStackFrame.LastInstructionIndex);
            }
            
            stackFrame.FirstInstructionIndex = first;
            stackFrame.LastInstructionIndex = last;
            */

            var stack = new Stack<(StackFrame frame, int childIndex, int first, int last)>();

            // Push the root frame with initial state
            stack.Push((
                frame: stackFrame,
                childIndex: 0,
                first: stackFrame.FirstSelfInstructionIndex == -1 ? instructionCount - 1 : stackFrame.FirstSelfInstructionIndex,
                last: stackFrame.LastSelfInstructionIndex == -1 ? 0 : stackFrame.LastSelfInstructionIndex
            ));

            while (stack.Count > 0)
            {
                var (frame, childIndex, first, last) = stack.Pop();

                if (childIndex < frame.Children.Count)
                {
                    // we still have children to process so push the current frame back with childIndex incremented
                    stack.Push((frame, childIndex + 1, first, last));

                    // now push the child frame as a new work item
                    var child = frame.Children[childIndex];
                    int childFirst = child.FirstSelfInstructionIndex == -1 ? instructionCount - 1 : child.FirstSelfInstructionIndex;
                    int childLast = child.LastSelfInstructionIndex == -1 ? 0 : child.LastSelfInstructionIndex;

                    stack.Push((child, 0, childFirst, childLast));
                }
                else
                {
                    // all children processed — so we can now determine the instruction indices
                    foreach (var child in frame.Children)
                    {
                        first = Math.Min(first, child.FirstInstructionIndex);
                        last = Math.Max(last, child.LastInstructionIndex);
                    }

                    frame.FirstInstructionIndex = first;
                    frame.LastInstructionIndex = last;
                }
            }
        }

#if DEBUG && STACKFRAME_INVARIANT
        public static void AssertInvariant(StackFrame rootStackFrame, Instruction[] instructions, InstructionSet instructionSet)
        {
            int totalCycles = 0;
            for (int i = 0; i < instructions.Length; i++)
                totalCycles += instructions[i].CycleCount;

            var stack = new Stack<StackFrame>();
            stack.Push(rootStackFrame);

            while (stack.Count > 0)
            {
                var stackFrame = stack.Pop();

                // first and last self instruction indices
                if (stackFrame.FirstSelfInstructionIndex == -1 || stackFrame.LastSelfInstructionIndex == -1)
                {
                    Debug.Assert(stackFrame.FirstSelfInstructionIndex == -1);
                    Debug.Assert(stackFrame.LastSelfInstructionIndex == -1);
                }

                // first and last instruction indices
                if (stackFrame.FirstSelfInstructionIndex != -1)
                    Debug.Assert(stackFrame.FirstInstructionIndex <= stackFrame.FirstSelfInstructionIndex);

                if (stackFrame.LastSelfInstructionIndex != -1)
                    Debug.Assert(stackFrame.LastInstructionIndex >= stackFrame.LastSelfInstructionIndex);

                Debug.Assert(stackFrame.FirstInstructionIndex >= 0 && stackFrame.FirstInstructionIndex <= instructions.Length - 1);
                Debug.Assert(stackFrame.LastInstructionIndex >= 0 && stackFrame.LastInstructionIndex <= instructions.Length - 1);

                // first instruction
                if (stackFrame.FirstSelfInstructionIndex != -1)
                {
                    ref var firstInstruction = ref instructions[stackFrame.FirstSelfInstructionIndex];
                    switch (stackFrame.CallType)
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
                var lastFrame = stackFrame.GetLastInstructionStackFrame();
                if (!lastFrame.InsertedTailCall)
                {
                    var lastInstructionIndex = stackFrame.LastInstructionIndex;
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
                Debug.Assert(stackFrame.StartCycleCount < stackFrame.EndCycleCount);
                Debug.Assert(stackFrame.StartCycleCount >= 0 && stackFrame.EndCycleCount <= totalCycles);

                // first and last stack frames
                var firstFrame = stackFrame.GetFirstInstructionStackFrame();
                if (firstFrame != stackFrame)
                {
                    Debug.Assert(firstFrame.StartCycleCount == stackFrame.StartCycleCount);
                    Debug.Assert(firstFrame.FirstSelfInstructionIndex < stackFrame.FirstSelfInstructionIndex);
                    Debug.Assert(firstFrame.LastSelfInstructionIndex < stackFrame.FirstSelfInstructionIndex);
                }

                if (lastFrame != stackFrame)
                {
                    Debug.Assert(lastFrame.EndCycleCount == stackFrame.EndCycleCount);
                    Debug.Assert(lastFrame.FirstSelfInstructionIndex > stackFrame.LastSelfInstructionIndex);
                    Debug.Assert(lastFrame.LastSelfInstructionIndex > stackFrame.LastSelfInstructionIndex);
                }

                // instruction indices
                if (stackFrame.FirstSelfInstructionIndex != -1)
                    Debug.Assert(stackFrame.FirstSelfInstructionIndex >= 0 && stackFrame.FirstSelfInstructionIndex < instructions.Length);

                if (stackFrame.LastSelfInstructionIndex != -1)
                    Debug.Assert(stackFrame.LastSelfInstructionIndex >= 0 && stackFrame.LastSelfInstructionIndex < instructions.Length);

                if (stackFrame.FirstSelfInstructionIndex != -1 && stackFrame.LastSelfInstructionIndex != -1)
                    Debug.Assert(stackFrame.FirstSelfInstructionIndex <= stackFrame.LastSelfInstructionIndex);

                // children
                StackFrame? prevChildFrame = null;
                for (int i = 0; i < stackFrame.Children.Count; i++)
                {
                    var childFrame = stackFrame.Children[i];

                    Debug.Assert(childFrame.StartCycleCount >= stackFrame.StartCycleCount);
                    Debug.Assert(childFrame.EndCycleCount <= stackFrame.EndCycleCount);

                    var childFrameFirstInstructionIndex = childFrame.LastInstructionIndex;
                    var childFrameLastInstructionIndex = childFrame.GetFirstInstructionStackFrame().LastSelfInstructionIndex;

                    if (stackFrame.FirstSelfInstructionIndex != -1)
                        Debug.Assert(
                            childFrameFirstInstructionIndex == -1 ||
                            childFrameLastInstructionIndex == -1 ||
                            stackFrame.FirstSelfInstructionIndex < childFrameFirstInstructionIndex ||
                            stackFrame.FirstSelfInstructionIndex > childFrameLastInstructionIndex);

                    if (stackFrame.LastSelfInstructionIndex != -1)
                        Debug.Assert(
                            childFrameFirstInstructionIndex == -1 ||
                            childFrameLastInstructionIndex == -1 ||
                            stackFrame.LastSelfInstructionIndex < childFrameFirstInstructionIndex ||
                            stackFrame.LastSelfInstructionIndex > childFrameLastInstructionIndex);

                    if (i == 0 && stackFrame != firstFrame)
                        Debug.Assert(childFrame.StartCycleCount == stackFrame.StartCycleCount);

                    if (i == stackFrame.Children.Count - 1 && stackFrame != lastFrame)
                        Debug.Assert(childFrame.EndCycleCount == stackFrame.EndCycleCount);

                    if (prevChildFrame != null)
                        Debug.Assert(childFrame.StartCycleCount >= prevChildFrame.EndCycleCount);

                    prevChildFrame = childFrame;
                }

                // apply invariant to children
                foreach (var childStackFrame in stackFrame.Children)
                    stack.Push(childStackFrame);
            }
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