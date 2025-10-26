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
            BBCModelType modelType,
            Instruction[] instructions,
            InstructionSet instructionSet,
            Dictionary<ushort, string> labels,
            byte[][] snapshotMemory)
        {
            _ModelType = modelType;
            _Instructions = instructions;
            _InstructionSet = instructionSet;
            _Labels = labels;
            _SnapshotMemory = snapshotMemory;
        }

        public async Task<bool> DynamicAnalysis(int startCycleCount, int endCycleCount)
        {
            return await Task.Run(() =>
            {
                StartCycleCount = startCycleCount;
                EndCycleCount = endCycleCount;
                DynamicAnalysis();
                return true;
            });
        }

        private void DynamicAnalysis()
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

                if (cycleCount >= StartCycleCount && postCycleCount < EndCycleCount && instruction.IsInstruction)
                {
                    byte opcode = instruction.Opcode;
                    byte memoryAccess = _InstructionSet!.MemoryAccess(opcode);

                    if (memoryAccess != 0) // memory access?
                    {
                        var memoryAddress = instruction.MemoryAddress;
                        int page = (int)memoryAddress.Page;
                        int offset = memoryAddress.PageOffset;
                        memory[page][offset] += increments[memoryAccess];
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
                            ReadWriteCount = readCount + writeCount
                        });
                    }
                }
            }
        }

        public struct MemoryAccess
        {
            public CanonicalAddress Address;
            public int ReadCount;
            public int WriteCount;
            public int ReadWriteCount;
        }

        public int StartCycleCount;
        public int EndCycleCount;
        public List<MemoryAccess> MemoryAccesses = [];

        private BBCModelType _ModelType;
        private Instruction[] _Instructions = [];
        private InstructionSet? _InstructionSet;
        private Dictionary<ushort, string> _Labels = [];
        private byte[][] _SnapshotMemory = [];
    }
}