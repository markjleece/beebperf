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

using BeebPerf.model;
using System.Collections.Generic;
using System.Diagnostics;

namespace BeebPerf
{
    public class CPUAnalysis
    {
        public void StaticAnalysis(Model model)
        {
            Preamble(model);
            IdentifyRoutines();
            IdentifyStackFrames();
        }

        public void DynamicAnalysis(Model model, int startCycleCount, int endCycleCount)
        {
            Preamble(model);
            CalculateInstructionRange(startCycleCount, endCycleCount);
            CalculateMetrics(startCycleCount, endCycleCount);
            PopulateHotRoutines();
            PopulateProgramCallTree();
            PopulateNonMaskableInterruptCallTree();
            PopulateMaskableInterruptCallTree();
            MarkHotPaths();
        }

        private void Preamble(Model model)
        {
            _Instructions = model.Instructions;
            _InstructionSet = model.InstructionSet;
            _Labels = model.Labels;
        }

        private void CalculateInstructionRange(int startCycleCount, int endCycleCount)
        {
            int cycleCount = 0;
            int instructionIndex = 0;

            if (startCycleCount <= StartCycleCount)
            {
                FirstInstructionIndex = 0;
            }
            else while (instructionIndex < _Instructions.Length)
                {
                    if (cycleCount >= startCycleCount)
                    {
                        FirstInstructionIndex = instructionIndex;
                        break;
                    }
                    cycleCount += _Instructions[instructionIndex++].CycleCount;
                }

            if (endCycleCount >= EndCycleCount)
            {
                LastInstructionIndex = _Instructions.Length - 1;
            }
            else while (instructionIndex < _Instructions.Length)
                {
                    if (cycleCount >= endCycleCount)
                    {
                        LastInstructionIndex = instructionIndex;
                        break;
                    }
                    cycleCount += _Instructions[instructionIndex++].CycleCount;
                }
        }

        private int CalculatedExcludedCycles(model.StackFrame stackFrame, int startCycleCount, int endCycleCount)
        {
            Debug.Assert(
                (startCycleCount > stackFrame.StartCycleCount && startCycleCount <= stackFrame.EndCycleCount) ||
                (endCycleCount >= stackFrame.StartCycleCount && endCycleCount < stackFrame.EndCycleCount));

            int excludedCycleCount = 0;
            int childIndex = 0;
            int instructionIndex = stackFrame.FirstInstructionIndex;
            int cycleCount = stackFrame.StartCycleCount;

            model.StackFrame? childStackFrame = (stackFrame.Children.Count > 0) ? stackFrame.Children[0] : null;

            while (cycleCount <= stackFrame.EndCycleCount)
            {
                if (childStackFrame is not null && cycleCount == childStackFrame.StartCycleCount)
                {
                    // skip over child stack frames
                    instructionIndex = childStackFrame.GetLastInstructionStackFrame().LastInstructionIndex + 1;
                    cycleCount = childStackFrame.EndCycleCount;

                    childStackFrame = (++childIndex < stackFrame.Children.Count) ? stackFrame.Children[childIndex] : null;
                    continue;
                }

                // update cycle counts
                ref Instruction instruction = ref _Instructions[instructionIndex];
                int instructionCycleCount = instruction.CycleCount;
                if (cycleCount < startCycleCount || cycleCount > endCycleCount)
                    excludedCycleCount += instructionCycleCount;

                cycleCount += instructionCycleCount;
                instructionIndex++;
            }

            return excludedCycleCount;
        }

