using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using static BeebPerf.Model;

namespace BeebPerf
{
    public class PerfReader
    {
        [DllImport("zlib.dll")]
        static private extern bool Inflate([In] byte[] compressedData, int compressedSize, [Out] byte[] uncompressedData, int uncompressedSize);

        public Model? ReadFile(string fileName)
        {
            Model? model = null;

            _LastOpcodeAddress = 0;

            var fs = File.OpenRead(fileName);

            while (fs.Position < fs.Length)
            {
                var dataStream = ReadChunk(fs, out string tag);
                switch (tag)
                {
                    case "PFhd":
                        if (model is not null)
                            throw new Exception("invalid .perf file format");

                        model = ReadHeaderData(dataStream);
                        break;

                    case "PFlb":
                        if (model is null)
                            throw new Exception("invalid .perf file format");

                        ReadLabelData(dataStream, model);
                        break;

                    case "PFss":
                        if (model is null)
                            throw new Exception("invalid .perf file format");

                        ReadSnapshotData(dataStream, model);
                        break;

                    case "PFex":
                        if (model is null)
                            throw new Exception("invalid .perf file format");

                        ReadExecutionData(dataStream, model);
                        break;

                    default:
                        Debug.Assert(false);
                        break;
                }
            }

            return model;
        }

        private Model ReadHeaderData(Stream dataStream)
        {
            byte majorVersion = ReadByte(dataStream);
            byte minorVersion = ReadByte(dataStream);

            if (majorVersion != 1 && minorVersion != 0)
                throw new Exception($"Unsupported .perf file version: {majorVersion}.{minorVersion}");

            BBCModelType bbcModel = (BBCModelType)ReadByte(dataStream);
            int totalExecutionCount = ReadInt(dataStream);

            Model model = new Model(bbcModel, totalExecutionCount);

            PopulateOpcodeTables(model.CPU);

            return model;
        }

        private void ReadLabelData(Stream dataStream, Model model)
        {
            while (dataStream.Position < dataStream.Length)
            {
                ushort address = ReadShort(dataStream);
                List<byte> label = new();
                while (true)
                {
                    byte ch = ReadByte(dataStream);
                    if (ch == 0) break;
                    label.Add(ch);
                }

                model.Labels[address] = Encoding.ASCII.GetString(label.ToArray<byte>(), index: 0, count: label.Count);
            }
        }

        private void ReadSnapshotData(Stream dataStream, Model model)
        {
            bool hasShadowRam = (model.BBCModel == BBCModelType.BPlus || model.BBCModel == BBCModelType.IntegraB || model.BBCModel == BBCModelType.Master128 || model.BBCModel == BBCModelType.MasterET);
            bool hasPrivateRam = (model.BBCModel == BBCModelType.BPlus || model.BBCModel == BBCModelType.IntegraB);
            bool hasPrivateMosRam = (model.BBCModel == BBCModelType.Master128 || model.BBCModel == BBCModelType.MasterET);
            bool hasFilingSystemRam = (model.BBCModel == BBCModelType.Master128 || model.BBCModel == BBCModelType.MasterET);
            bool hasHiddenRam = (model.BBCModel == BBCModelType.IntegraB);

            model.Snapshot.StackPointer = ReadByte(dataStream);

            byte romPagingRegister = ReadByte(dataStream);
            model.Snapshot.RomPagingRegister = romPagingRegister;
            RomPagingRegisterChange(model, romPagingRegister);

            byte accessControlRegister = ReadByte(dataStream);
            model.Snapshot.AccessControlRegister = accessControlRegister;
            AccessControlRegisterChange(model, accessControlRegister);

            if (hasHiddenRam)
            {
                byte hiddenRamAddress = ReadByte(dataStream);
                model.Snapshot.HiddenRamAddress = hiddenRamAddress;
                _HiddenRamAddress = hiddenRamAddress;
            }

            model.Snapshot.VideoULARegister = ReadByte(dataStream);
            byte[] palette = new byte[16];
            dataStream.ReadExactly(palette);
            model.Snapshot.VideoULAPalette = palette;

            byte[] controlRegisters = new byte[18];
            dataStream.ReadExactly(controlRegisters);
            model.Snapshot.VideoCtrlRegisters = controlRegisters;

            model.Snapshot.Memory = new byte[(int)MemoryPage.Count][];

            byte[] wholeRam = new byte[65536];
            dataStream.ReadExactly(wholeRam);
            model.Snapshot.Memory[(byte)MemoryPage.WholeRam] = wholeRam;

            model.Snapshot.MemoryReadOnly = new bool[16];

            byte bankCount = ReadByte(dataStream);
            for (int i = 0; i < bankCount; i++)
            {
                byte bankId = ReadByte(dataStream);
                bool readOnly = (ReadByte(dataStream) != 0);
                byte[] bankMemory = new byte[16384];
                dataStream.ReadExactly(bankMemory);

                model.Snapshot.Memory[bankId] = bankMemory;
                model.Snapshot.MemoryReadOnly[bankId] = readOnly;
            }

            if (hasShadowRam)
            {
                byte[] shadowRam = new byte[20480];
                dataStream.ReadExactly(shadowRam);
                model.Snapshot.Memory[(byte)MemoryPage.ShadowRam] = shadowRam;
            }

            if (hasPrivateRam)
            {
                byte[] privateRam = new byte[12288];
                dataStream.ReadExactly(privateRam);
                model.Snapshot.Memory[(byte)MemoryPage.PrivateRam] = privateRam;
            }

            if (hasPrivateMosRam)
            {
                byte[] privateRam = new byte[4096];
                dataStream.ReadExactly(privateRam);
                model.Snapshot.Memory[(byte)MemoryPage.PrivateRam] = privateRam;
            }

            if (hasFilingSystemRam)
            {
                byte[] filingSystemRam = new byte[8192];
                dataStream.ReadExactly(filingSystemRam);
                model.Snapshot.Memory[(byte)MemoryPage.FilingSystemRam] = filingSystemRam;
            }

            if (hasHiddenRam)
            {
                byte[] hiddenRam = new byte[256];
                dataStream.ReadExactly(hiddenRam);
                model.Snapshot.Memory[(byte)MemoryPage.HiddenRam] = hiddenRam;
            }
        }

