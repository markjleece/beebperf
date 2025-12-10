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

namespace BeebPerf
{
    public class MemoryAnalysis
    {
        public void Initialize(
            model.StackFrame rootStackFrame,
            Instruction[] instructions,
            InstructionSet instructionSet,
            Dictionary<ushort, string> labels,
            byte[][] snapshotMemory)
        {
            _RootStackFrame = rootStackFrame;
            _Instructions = instructions;
            _InstructionSet = instructionSet;
            _Labels = labels;
            _SnapshotMemory = snapshotMemory;
        }

        public async Task<bool> DynamicAnalysisAsync(int startCycleCount, int endCycleCount, bool zeroPage)
        {
            return await Task.Run(() =>
            {
                DynamicAnalysis(startCycleCount, endCycleCount, zeroPage);
                return true;
            });
        }

        public async Task<bool> DynamicAddressAnalysisAsync(CanonicalAddress address, int startCycleCount, int endCycleCount)
        {
            return await Task.Run(() =>
            {
                DynamicAddressAnalysis(address, startCycleCount, endCycleCount);
                return true;
            });
        }

        private void DynamicAnalysis(int startCycleCount, int endCycleCount, bool zeroPage)
        {
            // initialize read/write counters
            int addressCount = 0;
            ulong[][] memory = new ulong[_SnapshotMemory.Length][];
            for (int i = 0; i < _SnapshotMemory.Length; i++)
            {
                if (_SnapshotMemory[i] != null)
                {
                    int pageSize = _SnapshotMemory[i].Length;
                    memory[i] = new ulong[pageSize];
                    addressCount += pageSize;
                }
            }

            ulong[] increments = [0, 1, (ulong)uint.MaxValue + 1, (ulong)uint.MaxValue + 2];

            // enumerate all instructions, updating counters
            int cycleCount = 0;
            for (int i = 0; i < _Instructions.Length; i++)
            {
                ref Instruction instruction = ref _Instructions[i];
                
                int postCycleCount = cycleCount + instruction.CycleCount;

                if (cycleCount >= startCycleCount && postCycleCount < endCycleCount && instruction.IsInstruction)
                {
                    byte opcode = instruction.Opcode;
                    byte memoryAccess = _InstructionSet!.MemoryAccess(opcode);

                    if (memoryAccess != 0) // memory access?
                    {
                        var memoryAddress = instruction.MemoryAddress;
                        var page = memoryAddress.Page;
                        if (!zeroPage || (page == MemoryPage.WholeRam && memoryAddress.Address < 0x100))
                            if (memory[(int)page] != null)
                                memory[(int)page][memoryAddress.PageOffset] += increments[memoryAccess];
                    }
                }

                cycleCount = postCycleCount;
            }

            // populate memory accesses
            MemoryAccesses.Clear();
            MemoryAccesses.Capacity = addressCount;

            for (int i = 0; i < memory.Length; i++)
            {
                if (memory[i] != null)
                {
                    var page = (MemoryPage)i;

                    int baseAddress = page switch
                    {
                        MemoryPage.WholeRam => 0,
                        MemoryPage.HiddenRam => 0,
                        MemoryPage.ShadowRam => 0x3000,
                        MemoryPage.PrivateRam => 0x8000,
                        MemoryPage.FilingSystemRam => 0xC000,
                        _ => 0x8000
                    };

                    for (int j = 0; j < memory[i].Length; j++)
                    {
                        ulong readWriteCount = memory[i][j];
                        if (readWriteCount == 0)
                            continue;

                        int readCount = (int)readWriteCount;
                        int writeCount = (int)(readWriteCount >> 32);

                        MemoryAccesses.Add(new MemoryAccess
                        {
                            Address = new CanonicalAddress((ushort)(baseAddress + j), page),
                            ReadCount = readCount,
                            WriteCount = writeCount,
                        });
                    }
                }
            }
        }