        private int CalculateMetrics(model.StackFrame stackFrame, int startCycleCount, int endCycleCount)
        {
            stackFrame.CPUMetrics.Clear();

            if (startCycleCount > stackFrame.EndCycleCount || endCycleCount < stackFrame.StartCycleCount)
            {
                return (stackFrame.EndCycleCount - stackFrame.StartCycleCount); // excluded cycle instructionCount
            }

            int excludedCycleCount = 0;
            if (startCycleCount > stackFrame.StartCycleCount || endCycleCount < stackFrame.EndCycleCount)
            {
                excludedCycleCount = CalculatedExcludedCycles(stackFrame, startCycleCount, endCycleCount);
            }

            int childInclusiveCycleCount = 0;
            int childElapsedCycleCount = 0;

            foreach (var childStackFrame in stackFrame.Children)
            {
                excludedCycleCount += CalculateMetrics(childStackFrame, startCycleCount, endCycleCount);
                childElapsedCycleCount += childStackFrame.CPUMetrics.ElapsedCycleCount;
                if (childStackFrame.Type != CallType.ISR)
                    childInclusiveCycleCount += childStackFrame.CPUMetrics.InclusiveCycleCount;
            }

            var cpuMetrics = stackFrame.CPUMetrics;
            cpuMetrics.ExecutionCount = 1;
            cpuMetrics.ElapsedCycleCount = (stackFrame.EndCycleCount - stackFrame.StartCycleCount) - excludedCycleCount;
            cpuMetrics.SelfCycleCount = cpuMetrics.ElapsedCycleCount - childElapsedCycleCount;
            cpuMetrics.InclusiveCycleCount = cpuMetrics.SelfCycleCount + childInclusiveCycleCount;

            Debug.Assert(cpuMetrics.ElapsedCycleCount >= 0 && cpuMetrics.SelfCycleCount >= 0 && cpuMetrics.InclusiveCycleCount >= 0);
            Debug.Assert(cpuMetrics.InclusiveCycleCount <= cpuMetrics.ElapsedCycleCount && cpuMetrics.SelfCycleCount <= cpuMetrics.ElapsedCycleCount && cpuMetrics.SelfCycleCount <= cpuMetrics.InclusiveCycleCount);

            if (stackFrame.Routine.MetricsByStack.TryGetValue(stackFrame, out var metrics))
                metrics.Add(cpuMetrics);
            else
                stackFrame.Routine.MetricsByStack.Add(stackFrame, cpuMetrics.Clone());

            stackFrame.Routine.AggregateMetrics.Add(cpuMetrics);

            return excludedCycleCount;
        }

