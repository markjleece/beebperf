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
                Color.Black,   // flashing black-white
                Color.Red,     // flashing red-cyan
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
            _ReadScreenDataFunc = ReadScreenData_Void;

            for (int i = 0; i < 256; i++)
                _CRTBitmapTbl[i] = new byte[8];

            // TODO: put on separate thread or embed bitmaps in exe
            InitialiseMode7Font(GetSolutionFolder() + "\\teletext.fnt");
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
            _CRTBitmap = new byte[_CRTMaxBitmapHeight * _CRTBitmapStride];
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

        private delegate byte ReadScreenData();
        private delegate void WriteBitmapData(byte value);
        private void UpdateVideo()
        {
            _RegisterModified = false;

            int characterClockRate = (_ULARegister >> 4) & 0x01;
            _CharacterClockShift = 1 - characterClockRate;

            bool interlacedSync = (_CtrlR8_InterlaceAndDelay & 0x1) != 0;
            _CRTBitmapScanlineOffsetIncrement = (interlacedSync ? 2 : 1) * _CRTBitmapStride;
            _CRTBitmapScanlineOffsetReset = (interlacedSync ? _FrameCount % 2 : 0) * _CRTBitmapStride;

            bool interlacedVideo = (_CtrlR8_InterlaceAndDelay & 0x2) != 0;
            _ScanlineCounterIncrement = interlacedVideo ? 2 : 1;
            _ScanlineCounterReset = interlacedVideo ? _FrameCount % 2 : 0;

            _ScanlinesPerCharAdjust = (interlacedSync && interlacedVideo) ? 2 : 1;

            _TeletextMode = ((_ULARegister >> 1) & 0x01) != 0;

            if (_TeletextMode)
                _ScreenStartAddress = (((_CtrlR12_ScreenStartHigh ^ 0x20) + 0x74) << 8) + _CtrlR13_ScreenStartLow;
            else
                _ScreenStartAddress = (_CtrlR12_ScreenStartHigh << 8) + _CtrlR13_ScreenStartLow;

            _ReadScreenDataFunc = _TeletextMode ? ReadScreenData_Teletext : ReadScreenData_NonTeletext;

            int horizontalMultiplier;
            int bitmapWidth, bitmapHeight;
            if (!_TeletextMode)
            {
                switch ((_ULARegister >> 2) & 0x7)
                {
                    case 7: // Mode 0 & 3 (2 pixelColor, high res)
                        _BitsPerPixel = 1;
                        Build4bppLookupTbl();
                        _WriteBitmapDataFunc = WriteBitmapData_32bits;
                        horizontalMultiplier = 1;
                        break;

                    case 6: // Mode 1 (4 pixelColor, medium res)
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

                    case 2: // Mode 4 & 6 (2 pixelColor, medium res)
                        _BitsPerPixel = 1;
                        Build8bppLookupTbl();
                        _WriteBitmapDataFunc = WriteBitmapData_64bits;
                        horizontalMultiplier = 2;
                        break;

                    case 1: // Mode 5 (4 pixelColor, low res)
                        _BitsPerPixel = 2;
                        Build16bppLookupTbl();
                        _WriteBitmapDataFunc = WriteBitmapData_64bits;
                        horizontalMultiplier = 2;
                        break;

                    case 0: // Mode 8 (16 pixelColor, very low res)
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

                bitmapWidth = horizontalMultiplier * (_CtrlR1_HorizontalDisplayed * 8);
                bitmapHeight = _CtrlR6_VerticalDisplayed * (_CtrlR9_ScanLinesPerChar + _ScanlinesPerCharAdjust);
            }
            else
            {
                _WriteBitmapDataFunc = WriteBitmapData_Mode7;

                bitmapWidth = 480;
                bitmapHeight = 500;
            }

            _CRTBitmapWidth = Math.Max(_CRTBitmapWidth, bitmapWidth);
            _CRTBitmapHeight = Math.Max(_CRTBitmapHeight, bitmapHeight);
        }

        private void StartFrame(int cycleCount)
        {
            Debug.Assert(!_DisplayEnabled);
            _DisplayEnabled = true;

            _CRTBitmapWidth = 0;
            _CRTBitmapHeight = 0;

            _StartCycleCount = cycleCount;
            _LastCycleCount = cycleCount;

            _BlankSpace = false;
            _RowCounter = 0;
            _ColumnCounter = 0;

            _Mode7State.Mode7FlashOn = false;

            if (--_Mode7State.Mode7FlashTrigger < 0)
            {
                _Mode7State.Mode7FlashTrigger = _Mode7State.Mode7FlashOn ? Mode7_FlashOffFrameCount : Mode7_FlashOnFrameCount;
                _Mode7State.Mode7FlashOn = !_Mode7State.Mode7FlashOn; // toggle flash state
            }

            UpdateVideo();

            _ScanlineCounter = _ScanlineCounterReset;
            _CRTBitmapScanlineOffset = _CRTBitmapScanlineOffsetReset;
        }

        private void EndFrame(int cycleCount)
        {
            Debug.Assert(cycleCount - _StartCycleCount < 480000);

            Debug.Assert(_DisplayEnabled);
            _DisplayEnabled = false;

            Bitmap bitmap = new Bitmap(_CRTBitmapWidth, _CRTBitmapHeight, PixelFormat.Format4bppIndexed);
            bitmap.Palette = _ColorPalette;

            Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            System.Drawing.Imaging.BitmapData? data = null;

            try
            {
                data = bitmap.LockBits(rect, ImageLockMode.WriteOnly, bitmap.PixelFormat);
                unsafe
                {
                    fixed (byte* src = _CRTBitmap)
                    {
                        byte* dst = (byte*)data.Scan0;

                        if (data.Stride != _CRTBitmapStride)
                        {
                            for (int i = 0; i < _CRTBitmapHeight; i++)
                            {
                                Buffer.MemoryCopy(
                                    src + (i * _CRTBitmapStride),
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
                                _CRTBitmap.Length,
                                _CRTBitmap.Length);
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
                AspectRatio = _TeletextMode ? _CRTAspectRatio_Teletext : _CRTAspectRatio_NonTeletext,
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
                        _ => throw new ArgumentOutOfRangeException()
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

            for (int i = 0; i < characterCount; i++) // TODO: Need to deal with interlacing
            {
                if (!_BlankSpace)
                {
                    byte value = _ReadScreenDataFunc();
                    _WriteBitmapDataFunc(value);
                }

                // advance counters
                _ColumnCounter++;
                _BlankSpace |= (_ColumnCounter == _CtrlR1_HorizontalDisplayed);
                if (_ColumnCounter == _CtrlR0_HorizontalTotal + 1)
                {
                    _ColumnCounter = 0;
                    _CRTBitmapScanlineOffset += _CRTBitmapScanlineOffsetIncrement;

                    _ScanlineCounter += _ScanlineCounterIncrement;
                    if (_ScanlineCounter >= (_CtrlR9_ScanLinesPerChar + _ScanlinesPerCharAdjust))
                    {
                        _ScanlineCounter = _ScanlineCounterReset;
                        _RowCounter++;

                        if (_RowCounter == _CtrlR6_VerticalDisplayed)
                            EndFrame(cycleCount);
                    }

                    _BlankSpace = (_RowCounter >= _CtrlR6_VerticalDisplayed);
                }
            }
        }

        private byte ReadScreenData_Void()
        {
            return 0;
        }

        private byte ReadScreenData_Teletext()
        {
            int characterAddress = _ScreenStartAddress + (_RowCounter * _CtrlR1_HorizontalDisplayed) + _ColumnCounter;

            if (characterAddress > 0x8000)
                characterAddress -= _ScreenSize;

            return _Memory[characterAddress];
        }

        private byte ReadScreenData_NonTeletext()
        {
            if (_ScanlineCounter >= 8)
                return 0;

            int characterAddress = _ScreenStartAddress + (_RowCounter * _CtrlR1_HorizontalDisplayed) + _ColumnCounter;
            int memoryAddress = (characterAddress << 3) + _ScanlineCounter;

            if (memoryAddress > 0x8000)
                memoryAddress -= _ScreenSize;

            return _Memory[memoryAddress];
        }

        private void WriteBitmapData_Void(byte value)
        {
        }

        private void WriteBitmapData_32bits(byte value)
        {
            int index = _CRTBitmapScanlineOffset + (_ColumnCounter << 2);
            Buffer.BlockCopy(_CRTBitmapTbl[value], 0, _CRTBitmap, index, 4);
        }

        private void WriteBitmapData_64bits(byte value)
        {
            int index = _CRTBitmapScanlineOffset + (_ColumnCounter << 3);
            Buffer.BlockCopy(_CRTBitmapTbl[value], 0, _CRTBitmap, index, 8);
        }

        private void UpdateMode7StateBegin(byte value)
        {
            if (_ColumnCounter == 0) // first char in row?
            {
                _Mode7State.ForeColor = 7;
                _Mode7State.ForeColorPending = 7;
                _Mode7State.BackColor = 0;
                _Mode7State.NextFlash = false;
                _Mode7State.DoubleHeight = false;
                _Mode7State.NextGraphics = false;
                _Mode7State.Mosaic = false;
                _Mode7State.NextHoldGraphics = false;
                _Mode7State.NextHoldGraphicsChar = 32;
                _Mode7State.NextHoldMosaic = false;
            }

            _Mode7State.HoldGraphics = _Mode7State.NextHoldGraphics;
            _Mode7State.HoldGraphicsChar = _Mode7State.NextHoldGraphicsChar;
            _Mode7State.HoldMosaic = _Mode7State.NextHoldMosaic;
            _Mode7State.Graphics = _Mode7State.NextGraphics;
            _Mode7State.Flash = _Mode7State.NextFlash;

            if (value < 32)
                value += 128; // some programs use 7-bit control codes!

            if ((value & 32) != 0 && _Mode7State.Graphics)
            {
                _Mode7State.NextHoldGraphicsChar = value;
                _Mode7State.NextHoldMosaic = _Mode7State.Mosaic;
            }

            uint[][] characterScanlines;

            if ((value >= 128) && (value <= 159))
            {
                if (!_Mode7State.HoldGraphics && value != 158) 
                    _Mode7State.NextHoldGraphicsChar = 32; // SAA5050 teletext rendering bug

                switch (value)
                {
                    case 129: // alphanumeric red
                    case 130: // alphanumeric green
                    case 131: // alphanumeric yellow
                    case 132: // alphanumeric blue
                    case 133: // alphanumeric magenta
                    case 134: // alphanumeric cyan
                    case 135: // alphanumeric white
                        _Mode7State.ForeColorPending = (byte)(value - 128);
                        _Mode7State.NextGraphics = false;
                        _Mode7State.NextHoldGraphicsChar = 32; // space
                        break;

                    case 136: // flash
                        _Mode7State.NextFlash = true;
                        break;

                    case 137: // steady
                        _Mode7State.NextFlash = false;
                        _Mode7State.Flash = false;
                        break;

                    case 140: // normal height
                        if (_Mode7State.DoubleHeight)
                        {
                            _Mode7State.NextHoldGraphicsChar = 32;
                            _Mode7State.HoldGraphicsChar = _Mode7State.NextHoldGraphicsChar;
                        }
                        _Mode7State.DoubleHeight = false;
                        break;

                    case 141: // double height
                        if (!_Mode7State.DoubleHeight)
                        {
                            _Mode7State.NextHoldGraphicsChar = 32;
                            _Mode7State.HoldGraphicsChar = _Mode7State.NextHoldGraphicsChar;
                        }
                        _Mode7State.DoubleHeight = true;
                        break;

                    case 145: // graphics red
                    case 146: // graphics green
                    case 147: // graphics yellow
                    case 148: // graphics blue
                    case 149: // graphics magenta
                    case 150: // graphics cyan
                    case 151: // graphics white
                        _Mode7State.ForeColorPending = (byte)(value - 144);
                        _Mode7State.NextGraphics = true;
                        break;

                    case 152: // conceal display
                        _Mode7State.ForeColor = _Mode7State.BackColor;
                        _Mode7State.ForeColorPending = _Mode7State.BackColor;
                        break;

                    case 153: // contiguous graphics
                        _Mode7State.Mosaic = false;
                        break;

                    case 154: // mosaic graphics
                        _Mode7State.Mosaic = true;
                        break;

                    case 156: // black background
                        _Mode7State.BackColor = 0;
                        break;

                    case 157: // new background
                        _Mode7State.BackColor = _Mode7State.ForeColor;
                        break;

                    case 158: // hold graphics
                        _Mode7State.NextHoldGraphics = true;
                        _Mode7State.HoldGraphics = true;
                        break;

                    case 159: // release graphics
                        _Mode7State.NextHoldGraphics = false;
                        break;
                }

                if (_Mode7State.HoldGraphics && _Mode7State.Graphics)
                {
                    characterScanlines = _Mode7State.HoldMosaic ? _Mode7MosaicFont : _Mode7GraphicFont;
                    value = _Mode7State.HoldGraphicsChar;
                }
                else
                {
                    characterScanlines = _Mode7State.Graphics ? (_Mode7State.Mosaic ? _Mode7MosaicFont : _Mode7GraphicFont) : _Mode7TextFont;
                    value = 32; // space
                }
            }
            else
            {
                characterScanlines = _Mode7State.Graphics ? (_Mode7State.Mosaic ? _Mode7MosaicFont : _Mode7GraphicFont) : _Mode7TextFont;
            }

            Mode7CharacterHeight height;
            if (_Mode7State.DoubleHeight)
            {
                height = _Mode7State.LineChars[_ColumnCounter].Height switch
                {
                    Mode7CharacterHeight.Normal => Mode7CharacterHeight.Double_Upper,
                    Mode7CharacterHeight.Double_Upper => Mode7CharacterHeight.Double_Lower,
                    Mode7CharacterHeight.Double_Lower => Mode7CharacterHeight.Double_Upper,
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
            else
            {
                height = Mode7CharacterHeight.Normal;
            }

            value &= 0x7F;

            if (value >= 32)
                value -= 32;
            else
                value = 0;

            byte foreground;
            if (_Mode7State.Flash && !_Mode7State.Mode7FlashOn)
                foreground = _Mode7State.BackColor;
            else
                foreground = _Mode7State.ForeColor;

            _Mode7State.ForeColor = _Mode7State.ForeColorPending;

            _Mode7State.LineChars[_ColumnCounter] = new Mode7Character()
            {
                Height = height,
                FontBitmap = characterScanlines[value],
                ForeColor = foreground,
                BackColor = _Mode7State.BackColor
            };
        }

        private void WriteBitmapData_Mode7(byte value)
        {
            if (_ColumnCounter >= 40)
                return;

            if (_ScanlineCounter <= 1) // first scanline?
                UpdateMode7StateBegin(value);

            var character = _Mode7State.LineChars[_ColumnCounter];

            int scanlineIndex = character.Height switch
            {
                Mode7CharacterHeight.Normal => _ScanlineCounter,
                Mode7CharacterHeight.Double_Upper => _ScanlineCounter >> 1,
                Mode7CharacterHeight.Double_Lower => 10 + (_ScanlineCounter >> 1),
                _ => throw new ArgumentOutOfRangeException()
            };
            uint bitmap = character.FontBitmap[scanlineIndex];

            int index = _CRTBitmapScanlineOffset + (_ColumnCounter * 6);

            uint pixelMask = 0x800;
            while (pixelMask != 0)
            {
                uint hiColor = ((bitmap & pixelMask) != 0) ? character.ForeColor : character.BackColor;
                pixelMask >>= 1;

                uint loColor = ((bitmap & pixelMask) != 0) ? character.ForeColor : character.BackColor;
                pixelMask >>= 1;

                _CRTBitmap[index++] = (byte)((hiColor << 4) | loColor);
            }
        }

        private void Build4bppLookupTbl()
        {
            Debug.Assert(_BitsPerPixel == 1);
            int shiftCount = 8 / _BitsPerPixel;
            for (int value = 0; value < 256; value++)
            {
                var bitmapTbl = _CRTBitmapTbl[value];

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
                    firstEntry ^= 0x7;

                    if (firstEntry > 0x7) // flashing color?
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
                    secondEntry ^= 0x7;

                    if (secondEntry > 0x7) // flashing color?
                    {
                        secondEntry &= 0x7;
                        if ((_ULARegister & 0x1) != 0)
                            secondEntry ^= 0x7;
                    }

                    shiftRegister = (shiftRegister << 1) | 0x1;

                    byte pixelPair = (byte)((firstEntry << 4) | secondEntry);
                    bitmapTbl[i >> 1] = pixelPair;
                }
            }
        }

        private void Build8bppLookupTbl()
        {
            Debug.Assert(_BitsPerPixel == 1 || _BitsPerPixel == 2);
            int shiftCount = 8 / _BitsPerPixel;
            for (int value = 0; value < 256; value++)
            {
                var bitmapTbl = _CRTBitmapTbl[value];

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
                    entry ^= 0x7;

                    if (entry > 0x7) // flashing color?
                    {
                        entry &= 0x7;
                        if ((_ULARegister & 0x1) != 0)
                            entry ^= 0x7;
                    }

                    shiftRegister = (shiftRegister << 1) | 0x1;

                    byte pixelPair = (byte)((entry << 4) | entry);
                    bitmapTbl[i] = pixelPair;
                }
            }
        }

        private void Build16bppLookupTbl()
        {
            Debug.Assert(_BitsPerPixel == 2 || _BitsPerPixel == 4);
            int shiftCount = 8 / _BitsPerPixel;
            for (int value = 0; value < 256; value++)
            {
                var bitmapTbl = _CRTBitmapTbl[value];

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
                    entry ^= 0x7;

                    if (entry > 0x7) // flashing color?
                    {
                        entry &= 0x7;
                        if ((_ULARegister & 0x1) != 0)
                            entry ^= 0x7;
                    }

                    shiftRegister = (shiftRegister << 1) | 0x1;

                    var pixelPair = (byte)((entry << 4) | entry);
                    int j = i << 1;
                    bitmapTbl[j++] = pixelPair;
                    bitmapTbl[j] = pixelPair;
                }
            }
        }

        private void Build32bppLookupTbl()
        {
            Debug.Assert(_BitsPerPixel == 4);
            int shiftCount = 8 / _BitsPerPixel;
            for (int value = 0; value < 256; value++)
            {
                var bitmapTbl = _CRTBitmapTbl[value];

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
                    entry ^= 0x7;

                    if (entry > 0x7) // flashing color?
                    {
                        entry &= 0x7;
                        if ((_ULARegister & 0x1) != 0)
                            entry ^= 0x7;
                    }

                    shiftRegister = (shiftRegister << 1) | 0x1;

                    var pixelPair = (byte)((entry << 4) | entry);
                    int j = i << 2;
                    bitmapTbl[j++] = pixelPair;
                    bitmapTbl[j++] = pixelPair;
                    bitmapTbl[j++] = pixelPair;
                    bitmapTbl[j] = pixelPair;
                }
            }
        }

        private bool InitialiseMode7Font(string fileName)
        {
            for (int ch = 0; ch <= 127 - 32; ch++)
            {
                _Mode7TextFont[ch] = new uint[20];
                _Mode7GraphicFont[ch] = new uint[20];
                _Mode7MosaicFont[ch] = new uint[20];
            }

            try
            {
                using var fs = File.OpenRead(fileName);

                for (int ch = 0; ch < 96; ch++)
                {
                    for (int i = 0; i < 2; i++)
                        _Mode7TextFont[ch][i] = 0;

                    for (int i = 2; i < 20; i++)
                    {
                        int loByte = fs.ReadByte();
                        if (loByte == -1)
                            throw new EndOfStreamException();

                        int hiByte = fs.ReadByte();
                        if (hiByte == -1)
                            throw new EndOfStreamException();

                        _Mode7TextFont[ch][i] = (uint)((hiByte << 8) | loByte);
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }

            for (int ch = 0; ch < 96; ch++)
            {
                if ((ch & 32) == 0)
                {
                    // graphic fonts
                    uint top = 0;
                    uint middle = 0;
                    uint bottom = 0;

                    if ((ch & 0x01) != 0) top |= 0x0FC0; 
                    if ((ch & 0x02) != 0) top |= 0x003F;
                    if ((ch & 0x04) != 0) middle |= 0x0FC0;
                    if ((ch & 0x08) != 0) middle |= 0x003F;
                    if ((ch & 0x10) != 0) bottom |= 0x0FC0;
                    if ((ch & 0x40) != 0) bottom |= 0x003F;

                    for (int i = 0; i < 6; i++)
                        _Mode7GraphicFont[ch][i] = top;

                    for (int i = 6; i < 14; i++)
                        _Mode7GraphicFont[ch][i] = middle;

                    for (int i = 14; i < 20; i++)
                        _Mode7GraphicFont[ch][i] = bottom;

                    // mosaic fonts
                    top &= 0x03CF;
                    middle &= 0x03CF;
                    bottom &= 0x03CF;

                    for (int i = 0; i < 4; i++)
                        _Mode7MosaicFont[ch][i] = top;

                    for (int i = 4; i < 6; i++)
                        _Mode7MosaicFont[ch][i] = 0;

                    for (int i = 6; i < 12; i++)
                        _Mode7MosaicFont[ch][i] = middle;

                    for (int i = 12; i < 14; i++)
                        _Mode7MosaicFont[ch][i] = 0;

                    for (int i = 14; i < 18; i++)
                        _Mode7MosaicFont[ch][i] = bottom;

                    for (int i = 18; i < 20; i++)
                        _Mode7MosaicFont[ch][i] = 0;
                }
            }

            return true;
        }

        static private string GetSolutionFolder()
        {
            var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (directory != null && directory.GetFiles("*.sln").Length == 0)
            {
                directory = directory.Parent;
            }
            return (directory != null) ? directory.FullName : string.Empty;
        }

        public class FrameBitmap
        {
            public int FrameNumber; // 1, 2, 3...
            public int StartCycleCount;
            public int EndCycleCount;
            public Bitmap? Bitmap;
            public float AspectRatio;
        }

        public List<FrameBitmap> FrameBitmaps = [];

        private Instruction[] _Instructions = [];
        private InstructionSet? _InstructionSet;
        private WriteBitmapData _WriteBitmapDataFunc;
        private ReadScreenData _ReadScreenDataFunc;
        private byte[] _Memory = [];
        private byte[] _ShadowRAM = [];
        private byte _SystemVIA_DataDirection;
        private byte _ScreenAddressLatch;
        private int _StartCycleCount;
        private int _LastCycleCount;
        private bool _RegisterModified;

        // 6845...
        private bool _DisplayEnabled;
        private byte _CtrlR0_HorizontalTotal;
        private byte _CtrlR1_HorizontalDisplayed;
        private byte _CtrlR4_VerticalTotal;
        private byte _CtrlR6_VerticalDisplayed;
        private byte _CtrlR8_InterlaceAndDelay;
        private byte _CtrlR9_ScanLinesPerChar;
        private byte _CtrlR12_ScreenStartHigh;
        private byte _CtrlR13_ScreenStartLow;
        private byte _CtrlWriteRegister;
        private int _ColumnCounter;
        private int _RowCounter;
        private int _ScanlineCounter;
        private int _ScanlineCounterIncrement;
        private int _ScanlineCounterReset;
        private int _ScreenSize;
        private int _ScreenAddress;
        private int _ScreenStartAddress;
        private int _FrameCount;

        // ULA...
        private byte _ULARegister;
        private byte[] _ULAPalette = [];
        private int _BitsPerPixel;
        private int _CharacterClockShift; // 0 for modes 0..3, 1 for modes 4..7
        private bool _TeletextMode;
        private bool _BlankSpace;
        private int _ScanlinesPerCharAdjust;
        private static ColorPalette _ColorPalette;

        // CRT bitmap...
        private const int _CRTMaxBitmapWidth = 640;
        private const int _CRTMaxBitmapHeight = 512;
        private const int _CRTBitmapStride = 640 / 2;
        private const float _CRTAspectRatio_Teletext = 0.813f;
        private const float _CRTAspectRatio_NonTeletext = 0.46f;

        private int _CRTBitmapWidth;
        private int _CRTBitmapHeight;
        private int _CRTBitmapScanlineOffset;
        private int _CRTBitmapScanlineOffsetIncrement;
        private int _CRTBitmapScanlineOffsetReset;
        private byte[] _CRTBitmap = [];
        private byte[][] _CRTBitmapTbl = new byte[256][];

        // mode 7...
        private uint[][] _Mode7TextFont = new uint[96][];
        private uint[][] _Mode7GraphicFont = new uint[96][];
        private uint[][] _Mode7MosaicFont = new uint[96][];

        private enum Mode7CharacterHeight
        {
            Normal = 0,
            Double_Upper = 1,
            Double_Lower = 2
        }

        private struct Mode7Character
        {
            public byte ForeColor;
            public byte BackColor;
            public uint[] FontBitmap;
            public Mode7CharacterHeight Height;
        }

        private struct Mode7State
        {
            public Mode7State()
            {
                Mode7FlashOn = true;
                LineChars = new Mode7Character[40];
            }

            public byte ForeColor;
            public byte ForeColorPending;
            public byte BackColor;
            public bool Flash;
            public bool NextFlash;
            public bool DoubleHeight;
            public bool Graphics;
            public bool NextGraphics;
            public bool Mosaic;
            public bool HoldGraphics;
            public bool NextHoldGraphics;
            public byte HoldGraphicsChar;
            public byte NextHoldGraphicsChar;
            public bool HoldMosaic;
            public bool NextHoldMosaic;
            public bool Mode7FlashOn;
            public int Mode7FlashTrigger;
            public Mode7Character[] LineChars;
        }

        private Mode7State _Mode7State = new();
        private const int Mode7_FlashOffFrameCount = 13;
        private const int Mode7_FlashOnFrameCount = 37;
    }
}