        private void DynamicAddressAnalysis(CanonicalAddress address, int startCycleCount, int endCycleCount)
        {
            Dictionary<CanonicalAddress, RoutineMemoryAccess> routineMetrics = new();
            CalculateStackFrameMemoryAccessMetrics(address, _RootStackFrame, startCycleCount, endCycleCount, routineMetrics);
            RoutineAccesses = routineMetrics.Values.ToList();
        }

        private void CalculateStackFrameMemoryAccessMetrics(
            CanonicalAddress address,
            model.StackFrame stackFrame,
            int startCycleCount, 
            int endCycleCount,
            Dictionary<CanonicalAddress, RoutineMemoryAccess> routineMetrics)
        {
            int childIndex = 0;
            int instructionIndex = stackFrame.FirstInstructionIndex;
            int cycleCount = stackFrame.StartCycleCount;

            model.StackFrame? childStackFrame = (stackFrame.Children.Count > 0) ? stackFrame.Children[0] : null;

            while (cycleCount < stackFrame.EndCycleCount && instructionIndex < stackFrame.EndCycleCount)
            {
                if (childStackFrame != null && cycleCount >= childStackFrame.StartCycleCount)
                {
                    if (startCycleCount <= stackFrame.EndCycleCount && endCycleCount >= stackFrame.StartCycleCount)
                        CalculateStackFrameMemoryAccessMetrics(address, childStackFrame, startCycleCount, endCycleCount, routineMetrics);

                    instructionIndex = childStackFrame.GetLastInstructionStackFrame().LastInstructionIndex + 1;
                    cycleCount = childStackFrame.EndCycleCount;

                    childStackFrame = (++childIndex < stackFrame.Children.Count) ? stackFrame.Children[childIndex] : null;
                    continue;
                }

                ref Instruction instruction = ref _Instructions[instructionIndex];
                int instructionCycleCount = instruction.CycleCount;
                if (instruction.IsInstruction &&
                    cycleCount >= startCycleCount && cycleCount < endCycleCount)
                {
                    byte opcode = instruction.Opcode;
                    byte memoryAccess = _InstructionSet!.MemoryAccess(opcode);
                    if (memoryAccess != 0 && instruction.MemoryAddress.Equals(address))
                    {
                        var routineAddress = stackFrame.Routine.StartAddress;
                        var metrics = routineMetrics.TryGetValue(routineAddress, out var existing)
                            ? existing
                            : routineMetrics[routineAddress] = new RoutineMemoryAccess(stackFrame.Routine, address);

                        var coreInstruction = new CoreInstruction(ref instruction);

                        if ((memoryAccess & 0x1) != 0)
                        {
                            metrics.ReadCount++;
                            metrics.InstructionReadCounts[coreInstruction] = metrics.InstructionReadCounts.GetValueOrDefault(coreInstruction) + 1;
                        }

                        if ((memoryAccess & 0x2) != 0)
                        {
                            metrics.WriteCount++;
                            metrics.InstructionWriteCounts[coreInstruction] = metrics.InstructionWriteCounts.GetValueOrDefault(coreInstruction) + 1;
                        }
                    }
                }

                cycleCount += instructionCycleCount;
                instructionIndex++;
            }
        }

        public class RoutineMemoryAccess
        {
            public RoutineMemoryAccess(Routine routine, CanonicalAddress address)
            {
                Routine = routine;
                Address = address;
                InstructionReadCounts = new();
                InstructionWriteCounts = new();
            }

            public Routine Routine;
            public CanonicalAddress Address;
            public int ReadCount;
            public int WriteCount;
            public Dictionary<CoreInstruction, int> InstructionReadCounts;
            public Dictionary<CoreInstruction, int> InstructionWriteCounts;
        }

        public class MemoryAccess
        {
            public CanonicalAddress Address;
            public int ReadCount;
            public int WriteCount;
        }

        public List<MemoryAccess> MemoryAccesses = [];
        public List<RoutineMemoryAccess> RoutineAccesses = [];

        private Instruction[] _Instructions = [];
        private InstructionSet? _InstructionSet;
        private model.StackFrame _RootStackFrame = new();
        private Dictionary<ushort, string> _Labels = [];
        private byte[][] _SnapshotMemory = [];
    }
}