        private void IdentifyRoutines()
        {
            RoutinesByAddress = new();
            _SortedRoutineAddresses = new();

            // create routines for all interrupt and JSR destinations. Also collect JSR and
            // RTS return addresses, plus all branch/jump source and destination addresses6
            HashSet<CanonicalAddress> jsrReturnAddresses = new(1024);
            HashSet<CanonicalAddress> rtsReturnAddresses = new(1024);
            HashSet<CanonicalAddressPair> branchesAndJumps = new(1024);

            foreach (var instruction in _Instructions)
            {
                if (instruction.IsInterrupt)
                {
                    RoutineType routineType = instruction.NonMaskableInterrupt ? RoutineType.NonMaskableISR : RoutineType.NonMaskableISR;
                    CreateRoutine(instruction.ISRAddress, routineType);
                }
                else if (instruction.Opcode == 0x20/*JSR*/)
                {
                    CreateRoutine(instruction.DestinationAddress, RoutineType.JSR);
                    jsrReturnAddresses.Add(instruction.OpcodeAddress.Offset(3));
                }
                else if (instruction.Opcode == 0x60/*RTS*/)
                {
                    CanonicalAddress destinationAddress = instruction.DestinationAddress;
                    rtsReturnAddresses.Add(instruction.DestinationAddress);
                }
                else if (_InstructionSet!.IsBranchOrJump(instruction.Opcode))
                {
                    CanonicalAddress destination = instruction.DestinationAddress;
                    if ((instruction.Opcode & 0x0F) == 0) // is branch?
                    {
                        int branchedAddress = unchecked(instruction.OpcodeAddress.Address + 2 + (sbyte)instruction.Operand);
                        destination = new CanonicalAddress((ushort)branchedAddress, instruction.OpcodeAddress.Page);
                    }
                    branchesAndJumps.Add(new CanonicalAddressPair(instruction.OpcodeAddress, destination));
                }
            }

            // create routines for every RTS return address which doesn't match a JSR return address
            foreach (CanonicalAddress rtsReturnAddress in rtsReturnAddresses)
                if (!jsrReturnAddresses.Contains(rtsReturnAddress))
                    CreateRoutine(rtsReturnAddress, RoutineType.Pseudo);

            // group branches and jumps by routine
            Dictionary<CanonicalAddress/*routine*/, HashSet<CanonicalAddressPair>> branchesAndJumpsByRoutine = new();

            foreach (CanonicalAddressPair branchOrJump in branchesAndJumps)
            {
                CanonicalAddress routineAddress = _SortedRoutineAddresses.Find(branchOrJump.FirstCanonicalAddress);

                HashSet<CanonicalAddressPair> routineBranchesAndJumps;
                if (branchesAndJumpsByRoutine.ContainsKey(routineAddress))
                {
                    routineBranchesAndJumps = branchesAndJumpsByRoutine[routineAddress];
                }
                else
                {
                    routineBranchesAndJumps = new();
                    branchesAndJumpsByRoutine.TryAdd(routineAddress, routineBranchesAndJumps);
                }

                routineBranchesAndJumps.Add(branchOrJump);
            }

            // create routines for every branch or jump destination that lies within another routine
            var allRoutines = RoutinesByAddress.Keys.ToList<CanonicalAddress>();
            Stack<CanonicalAddress> pendingRoutines = new(allRoutines);

            while (pendingRoutines.Count > 0)
            {
                CanonicalAddress routineAddress = pendingRoutines.Pop();
                if (!branchesAndJumpsByRoutine.ContainsKey(routineAddress))
                    continue; // routine doesn't contain any branches or jumps, so nothing to do

                // for each of the routine's branches and jumps...
                var routineBranchesAndJumps = branchesAndJumpsByRoutine[routineAddress];
                foreach (var routineBranchOrJump in routineBranchesAndJumps)
                {
                    CanonicalAddress destinationAddress = routineBranchOrJump.SecondCanonicalAddress;
                    if (RoutinesByAddress.ContainsKey(destinationAddress))
                        continue; // branch/jump destination matches and existing routine, so nothing to do

                    CanonicalAddress destinationRoutine = _SortedRoutineAddresses.Find(destinationAddress);
                    if (destinationRoutine.Equals(routineAddress))
                        continue; // branch/jump source and destination lie inside the same routine, so nothing to do

                    // at this point we know the branch/jump destination lands in the middle of another routine,
                    // so we need to create a new routine, splitting the existing one
                    CanonicalAddress newRoutine = destinationAddress;
                    CanonicalAddress existingRoutine = destinationRoutine;

                    CreateRoutine(newRoutine, RoutineType.Pseudo);

                    if (!branchesAndJumpsByRoutine.ContainsKey(existingRoutine))
                        continue; // the routine we are splitting does not contain any branches or jumps, so nothing more to do

                    // redistribute the branches and jumps across the two routines
                    var newRoutineBranchesAndJumps = new HashSet<CanonicalAddressPair>();
                    var existingRoutineBranchesAndJumps = new HashSet<CanonicalAddressPair>();

                    foreach (var branchOrJump in branchesAndJumpsByRoutine[existingRoutine])
                    {
                        CanonicalAddress sourceAddress = branchOrJump.FirstCanonicalAddress;
                        if (sourceAddress.Address < newRoutine.Address)
                            existingRoutineBranchesAndJumps.Add(branchOrJump);
                        else
                            newRoutineBranchesAndJumps.Add(branchOrJump);
                    }

                    branchesAndJumpsByRoutine[existingRoutine] = existingRoutineBranchesAndJumps;
                    branchesAndJumpsByRoutine[newRoutine] = newRoutineBranchesAndJumps;

                    // ensure the two routines are re-evaluated
                    pendingRoutines.Push(newRoutine);

                    if (!pendingRoutines.Contains(existingRoutine))
                        pendingRoutines.Push(existingRoutine);
                }
            }
        }

        private Routine CreateRoutine(CanonicalAddress address, RoutineType routineType)
        {
            if (RoutinesByAddress.TryGetValue(address, out Routine? routine))
                return routine;

            string label = _Labels.TryGetValue(address.Address, out var lbl) ? lbl : string.Empty;
            routine = new Routine(address, routineType, label);

            if (routineType == RoutineType.NonMaskableISR)
                _NonMaskableISR = routine;
            else if (routineType == RoutineType.MaskableISR)
                _MaskableISR = routine;

            RoutinesByAddress.Add(address, routine);
            _SortedRoutineAddresses.Add(address);

            Debug.WriteLine($"CreateRoutine( {address.ToString()} {routine.Label}, {routineType.ToString()} )");

            return routine;
        }

        private Routine GetRoutine(CanonicalAddress address)
        {
            CanonicalAddress routineAddress = _SortedRoutineAddresses.Find(address);
            if (RoutinesByAddress.TryGetValue(routineAddress, out Routine? routine))
                return routine;

            return CreateRoutine(address, RoutineType.Unknown);
        }

