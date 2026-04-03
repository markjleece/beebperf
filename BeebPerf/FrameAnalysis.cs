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

//
// Acknowlegment: Video related code is derived from the BeebEm project,
// specifically portions of the Video class. Uses the same SAA5050 font
// loading and font file, and SAA5050 state machine logic.
//
// The code generates a list of analysis frames, which captures
// how long each game-loop iteration takes, and a list of display
// frames, which captures the displayed CRT frames.
//
// Analysis frames also capture the number of screen writes
// that occur before and after the screen memory is read for display,
// and their offset (cycles) from the next display frame.
// 
// The video code emulates the 6845, ULA, and SAA5050 at a
// character/address level.
//
// The code renders frame bitmaps in 4bpp indexed format, where
// the palette is set to match the BBC Micro's 16-color palette.
//
// Supports:
// - Standard modes 0-6, and teletext mode 7
// - Non-standard video modes (e.g. Mode 8)
// - Mixed non-teletext modes (mid-frame ULA changes)
// - Interlaced support
// - Over-scan resolutions up to 1024x640
// - Hardware scrolling
// 
// Limitations:
// - No support for shadow RAM reads
// - No support for hardware cursor
// - No support for non-interlaced teletext modes
// - No support for mixed teletext and non-teletext modes
// - No support for graphics tablets
// - No support for custom ULA palettes
//

using BeebPerf.model;
using System.Diagnostics;
using System.Drawing.Imaging;

namespace BeebPerf
{
    public class FrameAnalysis
    {
        private delegate byte ReadScreenData();
        private delegate void WriteBitmapData(byte value);

        static FrameAnalysis()
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

        public FrameAnalysis()
        {
            _WriteBitmapDataFunc = WriteBitmapData_Void;
            _ReadScreenDataFunc = ReadScreenData_Void;

            for (int i = 0; i < 256; i++)
                _CRTBitmapTbl[i] = new byte[8];

            ConstructMode7Fonts(GetSolutionFolder() + "\\teletext.fnt");
        }

        public async Task<bool> AnalysisAsync(
            Instruction[] instructions,
            InstructionSet instructionSet,
            Model model,
            FrameSettings? frameSettings,
            model.StackFrame rootStackFrame)
        {
            return await Task.Run(() =>
            {
                return Analysis(instructions, instructionSet, model, frameSettings, rootStackFrame);
            });
        }

        private bool Analysis(
            Instruction[] instructions,
            InstructionSet instructionSet,
            Model model,
            FrameSettings? frameSettings,
            model.StackFrame? rootStackFrame)
        {
            _RootStackFrame = rootStackFrame;
            _Instructions = instructions;
            _InstructionSet = instructionSet;
            _BBCModel = model.BBCModel;

            InitializeVideo(model);
            InitializeFrames(frameSettings);

            // process instructions whilst emulating 6845, ULA, and SAA5050 behavior to generate
            // display frames whilst also creating analysis frames based on the provided frame settings
            DisplayFrames = [];
            int cycleCount = 0;
            for (int instructionIndex = 0; instructionIndex < instructions.Length; instructionIndex++)
            {
                ref var instruction = ref instructions[instructionIndex];
                int postCycleCount = cycleCount + instruction.CycleCount;

                if (instructionIndex == _FrameStartInstructionIndex)
                    StartAnalysisFrame(cycleCount, instructionIndex);
                if (instructionIndex == _FrameEndInstructionIndex)
                    EndAnalysisFrame(postCycleCount, instructionIndex);

                if (instruction.IsInstruction)
                {
                    byte opcode = instruction.Opcode;
                    var memoryAccess = instructionSet.MemoryAccess(opcode);
                    if ((memoryAccess & InstructionSet.MemoryAccessType.Write) != 0)
                    {
                        MemoryWrite(instruction.MemoryAddress, instruction.MemoryWriteValue);
                        if (_RegisterModified)
                            UpdateVideoState();
                    }
                }
                else if (instruction.IsBeginDisplayEvent)
                {
                    StartDisplayFrame(cycleCount);
                }

                cycleCount = postCycleCount;

                if (_DisplayFrame)
                    DisplayMemory(cycleCount);
            }

            UpdateFrames();

            return true;
        }

