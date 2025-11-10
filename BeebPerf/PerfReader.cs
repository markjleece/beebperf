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

using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;
using BeebPerf.model;

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
            _InstructionSet = model.InstructionSet;
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

                var instructionSet = _InstructionSet!;
                int size = instructionSet.Size(opcode);
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
                if (instructionSet.ModifiesStackPointer(opcode))
                    instruction.StackPointer = ReadByte(dataStream);

                // destination address
                if (opcode == 0x20/*JSR abs*/ || opcode == 0x4C/*JMP abs*/)
                {
                    // JSR/JMP return address
                    instruction.DestinationAddress = ToCanonicalAddress(model, operand);
                }
                else if (opcode == 0x60/*RTS*/ || opcode == 0x40/*RTI*/ ||
                         opcode == 0x6C/*JMP ind*/ || (opcode == 0x7C/*JMP (abs,X)*/ && model.InstructionSet!.CPU == CPUType._65C02))
                {
                    // RTS/RTI/JMP destination address
                    ushort destinationAddress = ReadShort(dataStream);
                    instruction.DestinationAddress = ToCanonicalAddress(model, destinationAddress);
                }
                else if (instructionSet.IsBranch(opcode))
                {
                    // branch destination
                    int destinationAddress = opcodeAddress + 2;
                    if (cycleCount > 2)
                        destinationAddress = unchecked(destinationAddress + (sbyte)instruction.Operand);
                    instruction.DestinationAddress = ToCanonicalAddress(model, (ushort)destinationAddress);
                }
                else
                {
                    // memory access address
                    byte memoryAccess = instructionSet.MemoryAccess(opcode);
                    if (memoryAccess != 0x0)
                    {
                        ushort memoryAddress;
                        if (instructionSet.AddressingMode(opcode) >= InstructionSet.AddressMode.Complex)
                            memoryAddress = ReadShort(dataStream);
                        else
                            memoryAddress = operand;

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
            MemoryPage page;

            switch (model.BBCModel)
            {
                case BBCModelType.B:
                    if (address >= 0x8000 && address < 0xC000)
                        page = (MemoryPage)(_RomPagingRegister & 0x0F);
                    else
                        page = MemoryPage.WholeRam;
                    break;

                case BBCModelType.BPlus:
                    if (address >= 0x3000 && address < 0x8000 && _ShadowRamSelected)
                        page = MemoryPage.ShadowRam;
                    else if (address >= 0x8000 && address < 0xB000 && _PrivateRamSelected)
                        page = MemoryPage.PrivateRam;
                    else if (address >= 0x8000 && address < 0xC000)
                        page = (MemoryPage)(_RomPagingRegister & 0x0F);
                    else
                        page = MemoryPage.WholeRam;
                    break;
                    
                case BBCModelType.IntegraB:
                    if (address < 0x8000)
                        if (address >= 0x3000 && _ShadowRamEnabled && !_ShadowRamSelected)
                            page = MemoryPage.ShadowRam;
                        else
                            page = MemoryPage.WholeRam;
                    else if (_PrivateRamSelected && (
                        (_PrivateRam8kArea && address < 0x8400) ||
                        (_PrivateRam4kArea && address < 0x9000) ||
                        (_PrivateRam1kArea && address >= 0x9000 && address < 0xB000)))
                        page = MemoryPage.PrivateRam;
                    else if (address < 0xC000)
                        page = (MemoryPage)(_RomPagingRegister & 0x0F);
                    else
                        page = MemoryPage.WholeRam;
                    break;

                case BBCModelType.Master128:
                case BBCModelType.MasterET:
                    if (address >= 0x3000 && address < 0x8000 && _ShadowRamSelected)
                        page = MemoryPage.ShadowRam;
                    else if (address >= 0x8000 && address < 0x9000 && _PrivateRamSelected)
                        page = MemoryPage.PrivateRam;
                    else if (address >= 0x8000 && address < 0xC000)
                        page = (MemoryPage)(_RomPagingRegister & 0x0F);
                    else if (address >= 0xC000 && address < 0xE000)
                        page = MemoryPage.FilingSystemRam;
                    else
                        page = MemoryPage.WholeRam;
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

        private InstructionSet? _InstructionSet;

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