        public void IdentifyStackFrames()
        {
            int cycleCount = 0;

            // create initial stack frame
            ref Instruction firstInstruction = ref _Instructions[0];
            Routine firstRoutine = GetRoutine(firstInstruction.OpcodeAddress);
            model.StackFrame currentStackFrame = CreateStackFrame(CallType.Unknown, firstRoutine, startCycleCount: 0, parent: null);
            var instructionSet = _InstructionSet!;

            // for every instruction...
            for (int instructionIndex = 0; instructionIndex < _Instructions.Length; instructionIndex++)
            {
                ref Instruction instruction = ref _Instructions[instructionIndex];

                bool isFirstInstruction = (instructionIndex == 0);
                bool isLastInstruction = (instructionIndex == _Instructions.Length - 1);

                int postCycleCount = cycleCount + instruction.CycleCount;

                if (instruction.IsInstruction)
                {
                    // extend unknown stack frames to include backward branch/jump destinations that don't resolve
                    if (currentStackFrame.Type == CallType.Unknown &&
                        instructionSet.IsBranchOrJump(instruction.Opcode) &&
                        instruction.DestinationAddress.CompareTo(instruction.OpcodeAddress) < 0 &&
                        _SortedRoutineAddresses.Find(instruction.DestinationAddress).Address == 0)
                        ExtendStackFrame(currentStackFrame, instruction.DestinationAddress);

                    // is first instruction of a routine?
                    if (RoutinesByAddress.TryGetValue(instruction.OpcodeAddress, out Routine? routine) && !isFirstInstruction)
                        if (routine != currentStackFrame.Routine) // fall through?
                            currentStackFrame = CreateStackFrame(CallType.FallThrough, routine, cycleCount, parent: currentStackFrame);

                    // update instruction indices to include instruction
                    if (currentStackFrame.FirstInstructionIndex == 0)
                        currentStackFrame.FirstInstructionIndex = instructionIndex;

                    if (currentStackFrame.LastInstructionIndex < instructionIndex)
                        currentStackFrame.LastInstructionIndex = instructionIndex;

                    if (instruction.Opcode == 0x20/*JSR*/ && !isLastInstruction)
                    {
                        // create new stack frame for routine
                        Routine jsrRoutine = RoutinesByAddress[instruction.DestinationAddress];
                        currentStackFrame = CreateStackFrame(CallType.JSR, jsrRoutine, postCycleCount, parent: currentStackFrame);
                    }
                    else if (instructionSet.IsBranchOrJump(instruction.Opcode) && instruction.CycleCount > 2 &&
                             RoutinesByAddress.TryGetValue(instruction.DestinationAddress, out Routine? destinationRoutine) &&
                             destinationRoutine != currentStackFrame.Routine)
                    {
                        // create new stack frame for tail call
                        Routine jsrRoutine = RoutinesByAddress[instruction.DestinationAddress];
                        currentStackFrame = CreateStackFrame(CallType.TailCall, jsrRoutine, postCycleCount, parent: currentStackFrame);
                    }
                    else if (instruction.Opcode == 0x60/*RTS*/ || instruction.Opcode == 0x40/*RTI*/)
                    {
                        // unwind any tail or fall-through calls
                        while (true)
                        {
                            currentStackFrame.EndCycleCount = postCycleCount;

                            if (currentStackFrame.Parent is null)
                                break;

                            if (currentStackFrame.Type != CallType.TailCall && currentStackFrame.Type != CallType.FallThrough)
                                break;

                            currentStackFrame = currentStackFrame.Parent;
                        }

                        if (IsRTSTailCall(ref instruction, currentStackFrame))
                        {
                            // create new stack frame for tail call
                            Debug.Assert(RoutinesByAddress.ContainsKey(instruction.DestinationAddress));
                            var returnRoutine = RoutinesByAddress[instruction.DestinationAddress];
                            currentStackFrame = CreateStackFrame(CallType.TailCall, RoutinesByAddress[instruction.DestinationAddress], postCycleCount, parent: currentStackFrame);
                        }
                        else if (currentStackFrame.Parent is null)
                        {
                            // create new root stack frame
                            var rootRoutine = GetRoutine(instruction.DestinationAddress.Offset(-3));
                            var newRoot = new model.StackFrame(rootRoutine, CallType.Unknown, parent: null);
                            newRoot.StartCycleCount = 0;
                            newRoot.Children.Add(currentStackFrame);

                            currentStackFrame.Parent = newRoot;
                            currentStackFrame = newRoot;
                        }
                        else
                            currentStackFrame = currentStackFrame.Parent;
                    }
                }
                else if (instruction.IsInterrupt && !isLastInstruction)
                {
                    // update instruction indices to include interrupt
                    if (currentStackFrame.FirstInstructionIndex == 0)
                        currentStackFrame.FirstInstructionIndex = instructionIndex;

                    if (currentStackFrame.LastInstructionIndex < instructionIndex)
                        currentStackFrame.LastInstructionIndex = instructionIndex;

                    // create new stack frame for interrupt service routine
                    Routine isrRoutine = RoutinesByAddress[instruction.ISRAddress];
                    currentStackFrame = CreateStackFrame(CallType.ISR, isrRoutine, postCycleCount, parent: currentStackFrame);
                }

                cycleCount = postCycleCount;
            }

            StartCycleCount = 0;
            EndCycleCount = cycleCount;

            // unwind residual stack
            int depth = 0;
            while (true)
            {
                currentStackFrame.EndCycleCount = cycleCount;

                if (currentStackFrame.Parent is null) break;

                currentStackFrame = currentStackFrame.Parent;
                depth++;
            }
            Debug.Assert(depth < 10);

            _RootStackFrame = currentStackFrame;
        }

