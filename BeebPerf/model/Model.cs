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

namespace BeebPerf.model
{
    //
    // Represents the data loaded from a .perf file. The BBC
    // model, associated instruction-set, snapshot data,
    // instructions, and optional labels.
    // 
    public class Model
    {
        public Model()
        {
        }

        public Model(BBCModelType bbcModel, int executionCount)
        {
            BBCModel = bbcModel;
            InstructionSet = new InstructionSet(bbcModel switch
            {
                BBCModelType.Master128 => CPUType._65C02,
                BBCModelType.MasterET => CPUType._65C02,
                BBCModelType.B => CPUType._6502,
                BBCModelType.IntegraB => CPUType._6502,
                BBCModelType.BPlus => CPUType._6502,
                _ => throw new ArgumentOutOfRangeException()
            });
            Instructions = new Instruction[executionCount];
        }

        public Model Clone()
        {
            return new Model()
            {
                BBCModel = BBCModel,
                InstructionSet = InstructionSet,
                Snapshot = Snapshot.Clone(),
                Instructions = (Instruction[])Instructions.Clone(),
                FrameStartParams = FrameStartParams.ToList(),
                Labels = Labels.ToList()
            };
        }

        public void Set(Model other)
        {
            BBCModel = other.BBCModel;
            InstructionSet = other.InstructionSet;
            Snapshot = other.Snapshot;
            Instructions = other.Instructions;
            FrameStartParams = other.FrameStartParams;
            Labels = other.Labels;
        }

        public class SnapshotType
        {
            public SnapshotType Clone()
            {
                return new SnapshotType()
                {
                    Memory = (byte[][])Memory.Clone(),
                    MemoryReadOnly = (bool[])MemoryReadOnly.Clone(),
                    ProgramCounter = ProgramCounter,
                    Accumulator = Accumulator,
                    XRegister = XRegister,
                    YRegister = YRegister,
                    StackPointer = StackPointer,
                    StackFrames = (MiniStackFrame[])StackFrames.Clone(),
                    RomPagingRegister = RomPagingRegister,
                    AccessControlRegister = AccessControlRegister,
                    ScreenWrapAddress = ScreenWrapAddress,
                    HiddenRamAddress = HiddenRamAddress
                };
            }

            public byte[][] Memory = [];
            public bool[] MemoryReadOnly = [];
            public ushort ProgramCounter;
            public byte Accumulator;
            public byte XRegister;
            public byte YRegister;
            public byte StackPointer;
            public MiniStackFrame[] StackFrames = [];
            public byte RomPagingRegister;
            public byte AccessControlRegister;
            public ushort ScreenWrapAddress;
            public byte HiddenRamAddress;
        }

        public BBCModelType BBCModel;
        public InstructionSet? InstructionSet;
        public SnapshotType Snapshot = new();
        public Instruction[] Instructions = [];
        public List<FrameStartEventParams> FrameStartParams = [];
        public List<(string Name, ushort Address)> Labels = [];
    }
}