        private void InitializeVideo(Model model)
        {
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

            _Memory = new byte[32768];
            Buffer.BlockCopy(model.Snapshot.Memory[(int)MemoryPage.WholeRam], 0, _Memory, 0, 32768);

            _ShadowRam = new byte[20480];
            if (model.Snapshot.Memory[(int)MemoryPage.ShadowRam] != null)
                Buffer.BlockCopy(model.Snapshot.Memory[(int)MemoryPage.ShadowRam], 0, _ShadowRam, 0, 20480);

            _FilingSystemRam = new byte[8192];
            if (model.Snapshot.Memory[(int)MemoryPage.FilingSystemRam] != null)
                Buffer.BlockCopy(model.Snapshot.Memory[(int)MemoryPage.FilingSystemRam], 0, _FilingSystemRam, 0, 8192);

            _ScreenWrapAddress = model.Snapshot.ScreenWrapAddress;
            _ScreenWrapOffset = 0x8000 - _ScreenWrapAddress;

            SetDisplayShadowRam(model.Snapshot.AccessControlRegister);

            _DisplayFrame = false;
            _DisplayFrameCount = 1;
            _CRTBitmap = new byte[_CRTMaxBufferHeight * _CRTBitmapStride];

            UpdateVideoState();
        }

        private void InitializeFrames(FrameSettings? frameSettings)
        {
            Frames = [];
            _FrameCount = 1;

            if (frameSettings != null && !frameSettings.Match(_Instructions))
                frameSettings = null;

            _FrameSettings = frameSettings;

            _FrameStartInstructionIndex = (frameSettings != null) ? FindInstructionIndex(indexFrom: 0, frameSettings.StartAddress) : -1;
            _FrameEndInstructionIndex = -1;

            _MemoryDisplayFrame = new int[32768];
            _ShadowRamDisplayFrame = new int[20480];
            _FilingSystemRamDisplayFrame = new int[8192];
        }

        private void UpdateFrames()
        {
            int displayFrameIndex = 0;
            DisplayFrame? displayFrame = DisplayFrames.Count > 0 ? DisplayFrames[displayFrameIndex] : null;

            for (int frameIndex = 0; frameIndex < Frames.Count; frameIndex++)
            {
                var frame = Frames[frameIndex];
                var displayFrameSpans = new List<Frame.DisplayFrameSpan>();
                int nextFrameStartCycleCount = -1;

                // skip any prior display frames 
                while (displayFrame != null && displayFrame.EndCycleCount < frame.StartCycleCount)
                {
                    displayFrame = displayFrameIndex < DisplayFrames.Count - 1 ? DisplayFrames[++displayFrameIndex] : null;
                }

                // process intersecting frames
                while (displayFrame != null && displayFrame.StartCycleCount < frame.EndCycleCount)
                {
                    if (nextFrameStartCycleCount == -1)
                        nextFrameStartCycleCount = displayFrame.StartCycleCount;

                    displayFrameSpans.Add(new Frame.DisplayFrameSpan()
                    {
                        StartCycleCount = displayFrame.StartCycleCount,
                        EndCycleCount = displayFrame.EndCycleCount
                    });

                    displayFrame = displayFrameIndex < DisplayFrames.Count - 1 ? DisplayFrames[++displayFrameIndex] : null;
                }

                if (displayFrame != null)
                {
                    if (nextFrameStartCycleCount == -1)
                        nextFrameStartCycleCount = displayFrame.StartCycleCount;

                    // back up to deal with cross spans
                    displayFrame = DisplayFrames[--displayFrameIndex];
                }

                frame.DisplayFrameSpans = displayFrameSpans.ToArray();

                if (nextFrameStartCycleCount != 0)
                    frame.DisplayFrameOffset = nextFrameStartCycleCount - frame.StartCycleCount;
                else
                    frame.DisplayFrameOffset = 0;
            }
        }

