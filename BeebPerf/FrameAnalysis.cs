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
// acknowledgement: Video related code is derived from the BeebEm project,
// specifically portions of the Video class.
//

using BeebPerf.model;
using System.Diagnostics;
using System.Drawing.Imaging;

namespace BeebPerf
{
    //
    // Frame analysis generates a list of analysis frames, which captures
    // how long each game-loop iteration takes, and a list of display
    // frames, which captures the displayed CRT frames.
    //
    // Analysis frames also capture the number of screen writes
    // that occur before and after the screen memory is scanned for
    // display, and their offset (cycles) from the next display frame.
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
    // - Hardware scrolling and split screen support
    // 
    // Limitations:
    // - No support for the hardware cursor
    // - No support for custom teletext modes
    // - No support for graphics tablets
    // - No support for custom ULA hardware
    //
    public class FrameAnalysis
    {
        private delegate byte ReadScreenData();
        private delegate void WriteBitmapData(byte value);

        public FrameAnalysis()
        {
            ConstructMode7Fonts();
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

                if (instructionIndex == _AnalysisFrameState.EndInstructionIndex)
                    EndAnalysisFrame(postCycleCount, instructionIndex);

                if (instructionIndex == _AnalysisFrameState.StartInstructionIndex)
                    StartAnalysisFrame(cycleCount, instructionIndex);

                if (instruction.IsBeginDisplayEvent)
                {
                    StartDisplayFrame(cycleCount);
                }
                else if (instruction.IsInstruction)
                {
                    byte opcode = instruction.Opcode;
                    var memoryAccess = instructionSet.MemoryAccess(opcode);
                    if ((memoryAccess & InstructionSet.MemoryAccessType.Write) != 0)
                        MemoryWrite(instruction.MemoryAddress, instruction.MemoryWriteValue);
                }

                cycleCount = postCycleCount;

                if (_DisplayFrame)
                    DisplayMemory(cycleCount);
            }

            UpdateFrames();

            return true;
        }