        private void ReadExecutionData(Stream dataStream, Model model)
        {
            while (dataStream.Position < dataStream.Length)
            {
                // read opcode address
                ushort opcodeAddress = 0;
                Instruction instruction = new Instruction();

                byte addressHi = ReadByte(dataStream);
                if (addressHi > 0x80)
                {
                    // single-byte encoding (most common)
                    int diff = addressHi - 0xC0;
                    Debug.Assert(Math.Abs(diff) <= 63);
                    opcodeAddress = (ushort)(_LastOpcodeAddress + diff);
                }
                else if (addressHi < 0x80)
                {
                    // two-byte encoding
                    opcodeAddress = (ushort)((addressHi << 8) | ReadByte(dataStream));
                }
                else // if (addressHi == 0x80)
                {
                    addressHi = ReadByte(dataStream);
                    if (addressHi >= 0x80)
                    {
                        // three-byte encoding
                        opcodeAddress = (ushort)((addressHi << 8) | ReadByte(dataStream));
                    }
                    else
                    {
                        // interrupt
                        Debug.Assert(addressHi == 0 || addressHi == 1);

                        // mark as interrupt
                        instruction.IsInterrupt = true;

                        // non-maskable interrupt
                        instruction.NonMaskableInterrupt = (addressHi == 1);

                        // interrupt service routine address
                        ushort isrAddress = ReadShort(dataStream);
                        instruction.ISRAddress = ToCanonicalAddress(model, isrAddress);

                        // interrupted opcode address
                        ushort interruptedAddress = ReadShort(dataStream);
                        instruction.InterruptedAddress = ToCanonicalAddress(model, interruptedAddress);

                        // cycle count
                        int cycles = ReadByte(dataStream);
                        Debug.Assert(cycles == 7);
                        instruction.CycleCount = cycles;

                        // stack pointer
                        instruction.StackPointer = ReadByte(dataStream);

                        model.Instructions[_InstructionCount++] = instruction;

                        continue;
                    }
                }

                _LastOpcodeAddress = opcodeAddress;

                // mark as an instruction
                instruction.IsInstruction = true;

                // opcode address
                instruction.OpcodeAddress = ToCanonicalAddress(model, opcodeAddress);

                // opcode
                byte opcode = ReadByte(dataStream);
                instruction.Opcode = opcode;

                // operand
                ushort operand = 0;

                int size = _OpcodeSize[opcode];
                if (size == 2)
                    operand = ReadByte(dataStream);
                else if (size == 3)
                    operand = ReadShort(dataStream);

                instruction.Operand = operand;

                // cycle count
                int cycleCount = ReadByte(dataStream);
                Debug.Assert(cycleCount <= 7);
                instruction.CycleCount = cycleCount;

                // stack pointer
                if (_OpcodeStack[opcode] != 0)
                    instruction.StackPointer = ReadByte(dataStream);

                // destination address
                if (opcode == 0x20/*JSR abs*/ || opcode == 0x4C/*JMP abs*/)
                {
                    // jsr/jmp return address
                    instruction.DestinationAddress = ToCanonicalAddress(model, operand);
                }
                else if (opcode == 0x60/*RTS*/ || opcode == 0x40/*RTI*/ ||
                         opcode == 0x6C/*JMP ind*/ || (opcode == 0x7C/*JMP (abs,X)*/ && model.CPU == CPUType._65C02))
                {
                    // rts/rti/jmp destination address
                    ushort destinationAddress = ReadShort(dataStream);
                    instruction.DestinationAddress = ToCanonicalAddress(model, destinationAddress);
                }
                else
                {
                    // memory access address
                    byte memoryAccess = _OpcodeMemoryAccess[opcode];
                    if (memoryAccess != 0x0)
                    {
                        ushort memoryAddress;
                        if (_OpcodeAddressMode[opcode] != 0/*complex addressing*/)
                            memoryAddress = ReadShort(dataStream);
                        else
                            memoryAddress = opcode;

                        instruction.MemoryAddress = ToCanonicalAddress(model, memoryAddress);

                        if ((memoryAccess & 0x1) != 0)
                        {
                            byte memoryReadValue = ReadByte(dataStream);
                            instruction.MemoryReadValue = memoryReadValue;
                        }

                        if ((memoryAccess & 0x2) != 0)
                        {
                            byte memoryWriteValue = ReadByte(dataStream);
                            instruction.MemoryWriteValue = memoryWriteValue;

                            if (memoryAddress >= 0xFE30 && memoryAddress < 0xFE34)
                            {
                                if ((model.BBCModel != BBCModelType.IntegraB) ||
                                    (model.BBCModel == BBCModelType.IntegraB && memoryAddress == 0xFE30))
                                    RomPagingRegisterChange(model, memoryWriteValue);
                            }

                            if (memoryAddress >= 0xFE34 && memoryAddress < 0xFE38 && (model.BBCModel == BBCModelType.Master128 || model.BBCModel == BBCModelType.MasterET))
                                AccessControlRegisterChange(model, memoryWriteValue);

                            if (memoryAddress == 0xFE38 && model.BBCModel == BBCModelType.IntegraB)
                                _HiddenRamAddress = memoryWriteValue;
                        }
                    }
                }

                model.Instructions[_InstructionCount++] = instruction;
            }
        }

