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
using System.Diagnostics;

namespace BeebPerf
{
    public class CPUAnalysis
    {
        public async Task<bool> StaticAnalysis(Model model)
        {
            return await Task.Run(() => 
            {
                Initialize(model);
                IdentifyRoutines();
                IdentifyStackFrames();
                return true; 
            });
        }

        public async Task<bool> DynamicAnalysisAsync(int startCycleCount, int endCycleCount)
        {
            return await Task.Run(() => 
            {
                CalculateRange(startCycleCount, endCycleCount);
                CalculateMetrics();
                PopulateHotRoutines();
                PopulateProgramCallTree();
                PopulateNonMaskableInterruptCallTree();
                PopulateMaskableInterruptCallTree();
                MarkHotPaths();
                return true; 
            });
        }

        //
        // static analysis...
        //
        private void Initialize(Model model)
        {
            _Instructions = model.Instructions;
            _InstructionSet = model.InstructionSet;
            _Labels = model.Labels;
            _InitialCallStack = model.Snapshot.StackFrames;
            _InitialStackPointer = model.Snapshot.StackPointer;
        }

        private void IdentifyRoutines()
        {
            // we do this in two phases
            // 1) identify an initial set of routines based on JSR instructions, interrupt vectors etc.
            // 2) identify any additional routines based on branches and jumps that land in the middle of existing routines, splitting those routines as necessary

            RoutinesByAddress = new();
            _SortedRoutineAddresses = new();

            HashSet<CanonicalAddressPair> branchesAndJumps = new(1024);

            // first prime a call stack with the initial call stack
            Stack<MiniStackFrame> stackFrames = new();
            foreach (var stackFrame in _InitialCallStack)
            {
                pushStackFrame(stackFrame, RoutineType.JSR);
                //Debug.WriteLine($"Initial stack frame: {stackFrame}");
            }

            // next, iterate through instructions, tracking the stack frames to identify routine entry points, whilst collecting branches and jumps for phase 2
            byte stackPointer = _InitialStackPointer;
            bool fSyncStack = false;

            void syncStackFrames()
            {
                if (!fSyncStack)
                    return;
                fSyncStack = false;
                while (stackFrames.Count > 1)
                {
                    if (stackPointer < stackFrames.Peek().StackPointer)
                        break;
                    stackFrames.Pop();
                }
            }

            void pushStackFrame(MiniStackFrame stackFrame, RoutineType routineType)
            {
                stackFrames.Push(stackFrame);
                CreateRoutine(stackFrame.StartAddress, routineType);
            }

            foreach (var instruction in _Instructions)
            {
                if (instruction.IsInstruction)
                {
                    switch (instruction.Opcode)
                    {
                        case 0x00/*BRK*/:
                        case 0x20/*JSR*/:
                            syncStackFrames();
                            var routineType = instruction.Opcode == 0x20 ? RoutineType.JSR : RoutineType.BRK;
                            var callTyle = instruction.Opcode == 0x20 ? CallType.JSR : CallType.BRK;
                            pushStackFrame(new MiniStackFrame(callTyle, instruction.DestinationAddress, instruction.ReturnAddress, stackPointer), routineType);
                            stackPointer -= (byte)(instruction.Opcode == 0x20 ? 2/*JSR*/ : 3/*BRK*/);
                            break;

                        case 0x40/*RTI*/:
                        case 0x60/*RTS*/:
                            syncStackFrames();
                            stackPointer += (byte)(instruction.Opcode == 0x60 ? 2/*RTS*/ : 3/*RTI*/);
                            if (stackPointer < stackFrames.Peek().StackPointer)
                                CreateRoutine(instruction.DestinationAddress, RoutineType.Pseudo);
                            else
                                stackFrames.Pop();
                            break;

                        case 0x48/*PHA*/:
                        case 0x68/*PLA*/:
                        case 0x08/*PHP*/:
                        case 0x28/*PLP*/:
                        case 0xDA/*PHX*/:
                        case 0xFA/*PLX*/:
                        case 0x5A/*PHY*/:
                        case 0x7A/*PLY*/:
                        case 0x9A/*TXS*/:
                        case 0x9B/*TAS*/:
                            stackPointer = instruction.StackPointer;
                            fSyncStack = true;
                            break;

                        default:
                            // collect branches and jumps for phase 2
                            if (_InstructionSet!.IsBranchOrJump(instruction.Opcode))
                            {
                                CanonicalAddress destination = instruction.DestinationAddress;
                                if ((instruction.Opcode & 0x0F) == 0) // is branch?
                                {
                                    int branchedAddress = unchecked(instruction.OpcodeAddress.Address + 2 + (sbyte)instruction.Operand);
                                    destination = new CanonicalAddress((ushort)branchedAddress, instruction.OpcodeAddress.Page);
                                }
                                branchesAndJumps.Add(new CanonicalAddressPair(instruction.OpcodeAddress, destination));
                            }
                            break;
                    }
                }
                else if (instruction.IsNonMaskableInterrupt || instruction.IsMaskableInterrupt)
                {
                    var routineType = instruction.IsMaskableInterrupt ? RoutineType.IRQ : RoutineType.NMI;
                    var callTyle = instruction.IsMaskableInterrupt? CallType.IRQ : CallType.NMI;
                    syncStackFrames();
                    pushStackFrame(new MiniStackFrame(callTyle, instruction.DestinationAddress, instruction.ReturnAddress, stackPointer), routineType);
                    stackPointer -= 3;
                }
            }

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

            // now we have an initial set of routines and a set of branches and jumps, so we can identify any additional
            // routines that are required to ensure all branch and jump destinations are routine entry points
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

            if (routineType == RoutineType.NMI)
                _NonMaskableISR = routine;
            else if (routineType == RoutineType.IRQ || routineType == RoutineType.BRK)
                _MaskableISR = routine;

            RoutinesByAddress.Add(address, routine);
            _SortedRoutineAddresses.Add(address);

            return routine;
        }

