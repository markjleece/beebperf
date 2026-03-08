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

namespace BeebPerf
{
    public class LabelResolver
    {
        public void Initialize(List<model.LabelsFile> labelsFiles)
        {
            // count labels
            int labelCount = 0;
            foreach (var labelsFile in labelsFiles)
                if (labelsFile.Status == model.LabelsFileStatus.Loaded && labelsFile.Enabled)
                    labelCount += labelsFile.Labels.Count;

            // populate _SortedAddresses & _LabelsByAddress
            _SortedAddresses = new(labelCount);
            _LabelsByAddress = new((int)double.Ceiling((double)labelCount / 0.72));
            foreach (var labelsFile in labelsFiles)
            {
                if (labelsFile.Status == model.LabelsFileStatus.Loaded && labelsFile.Enabled)
                {
                    foreach (var label in labelsFile.Labels)
                    {
                        _SortedAddresses.Add(label.Address);
                        _LabelsByAddress[label.Address] = label.Name;
                    }
                }
            }
            _SortedAddresses.Sort();
        }

        public string Resolve(model.CanonicalAddress address)
        {
            return _LabelsByAddress.TryGetValue(address.Address, out string? name) ? name : string.Empty;
        }

        public string Resolve(ushort address)
        {
            return _LabelsByAddress.TryGetValue(address, out string? name) ? name : string.Empty;
        }

        public string ResolveWithOffset(ushort address)
        {
            int index = _SortedAddresses.BinarySearch(address);
            if (index >= 0)
                return Resolve(address);

            index = ~index - 1;
            if (index >= 0)
            {
                ushort lowerAddress = _SortedAddresses[index];
                int offset = address - lowerAddress;
                if (offset < 0x100)
                    return $"{Resolve(lowerAddress)}+{offset}";
            }

            return string.Empty;
        }

        private Dictionary<ushort, string> _LabelsByAddress = new();
        private List<ushort> _SortedAddresses = new();
    }
}