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
    public class MemoryPageTraits
    {
        public static int PageSize(MemoryPage memoryPage)
        {
            return _PageSizes[(int)memoryPage];
        }

        public static ushort PageStartAddress(MemoryPage memoryPage)
        {
            return _PageStartAddress[(int)memoryPage];
        }

        private static int[] _PageSizes = [
            16384, // PageRom0
            16384, // PageRom1
            16384, // PageRom2
            16384, // PageRom3
            16384, // PageRom4
            16384, // PageRom5
            16384, // PageRom6
            16384, // PageRom7
            16384, // PageRom8
            16384, // PageRom9
            16384, // PageRom10
            16384, // PageRom11
            16384, // PageRom12
            16384, // PageRom13
            16384, // PageRom14
            16384, // PageRom15
            65536, // WholeRam
            20480, // ShadowRam
            12288, // PrivateRam
            8192,  // FilingSystemRam
            256,   // HiddenRam
        ];

        private static ushort[] _PageStartAddress = [
            0x8000, // PageRom0
            0x8000, // PageRom1
            0x8000, // PageRom2
            0x8000, // PageRom3
            0x8000, // PageRom4
            0x8000, // PageRom5
            0x8000, // PageRom6
            0x8000, // PageRom7
            0x8000, // PageRom8
            0x8000, // PageRom9
            0x8000, // PageRom10
            0x8000, // PageRom11
            0x8000, // PageRom12
            0x8000, // PageRom13
            0x8000, // PageRom14
            0x8000, // PageRom15
            0x0,    // WholeRam
            0x3000, // ShadowRam
            0x8000, // PrivateRam
            0xC000, // FilingSystemRam
            0x0,    // HiddenRam
        ];
    }
}