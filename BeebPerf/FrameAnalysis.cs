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
            InitializeAnalysisFrames(frameSettings);

            // process instructions whilst emulating video hardware to generate display frames whilst
            // also creating analysis frames based on the provided frame settings
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

                if (instruction.IsInstruction)
                {
                    byte opcode = instruction.Opcode;
                    var memoryAccess = instructionSet.MemoryAccess(opcode);
                    if ((memoryAccess & InstructionSet.MemoryAccessType.Write) != 0)
                        MemoryWrite(instruction.MemoryAddress, instruction.MemoryWriteValue);
                }
                else if (instruction.IsFrameStart)
                {
                    FrameStart(model.FrameStartEventParamsList[instruction.FrameStartEventParamsIndex], cycleCount);
                }

                if (_DisplayState.FrameNumber > 0)
                    DisplayMemory(postCycleCount);

                cycleCount = postCycleCount;
            }

            UpdateAnalysisFrames();

            return true;
        }

        //
        // Analysis frames...
        //
        private void InitializeAnalysisFrames(FrameSettings? frameSettings)
        {
            Frames = [];
            _AnalysisFrameState.FrameNumber = 1;

            if (frameSettings != null && !frameSettings.Match(_Instructions))
                frameSettings = null;

            _AnalysisFrameState.FrameSettings = frameSettings;
            _AnalysisFrameState.StartInstructionIndex = (frameSettings != null) ? FindInstructionIndex(indexFrom: 0, frameSettings.StartAddress) : -1;
            _AnalysisFrameState.EndInstructionIndex = -1;

            _AnalysisFrameState.MemoryDisplayFrame = new int[32768];
            _AnalysisFrameState.ShadowRamDisplayFrame = new int[20480];
            _AnalysisFrameState.FilingSystemRamDisplayFrame = new int[8192];
        }

        private void UpdateAnalysisFrames()
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

                // update WritesBeforeDisplayRead & WritesAfterDisplayRead
                if (displayFrameSpans.Count == 0)
                {
                    frame.WritesBeforeDisplayRead = 0;
                    frame.WritesAfterDisplayRead = 0;
                }
                else if (displayFrameSpans[0].StartCycleCount >= frame.StartCycleCount)
                {
                    // use counts associated with the next display frame
                    frame.WritesBeforeDisplayRead = frame.WritesBeforeDisplayReadNext;
                    frame.WritesAfterDisplayRead = frame.WritesAfterDisplayReadNext;
                }

                // clear, as no longer used
                frame.WritesBeforeDisplayReadNext = 0;
                frame.WritesAfterDisplayReadNext = 0;

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

            // reset counts (for current and next display frames)
            _AnalysisFrameState.WritesBeforeDisplayRead = 0;
            _AnalysisFrameState.WritesAfterDisplayRead = 0;
            _AnalysisFrameState.WritesBeforeDisplayReadNext = 0;
            _AnalysisFrameState.WritesAfterDisplayReadNext = 0;

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
                WritesBeforeDisplayReadNext = _AnalysisFrameState.WritesBeforeDisplayReadNext,
                WritesAfterDisplayReadNext = _AnalysisFrameState.WritesAfterDisplayReadNext,
            });

            // find next start instruction
            if (_AnalysisFrameState.FrameSettings!.Type == FrameSettings.FrameType.StartAndEndAddresses &&
                _AnalysisFrameState.FrameSettings.StartAddress.Equals(_AnalysisFrameState.FrameSettings.EndAddress))
                _AnalysisFrameState.StartInstructionIndex = instructionIndex;
            else
                _AnalysisFrameState.StartInstructionIndex = FindInstructionIndex(instructionIndex + 1, _AnalysisFrameState.FrameSettings!.StartAddress);

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

        //
        // Video frames...
        //
        private void InitializeVideo(Model model)
        {
            // initialize display state
            _DisplayState.FrameNumber = 0;
            _DisplayState.CharacterCycleCount = 0;
            _DisplayState.FrameBuffer = new byte[(DisplayState.MaxHeight * 2 + 1) * DisplayState.FrameBufferStride];

            // initialize ULA state
            _ULAState.WriteDisplayDataTblInvalid = true;

            // initialize screen memory
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
            _ScreenMemory.PortBAddressableLatch = _ScreenMemory.WrapAddress switch
            {
                0x4000 => 0x00,
                0x6000 => 0x10,
                0x3000 => 0x20,
                0x5800 => 0x30,
                _ => 0x00
            };

            SetDisplayShadowRam(model.Snapshot.AccessControlRegister);
        }

        private void CaptureDisplayFrame()
        {
            Debug.Assert(_DisplayState.EndCycleCount - _DisplayState.StartCycleCount < 80000/*two frames*/);

            // calculate top scanline and scanline count
            int topScanline, scanlineCount;

            scanlineCount = _DisplayState.LastDisplayScanline - _DisplayState.FirstDisplayScanline + 1;
            if (_DisplayState.CaptureTeletextFrame)
            {
                topScanline = _DisplayState.FirstDisplayScanline;
                scanlineCount = 250;
            }
            else
            {
                topScanline = 40;
                scanlineCount = 256;
            }

            if (topScanline + scanlineCount > 312)
                topScanline = 312 - scanlineCount;

            // calculate bitmap height and frame buffer stride
            int bitmapHeight, frameBufferStride;
            if (_DisplayState.CaptureTeletextFrame)
            {
                bitmapHeight = scanlineCount * 2;
                frameBufferStride = DisplayState.FrameBufferStride;
            }
            else
            {
                bitmapHeight = scanlineCount;
                frameBufferStride = DisplayState.FrameBufferStride * 2;
            }

            int frameBufferOffset = topScanline * DisplayState.FrameBufferStride * 2;

            Bitmap bitmap = new Bitmap(_DisplayState.Width, bitmapHeight, PixelFormat.Format4bppIndexed);
            bitmap.Palette = ULAState.BBCPalette;

            Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            System.Drawing.Imaging.BitmapData? data = null;
            try
            {
                data = bitmap.LockBits(rect, ImageLockMode.WriteOnly, bitmap.PixelFormat);
                unsafe
                {
                    fixed (byte* src = _DisplayState.FrameBuffer)
                    {
                        byte* dst = (byte*)data.Scan0;
                        for (int i = 0; i < bitmapHeight; i++)
                        {
                            Buffer.MemoryCopy(
                                src + frameBufferOffset + (i * frameBufferStride),
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

            DisplayFrames.Add(new()
            {
                AspectRatio = _DisplayState.AspectRatio,
                FrameNumber = _DisplayState.FrameNumber,
                StartCycleCount = _DisplayState.StartCycleCount,
                EndCycleCount = _DisplayState.EndCycleCount,
                Bitmap = bitmap
            });
        }

        private void UpdateVideoState(bool forceUpdate = false)
        {
            if (forceUpdate || _ULAState.ControlRegisterModified || _CRTCState.RegisterModified)
            {
                _ULAState.TeletextMode = (_ULAState.ControlRegister & 0x02) == 0x02;
                _ULAState.CyclesPerCharacter = ((_ULAState.ControlRegister & 0x10) == 0x10) ? 1 : 2;

                bool interlacedSyncAndVideo = (_CRTCState.Register8_InterlaceAndDelay & 0x03) == 0x03;
                _CRTCState.CharacterScanlineIncrement = interlacedSyncAndVideo ? 2 : 1;
                _CRTCState.CharacterScanlineReset = (!interlacedSyncAndVideo || _CRTCState.DisplayField == CRTCState.DisplayFieldEven) ? 0 : 1;
                _CRTCState.VideoOutputEnabled = (_CRTCState.Register8_InterlaceAndDelay & 0x30) != 0x30;

                // in Interlaced Sync & Video mode (mode 7), the ScanlinesPerCharacter register is required
                // to hold an odd value (ref MC6845 data sheet). However, in mode 7 its value is set to 18!
                // Account for the undocumented behavior where even values are treated as the next odd values.
                _CRTCState.ScanlinesPerCharacterAdjust = (interlacedSyncAndVideo && (_CRTCState.Register9_ScanlinesPerCharacter % 2) == 0) ? 1 : 0;

                int displayWidth = _CRTCState.Register1_HorizontalDisplayed * CalcDisplayBytesPerCharacter();
                if (displayWidth > _DisplayState.Width)
                    _DisplayState.Width = displayWidth;

                // clear ULA modified flag
                _ULAState.ControlRegisterModified = false;
            }

            if (forceUpdate || _ULAState.ControlRegisterModified)
            {
                if (_ULAState.TeletextMode)
                {
                    // teletext (Mode 7)
                    _DisplayState.ReadScreenDataFunc = ReadScreenData_Teletext;
                    _DisplayState.WriteDisplayDataFunc = WriteDisplayData_Teletext;
                    _DisplayState.AspectRatio = DisplayState.AspectRatio_Teletext;
                }
                else
                {
                    // non-teletext modes (Modes 0-6, and 8)
                    _DisplayState.ReadScreenDataFunc = ReadScreenData_NonTeletext;
                    _DisplayState.WriteDisplayDataFunc = ((_ULAState.ControlRegister & 0x10) == 0x10)
                        ? WriteDisplayData_32bits  // modes 0,1,2,3
                        : WriteDisplayData_64bits; // modes 4,5,6,8,
                    _DisplayState.AspectRatio = DisplayState.AspectRatio_NonTeletext;
                }

                // clear CRTC modified flag
                _CRTCState.RegisterModified = false;
            }
        }

        private void BuildWriteDisplayDataTbl()
        {
            if (_ULAState.TeletextMode)
                return;

            switch ((_ULAState.ControlRegister >> 2) & 0x7)
            {
                case 7: // Mode 0 & 3 (2 colors, high res)
                    BuildWriteDisplayDataTbl_4bpp(bitsPerPixel: 1);
                    break;

                case 6: // Mode 1 (4 colors, medium res)
                    BuildWriteDisplayDataTbl_8bpp(bitsPerPixel: 2);
                    break;

                case 5: // Mode 2 (16 colors, low res)
                    BuildWriteDisplayDataTbl_16bpp(bitsPerPixel: 4);
                    break;

                case 2: // Mode 4 & 6 (2 colors, medium res)
                    BuildWriteDisplayDataTbl_8bpp(bitsPerPixel: 1);
                    break;

                case 1: // Mode 5 (4 colors, low res)
                    BuildWriteDisplayDataTbl_16bpp(bitsPerPixel: 2);
                    break;

                case 0: // Mode 8 (16 colors, very low res)
                    BuildWriteDisplayDataTbl_32bpp(bitsPerPixel: 4);
                    break;

                default:
                    throw new NotImplementedException();
            }

            _ULAState.WriteDisplayDataTblInvalid = false;
        }

        private void FrameStart(FrameStartEventParams frameStartParams, int cycleCount)
        {
            int startFrameCycleCount = cycleCount - frameStartParams.OffsetCycleCount;
            _DisplayState.CharacterCycleCount = startFrameCycleCount;

            if (!frameStartParams.SplitScreen) // true frame?
            {
                // capture previous frame
                if (_DisplayState.FrameNumber > 0)
                    CaptureDisplayFrame();

                // increment frame number
                _DisplayState.FrameNumber++;

                // initialize display state
                _DisplayState.StartCycleCount = startFrameCycleCount;
                _DisplayState.FirstDisplayScanline = frameStartParams.DisplayScanline;
                _DisplayState.LastDisplayScanline = frameStartParams.DisplayScanline;
                _DisplayState.Width = 0; // updated in UpdateVideoState(...)

                // reset teletext state
                _TeletextState.DoubleHeightRow = TeletextState.DoubleHeightTopRow;
                _TeletextState.LastRowCounter = -1;

                if (--_TeletextState.FlashTrigger < 0)
                {
                    _TeletextState.FlashTrigger = _TeletextState.FlashOn
                        ? TeletextState.FlashOffFrameCount
                        : TeletextState.FlashOnFrameCount;
                    _TeletextState.FlashOn = !_TeletextState.FlashOn; // toggle flash state
                }

                // clear frame buffer
                Array.Clear(_DisplayState.FrameBuffer, 0, DisplayState.MaxHeight * 2 * DisplayState.FrameBufferStride);
            }
            else if (_DisplayState.FrameNumber == 0)
            {
                return; // ignore split screen frames that occur before the first true frame
            }

            // does the write display data table need rebuilding?
            _ULAState.WriteDisplayDataTblInvalid |= ((_ULAState.ControlRegister ^ frameStartParams.ULAControlRegister) & 0x1D) != 0;
            for (int i = 0; i < _ULAState.ColorPalette.Length && i < frameStartParams.ULAColorPalette.Length; i++)
                _ULAState.WriteDisplayDataTblInvalid |= (_ULAState.ColorPalette[i] != frameStartParams.ULAColorPalette[i]);

            // reset registers
            _ULAState.ControlRegister = frameStartParams.ULAControlRegister;
            _ULAState.ColorPalette = frameStartParams.ULAColorPalette.ToArray();

            _CRTCState.RegisterSelect = frameStartParams.CRTCRegisterSelect;
            _CRTCState.Register0_HorizontalTotal = frameStartParams.CRTCRegisters[0];
            _CRTCState.Register1_HorizontalDisplayed = frameStartParams.CRTCRegisters[1];
            _CRTCState.Register2_HorizontalSyncPos = frameStartParams.CRTCRegisters[2];
            _CRTCState.Register3_SyncWidth = frameStartParams.CRTCRegisters[3];
            _CRTCState.Register4_VerticalTotal = frameStartParams.CRTCRegisters[4];
            _CRTCState.Register5_VerticalTotalAdjust = frameStartParams.CRTCRegisters[5];
            _CRTCState.Register6_VerticalDisplayed = frameStartParams.CRTCRegisters[6];
            _CRTCState.Register7_VerticalSyncPos = frameStartParams.CRTCRegisters[7];
            _CRTCState.Register8_InterlaceAndDelay = frameStartParams.CRTCRegisters[8];
            _CRTCState.Register9_ScanlinesPerCharacter = frameStartParams.CRTCRegisters[9];
            _CRTCState.Register12_ScreenStartHigh = frameStartParams.CRTCRegisters[12];
            _CRTCState.Register13_ScreenStartLow = frameStartParams.CRTCRegisters[13];

            _CRTCState.DisplayScanline = frameStartParams.DisplayScanline;
            _CRTCState.DisplayScanlinePos = 0;
            _CRTCState.DisplayField = frameStartParams.DisplayField;
            _CRTCState.SplitScreen = frameStartParams.SplitScreen;

            // update video state
            UpdateVideoState(forceUpdate: true);

            // reset CRTC counters
            _CRTCState.CharacterRow = 0;
            _CRTCState.CharacterColumn = 0;
            _CRTCState.CharacterScanline = _CRTCState.CharacterScanlineReset; // set in UpdateVideoState()

            // calculate first character row address
            if (_ULAState.TeletextMode)
                _CRTCState.CharacterRowAddress = (((_CRTCState.Register12_ScreenStartHigh ^ 0x20) + 0x74) << 8) + _CRTCState.Register13_ScreenStartLow;
            else
                _CRTCState.CharacterRowAddress = (_CRTCState.Register12_ScreenStartHigh << 8) + _CRTCState.Register13_ScreenStartLow;

            _CRTCState.CharacterAddress = _CRTCState.CharacterRowAddress;

            // calculate screen memory start address and size
            _ScreenMemory.StartAddress = _CRTCState.CharacterRowAddress;
            _ScreenMemory.Size = _CRTCState.Register6_VerticalDisplayed * _CRTCState.Register1_HorizontalDisplayed;

            if (!_ULAState.TeletextMode)
            {
                _ScreenMemory.StartAddress *= 8;
                _ScreenMemory.Size *= 8;
            }
        }

        private void DisplayMemory(int cycleCount)
        {
            // update video state if any registers have been modified
            if (_CRTCState.RegisterModified || _ULAState.ControlRegisterModified)
                UpdateVideoState();

            while (_DisplayState.CharacterCycleCount < cycleCount)
            {
                _DisplayState.CharacterCycleCount += _ULAState.CyclesPerCharacter;

                if (_CRTCState.DisplayScanline >= 312)
                    continue;

                if (_CRTCState.CharacterColumn < _CRTCState.Register1_HorizontalDisplayed &&
                    _CRTCState.CharacterRow < _CRTCState.Register6_VerticalDisplayed &&
                    _CRTCState.VideoOutputEnabled)
                {
                    // update variables used to extract the image from the frame buffer
                    _DisplayState.CaptureTeletextFrame = _ULAState.TeletextMode;
                    _DisplayState.LastDisplayScanline = _CRTCState.DisplayScanline;
                    _DisplayState.EndCycleCount = cycleCount;

                    // rebuild write display table?
                    if (_ULAState.WriteDisplayDataTblInvalid)
                        BuildWriteDisplayDataTbl();

                    // read screen memory and rasterize it
                    _DisplayState.WriteDisplayDataFunc(_DisplayState.ReadScreenDataFunc());
                }

                // advance CRTC counters
                _CRTCState.CharacterColumn++;
                _CRTCState.CharacterAddress++;

                if (_CRTCState.CharacterColumn >= _CRTCState.Register0_HorizontalTotal + 1)
                {
                    // next scanline
                    _CRTCState.CharacterColumn = 0;
                    _CRTCState.CharacterScanline += _CRTCState.CharacterScanlineIncrement;

                    // increment display scanline
                    _CRTCState.DisplayScanlinePos = 0;
                    _CRTCState.DisplayScanline++;

                    // next character row?
                    if (_CRTCState.CharacterScanline > _CRTCState.Register9_ScanlinesPerCharacter + _CRTCState.ScanlinesPerCharacterAdjust)
                    {
                        _CRTCState.CharacterRow++;
                        _CRTCState.CharacterScanline = _CRTCState.CharacterScanlineReset;
                        _CRTCState.CharacterRowAddress += _CRTCState.Register1_HorizontalDisplayed;
                    }

                    _CRTCState.CharacterAddress = _CRTCState.CharacterRowAddress;
                }
            }
        }

        private int CalcDisplayBytesPerCharacter()
        {
            if ((_ULAState.ControlRegister & 0x2) != 0)
                return 12; // modes 7 (teletext)
            else if ((_ULAState.ControlRegister & 0x10) != 0)
                return 8; // modes 0,1,2,3
            else
                return 16; // modes 4,5,6,8
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
                        displayFrame = _AnalysisFrameState.MemoryDisplayFrame[address];
                }
                else if (memoryAddress.Page == MemoryPage.ShadowRam)
                {
                    _ScreenMemory.ShadowRam[address - 0x3000] = value;

                    if (IsScreenAddress(address))
                        displayFrame = _AnalysisFrameState.ShadowRamDisplayFrame[address - 0x3000];
                }
                else if (memoryAddress.Page == MemoryPage.FilingSystemRam)
                {
                    _ScreenMemory.FilingSystemRam[address - 0xC000] = value;

                    if (IsScreenAddress(address))
                        displayFrame = _AnalysisFrameState.FilingSystemRamDisplayFrame[address - 0xC000];
                }

                if (displayFrame != -1)
                {
                    // increment counts for current display frame
                    if (displayFrame < _DisplayState.FrameNumber)
                        _AnalysisFrameState.WritesBeforeDisplayRead++;
                    else
                        _AnalysisFrameState.WritesAfterDisplayRead++;

                    // increment counts for next display frame
                    if (displayFrame < _DisplayState.FrameNumber + 1)
                        _AnalysisFrameState.WritesBeforeDisplayReadNext++;
                    else
                        _AnalysisFrameState.WritesAfterDisplayReadNext++;
                }

                return;
            }

            if (memoryAddress.Page != MemoryPage.WholeRam)
                return;

            if (address == 0xFE00)
            {
                _CRTCState.RegisterSelect = value;
            }
            else if (address == 0xFE01)
            {
                switch (_CRTCState.RegisterSelect)
                {
                    case 0:
                        _CRTCState.RegisterModified = (value != _CRTCState.Register0_HorizontalTotal);
                        _CRTCState.Register0_HorizontalTotal = value;
                        break;

                    case 1:
                        _CRTCState.RegisterModified = (value != _CRTCState.Register1_HorizontalDisplayed);
                        _CRTCState.Register1_HorizontalDisplayed = value;
                        break;

                    case 2:
                        _CRTCState.RegisterModified = (value != _CRTCState.Register2_HorizontalSyncPos);
                        _CRTCState.Register2_HorizontalSyncPos = value;
                        break;

                    case 3:
                        _CRTCState.RegisterModified = (value != _CRTCState.Register3_SyncWidth);
                        _CRTCState.Register3_SyncWidth = value;
                        break;

                    case 4:
                        value &= 0x7F; // 7 bit register
                        _CRTCState.RegisterModified = (value != _CRTCState.Register4_VerticalTotal);
                        _CRTCState.Register4_VerticalTotal = value;
                        break;

                    case 5:
                        value &= 0x1F; // 5 bit register
                        _CRTCState.RegisterModified = (value != _CRTCState.Register5_VerticalTotalAdjust);
                        _CRTCState.Register5_VerticalTotalAdjust = value;
                        break;

                    case 6:
                        value &= 0x7F; // 7 bit register
                        _CRTCState.RegisterModified = (value != _CRTCState.Register6_VerticalDisplayed);
                        _CRTCState.Register6_VerticalDisplayed = value;
                        break;

                    case 7:
                        value &= 0x7F; // 7 bit register
                        _CRTCState.RegisterModified = (value != _CRTCState.Register7_VerticalSyncPos);
                        _CRTCState.Register7_VerticalSyncPos = value;
                        break;

                    case 8:
                        value &= 0x3F; // 6 bit register
                        _CRTCState.RegisterModified = (value != _CRTCState.Register8_InterlaceAndDelay);
                        _CRTCState.Register8_InterlaceAndDelay = value;
                        break;

                    case 9:
                        value &= 0x1F; // 5 bit register
                        _CRTCState.RegisterModified = (value != _CRTCState.Register9_ScanlinesPerCharacter);
                        _CRTCState.Register9_ScanlinesPerCharacter = value;
                        break;

                    case 12:
                        _CRTCState.RegisterModified = (value != _CRTCState.Register12_ScreenStartHigh);
                        _CRTCState.Register12_ScreenStartHigh = value;
                        break;

                    case 13:
                        _CRTCState.RegisterModified = (value != _CRTCState.Register13_ScreenStartLow);
                        _CRTCState.Register13_ScreenStartLow = value;
                        break;

                    default:
                        break;
                }
            }
            else if (address == 0xFE20)
            {
                _ULAState.WriteDisplayDataTblInvalid |= ((value ^ _ULAState.ControlRegister) & 0x1D) != 0;
                _ULAState.ControlRegisterModified |= (value != _ULAState.ControlRegister);
                _ULAState.ControlRegister = value;
            }
            else if (address == 0xFE21)
            {
                int index = value >> 4;
                byte color = (byte)((value ^ 0x7) & 0xF);
                _ULAState.WriteDisplayDataTblInvalid |= (color != _ULAState.ColorPalette[index]);
                _ULAState.ColorPalette[index] = color;
            }
            else if (address == 0xFE40 || address == 0xFE50)
            {
                int bit = 1 << (value & 0x07);
                if ((value & 0x8) == 0x8)
                    _ScreenMemory.PortBAddressableLatch |= (byte)bit;
                else
                    _ScreenMemory.PortBAddressableLatch &= (byte)~bit;

                _ScreenMemory.WrapAddress = (_ScreenMemory.PortBAddressableLatch & 0x30) switch
                {
                    0x00 => 0x4000,
                    0x10 => 0x6000,
                    0x20 => 0x3000,
                    0x30 => 0x5800,
                    _ => throw new ArgumentOutOfRangeException()
                };
                _ScreenMemory.WrapOffset = 0x8000 - _ScreenMemory.WrapAddress;
            }
            else if (address >= 0xFE34 && address < 0xFE38)
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
                    _ScreenMemory.DisplayShadowRam = (accessControlRegister & 0x80) != 0;
                    break;

                case BBCModelType.Master128:
                case BBCModelType.MasterET:
                    _ScreenMemory.DisplayShadowRam = (accessControlRegister & 0x01) != 0;
                    break;

                default:
                    _ScreenMemory.DisplayShadowRam = false;
                    break;
            }
        }

        private byte ReadScreenData_Teletext()
        {
            int memoryAddress = _CRTCState.CharacterAddress;

            if (memoryAddress >= 0x8000)
                memoryAddress = (memoryAddress - _ScreenMemory.WrapOffset) & 0x7FFF;

            return ReadDisplayMemory(memoryAddress);
        }

        private byte ReadScreenData_NonTeletext()
        {
            if (_CRTCState.CharacterScanline >= 8)
                return 0;

            int memoryAddress = (_CRTCState.CharacterAddress << 3) + _CRTCState.CharacterScanline;

            if (memoryAddress >= 0x8000)
                memoryAddress = (memoryAddress - _ScreenMemory.WrapOffset) & 0x7FFF;

            return ReadDisplayMemory(memoryAddress);
        }

        private byte ReadDisplayMemory(int address)
        {
            if (_ScreenMemory.DisplayShadowRam)
            {
                if (address < 0x3000)
                {
                    if (_BBCModel == BBCModelType.Master128 || _BBCModel == BBCModelType.MasterET)
                    {
                        address -= 0xC000;
                        _AnalysisFrameState.FilingSystemRamDisplayFrame[address] = _DisplayState.FrameNumber;
                        return _ScreenMemory.FilingSystemRam[address];
                    }
                    else
                    {
                        _AnalysisFrameState.MemoryDisplayFrame[address] = _DisplayState.FrameNumber;
                        return _ScreenMemory.Memory[address];
                    }
                }
                else
                {
                    address -= 0x3000;
                    _AnalysisFrameState.ShadowRamDisplayFrame[address] = _DisplayState.FrameNumber;
                    return _ScreenMemory.ShadowRam[address];
                }
            }
            else
            {
                _AnalysisFrameState.MemoryDisplayFrame[address] = _DisplayState.FrameNumber;
                return _ScreenMemory.Memory[address];
            }
        }

        private void WriteDisplayData_32bits(byte value)
        {
            int index = (_CRTCState.DisplayScanline * DisplayState.FrameBufferStride * 2) + (_CRTCState.DisplayScanlinePos);
            var displayData = _ULAState.WriteDisplayDataTbl[value];
            Buffer.BlockCopy(displayData, 0, _DisplayState.FrameBuffer, index, 4);
            Buffer.BlockCopy(displayData, 0, _DisplayState.FrameBuffer, index + DisplayState.FrameBufferStride, 4);
            _CRTCState.DisplayScanlinePos += 4;
        }

        private void WriteDisplayData_64bits(byte value)
        {
            int index = (_CRTCState.DisplayScanline * DisplayState.FrameBufferStride * 2) + (_CRTCState.DisplayScanlinePos);
            var displayData = _ULAState.WriteDisplayDataTbl[value];
            Buffer.BlockCopy(displayData, 0, _DisplayState.FrameBuffer, index, 8);
            Buffer.BlockCopy(displayData, 0, _DisplayState.FrameBuffer, index + DisplayState.FrameBufferStride, 8);
            _CRTCState.DisplayScanlinePos += 8;
        }

        private void WriteDisplayData_Teletext(byte value)
        {
            if (_CRTCState.CharacterColumn >= 40)
                return;

            // update double-height row state
            if (_CRTCState.CharacterRow != _TeletextState.LastRowCounter)
            {
                if (_CRTCState.CharacterRow == 0)
                {
                    _TeletextState.DoubleHeightRow = TeletextState.DoubleHeightTopRow;
                }
                else if (_TeletextState.DoubleHeight)
                {
                    _TeletextState.DoubleHeightRow =
                        _TeletextState.DoubleHeightRow == TeletextState.DoubleHeightTopRow
                        ? TeletextState.DoubleHeightBottomRow
                        : TeletextState.DoubleHeightTopRow;
                }

                _TeletextState.LastRowCounter = _CRTCState.CharacterRow;
            }

            // reset state at start of scanline
            if (_CRTCState.CharacterColumn == 0)
            {
                _TeletextState.ForeColor = 7;
                _TeletextState.BackColor = 0;
                _TeletextState.ForeColorPending = 7;
                _TeletextState.DoubleHeight = false;
                _TeletextState.NextHoldGraphics = false;
                _TeletextState.NextHoldGraphicsChar = 32; // space
                _TeletextState.NextHoldMosaic = false;
                _TeletextState.NextGraphics = false;
                _TeletextState.NextFlash = false;
                _TeletextState.Mosaic = false;
            }

            // apply pending state changes
            _TeletextState.HoldGraphics = _TeletextState.NextHoldGraphics;
            _TeletextState.HoldGraphicsChar = _TeletextState.NextHoldGraphicsChar;
            _TeletextState.HoldMosaic = _TeletextState.NextHoldMosaic;
            _TeletextState.Graphics = _TeletextState.NextGraphics;
            _TeletextState.Flash = _TeletextState.NextFlash;

            if (value < 32)
                value += 128; // some programs use 7-bit control codes!

            if ((value & 32) != 0 && _TeletextState.Graphics)
            {
                _TeletextState.NextHoldGraphicsChar = value;
                _TeletextState.NextHoldMosaic = _TeletextState.Mosaic;
            }

            uint[,] fontBitmap;

            if (value >= 128 && value <= 159)
            {
                // control codes...
                if (!_TeletextState.HoldGraphics && value != 158)
                    _TeletextState.NextHoldGraphicsChar = 32; // SAA5050 teletext rendering bug

                switch (value)
                {
                    case 129: // alphanumeric red
                    case 130: // alphanumeric green
                    case 131: // alphanumeric yellow
                    case 132: // alphanumeric blue
                    case 133: // alphanumeric magenta
                    case 134: // alphanumeric cyan
                    case 135: // alphanumeric white
                        _TeletextState.ForeColorPending = (byte)(value - 128);
                        _TeletextState.NextGraphics = false;
                        _TeletextState.NextHoldGraphicsChar = 32; // space
                        break;

                    case 136: // flash
                        _TeletextState.NextFlash = true;
                        break;

                    case 137: // steady
                        _TeletextState.NextFlash = false;
                        _TeletextState.Flash = false;
                        break;

                    case 140: // normal height
                        if (_TeletextState.DoubleHeight)
                        {
                            _TeletextState.NextHoldGraphicsChar = 32; // space
                            _TeletextState.HoldGraphicsChar = _TeletextState.NextHoldGraphicsChar;
                        }
                        _TeletextState.DoubleHeight = false;
                        break;

                    case 141: // double height
                        if (!_TeletextState.DoubleHeight)
                        {
                            _TeletextState.NextHoldGraphicsChar = 32; // space
                            _TeletextState.HoldGraphicsChar = _TeletextState.NextHoldGraphicsChar;
                        }
                        _TeletextState.DoubleHeight = true;
                        break;

                    case 145: // graphics red
                    case 146: // graphics green
                    case 147: // graphics yellow
                    case 148: // graphics blue
                    case 149: // graphics magenta
                    case 150: // graphics cyan
                    case 151: // graphics white
                        _TeletextState.ForeColorPending = (byte)(value - 144);
                        _TeletextState.NextGraphics = true;
                        break;

                    case 152: // conceal display
                        _TeletextState.ForeColor = _TeletextState.BackColor;
                        _TeletextState.ForeColorPending = _TeletextState.BackColor;
                        break;

                    case 153: // contiguous graphics
                        _TeletextState.Mosaic = false;
                        break;

                    case 154: // mosaic graphics
                        _TeletextState.Mosaic = true;
                        break;

                    case 156: // black background
                        _TeletextState.BackColor = 0;
                        break;

                    case 157: // new background
                        _TeletextState.BackColor = _TeletextState.ForeColor;
                        break;

                    case 158: // hold graphics
                        _TeletextState.NextHoldGraphics = true;
                        _TeletextState.HoldGraphics = true;
                        break;

                    case 159: // release graphics
                        _TeletextState.NextHoldGraphics = false;
                        break;
                }

                if (_TeletextState.HoldGraphics && _TeletextState.Graphics)
                {
                    value = _TeletextState.HoldGraphicsChar;
                    fontBitmap = _TeletextState.HoldMosaic ? _TeletextState.MosaicFont : _TeletextState.GraphicFont;
                }
                else
                {
                    value = 32; // space
                    fontBitmap = _TeletextState.Graphics ? (_TeletextState.Mosaic ? _TeletextState.MosaicFont : _TeletextState.GraphicFont) : _TeletextState.TextFont;
                }
            }
            else
            {
                fontBitmap = _TeletextState.Graphics ? (_TeletextState.Mosaic ? _TeletextState.MosaicFont : _TeletextState.GraphicFont) : _TeletextState.TextFont;
            }

            value &= 0x7F; // clear top bit

            // determine foreground & background colors, considering flash state
            byte foreColor;
            if (_TeletextState.Flash && !_TeletextState.FlashOn)
                foreColor = _TeletextState.BackColor;
            else
                foreColor = _TeletextState.ForeColor;

            byte backColor = _TeletextState.BackColor;

            // convert character code to font index
            int fontIndex = Math.Max(value - 32, 0);

            // get offset into frame buffer, advancing DisplayScanlinePos
            int frameBufferOffset = (_CRTCState.DisplayScanline * DisplayState.FrameBufferStride * 2) + _CRTCState.DisplayScanlinePos;
            _CRTCState.DisplayScanlinePos += 6;

            // determine scanline index
            int characterScanlineEven = (_CRTCState.CharacterScanline & ~1); 
            int fontScanlineIndex;
            if (_TeletextState.DoubleHeight)
                fontScanlineIndex = (characterScanlineEven >> 1) + (_TeletextState.DoubleHeightRow == TeletextState.DoubleHeightTopRow ? 0 : 10);
            else
                fontScanlineIndex = characterScanlineEven;

            // fetch font scanline
            uint characterScanline = fontBitmap[fontIndex, fontScanlineIndex];

            // render 12 pixels (6 bytes)
            int frameBufferIndex = frameBufferOffset;
            uint pixelMask = 0x800;
            while (pixelMask != 0)
            {
                uint hiColor = ((characterScanline & pixelMask) != 0) ? foreColor : backColor;
                pixelMask >>= 1;

                uint loColor = ((characterScanline & pixelMask) != 0) ? foreColor : backColor;
                pixelMask >>= 1;

                _DisplayState.FrameBuffer[frameBufferIndex++] = (byte)((hiColor << 4) | loColor);
            }

            // if normal height text, fetch font scanline, one scanline below
            if (!_TeletextState.DoubleHeight)
                characterScanline = fontBitmap[fontIndex, fontScanlineIndex + 1];

            // render 12 pixels (6 bytes), one scanline below
            frameBufferIndex = frameBufferOffset + DisplayState.FrameBufferStride;
            pixelMask = 0x800;
            while (pixelMask != 0)
            {
                uint hiColor = ((characterScanline & pixelMask) != 0) ? foreColor : backColor;
                pixelMask >>= 1;

                uint loColor = ((characterScanline & pixelMask) != 0) ? foreColor : backColor;
                pixelMask >>= 1;

                _DisplayState.FrameBuffer[frameBufferIndex++] = (byte)((hiColor << 4) | loColor);
            }

            // commit pending foreground color
            _TeletextState.ForeColor = _TeletextState.ForeColorPending;
        }

        private void BuildWriteDisplayDataTbl_4bpp(int bitsPerPixel)
        {
            Debug.Assert(bitsPerPixel == 1);
            int shiftCount = 8 / bitsPerPixel;
            for (int value = 0; value < 256; value++)
            {
                var writeDisplayData = _ULAState.WriteDisplayDataTbl[value];

                int shiftRegister = value;
                for (int i = 0; i < shiftCount; i += 2)
                {
                    // first entry
                    int firstPaletteIndex = _ULAState.PaletteIndices[shiftRegister & 0xFF];

                    int firstPaletteEntry = _ULAState.ColorPalette[firstPaletteIndex];
                    if (firstPaletteEntry > 7) // flashing color?
                    {
                        firstPaletteEntry &= 0x7;
                        if ((_ULAState.ControlRegister & 0x1) != 0)
                            firstPaletteEntry ^= 0x7;
                    }

                    shiftRegister = (shiftRegister << 1) | 0x1;

                    // second entry
                    int secondPaletteIndex = _ULAState.PaletteIndices[shiftRegister & 0xFF];

                    int secondPaletteEntry = _ULAState.ColorPalette[secondPaletteIndex];
                    if (secondPaletteEntry > 7) // flashing color?
                    {
                        secondPaletteEntry &= 0x7;
                        if ((_ULAState.ControlRegister & 0x1) != 0)
                            secondPaletteEntry ^= 0x7;
                    }

                    shiftRegister = (shiftRegister << 1) | 0x1;

                    byte pixelPair = (byte)((firstPaletteEntry << 4) | secondPaletteEntry);
                    writeDisplayData[i >> 1] = pixelPair;
                }
            }
        }

        private void BuildWriteDisplayDataTbl_8bpp(int bitsPerPixel)
        {
            Debug.Assert(bitsPerPixel == 1 || bitsPerPixel == 2);
            int shiftCount = 8 / bitsPerPixel;
            for (int value = 0; value < 256; value++)
            {
                var writeDisplayData = _ULAState.WriteDisplayDataTbl[value];

                int shiftRegister = value;
                for (int i = 0; i < shiftCount; i++)
                {
                    int paletteIndex = _ULAState.PaletteIndices[shiftRegister & 0xFF];

                    int paletteEntry = _ULAState.ColorPalette[paletteIndex];
                    if (paletteEntry > 7) // flashing color?
                    {
                        paletteEntry &= 0x7;
                        if ((_ULAState.ControlRegister & 0x1) != 0)
                            paletteEntry ^= 0x7;
                    }

                    shiftRegister = (shiftRegister << 1) | 0x1;

                    byte pixelPair = (byte)((paletteEntry << 4) | paletteEntry);
                    writeDisplayData[i] = pixelPair;
                }
            }
        }

        private void BuildWriteDisplayDataTbl_16bpp(int bitsPerPixel)
        {
            Debug.Assert(bitsPerPixel == 2 || bitsPerPixel == 4);
            int shiftCount = 8 / bitsPerPixel;
            for (int value = 0; value < 256; value++)
            {
                var writeDisplayData = _ULAState.WriteDisplayDataTbl[value];

                int shiftRegister = value;
                for (int i = 0; i < shiftCount; i++)
                {
                    int paletteIndex = _ULAState.PaletteIndices[shiftRegister & 0xFF];

                    int paletteEntry = _ULAState.ColorPalette[paletteIndex];
                    if (paletteEntry > 7) // flashing color?
                    {
                        paletteEntry &= 0x7;
                        if ((_ULAState.ControlRegister & 0x1) != 0)
                            paletteEntry ^= 0x7;
                    }

                    shiftRegister = (shiftRegister << 1) | 0x1;

                    var pixelPair = (byte)((paletteEntry << 4) | paletteEntry);
                    int j = i << 1;
                    writeDisplayData[j++] = pixelPair;
                    writeDisplayData[j] = pixelPair;
                }
            }
        }

        private void BuildWriteDisplayDataTbl_32bpp(int bitsPerPixel)
        {
            Debug.Assert(bitsPerPixel == 4);
            int shiftCount = 8 / bitsPerPixel;
            for (int value = 0; value < 256; value++)
            {
                var writeDisplayData = _ULAState.WriteDisplayDataTbl[value];

                int shiftRegister = value;
                for (int i = 0; i < shiftCount; i++)
                {
                    int paletteIndex = _ULAState.PaletteIndices[shiftRegister & 0xFF];

                    int paletteEntry = _ULAState.ColorPalette[paletteIndex];
                    if (paletteEntry > 7) // flashing color?
                    {
                        paletteEntry &= 0x7;
                        if ((_ULAState.ControlRegister & 0x1) != 0)
                            paletteEntry ^= 0x7;
                    }

                    shiftRegister = (shiftRegister << 1) | 0x1;

                    var pixelPair = (byte)((paletteEntry << 4) | paletteEntry);
                    int j = i << 2;
                    writeDisplayData[j++] = pixelPair;
                    writeDisplayData[j++] = pixelPair;
                    writeDisplayData[j++] = pixelPair;
                    writeDisplayData[j] = pixelPair;
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
                        _TeletextState.TextFont[ch, i] = 0;

                    for (int i = 2; i < 20; i++)
                    {
                        int loByte = ms.ReadByte();
                        if (loByte == -1)
                            throw new EndOfStreamException();

                        int hiByte = ms.ReadByte();
                        if (hiByte == -1)
                            throw new EndOfStreamException();

                        _TeletextState.TextFont[ch, i] = (uint)((hiByte << 8) | loByte);
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
                        _TeletextState.GraphicFont[ch, i] = top;

                    for (int i = 6; i < 14; i++)
                        _TeletextState.GraphicFont[ch, i] = middle;

                    for (int i = 14; i < 20; i++)
                        _TeletextState.GraphicFont[ch, i] = bottom;

                    // mosaic fonts
                    top &= 0x03CF;
                    middle &= 0x03CF;
                    bottom &= 0x03CF;

                    for (int i = 0; i < 4; i++)
                        _TeletextState.MosaicFont[ch, i] = top;

                    for (int i = 4; i < 6; i++)
                        _TeletextState.MosaicFont[ch, i] = 0;

                    for (int i = 6; i < 12; i++)
                        _TeletextState.MosaicFont[ch, i] = middle;

                    for (int i = 12; i < 14; i++)
                        _TeletextState.MosaicFont[ch, i] = 0;

                    for (int i = 14; i < 18; i++)
                        _TeletextState.MosaicFont[ch, i] = bottom;

                    for (int i = 18; i < 20; i++)
                        _TeletextState.MosaicFont[ch, i] = 0;
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
            public required int WritesBeforeDisplayReadNext;
            public required int WritesAfterDisplayReadNext;
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

        // analysis frame state
        private struct AnalysisFrameState
        {
            public AnalysisFrameState() { }

            public FrameSettings? FrameSettings;
            public int WritesBeforeDisplayRead;
            public int WritesAfterDisplayRead;
            public int WritesBeforeDisplayReadNext;
            public int WritesAfterDisplayReadNext;
            public int StartInstructionIndex;
            public int EndInstructionIndex;
            public int StartCycleCount;
            public int FrameNumber = 0;
            
            public int[] MemoryDisplayFrame = [];
            public int[] ShadowRamDisplayFrame = [];
            public int[] FilingSystemRamDisplayFrame = [];
        }
        private AnalysisFrameState _AnalysisFrameState = new();

        // display state
        private struct DisplayState
        {
            public DisplayState()
            {
                WriteDisplayDataFunc = WriteDisplayData_Void;
                ReadScreenDataFunc = ReadScreenData_Void;
            }

            private void WriteDisplayData_Void(byte value)
            {
            }

            private byte ReadScreenData_Void()
            {
                return 0;
            }

            public const int MaxWidth = 1024;
            public const int MaxHeight = 312;
            public const float AspectRatio_Teletext = 0.813f;
            public const float AspectRatio_NonTeletext = 0.46f;
            public const int FrameBufferStride = MaxWidth / 2;

            public WriteBitmapData WriteDisplayDataFunc;
            public ReadScreenData ReadScreenDataFunc;

            public int FrameNumber;
            public int StartCycleCount;
            public int EndCycleCount;
            public int Width;
            public float AspectRatio;
            public int CharacterCycleCount;
            public int FirstDisplayScanline;
            public int LastDisplayScanline;
            public bool CaptureTeletextFrame;
            public byte[] FrameBuffer = [];
        }
        private DisplayState _DisplayState = new();

        // ULA state
        private struct ULAState
        {
            public ULAState()
            {
                for (int i = 0; i < 256; i++)
                {
                    PaletteIndices[i] = (byte)(
                        ((i >> 4) & 0x8) | 
                        ((i >> 3) & 0x4) | 
                        ((i >> 2) & 0x2) | 
                        ((i >> 1) & 0x1));
                 
                    WriteDisplayDataTbl[i] = new byte[8];
                }
            }

            public byte ControlRegister;
            public byte[] ColorPalette = new byte[16];
            public byte[] PaletteIndices = new byte[256];

            public int CyclesPerCharacter;
            public bool TeletextMode;
            public bool ControlRegisterModified;
            public bool WriteDisplayDataTblInvalid;
            public byte[][] WriteDisplayDataTbl = new byte[256][];

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
        private ULAState _ULAState = new();

        // CRTC state
        private struct CRTCState
        {
            public const int DisplayFieldEven = 0;
            public const int DisplayFieldOdd = 1;

            public byte RegisterSelect;
            public byte Register0_HorizontalTotal;
            public byte Register1_HorizontalDisplayed;
            public byte Register2_HorizontalSyncPos;
            public byte Register3_SyncWidth;
            public byte Register4_VerticalTotal;
            public byte Register5_VerticalTotalAdjust;
            public byte Register6_VerticalDisplayed;
            public byte Register7_VerticalSyncPos;
            public byte Register8_InterlaceAndDelay;
            public byte Register9_ScanlinesPerCharacter;
            public byte Register12_ScreenStartHigh;
            public byte Register13_ScreenStartLow;
            public bool RegisterModified;

            public int CharacterAddress;
            public int CharacterRowAddress;
            public int CharacterColumn;
            public int CharacterRow;
            public int CharacterScanline;
            public int CharacterScanlineIncrement;
            public int CharacterScanlineReset;
            public int ScanlinesPerCharacterAdjust;
            public int DisplayScanline;
            public int DisplayScanlinePos;
            public int DisplayField;
            public bool SplitScreen;
            public bool VideoOutputEnabled;
        }
        private CRTCState _CRTCState = new();

        // teletext state
        private struct TeletextState
        {
            public TeletextState()
            {
            }

            public const int FlashOffFrameCount = 13;
            public const int FlashOnFrameCount = 37;
            public const int DoubleHeightTopRow = 0;
            public const int DoubleHeightBottomRow = 1;

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
            public int DoubleHeightRow;

            public uint[,] TextFont = new uint[96, 20];
            public uint[,] GraphicFont = new uint[96, 20];
            public uint[,] MosaicFont = new uint[96, 20];
        }
        private TeletextState _TeletextState = new();

        // screen memory 
        private struct ScreenMemory
        {
            public ScreenMemory() { }

            public bool DisplayShadowRam;
            public int StartAddress;
            public int WrapAddress;
            public int WrapOffset;
            public int Size;
            public byte PortBAddressableLatch;

            public byte[] Memory = [];
            public byte[] ShadowRam = [];
            public byte[] FilingSystemRam = [];
        }
        private ScreenMemory _ScreenMemory = new();
    }
}