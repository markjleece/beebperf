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

using System.Text;

class Program
{
    static int Main(string[] args)
    {
        // command line options...
        bool showHelp = (args.Length == 0);
        bool includeAssignments = true;
        int addressesFrom = 0;
        string outputFile = string.Empty;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-h" || args[i] == "--help")
            {
                showHelp = true;
                break;
            }
            else if (args[i] == "-o" && i + 1 < args.Length)
            {
                outputFile = args[++i];
            }
            else if (args[i] == "-i")
            {
                includeAssignments = false;
            }
            else if (args[i] == "-r" && i + 1 < args.Length)
            {
                string hexAddress = args[++i];
                if (hexAddress.StartsWith("$") || hexAddress.StartsWith("&"))
                    hexAddress = hexAddress.Substring(1);
                else if (hexAddress.StartsWith("0x"))
                    hexAddress = hexAddress.Substring(2);

                if (int.TryParse(hexAddress, System.Globalization.NumberStyles.HexNumber, null, out int address))
                {
                    if (address < 0 || address > 0xFFFF)
                    {
                        Console.WriteLine($"ERROR: Address out of range: '{args[i]:X4}'");
                        return 1;
                    }
                    addressesFrom = address;
                }
                else
                {
                    Console.WriteLine($"ERROR: Invalid hex address: '{args[i]}'");
                    return 1;
                }
            }
            else if (args[i].StartsWith('-'))
            {
                Console.WriteLine($"ERROR: Unknown option: '{args[i]}'");
                showHelp = true;
                break;
            }
        }

        if (outputFile.Length == 0 && !showHelp)
        {
            Console.WriteLine($"ERROR: No output file specified");
            showHelp = true;
        }

        if (showHelp)
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  ExtractLabels <options> <listingfile>");
            Console.WriteLine("Options:");
            Console.WriteLine("  -h, --help         Show this help message");
            Console.WriteLine("  -o <file>          Output file");
            Console.WriteLine("  -i                 Ignore labels defined with an assignment (e.g. label = $xxxx)");
            Console.WriteLine("  -r <hex-address>   Ignore labels with addresses below <hex-address>");
            return 1;
        }

        // read all lines from listing file
        string inputFile = args[args.Length - 1];
        string[] lines;
        try
        {
            lines = File.ReadAllLines(inputFile);
        }
        catch (UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"ERROR: Access denied reading '{inputFile}'.");
            return 1;
        }
        catch (DirectoryNotFoundException)
        {
            Console.Error.WriteLine($"ERROR: Directory not found for '{inputFile}'.");
            return 1;
        }
        catch (FileNotFoundException)
        {
            Console.Error.WriteLine($"ERROR: File not found: '{inputFile}'.");
            return 1;
        }
        catch (PathTooLongException)
        {
            Console.Error.WriteLine($"ERROR: Path is too long: '{inputFile}'.");
            return 1;
        }
        catch (IOException)
        {
            Console.Error.WriteLine($"ERROR: I/O failure reading '{inputFile}'.");
            return 1;
        }
        catch (ArgumentException)
        {
            Console.Error.WriteLine($"ERROR: Invalid path: '{inputFile}'.");
            return 1;
        }
        catch (OutOfMemoryException)
        {
            Console.Error.WriteLine($"ERROR: File '{inputFile}' is too large to load into memory.");
            return 1;
        }

        // extract labels
        var labels = ExtractLabels.Extract(lines, includeAssignments: includeAssignments, addressesFrom: addressesFrom);

        // format labels to BeebAsm.exe -labels format: [{'label1':1234L,'label2':5678L}]
        var sb = new StringBuilder();
        sb.Append("[{");
        bool first = true;
        foreach (var label in labels)
        {
            if (!first) sb.Append(",");
            sb.Append($"'{label.Name}':{label.Address}L");
            first = false;
        }
        sb.Append("}]");
        string outputLabels = sb.ToString();

        // write labels to output file
        try
        {
            File.WriteAllText(outputFile, outputLabels);
        }
        catch (UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"ERROR: Access denied writing to '{outputFile}'.");
            return 1;
        }
        catch (DirectoryNotFoundException)
        {
            Console.Error.WriteLine($"ERROR: Directory not found for '{outputFile}'.");
            return 1;
        }
        catch (PathTooLongException)
        {
            Console.Error.WriteLine($"ERROR: Path is too long: '{outputFile}'.");
            return 1;
        }
        catch (IOException)
        {
            Console.Error.WriteLine($"ERROR: I/O failure writing '{outputFile}'.");
            return 1;
        }
        catch (ArgumentException)
        {
            Console.Error.WriteLine($"ERROR: Invalid output path: '{outputFile}'.");
            return 1;
        }

        // output success message
        Console.WriteLine($"{labels.Count} labels written to '{outputFile}'");
        return 0;
    }
}