        private void DebugInstruction(int instructionIndex, Instruction instruction, model.StackFrame stackFrame)
        {
            string destinationAddress = string.Empty;
            if (instruction.IsInstruction && (instruction.Opcode == 0x60/*RTS*/ || instruction.Opcode == 0x40/*RTI*/))
                destinationAddress = instruction.DestinationAddress.ToString();

            string returnAddress = string.Empty;
            if (instruction.IsNonMaskableInterrupt)
                returnAddress = instruction.ReturnAddress.ToString();

            Debug.WriteLine(
                "".PadLeft(stackFrame.FullDepth * 2, ' ') +
                $"instruction index: {instructionIndex}, " +
                $"instruction: {instruction.ToString(_InstructionSet!)}, " +
                $"destinationAddress: {destinationAddress}, " +
                $"returnAddress: {returnAddress}, " +
                $"stackFrame: {stackFrame.Routine.StartAddress}{stackFrame.Routine.Label}, " +
                $"stackFrame type: {stackFrame.Type}");
        }

        public void IdentifyStackFrames()
        {
            // prime call stack with initial stack frames
            model.StackFrame? currentStackFrame = null;
            foreach (var stackFrame in _InitialCallStack)
            {
                currentStackFrame = CreateStackFrame(
                    CallType.JSR, // TODO: could be an ISR here (need to serialize this!)
                    RoutinesByAddress[stackFrame.StartAddress],
                    stackFrame.ReturnAddress,
                    stackFrame.StackPointer,
                    startCycleCount: 0,
                    parent: currentStackFrame);
            }

            // iterate over all the instructions...
            byte stackPointer = _InitialStackPointer;
            var instructionSet = _InstructionSet!;
            bool fSyncStack = false;
            int cycleCount = 0;

            void syncStackFrames(int postCycleCount)
            {
                if (!fSyncStack)
                    return;
                fSyncStack = false;
                while (currentStackFrame?.Parent != null)
                {
                    currentStackFrame.EndCycleCount = postCycleCount;
                    if (stackPointer < currentStackFrame.StackPointer)
                        break;
                    //Debug.WriteLine($"SYNC:{currentStackFrame}");
                    currentStackFrame = currentStackFrame.Parent;
                }
            }

            for (int instructionIndex = 0; instructionIndex < _Instructions.Length; instructionIndex++)
            {
                ref Instruction instruction = ref _Instructions[instructionIndex];

                bool isFirstInstruction = (instructionIndex == 0);
                bool isLastInstruction = (instructionIndex == _Instructions.Length - 1);

                int postCycleCount = cycleCount + instruction.CycleCount;

                if (instruction.IsInstruction)
                {
                    // TODO: Make sure BRKs are treated like ISRs

                    // is first instruction of a routine?
                    if (RoutinesByAddress.TryGetValue(instruction.OpcodeAddress, out Routine? routine) && !isFirstInstruction)
                        if (routine != currentStackFrame!.Routine) // fall through?
                            currentStackFrame = CreateStackFrame(
                                CallType.FallThrough,
                                routine,
                                currentStackFrame.ReturnAddress,
                                currentStackFrame.StackPointer,
                                cycleCount, 
                                parent: currentStackFrame);

                    // update instruction indices to include instruction
                    if (currentStackFrame!.FirstInstructionIndex < 0)
                        currentStackFrame.FirstInstructionIndex = instructionIndex;

                    if (currentStackFrame.LastInstructionIndex < instructionIndex)
                        currentStackFrame.LastInstructionIndex = instructionIndex;

                    if ((instruction.Opcode == 0x00/*BRK*/ || instruction.Opcode == 0x20/*JSR*/) && !isLastInstruction)
                    {
                        // create new stack frame for routine / break
                        syncStackFrames(postCycleCount);
                        currentStackFrame = CreateStackFrame(
                            instruction.Opcode == 0x20 ? CallType.JSR : CallType.BRK,
                            RoutinesByAddress[instruction.DestinationAddress],
                            returnAddress: instruction.OpcodeAddress.Offset(3),
                            stackPointer,
                            postCycleCount,
                            parent: currentStackFrame);
                        stackPointer -= (byte)(instruction.Opcode == 0x20 ? 2/*JSR*/ : 3/*BRK*/);
                    }
                    else if (instructionSet.IsBranchOrJump(instruction.Opcode) && instruction.CycleCount > 2 &&
                             RoutinesByAddress.TryGetValue(instruction.DestinationAddress, out Routine? destinationRoutine) &&
                             destinationRoutine != currentStackFrame.Routine)
                    {
                        // create new stack frame for tail call
                        syncStackFrames(postCycleCount);
                        currentStackFrame = CreateStackFrame(
                            CallType.TailCall,
                            RoutinesByAddress[instruction.DestinationAddress], 
                            currentStackFrame.ReturnAddress,
                            currentStackFrame.StackPointer, 
                            postCycleCount, 
                            parent: currentStackFrame);
                    }
                    else if (instruction.Opcode == 0x60/*RTS*/ || instruction.Opcode == 0x40/*RTI*/)
                    {
                        syncStackFrames(postCycleCount);

                        stackPointer += (byte)(instruction.Opcode == 0x60 ? 2/*RTS*/ : 3/*RTI*/);

                        // unwind any fall-through and tail calls
                        while (currentStackFrame!.Type == CallType.TailCall || currentStackFrame!.Type == CallType.FallThrough)
                        {
                            currentStackFrame.EndCycleCount = postCycleCount;
                            currentStackFrame = currentStackFrame.Parent;
                        }

                        currentStackFrame.EndCycleCount = postCycleCount;

                        if (stackPointer < currentStackFrame!.StackPointer)
                        {
                            // create new stack frame for tail call
                            currentStackFrame = CreateStackFrame(
                                CallType.TailCall, 
                                RoutinesByAddress[instruction.DestinationAddress], 
                                currentStackFrame.ReturnAddress,
                                currentStackFrame.StackPointer,
                                postCycleCount, 
                                parent: currentStackFrame);
                        }
                        else
                        {
                            var destinationAddress = _SortedRoutineAddresses.Find(instruction.DestinationAddress);
                            if (!destinationAddress.Equals(currentStackFrame!.Parent!.Routine.StartAddress))
                            {
                                // not returning directly to caller, so create and insert new stack frame for landing tail call
                                //Debug.WriteLine($"RTNI:{currentStackFrame}");
                                var newParentStackFrame = CreateStackFrame(
                                    CallType.TailCall,
                                    RoutinesByAddress[destinationAddress],
                                    currentStackFrame.ReturnAddress,
                                    currentStackFrame.StackPointer,
                                    currentStackFrame.StartCycleCount,
                                    parent: currentStackFrame!.Parent);
                                currentStackFrame.Parent.Children.Remove(currentStackFrame);
                                newParentStackFrame.Children.Add(currentStackFrame);
                                currentStackFrame.Parent = newParentStackFrame;
                                currentStackFrame = newParentStackFrame;
                            }
                            else
                            {
                                //Debug.WriteLine($"RTND:{currentStackFrame}");
                                currentStackFrame = currentStackFrame.Parent;
                            }
                        }
                    }
                    else switch (instruction.Opcode)
                    {
                        case 0x48/*PHA*/:
                        case 0x68/*PLA*/:
                        case 0x08/*PHP*/:
                        case 0x28/*PLP*/:
                        case 0xDA/*PHX*/:
                        case 0xFA/*PLX*/:
                        case 0x5A/*PHY*/:
                        case 0x7A/*PLY*/:
                        case 0x9A/*TXS*/:
                        case 0x9B/*TAS*/:
                            stackPointer = instruction.StackPointer;
                            fSyncStack = true;
                            break;
                        default:
                            break;
                    }
                }
                else if ((instruction.IsMaskableInterrupt || instruction.IsNonMaskableInterrupt) && !isLastInstruction)
                {
                    // update instruction indices to include interrupt
                    if (currentStackFrame!.FirstInstructionIndex < 0)
                        currentStackFrame.FirstInstructionIndex = instructionIndex;

                    if (currentStackFrame.LastInstructionIndex < instructionIndex)
                        currentStackFrame.LastInstructionIndex = instructionIndex;

                    syncStackFrames(postCycleCount);

                    // create new stack frame for interrupt service routine
                    currentStackFrame = CreateStackFrame(
                        CallType.IRQ,
                        RoutinesByAddress[instruction.DestinationAddress],
                        instruction.ReturnAddress,
                        stackPointer,
                        postCycleCount, 
                        parent: currentStackFrame);

                    stackPointer -= 3;
                }

                cycleCount = postCycleCount;
            }

            syncStackFrames(cycleCount);

            // set initial cycle counts
            StartCycleCount = 0;
            EndCycleCount = cycleCount;

            // unwind residual stack
            int depth = 0;
            while (true)
            {
                currentStackFrame!.EndCycleCount = cycleCount;
                if (currentStackFrame.Parent is null) 
                    break;
                currentStackFrame = currentStackFrame.Parent;
                depth++;
            }
            Debug.Assert(depth < 10);

            RootStackFrame = currentStackFrame;
        }