        private void StartAnalysisFrame(int cycleCount, int instructionIndex)
        {
            _FrameStartCycleCount = cycleCount;

            // find end instruction index based on frame settings
            _FrameEndInstructionIndex = -1;
            switch (_FrameSettings!.Type)
            {
                case FrameSettings.FrameType.StartAndEndAddresses:
                    _FrameEndInstructionIndex = FindInstructionIndex(instructionIndex, _FrameSettings.EndAddress);
                    break;

                case FrameSettings.FrameType.RoutineAddress:
                    var stackFrame = FindStackFrame(instructionIndex);
                    var lastInstructionIndex = stackFrame.GetLastInstructionStackFrame().LastInstructionIndex;
                    _FrameEndInstructionIndex = GetNearestInstructionIndex(lastInstructionIndex);
                    break;

                case FrameSettings.FrameType.JSRAddress:
                    var destinationAddress = _Instructions[instructionIndex].DestinationAddress;
                    stackFrame = FindStackFrame(instructionIndex);
                    foreach (var childStackFrame in stackFrame.Children)
                    {
                        if (_Instructions[childStackFrame.FirstInstructionIndex].OpcodeAddress.Equals(destinationAddress))
                        {
                            lastInstructionIndex = childStackFrame.GetLastInstructionStackFrame().LastInstructionIndex;
                            _FrameEndInstructionIndex = GetNearestInstructionIndex(lastInstructionIndex);
                            break;
                        }
                    }
                    Debug.Assert(_FrameEndInstructionIndex >= 0);
                    break;
            }

            // reset counts
            _WritesBeforeDisplayRead = 0;
            _WritesAfterDisplayRead = 0;
        }

        private void EndAnalysisFrame(int cycleCount, int instructionIndex)
        {
            // create display frame
            Frames.Add(new FrameAnalysis.Frame()
            {
                FrameNumber = _FrameCount++,
                StartCycleCount = _FrameStartCycleCount,
                EndCycleCount = cycleCount,
                WritesBeforeDisplayRead = _WritesBeforeDisplayRead,
                WritesAfterDisplayRead = _WritesAfterDisplayRead,
            });

            _FrameStartInstructionIndex = FindInstructionIndex(indexFrom: instructionIndex, _FrameSettings!.StartAddress);
            _FrameEndInstructionIndex = -1;
        }

        private int FindInstructionIndex(int indexFrom, CanonicalAddress address)
        {
            for (int instructionIndex = indexFrom; instructionIndex < _Instructions.Length; instructionIndex++)
            {
                ref var instruction = ref _Instructions[instructionIndex];
                if (instruction.IsInstruction && instruction.OpcodeAddress.Equals(address))
                    return instructionIndex;
            }
            return -1;
        }

        private model.StackFrame FindStackFrame(int instructionIndex)
        {
            Debug.Assert(_RootStackFrame != null);
            var result = FindStackFrame(_RootStackFrame!, instructionIndex);
            return result!;
        }

        private static model.StackFrame? FindStackFrame(model.StackFrame stackFrame, int instructionIndex)
        {
            // bounds check to avoid unnecessary recursion
            if (instructionIndex < stackFrame.GetFirstInstructionStackFrame().FirstInstructionIndex &&
                instructionIndex > stackFrame.GetLastInstructionStackFrame().LastInstructionIndex)
                return null;

            // check children first, as they will be more specific than the parent stack frame
            foreach (var childStackFrame in stackFrame.Children)
            {
                var result = FindStackFrame(childStackFrame, instructionIndex);
                if (result != null)
                    return result;
            }

            // check stack frame itself
            if (instructionIndex >= stackFrame.FirstInstructionIndex &&
                instructionIndex <= stackFrame.LastInstructionIndex)
                return stackFrame;

            return null;
        }

        private int GetNearestInstructionIndex(int index)
        {
            while (index < _Instructions.Length)
                if (_Instructions[index++].IsInstruction)
                    return index;

            index = _Instructions.Length - 1;

            while (index > 0)
                if (_Instructions[index--].IsInstruction)
                    return index;

            return 0;
        }

