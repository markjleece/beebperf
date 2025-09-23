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

namespace BeebPerf
{
    public class Model
    {
        public Model() 
        {
        }

        public Model(BBCModelType bbcModel, int executionCount)
        {
            BBCModel = bbcModel;
            
            CPU = bbcModel switch {
                BBCModelType.Master128 => CPUType._65C02,
                BBCModelType.MasterET => CPUType._65C02,
                BBCModelType.B => CPUType._6502,
                BBCModelType.IntegraB => CPUType._6502,
                BBCModelType.BPlus => CPUType._6502,
                _ => CPUType._6502 };

            Labels = new();

            Instructions = new Instruction[executionCount];
        }
        public Model Clone()
        {
            return new Model()
            {
                BBCModel = BBCModel,
                CPU = CPU,
                Snapshot = Snapshot.Clone(),
                Instructions = (Instruction[])Instructions.Clone(),
                Labels = new(Labels)
            };
        }

        public void Set(Model other)
        {
            BBCModel = other.BBCModel;
            CPU = other.CPU;
            Snapshot = other.Snapshot;
            Instructions = other.Instructions;
            Labels = other.Labels;
        }

        public enum CPUType
        {
            _6502 = 0,
            _65C02 = 1,
        }

        public enum BBCModelType
        {
            B = 0,
            IntegraB = 1,
            BPlus = 2,
            Master128 = 3,
            MasterET = 4
        }

        // memory
        public enum MemoryPage
        {
            PagedRom = 0,
            WholeRam = 16,
            ShadowRam = 17,
            PrivateRam = 18,
            FilingSystemRam = 19,
            HiddenRam = 20,
            Count = 21
        }

        public class SnapshotType
        {
            public SnapshotType Clone()
            {
                return new SnapshotType()
                {
                    Memory = (byte[][])Memory.Clone(),
                    MemoryReadOnly = (bool[])MemoryReadOnly.Clone(),
                    StackPointer = StackPointer,
                    RomPagingRegister = RomPagingRegister,
                    AccessControlRegister = AccessControlRegister,
                    HiddenRamAddress = HiddenRamAddress,
                    VideoULARegister = VideoULARegister,
                    VideoULAPalette = (byte[])VideoULAPalette.Clone(),
                    VideoCtrlRegisters = (byte[])VideoCtrlRegisters.Clone(),
                };
            }

            public byte[][] Memory = [];
            public bool[] MemoryReadOnly = [];
            public byte StackPointer;
            public byte RomPagingRegister;
            public byte AccessControlRegister;
            public byte HiddenRamAddress;
            public byte VideoULARegister;
            public byte[] VideoULAPalette = [];
            public byte[] VideoCtrlRegisters = [];
        }

        public BBCModelType BBCModel;
        public CPUType CPU;
        public SnapshotType Snapshot = new();
        public Instruction[] Instructions = [];
        public Dictionary<ushort, string> Labels = [];
    }
}