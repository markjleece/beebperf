// --------------------------------------------------------------
// An Adventure In Time - A Doctor Who fan game for the BBC Micro
// Model B
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
using System.Text.RegularExpressions;

namespace ExtractLabels
{
    //
    // Usage: ExtractLabels <input-filename>
    // The output can be piped to an output fileName.
    //
    // Extracts labels from assembler output files or from label
    // files that have the form:
    //  [{'label1':address1L,'label2':address2L,...}]
    //
    internal class Program
    {
        static void Main(string[] args)
        {
            // command line options
            bool showHelp = (args.Length == 0);
            bool stripLineNumbers = false;
            string fileName = string.Empty;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-h" && !showHelp)
                    showHelp = true;
                else if (args[i] == "-s" && !stripLineNumbers)
                    stripLineNumbers = true;
                else if (args[i].StartsWith("-") || fileName != string.Empty)
                {
                    showHelp = true;
                }
                else
                {
                    fileName = args[i];
                }
            }

            if (showHelp || fileName == string.Empty)
            {
                Console.WriteLine("ExtractLabels - Extract labels from assembler output files");
                Console.WriteLine("Usage:");
                Console.WriteLine("  ExtractLabels [options...] [fileName]...");
                Console.WriteLine("Options:");
                Console.WriteLine("  -h  show this help");
                Console.WriteLine("  -s  strip line numbers");
                return;
            }

            // read input file
            string text;
            try
            {
                text = File.ReadAllText(fileName).Trim([' ', '\t', '\n', '\r']);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return;
            }

            // extract labels: look for lines containing a label name and an address
            var nameRegex = new Regex(@"(?<=^| )(?<name>\.[A-Za-z_][A-Za-z0-9_.]*)(?=$| )", RegexOptions.Compiled);
            var addressRegex = new Regex(@"(?<=^| )(?<address>[A-Fa-f0-9]{4})(?=$| )", RegexOptions.Compiled);

            var labels = new List<(string Name, ushort Address)>();
            string name = string.Empty;
            foreach (var line_ in text.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries))
            {
                string line = line_!.Trim();

                if (stripLineNumbers)
                {
                    // strip leading digits (line numbers)
                    int digitCount = 0;
                    while (digitCount < line.Length && line[digitCount] >= '0' && line[digitCount] <= '9')
                        digitCount++;
                    line = line.Substring(digitCount);
                }

                if (line.Length > 6)
                    line = line.Substring(6);

                var nameMatch = nameRegex.Match(line);
                if (nameMatch.Success)
                    name = nameMatch.Groups["name"].Value;

                var addressMatch = addressRegex.Match(line);
                if (addressMatch.Success && name.Length > 0)
                {
                    ushort address = (ushort)Convert.ToInt32(addressMatch.Groups["address"].Value, 16);
                    labels.Add((name, address));
                    name = string.Empty;
                }
            }

            // build output: [{'.label':123L, ... }]
            var sb = new StringBuilder();
            sb.Append("[").Append("{");

            foreach (var label in labels)
            {
                if (sb.Length > 0) sb.Append(",");
                sb.Append($"'{label.Name}':{label.Address}L");
            }

            sb.Append("}").Append("]");

            Console.WriteLine(sb.ToString());
        }
    }
}
