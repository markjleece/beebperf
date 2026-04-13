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
    public class CanonicalAddressMap<TValue>
    {
        public bool ContainsKey(CanonicalAddress key)
        {
            return Find(key) != null;
        }

        public bool TryGetValue(CanonicalAddress key, out TValue value)
        {
            var found = Find(key);
            if (found != null)
            {
                value = found;
                return true;
            }

            value = default(TValue)!;
            return false;
        }

        public void Add(CanonicalAddress key, TValue value)
        {
            var page = key.Page;

            var entries = _PageEntries[(int)page];
            if (entries == null)
            {
                entries = new object[MemoryPageTraits.Size(page)];
                _PageEntries[(int)page] = entries;
            }

            int address = key.Address - MemoryPageTraits.BaseAddress(page);

            if (address < 0 || address >= MemoryPageTraits.Size(page))
                throw new ArgumentException("invalid key");

            if (entries[address] != null)
                throw new ArgumentException("entry already exist");

            _Keys.Add(key);

            entries[address] = value;
        }

        public TValue this[CanonicalAddress key]
        {
            get => GetValue(key);
        }

        public TValue GetValue(CanonicalAddress key)
        {
            var found = Find(key);
            if (found == null)
                throw new ArgumentException("invalid key");

            return found;
        }

        public IEnumerable<CanonicalAddress> Keys
        {
            get => _Keys;
        }

        public IEnumerable<TValue> Values
        {
            get
            {
                foreach (var key in _Keys)
                    yield return GetValue(key);
            }
        }

        private TValue? Find(CanonicalAddress key)
        {
            var page = key.Page;

            var entries = _PageEntries[(int)page];
            if (entries == null)
                return default(TValue);

            int address = key.Address - MemoryPageTraits.BaseAddress(page);

            if (address < 0 || address >= MemoryPageTraits.Size(page))
                throw new ArgumentException("invalid key");

            return (TValue?)entries[address];
        }

        private List<CanonicalAddress> _Keys = [];
        private object?[][] _PageEntries = new object[(int)MemoryPage.Count][];
    }
}