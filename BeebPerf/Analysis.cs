using System.Diagnostics;
using System.Text;

namespace BeebPerf
{
    public class Analysis
    {
        public Analysis(Model model)
        {
            _Instructions = model.Instructions;
            _Labels = model.Labels;

            _BranchOrJumpOpcodeTable = [
             // 0 1 2 3 4 5 6 7 8 9 a b c d e f
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 0
                1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 1
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 2
                1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 3
                0,0,0,0,0,0,0,0,0,0,0,0,1,0,0,0, // 4
                1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 5
                0,0,0,0,0,0,0,0,0,0,0,0,1,0,0,0, // 6
                1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 7
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 8
                1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 9
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // a
                1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // b
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // c
                1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // d
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // e
                1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0];// f

            if (model.CPU == Model.CPUType._6502)
                _BranchOrJumpOpcodeTable[0x7C] = 1; // add JMP (abs,X)
        }

        public void StaticCPUAnalysis()
        {
            IdentifyRoutines();
            IdentifyStackFrames();
        }

        public void DynamicCPUAnalysis(int startCycleCount, int endCycleCount)
        {
            CalculateCPUMetrics(startCycleCount, endCycleCount);
            PopulateHotRoutines();
            PopulateProgramCallTree();
            PopulateNonMaskableInterruptCallTree();
            PopulateMaskableInterruptCallTree();
        }

