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

namespace BeebPerf.model
{
    // 
    // MetricIteration - represents an metric's iteration
    //
    public class MetricIteration
    {
        public struct DisplayFrameSpan
        {
            public required int FrameNumber;
            public required int StartCycleCount;
            public required int EndCycleCount;
        }

        public required int IterationNumber; // 1, 2, 3...
        public required int StartCycleCount;
        public required int EndCycleCount;
        public required int WritesBeforeDisplayRead;
        public required int WritesAfterDisplayRead;
        public required int WritesBeforeDisplayReadNext; // only used during video analysis
        public required int WritesAfterDisplayReadNext; // only used during video analysis
        public int DisplayFrameOffset;
        public int DisplayFrameIndex; // display frame metrics apply to
        public DisplayFrameSpan[] DisplayFrameSpans = [];
    }
}
