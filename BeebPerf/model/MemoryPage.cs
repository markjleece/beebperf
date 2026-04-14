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
    // Memory page
    //
    public enum MemoryPage
    {
        PageRom0 = 0,
        PageRom1 = 1,
        PageRom2 = 2,
        PageRom3 = 3,
        PageRom4 = 4,
        PageRom5 = 5,
        PageRom6 = 6,
        PageRom7 = 7,
        PageRom8 = 8,
        PageRom9 = 9,
        PageRom10 = 10,
        PageRom11 = 11,
        PageRom12 = 12,
        PageRom13 = 13,
        PageRom14 = 14,
        PageRom15 = 15,
        WholeRam = 16,
        ShadowRam = 17,
        PrivateRam = 18,
        FilingSystemRam = 19,
        HiddenRam = 20,
        Count = 21
    }
}