        private int CalculatedExcludedCycles(StackFrame stackFrame, int startCycleCount, int endCycleCount)
        {
            Debug.Assert(
                (startCycleCount > stackFrame.StartCycleCount && startCycleCount <= stackFrame.EndCycleCount) ||
                (endCycleCount >= stackFrame.StartCycleCount && endCycleCount < stackFrame.EndCycleCount));

            int excludedCycleCount = 0;
            int childIndex = 0;
            int instructionIndex = stackFrame.FirstInstructionIndex;
            int cycleCount = stackFrame.StartCycleCount;

            StackFrame? childStackFrame = (stackFrame.Children.Count > 0) ? stackFrame.Children[0] : null;

            while (cycleCount <= stackFrame.EndCycleCount && instructionIndex <= stackFrame.LastInstructionIndex)
            {
                if (childStackFrame is not null && cycleCount == childStackFrame.StartCycleCount)
                {
                    // skip over child stack frame
                    instructionIndex = childStackFrame.LastInstructionIndex + 1;
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

        private struct CPUMetricResults
        {
            public int ExcludedCycleCount;
            public int ElapsedCycleCount;
            public int InclusiveCycleCount;
        }

        private CPUMetricResults CalculateCPUMetrics(StackFrame stackFrame, int startCycleCount, int endCycleCount)
        {
            if (startCycleCount > stackFrame.EndCycleCount || endCycleCount < stackFrame.StartCycleCount)
            {
                return new() 
                {
                    ExcludedCycleCount = (stackFrame.EndCycleCount - stackFrame.StartCycleCount),
                    ElapsedCycleCount = 0,
                    InclusiveCycleCount = 0
                };
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
                var childCPUMetrics = CalculateCPUMetrics(childStackFrame, startCycleCount, endCycleCount);
                excludedCycleCount += childCPUMetrics.ExcludedCycleCount;
                childElapsedCycleCount += childCPUMetrics.ElapsedCycleCount;
                if (childStackFrame.Type != StackFrameType.ISR)
                    childInclusiveCycleCount += childCPUMetrics.InclusiveCycleCount;
            }

            int elapsedCycleCount = (stackFrame.EndCycleCount - stackFrame.StartCycleCount) - excludedCycleCount;
            int selfCycleCount = elapsedCycleCount - childElapsedCycleCount;
            int inclusiveCycleCount = selfCycleCount + childInclusiveCycleCount;

            Debug.Assert(elapsedCycleCount >= 0 && selfCycleCount >= 0 && inclusiveCycleCount >= 0);
            Debug.Assert(inclusiveCycleCount <= elapsedCycleCount && selfCycleCount <= elapsedCycleCount && selfCycleCount <= inclusiveCycleCount);

            CPUMetrics cpuMetrics = new() 
            {
                Count = 1,
                ElapsedCycleCount = elapsedCycleCount,
                SelfCycleCount = selfCycleCount,
                InclusiveCycleCount = inclusiveCycleCount
            };

            if (!stackFrame.Routine.CPUMetricsByStack.TryAdd(stackFrame, cpuMetrics))
                stackFrame.Routine.CPUMetricsByStack[stackFrame].Add(cpuMetrics);

            stackFrame.Routine.AggregateCPUMetrics.Add(cpuMetrics);

            return new() 
            {
                ExcludedCycleCount = excludedCycleCount,
                ElapsedCycleCount = elapsedCycleCount,
                InclusiveCycleCount = inclusiveCycleCount
            };
        }

        private void IdentifyRoutines()
        {
            _RoutinesByAddress = new();
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
                else if (_BranchOrJumpOpcodeTable[instruction.Opcode] != 0)
                {
                    CanonicalAddress destination = instruction.DestinationAddress;
                    if ((instruction.Opcode & 0x0F) == 0) // is branch?
                    {
                        int branchedAddress = (int)instruction.OpcodeAddress.Address + 2 + unchecked((sbyte)instruction.Operand);
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
            var allRoutines = _RoutinesByAddress.Keys.ToList<CanonicalAddress>();
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
                    if (_RoutinesByAddress.ContainsKey(destinationAddress))
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

                    // redistribute the the branches and jumps across the two routines
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
            if (_RoutinesByAddress.TryGetValue(address, out Routine? routine))
                return routine;

            string label = _Labels.TryGetValue(address.Address, out var lbl) ? lbl : string.Empty;
            routine = new Routine(address, routineType, label);

            if (routineType == RoutineType.NonMaskableISR)
                _NonMaskableISR = routine;
            else if (routineType == RoutineType.MaskableISR)
                _MaskableISR = routine;

            _RoutinesByAddress.Add(address, routine);
            _SortedRoutineAddresses.Add(address);

            Debug.WriteLine($"CreateRoutine( 0x{address.Page:X2}{address.Address:X2}{label}, {routineType.ToString()} )");

            return routine;
        }

        private Routine GetRoutine(CanonicalAddress address)
        {
            CanonicalAddress routineAddress = _SortedRoutineAddresses.Find(address);
            if (_RoutinesByAddress.TryGetValue(routineAddress, out Routine? routine))
                return routine;

            return CreateRoutine(address, RoutineType.Unknown);
        }

        public void IdentifyStackFrames()
        {
            int cycleCount = 0;

            // create initial stack frame
            ref Instruction firstInstruction = ref _Instructions[0];
            Routine firstRoutine = GetRoutine(firstInstruction.OpcodeAddress);
            StackFrame currentStackFrame = CreateStackFrame(StackFrameType.Unknown, firstRoutine, startCycleCount: 0, parent: null);

            // for every instruction...
            for (int instructionIndex = 0; instructionIndex < _Instructions.Length; instructionIndex++)
            {
                ref Instruction instruction = ref _Instructions[instructionIndex];

                bool isFirstInstruction = (instructionIndex == 0);
                bool isLastInstruction = (instructionIndex == _Instructions.Length - 1);

                int postCycleCount = cycleCount + instruction.CycleCount;

                if (instruction.IsInstruction)
                {
                    // is first instruction of a routine?
                    if (_RoutinesByAddress.TryGetValue(instruction.OpcodeAddress, out Routine? routine) && !isFirstInstruction)
                        if (routine != currentStackFrame.Routine) // tail call?
                            currentStackFrame = CreateStackFrame(StackFrameType.TailCall, routine, cycleCount, parent: currentStackFrame); // create stack frame for tail-call

                    // update instruction indices
                    if (currentStackFrame.FirstInstructionIndex == 0)
                        currentStackFrame.FirstInstructionIndex = instructionIndex;

                    if (currentStackFrame.LastInstructionIndex < instructionIndex)
                        currentStackFrame.LastInstructionIndex = instructionIndex;

                    if (instruction.Opcode == 0x20/*JSR*/ && !isLastInstruction)
                    {
                        // create new stack frame for routine
                        Routine jsrRoutine = _RoutinesByAddress[instruction.DestinationAddress];
                        currentStackFrame = CreateStackFrame(StackFrameType.JSR, jsrRoutine, postCycleCount, parent: currentStackFrame);
                    }
                    else if (instruction.Opcode == 0x60/*RTS*/ || instruction.Opcode == 0x40/*RTI*/)
                    {
                        // unwind any tail-calls
                        while (true)
                        {
                            currentStackFrame.EndCycleCount = postCycleCount;

                            if (currentStackFrame.Parent is null || (currentStackFrame.Type != StackFrameType.TailCall))
                                break;

                            currentStackFrame = currentStackFrame.Parent;
                        }

                        // unwind routine
                        if (currentStackFrame.Parent is null)
                        {
                            // create new root stack frame
                            Routine rootRoutine = GetRoutine(instruction.DestinationAddress);
                            StackFrame newRoot = new(rootRoutine, StackFrameType.Unknown, parent: null);
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
                    // create new stack frame for interrupt service routine
                    Routine isrRoutine = _RoutinesByAddress[instruction.ISRAddress];
                    currentStackFrame = CreateStackFrame(StackFrameType.ISR, isrRoutine, postCycleCount, parent: currentStackFrame);
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

        private StackFrame CreateStackFrame(StackFrameType type, Routine routine, int startCycleCount, StackFrame? parent)
        {
            StackFrame stackFrame = new(routine, type, parent);
            stackFrame.StartCycleCount = startCycleCount;
            if (parent != null)
                parent.Children.Add(stackFrame);
            return stackFrame;
        }

        private void ValidateStackFrame(StackFrame stackFrame)
        {
            Debug.Assert(stackFrame.StartCycleCount == 0 || stackFrame.FirstInstructionIndex > 0);
            Debug.Assert(stackFrame.LastInstructionIndex > 0);
            Debug.Assert(stackFrame.StartCycleCount <= stackFrame.EndCycleCount);
            Debug.Assert(stackFrame.FirstInstructionIndex <= stackFrame.LastInstructionIndex);

            StackFrame? lastChildStackFrame = null;
            foreach (StackFrame childStackFrame in stackFrame.Children)
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

        private void CalculateCPUMetrics(int startCycleCount, int endCycleCount)
        {
            if (startCycleCount < StartCycleCount)
                startCycleCount = StartCycleCount;

            if (endCycleCount < EndCycleCount)
                endCycleCount = EndCycleCount;

            foreach (var routine in _RoutinesByAddress.Values)
            {
                routine.CPUMetricsByStack.Clear();
                routine.AggregateCPUMetrics.Clear();
            }

            CalculateCPUMetrics(_RootStackFrame, startCycleCount, endCycleCount);
        }

        private void PopulateHotRoutines()
        {
            HotRoutines.Clear();
            foreach (var routine in _RoutinesByAddress.Values)
                HotRoutines.Add(routine);
            HotRoutines.Sort((a, b) => -a.AggregateCPUMetrics.SelfCycleCount.CompareTo(b.AggregateCPUMetrics.SelfCycleCount));

            Debug.WriteLine("Hot routines:");
            foreach (var routine in HotRoutines)
                DebugPrintLine(indent: 0, routine, routine.AggregateCPUMetrics);
        }

        private void PopulateProgramCallTree()
        {
            Dictionary<StackFrame, CallTreeNode> treeNodesByStack = new();
            ProgramCallTree = new CallTreeNode(_RootStackFrame);
            treeNodesByStack.Add(_RootStackFrame, ProgramCallTree);

            foreach (var routine in _RoutinesByAddress.Values)
                foreach (var stackFrame in routine.CPUMetricsByStack.Keys)
                    PopulateProgramCallTree(stackFrame, ProgramCallTree, treeNodesByStack);

            SortTree(ProgramCallTree);

            Debug.WriteLine("Program stack:");
            DebugPrintTree(ProgramCallTree, depth: 0);
        }

        private static CallTreeNode? PopulateProgramCallTree(StackFrame stackFrame, CallTreeNode rootTreeNode, Dictionary<StackFrame, CallTreeNode> treeNodesByStack)
        {
            if (treeNodesByStack.TryGetValue(stackFrame, out CallTreeNode? treeNode))
                return treeNode;

            if (stackFrame.Routine.RoutineType is RoutineType.NonMaskableISR or RoutineType.MaskableISR)
                return null;

            var newTreeNode = new CallTreeNode(stackFrame);

            var parentStackFrame = stackFrame.Parent;
            if (parentStackFrame is not null)
            {
                var parentTreeNode = PopulateProgramCallTree(parentStackFrame, rootTreeNode, treeNodesByStack);
                if (parentTreeNode is null)
                    return null;

                parentTreeNode.Children.Add(newTreeNode);
            }

            treeNodesByStack[stackFrame] = newTreeNode;

            return newTreeNode;
        }

        private void PopulateNonMaskableInterruptCallTree()
        {
            NonMaskableInterruptCallTree = null;
            if (_NonMaskableISR is null)
                return;

            Dictionary<StackFrame, CallTreeNode> interruptTreeNodesByStack = new();
            StackFrame stack = _NonMaskableISR.CPUMetricsByStack.Keys.First();
            NonMaskableInterruptCallTree = new CallTreeNode(stack);
            interruptTreeNodesByStack.Add(stack, NonMaskableInterruptCallTree);

            foreach (var routine in _RoutinesByAddress.Values)
                foreach (var stackFrame in routine.CPUMetricsByStack.Keys)
                    PopulateInterruptCallTree(stackFrame, NonMaskableInterruptCallTree, interruptTreeNodesByStack);

            SortTree(NonMaskableInterruptCallTree);

            Debug.WriteLine("Non Maskable Interrupt stack:");
            DebugPrintTree(NonMaskableInterruptCallTree, depth: 0);
        }

        private void PopulateMaskableInterruptCallTree()
        {
            MaskableInterruptCallTree = null;
            if (_MaskableISR is null)
                return;

            Dictionary<StackFrame, CallTreeNode> interruptTreeNodesByStack = new();
            StackFrame stack = _MaskableISR.CPUMetricsByStack.Keys.First();
            MaskableInterruptCallTree = new CallTreeNode(stack);
            interruptTreeNodesByStack.Add(stack, MaskableInterruptCallTree);

            foreach (var routine in _RoutinesByAddress.Values)
                foreach (var stackFrame in routine.CPUMetricsByStack.Keys)
                    PopulateInterruptCallTree(stackFrame, MaskableInterruptCallTree, interruptTreeNodesByStack);

            SortTree(MaskableInterruptCallTree);

            Debug.WriteLine("Maskable Interrupt stack:");
            DebugPrintTree(MaskableInterruptCallTree, depth: 0);
        }

        private static CallTreeNode? PopulateInterruptCallTree(StackFrame stackFrame, CallTreeNode rootTreeNode, Dictionary<StackFrame, CallTreeNode> treeNodesByStack)
        {
            if (treeNodesByStack.TryGetValue(stackFrame, out var treeNode))
                return treeNode;

            if (stackFrame.Parent is null ||
                stackFrame.Routine.RoutineType is RoutineType.NonMaskableISR or RoutineType.MaskableISR)
                return null;

            var newTreeNode = new CallTreeNode(stackFrame);

            var parentStackFrame = stackFrame.Parent;
            if (parentStackFrame is not null)
            {
                CallTreeNode? parentTreeNode = PopulateInterruptCallTree(parentStackFrame, rootTreeNode, treeNodesByStack);
                if (parentTreeNode is null)
                    return null;

                parentTreeNode.Children.Add(newTreeNode);
            }

            treeNodesByStack[stackFrame] = newTreeNode;

            return newTreeNode;
        }


        private static void SortTree(CallTreeNode treeNode)
        {
            treeNode.Children.Sort((a, b) => b.CPUMetrics.InclusiveCycleCount.CompareTo(a.CPUMetrics.InclusiveCycleCount));

            foreach (var childNode in treeNode.Children)
                SortTree(childNode);
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
            Debug.WriteLine(
                $"{"".PadLeft(indent)}" +
                $"0x{routine.StartAddress.Page:X2}:{routine.StartAddress.Address:X4}{routine.Label}, " +
                $"self: {metrics.SelfCycleCount}, " +
                $"inclusive: {metrics.InclusiveCycleCount}, " +
                $"elapsed: {metrics.ElapsedCycleCount}, " +
                $"count: {metrics.Count}");
        }

        public List<Routine> HotRoutines = new();
        public CallTreeNode? ProgramCallTree;
        public CallTreeNode? NonMaskableInterruptCallTree;
        public CallTreeNode? MaskableInterruptCallTree;
        public int StartCycleCount;
        public int EndCycleCount;

        private Routine? _MaskableISR = null;
        private Routine? _NonMaskableISR = null;
        private StackFrame _RootStackFrame = new();
        private Dictionary<CanonicalAddress, Routine> _RoutinesByAddress = new();
        private SortedCanonicalAddresses _SortedRoutineAddresses = new();
        private byte[] _BranchOrJumpOpcodeTable;
        private Instruction[] _Instructions;
        private Dictionary<ushort, string> _Labels;
    }
}