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
    public class SortedCanonicalAddresses
    {
        private readonly List<CanonicalAddress> _List = new();

        public SortedCanonicalAddresses()
        { 
        }

        public SortedCanonicalAddresses(List<CanonicalAddress> items)
        {
            _List = items.ToList();
            _List.Sort();
        }

        public void Add(CanonicalAddress value)
        {
            int index = _List.BinarySearch(value);
            if (index < 0)
            {
                index = ~index; // insertion point
                _List.Insert(index, value);
            }
        }

        public void Remove(CanonicalAddress value)
        {
            int index = _List.BinarySearch(value);
            if (index >= 0)
            {
                _List.RemoveAt(index);
            }
        }

        public bool Contains(CanonicalAddress value)
        {
            return _List.BinarySearch(value) >= 0;
        }

        public CanonicalAddress Find(CanonicalAddress value)
        {
            if (_List.Count > 0)
            {
                int index = _List.BinarySearch(value);
                if (index >= 0 && index < _List.Count)
                {
                    return _List[index];
                }

                index = ~index;

                if (index > 0)
                {
                    index--;
                    CanonicalAddress result = _List[index];
                    if (result.Page == value.Page)
                        return result; // partial match
                }
            }

            return new CanonicalAddress(0, value.Page); // no match
        }
    }
}