        private model.StackFrame CreateStackFrame(
            CallType type,
            Routine routine,
            CanonicalAddress returnAddress,
            byte stackPointer,
            int startCycleCount,
            model.StackFrame? parent)
        {
            model.StackFrame stackFrame = new(routine, returnAddress, stackPointer, type, parent);
            stackFrame.StartCycleCount = startCycleCount;
            if (parent != null)
                parent.Children.Add(stackFrame);
            string prefix = type switch {
                CallType.None => "NONE",
                CallType.JSR => "JSR_",
                CallType.IRQ => "IRQ_",
                CallType.NMI => "NMI_",
                CallType.BRK => "BRK_",
                CallType.TailCall => "TAIL",
                CallType.FallThrough => "FALL",
                _ => "????"
            };
            //Debug.WriteLine($"{prefix}:{stackFrame}");
            return stackFrame;
        }

        //
        // dynamic analysis...
        //
        private void CalculateRange(int startCycleCount, int endCycleCount)
        {
            int cycleCount = 0;
            int instructionIndex = 0;

            while (instructionIndex < _Instructions.Length)
            {
                if (cycleCount >= startCycleCount)
                    break;

                cycleCount += _Instructions[instructionIndex++].CycleCount;
            }

            FirstInstructionIndex = instructionIndex;
            StartCycleCount = cycleCount;

            while (instructionIndex < _Instructions.Length)
            {
                cycleCount += _Instructions[instructionIndex].CycleCount;
                if (cycleCount >= endCycleCount)
                    break;
                instructionIndex++;
            }

            LastInstructionIndex = instructionIndex;
            EndCycleCount = cycleCount;
        }