        private void UpdateVideoState()
        {
            _RegisterModified = false;

            int characterClockRate = (_ULARegister >> 4) & 0x01;
            _CharacterClockShift = 1 - characterClockRate;

            bool interlacedSync = (_CtrlR8_InterlaceAndDelay & 0x1) != 0;
            _CRTBitmapScanlineOffsetIncrement = (interlacedSync ? 2 : 1) * _CRTBitmapStride;
            _CRTBitmapScanlineOffsetReset = (interlacedSync ? _DisplayFrameCount % 2 : 0) * _CRTBitmapStride;

            bool interlacedVideo = (_CtrlR8_InterlaceAndDelay & 0x2) != 0;
            _ScanlineCounterIncrement = interlacedVideo ? 2 : 1;
            _ScanlineCounterReset = interlacedVideo ? _DisplayFrameCount % 2 : 0;

            _ScanlinesPerCharAdjust = (interlacedSync && interlacedVideo) ? 2 : 1;

            _TeletextMode = ((_ULARegister >> 1) & 0x01) != 0;

            if ((_TeletextMode && (!interlacedSync || !interlacedVideo)) ||
                (!_TeletextMode && interlacedVideo))
            {
                _DisplayFrame = false; // unsupported interlaced mode!
            }

            if (_TeletextMode)
            {
                _ScreenStartAddressDiv8 = 0; // not used in teletext mode
                _ScreenStartAddress = (((_CtrlR12_ScreenStartHigh ^ 0x20) + 0x74) << 8) + _CtrlR13_ScreenStartLow;
                _ScreenSize = _CtrlR6_VerticalDisplayed * _CtrlR1_HorizontalDisplayed;
            }
            else
            {
                _ScreenStartAddressDiv8 = (_CtrlR12_ScreenStartHigh << 8) + _CtrlR13_ScreenStartLow;
                _ScreenStartAddress = _ScreenStartAddressDiv8 * 8;
                _ScreenSize = _CtrlR6_VerticalDisplayed * _CtrlR1_HorizontalDisplayed * 8;
            }

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
                        throw new NotImplementedException();
                }

                bitmapWidth = horizontalMultiplier * (_CtrlR1_HorizontalDisplayed * 8);
                bitmapHeight = _CtrlR6_VerticalDisplayed * (_CtrlR9_ScanLinesPerChar + _ScanlinesPerCharAdjust);

                _CRTAspectRatio = _CRTAspectRatio_NonTeletext;

                if (interlacedSync)
                {
                    bitmapHeight *= 2;
                    _CRTAspectRatio *= 2.0f;
                }

                if (bitmapWidth > _CRTMaxBitmapWidth || bitmapHeight > _CRTMaxBufferHeight)
                    _DisplayFrame = false; // unsupported over-scan!
            }
            else
            {
                // Mode 7 (teletext)
                _WriteBitmapDataFunc = WriteBitmapData_Mode7;
                
                bitmapWidth = 480;
                bitmapHeight = 500;

                _CRTAspectRatio = _CRTAspectRatio_Teletext;
            }

            if (_CRTBitmapWidth < bitmapWidth)
                _CRTBitmapWidth = bitmapWidth;

