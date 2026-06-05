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

class Program
{
    static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Usage: ExtractLabels <listingfile>");
            return;
        }

        // read all lines from listing file
        var lines = File.ReadAllLines(args[0]);

        // extract labels from assembly listing
        var labels = ExtractLabels.Extract(lines, includeAssignments: false, addressesFrom: 0xC000);

        // output labels in BeebAsm.exe -labels format: [{'label1':1234L,'label2':5678L}]
        Console.Write("[{");
        bool first = true;
        foreach (var label in labels)
        {
            if (!first) Console.Write(",");
            Console.Write($"'{label.Name}':{label.Address}L");
            first = false;
        }
        Console.Write("}]");
    }
}
