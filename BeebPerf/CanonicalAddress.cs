using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using static BeebPerf.Model;

namespace BeebPerf
{
    public struct CanonicalAddress : IComparable<CanonicalAddress>, IEquatable<CanonicalAddress>
    {
        public CanonicalAddress(ushort address, byte page)
        {
            _PageAddress = (page << 16) | address;
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

        public ushort Address 
        {
            get => (ushort)_PageAddress;
        }

        public byte Page
        {
            get => (byte)(_PageAddress >> 16);
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