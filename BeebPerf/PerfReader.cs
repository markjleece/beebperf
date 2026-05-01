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
using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace BeebPerf
{
    //
    // Reads a .perf file populating a Model instance with its contents.
    //
    public class PerfReader
    {
        [DllImport("zlib.dll")]
        static private extern bool Inflate([In] byte[] compressedData, int compressedSize, [Out] byte[] uncompressedData, int uncompressedSize);

        public Model? ReadFile(string fileName)
        {
            _InstructionCount = 0;
            _LastOpcodeAddress = 0;

            bool headerChunkRead = false;
            bool labelsChunkRead = false;
            bool snapshotChunkRead = false;
            bool executionChunkRead = false;

            using var fs = File.OpenRead(fileName);

            Model? model = null;

            while (fs.Position < fs.Length)
            {
                using var dataStream = ReadChunk(fs, out string tag);
                switch (tag)
                {
                    case "PFhd":
                        if (headerChunkRead || labelsChunkRead || snapshotChunkRead || executionChunkRead)
                            throw new InvalidDataException("invalid .perf file format - unexpected header chunk");

                        model = ReadHeaderData(dataStream);
                        headerChunkRead = true;
                        break;

                    case "PFlb":
                        if (!headerChunkRead || labelsChunkRead || snapshotChunkRead || executionChunkRead)
                            throw new InvalidDataException("invalid .perf file format - unexpected labels chunk");

                        ReadLabelData(dataStream, model!);
                        labelsChunkRead = true;
                        break;

                    case "PFss":
                        if (!headerChunkRead || snapshotChunkRead || executionChunkRead)
                            throw new InvalidDataException("invalid .perf file format - unexpected snapshot chunk");

                        ReadSnapshotData(dataStream, model!);
                        snapshotChunkRead = true;
                        break;

                    case "PFex":
                        if (!headerChunkRead || !snapshotChunkRead)
                            throw new InvalidDataException("invalid .perf file format - unexpected execution chunk");

                        ReadExecutionData(dataStream, model!);
                        executionChunkRead = true;
                        break;

                    default:
                        throw new InvalidDataException("invalid .perf file format - unknown chunk");
                }
            }

            return model;
        }

        private Model ReadHeaderData(Stream dataStream)
        {
            byte majorVersion = ReadByte(dataStream);
            byte minorVersion = ReadByte(dataStream);
            if (majorVersion != 1 || minorVersion != 0)
                throw new InvalidDataException($"Unsupported .perf file version: {majorVersion}.{minorVersion}");

            BBCModelType bbcModel = (BBCModelType)ReadByte(dataStream);
            if (bbcModel != BBCModelType.B && bbcModel != BBCModelType.IntegraB && bbcModel != BBCModelType.BPlus && bbcModel != BBCModelType.Master128 && bbcModel != BBCModelType.MasterET)
                throw new InvalidDataException("invalid .perf file format: unknown BBC model");

            int totalExecutionCount = ReadInt(dataStream);
            if (totalExecutionCount <= 0)
                throw new InvalidDataException("invalid .perf file format");

            Model model = new Model(bbcModel, totalExecutionCount);
            _InstructionSet = model.InstructionSet;
            return model;
        }

        private void ReadLabelData(Stream dataStream, Model model)
        {
            List<(string Name, ushort Address)> labels = [];

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

                string name = Encoding.ASCII.GetString(label.ToArray<byte>(), index: 0, count: label.Count);
                labels.Add((name, address));
            }

            model.Labels = labels;
        }

        private void ReadSnapshotData(Stream dataStream, Model model)
        {
            bool hasShadowRam = (model.BBCModel == BBCModelType.BPlus || model.BBCModel == BBCModelType.IntegraB || model.BBCModel == BBCModelType.Master128 || model.BBCModel == BBCModelType.MasterET);
            bool hasPrivateRam = (model.BBCModel == BBCModelType.BPlus || model.BBCModel == BBCModelType.IntegraB);
            bool hasPrivateMosRam = (model.BBCModel == BBCModelType.Master128 || model.BBCModel == BBCModelType.MasterET);
            bool hasFilingSystemRam = (model.BBCModel == BBCModelType.Master128 || model.BBCModel == BBCModelType.MasterET);
            bool hasHiddenRam = (model.BBCModel == BBCModelType.IntegraB);

            // CPU registers
            model.Snapshot.ProgramCounter = ReadShort(dataStream);
            model.Snapshot.Accumulator = _Accumulator = ReadByte(dataStream);
            model.Snapshot.XRegister = _XRegister = ReadByte(dataStream);
            model.Snapshot.YRegister = _YRegister = ReadByte(dataStream);
            ReadByte(dataStream); // skip status register, not used
            model.Snapshot.StackPointer = _StackPointer = ReadByte(dataStream);

            // paging registers
            byte romPagingRegister = ReadByte(dataStream);
            model.Snapshot.RomPagingRegister = romPagingRegister;
            RomPagingRegisterChange(model, romPagingRegister);

            byte accessControlRegister = ReadByte(dataStream);
            model.Snapshot.AccessControlRegister = accessControlRegister;
            AccessControlRegisterChange(model, accessControlRegister);

            // hidden RAM address
            if (hasHiddenRam)
            {
                byte hiddenRamAddress = ReadByte(dataStream);
                model.Snapshot.HiddenRamAddress = hiddenRamAddress;
            }

            int stackFrameCount = ReadByte(dataStream);
            model.Snapshot.StackFrames = new MiniStackFrame[stackFrameCount];
            for (int i = 0; i < stackFrameCount; i++)
            {
                var type = (CallType)ReadByte(dataStream);
                var startAddressPage = (MemoryPage)ReadByte(dataStream);
                ushort startAddress = ReadShort(dataStream);
                var returnAddressPage = (MemoryPage)ReadByte(dataStream);
                ushort returnAddress = ReadShort(dataStream);
                byte returnStackPointer = ReadByte(dataStream);

                if (type != CallType.None && type != CallType.JSR && type != CallType.IRQ && type != CallType.NMI && type != CallType.BRK)
                    throw new InvalidDataException("invalid .perf file format: invalid stack frame type");

                if (startAddressPage >= MemoryPage.Count)
                    throw new InvalidDataException("invalid .perf file format: invalid stack frame start address page");

                if (returnAddressPage >= MemoryPage.Count)
                    throw new InvalidDataException("invalid .perf file format: invalid stack frame return address page");

                var startAddr = new CanonicalAddress(startAddress, startAddressPage);
                var returnAddr = new CanonicalAddress(returnAddress, returnAddressPage);

                if (!startAddr.IsValid())
                    throw new InvalidDataException("invalid .perf file format: invalid stack frame start address");

                if (!returnAddr.IsValid())
                    throw new InvalidDataException("invalid .perf file format: invalid stack frame return address");

                model.Snapshot.StackFrames[i] = new(type, startAddr, returnAddr, returnStackPointer);
            }

            // video ULA
            model.Snapshot.ULAControlRegister = ReadByte(dataStream);

            byte[] palette = new byte[16];
            dataStream.ReadExactly(palette);
            model.Snapshot.ULAPalette = palette;

            // video CRTC
            byte controlRegister = ReadByte(dataStream);
            if (controlRegister >= 18)
                throw new InvalidDataException("invalid .perf file format: invalid CTRL control register");
            model.Snapshot.CRTCRegisterSelect = controlRegister;

            byte[] registers = new byte[18];
            dataStream.ReadExactly(registers);
            model.Snapshot.CRTCRegisters = registers;
            model.Snapshot.CRTCCharacterRow = ReadByte(dataStream);
            model.Snapshot.CRTCCharacterColumn = ReadByte(dataStream);
            model.Snapshot.CRTCCharacterScanline = ReadByte(dataStream);
            model.Snapshot.CRTCDisplayScanline = ReadShort(dataStream);

            byte displayField = ReadByte(dataStream);
            if (displayField != 0 && displayField != 1)
                throw new InvalidDataException("invalid .perf file format: invalid display field");
            model.Snapshot.CRTCDisplayField = displayField;

            // screen wrap address
            byte screenWrapAddress = ReadByte(dataStream);
            model.Snapshot.ScreenWrapAddress = screenWrapAddress switch
            {
                0 => 0x4000,
                1 => 0x6000,
                2 => 0x3000,
                3 => 0x5800,
                _ => throw new InvalidDataException("invalid .perf file format: invalid screen wrap address")
            };

            // memory
            model.Snapshot.Memory = new byte[(int)MemoryPage.Count][];

            byte[] wholeRam = new byte[65536];
            dataStream.ReadExactly(wholeRam);
            model.Snapshot.Memory[(byte)MemoryPage.WholeRam] = wholeRam;

            model.Snapshot.MemoryReadOnly = new bool[16];

            byte pageCount = ReadByte(dataStream);
            if (pageCount > 16)
                throw new InvalidDataException("invalid .perf file format: invalid page count");

            for (int i = 0; i < pageCount; i++)
            {
                byte pageBank = ReadByte(dataStream);
                if (pageBank >= 16)
                    throw new InvalidDataException("invalid .perf file format: invalid page bank");

                int pageReadOnly = ReadByte(dataStream);
                if (pageReadOnly != 0 && pageReadOnly != 1)
                    throw new InvalidDataException("invalid .perf file format: invalid page readonly");

                byte[] pageMemory = new byte[16384];
                dataStream.ReadExactly(pageMemory);

                model.Snapshot.Memory[pageBank] = pageMemory;
                model.Snapshot.MemoryReadOnly[pageBank] = (pageReadOnly == 1);
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

                // read marker
                byte marker = ReadByte(dataStream);
                if (marker > 0x80)
                {
                    // instruction: Single-byte relative address encoding (most common)
                    int diff = marker - 0xC0;
                    Debug.Assert(Math.Abs(diff) <= 63);
                    opcodeAddress = (ushort)(_LastOpcodeAddress + diff);
                }
                else if (marker < 0x80)
                {
                    // instruction: Two-byte address encoding
                    opcodeAddress = (ushort)((marker << 8) | ReadByte(dataStream));
                }
                else // if (marker == 0x80)
                {
                    marker = ReadByte(dataStream);
                    if (marker < 0x80)
                    {
                        // event
                        var eventType = (EventType)marker;
                        if (eventType == EventType.IRQ || eventType == EventType.NMI)
                        {
                            // IRQ/NMI event
                            instruction.Type = eventType switch
                            {
                                EventType.IRQ => InstructionType.IRQ,
                                EventType.NMI => InstructionType.NMI,
                                _ => throw new ArgumentOutOfRangeException()
                            };

                            // cycle count
                            int cycles = ReadByte(dataStream);
                            Debug.Assert(cycles == 7);
                            instruction.CycleCount = cycles;

                            // interrupt service routine address
                            ushort destinationAddress = ReadShort(dataStream);
                            instruction.DestinationAddress = ToCanonicalAddress(model, destinationAddress);

                            // return address
                            ushort returnAddress = ReadShort(dataStream);
                            instruction.ReturnAddress = ToCanonicalAddress(model, returnAddress);

                            // skip pushed status register, not used
                            ReadByte(dataStream); 

                            model.Instructions[_InstructionCount++] = instruction;
                            continue;
                        }

                        throw new InvalidDataException("invalid .perf file format: invalid event");
                    }

                    // Instruction: Three-byte address encoding
                    opcodeAddress = (ushort)((marker << 8) | ReadByte(dataStream));
                }

                _LastOpcodeAddress = opcodeAddress;

                // mark as an instruction
                instruction.Type = InstructionType.Instruction;

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

                // cycle count and register changes
                byte bits = ReadByte(dataStream);

                int cycleCount = (bits & 0x7) + 2;
                instruction.CycleCount = cycleCount;

                if ((bits & (byte)ModifiedRegister.Accumulator) != 0) // new value of accumulator
                    _Accumulator = ReadByte(dataStream);

                if ((bits & (byte)ModifiedRegister.XRegister) != 0) // new value of X register
                    _XRegister = ReadByte(dataStream);

                if ((bits & (byte)ModifiedRegister.YRegister) != 0) // new value of Y register
                    _YRegister = ReadByte(dataStream);

                if ((bits & (byte)ModifiedRegister.StatusRegister) != 0) // new value of status register
                    ReadByte(dataStream); // skip, not used

                if ((bits & (byte)ModifiedRegister.StackPointer) != 0) // new value of stack pointer
                    _StackPointer = ReadByte(dataStream);

                // stack pointer (excluding BRK, JSR, RTI, RTS)
                if (model.InstructionSet!.ModifiesStackPointer(opcode) && (opcode & 0x0F) != 0)
                    instruction.StackPointer = _StackPointer;

                // stack value
                if (opcode == 0x48/*PHA*/ || opcode == 0x68/*PLA*/)
                {
                    instruction.StackValue = _Accumulator;
                }
                else if (opcode == 0xDA/*PHX*/ || opcode == 0xFA/*PLX*/)
                {
                    if (model.InstructionSet!.CPU == CPUType._65C02)
                        instruction.StackValue = _XRegister;
                    else
                        instruction.StackValue = _StackPointer;
                }
                else if (opcode == 0x5A/*PHY*/ || opcode == 0x7A/*PLY*/)
                {
                    if (model.InstructionSet!.CPU == CPUType._65C02)
                        instruction.StackValue = _YRegister;
                    else
                        instruction.StackValue = _StackPointer;
                }

                // destination address
                if (opcode == 0x00/*BRK*/ || opcode == 0x60/*RTS*/ || 
                    opcode == 0x40/*RTI*/ || opcode == 0x6C/*JMP (abs)*/ ||
                    (opcode == 0x7C/*JMP (abs,X)*/ && model.InstructionSet!.CPU == CPUType._65C02))
                {
                    instruction.DestinationAddress = ToCanonicalAddress(model, ReadShort(dataStream));
                }
                else if (opcode == 0x20/*JSR abs*/ || opcode == 0x4C/*JMP abs*/)
                {
                    instruction.DestinationAddress = ToCanonicalAddress(model, operand);
                }
                else if (instructionSet.IsBranch(opcode))
                {
                    int destinationAddress = opcodeAddress + 2;
                    if (cycleCount > 2)
                        destinationAddress = unchecked(destinationAddress + (sbyte)instruction.Operand);
                    instruction.DestinationAddress = ToCanonicalAddress(model, (ushort)destinationAddress);
                }

                // return address
                if (opcode == 0x00/*BRK*/)
                {
                    instruction.ReturnAddress = instruction.OpcodeAddress.Offset(2);
                }
                else if (opcode == 0x20/*JSR*/)
                {
                    instruction.ReturnAddress = instruction.OpcodeAddress.Offset(3);
                }

                // memory access address and read/write values
                var memoryAccess = instructionSet.MemoryAccess(opcode);
                if (memoryAccess != InstructionSet.MemoryAccessType.None)
                {
                    ushort memoryAddress;
                    if (instructionSet.AddressingMode(opcode) >= InstructionSet.AddressingModeType.IndexedOrIndirect)
                        memoryAddress = ReadShort(dataStream);
                    else
                        memoryAddress = operand;

                    instruction.MemoryAddress = ToCanonicalAddress(model, memoryAddress, opcodeAddress);

                    if ((memoryAccess & InstructionSet.MemoryAccessType.Read) != 0)
                    {
                        byte memoryReadValue = 0;
                        switch (instructionSet.LoadOrStore(opcode))
                        {
                            case InstructionSet.LoadOrStoreType.Neither:
                                memoryReadValue = ReadByte(dataStream);
                                break;

                            case InstructionSet.LoadOrStoreType.LDA:
                                memoryReadValue = _Accumulator;
                                break;

                            case InstructionSet.LoadOrStoreType.LDX:
                                memoryReadValue = _XRegister;
                                break;

                            case InstructionSet.LoadOrStoreType.LDY:
                                memoryReadValue = _YRegister;
                                break;

                            default:
                                Debug.Assert(false);
                                break;
                        }
                        instruction.MemoryReadValue = memoryReadValue;
                    }

                    if ((memoryAccess & InstructionSet.MemoryAccessType.Write) != 0)
                    {
                        byte memoryWriteValue = 0;
                        switch (instructionSet.LoadOrStore(opcode))
                        {
                            case InstructionSet.LoadOrStoreType.Neither:
                                memoryWriteValue = ReadByte(dataStream);
                                break;

                            case InstructionSet.LoadOrStoreType.STA:
                                memoryWriteValue = _Accumulator;
                                break;

                            case InstructionSet.LoadOrStoreType.STX:
                                memoryWriteValue = _XRegister;
                                break;

                            case InstructionSet.LoadOrStoreType.STY:
                                memoryWriteValue = _YRegister;
                                break;

                            default:
                                Debug.Assert(false);
                                break;
                        }
                        instruction.MemoryWriteValue = memoryWriteValue;

                        if (memoryAddress >= 0xFE30 && memoryAddress < 0xFE34)
                            RomPagingRegisterChange(model, memoryWriteValue);
                        else if (memoryAddress >= 0xFE34 && memoryAddress < 0xFE38)
                            AccessControlRegisterChange(model, memoryWriteValue);
                    }
                }

                model.Instructions[_InstructionCount++] = instruction;
            }
        }

        private void RomPagingRegisterChange(Model model, byte value)
        {
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
                    _ShadowRamXBit = (value & 4) != 0;
                    _ShadowRamEBit = (value & 2) != 0;
                    _FilingSystemRamSelected = (value & 8) != 0;
                    break;
            }
        }

        private CanonicalAddress ToCanonicalAddress(Model model, ushort address, ushort opcodeAddress = 0)
        {
            MemoryPage page;

            switch (model.BBCModel)
            {
                case BBCModelType.B:
                    if (address >= 0x8000 && address < 0xC000)
                        page = (MemoryPage)_RomPageSelected;
                    else
                        page = MemoryPage.WholeRam;
                    break;

                case BBCModelType.BPlus:
                    if (address < 0x3000)
                        page = MemoryPage.WholeRam;
                    else if (address < 0x8000)
                    {
                        if (_ShadowRamSelected && 
                            ((opcodeAddress >= 0xC000 && opcodeAddress < 0xE000) || 
                             (opcodeAddress >= 0xA000 && opcodeAddress < 0xB000 && _PrivateRamSelected)))
                            page = MemoryPage.ShadowRam;
                        else
                            page = MemoryPage.WholeRam;
                    }
                    else if (address < 0xB000 && _PrivateRamSelected)
                        page = MemoryPage.PrivateRam;
                    else if (address < 0xC000)
                        page = (MemoryPage)_RomPageSelected;
                    else
                        page = MemoryPage.WholeRam;
                    break;
                    
                case BBCModelType.IntegraB:
                    if (address < 0x3000)
                        page = MemoryPage.WholeRam;
                    else if (address < 0x8000)
                        if (_ShadowRamEnabled && !_ShadowRamSelected)
                            page = MemoryPage.ShadowRam;
                        else
                            page = MemoryPage.WholeRam;
                    else if (_PrivateRamSelected && (
                        (_PrivateRam8kArea && address < 0x8400) ||
                        (_PrivateRam4kArea && address < 0x9000) ||
                        (_PrivateRam1kArea && address >= 0x9000 && address < 0xB000)))
                        page = MemoryPage.PrivateRam;
                    else if (address < 0xC000)
                        page = (MemoryPage)_RomPageSelected;
                    else
                        page = MemoryPage.WholeRam;
                    break;

                case BBCModelType.Master128:
                case BBCModelType.MasterET:
                    if (address < 0x3000)
                        page = MemoryPage.WholeRam;
                    else if (address < 0x8000)
                    {
                        if (_ShadowRamXBit || (_ShadowRamEBit && opcodeAddress >= 0xC000 && opcodeAddress < 0xE000))
                            page = MemoryPage.ShadowRam;
                        else
                            page = MemoryPage.WholeRam;
                    }
                    else if (address < 0x9000 && _PrivateRamSelected)
                        page = MemoryPage.PrivateRam;
                    else if (address < 0xC000)
                        page = (MemoryPage)_RomPageSelected;
                    else if (address < 0xE000 && _FilingSystemRamSelected)
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

        private static bool IsLetter(byte b)
        {
            return (b >= (byte)'A' && b <= (byte)'Z') || (b >= (byte)'a' && b <= (byte)'z');
        }

        private Stream ReadChunk(FileStream fs, out string tag)
        {
            // read header
            var buffer = new byte[13];
            fs.ReadExactly(buffer);

            for (int i = 0; i < 4; i++)
                if (!IsLetter(buffer[i]))
                    throw new InvalidDataException("invalid .perf file format");

            tag = Encoding.ASCII.GetString(buffer, index:0, count:4);

            byte compressed = buffer[4];
            int uncompressedSize = BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(5));
            int compressedSize = BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(9));

            if (compressed != 0 && compressed != 1)
                throw new InvalidDataException("invalid .perf file format");

            if (compressedSize <= 0 || compressedSize > MaxChunkSize || uncompressedSize <= 0 || uncompressedSize > MaxChunkSize)
                throw new InvalidDataException("invalid .perf file format");

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
                    throw new InvalidDataException("Inflate error");
            }

            // wrap uncompressed data
            return new MemoryStream(uncompressedData);
        }

        private static byte ReadByte(Stream ms)
        {
            int value = ms.ReadByte();
            if (value == -1) 
                throw new EndOfStreamException();
            return (byte)value;
        }

        private static ushort ReadShort(Stream ms)
        {
            int byte0 = ms.ReadByte();
            int byte1 = ms.ReadByte();
            if (byte1 == -1) 
                throw new EndOfStreamException();
            return (ushort)(byte0 | (byte1 << 8));
        }

        private static int ReadInt(Stream ms)
        {
            int byte0 = ms.ReadByte();
            int byte1 = ms.ReadByte();
            int byte2 = ms.ReadByte();
            int byte3 = ms.ReadByte();
            if (byte3 == -1)
                throw new EndOfStreamException();
            return byte0 | (byte1 << 8) | (byte2 << 16) | (byte3 << 24);
        }

        private enum EventType
        {
            IRQ = 0,
            NMI = 1,
        }

        private enum ModifiedRegister : byte
        {
            // cycle count held in bits 0..3
            Accumulator = 0x08,
            XRegister = 0x10,
            YRegister = 0x20,
            StatusRegister = 0x40,
            StackPointer = 0x80
        };

        private InstructionSet? _InstructionSet;

        private ushort _LastOpcodeAddress = 0;
        private int _InstructionCount = 0;

        private byte _Accumulator;
        private byte _XRegister;
        private byte _YRegister;
        private byte _StackPointer;

        private int _RomPageSelected;
        private bool _ShadowRamSelected;        // BPlus, IntegraB, Master
        private bool _ShadowRamEnabled;         // IntegraB
        private bool _ShadowRamXBit;            // Master
        private bool _ShadowRamEBit;            // Master
        private bool _PrivateRamSelected;       // BPlus, IntegraB, Master 
        private bool _PrivateRam1kArea;         // IntegraB
        private bool _PrivateRam4kArea;         // IntegraB
        private bool _PrivateRam8kArea;         // IntegraB
        private bool _FilingSystemRamSelected;  // Master

        private static readonly int MaxChunkSize = 0x100000; // 1MB 
    }
}