        private void CalculateMetrics()
        {
            RootStackFrame.ClearMetrics();

            foreach (var routine in RoutinesByAddress.Values)
                routine.ClearMetrics();

            CalculateMetrics(RootStackFrame);
        }

        private int CalculateMetrics(model.StackFrame stackFrame)
        {
            stackFrame.CPUMetrics.Clear();

            if (StartCycleCount > stackFrame.EndCycleCount || EndCycleCount < stackFrame.StartCycleCount)
            {
                return (stackFrame.EndCycleCount - stackFrame.StartCycleCount); // excluded cycle instructionCount
            }

            int excludedCycleCount = 0;
            if (StartCycleCount > stackFrame.StartCycleCount || EndCycleCount < stackFrame.EndCycleCount)
            {
                excludedCycleCount = CalculatedExcludedCycles(stackFrame);
            }

            int childInclusiveCycleCount = 0;
            int childElapsedCycleCount = 0;

            foreach (var childStackFrame in stackFrame.Children)
            {
                excludedCycleCount += CalculateMetrics(childStackFrame);
                childElapsedCycleCount += childStackFrame.CPUMetrics.ElapsedCycleCount;
                if (childStackFrame.Type != CallType.IRQ && childStackFrame.Type != CallType.NMI && childStackFrame.Type != CallType.BRK)
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

        private int CalculatedExcludedCycles(model.StackFrame stackFrame)
        {
            Debug.Assert(
                (StartCycleCount > stackFrame.StartCycleCount && StartCycleCount <= stackFrame.EndCycleCount) ||
                (EndCycleCount >= stackFrame.StartCycleCount && EndCycleCount < stackFrame.EndCycleCount));

            int excludedCycleCount = 0;
            int childIndex = 0;
            int instructionIndex = stackFrame.FirstInstructionIndex;
            int cycleCount = stackFrame.StartCycleCount;

            int lastInstructionIndex = stackFrame.GetLastInstructionStackFrame().LastInstructionIndex;
            model.StackFrame? childStackFrame = (stackFrame.Children.Count > 0) ? stackFrame.Children[0] : null;

            while (cycleCount <= stackFrame.EndCycleCount && instructionIndex <= lastInstructionIndex)
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
                if (cycleCount < StartCycleCount || cycleCount > EndCycleCount)
                    excludedCycleCount += instructionCycleCount;

                cycleCount += instructionCycleCount;
                instructionIndex++;
            }

            return excludedCycleCount;
        }

        private void PopulateHotRoutines()
        {
            HotRoutines.Clear();
            foreach (var routine in RoutinesByAddress.Values)
                if (routine.AggregateMetrics.ExecutionCount > 0)
                    HotRoutines.Add(routine);
            HotRoutines.Sort((a, b) => b.AggregateMetrics.SelfCycleCount.CompareTo(a.AggregateMetrics.SelfCycleCount));

            int hotRoutineCount = int.Min(HotRoutines.Count, (int)float.Ceiling(0.05f * HotRoutines.Count)); 
            for (int i = 0; i < HotRoutines.Count; i++)
                HotRoutines[i].HotRoutine = (i < hotRoutineCount);
        }

        private void PopulateProgramCallTree()
        {
            Dictionary<CallStack, CallTreeNode> treeNodesByStack = new();
            ProgramCallTree = new CallTreeNode(RootStackFrame);
            treeNodesByStack.Add(RootStackFrame, ProgramCallTree);

            foreach (var routine in RoutinesByAddress.Values)
                foreach (var callStack in routine.MetricsByStack.Keys)
                    PopulateProgramCallTree(callStack, ProgramCallTree, treeNodesByStack);

            ProgramCallTree.Sort(CallTreeNode.SortField.InclusiveCPU, SortOrder.Descending);
        }

        private static CallTreeNode? PopulateProgramCallTree(CallStack callStack, CallTreeNode rootTreeNode, Dictionary<CallStack, CallTreeNode> treeNodesByStack)
        {
            if (treeNodesByStack.TryGetValue(callStack, out CallTreeNode? treeNode))
                return treeNode;

            if (callStack.Routine.RoutineType is RoutineType.NMI or RoutineType.IRQ)
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
            if (_NonMaskableISR is null || _NonMaskableISR.MetricsByStack.Keys.Count == 0)
                return;

            Dictionary<CallStack, CallTreeNode> interruptTreeNodesByStack = new();
            CallStack stack = _NonMaskableISR.MetricsByStack.Keys.First();
            NonMaskableInterruptCallTree = new CallTreeNode(stack);
            interruptTreeNodesByStack.Add(stack, NonMaskableInterruptCallTree);

            foreach (var routine in RoutinesByAddress.Values)
                foreach (var callStack in routine.MetricsByStack.Keys)
                    PopulateInterruptCallTree(callStack, NonMaskableInterruptCallTree, interruptTreeNodesByStack);

            NonMaskableInterruptCallTree.Sort(CallTreeNode.SortField.InclusiveCPU, SortOrder.Descending);
        }

        private void PopulateMaskableInterruptCallTree()
        {
            MaskableInterruptCallTree = null;
            if (_MaskableISR is null || _MaskableISR.MetricsByStack.Keys.Count == 0)
                return;

            Dictionary<CallStack, CallTreeNode> interruptTreeNodesByStack = new();
            CallStack stack = _MaskableISR.MetricsByStack.Keys.First();
            MaskableInterruptCallTree = new CallTreeNode(stack);
            interruptTreeNodesByStack.Add(stack, MaskableInterruptCallTree);

            foreach (var routine in RoutinesByAddress.Values)
                foreach (var callStack in routine.MetricsByStack.Keys)
                    PopulateInterruptCallTree(callStack, MaskableInterruptCallTree, interruptTreeNodesByStack);

            MaskableInterruptCallTree.Sort(CallTreeNode.SortField.InclusiveCPU, SortOrder.Descending);
        }

        private static CallTreeNode? PopulateInterruptCallTree(CallStack callStack, CallTreeNode rootTreeNode, Dictionary<CallStack, CallTreeNode> treeNodesByStack)
        {
            if (treeNodesByStack.TryGetValue(callStack, out var treeNode))
                return treeNode;

            if (callStack.Parent is null ||
                callStack.Routine.RoutineType is RoutineType.NMI or RoutineType.IRQ)
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
            for (int i = 0; i < treeNodes.Count; i++)
                treeNodes[i].HotPath = (i < hotPathCount);
        }

        private void PopulateCallTreeNodeList(CallTreeNode treeNode, List<CallTreeNode> treeNodeList)
        {
            treeNodeList.Add(treeNode);
            foreach (var child in treeNode.Children)
                PopulateCallTreeNodeList(child, treeNodeList);
        }

        //
        // instruction metrics...
        //
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
                        {
                            metrics.InclusiveCycleCount += childStackFrame.CPUMetrics.InclusiveCycleCount;
                            if (childStackFrame.Type == CallType.TailCall)
                                metrics.TailCall |= true;
                        }
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
                    var coreInstruction = new CoreInstruction(ref instruction);
                    var metrics = instructionMetrics.TryGetValue(coreInstruction, out var existing)
                        ? existing
                        : instructionMetrics[coreInstruction] = new InstructionMetrics(coreInstruction, instructionOrdinal++);

                    metrics.InclusiveCycleCount += instructionCycleCount;
                    metrics.ExecutionCount += 1;

                    if (_InstructionSet!.IsBranch(instruction.Opcode) && instructionCycleCount > 2)
                        metrics.BranchCount += 1;

                    previousInstructionIndex = instructionIndex;
                }

