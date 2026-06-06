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
    // Metric - used to define a metric's span and threshold settings.
    //
    // The span can be defined by setting its type to 
    // start & end address, routine address, of JSR address.
    //
    public class Metric
    {
        public enum MetricType : int
        {
            None = 0,
            StartAndEndAddresses = 1,
            RoutineAddress = 2,
            JSRAddress = 3
        }

        public Metric Clone()
        {
            return new Metric()
            {
                Type = Type,
                Name = Name,
                StartAddress = StartAddress,
                EndAddress = EndAddress,
                ThresholdCycles = ThresholdCycles,
            };
        }

        static public string Serialize(Metric metric)
        {
            return 
                $"{(int)metric.Type};" +
                $"{metric.Name};" +
                $"{metric.StartAddress.Address};" +
                $"{(int)metric.StartAddress.Page};" +
                $"{metric.EndAddress.Address};" + 
                $"{(int)metric.EndAddress.Page};" + 
                $"{metric.ThresholdCycles}";
        }

        static public Metric DeSerialize(string encoding)
        {
            var parts = encoding.Split(';');
            return new Metric()
            {
                Type = (MetricType)int.Parse(parts[0]),
                Name = parts[1],
                StartAddress = new CanonicalAddress(ushort.Parse(parts[2]), (MemoryPage)int.Parse(parts[3])),
                EndAddress = new CanonicalAddress(ushort.Parse(parts[4]), (MemoryPage)int.Parse(parts[5])),
                ThresholdCycles = int.Parse(parts[6]),
            };
        }

        public bool Match(Instruction[] instructions)
        {
            return Type switch
            {
                Metric.MetricType.StartAndEndAddresses =>
                    AddressMatchesInstruction(StartAddress, instructions) &&
                    AddressMatchesInstruction(EndAddress, instructions),
                Metric.MetricType.RoutineAddress =>
                    AddressMatchesInstruction(StartAddress, instructions),
                Metric.MetricType.JSRAddress =>
                    AddressMatchesJSRInstruction(StartAddress, instructions),
                _ => false
            };
        }

        private static bool AddressMatchesInstruction(CanonicalAddress address, Instruction[] instructions)
        {
            for (int i = 0; i < instructions.Length; i++)
            {
                ref var instruction = ref instructions[i];
                if (instruction.IsInstruction &&
                    instruction.OpcodeAddress.Equals(address))
                    return true;
            }
            return false;
        }

        private static bool AddressMatchesJSRInstruction(CanonicalAddress address, Instruction[] instructions)
        {
            for (int i = 0; i < instructions.Length; i++)
            {
                ref var instruction = ref instructions[i];
                if (instruction.IsInstruction &&
                    instruction.OpcodeAddress.Equals(address) &&
                    instruction.Opcode == 0x20/*JSR*/)
                    return true;
            }
            return false;
        }


        public required MetricType Type;
        public required string Name;
        public required CanonicalAddress StartAddress;
        public required CanonicalAddress EndAddress;
        public required int ThresholdCycles;
    }
}