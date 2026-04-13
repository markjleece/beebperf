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
    public struct CanonicalAddress : IComparable<CanonicalAddress>, IEquatable<CanonicalAddress>
    {
        public CanonicalAddress(ushort address, MemoryPage page)
        {
            _PageAddress = ((int)page << 16) | address;
        }

        public CanonicalAddress Offset(int addressOffset)
        {
            return new CanonicalAddress((ushort)(Address + addressOffset), Page);
        }

        public int CompareTo(CanonicalAddress other)
        {
            int diff = Address.CompareTo(other.Address);
            if (diff == 0)
                diff = Page.CompareTo(other.Page);
            return diff;
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

        public MemoryPage Page
        {
            get => (MemoryPage)(_PageAddress >> 16);
        }

        public ushort PageOffset
        {
            get => (ushort)(Address - MemoryPageTraits.PageStartAddress(Page));
        }

        public bool IsValid()
        {
            var page = Page;
            ushort address = Address;

            var pageStartAddress = MemoryPageTraits.PageStartAddress(page);
            var pageSize = MemoryPageTraits.PageSize(page);

            return (address >= pageStartAddress && address < pageStartAddress + pageSize);
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