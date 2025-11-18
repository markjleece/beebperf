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
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace BeebPerf
{
    public class VideoAnalysis
    {
        static VideoAnalysis()
        {
            Color[] colors = [
                Color.Black,
                Color.Red,
                Color.Green,
                Color.Yellow,
                Color.Blue,
                Color.Magenta,
                Color.Cyan,
                Color.White,
                Color.Black,    // flashing black-white
                Color.Red,      // flashing red-cyan
                Color.Green,   // flashing green-magenta
                Color.Yellow,  // flashing yellow-blue
                Color.Blue,    // flashing blue-yellow
                Color.Magenta, // flashing magenta-green
                Color.Cyan,    // flashing cyan-red
                Color.White ]; // flashing white-black

            _ColorPalette = new(colors);
        }

        public VideoAnalysis()
        {
            _WriteBitmapDataFunc = WriteBitmapData_Void;
        }

        public async Task<bool> AnalysisAsync(
            Instruction[] instructions,
            InstructionSet instructionSet,
            Model model)
        {
            return await Task.Run(() =>
            {
                Analysis(instructions, instructionSet, model);
                return true;
            });
        }

        private bool Analysis(
            Instruction[] instructions,
            InstructionSet instructionSet,
            Model model)
        {
            _Instructions = instructions;
            _InstructionSet = instructionSet;

            FrameBitmaps = new();
            _BitmapData = [];
            _ULAPalette = model.Snapshot.VideoULAPalette.ToArray();
            _ULARegister = model.Snapshot.VideoULARegister;

            _CtrlR0_HorizontalTotal = model.Snapshot.VideoCtrlRegisters[0];
            _CtrlR1_HorizontalDisplayed = model.Snapshot.VideoCtrlRegisters[1];
            _CtrlR4_VerticalTotal = model.Snapshot.VideoCtrlRegisters[4];
            _CtrlR6_VerticalDisplayed = model.Snapshot.VideoCtrlRegisters[6];
            _CtrlR8_InterlaceAndDelay = model.Snapshot.VideoCtrlRegisters[8];
            _CtrlR9_ScanLinesPerChar = model.Snapshot.VideoCtrlRegisters[9];
            _CtrlR12_ScreenStartHigh = model.Snapshot.VideoCtrlRegisters[12];
            _CtrlR13_ScreenStartLow = model.Snapshot.VideoCtrlRegisters[13];

            _Memory = new byte[65536];
            Buffer.BlockCopy(model.Snapshot.Memory[(int)MemoryPage.WholeRam], 0, _Memory, 0, 65536);

            _ShadowRAM = new byte[20480];
            if (model.Snapshot.Memory[(int)MemoryPage.ShadowRam] != null)
                Buffer.BlockCopy(model.Snapshot.Memory[(int)MemoryPage.ShadowRam], 0, _ShadowRAM, 0, 20480);

            _ScreenAddress = model.Snapshot.ScreenAddress;
            _ScreenSize = 0x8000 - _ScreenAddress;

            _ScreenStartAddress = 0;
            _RegisterModified = true;

            GenerateFrameBitmaps();

            return true;
        }

        public void GenerateFrameBitmaps()
        {
            _DisplayEnabled = false;
            _FrameCount = 1;

            FrameBitmaps = new();

            int cycleCount = 0;
            foreach (var instruction in _Instructions)
            {
                if (instruction.IsInstruction)
                {
                    byte opcode = instruction.Opcode;
                    byte memoryAccess = _InstructionSet!.MemoryAccess(opcode);
                    if ((memoryAccess & 0x2/*write*/) != 0)
                    {
                        MemoryWrite(instruction.MemoryAddress, instruction.MemoryWriteValue);
                        if (_RegisterModified)
                            UpdateVideo();
                    }
                }
                else if (instruction.IsBeginDisplayEvent)
                {
                    StartFrame(cycleCount);
                }

                cycleCount += instruction.CycleCount;

                if (_DisplayEnabled)
                    DisplayMemory(cycleCount);
            }
        }

        private void StartFrame(int cycleCount)
        {
            Debug.Assert(!_DisplayEnabled);
            _DisplayEnabled = true;

            _BitmapWidth = 0;
            _BitmapHeight = 0;
            _StartCycleCount = cycleCount;
            _BitmapData = new uint[80 * 256];

            _BlankSpace = false;
            _RowCounter = 0;
            _ColumnCounter = 0;
            _ScanlineCounter = 0;
            _BitmapScanlineOffset = 0;

            UpdateVideo();
        }

        private void EndFrame(int cycleCount)
        {
            Debug.Assert(cycleCount - _StartCycleCount < 480000);

            Debug.Assert(_DisplayEnabled);
            _DisplayEnabled = false;

            Bitmap bitmap = new Bitmap(_BitmapWidth, _BitmapHeight, PixelFormat.Format4bppIndexed);
            bitmap.Palette = _ColorPalette;

            Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            System.Drawing.Imaging.BitmapData? data = null;

            try
            {
                data = bitmap.LockBits(rect, ImageLockMode.WriteOnly, bitmap.PixelFormat);
                unsafe
                {
                    fixed (uint* src = _BitmapData)
                    {
                        byte* dst = (byte*)data.Scan0;

                        if (data.Stride != 80 * sizeof(uint))
                        {
                            for (int i = 0; i < _BitmapHeight; i++)
                            {
                                Buffer.MemoryCopy(
                                    src + (i * 80),
                                    dst + (i * data.Stride),
                                    data.Width / 2,
                                    data.Width / 2);
                            }
                        }
                        else
                        {
                            Buffer.MemoryCopy(
                                dst,
                                (void*)data.Scan0,
                                _BitmapData.Length * sizeof(uint),
                                _BitmapData.Length * sizeof(uint));
                        }
                    }
                }
            }
            finally
            {
                if (data != null)
                    bitmap.UnlockBits(data);
            }

            FrameBitmaps.Add(new()
            {
                FrameNumber = _FrameCount++,
                StartCycleCount = _StartCycleCount,
                EndCycleCount = cycleCount,
                Bitmap = bitmap
            });
        }

        private void MemoryWrite(CanonicalAddress memoryAddress, byte value)
        {
            ushort address = memoryAddress.Address;

            // TODO shadow ram
            if (memoryAddress.Page != MemoryPage.WholeRam || address < _ScreenAddress)
                return;

            if (address < 0x8000)
            {
                _Memory[address] = value;
            }
            else if (address == 0xFE00)
            {
                _CtrlWriteRegister = value;
            }
            else if (address == 0xFE01)
            {
                switch (_CtrlWriteRegister)
                {
                    case 0:
                        _RegisterModified |= (value != _CtrlR0_HorizontalTotal);
                        _CtrlR0_HorizontalTotal = value;
                        break;

                    case 1:
                        _RegisterModified |= (value != _CtrlR1_HorizontalDisplayed);
                        _CtrlR1_HorizontalDisplayed = value;
                        break;

                    case 4:
                        _RegisterModified |= (value != _CtrlR4_VerticalTotal);
                        _CtrlR4_VerticalTotal = value;
                        break;

                    case 6:
                        _RegisterModified |= (value != _CtrlR6_VerticalDisplayed);
                        _CtrlR6_VerticalDisplayed = value;
                        break;

                    case 8:
                        _RegisterModified |= (value != _CtrlR8_InterlaceAndDelay);
                        _CtrlR8_InterlaceAndDelay = value;
                        break;

                    case 9:
                        _RegisterModified |= (value != _CtrlR9_ScanLinesPerChar);
                        _CtrlR9_ScanLinesPerChar = value;
                        break;

                    case 12:
                        _RegisterModified |= (value != _CtrlR12_ScreenStartHigh);
                        _CtrlR12_ScreenStartHigh = value;
                        break;

                    case 13:
                        _RegisterModified |= (value != _CtrlR13_ScreenStartLow);
                        _CtrlR13_ScreenStartLow = value;
                        break;

                    default:
                        break;
                }
            }
            else if (address == 0xFE20)
            {
                _RegisterModified |= (value != _ULARegister);
                _ULARegister = value;
            }
            else if (address == 0xFE21)
            {
                _RegisterModified |= (value != _ULAPalette[value >> 4]);
                _ULAPalette[value >> 4] = value;
            }
            else if (address == 0xFE42) // set data direction
            {
                _SystemVIA_DataDirection = value;
            }
            else if (address == 0xFE40) // set screen wrap direction
            {
                if ((_SystemVIA_DataDirection & 0xF) == 0xF)
                {
                    int latchIndex = (value & 0x07);
                    int latchValue = (value >> 3) & 0x01;
                    if (latchIndex == 4)
                        _ScreenAddressLatch = (byte)((_ScreenAddressLatch & 0xFE) | latchValue);
                    else if (latchIndex == 5)
                        _ScreenAddressLatch = (byte)((_ScreenAddressLatch & 0xFD) | latchValue);

                    _ScreenAddress = _ScreenAddressLatch switch
                    {
                        0 => 0x4000,
                        1 => 0x6000,
                        2 => 0x3000,
                        3 => 0x5800,
                        _ => throw new NotImplementedException()
                    };

                    _ScreenSize = 0x8000 - _ScreenAddress;
                }
            }
        }

        private void DisplayMemory(int cycleCount)
        {
            Debug.Assert(_DisplayEnabled);

            int characterCount = (cycleCount >> _CharacterClockShift) - (_LastCycleCount >> _CharacterClockShift);

            _LastCycleCount = cycleCount;

            for (int i = 0; i < characterCount; i++) // need to deal with interlacing
            {
                // advance counters
                _ColumnCounter++;
                _BlankSpace |= (_ColumnCounter == _CtrlR1_HorizontalDisplayed);
                if (_ColumnCounter == _CtrlR0_HorizontalTotal + 1)
                {
                    _ColumnCounter = 0;
                    _BitmapScanlineOffset += 80;

                    _ScanlineCounter++;
                    if (_ScanlineCounter == (_CtrlR9_ScanLinesPerChar + 1))
                    {
                        _ScanlineCounter = 0;
                        _RowCounter++;

                        if (_RowCounter == _CtrlR6_VerticalDisplayed)
                            EndFrame(cycleCount);
                    }

                    _BlankSpace = (_RowCounter >= _CtrlR6_VerticalDisplayed);
                }

                if (_BlankSpace)
                    continue;

                byte value = 0;

                if (_ScanlineCounter < 8)
                {
                    // calc memory address
                    int characterAddress = _ScreenStartAddress + (_RowCounter * _CtrlR1_HorizontalDisplayed) + _ColumnCounter;
                    int memoryAddress = (characterAddress << 3) + _ScanlineCounter;

                    if (memoryAddress > 0x8000)
                        memoryAddress -= _ScreenSize;

                    // read memory
                    value = _Memory[memoryAddress];
                }

                // write pixel data with delegate
                _WriteBitmapDataFunc(value);
            }
        }

        private delegate void WriteBitmapData(byte value);

        private void UpdateVideo()
        {
            _RegisterModified = false;

            _TeletextMode = ((_ULARegister >> 1) & 0x01) != 0;

            int characterClockRate = (_ULARegister >> 4) & 0x01;
            _CharacterClockShift = 1 - characterClockRate;

            _ScreenStartAddress = (_CtrlR12_ScreenStartHigh << 8) + _CtrlR13_ScreenStartLow;
            int horizontalMultiplier;

            switch ((_ULARegister >> 2) & 0x7)
            {
                case 7: // Mode 0 & 3 (2 color, high res)
                    _BitsPerPixel = 1;
                    Build4bppLookupTbl();
                    _WriteBitmapDataFunc = WriteBitmapData_32bits;
                    horizontalMultiplier = 1;
                    break;

                case 6: // Mode 1 (4 color, medium res)
                    _BitsPerPixel = 2;
                    Build8bppLookupTbl();
                    _WriteBitmapDataFunc = WriteBitmapData_32bits;
                    horizontalMultiplier = 1;
                    break;

                case 5: // Mode 2 (16 colors, low res)
                    _BitsPerPixel = 4;
                    Build16bppLookupTbl();
                    _WriteBitmapDataFunc = WriteBitmapData_32bits;
                    horizontalMultiplier = 1;
                    break;

                case 2: // Mode 4 & 6 (2 color, medium res)
                    _BitsPerPixel = 1;
                    Build8bppLookupTbl();
                    _WriteBitmapDataFunc = WriteBitmapData_64bits;
                    horizontalMultiplier = 2;
                    break;

                case 1: // Mode 5 (4 color, low res)
                    _BitsPerPixel = 2;
                    Build16bppLookupTbl();
                    _WriteBitmapDataFunc = WriteBitmapData_64bits;
                    horizontalMultiplier = 2;
                    break;

                case 0: // Mode 8 (16 color, very low res)
                    _BitsPerPixel = 4;
                    Build32bppLookupTbl();
                    _WriteBitmapDataFunc = WriteBitmapData_64bits;
                    horizontalMultiplier = 2;
                    break;

                default:
                    _WriteBitmapDataFunc = WriteBitmapData_Void;
                    horizontalMultiplier = 1;
                    break;
            }

            int bitmapWidth = horizontalMultiplier * (_CtrlR1_HorizontalDisplayed * 8);
            int bitmapHeight = _CtrlR6_VerticalDisplayed * (_CtrlR9_ScanLinesPerChar + 1);

            _BitmapWidth = Math.Max(_BitmapWidth, bitmapWidth);
            _BitmapHeight = Math.Max(_BitmapHeight, bitmapHeight);
        }

        private void WriteBitmapData_Void(byte value)
        {
            // nothing
        }

        private void WriteBitmapData_32bits(byte value)
        {
            unsafe
            {
                int index = _BitmapScanlineOffset + _ColumnCounter;
                _BitmapData[index] = _BitmapDataTbl[value].Words[0];
            }
        }

        private void WriteBitmapData_64bits(byte value)
        {
            unsafe
            {
                int index = _BitmapScanlineOffset + (_ColumnCounter << 1);
                fixed (uint* words = _BitmapDataTbl[value].Words)
                {
                    _BitmapData[index] = words[0];
                    _BitmapData[index + 1] = words[1];
                }
            }
        }

        private void Build4bppLookupTbl()
        {
            Debug.Assert(_BitsPerPixel == 1);
            int shiftCount = 8 / _BitsPerPixel;
            for (int value = 0; value < 256; value++)
            {
                int shiftRegister = value;
                for (int i = 0; i < shiftCount; i += 2)
                {
                    // first lookup / shift
                    int firstIndex =
                        ((shiftRegister >> 4) & 0x8) |
                        ((shiftRegister >> 3) & 0x4) |
                        ((shiftRegister >> 2) & 0x2) |
                        (shiftRegister & 0x1);

                    int firstEntry = _ULAPalette[firstIndex] & 0xF;
                    if (firstEntry > 0x7) // apply flash?
                    {
                        firstEntry &= 0x7;
                        if ((_ULARegister & 0x1) != 0)
                            firstEntry ^= 0x7;
                    }

                    shiftRegister = (shiftRegister << 1) | 0x1;

                    // second lookup / shift
                    int secondIndex =
                        ((shiftRegister >> 4) & 0x8) |
                        ((shiftRegister >> 3) & 0x4) |
                        ((shiftRegister >> 2) & 0x2) |
                        (shiftRegister & 0x1);

                    int secondEntry = _ULAPalette[secondIndex] & 0xF;
                    if (secondEntry > 0x7) // apply flash?
                    {
                        secondEntry &= 0x7;
                        if ((_ULARegister & 0x1) != 0)
                            secondEntry ^= 0x7;
                    }

                    shiftRegister = (shiftRegister << 1) | 0x1;

                    unsafe
                    {
                        byte pixelPair = (byte)((firstEntry << 4) | secondEntry);
                        _BitmapDataTbl[value].Bytes[i >> 1] = pixelPair;
                    }
                }
            }
        }

        private void Build8bppLookupTbl()
        {
            Debug.Assert(_BitsPerPixel == 1 || _BitsPerPixel == 2);
            int shiftCount = 8 / _BitsPerPixel;
            for (int value = 0; value < 256; value++)
            {
                int shiftRegister = value;
                for (int i = 0; i < shiftCount; i++)
                {
                    // lookup / shift
                    int index =
                        ((shiftRegister >> 4) & 0x8) |
                        ((shiftRegister >> 3) & 0x4) |
                        ((shiftRegister >> 2) & 0x2) |
                        (shiftRegister & 0x1);

                    int entry = _ULAPalette[index] & 0xF;

                    if (entry > 0x7) // apply flash?
                    {
                        entry &= 0x7;
                        if ((_ULARegister & 0x1) != 0)
                            entry ^= 0x7;
                    }

                    shiftRegister = (shiftRegister << 1) | 0x1;

                    unsafe
                    {
                        byte pixelPair = (byte)((entry << 4) | entry);
                        _BitmapDataTbl[value].Bytes[i] = pixelPair;
                    }
                }
            }
        }

        private void Build16bppLookupTbl()
        {
            Debug.Assert(_BitsPerPixel == 2 || _BitsPerPixel == 4);
            int shiftCount = 8 / _BitsPerPixel;
            for (int value = 0; value < 256; value++)
            {
                int shiftRegister = value;
                for (int i = 0; i < shiftCount; i++)
                {
                    // lookup / shift
                    int index =
                        ((shiftRegister >> 4) & 0x8) |
                        ((shiftRegister >> 3) & 0x4) |
                        ((shiftRegister >> 2) & 0x2) |
                        (shiftRegister & 0x1);

                    int entry = _ULAPalette[index] & 0xF;

                    if (entry > 0x7) // apply flash?
                    {
                        entry &= 0x7;
                        if ((_ULARegister & 0x1) != 0)
                            entry ^= 0x7;
                    }

                    shiftRegister = (shiftRegister << 1) | 0x1;

                    unsafe
                    {
                        var pixelQuad = (ushort)((entry << 12) | (entry << 8) | (entry << 4) | entry);
                        _BitmapDataTbl[value].Shorts[i] = pixelQuad;
                    }
                }
            }
        }

        private void Build32bppLookupTbl()
        {
            Debug.Assert(_BitsPerPixel == 4);
            int shiftCount = 8 / _BitsPerPixel;
            for (int value = 0; value < 256; value++)
            {
                int shiftRegister = value;
                for (int i = 0; i < shiftCount; i++)
                {
                    // lookup / shift
                    int index =
                        ((shiftRegister >> 4) & 0x8) |
                        ((shiftRegister >> 3) & 0x4) |
                        ((shiftRegister >> 2) & 0x2) |
                        (shiftRegister & 0x1);

                    int entry = _ULAPalette[index] & 0xF;

                    if (entry > 0x7) // apply flash?
                    {
                        entry &= 0x7;
                        if ((_ULARegister & 0x1) != 0)
                            entry ^= 0x7;
                    }

                    shiftRegister = (shiftRegister << 1) | 0x1;

                    unsafe
                    {
                        var pixelQuad = (ushort)((entry << 12) | (entry << 8) | (entry << 4) | entry);
                        var pixelOct = (uint)(pixelQuad << 16 | pixelQuad);
                        _BitmapDataTbl[value].Words[i] = pixelOct;
                    }
                }
            }
        }

        public class FrameBitmap
        {
            public int FrameNumber; // 1, 2, 3...
            public int StartCycleCount;
            public int EndCycleCount;
            public Bitmap? Bitmap;
        }

        public List<FrameBitmap> FrameBitmaps = [];

        private Instruction[] _Instructions = [];
        private InstructionSet? _InstructionSet;

        private byte _ULARegister;
        private byte[] _ULAPalette = [];

        private byte _CtrlR0_HorizontalTotal;
        private byte _CtrlR1_HorizontalDisplayed;
        private byte _CtrlR4_VerticalTotal;
        private byte _CtrlR6_VerticalDisplayed;
        private byte _CtrlR8_InterlaceAndDelay;
        private byte _CtrlR9_ScanLinesPerChar;
        private byte _CtrlR12_ScreenStartHigh;
        private byte _CtrlR13_ScreenStartLow;
        private byte _CtrlWriteRegister;

        private bool _RegisterModified;

        private WriteBitmapData _WriteBitmapDataFunc;
        private byte[] _Memory = [];
        private byte[] _ShadowRAM = [];

        private int _ScanlineCounter;
        private int _ColumnCounter;
        private int _RowCounter;
        private int _BitmapScanlineOffset;
        private int _ScreenSize;
        private int _ScreenAddress;
        private int _ScreenStartAddress;
        private uint[] _BitmapData = [];
        private int _BitsPerPixel;
        private int _FrameCount;
        private int _StartCycleCount;
        private int _LastCycleCount;
        private int _CharacterClockShift; // 0 for modes 0..3, 1 for modes 4..7
        private int _BitmapWidth;
        private int _BitmapHeight;
        private bool _TeletextMode;
        private static ColorPalette _ColorPalette;
        private byte _SystemVIA_DataDirection;
        private byte _ScreenAddressLatch;
        private bool _BlankSpace;
        private bool _DisplayEnabled;

        [StructLayout(LayoutKind.Explicit)]
        public unsafe struct BitmapData
        {
            [FieldOffset(0)] public fixed byte Bytes[8];
            [FieldOffset(0)] public fixed ushort Shorts[4];
            [FieldOffset(0)] public fixed uint Words[2];
        }

        private BitmapData[] _BitmapDataTbl = new BitmapData[256];
    }
}