        private void RomPagingRegisterChange(Model model, byte value)
        {
            _RomPagingRegister = value;
            _RomPageSelected = (byte)(value & 0x0F);

            switch (model.BBCModel)
            {
                case BBCModelType.B:
                default:
                    break;

                case BBCModelType.BPlus:
                    _PrivateRamSelected = (value & 0x80) != 0;
                    break;

                case BBCModelType.IntegraB:
                    _ShadowRamSelected = (value & 0x80) != 0;
                    _PrivateRamSelected = (value & 0x40) != 0;
                    break;

                case BBCModelType.Master128:
                case BBCModelType.MasterET:
                    _PrivateRamSelected = (value & 0x80) != 0;
                    break;
            }
        }

        private void AccessControlRegisterChange(Model model, byte value)
        {
            _AccessControlRegister = value;

            switch (model.BBCModel)
            {
                case BBCModelType.B:
                default:
                    break;

                case BBCModelType.BPlus:
                    _ShadowRamSelected = (value & 0x80) != 0;
                    break;

                case BBCModelType.IntegraB:
                    _ShadowRamEnabled = (value & 0x80) != 0;
                    _PrivateRam8kArea = (value & 0x40) != 0;
                    _PrivateRam4kArea = (value & 0x20) != 0;
                    _PrivateRam1kArea = (value & 0x10) != 0;
                    break;

                case BBCModelType.Master128:
                case BBCModelType.MasterET:
                    _ShadowRamSelected = (value & 1) != 0;
                    _FilingSystemRamSelected = (value & 8) != 0;
                    break;
            }
        }