        private model.StackFrame CreateStackFrame(CallType type, Routine routine, int startCycleCount, model.StackFrame? parent)
        {
            Debug.Assert(parent is null || type != CallType.Unknown);
            model.StackFrame stackFrame = new(routine, type, parent);
            stackFrame.StartCycleCount = startCycleCount;
            if (parent != null)
                parent.Children.Add(stackFrame);
            return stackFrame;
        }

        private void ExtendStackFrame(model.StackFrame stackFrame, CanonicalAddress newAddress)
        {
            Debug.Assert(stackFrame.Type == CallType.Unknown);

            CanonicalAddress oldAddress = stackFrame.CanonicalAddress;

            stackFrame.CanonicalAddress = newAddress;
            stackFrame.Routine.StartAddress = newAddress;
            stackFrame.Routine.Label = _Labels.TryGetValue(newAddress.Address, out var lbl) ? lbl : string.Empty;

            RoutinesByAddress.Remove(oldAddress);
            RoutinesByAddress.Add(newAddress, stackFrame.Routine);

            _SortedRoutineAddresses.Remove(oldAddress);
            _SortedRoutineAddresses.Add(newAddress);
        }

        private bool IsRTSTailCall(ref Instruction instruction, model.StackFrame stackFrame)
        {
            if (instruction.Opcode != 0x60/*RTS*/)
                return false;

            if (stackFrame.Type == CallType.ISR)
                return true; // RTS from an ISR must perform a tail call to invoke RTI

            if (stackFrame.Type != CallType.JSR)
                return false;

            // does RTS destination match the parent's JSR instruction?
            ref Instruction jsrInstruction = ref _Instructions[stackFrame.Parent!.LastInstructionIndex];
            return (instruction.DestinationAddress.CompareTo(jsrInstruction.OpcodeAddress.Offset(3)) != 0);
        }

        private void ValidateStackFrame(model.StackFrame stackFrame)
        {
            Debug.Assert(stackFrame.StartCycleCount == 0 || stackFrame.FirstInstructionIndex > 0);
            Debug.Assert(stackFrame.LastInstructionIndex > 0);
            Debug.Assert(stackFrame.StartCycleCount <= stackFrame.EndCycleCount);
            Debug.Assert(stackFrame.FirstInstructionIndex <= stackFrame.LastInstructionIndex);

            model.StackFrame? lastChildStackFrame = null;
            foreach (var childStackFrame in stackFrame.Children)
            {
                ValidateStackFrame(childStackFrame);

                Debug.Assert(childStackFrame.StartCycleCount >= stackFrame.StartCycleCount);
                Debug.Assert(childStackFrame.EndCycleCount <= stackFrame.EndCycleCount);

                if (lastChildStackFrame != null)
                {
                    Debug.Assert(lastChildStackFrame.EndCycleCount <= childStackFrame.StartCycleCount);
                    Debug.Assert(lastChildStackFrame.LastInstructionIndex < childStackFrame.FirstInstructionIndex);
                }

                lastChildStackFrame = childStackFrame;
            }
        }

        private void CalculateMetrics(int startCycleCount, int endCycleCount)
        {
            if (startCycleCount < StartCycleCount)
                startCycleCount = StartCycleCount;

            if (endCycleCount < EndCycleCount)
                endCycleCount = EndCycleCount;

            _RootStackFrame.ClearMetrics();

            foreach (var routine in RoutinesByAddress.Values)
                routine.ClearMetrics();

            CalculateMetrics(_RootStackFrame, startCycleCount, endCycleCount);
        }

