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

using System.Diagnostics;
using static BeebPerf.Model;

namespace BeebPerf
{
    public struct CanonicalAddress : IComparable<CanonicalAddress>, IEquatable<CanonicalAddress>
    {
        public CanonicalAddress(ushort address, Model.MemoryPage page)
        {
            _PageAddress = ((int)page << 16) | address;
        }

        public CanonicalAddress Offset(int addressOffset)
        {
            return new CanonicalAddress((ushort)(Address + addressOffset), Page);
        }

        public int CompareTo(CanonicalAddress other)
        {
            return _PageAddress.CompareTo(other._PageAddress);
        }

        public bool Equals(CanonicalAddress other)
        {
            return _PageAddress == other._PageAddress;
        }

        public override int GetHashCode()
        {
            return _PageAddress;
        }

        public override string ToString()
        {
            int address = (_PageAddress & 0xFFFF);
            return $"&{address:X4}";
        }

        public ushort Address 
        {
            get => (ushort)_PageAddress;
        }

        public Model.MemoryPage Page
        {
            get => (Model.MemoryPage)(_PageAddress >> 16);
        }

        public ushort PageOffset
        {
            get
            {
                int address = (_PageAddress & 0xFFFF);
                int page = (_PageAddress >> 16);
                switch ((MemoryPage)page)
                {
                    case Model.MemoryPage.WholeRam:
                    case Model.MemoryPage.HiddenRam:
                        return (ushort)address;

                    case Model.MemoryPage.ShadowRam:
                        return (ushort)(address - 0x3000);

                    case Model.MemoryPage.PrivateRam:
                        return (ushort)(address - 0x8000);

                    case Model.MemoryPage.FilingSystemRam:
                        return (ushort)(address - 0xC000);

                    default:
                        Debug.Assert(page < 16);
                        return (ushort)(address - 0x8000);
                }
            }
        }

        private int _PageAddress;
    }

    struct CanonicalAddressPair : IEquatable<CanonicalAddressPair>
    {
        public CanonicalAddressPair(CanonicalAddress opcodeAddress, CanonicalAddress destinationAddress)
        {
            FirstCanonicalAddress = opcodeAddress;
            SecondCanonicalAddress = destinationAddress;

            _HashCode = unchecked(opcodeAddress.GetHashCode() * 31 + destinationAddress.GetHashCode());
        }

        public bool Equals(CanonicalAddressPair other)
        {
            return (FirstCanonicalAddress.Equals(other.FirstCanonicalAddress) &&
                    SecondCanonicalAddress.Equals(other.SecondCanonicalAddress));
        }

        public override int GetHashCode()
        {
            return _HashCode;
        }

        public readonly CanonicalAddress FirstCanonicalAddress;
        public readonly CanonicalAddress SecondCanonicalAddress;
        private readonly int _HashCode;
    }

}