        private CanonicalAddress ToCanonicalAddress(Model model, ushort address)
        {
            byte page;

            switch (model.BBCModel)
            {
                case BBCModelType.B:
                    if (address >= 0x8000 && address < 0xC000)
                    {
                        page = (byte)(_RomPagingRegister & 0x0F);
                        address -= 0x8000;
                    }
                    else
                    {
                        page = (byte)MemoryPage.WholeRam;
                    }
                    break;

                case BBCModelType.BPlus:
                    if (address >= 0x3000 && address < 0x8000 && _ShadowRamSelected)
                    {
                        page = (byte)MemoryPage.ShadowRam;
                        address -= 0x3000;
                    }
                    else if (address >= 0x8000 && address < 0xB000 && _PrivateRamSelected)
                    {
                        page = (byte)MemoryPage.PrivateRam;
                        address -= 0x8000;
                    }
                    else if (address >= 0x8000 && address < 0xC000)
                    {
                        page = (byte)(_RomPagingRegister & 0x0F);
                        address -= 0x8000;
                    }
                    else
                    {
                        page = (byte)MemoryPage.WholeRam;
                    }
                    break;
                    
                case BBCModelType.IntegraB:
                    if (address < 0x8000)
                    {
                        if (address >= 0x3000 && _ShadowRamEnabled && !_ShadowRamSelected)
                        {
                            page = (byte)MemoryPage.ShadowRam;
                            address -= 0x3000;
                        }
                        else
                        {
                            page = (byte)MemoryPage.WholeRam;
                        }
                    }
                    else if (_PrivateRamSelected && (
                        (_PrivateRam8kArea && address < 0x8400) ||
                        (_PrivateRam4kArea && address < 0x9000) ||
                        (_PrivateRam1kArea && address >= 0x9000 && address < 0xB000)))
                    {
                        page = (byte)MemoryPage.PrivateRam;
                        address -= 0x8000;
                    }
                    else if (address < 0xC000)
                    {
                        page = (byte)(_RomPagingRegister & 0x0F);
                        address -= 0x8000;
                    }
                    else
                    {
                        page = (byte)MemoryPage.WholeRam;
                    }
                    break;

                case BBCModelType.Master128:
                case BBCModelType.MasterET:
                    if (address >= 0x3000 && address < 0x8000 && _ShadowRamSelected)
                    {
                        page = (byte)MemoryPage.ShadowRam;
                        address -= 0x3000;
                    }
                    else if (address >= 0x8000 && address < 0x9000 && _PrivateRamSelected)
                    {
                        page = (byte)MemoryPage.PrivateRam;
                        address -= 0x8000;
                    }
                    else if (address >= 0x8000 && address < 0xC000)
                    {
                        page = (byte)(_RomPagingRegister & 0x0F);
                        address -= 0x8000;
                    }
                    else if (address >= 0xC000 && address < 0xE000)
                    {
                        page = (byte)MemoryPage.FilingSystemRam;
                        address -= 0xC000;
                    }
                    else
                    {
                        page = (byte)MemoryPage.WholeRam;
                    }
                    break;

                default:
                    Debug.Assert(false);
                    page = 0;
                    break;
            }

            return new CanonicalAddress(address, page);
        }

        private Stream ReadChunk(FileStream fs, out string tag)
        {
            // read header
            var buffer = new byte[13];
            fs.ReadExactly(buffer);
            tag = Encoding.ASCII.GetString(buffer, index:0, count:4);
            int compressed = buffer[4];
            int uncompressedSize = BitConverter.ToInt32(buffer, 5);
            int compressedSize = BitConverter.ToInt32(buffer, 9);

            // read data
            byte[] uncompressedData;

            if (compressed == 0)
            {
                Debug.Assert(compressedSize == uncompressedSize);

                uncompressedData = new byte[uncompressedSize];
                fs.ReadExactly(uncompressedData);
            }
            else
            {
                Debug.Assert(compressedSize <= uncompressedSize);

                byte[] compressedData = new byte[compressedSize];
                fs.ReadExactly(compressedData);

                uncompressedData = new byte[uncompressedSize];
                if (!Inflate(compressedData, compressedSize, uncompressedData, uncompressedSize))
                    throw new Exception("Inflate error");
            }

            // wrap uncompressed data
            return new MemoryStream(uncompressedData);
        }