        private void PopulateHotRoutines()
        {
            HotRoutines.Clear();
            foreach (var routine in RoutinesByAddress.Values)
                HotRoutines.Add(routine);
            HotRoutines.Sort((a, b) => -a.AggregateMetrics.SelfCycleCount.CompareTo(b.AggregateMetrics.SelfCycleCount));

            int hotRoutineCount = int.Min(HotRoutines.Count, (int)float.Ceiling(0.05f * HotRoutines.Count)); 
            for (int i = 0; i < hotRoutineCount; i++)
                HotRoutines[i].HotRoutine = true;

            Debug.WriteLine("HotPath routines:");
            foreach (var routine in HotRoutines)
                DebugPrintLine(indent: 0, routine, routine.AggregateMetrics);
        }

        private void PopulateCallTreeNodeList(CallTreeNode treeNode, List<CallTreeNode> treeNodeList)
        {
            treeNodeList.Add(treeNode);
            foreach (var child in treeNode.Children)
                PopulateCallTreeNodeList(child, treeNodeList);
        }

        private void PopulateProgramCallTree()
        {
            Dictionary<CallStack, CallTreeNode> treeNodesByStack = new();
            ProgramCallTree = new CallTreeNode(_RootStackFrame);
            treeNodesByStack.Add(_RootStackFrame, ProgramCallTree);

            foreach (var routine in RoutinesByAddress.Values)
                foreach (var callStack in routine.MetricsByStack.Keys)
                    PopulateProgramCallTree(callStack, ProgramCallTree, treeNodesByStack);

            ProgramCallTree.Sort(CallTreeNode.SortField.InclusiveCPU, SortOrder.Descending);

            Debug.WriteLine("Program stack:");
            DebugPrintTree(ProgramCallTree, depth: 0);
        }

        private bool SetHotPaths(CallTreeNode treeNode)
        {
            bool hotChild = false;
            foreach (var child in treeNode.Children)
                hotChild |= SetHotPaths(child);
            treeNode.HotPath = (hotChild || treeNode.Routine.HotRoutine);
            return treeNode.HotPath;
        }

        private static CallTreeNode? PopulateProgramCallTree(CallStack callStack, CallTreeNode rootTreeNode, Dictionary<CallStack, CallTreeNode> treeNodesByStack)
        {
            if (treeNodesByStack.TryGetValue(callStack, out CallTreeNode? treeNode))
                return treeNode;

            if (callStack.Routine.RoutineType is RoutineType.NonMaskableISR or RoutineType.MaskableISR)
                return null;

            var newTreeNode = new CallTreeNode(callStack);

            var parentStackFrame = callStack.Parent;
            if (parentStackFrame is not null)
            {
                var parentTreeNode = PopulateProgramCallTree(parentStackFrame, rootTreeNode, treeNodesByStack);
                if (parentTreeNode is null)
                    return null;

                parentTreeNode.AddChild(newTreeNode);
            }

            treeNodesByStack[callStack] = newTreeNode;

            return newTreeNode;
        }

        private void PopulateNonMaskableInterruptCallTree()
        {
            NonMaskableInterruptCallTree = null;
            if (_NonMaskableISR is null)
                return;

            Dictionary<CallStack, CallTreeNode> interruptTreeNodesByStack = new();
            CallStack stack = _NonMaskableISR.MetricsByStack.Keys.First();
            NonMaskableInterruptCallTree = new CallTreeNode(stack);
            interruptTreeNodesByStack.Add(stack, NonMaskableInterruptCallTree);

            foreach (var routine in RoutinesByAddress.Values)
                foreach (var callStack in routine.MetricsByStack.Keys)
                    PopulateInterruptCallTree(callStack, NonMaskableInterruptCallTree, interruptTreeNodesByStack);

            NonMaskableInterruptCallTree.Sort(CallTreeNode.SortField.InclusiveCPU, SortOrder.Descending);

            Debug.WriteLine("Non Maskable Interrupt stack:");
            DebugPrintTree(NonMaskableInterruptCallTree, depth: 0);
        }

        private void PopulateMaskableInterruptCallTree()
        {
            MaskableInterruptCallTree = null;
            if (_MaskableISR is null)
                return;

            Dictionary<CallStack, CallTreeNode> interruptTreeNodesByStack = new();
            CallStack stack = _MaskableISR.MetricsByStack.Keys.First();
            MaskableInterruptCallTree = new CallTreeNode(stack);
            interruptTreeNodesByStack.Add(stack, MaskableInterruptCallTree);

            foreach (var routine in RoutinesByAddress.Values)
                foreach (var callStack in routine.MetricsByStack.Keys)
                    PopulateInterruptCallTree(callStack, MaskableInterruptCallTree, interruptTreeNodesByStack);

            MaskableInterruptCallTree.Sort(CallTreeNode.SortField.InclusiveCPU, SortOrder.Descending);

            Debug.WriteLine("Maskable Interrupt stack:");
            DebugPrintTree(MaskableInterruptCallTree, depth: 0);
        }

