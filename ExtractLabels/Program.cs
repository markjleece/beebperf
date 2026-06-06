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
        // command line
        bool showHelp = (args.Length == 0);
        bool includeAssignments = true;
        int addressesFrom = 0;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-h" || args[i] == "--help")
            {
                showHelp = true;
                break;
            }
            else if (args[i] == "-i")
            {
                includeAssignments = false;
            }
            else if (args[i] == "-r" && i + 1 < args.Length)
            {
                if (int.TryParse(args[i + 1], System.Globalization.NumberStyles.HexNumber, null, out int addr))
                {
                    if (addr < 0 || addr > 0xFFFF)
                    {
                        Console.WriteLine($"Address out of range: {args[i + 1]:X4}");
                        return;
                    }
                    addressesFrom = addr;
                    i++;
                }
                else
                {
                    Console.WriteLine($"Invalid hex address: {args[i + 1]}");
                    return;
                }
            }
            else if (args[i].StartsWith('-'))
            {
                Console.WriteLine($"Unknown option: {args[i]}");
                showHelp = true;
                break;
            }
        }

        if (showHelp)
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  ExtractLabels <options> <listingfile>");
            Console.WriteLine("Options:");
            Console.WriteLine("  -h, --help     Show this help message");
            Console.WriteLine("  -i             Ignore labels assigned a value (e.g. label = XXXX)");
            Console.WriteLine("  -r XXXX        Ignore labels with address below XXXX");
            return;
        }

        // read all lines from listing file
        string[] lines;
        try
        {
            lines = File.ReadAllLines(args[args.Length - 1]);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading listing file: {ex.Message}");
            return;
        }

        // extract labels from assembly listing
        var labels = ExtractLabels.Extract(lines, includeAssignments: includeAssignments, addressesFrom: addressesFrom);

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