                cycleCount += instructionCycleCount;
                instructionIndex++;
            }
        }

        private void IdentifyModifiedCode(List<InstructionMetrics> instructionMetrics)
        {
            // construct dictionary that maps addresses to number of distinct instructions
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

        //
        // caller / callee metrics...
        //
        public List<RoutineMetrics> GetCallerMetrics(Routine routine)
        {
            Dictionary<CallStack, CPUMetrics> callerMetrics = new();

            foreach (var kvp in routine!.MetricsByStack)
            {
                if (kvp.Key.Parent == null)
                    continue;

                callerMetrics.Add(kvp.Key.Parent, kvp.Value.Clone());
            }

            return ToList(callerMetrics);
        }

        public List<RoutineMetrics> GetCalleeMetrics(Routine routine)
        {
            Dictionary<CallStack, CPUMetrics> calleeMetrics = new();

            foreach (var stackFrame in routine!.StackFrames)
            {
                if (StartCycleCount > stackFrame.EndCycleCount || EndCycleCount < stackFrame.StartCycleCount)
                    continue;

                foreach (var child in stackFrame.Children)
                {
                    if (child.Type != CallType.IRQ && child.Type != CallType.NMI || child.Type != CallType.BRK)
                        continue;

                    if (!calleeMetrics.ContainsKey(child))
                        if (child.Routine.MetricsByStack.TryGetValue(child, out var metrics))
                            calleeMetrics[child] = metrics.Clone();
                }
            }

            return ToList(calleeMetrics);
        }

        private List<RoutineMetrics> ToList(Dictionary<CallStack, CPUMetrics> routineMetrics)
        {
            Dictionary<Routine, CPUMetrics> metricsByRoutine = new();
            foreach (var kvp in routineMetrics)
            {
                var routine = kvp.Key.Routine;
                var metrics = metricsByRoutine.TryGetValue(routine, out var existing)
                    ? existing
                    : metricsByRoutine[routine] = new CPUMetrics();
                metrics.Add(kvp.Value);
            }

            var list = new List<RoutineMetrics>(metricsByRoutine.Count);
            foreach (var kvp in metricsByRoutine)
                list.Add(new RoutineMetrics { Routine = kvp.Key, CPUMetrics = kvp.Value });

            list.Sort((a, b) => (b.CPUMetrics.InclusiveCycleCount - a.CPUMetrics.InclusiveCycleCount));

            return list;
        }

        public struct RoutineMetrics
        {
            public Routine Routine;
            public CPUMetrics CPUMetrics;
        };

        public Dictionary<CanonicalAddress, Routine> RoutinesByAddress = new();
        public List<Routine> HotRoutines = new();
        public CallTreeNode? ProgramCallTree;
        public CallTreeNode? NonMaskableInterruptCallTree;
        public CallTreeNode? MaskableInterruptCallTree;
        public int FirstInstructionIndex;
        public int LastInstructionIndex;
        public int StartCycleCount;
        public int EndCycleCount;
        public model.StackFrame RootStackFrame = new();

        private Routine? _MaskableISR = null;
        private Routine? _NonMaskableISR = null;
        private SortedCanonicalAddresses _SortedRoutineAddresses = new();
        private Instruction[] _Instructions = [];
        private Dictionary<ushort, string> _Labels = [];
        private InstructionSet? _InstructionSet;
        private MiniStackFrame[] _InitialCallStack = [];
        private byte _InitialStackPointer;
    }
}