            if (_CRTBitmapHeight < bitmapHeight)
                _CRTBitmapHeight = bitmapHeight;
        }

        private void StartDisplayFrame(int cycleCount)
        {
            if (_DisplayFrame)
                EndDisplayFrame(cycleCount); // shouldn't happen

            _DisplayFrame = true;

            _CRTBitmapWidth = 0;
            _CRTBitmapHeight = 0;

            _StartCycleCount = cycleCount;
            _LastCycleCount = cycleCount;

            _BlankSpace = false;
            _RowCounter = 0;
            _ColumnCounter = 0;

            _Mode7State.DoubleHeightRow = Mode7DoubleHeightRow.Top;
            _Mode7State.LastRowCounter = -1;

            if (--_Mode7State.FlashTrigger < 0)
            {
                _Mode7State.FlashTrigger = _Mode7State.FlashOn ? _Mode7FlashOffFrameCount : _Mode7FlashOnFrameCount;
                _Mode7State.FlashOn = !_Mode7State.FlashOn; // toggle flash state
            }

            UpdateVideoState();

            _ScanlineCounter = _ScanlineCounterReset;
            _CRTBitmapScanlineOffset = _CRTBitmapScanlineOffsetReset;
        }

        private void EndDisplayFrame(int cycleCount)
        {
            Debug.Assert(cycleCount - _StartCycleCount < 480000);

            Bitmap bitmap = new Bitmap(_CRTBitmapWidth, _CRTBitmapHeight, PixelFormat.Format4bppIndexed);
            bitmap.Palette = _ColorPalette;

            if (_DisplayFrame)
            {
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
                            for (int i = 0; i < _CRTBitmapHeight; i++)
                            {
                                Buffer.MemoryCopy(
                                    src + (i * _CRTBitmapStride),
                                    dst + (i * data.Stride),
                                    data.Stride,
                                    data.Stride);
                            }
                        }
                    }
                }
                finally
                {
                    if (data != null)
                        bitmap.UnlockBits(data);
                }
            }

            DisplayFrames.Add(new()
            {
                AspectRatio = _CRTAspectRatio,
                FrameNumber = _DisplayFrameCount++,
                StartCycleCount = _StartCycleCount,
                EndCycleCount = cycleCount,
                Bitmap = bitmap
            });

            _DisplayFrame = false;
        }

        private bool IsScreenAddress(ushort address)
        {
            int screenEndAddress = _ScreenStartAddress + _ScreenSize;
            int screenWrapSize = screenEndAddress - 0x8000;

            if (screenWrapSize <= 0)
                return (address >= _ScreenStartAddress && address < screenEndAddress);
            else
                return ((address >= _ScreenStartAddress && address < 0x8000) ||
                        (address >= _ScreenWrapAddress && address < _ScreenWrapAddress + screenWrapSize));
        }

        private void MemoryWrite(CanonicalAddress memoryAddress, byte value)
        {
            ushort address = memoryAddress.Address;

            if (address < 0xE000)
            {
                int displayFrame = -1;

                if (memoryAddress.Page == MemoryPage.WholeRam && address < 0x8000)
                {
                    _Memory[address] = value;

                    if (IsScreenAddress(address))
                        displayFrame = _MemoryDisplayFrame[address];
                }
                else if (memoryAddress.Page == MemoryPage.ShadowRam)
                {
                    _ShadowRam[address - 0x3000] = value;

                    if (IsScreenAddress(address))
                        displayFrame = _ShadowRamDisplayFrame[address - 0x3000];
                }
                else if (memoryAddress.Page == MemoryPage.FilingSystemRam)
                {
                    _FilingSystemRam[address - 0xC000] = value;

                    if (IsScreenAddress(address))
                        displayFrame = _FilingSystemRamDisplayFrame[address - 0xC000];
                }

                if (displayFrame != -1)
                {
                    if (displayFrame < _DisplayFrameCount)
                        _WritesBeforeDisplayRead++;
                    else
                        _WritesAfterDisplayRead++;
                }
            }

            if (memoryAddress.Page != MemoryPage.WholeRam)
                return;

            if (address == 0xFE00)
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
            else if (address == 0xFE40) // set screen wrap address
            {
                if ((_SystemVIA_DataDirection & 0xF) == 0xF)
                {
                    int latchIndex = (value & 0x07);
                    int latchValue = (value >> 3) & 0x01;
                    if (latchIndex == 4)
                        _ScreenWrapAddressLatch = (byte)((_ScreenWrapAddressLatch & 0xFE) | latchValue);
                    else if (latchIndex == 5)
                        _ScreenWrapAddressLatch = (byte)((_ScreenWrapAddressLatch & 0xFD) | latchValue);

                    _ScreenWrapAddress = _ScreenWrapAddressLatch switch
                    {
                        0 => 0x4000,
                        1 => 0x6000,
                        2 => 0x3000,
                        3 => 0x5800,
                        _ => throw new ArgumentOutOfRangeException()
                    };
                    _ScreenWrapOffset = 0x8000 - _ScreenWrapAddress;
                }
            }

            if (address >= 0xFE34 && address < 0xFE38)
            {
                SetDisplayShadowRam(value);
            }
        }

        private void SetDisplayShadowRam(byte accessControlRegister)
        {
            switch (_BBCModel)
            {
                case BBCModelType.BPlus:
                case BBCModelType.IntegraB:
                    _DisplayShadowRam = (accessControlRegister & 0x80) != 0;
                    break;

                case BBCModelType.Master128:
                case BBCModelType.MasterET:
                    _DisplayShadowRam = (accessControlRegister & 0x01) != 0;
                    break;

                default:
                    _DisplayShadowRam = false;
                    break;
            }
        }

        private void DisplayMemory(int cycleCount)
        {
            int characterCount = (cycleCount >> _CharacterClockShift) - (_LastCycleCount >> _CharacterClockShift);

            _LastCycleCount = cycleCount;

            for (int i = 0; i < characterCount; i++)
            {
                // read screen memory and rasterize it to CRT bitmap, if not in blanking period
                if (!_BlankSpace)
                    _WriteBitmapDataFunc(_ReadScreenDataFunc());

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
                            EndDisplayFrame(cycleCount);
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

            if (characterAddress >= 0x8000)
                characterAddress -= _ScreenWrapOffset;

            return ReadDisplayMemory(characterAddress);
        }

        private byte ReadScreenData_NonTeletext()
        {
            if (_ScanlineCounter >= 8)
                return 0;

            int characterAddress = _ScreenStartAddressDiv8 + (_RowCounter * _CtrlR1_HorizontalDisplayed) + _ColumnCounter;
            int memoryAddress = (characterAddress << 3) + _ScanlineCounter;

            if (memoryAddress >= 0x8000)
                memoryAddress -= _ScreenWrapOffset;

            return ReadDisplayMemory(memoryAddress);
        }

        private byte ReadDisplayMemory(int address)
        {
            if (_DisplayShadowRam)
            {
                if (address < 0x3000)
                {
                    if (_BBCModel == BBCModelType.Master128 || _BBCModel == BBCModelType.MasterET)
                    {
                        address -= 0xC000;
                        _FilingSystemRamDisplayFrame[address] = _DisplayFrameCount;
                        return _FilingSystemRam[address];
                    }
                    else
                    {
                        _MemoryDisplayFrame[address] = _DisplayFrameCount;
                        return _Memory[address];
                    }
                }
                else
                {
                    address -= 0x3000;
                    _ShadowRamDisplayFrame[address] = _DisplayFrameCount;
                    return _ShadowRam[address];
                }
            }
            else
            {
                _MemoryDisplayFrame[address] = _DisplayFrameCount;
                return _Memory[address];
            }
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

        private void WriteBitmapData_Mode7(byte value)
        {
            if (_ColumnCounter >= 40)
                return;

            // update double-height row state
            if (_RowCounter != _Mode7State.LastRowCounter)
            {
                if (_RowCounter == 0)
                {
                    _Mode7State.DoubleHeightRow = Mode7DoubleHeightRow.Top;
                }
                else if (_Mode7State.DoubleHeight)
                {
                    _Mode7State.DoubleHeightRow =
                        _Mode7State.DoubleHeightRow == Mode7DoubleHeightRow.Top
                        ? Mode7DoubleHeightRow.Bottom
                        : Mode7DoubleHeightRow.Top;
                }

                _Mode7State.LastRowCounter = _RowCounter;
            }

            // reset state at start of scanline
            if (_ColumnCounter == 0)
            {
                _Mode7State.ForeColor = 7;
                _Mode7State.BackColor = 0;
                _Mode7State.ForeColorPending = 7;
                _Mode7State.DoubleHeight = false;
                _Mode7State.NextHoldGraphics = false;
                _Mode7State.NextHoldGraphicsChar = 32; // space
                _Mode7State.NextHoldMosaic = false;
                _Mode7State.NextGraphics = false;
                _Mode7State.NextFlash = false;
                _Mode7State.Mosaic = false;
            }

            // apply pending state changes
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

            uint[,] fontBitmap;

            if (value >= 128 && value <= 159)
            {
                // control codes...
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
                            _Mode7State.NextHoldGraphicsChar = 32; // space
                            _Mode7State.HoldGraphicsChar = _Mode7State.NextHoldGraphicsChar;
                        }
                        _Mode7State.DoubleHeight = false;
                        break;

                    case 141: // double height
                        if (!_Mode7State.DoubleHeight)
                        {
                            _Mode7State.NextHoldGraphicsChar = 32; // space
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
                    value = _Mode7State.HoldGraphicsChar;
                    fontBitmap = _Mode7State.HoldMosaic ? _Mode7MosaicFont : _Mode7GraphicFont;
                }
                else
                {
                    value = 32; // space
                    fontBitmap = _Mode7State.Graphics ? (_Mode7State.Mosaic ? _Mode7MosaicFont : _Mode7GraphicFont) : _Mode7TextFont;
                }
            }
            else
            {
                fontBitmap = _Mode7State.Graphics ? (_Mode7State.Mosaic ? _Mode7MosaicFont : _Mode7GraphicFont) : _Mode7TextFont;
            }

            value &= 0x7F; // clear top bit

            // determine foreground color (considering flash state)
            byte foreColor;
            if (_Mode7State.Flash && !_Mode7State.FlashOn)
                foreColor = _Mode7State.BackColor;
            else
                foreColor = _Mode7State.ForeColor;

            // determine scanline index
            int scanlineIndex;
            if (_Mode7State.DoubleHeight)
                scanlineIndex = (_ScanlineCounter >> 1) + (_Mode7State.DoubleHeightRow == Mode7DoubleHeightRow.Top ? 0 : 10);
            else
                scanlineIndex = _ScanlineCounter;

            // fetch font scanline
            int valueAsIndex = Math.Max(value - 32, 0);
            uint fontScanline = fontBitmap[valueAsIndex, scanlineIndex];

            // render 12 pixels (6 bytes)
            int index = _CRTBitmapScanlineOffset + (_ColumnCounter * 6);
            uint pixelMask = 0x800;
            while (pixelMask != 0)
            {
                uint hiColor = ((fontScanline & pixelMask) != 0) ? foreColor : _Mode7State.BackColor;
                pixelMask >>= 1;

                uint loColor = ((fontScanline & pixelMask) != 0) ? foreColor : _Mode7State.BackColor;
                pixelMask >>= 1;

                _CRTBitmap[index++] = (byte)((hiColor << 4) | loColor);
            }

            // commit pending foreground color
            _Mode7State.ForeColor = _Mode7State.ForeColorPending;
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

        private bool ConstructMode7Fonts(string fileName)
        {
            try
            {
                using var fs = File.OpenRead(fileName);

                for (int ch = 0; ch < 96; ch++)
                {
                    for (int i = 0; i < 2; i++)
                        _Mode7TextFont[ch, i] = 0;

                    for (int i = 2; i < 20; i++)
                    {
                        int loByte = fs.ReadByte();
                        if (loByte == -1)
                            throw new EndOfStreamException();

                        int hiByte = fs.ReadByte();
                        if (hiByte == -1)
                            throw new EndOfStreamException();

                        _Mode7TextFont[ch, i] = (uint)((hiByte << 8) | loByte);
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
                        _Mode7GraphicFont[ch, i] = top;

                    for (int i = 6; i < 14; i++)
                        _Mode7GraphicFont[ch, i] = middle;

                    for (int i = 14; i < 20; i++)
                        _Mode7GraphicFont[ch, i] = bottom;

                    // mosaic fonts
                    top &= 0x03CF;
                    middle &= 0x03CF;
                    bottom &= 0x03CF;

                    for (int i = 0; i < 4; i++)
                        _Mode7MosaicFont[ch, i] = top;

                    for (int i = 4; i < 6; i++)
                        _Mode7MosaicFont[ch, i] = 0;

                    for (int i = 6; i < 12; i++)
                        _Mode7MosaicFont[ch, i] = middle;

                    for (int i = 12; i < 14; i++)
                        _Mode7MosaicFont[ch, i] = 0;

                    for (int i = 14; i < 18; i++)
                        _Mode7MosaicFont[ch, i] = bottom;

                    for (int i = 18; i < 20; i++)
                        _Mode7MosaicFont[ch, i] = 0;
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

        public class Frame
        {
            public struct DisplayFrameSpan
            {
                public int StartCycleCount;
                public int EndCycleCount;
            }

            public required int FrameNumber; // 1, 2, 3...
            public required int StartCycleCount;
            public required int EndCycleCount;
            public required int WritesBeforeDisplayRead;
            public required int WritesAfterDisplayRead;
            public int DisplayFrameOffset;
            public DisplayFrameSpan[] DisplayFrameSpans = [];
        }

        public class DisplayFrame
        {
            public int FrameNumber; // 1, 2, 3...
            public int StartCycleCount;
            public int EndCycleCount;
            public Bitmap? Bitmap;
            public float AspectRatio;
        }

        public List<Frame> Frames = [];
        public List<DisplayFrame> DisplayFrames = [];

        private model.StackFrame? _RootStackFrame = null;
        private Instruction[] _Instructions = [];
        private InstructionSet? _InstructionSet;

        private WriteBitmapData _WriteBitmapDataFunc;
        private ReadScreenData _ReadScreenDataFunc;
        private BBCModelType _BBCModel;
        private byte[] _Memory = [];
        private byte[] _ShadowRam = [];
        private byte[] _FilingSystemRam = [];

        private int[] _MemoryDisplayFrame = [];
        private int[] _ShadowRamDisplayFrame = [];
        private int[] _FilingSystemRamDisplayFrame = [];

        private byte _SystemVIA_DataDirection;
        private byte _ScreenWrapAddressLatch;
        private int _StartCycleCount;
        private int _LastCycleCount;
        private bool _RegisterModified;

        // analysis frames
        private FrameSettings? _FrameSettings;
        private int _WritesBeforeDisplayRead;
        private int _WritesAfterDisplayRead;
        private int _FrameStartInstructionIndex;
        private int _FrameEndInstructionIndex;
        private int _FrameStartCycleCount;
        private int _FrameCount = 0;

        // 6845...
        private bool _DisplayFrame;
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
        private int _ScreenWrapAddress;
        private int _ScreenWrapOffset;
        private int _ScreenSize;
        private int _ScreenStartAddressDiv8;
        private int _ScreenStartAddress;
        private int _DisplayFrameCount;

        // ULA...
        private byte _ULARegister;
        private byte[] _ULAPalette = [];
        private int _BitsPerPixel;
        private int _CharacterClockShift; // 0 for modes 0..3, 1 for modes 4..7
        private int _ScanlinesPerCharAdjust;
        private bool _TeletextMode;
        private bool _BlankSpace;
        private bool _DisplayShadowRam;
        private static ColorPalette _ColorPalette;

        // CRT...
        private const int _CRTMaxBitmapWidth = 1024;
        private const int _CRTMaxBufferHeight = 640;
        private const int _CRTBitmapStride = _CRTMaxBitmapWidth / 2;
        private const float _CRTAspectRatio_Teletext = 0.813f;
        private const float _CRTAspectRatio_NonTeletext = 0.46f;

        private int _CRTBitmapWidth;
        private int _CRTBitmapHeight;
        private int _CRTBitmapScanlineOffset;
        private int _CRTBitmapScanlineOffsetIncrement;
        private int _CRTBitmapScanlineOffsetReset;
        private byte[] _CRTBitmap = [];
        private byte[][] _CRTBitmapTbl = new byte[256][];
        private float _CRTAspectRatio;

        // mode 7...
        private uint[,] _Mode7TextFont = new uint[96, 20];
        private uint[,] _Mode7GraphicFont = new uint[96, 20];
        private uint[,] _Mode7MosaicFont = new uint[96, 20];

        private enum Mode7DoubleHeightRow
        {
            Top = 0,
            Bottom = 1
        }

        private class Mode7State
        {
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
            public bool FlashOn;
            public int FlashTrigger;
            public int LastRowCounter;
            public Mode7DoubleHeightRow DoubleHeightRow;
        }

        private Mode7State _Mode7State = new();
        private const int _Mode7FlashOffFrameCount = 13;
        private const int _Mode7FlashOnFrameCount = 37;
    }
}