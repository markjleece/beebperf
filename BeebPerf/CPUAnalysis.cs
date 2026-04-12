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

using BeebPerf.model;
using System.Diagnostics;

namespace BeebPerf
{
    public class CPUAnalysis
    {
        public CPUAnalysis(LabelResolver labelResolver)
        {
            _LabelResolver = labelResolver;
        }

        public async Task<bool> StaticAnalysisAsync(Model model)
        {
            return await Task.Run(() => 
            {
                Initialize(model);
                CreateRoutines();
                CreateStackFrames();
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
                PopulateCallTrees();
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
            _InitialCallStack = model.Snapshot.StackFrames;
            _InitialStackPointer = model.Snapshot.StackPointer;
        }

        private void CreateRoutines()
        {
            RoutinesByAddress = new();
            _SortedRoutineAddresses = new();

            // create routines invoked by JSR, BRK, IRQ, NMI, RTS, and RTI, whilst collecting branches and jumps
            var branchesAndJumps = CreatePrimaryRoutines();

            // group branches and jumps by routine
            var branchesAndJumpByRoutine = GroupBranchesAndJumps(branchesAndJumps);

            // create additional routines invoked by cross routine jumps and branches
            CreateSecondaryRoutines(branchesAndJumpByRoutine);
        }

        private HashSet<CanonicalAddressPair> CreatePrimaryRoutines()
        {
            // we create all the routines invoked by JSR, BRK, IRQ, NMI, RTS, and RTI by simulating
            // the call stack where we create routines for pushed stack frames. We also create routines
            // for any RTI or RTS which doesn't return to the caller

            HashSet<CanonicalAddressPair> branchesAndJumps = new(1024);
            Stack<MiniStackFrame> stackFrames = new();
            byte stackPointer = _InitialStackPointer;

            void syncStackFrames(byte returnStackPointer)
            {
                while (stackFrames.Count > 1 && returnStackPointer >= stackFrames.Peek().ReturnStackPointer)
                    stackFrames.Pop();
            }

            void pushStackFrame(MiniStackFrame stackFrame)
            {
                syncStackFrames(stackFrame.ReturnStackPointer);
                stackFrames.Push(stackFrame);
                var routine = CreateRoutine(stackFrame.StartAddress);
                switch (stackFrame.CallType)
                {
                    case CallType.IRQ:
                    case CallType.BRK:
                        routine.Label = "IRQ/BRK";
                        _IRQBRKRoutine = routine;
                        break;

                    case CallType.NMI:
                        routine.Label = "NMI";
                        _NMIRoutine = routine;
                        break;
                }
            }

            // first populate the call stack with the initial call stack
            foreach (var stackFrame in _InitialCallStack)
                pushStackFrame(stackFrame);

            // next iterate over instructions simulating the call stack, whilst collecting branches and jumps
            foreach (var instruction in _Instructions)
            {
                if (instruction.IsInstruction)
                {
                    byte opcode = instruction.Opcode;
                    if (opcode == 0x20/*JSR*/)
                    {
                        pushStackFrame(new MiniStackFrame(CallType.JSR, instruction.DestinationAddress, instruction.ReturnAddress, stackPointer));
                        stackPointer -= 2;
                    }
                    else if (opcode == 0x60/*RTS*/)
                    {
                        syncStackFrames(stackPointer);
                        stackPointer += 2;
                        if (stackPointer >= stackFrames.Peek().ReturnStackPointer && stackFrames.Count > 1)
                            stackFrames.Pop();
                        else
                            CreateRoutine(instruction.DestinationAddress);
                    }
                    else if (opcode == 0x00/*BRK*/)
                    {
                        pushStackFrame(new MiniStackFrame(CallType.BRK, instruction.DestinationAddress, instruction.ReturnAddress, stackPointer));
                        stackPointer -= 3;
                    }
                    else if (opcode == 0x40/*RTI*/)
                    {
                        syncStackFrames(stackPointer);
                        stackPointer += 3;
                        if (stackPointer >= stackFrames.Peek().ReturnStackPointer && stackFrames.Count > 1)
                            stackFrames.Pop();
                        else
                            CreateRoutine(instruction.DestinationAddress);
                    }
                    else if (_InstructionSet!.ModifiesStackPointer(opcode))
                    {
                        stackPointer = instruction.StackPointer;
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
                else if (instruction.IsIRQ)
                {
                    pushStackFrame(new MiniStackFrame(CallType.IRQ, instruction.DestinationAddress, instruction.ReturnAddress, stackPointer));
                    stackPointer -= 3;
                }
                else if (instruction.IsNMI)
                {
                    pushStackFrame(new MiniStackFrame(CallType.NMI, instruction.DestinationAddress, instruction.ReturnAddress, stackPointer));
                    stackPointer -= 3;
                }
            }

            return branchesAndJumps;
        }

        private Dictionary<CanonicalAddress/*routine*/, HashSet<CanonicalAddressPair>> GroupBranchesAndJumps(HashSet<CanonicalAddressPair> branchesAndJumps)
        { 
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

            return branchesAndJumpsByRoutine;
        }

        private void CreateSecondaryRoutines(Dictionary<CanonicalAddress/*routine*/, HashSet<CanonicalAddressPair>> branchesAndJumpsByRoutine)
        {
            // now we have an initial set of routines and a set of branches and jumps, so we create additional
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

                    CreateRoutine(newRoutine);

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

        private Routine CreateRoutine(CanonicalAddress address)
        {
            if (RoutinesByAddress.TryGetValue(address, out Routine? routine))
                return routine;

            string label = _LabelResolver.Resolve(address);
            routine = new Routine(address, label);

            RoutinesByAddress.Add(address, routine);
            _SortedRoutineAddresses.Add(address);

            return routine;
        }

        private Routine CreatePageBaseRoutine(MemoryPage page)
        {
            ushort baseAddress = page switch
            {
                MemoryPage.WholeRam => 0x0000,
                MemoryPage.ShadowRam => 0x3000,
                MemoryPage.PrivateRam => 0x8000,
                MemoryPage.FilingSystemRam => 0xC000,
                MemoryPage.HiddenRam => 0x0000,
                _ => 0x8000
            };

            return CreateRoutine(new CanonicalAddress(baseAddress, page));
        }

        public void CreateStackFrames()
        {
            // create initial call stack
            var currentStackFrame = CreateInitialStackFrames();

            // iterate over instructions, creating remaining stack frames
            CreateRemainingStackFrames(currentStackFrame);

            // review inserted stack-frames, as their associated routine may be incorrect
            ReviewInsertedStackFrames(currentStackFrame);

            // populate effective first & last instruction indices
            model.StackFrame.ComputeInstructionIndices(RootStackFrame, _Instructions.Length);

#if DEBUG && STACKFRAME_INVARIANT
            model.StackFrame.AssertInvariant(RootStackFrame, _Instructions, _InstructionSet!);
#endif
        }

        private model.StackFrame? CreateInitialStackFrames()
        {
            // reconstruct the initial call stack inserting tail calls when
            // the return address is not within the parent

            model.StackFrame? currentStackFrame = null;

            foreach (var stackFrame in _InitialCallStack)
            {
                if (currentStackFrame != null)
                {
                    // is the return address within the parent routine?
                    var returnRoutineAddress = _SortedRoutineAddresses.Find(stackFrame.ReturnAddress);
                    if (!returnRoutineAddress.Equals(currentStackFrame!.StartAddress))
                    {
                        // create stack frame for tail call, creating a base routine if needed
                        Routine returnRoutine;
                        if (returnRoutineAddress.Address != 0)
                            returnRoutine = RoutinesByAddress[returnRoutineAddress];
                        else
                            returnRoutine = CreatePageBaseRoutine(returnRoutineAddress.Page);

                        currentStackFrame = CreateStackFrame(
                            CallType.TailCall,
                            returnRoutine,
                            currentStackFrame.ReturnAddress,
                            currentStackFrame.ReturnStackPointer,
                            startCycleCount: 0,
                            parent: currentStackFrame);

                        // mark as inserted
                        currentStackFrame.InsertedTailCall = true;

                        // ensure the stack frame's routine is updated if no instructions
                        // fall within the stack frame
                        currentStackFrame.LowestAddress = stackFrame.ReturnAddress.Address;
                        currentStackFrame.HighestAddress = stackFrame.ReturnAddress.Address;
                    }
                }

                currentStackFrame = CreateStackFrame(
                    stackFrame.CallType,
                    RoutinesByAddress[stackFrame.StartAddress],
                    stackFrame.ReturnAddress,
                    stackFrame.ReturnStackPointer,
                    startCycleCount: 0,
                    parent: currentStackFrame);
            }

            // if first instruction within in a tail-call?
            var firstRoutineAddress = _SortedRoutineAddresses.Find(_Instructions[0].OpcodeAddress);
            if (!currentStackFrame!.StartAddress.Equals(firstRoutineAddress))
            {
                // create stack frame for tail call, creating a base routine if needed
                Routine firstRoutine;
                if (firstRoutineAddress.Address != 0)
                    firstRoutine = RoutinesByAddress[firstRoutineAddress];
                else
                    firstRoutine = CreatePageBaseRoutine(firstRoutineAddress.Page);

                currentStackFrame = CreateStackFrame(
                    CallType.TailCall,
                    firstRoutine,
                    currentStackFrame.ReturnAddress,
                    currentStackFrame.ReturnStackPointer,
                    startCycleCount: 0,
                    parent: currentStackFrame);

                // mark as inserted
                currentStackFrame.InsertedTailCall = true;
            }

            return currentStackFrame;
        }

        private void CreateRemainingStackFrames(model.StackFrame? currentStackFrame)
        {
            byte stackPointer = _InitialStackPointer;
            int cycleCount = 0;

            bool syncStackFrames(int postCycleCount)
            {
                bool stackModified = false;
                while (stackPointer >= currentStackFrame!.ReturnStackPointer && currentStackFrame!.Parent != null)
                {
                    if (currentStackFrame.IsEmpty())
                        currentStackFrame.Parent.Children.Remove(currentStackFrame);

                    currentStackFrame.EndCycleCount = postCycleCount;
                    currentStackFrame = currentStackFrame.Parent;
                    stackModified = true;
                }
                return stackModified;
            }

            for (int instructionIndex = 0; instructionIndex < _Instructions.Length; instructionIndex++)
            {
                ref Instruction instruction = ref _Instructions[instructionIndex];

                bool isFirstInstruction = (instructionIndex == 0);
                bool isLastInstruction = (instructionIndex == _Instructions.Length - 1);

                int postCycleCount = cycleCount + instruction.CycleCount;

                if (instruction.IsInstruction)
                {
                    // is first instruction of a routine?
                    if (RoutinesByAddress.TryGetValue(instruction.OpcodeAddress, out Routine? routine) && !isFirstInstruction)
                        if (routine != currentStackFrame!.Routine) // fall through?
                            currentStackFrame = CreateStackFrame(
                                CallType.FallThrough,
                                routine,
                                currentStackFrame.ReturnAddress,
                                currentStackFrame.ReturnStackPointer,
                                cycleCount,
                                parent: currentStackFrame);

                    // update self instruction indices to include instruction
                    if (currentStackFrame!.FirstSelfInstructionIndex < 0)
                        currentStackFrame.FirstSelfInstructionIndex = instructionIndex;

                    if (currentStackFrame.LastSelfInstructionIndex < instructionIndex)
                        currentStackFrame.LastSelfInstructionIndex = instructionIndex;

                    // update start and end addresses
                    ushort instructionAddress = instruction.OpcodeAddress.Address;
                    if (currentStackFrame.LowestAddress < instructionAddress)
                        currentStackFrame.LowestAddress = instructionAddress;

                    if (currentStackFrame.HighestAddress > instructionAddress)
                        currentStackFrame.HighestAddress = instructionAddress;

                    // update routine's end address, but not if the stack frame was inserted
                    if (!currentStackFrame.InsertedTailCall &&
                        currentStackFrame.Routine.EndAddress < instructionAddress)
                        currentStackFrame.Routine.EndAddress = instructionAddress;

                    if (instruction.Opcode == 0x00/*BRK*/ && !isLastInstruction)
                    {
                        // create new stack frame for BRK
                        syncStackFrames(postCycleCount);
                        currentStackFrame = CreateStackFrame(
                            CallType.BRK,
                            RoutinesByAddress[instruction.DestinationAddress],
                            returnAddress: instruction.OpcodeAddress.Offset(2),
                            returnStackPointer: stackPointer,
                            postCycleCount,
                            parent: currentStackFrame);
                        stackPointer -= 3;
                    }
                    else if (instruction.Opcode == 0x20/*JSR*/ && !isLastInstruction)
                    {
                        // create new stack frame for JSR
                        syncStackFrames(postCycleCount);
                        currentStackFrame = CreateStackFrame(
                            CallType.JSR,
                            RoutinesByAddress[instruction.DestinationAddress],
                            returnAddress: instruction.OpcodeAddress.Offset(3),
                            returnStackPointer: stackPointer,
                            postCycleCount,
                            parent: currentStackFrame);
                        stackPointer -= 2;
                    }
                    else if (_InstructionSet!.IsBranchOrJump(instruction.Opcode) && instruction.CycleCount > 2 &&
                             RoutinesByAddress.TryGetValue(instruction.DestinationAddress, out Routine? destinationRoutine) &&
                             destinationRoutine != currentStackFrame.Routine)
                    {
                        // create new stack frame for tail call
                        syncStackFrames(postCycleCount);
                        currentStackFrame = CreateStackFrame(
                            CallType.TailCall,
                            RoutinesByAddress[instruction.DestinationAddress],
                            currentStackFrame.ReturnAddress,
                            currentStackFrame.ReturnStackPointer,
                            postCycleCount,
                            parent: currentStackFrame);
                    }
                    else if (instruction.Opcode == 0x60/*RTS*/ || instruction.Opcode == 0x40/*RTI*/)
                    {
                        syncStackFrames(postCycleCount);
                        stackPointer += (byte)(instruction.Opcode == 0x60 ? 2/*RTS*/ : 3/*RTI*/);
                        if (!syncStackFrames(postCycleCount))
                        {
                            // create tail call as RTS/RTI didn't return to caller
                            currentStackFrame = CreateStackFrame(
                                CallType.TailCall,
                                RoutinesByAddress[instruction.DestinationAddress],
                                currentStackFrame.ReturnAddress,
                                currentStackFrame.ReturnStackPointer,
                                postCycleCount,
                                parent: currentStackFrame);
                        }
                    }
                    else if (_InstructionSet!.ModifiesStackPointer(instruction.Opcode) && !isLastInstruction)
                    {
                        stackPointer = instruction.StackPointer;
                    }
                }
                else if ((instruction.IsIRQ || instruction.IsNMI) && !isLastInstruction)
                {
                    // create new stack frame for IRQ/NMI
                    syncStackFrames(cycleCount);
                    currentStackFrame = CreateStackFrame(
                        instruction.IsIRQ ? CallType.IRQ : CallType.NMI,
                        RoutinesByAddress[instruction.DestinationAddress],
                        instruction.ReturnAddress,
                        stackPointer,
                        cycleCount,
                        parent: currentStackFrame);
                    stackPointer -= 3;

                    // set self instruction indices to include interrupt instruction
                    currentStackFrame.FirstSelfInstructionIndex = instructionIndex;
                    currentStackFrame.LastSelfInstructionIndex = instructionIndex;
                }

                cycleCount = postCycleCount;
            }

            // unwind residual stack, setting end cycle counts, whilst removing any empty stack frames
            syncStackFrames(cycleCount);

            int count = 0;
            while (currentStackFrame!.Parent != null)
            {
                if (currentStackFrame.IsEmpty())
                    currentStackFrame.Parent.Children.Remove(currentStackFrame);

                currentStackFrame.EndCycleCount = cycleCount;
                currentStackFrame = currentStackFrame.Parent;
                count++;
            }
            currentStackFrame!.EndCycleCount = cycleCount;
            Debug.Assert(count < 50);

            // set members
            RootStackFrame = currentStackFrame;
            StartCycleCount = 0;
            EndCycleCount = cycleCount;
        }

        private void ReviewInsertedStackFrames(model.StackFrame? stackFrame)
        {
            while (stackFrame != null)
            {
                if (stackFrame.InsertedTailCall)
                {
                    // do the stack frames instructions span a different address
                    // range than its current routine?
                    if (stackFrame.Routine.EndAddress < stackFrame.LowestAddress)
                    {
                        // create a new routine, updating the stack frame to reference it
                        var routineAddress = new CanonicalAddress(stackFrame.LowestAddress, stackFrame.Routine.StartAddress.Page);
                        stackFrame.Routine.StackFrames.Remove(stackFrame);
                        stackFrame.StartAddress = routineAddress;
                        stackFrame.Routine = CreateRoutine(routineAddress);
                        stackFrame.Routine.StackFrames.Add(stackFrame);
                        stackFrame.Routine.EndAddress = stackFrame.HighestAddress;
                    }
                    else if (stackFrame.Routine.EndAddress < stackFrame.HighestAddress)
                        stackFrame.Routine.EndAddress = stackFrame.HighestAddress;
                }

                stackFrame = stackFrame.Parent;
            }
        }

        private model.StackFrame CreateStackFrame(
            CallType type,
            Routine routine,
            CanonicalAddress returnAddress,
            byte returnStackPointer,
            int startCycleCount,
            model.StackFrame? parent)
        {
            model.StackFrame stackFrame = new(routine, returnAddress, returnStackPointer, type, parent);
            stackFrame.StartCycleCount = startCycleCount;
            if (parent != null)
                parent.Children.Add(stackFrame);
            return stackFrame;
        }

        public void ResolveRoutineLabels()
        {
            foreach (var routine in RoutinesByAddress.Values)
            {
                if (routine == _IRQBRKRoutine || routine == _NMIRoutine)
                    continue;
                routine.Label = _LabelResolver.Resolve(routine.StartAddress);
            }
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

            StartCycleCount = cycleCount;

            while (instructionIndex < _Instructions.Length)
            {
                cycleCount += _Instructions[instructionIndex++].CycleCount;
                if (cycleCount >= endCycleCount)
                    break;
            }

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
                excludedCycleCount = CalculatedExcludedCycles(stackFrame);

            int childInclusiveCycleCount = 0;
            int childElapsedCycleCount = 0;

            foreach (var childStackFrame in stackFrame.Children)
            {
                excludedCycleCount += CalculateMetrics(childStackFrame);
                childElapsedCycleCount += childStackFrame.CPUMetrics.ElapsedCycleCount;
                if (childStackFrame.CallType != CallType.IRQ && childStackFrame.CallType != CallType.NMI && childStackFrame.CallType != CallType.BRK)
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

            int childIndex = 0;
            int excludedCycleCount = 0;
            int cycleCount = stackFrame.StartCycleCount;
            int instructionIndex = stackFrame.FirstInstructionIndex;
            int lastInstructionIndex = stackFrame.LastInstructionIndex;

            model.StackFrame? childStackFrame = (stackFrame.Children.Count > 0) ? stackFrame.Children[0] : null;

            while (cycleCount <= stackFrame.EndCycleCount && instructionIndex <= lastInstructionIndex)
            {
                if (childStackFrame is not null && cycleCount >= childStackFrame.StartCycleCount)
                {
                    // skip over child stack frames
                    instructionIndex = childStackFrame.LastInstructionIndex + 1;
                    cycleCount = childStackFrame.EndCycleCount;

                    childStackFrame = (++childIndex < stackFrame.Children.Count) ? stackFrame.Children[childIndex] : null;
                    continue;
                }

                // update cycle counts
                ref Instruction instruction = ref _Instructions[instructionIndex];
                int instructionCycleCount = instruction.CycleCount;
                if (cycleCount < StartCycleCount || cycleCount >= EndCycleCount)
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

        private void PopulateCallTrees()
        {
            ProgramCallTree = null;
            IRQBRKCallTree = null;
            NMICallTree = null;

            var treeNodesByCallStack = new Dictionary<CallStack, CallTreeNode>();
            PopulateCallTrees(RootStackFrame, parentCallTreeNode: null, treeNodesByCallStack);

            if (ProgramCallTree != null)
                ProgramCallTree.Sort(CallTreeNode.SortField.InclusiveCPU, SortOrder.Descending);

            if (IRQBRKCallTree != null)
                IRQBRKCallTree.Sort(CallTreeNode.SortField.InclusiveCPU, SortOrder.Descending);

            if (NMICallTree != null)
                NMICallTree.Sort(CallTreeNode.SortField.InclusiveCPU, SortOrder.Descending);
        }

        private void PopulateCallTrees(
            model.StackFrame stackFrame,
            CallTreeNode? parentCallTreeNode,
            Dictionary<CallStack, CallTreeNode> treeNodesByCallStack)
        {
            var callStack = (CallStack)stackFrame;

            if (!stackFrame.Routine.MetricsByStack.ContainsKey(callStack))
                return;

            CallTreeNode callTreeNode;
            if (!treeNodesByCallStack.TryGetValue(callStack, out callTreeNode!))
            {
                callTreeNode = new CallTreeNode(callStack);
                treeNodesByCallStack.Add(callStack, callTreeNode);

                switch (stackFrame.CallType)
                {
                    case CallType.None:
                        ProgramCallTree = callTreeNode;
                        break;

                    case CallType.IRQ:
                    case CallType.BRK:
                        IRQBRKCallTree = callTreeNode;
                        break;

                    case CallType.NMI:
                        NMICallTree = callTreeNode;
                        break;

                    default:
                        if (parentCallTreeNode != null)
                            parentCallTreeNode.AddChild(callTreeNode);
                        break;
                }
            }

            foreach (var childStackFrame in stackFrame.Children)
                PopulateCallTrees(childStackFrame, callTreeNode, treeNodesByCallStack);
        }

        private void MarkHotPaths()
        {
            int count = 0;
            if (ProgramCallTree != null)
                count += ProgramCallTree.Count;
            if (NMICallTree != null)
                count += NMICallTree.Count;
            if (IRQBRKCallTree != null)
                count += IRQBRKCallTree.Count;

            var treeNodes = new List<CallTreeNode>(count);
            if (ProgramCallTree != null)
                PopulateCallTreeNodeList(ProgramCallTree, treeNodes);
            if (NMICallTree != null)
                PopulateCallTreeNodeList(NMICallTree, treeNodes);
            if (IRQBRKCallTree != null)
                PopulateCallTreeNodeList(IRQBRKCallTree, treeNodes);

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
            int cycleCount = stackFrame.StartCycleCount;
            int instructionIndex = stackFrame.FirstInstructionIndex;
            int lastInstructionIndex = stackFrame.LastInstructionIndex;
            int previousInstructionIndex = instructionIndex;

            model.StackFrame? childStackFrame = (stackFrame.Children.Count > 0) ? stackFrame.Children[0] : null;

            while (cycleCount <= stackFrame.EndCycleCount && instructionIndex <= lastInstructionIndex)
            {
                if (childStackFrame != null && cycleCount >= childStackFrame.StartCycleCount)
                {
                    if (previousInstructionIndex > -1 &&
                        (childStackFrame.CallType == CallType.JSR || childStackFrame.CallType == CallType.TailCall))
                    {
                        var previousInstruction = new CoreInstruction(ref _Instructions[previousInstructionIndex]);
                        if (instructionMetrics.TryGetValue(previousInstruction, out var metrics))
                        {
                            metrics.InclusiveCycleCount += childStackFrame.CPUMetrics.InclusiveCycleCount;
                            if (childStackFrame.CallType == CallType.TailCall)
                                metrics.TailCall = true;
                        }
                    }

                    if (childStackFrame.CallType == CallType.FallThrough)
                        CalculateStackFrameMetrics(childStackFrame, ref instructionOrdinal, instructionMetrics);

                    // skip over child stack frames
                    instructionIndex = childStackFrame.LastInstructionIndex + 1;
                    cycleCount = childStackFrame.EndCycleCount;

                    childStackFrame = (++childIndex < stackFrame.Children.Count) ? stackFrame.Children[childIndex] : null;
                    continue;
                }

                // update cycle counts
                ref Instruction instruction = ref _Instructions[instructionIndex];
                int instructionCycleCount = instruction.CycleCount;
                if (instruction.IsInstruction &&
                    cycleCount >= StartCycleCount && cycleCount < EndCycleCount)
                {
                    var coreInstruction = new CoreInstruction(ref instruction);
                    var metrics = instructionMetrics.TryGetValue(coreInstruction, out var existing)
                        ? existing
                        : instructionMetrics[coreInstruction] = new InstructionMetrics(coreInstruction, instructionOrdinal++);

                    metrics.InclusiveCycleCount += instructionCycleCount;
                    metrics.SelfCycleCount += instructionCycleCount;
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

            if (routine == _IRQBRKRoutine || routine == _NMIRoutine)
                return [];

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
                    if (child.CallType == CallType.IRQ || child.CallType == CallType.NMI || child.CallType == CallType.BRK)
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
        public CallTreeNode? NMICallTree;
        public CallTreeNode? IRQBRKCallTree;
        public int StartCycleCount;
        public int EndCycleCount;
        public model.StackFrame RootStackFrame = new();

        private Routine? _IRQBRKRoutine = null;
        private Routine? _NMIRoutine = null;
        private SortedCanonicalAddresses _SortedRoutineAddresses = new();
        private Instruction[] _Instructions = [];
        private LabelResolver _LabelResolver = new();
        private InstructionSet? _InstructionSet;
        private MiniStackFrame[] _InitialCallStack = [];
        private byte _InitialStackPointer;
    }
}