        private static CallTreeNode? PopulateInterruptCallTree(CallStack callStack, CallTreeNode rootTreeNode, Dictionary<CallStack, CallTreeNode> treeNodesByStack)
        {
            if (treeNodesByStack.TryGetValue(callStack, out var treeNode))
                return treeNode;

            if (callStack.Parent is null ||
                callStack.Routine.RoutineType is RoutineType.NonMaskableISR or RoutineType.MaskableISR)
                return null;

            var newTreeNode = new CallTreeNode(callStack);

            var parentStackFrame = callStack.Parent;
            if (parentStackFrame is not null)
            {
                CallTreeNode? parentTreeNode = PopulateInterruptCallTree(parentStackFrame, rootTreeNode, treeNodesByStack);
                if (parentTreeNode is null)
                    return null;

                parentTreeNode.AddChild(newTreeNode);
            }

            treeNodesByStack[callStack] = newTreeNode;

            return newTreeNode;
        }

        private void MarkHotPaths()
        {
            int count = 0;
            if (ProgramCallTree != null)
                count += ProgramCallTree.Count;
            if (NonMaskableInterruptCallTree != null)
                count += NonMaskableInterruptCallTree.Count;
            if (MaskableInterruptCallTree != null)
                count += MaskableInterruptCallTree.Count;

            var treeNodes = new List<CallTreeNode>(count);
            if (ProgramCallTree != null)
                PopulateCallTreeNodeList(ProgramCallTree, treeNodes);
            if (NonMaskableInterruptCallTree != null)
                PopulateCallTreeNodeList(NonMaskableInterruptCallTree, treeNodes);
            if (MaskableInterruptCallTree != null)
                PopulateCallTreeNodeList(MaskableInterruptCallTree, treeNodes);

            treeNodes.Sort((a, b) => (b.CPUMetrics.InclusiveCycleCount - a.CPUMetrics.InclusiveCycleCount));

            int hotPathCount = int.Min(treeNodes.Count, (int)float.Ceiling(0.03f * treeNodes.Count));
            for (int i = 0; i < hotPathCount; i++)
                treeNodes[i].HotPath = true;
        }

        private static void DebugPrintTree(CallTreeNode treeNode, int depth)
        {
            var routine = treeNode.Routine;
            DebugPrintLine(indent: depth, routine, treeNode.CPUMetrics);
            foreach (var childNode in treeNode.Children)
                DebugPrintTree(childNode, depth + 1);
        }

        private static void DebugPrintLine(int indent, Routine routine, CPUMetrics metrics)
        {
            indent -= 1515;
            indent = int.Max(0, indent);

            Debug.WriteLine(
                $"{"".PadLeft(indent)}" +
                $"{routine.StartAddress.ToString()} {routine.Label}, " +
                $"self: {metrics.SelfCycleCount}, " +
                $"inclusive: {metrics.InclusiveCycleCount}, " +
                $"elapsed: {metrics.ElapsedCycleCount}, " +
                $"instructionCount: {metrics.ExecutionCount}");
        }

        public List<InstructionMetrics> CalculateInstructionMetrics(Routine routine, CallStack? callStack)
        {
            Dictionary<CoreInstruction, InstructionMetrics> instructionMetrics = new();

            int instructionOrdinal = 0;

            foreach (var stackFrame in routine.StackFrames)
                if (StartCycleCount <= stackFrame.EndCycleCount && EndCycleCount >= stackFrame.StartCycleCount &&
                    (callStack == null || stackFrame.Equals(callStack)))
                    CalculateStackFrameMetrics(stackFrame, ref instructionOrdinal, instructionMetrics);

            var instructionMetricsList = instructionMetrics.Values.ToList();
            instructionMetricsList.Sort();
            
            IdentifyModifiedCode(instructionMetricsList);

            return instructionMetricsList;
        }

