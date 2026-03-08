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
    public class Routine
    {
        public Routine()
        {
            StartAddress = new CanonicalAddress();
            EndAddress = StartAddress.Address;
            Label = String.Empty;
        }

        public Routine(CanonicalAddress address, string label)
        {
            Label = label;
            StartAddress = address;
            EndAddress = address.Address;
        }

        public void ClearMetrics()
        {
            MetricsByStack.Clear();
            AggregateMetrics.Clear();
        }

        public bool HotRoutine;
        public string Label;
        public CanonicalAddress StartAddress;
        public int EndAddress;
        public Dictionary<CallStack, CPUMetrics> MetricsByStack = new();
        public CPUMetrics AggregateMetrics = new();
        public List<StackFrame> StackFrames = new();
    }
}