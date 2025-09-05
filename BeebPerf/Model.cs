namespace BeebPerf
{
    public class Model
    {
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

        public struct SnapshotType
        {
            public byte[][] Memory;
            public bool[] MemoryReadOnly;
            public byte StackPointer;
            public byte RomPagingRegister;
            public byte AccessControlRegister;
            public byte HiddenRamAddress;
            public byte VideoULARegister;
            public byte[] VideoULAPalette;
            public byte[] VideoCtrlRegisters;
        }

        public BBCModelType BBCModel;
        public CPUType CPU;
        public SnapshotType Snapshot = new();
        public Instruction[] Instructions;
        public Dictionary<ushort, string> Labels;
    }
}