        private void PopulateOpcodeTables(CPUType cpu)
        {
            byte[] sizeTable6502 = [
             // 0 1 2 3 4 5 6 7 8 9 a b c d e f
                1,2,1,2,2,2,2,2,1,2,1,2,3,3,3,3, // 0
                2,2,1,2,2,2,2,2,1,3,1,3,3,3,3,3, // 1
                3,2,1,2,2,2,2,2,1,2,1,2,3,3,3,3, // 2
                2,2,1,2,2,2,2,2,1,3,1,3,3,3,3,3, // 3
                1,2,1,2,2,2,2,2,1,2,1,2,3,3,3,3, // 4
                2,2,1,2,2,2,2,2,1,3,1,3,3,3,3,3, // 5
                1,2,1,2,2,2,2,2,1,2,1,2,3,3,3,3, // 6
                2,2,1,2,2,2,2,2,1,3,1,3,3,3,3,3, // 7
                2,2,2,2,2,2,2,2,1,2,1,2,3,3,3,3, // 8
                2,2,1,2,2,2,2,2,1,3,1,3,3,3,3,3, // 9
                2,2,2,2,2,2,2,2,1,2,1,2,3,3,3,3, // a
                2,2,1,2,2,2,2,2,1,3,1,3,3,3,3,3, // b
                2,2,2,2,2,2,2,2,1,2,1,2,3,3,3,3, // c
                2,2,1,2,2,2,2,2,1,3,1,3,3,3,3,3, // d
                2,2,2,2,2,2,2,2,1,2,1,2,3,3,3,3, // e
                2,2,1,2,2,2,2,2,1,3,1,3,3,3,3,3  // f
            ];

            byte[] sizeTable65C02 = [
             // 0 1 2 3 4 5 6 7 8 9 a b c d e f
                1,2,1,1,2,2,2,1,1,2,1,1,3,3,3,1, // 0
                2,2,2,1,2,2,2,1,1,3,1,1,3,3,3,1, // 1
                3,2,1,1,2,2,2,1,1,2,1,1,3,3,3,1, // 2
                2,2,2,1,2,2,2,1,1,3,1,1,3,3,3,1, // 3
                1,2,1,1,1,2,2,1,1,2,1,1,3,3,3,1, // 4
                2,2,2,1,1,2,2,1,1,3,1,1,1,3,3,1, // 5
                1,2,1,1,2,2,2,1,1,2,1,1,3,3,3,1, // 6
                2,2,2,1,2,2,2,1,1,3,1,1,3,3,3,1, // 7
                2,2,1,1,2,2,2,1,1,2,1,1,3,3,3,1, // 8
                2,2,2,1,2,2,2,1,1,3,1,1,3,3,3,1, // 9
                2,2,2,1,2,2,2,1,1,2,1,1,3,3,3,1, // a
                2,2,2,1,2,2,2,1,1,3,1,1,3,3,3,1, // b
                2,2,1,1,2,2,2,1,1,2,1,1,3,3,3,1, // c
                2,2,2,1,1,2,2,1,1,3,1,1,1,3,3,1, // d
                2,2,1,1,2,2,2,1,1,2,1,1,3,3,3,1, // e
                2,2,2,1,1,2,2,1,1,3,1,1,1,3,3,1  // f
            ];

            byte[] memoryAccessTable6502 = [
             // 0 1 2 3 4 5 6 7 8 9 a b c d e f
                0,1,0,3,0,1,3,3,0,0,0,0,0,1,3,3, // 0 none
                0,1,0,3,0,1,3,3,0,1,0,3,0,1,3,3, // 1 memory read
                0,1,0,3,1,1,3,3,0,0,0,0,1,1,3,3, // 2 memory write
                0,1,0,3,0,1,3,3,0,1,0,3,0,1,3,3, // 3 memory read & write
                0,1,0,3,0,1,3,3,0,0,0,0,0,1,3,3, // 4 
                0,1,0,3,0,1,3,3,0,1,0,3,0,1,3,3, // 5 
                0,1,0,3,0,1,3,3,0,0,0,0,0,1,3,3, // 6
                0,1,0,3,0,1,3,3,0,1,0,3,0,1,3,3, // 7
                0,2,0,2,2,2,2,2,0,0,0,0,2,2,2,2, // 8
                0,2,0,2,2,2,2,2,0,2,0,2,2,2,2,2, // 9
                0,1,0,1,1,1,1,1,0,0,0,0,1,1,1,1, // a
                0,1,0,1,1,1,1,1,0,1,0,1,1,1,1,1, // b
                0,1,0,3,1,1,3,3,0,0,0,0,1,1,3,3, // c
                0,1,0,3,0,1,3,3,0,1,0,3,0,1,3,3, // d
                0,1,0,3,1,1,3,3,0,0,0,0,1,1,3,3, // e
                0,1,0,3,0,1,3,3,0,1,0,3,0,1,3,3  // f
            ];

            byte[] memoryAccessTable65C02 = [
             // 0 1 2 3 4 5 6 7 8 9 a b c d e f
                0,1,0,0,3,1,3,0,0,0,0,0,3,1,3,0, // 0 none
                0,1,1,0,3,1,3,0,0,1,0,0,3,1,3,0, // 1 memory read
                0,1,0,0,1,1,3,0,0,0,0,0,1,1,3,0, // 2 memory write
                0,1,1,0,1,1,3,0,0,1,0,0,1,1,3,0, // 3 memory read & write
                0,1,0,0,0,1,3,0,0,0,0,0,0,1,3,0, // 4 
                0,1,1,0,0,1,3,0,0,1,0,0,0,1,3,0, // 5 
                0,1,0,0,2,1,3,0,0,0,0,0,0,1,3,0, // 6
                0,1,1,0,2,1,3,0,0,1,0,0,0,1,3,0, // 7
                0,2,0,0,2,2,2,0,0,0,0,0,2,2,2,0, // 8
                0,2,2,0,2,2,2,0,0,2,0,0,2,2,2,0, // 9
                0,1,0,0,1,1,1,0,0,0,0,0,1,1,1,0, // a
                0,1,1,0,1,1,1,0,0,1,0,0,1,1,1,0, // b
                0,1,0,0,1,1,3,0,0,0,0,0,1,1,3,0, // c
                0,1,1,0,0,1,3,0,0,1,0,0,0,1,3,0, // d
                0,1,0,0,1,1,3,0,0,0,0,0,1,1,3,0, // e
                0,1,1,0,0,1,3,0,0,1,0,0,0,1,3,0  // f
            ];

            byte[] addressModeTable6502 = [
             // 0 1 2 3 4 5 6 7 8 9 a b c d e f
                0,1,0,1,0,0,0,0,0,0,0,0,0,0,0,0, // 0 simple addressing (implied, immediate, relative, absolute, or zero-page)
                0,1,0,1,0,1,1,1,0,1,0,1,0,1,1,1, // 1 complex addressing (indexed, indirect addressing)
                0,1,0,1,0,0,0,0,0,0,0,0,0,0,0,0, // 2
                0,1,0,1,0,1,1,1,0,1,0,1,0,1,1,1, // 3 
                0,1,0,1,0,0,0,0,0,0,0,0,0,0,0,0, // 4 
                0,1,0,1,0,1,1,1,0,1,0,1,0,1,1,1, // 5 
                0,1,0,1,0,0,0,0,0,0,0,0,1,0,0,0, // 6
                0,1,0,1,0,1,1,1,0,1,0,1,0,1,1,1, // 7
                0,1,0,1,0,0,0,0,0,0,0,0,0,0,0,0, // 8
                0,1,0,1,1,1,1,1,0,1,0,1,1,1,1,1, // 9
                0,1,0,1,0,0,0,0,0,0,0,0,0,0,0,0, // a
                0,1,0,1,1,1,1,1,0,1,0,1,1,1,1,1, // b
                0,1,0,1,0,0,0,0,0,0,0,0,0,0,0,0, // c
                0,1,0,1,0,1,1,1,0,1,0,1,0,1,1,1, // d
                0,1,0,1,0,0,0,0,0,0,0,0,0,0,0,0, // e
                0,1,0,1,0,1,1,1,0,1,0,1,0,1,1,1  // f
            ];

            byte[] addressModeTable65C02 = [
             // 0 1 2 3 4 5 6 7 8 9 a b c d e f
                0,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 0 simple addressing (implied, immediate, relative, absolute, or zero-page)
                0,1,1,0,0,1,1,0,0,1,0,0,0,1,1,0, // 1 complex addressing (indexed, indirect addressing)
                0,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 2
                0,1,1,0,1,1,1,0,0,1,0,0,1,1,1,0, // 3 
                0,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 4 
                0,1,1,0,0,0,1,0,0,1,0,0,0,1,1,0, // 5 
                0,1,0,0,0,0,0,0,0,0,0,0,1,0,0,0, // 6
                0,1,1,0,1,1,1,0,0,1,0,0,1,1,1,0, // 7
                0,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 8
                0,1,1,0,1,1,1,0,0,1,0,0,0,1,1,0, // 9
                0,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // a
                0,1,1,0,1,1,1,0,0,1,0,0,1,1,1,0, // b
                0,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // c
                0,1,1,0,0,1,1,0,0,1,0,0,0,1,1,0, // d
                0,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // e
                0,1,1,0,0,1,1,0,0,1,0,0,0,1,1,0  // f
            ];

            byte[] modifiesStackPointerTable6502 = [
             // 0 1 2 3 4 5 6 7 8 9 a b c d e f
                0,0,0,0,0,0,0,0,1,0,0,0,0,0,0,0, // 0 false
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 1 true
                1,0,0,0,0,0,0,0,1,0,0,0,0,0,0,0, // 2
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 3
                1,0,0,0,0,0,0,0,1,0,0,0,0,0,0,0, // 4
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 5
                1,0,0,0,0,0,0,0,1,0,0,0,0,0,0,0, // 6
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 7
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 8
                0,0,0,0,0,0,0,0,0,0,1,1,0,0,0,0, // 9
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // a
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // b
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // c
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // d
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // e
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0  // f
            ];

            byte[] modifiesStackPointerTable65C02 = [
             // 0 1 2 3 4 5 6 7 8 9 a b c d e f
                0,0,0,0,0,0,0,0,1,0,0,0,0,0,0,0, // 0 false
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 1 true
                1,0,0,0,0,0,0,0,1,0,0,0,0,0,0,0, // 2
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 3
                1,0,0,0,0,0,0,0,1,0,0,0,0,0,0,0, // 4
                0,0,0,0,0,0,0,0,0,0,1,0,0,0,0,0, // 5
                1,0,0,0,0,0,0,0,1,0,0,0,0,0,0,0, // 6
                0,0,0,0,0,0,0,0,0,0,1,0,0,0,0,0, // 7
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // 8
                0,0,0,0,0,0,0,0,0,0,1,0,0,0,0,0, // 9
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // a
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // b
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // c
                0,0,0,0,0,0,0,0,0,0,1,0,0,0,0,0, // d
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // e
                0,0,0,0,0,0,0,0,0,0,1,0,0,0,0,0  // f
            ];

            if (cpu == CPUType._6502)
            {
                _OpcodeSize = sizeTable6502;
                _OpcodeMemoryAccess = memoryAccessTable6502;
                _OpcodeAddressMode = addressModeTable6502;
                _OpcodeStack = modifiesStackPointerTable6502;
            }
            else
            {
                Debug.Assert(cpu == CPUType._65C02);
                _OpcodeSize = sizeTable65C02;
                _OpcodeMemoryAccess = memoryAccessTable65C02;
                _OpcodeAddressMode = addressModeTable65C02;
                _OpcodeStack = modifiesStackPointerTable65C02;
            }
        }