        private void InitializeFrames(FrameSettings? frameSettings)
        {
            Frames = [];
            _AnalysisFrameState.FrameNumber = 1;

            if (frameSettings != null && !frameSettings.Match(_Instructions))
                frameSettings = null;

            _AnalysisFrameState.FrameSettings = frameSettings;
            _AnalysisFrameState.StartInstructionIndex = (frameSettings != null) ? FindInstructionIndex(indexFrom: 0, frameSettings.StartAddress) : -1;
            _AnalysisFrameState.EndInstructionIndex = -1;

            _ScreenMemory.MemoryDisplayFrame = new int[32768];
            _ScreenMemory.ShadowRamDisplayFrame = new int[20480];
            _ScreenMemory.FilingSystemRamDisplayFrame = new int[8192];
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
                bool first = true;
                while (displayFrame != null && (first || displayFrame.StartCycleCount < frame.EndCycleCount))
                {
                    first = false;
                    if (nextFrameStartCycleCount == -1)
                        nextFrameStartCycleCount = displayFrame.StartCycleCount;

                    displayFrameSpans.Add(new Frame.DisplayFrameSpan()
                    {
                        FrameNumber = displayFrame.FrameNumber,
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
                    if (displayFrameIndex > 0)
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
            // remember cycle count
            _AnalysisFrameState.StartCycleCount = cycleCount;

            // reset counts
            _AnalysisFrameState.WritesBeforeDisplayRead = 0;
            _AnalysisFrameState.WritesAfterDisplayRead = 0;

            // find end instruction index based on frame settings
            _AnalysisFrameState.EndInstructionIndex = -1;
            switch (_AnalysisFrameState.FrameSettings!.Type)
            {
                case FrameSettings.FrameType.StartAndEndAddresses:
                    _AnalysisFrameState.EndInstructionIndex = FindInstructionIndex(instructionIndex + 1, _AnalysisFrameState.FrameSettings.EndAddress);
                    break;

                case FrameSettings.FrameType.RoutineAddress:
                    var stackFrame = FindStackFrame(instructionIndex);
                    _AnalysisFrameState.EndInstructionIndex = stackFrame.LastInstructionIndex;
                    break;

                case FrameSettings.FrameType.JSRAddress:
                    var destinationAddress = _Instructions[instructionIndex].DestinationAddress;
                    var destinationInstructionIndex = FindInstructionIndex(instructionIndex + 1, destinationAddress);

                    stackFrame = FindStackFrame(destinationInstructionIndex);
                    _AnalysisFrameState.EndInstructionIndex = stackFrame.LastInstructionIndex;
                    break;
            }
        }

        private void EndAnalysisFrame(int cycleCount, int instructionIndex)
        {
            // create display frame
            Frames.Add(new FrameAnalysis.Frame()
            {
                FrameNumber = _AnalysisFrameState.FrameNumber++,
                StartCycleCount = _AnalysisFrameState.StartCycleCount,
                EndCycleCount = cycleCount,
                WritesBeforeDisplayRead = _AnalysisFrameState.WritesBeforeDisplayRead,
                WritesAfterDisplayRead = _AnalysisFrameState.WritesAfterDisplayRead,
            });

            // find next start instruction
            if (_AnalysisFrameState.FrameSettings!.Type == FrameSettings.FrameType.StartAndEndAddresses &&
                _AnalysisFrameState.FrameSettings.StartAddress.Equals(_AnalysisFrameState.FrameSettings.EndAddress))
                _AnalysisFrameState.StartInstructionIndex = instructionIndex;
            else
                _AnalysisFrameState.StartInstructionIndex = FindInstructionIndex(indexFrom: instructionIndex + 1, _AnalysisFrameState.FrameSettings!.StartAddress);

            _AnalysisFrameState.EndInstructionIndex = -1;
        }

        private int FindInstructionIndex(int indexFrom, CanonicalAddress address)
        {
            if (indexFrom < _Instructions.Length)
            {
                for (int instructionIndex = indexFrom; instructionIndex < _Instructions.Length; instructionIndex++)
                {
                    ref var instruction = ref _Instructions[instructionIndex];
                    if (instruction.IsInstruction && instruction.OpcodeAddress.Equals(address))
                        return instructionIndex;
                }
            }
            return -1;
        }

        private model.StackFrame FindStackFrame(int instructionIndex)
        {
            Debug.Assert(_RootStackFrame != null);

            var stack = new Stack<(model.StackFrame stackFrame, bool resume)>();
            stack.Push((_RootStackFrame, resume: false));

            while (stack.Count > 0)
            {
                var (stackFrame, resume) = stack.Pop();

                // first check the child stack frames
                if (stackFrame.Children.Count > 0 && !resume)
                {
                    stack.Push((stackFrame, resume: true));

                    foreach (var childStackFrame in stackFrame.Children) // order doesn't matter
                    {
                        // bounds check to avoid unnecessary processing
                        if (instructionIndex < childStackFrame.FirstInstructionIndex ||
                            instructionIndex > childStackFrame.LastInstructionIndex)
                            continue;

                        stack.Push((childStackFrame, resume: false));
                    }

                    continue;
                }

                // must be this stack frame!
                Debug.Assert(
                    instructionIndex >= stackFrame.FirstInstructionIndex &&
                    instructionIndex <= stackFrame.LastInstructionIndex);

                return stackFrame;
            }

            return _RootStackFrame!;
        }

        private void InitializeVideo(Model model)
        {
            _ULAState.Palette = model.Snapshot.VideoULAPalette.ToArray();
            _ULAState.Register = model.Snapshot.VideoULARegister;
            _ULAState.RegisterModified = true;
            _ULAState.PaletteModified = true;

            _CRTState.Register0_HorizontalTotal = model.Snapshot.VideoCtrlRegisters[0];
            _CRTState.Register1_HorizontalDisplayed = model.Snapshot.VideoCtrlRegisters[1];
            _CRTState.Register2_HorizontalSyncPos = model.Snapshot.VideoCtrlRegisters[2];
            _CRTState.Register3_SyncWidth = model.Snapshot.VideoCtrlRegisters[3];
            _CRTState.Register4_VerticalTotal = model.Snapshot.VideoCtrlRegisters[4];
            _CRTState.Register5_VerticalTotalAdjust = model.Snapshot.VideoCtrlRegisters[5];
            _CRTState.Register6_VerticalDisplayed = model.Snapshot.VideoCtrlRegisters[6];
            _CRTState.Register7_VerticalSyncPos = model.Snapshot.VideoCtrlRegisters[7];
            _CRTState.Register8_InterlaceAndDelay = model.Snapshot.VideoCtrlRegisters[8];
            _CRTState.Register9_ScanLinesPerChar = model.Snapshot.VideoCtrlRegisters[9];
            _CRTState.Register12_ScreenStartHigh_Latch = model.Snapshot.VideoCtrlRegisters[12];
            _CRTState.Register13_ScreenStartLow_Latch = model.Snapshot.VideoCtrlRegisters[13];

            _ScreenMemory.Memory = new byte[32768];
            Buffer.BlockCopy(model.Snapshot.Memory[(int)MemoryPage.WholeRam], 0, _ScreenMemory.Memory, 0, 32768);

            _ScreenMemory.ShadowRam = new byte[20480];
            if (model.Snapshot.Memory[(int)MemoryPage.ShadowRam] != null)
                Buffer.BlockCopy(model.Snapshot.Memory[(int)MemoryPage.ShadowRam], 0, _ScreenMemory.ShadowRam, 0, 20480);

            _ScreenMemory.FilingSystemRam = new byte[8192];
            if (model.Snapshot.Memory[(int)MemoryPage.FilingSystemRam] != null)
                Buffer.BlockCopy(model.Snapshot.Memory[(int)MemoryPage.FilingSystemRam], 0, _ScreenMemory.FilingSystemRam, 0, 8192);

            _ScreenMemory.WrapAddress = model.Snapshot.ScreenWrapAddress;
            _ScreenMemory.WrapOffset = 0x8000 - _ScreenMemory.WrapAddress;

            _PortBAddressableLatch = _ScreenMemory.WrapAddress switch
            {
                0x4000 => 0x00,
                0x6000 => 0x10,
                0x3000 => 0x20,
                0x5800 => 0x30,
                _ => 0x00
            };

            SetDisplayShadowRam(model.Snapshot.AccessControlRegister);

            _DisplayState.FrameNumber = 1;
            _DisplayState.Buffer = new byte[DisplayState.MaxHeight * DisplayState.BufferStride];

            _DisplayFrame = false;
        }

        private void StartDisplayFrame(int cycleCount)
        {
            if (_DisplayFrame)
                EndDisplayFrame();

            _DisplayFrame = true;

            _DisplayState.Width = 0;
            _DisplayState.StartCycleCount = cycleCount;
            _DisplayState.LastCycleCount = cycleCount;

            UpdateVideoState(newFrame: true);

            int verticalAdjustSize = _CRTState.Register5_VerticalTotalAdjust * _DisplayState.ScanlineOffsetIncrement;
            if (verticalAdjustSize > 0)
                Array.Clear(_DisplayState.Buffer, 0, verticalAdjustSize);

            _DisplayState.ScanlineOffset = _DisplayState.ScanlineOffsetReset + verticalAdjustSize;
            _DisplayState.ScanlineIndex = _CRTState.Register5_VerticalTotalAdjust;

            _Mode7State.DoubleHeightRow = Mode7DoubleHeightRow.Top;
            _Mode7State.LastRowCounter = -1;

            if (--_Mode7State.FlashTrigger < 0)
            {
                _Mode7State.FlashTrigger = _Mode7State.FlashOn
                    ? Mode7State.Mode7FlashOffFrameCount
                    : Mode7State.Mode7FlashOnFrameCount;
                _Mode7State.FlashOn = !_Mode7State.FlashOn; // toggle flash state
            }

            _CRTState.BlankSpace = false;
            _CRTState.CharacterRowCounter = 0;
            _CRTState.CharacterColumnCounter = 0;
        }

        private void EndDisplayFrame()
        {
            Debug.Assert(_DisplayState.EndCycleCount - _DisplayState.StartCycleCount < 480000);

            Bitmap bitmap = new Bitmap(_DisplayState.Width, _DisplayState.Height, PixelFormat.Format4bppIndexed);
            bitmap.Palette = DisplayState.BBCPalette;

            if (_DisplayFrame)
            {
                Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
                System.Drawing.Imaging.BitmapData? data = null;
                try
                {
                    data = bitmap.LockBits(rect, ImageLockMode.WriteOnly, bitmap.PixelFormat);
                    unsafe
                    {
                        fixed (byte* src = _DisplayState.Buffer)
                        {
                            byte* dst = (byte*)data.Scan0;
                            for (int i = 0; i < _DisplayState.Height; i++)
                            {
                                Buffer.MemoryCopy(
                                    src + (i * DisplayState.BufferStride),
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
                AspectRatio = _DisplayState.AspectRatio,
                FrameNumber = _DisplayState.FrameNumber++,
                StartCycleCount = _DisplayState.StartCycleCount,
                EndCycleCount = _DisplayState.EndCycleCount,
                Bitmap = bitmap
            });

            _DisplayFrame = false;
        }

        private void UpdateVideoState(bool newFrame)
        {
            bool interlacedSync = (_CRTState.Register8_InterlaceAndDelay & 0x1) != 0;
            bool interlacedVideo = (_CRTState.Register8_InterlaceAndDelay & 0x2) != 0;

            if (newFrame || _ULAState.RegisterModified)
            {
                _DisplayState.ScanlineIndexIncrement = interlacedSync ? 2 : 1;
                _DisplayState.ScanlineOffsetIncrement = (interlacedSync ? 2 : 1) * DisplayState.BufferStride;
                _DisplayState.ScanlineOffsetReset = (interlacedSync ? _DisplayState.FrameNumber % 2 : 0) * DisplayState.BufferStride;

                _CRTState.ScanlineCounterIncrement = interlacedVideo ? 2 : 1;
                _CRTState.ScanlineCounterReset = interlacedVideo ? _DisplayState.FrameNumber % 2 : 0;
                _CRTState.ScanlinesPerCharAdjust = (interlacedSync && interlacedVideo) ? 2 : 1;

                _TeletextMode = (_ULAState.Register & 0x2) != 0;

                if ((_TeletextMode && (!interlacedSync || !interlacedVideo)) ||
                    (!_TeletextMode && interlacedVideo))
                    _DisplayFrame = false; // unsupported interlaced mode!

                int characterClockRate = (_ULAState.Register >> 4) & 0x01;
                _CRTState.CharacterClockShift = 1 - characterClockRate;
            }

            if (newFrame)
            {
                _CRTState.CharacterRowCounter = 0;
                _CRTState.ScanlineCounter = _CRTState.ScanlineCounterReset;
                _CRTState.VerticalAdjustCounter = 0;
                _CRTState.BlankSpace = false;

                _CRTState.Register12_ScreenStartHigh = _CRTState.Register12_ScreenStartHigh_Latch;
                _CRTState.Register13_ScreenStartLow = _CRTState.Register13_ScreenStartLow_Latch;

                if (_TeletextMode)
                {
                    _ScreenMemory.StartAddressDiv8 = 0; // not used in teletext mode
                    _ScreenMemory.StartAddress = (((_CRTState.Register12_ScreenStartHigh ^ 0x20) + 0x74) << 8) + _CRTState.Register13_ScreenStartLow;
                    _ScreenMemory.Size = _CRTState.Register6_VerticalDisplayed * _CRTState.Register1_HorizontalDisplayed;
                }
                else
                {
                    _ScreenMemory.StartAddressDiv8 = (_CRTState.Register12_ScreenStartHigh << 8) + _CRTState.Register13_ScreenStartLow;
                    _ScreenMemory.StartAddress = _ScreenMemory.StartAddressDiv8 * 8;
                    _ScreenMemory.Size = _CRTState.Register6_VerticalDisplayed * _CRTState.Register1_HorizontalDisplayed * 8;
                }
            }

            if (_ULAState.RegisterModified || _ULAState.PaletteModified)
            {
                if (!_TeletextMode)
                {
                    _CRTState.ReadScreenDataFunc = ReadScreenData_NonTeletext;

                    switch ((_ULAState.Register >> 2) & 0x7)
                    {
                        case 7: // Mode 0 & 3 (2 colors, high res)
                            Build4bppLookupTbl(bitsPerPixel: 1);
                            _CRTState.WriteBitmapDataFunc = WriteBitmapData_32bits;
                            break;

                        case 6: // Mode 1 (4 colors, medium res)
                            Build8bppLookupTbl(bitsPerPixel: 2);
                            _CRTState.WriteBitmapDataFunc = WriteBitmapData_32bits;
                            break;

                        case 5: // Mode 2 (16 colors, low res)
                            Build16bppLookupTbl(bitsPerPixel: 4);
                            _CRTState.WriteBitmapDataFunc = WriteBitmapData_32bits;
                            break;

                        case 2: // Mode 4 & 6 (2 colors, medium res)
                            Build8bppLookupTbl(bitsPerPixel: 1);
                            _CRTState.WriteBitmapDataFunc = WriteBitmapData_64bits;
                            break;

                        case 1: // Mode 5 (4 colors, low res)
                            Build16bppLookupTbl(bitsPerPixel: 2);
                            _CRTState.WriteBitmapDataFunc = WriteBitmapData_64bits;
                            break;

                        case 0: // Mode 8 (16 colors, very low res)
                            Build32bppLookupTbl(bitsPerPixel: 4);
                            _CRTState.WriteBitmapDataFunc = WriteBitmapData_64bits;
                            break;

                        default:
                            throw new NotImplementedException();
                    }

                    float aspectRatio = DisplayState.AspectRatio_NonTeletext;
                    if (interlacedSync) aspectRatio *= 2.0f;
                    _DisplayState.AspectRatio = aspectRatio;
                }
                else
                {
                    // teletext (Mode 7)
                    _CRTState.ReadScreenDataFunc = ReadScreenData_Teletext;
                    _CRTState.WriteBitmapDataFunc = WriteBitmapData_Teletext;
                    _DisplayState.AspectRatio = DisplayState.AspectRatio_Teletext;
                }

                _ULAState.RegisterModified = false;
                _ULAState.PaletteModified = false;
            }

            // calc display frame width
            int horizontalMultiplier;
            if (_TeletextMode)
                horizontalMultiplier = 12;
            else if ((_ULAState.Register & 0x10) != 0)
                horizontalMultiplier = 8;
            else
                horizontalMultiplier = 16;

            int bitmapWidth = _CRTState.Register1_HorizontalDisplayed * horizontalMultiplier;
            if (bitmapWidth > DisplayState.MaxWidth)
                _DisplayFrame = false; // unsupported over-scan!

            if (_DisplayState.Width < bitmapWidth)
                _DisplayState.Width = bitmapWidth;
        }

        private void DisplayMemory(int cycleCount)
        {
            int characterCount = (cycleCount >> _CRTState.CharacterClockShift) - (_DisplayState.LastCycleCount >> _CRTState.CharacterClockShift);

            _DisplayState.LastCycleCount = cycleCount;

            if (_ULAState.RegisterModified || _ULAState.PaletteModified)
                UpdateVideoState(newFrame: false);

            for (int i = 0; i < characterCount; i++)
            {
                // read screen memory and rasterize it to CRT bitmap, if not in blanking period
                if (!_CRTState.BlankSpace)
                    _CRTState.WriteBitmapDataFunc(_CRTState.ReadScreenDataFunc());

                // advance CRT counters
                _CRTState.CharacterColumnCounter++;
                _CRTState.BlankSpace |= (_CRTState.CharacterColumnCounter == _CRTState.Register1_HorizontalDisplayed);

                if (_CRTState.CharacterColumnCounter == _CRTState.Register0_HorizontalTotal + 1)
                {
                    // new scanline
                    _CRTState.CharacterColumnCounter = 0;
                    _CRTState.ScanlineCounter += _CRTState.ScanlineCounterIncrement;

                    if (_DisplayState.ScanlineOffset > DisplayState.MaxScanlineOffset)
                        return; // over-scan!

                    _DisplayState.ScanlineOffset += _DisplayState.ScanlineOffsetIncrement;
                    _DisplayState.ScanlineIndex += _DisplayState.ScanlineIndexIncrement;

                    if (_CRTState.CharacterRowCounter <= _CRTState.Register4_VerticalTotal)
                    {
                        if (_CRTState.ScanlineCounter >= (_CRTState.Register9_ScanLinesPerChar + _CRTState.ScanlinesPerCharAdjust))
                        {
                            // new character row
                            _CRTState.ScanlineCounter = _CRTState.ScanlineCounterReset;
                            _CRTState.CharacterRowCounter++;

                            if (_CRTState.CharacterRowCounter == _CRTState.Register6_VerticalDisplayed)
                            {
                                // end of vertical display phase
                                _DisplayState.EndCycleCount = cycleCount;
                                _DisplayState.Height = (_DisplayState.ScanlineIndex + 1) & ~1; // round to even
                            }
                        }
                    }

                    if (_CRTState.CharacterRowCounter > _CRTState.Register4_VerticalTotal)
                    {
                        // vertical adjust phase
                        _CRTState.VerticalAdjustCounter += _CRTState.ScanlineCounterIncrement;
                        if (_CRTState.VerticalAdjustCounter >= _CRTState.Register5_VerticalTotalAdjust)
                            UpdateVideoState(newFrame: true);
                    }

                    _CRTState.BlankSpace = (_CRTState.CharacterRowCounter >= _CRTState.Register6_VerticalDisplayed);
                }
            }
        }

        private void MemoryWrite(CanonicalAddress memoryAddress, byte value)
        {
            ushort address = memoryAddress.Address;

            if (address < 0xE000)
            {
                int displayFrame = -1;

                if (memoryAddress.Page == MemoryPage.WholeRam && address < 0x8000)
                {
                    _ScreenMemory.Memory[address] = value;

                    if (IsScreenAddress(address))
                        displayFrame = _ScreenMemory.MemoryDisplayFrame[address];
                }
                else if (memoryAddress.Page == MemoryPage.ShadowRam)
                {
                    _ScreenMemory.ShadowRam[address - 0x3000] = value;

                    if (IsScreenAddress(address))
                        displayFrame = _ScreenMemory.ShadowRamDisplayFrame[address - 0x3000];
                }
                else if (memoryAddress.Page == MemoryPage.FilingSystemRam)
                {
                    _ScreenMemory.FilingSystemRam[address - 0xC000] = value;

                    if (IsScreenAddress(address))
                        displayFrame = _ScreenMemory.FilingSystemRamDisplayFrame[address - 0xC000];
                }

                if (displayFrame != -1)
                {
                    if (displayFrame < _AnalysisFrameState.FrameNumber)
                        _AnalysisFrameState.WritesBeforeDisplayRead++;
                    else
                        _AnalysisFrameState.WritesAfterDisplayRead++;
                }

                return;
            }

            if (memoryAddress.Page != MemoryPage.WholeRam)
                return;

            if (address == 0xFE00)
            {
                _CRTState.WriteRegister = value;
            }
            else if (address == 0xFE01)
            {
                switch (_CRTState.WriteRegister)
                {
                    case 0:
                        _CRTState.Register0_HorizontalTotal = value;
                        break;

                    case 1:
                        _CRTState.Register1_HorizontalDisplayed = value;
                        break;

                    case 2:
                        _CRTState.Register2_HorizontalSyncPos = value;
                        break;

                    case 3:
                        _CRTState.Register3_SyncWidth = value;
                        break;

                    case 4:
                        _CRTState.Register4_VerticalTotal = (byte)(value & 0x7F); // 7 bit register
                        break;

                    case 5:
                        _CRTState.Register5_VerticalTotalAdjust = (byte)(value & 0x1F); // 5 bit register
                        break;

                    case 6:
                        _CRTState.Register6_VerticalDisplayed = (byte)(value & 0x7F); // 7 bit register
                        break;

                    case 7:
                        _CRTState.Register7_VerticalSyncPos = (byte)(value & 0x7F); // 7 bit register
                        break;

                    case 8:
                        _CRTState.Register8_InterlaceAndDelay = (byte)(value & 0x3F); // 6 bit register;
                        break;

                    case 9:
                        _CRTState.Register9_ScanLinesPerChar = (byte)(value & 0x1F); // 5 bit register
                        break;

                    case 12:
                        _CRTState.Register12_ScreenStartHigh_Latch = value; // latched
                        break;

                    case 13:
                        _CRTState.Register13_ScreenStartLow_Latch = value; // latched
                        break;

                    default:
                        break;
                }
            }
            else if (address == 0xFE20)
            {
                _ULAState.RegisterModified |= (value != _ULAState.Register);
                _ULAState.Register = value;
            }
            else if (address == 0xFE21)
            {
                _ULAState.PaletteModified |= (value != _ULAState.Palette[value >> 4]);
                _ULAState.Palette[value >> 4] = value;
            }
            else if (address == 0xFE40 || address == 0xFE50)
            {
                int bit = 1 << (value & 0x07);
                if ((value & 0x8) == 0x8)
                    _PortBAddressableLatch |= (byte)bit;
                else
                    _PortBAddressableLatch &= (byte)~bit;

                _ScreenMemory.WrapAddress = (_PortBAddressableLatch & 0x30) switch
                {
                    0x00 => 0x4000,
                    0x10 => 0x6000,
                    0x20 => 0x3000,
                    0x30 => 0x5800,
                    _ => throw new ArgumentOutOfRangeException()
                };
                _ScreenMemory.WrapOffset = 0x8000 - _ScreenMemory.WrapAddress;
            }

            if (address >= 0xFE34 && address < 0xFE38)
            {
                SetDisplayShadowRam(value);
            }
        }

        private bool IsScreenAddress(ushort address)
        {
            int screenEndAddress = _ScreenMemory.StartAddress + _ScreenMemory.Size;
            int screenWrapSize = screenEndAddress - 0x8000;

            if (screenWrapSize <= 0)
                return (address >= _ScreenMemory.StartAddress && address < screenEndAddress);
            else
                return ((address >= _ScreenMemory.StartAddress && address < 0x8000) ||
                        (address >= _ScreenMemory.WrapAddress && address < _ScreenMemory.WrapAddress + screenWrapSize));
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

        private byte ReadScreenData_Void()
        {
            return 0;
        }

        private byte ReadScreenData_Teletext()
        {
            int characterAddress = _ScreenMemory.StartAddress + (_CRTState.CharacterRowCounter * _CRTState.Register1_HorizontalDisplayed) + _CRTState.CharacterColumnCounter;

            if (characterAddress >= 0x8000)
                characterAddress = (characterAddress - _ScreenMemory.WrapOffset) & 0x7FFF;

            return ReadDisplayMemory(characterAddress);
        }

        private byte ReadScreenData_NonTeletext()
        {
            if (_CRTState.ScanlineCounter >= 8)
                return 0;

            int characterAddress = _ScreenMemory.StartAddressDiv8 + (_CRTState.CharacterRowCounter * _CRTState.Register1_HorizontalDisplayed) + _CRTState.CharacterColumnCounter;
            int memoryAddress = (characterAddress << 3) + _CRTState.ScanlineCounter;

            if (memoryAddress >= 0x8000)
                memoryAddress = (memoryAddress - _ScreenMemory.WrapOffset) & 0x7FFF;

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
                        _ScreenMemory.FilingSystemRamDisplayFrame[address] = _DisplayState.FrameNumber;
                        return _ScreenMemory.FilingSystemRam[address];
                    }
                    else
                    {
                        _ScreenMemory.MemoryDisplayFrame[address] = _DisplayState.FrameNumber;
                        return _ScreenMemory.Memory[address];
                    }
                }
                else
                {
                    address -= 0x3000;
                    _ScreenMemory.ShadowRamDisplayFrame[address] = _DisplayState.FrameNumber;
                    return _ScreenMemory.ShadowRam[address];
                }
            }
            else
            {
                _ScreenMemory.MemoryDisplayFrame[address] = _DisplayState.FrameNumber;
                return _ScreenMemory.Memory[address];
            }
        }

        private void WriteBitmapData_Void(byte value)
        {
        }

        private void WriteBitmapData_32bits(byte value)
        {
            int index = _DisplayState.ScanlineOffset + (_CRTState.CharacterColumnCounter << 2);
            Buffer.BlockCopy(_DisplayState.LookupTbl[value], 0, _DisplayState.Buffer, index, 4);
        }

        private void WriteBitmapData_64bits(byte value)
        {
            int index = _DisplayState.ScanlineOffset + (_CRTState.CharacterColumnCounter << 3);
            Buffer.BlockCopy(_DisplayState.LookupTbl[value], 0, _DisplayState.Buffer, index, 8);
        }

        private void WriteBitmapData_Teletext(byte value)
        {
            if (_CRTState.CharacterColumnCounter >= 40)
                return;

            // update double-height row state
            if (_CRTState.CharacterRowCounter != _Mode7State.LastRowCounter)
            {
                if (_CRTState.CharacterRowCounter == 0)
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

                _Mode7State.LastRowCounter = _CRTState.CharacterRowCounter;
            }

            // reset state at start of scanline
            if (_CRTState.CharacterColumnCounter == 0)
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
                    fontBitmap = _Mode7State.HoldMosaic ? _Mode7State.MosaicFont : _Mode7State.GraphicFont;
                }
                else
                {
                    value = 32; // space
                    fontBitmap = _Mode7State.Graphics ? (_Mode7State.Mosaic ? _Mode7State.MosaicFont : _Mode7State.GraphicFont) : _Mode7State.TextFont;
                }
            }
            else
            {
                fontBitmap = _Mode7State.Graphics ? (_Mode7State.Mosaic ? _Mode7State.MosaicFont : _Mode7State.GraphicFont) : _Mode7State.TextFont;
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
                scanlineIndex = (_CRTState.ScanlineCounter >> 1) + (_Mode7State.DoubleHeightRow == Mode7DoubleHeightRow.Top ? 0 : 10);
            else
                scanlineIndex = _CRTState.ScanlineCounter;

            // fetch font scanline
            int valueAsIndex = Math.Max(value - 32, 0);
            uint fontScanline = fontBitmap[valueAsIndex, scanlineIndex];

            // render 12 pixels (6 bytes)
            int index = _DisplayState.ScanlineOffset + (_CRTState.CharacterColumnCounter * 6);
            uint pixelMask = 0x800;
            while (pixelMask != 0)
            {
                uint hiColor = ((fontScanline & pixelMask) != 0) ? foreColor : _Mode7State.BackColor;
                pixelMask >>= 1;

                uint loColor = ((fontScanline & pixelMask) != 0) ? foreColor : _Mode7State.BackColor;
                pixelMask >>= 1;

                _DisplayState.Buffer[index++] = (byte)((hiColor << 4) | loColor);
            }

            // commit pending foreground color
            _Mode7State.ForeColor = _Mode7State.ForeColorPending;
        }

        private void Build4bppLookupTbl(int bitsPerPixel)
        {
            Debug.Assert(bitsPerPixel == 1);
            int shiftCount = 8 / bitsPerPixel;
            for (int value = 0; value < 256; value++)
            {
                var bitmapTbl = _DisplayState.LookupTbl[value];

                int shiftRegister = value;
                for (int i = 0; i < shiftCount; i += 2)
                {
                    // first lookup / shift
                    int firstIndex =
                        ((shiftRegister >> 4) & 0x8) |
                        ((shiftRegister >> 3) & 0x4) |
                        ((shiftRegister >> 2) & 0x2) |
                        ((shiftRegister >> 1) & 0x1);

                    int firstEntry = _ULAState.Palette[firstIndex] & 0xF;
                    firstEntry ^= 0x7;

                    if (firstEntry > 0x7) // flashing color?
                    {
                        firstEntry &= 0x7;
                        if ((_ULAState.Register & 0x1) != 0)
                            firstEntry ^= 0x7;
                    }

                    shiftRegister = (shiftRegister << 1) | 0x1;

                    // second lookup / shift
                    int secondIndex =
                        ((shiftRegister >> 4) & 0x8) |
                        ((shiftRegister >> 3) & 0x4) |
                        ((shiftRegister >> 2) & 0x2) |
                        ((shiftRegister >> 1) & 0x1);

                    int secondEntry = _ULAState.Palette[secondIndex] & 0xF;
                    secondEntry ^= 0x7;

                    if (secondEntry > 0x7) // flashing color?
                    {
                        secondEntry &= 0x7;
                        if ((_ULAState.Register & 0x1) != 0)
                            secondEntry ^= 0x7;
                    }

                    shiftRegister = (shiftRegister << 1) | 0x1;

                    byte pixelPair = (byte)((firstEntry << 4) | secondEntry);
                    bitmapTbl[i >> 1] = pixelPair;
                }
            }
        }

        private void Build8bppLookupTbl(int bitsPerPixel)
        {
            Debug.Assert(bitsPerPixel == 1 || bitsPerPixel == 2);
            int shiftCount = 8 / bitsPerPixel;
            for (int value = 0; value < 256; value++)
            {
                var bitmapTbl = _DisplayState.LookupTbl[value];

                int shiftRegister = value;
                for (int i = 0; i < shiftCount; i++)
                {
                    // lookup / shift
                    int index =
                        ((shiftRegister >> 4) & 0x8) |
                        ((shiftRegister >> 3) & 0x4) |
                        ((shiftRegister >> 2) & 0x2) |
                        ((shiftRegister >> 1) & 0x1);

                    int entry = _ULAState.Palette[index] & 0xF;
                    entry ^= 0x7;

                    if (entry > 0x7) // flashing color?
                    {
                        entry &= 0x7;
                        if ((_ULAState.Register & 0x1) != 0)
                            entry ^= 0x7;
                    }

                    shiftRegister = (shiftRegister << 1) | 0x1;

                    byte pixelPair = (byte)((entry << 4) | entry);
                    bitmapTbl[i] = pixelPair;
                }
            }
        }

        private void Build16bppLookupTbl(int bitsPerPixel)
        {
            Debug.Assert(bitsPerPixel == 2 || bitsPerPixel == 4);
            int shiftCount = 8 / bitsPerPixel;
            for (int value = 0; value < 256; value++)
            {
                var bitmapTbl = _DisplayState.LookupTbl[value];

                int shiftRegister = value;
                for (int i = 0; i < shiftCount; i++)
                {
                    // lookup / shift
                    int index =
                        ((shiftRegister >> 4) & 0x8) |
                        ((shiftRegister >> 3) & 0x4) |
                        ((shiftRegister >> 2) & 0x2) |
                        ((shiftRegister >> 1) & 0x1);

                    int entry = _ULAState.Palette[index] & 0xF;
                    entry ^= 0x7;

                    if (entry > 0x7) // flashing color?
                    {
                        entry &= 0x7;
                        if ((_ULAState.Register & 0x1) != 0)
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

        private void Build32bppLookupTbl(int bitsPerPixel)
        {
            Debug.Assert(bitsPerPixel == 4);
            int shiftCount = 8 / bitsPerPixel;
            for (int value = 0; value < 256; value++)
            {
                var bitmapTbl = _DisplayState.LookupTbl[value];

                int shiftRegister = value;
                for (int i = 0; i < shiftCount; i++)
                {
                    // lookup / shift
                    int index =
                        ((shiftRegister >> 4) & 0x8) |
                        ((shiftRegister >> 3) & 0x4) |
                        ((shiftRegister >> 2) & 0x2) |
                        ((shiftRegister >> 1) & 0x1);

                    int entry = _ULAState.Palette[index] & 0xF;
                    entry ^= 0x7;

                    if (entry > 0x7) // flashing color?
                    {
                        entry &= 0x7;
                        if ((_ULAState.Register & 0x1) != 0)
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

        private bool ConstructMode7Fonts()
        {
            try
            {
                var resources = new System.ComponentModel.ComponentResourceManager(typeof(ux.BeebPerfForm));
                var bytes = (byte[])resources.GetObject("teletext.Font")!;
                using var ms = new MemoryStream(bytes!);

                for (int ch = 0; ch < 96; ch++)
                {
                    for (int i = 0; i < 2; i++)
                        _Mode7State.TextFont[ch, i] = 0;

                    for (int i = 2; i < 20; i++)
                    {
                        int loByte = ms.ReadByte();
                        if (loByte == -1)
                            throw new EndOfStreamException();

                        int hiByte = ms.ReadByte();
                        if (hiByte == -1)
                            throw new EndOfStreamException();

                        _Mode7State.TextFont[ch, i] = (uint)((hiByte << 8) | loByte);
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
                        _Mode7State.GraphicFont[ch, i] = top;

                    for (int i = 6; i < 14; i++)
                        _Mode7State.GraphicFont[ch, i] = middle;

                    for (int i = 14; i < 20; i++)
                        _Mode7State.GraphicFont[ch, i] = bottom;

                    // mosaic fonts
                    top &= 0x03CF;
                    middle &= 0x03CF;
                    bottom &= 0x03CF;

                    for (int i = 0; i < 4; i++)
                        _Mode7State.MosaicFont[ch, i] = top;

                    for (int i = 4; i < 6; i++)
                        _Mode7State.MosaicFont[ch, i] = 0;

                    for (int i = 6; i < 12; i++)
                        _Mode7State.MosaicFont[ch, i] = middle;

                    for (int i = 12; i < 14; i++)
                        _Mode7State.MosaicFont[ch, i] = 0;

                    for (int i = 14; i < 18; i++)
                        _Mode7State.MosaicFont[ch, i] = bottom;

                    for (int i = 18; i < 20; i++)
                        _Mode7State.MosaicFont[ch, i] = 0;
                }
            }

            return true;
        }

        public class Frame
        {
            public struct DisplayFrameSpan
            {
                public required int FrameNumber;
                public required int StartCycleCount;
                public required int EndCycleCount;
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
        private BBCModelType _BBCModel;

        private bool _TeletextMode;
        private bool _DisplayShadowRam;
        private byte _PortBAddressableLatch;
        private bool _DisplayFrame;

        // analysis frame state
        private struct AnalysisFrameState
        {
            public AnalysisFrameState() { }

            public FrameSettings? FrameSettings;
            public int WritesBeforeDisplayRead;
            public int WritesAfterDisplayRead;
            public int StartInstructionIndex;
            public int EndInstructionIndex;
            public int StartCycleCount;
            public int FrameNumber = 0;
        }
        private AnalysisFrameState _AnalysisFrameState = new();

        // screen memory state
        private struct ScreenMemory
        {
            public ScreenMemory() {}

            public int StartAddressDiv8;
            public int StartAddress;
            public int WrapAddress;
            public int WrapOffset;
            public int Size;

            public byte[] Memory = [];
            public byte[] ShadowRam = [];
            public byte[] FilingSystemRam = [];

            public int[] MemoryDisplayFrame = [];
            public int[] ShadowRamDisplayFrame = [];
            public int[] FilingSystemRamDisplayFrame = [];
        }
        private ScreenMemory _ScreenMemory = new();

        // ULA state
        private struct ULAState
        {
            public ULAState() {}

            public byte Register;
            public byte[] Palette = [];
            public bool RegisterModified;
            public bool PaletteModified;
        }
        private ULAState _ULAState = new();

        // CRT 6845...
        private struct CRTState
        {
            public CRTState(FrameAnalysis frameAnalysis)
            {
                WriteBitmapDataFunc = frameAnalysis.WriteBitmapData_Void;
                ReadScreenDataFunc = frameAnalysis.ReadScreenData_Void;
            }

            public byte WriteRegister;
            public byte Register12_ScreenStartHigh_Latch;
            public byte Register13_ScreenStartLow_Latch;

            public byte Register0_HorizontalTotal;
            public byte Register1_HorizontalDisplayed;
            public byte Register2_HorizontalSyncPos;
            public byte Register3_SyncWidth;
            public byte Register4_VerticalTotal;
            public byte Register5_VerticalTotalAdjust;
            public byte Register6_VerticalDisplayed;
            public byte Register7_VerticalSyncPos;
            public byte Register8_InterlaceAndDelay;
            public byte Register9_ScanLinesPerChar;
            public byte Register12_ScreenStartHigh;
            public byte Register13_ScreenStartLow;

            public bool BlankSpace;
            public int CharacterColumnCounter;
            public int CharacterRowCounter;
            public int VerticalAdjustCounter;
            public int ScanlineCounter;
            public int ScanlineCounterIncrement;
            public int ScanlineCounterReset;
            public int ScanlinesPerCharAdjust;
            public int CharacterClockShift;

            public WriteBitmapData WriteBitmapDataFunc;
            public ReadScreenData ReadScreenDataFunc;
        }
        private CRTState _CRTState = new();

        private struct DisplayState
        {
            public DisplayState()
            {
                for (int i = 0; i < 256; i++)
                    LookupTbl[i] = new byte[8];
            }

            public const int MaxWidth = 1024;
            public const int MaxHeight = 640;
            public const int BufferStride = MaxWidth / 2;
            public const int MaxScanlineOffset = (MaxHeight - 1) * BufferStride;
            public const float AspectRatio_Teletext = 0.813f;
            public const float AspectRatio_NonTeletext = 0.46f;

            public int FrameNumber;
            public int StartCycleCount;
            public int EndCycleCount;
            public int LastCycleCount;
            public int Height;
            public int Width;
            public float AspectRatio;

            public int ScanlineIndex;
            public int ScanlineIndexIncrement;

            public int ScanlineOffset;
            public int ScanlineOffsetIncrement;
            public int ScanlineOffsetReset;

            public byte[] Buffer = [];
            public byte[][] LookupTbl = new byte[256][];

            public static ColorPalette BBCPalette = new([
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
                Color.White    // flashing white-black
            ]);
        }
        private DisplayState _DisplayState = new();

        // mode 7...
        private enum Mode7DoubleHeightRow
        {
            Top = 0,
            Bottom = 1
        }

        private struct Mode7State
        {
            public Mode7State()
            {
            }
            
            public const int Mode7FlashOffFrameCount = 13;
            public const int Mode7FlashOnFrameCount = 37;

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

            public uint[,] TextFont = new uint[96, 20];
            public uint[,] GraphicFont = new uint[96, 20];
            public uint[,] MosaicFont = new uint[96, 20];
        }
        private Mode7State _Mode7State = new();
    }
}