        private void IdentifyModifiedCode(List<InstructionMetrics> instructionMetrics)
        {
            // construct diction that maps addresses to number of distinct instructions
            var addressSlots = new Dictionary<CanonicalAddress, int>(instructionMetrics.Count);
            foreach (var instructionMetric in instructionMetrics)
            {
                byte opcode = instructionMetric.Instruction.Opcode;
                var address = instructionMetric.Instruction.OpcodeAddress;
                int size = _InstructionSet!.Size(opcode);

                // determine instruction count
                int instructionCount = 1;
                for (int i = 0; i < size; i++)
                { 
                    if (addressSlots.TryGetValue(address, out var existingInstructionCount))
                        if (instructionCount <= existingInstructionCount)
                            instructionCount = existingInstructionCount + 1;
                    address = address.Offset(1);
                }

                // set instructionCount
                address = instructionMetric.Instruction.OpcodeAddress;
                for (int i = 0; i < size; i++)
                {
                    addressSlots[address] = instructionCount;
                    address = address.Offset(1);
                }
            }

            // mark instructions
            foreach (var instructionMetric in instructionMetrics)
            {
                var address = instructionMetric.Instruction.OpcodeAddress;
                if (addressSlots.TryGetValue(address, out var instructionCount))
                    instructionMetric.CodeModified = (instructionCount > 1);
            }
        }

        private void CalculateStackFrameMetrics(
            model.StackFrame stackFrame,
            ref int instructionOrdinal,
            Dictionary<CoreInstruction, InstructionMetrics> instructionMetrics)
        {
            int childIndex = 0;
            int instructionIndex = stackFrame.FirstInstructionIndex;
            int cycleCount = stackFrame.StartCycleCount;

            int previousInstructionIndex = instructionIndex;
            int lastInstructionIndex = stackFrame.GetLastInstructionStackFrame().LastInstructionIndex;

            model.StackFrame? childStackFrame = (stackFrame.Children.Count > 0) ? stackFrame.Children[0] : null;

            while (cycleCount <= stackFrame.EndCycleCount && instructionIndex <= lastInstructionIndex)
            {
                if (childStackFrame != null && cycleCount >= childStackFrame.StartCycleCount)
                {
                    if (childStackFrame.Type == CallType.JSR ||
                        childStackFrame.Type == CallType.TailCall)
                    {
                        // we need to get the cycle instructionCount
                        var previousInstruction = new CoreInstruction(ref _Instructions[previousInstructionIndex]);
                        if (instructionMetrics.TryGetValue(previousInstruction, out var metrics))
                            metrics.InclusiveCycleCount += childStackFrame.CPUMetrics.InclusiveCycleCount;
                    }

                    if (childStackFrame.Type == CallType.FallThrough)
                        CalculateStackFrameMetrics(childStackFrame, ref instructionOrdinal, instructionMetrics);

                    // skip over child stack frames
                    instructionIndex = childStackFrame.GetLastInstructionStackFrame().LastInstructionIndex + 1;
                    cycleCount = childStackFrame.EndCycleCount;

                    childStackFrame = (++childIndex < stackFrame.Children.Count) ? stackFrame.Children[childIndex] : null;
                    continue;
                }

                // update cycle counts
                ref Instruction instruction = ref _Instructions[instructionIndex];
                int instructionCycleCount = instruction.CycleCount;
                if (instruction.IsInstruction &&
                    cycleCount >= StartCycleCount && cycleCount <= EndCycleCount)
                {
                    InstructionMetrics? metrics;
                    var coreInstruction = new CoreInstruction(ref instruction);
                    if (!instructionMetrics.TryGetValue(coreInstruction, out metrics))
                    {
                        metrics = new InstructionMetrics(coreInstruction, instructionOrdinal++);
                        instructionMetrics.Add(coreInstruction, metrics);
                    }

                    metrics.InclusiveCycleCount += instructionCycleCount;
                    metrics.ExecutionCount += 1;

                    previousInstructionIndex = instructionIndex;
                }

                cycleCount += instructionCycleCount;
                instructionIndex++;
            }
        }

        public Dictionary<CanonicalAddress, Routine> RoutinesByAddress = new();
        public List<Routine> HotRoutines = new();
        public CallTreeNode? ProgramCallTree;
        public CallTreeNode? NonMaskableInterruptCallTree;
        public CallTreeNode? MaskableInterruptCallTree;
        public int FirstInstructionIndex;
        public int LastInstructionIndex;
        public int StartCycleCount;
        public int EndCycleCount;

        private Routine? _MaskableISR = null;
        private Routine? _NonMaskableISR = null;
        private model.StackFrame _RootStackFrame = new();
        private SortedCanonicalAddresses _SortedRoutineAddresses = new();
        private Instruction[] _Instructions = [];
        private Dictionary<ushort, string> _Labels = [];
        private InstructionSet? _InstructionSet;
    }
}