        private static int ReadInt(Stream ms)
        {
            return ms.ReadByte() | (ms.ReadByte() << 8) | (ms.ReadByte() << 16) | (ms.ReadByte() << 24);
        }

        private static ushort ReadShort(Stream ms)
        {
            return (ushort)(ms.ReadByte() | (ms.ReadByte() << 8));
        }

        private static byte ReadByte(Stream ms)
        {
            return (byte)ms.ReadByte();
        }

        private byte[] _OpcodeSize = new byte[0];
        private byte[] _OpcodeMemoryAccess = new byte[0];
        private byte[] _OpcodeAddressMode = new byte[0];
        private byte[] _OpcodeStack = new byte[0];
        private ushort _LastOpcodeAddress = 0;
        private int _InstructionCount = 0;

        private int _RomPageSelected;
        private bool _ShadowRamSelected;
        private bool _PrivateRamSelected;
        private bool _FilingSystemRamSelected;

        private byte _HiddenRamAddress; // IntegraB
        private bool _ShadowRamEnabled; // IntegraB
        private bool _PrivateRam1kArea; // IntegraB
        private bool _PrivateRam4kArea; // IntegraB
        private bool _PrivateRam8kArea; // IntegraB

        private byte _RomPagingRegister;
        private byte _AccessControlRegister;
    }
}