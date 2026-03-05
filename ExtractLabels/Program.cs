// --------------------------------------------------------------
// An Adventure In Time - A Doctor Who fan game for the BBC Micro
// Model B
//
// Copyright (C) 2025  Mark John Leece
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
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: ExtractLabels <inputfile>");
                return;
            }

            var lines = File.ReadAllLines(args[0]);
            var results = new List<(string label, long address)>();

            // regex: label at start of line: .Label123
            var labelRegex = new Regex(@"^(\.[A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled);

            // regex: first address on line: 5BF2
            var addrRegex = new Regex(@"(?:^|\s)([A-Fa-f0-9]{4})(?:$|\s)", RegexOptions.Compiled);

            string labelStr = string.Empty;
            for (int i = 0; i < lines.Length - 1; i++)
            {
                var labelMatch = labelRegex.Match(lines[i]);
                if (labelMatch.Success)
                {
                    labelStr = labelMatch.Groups[1].Value;
                    continue;
                }

                var addressMatch = addrRegex.Match(lines[i]);
                if (addressMatch.Success && labelStr.Length > 0)
                {
                    int address = Convert.ToInt32(addressMatch.Groups[1].Value, 16);
                    results.Add((labelStr, address));
                    labelStr = string.Empty;
                }
            }

            // build output: [{'.label':123L, ... }]
            var sb = new StringBuilder();
            sb.Append("[").Append("{");

            for (int i = 0; i < results.Count; i++)
            {
                var (label, address) = results[i];
                sb.Append($"'{label}':{address}L");
                if (i < results.Count - 1)
                    sb.Append(",");
            }

            sb.Append("}").Append("]");

            Console.WriteLine(sb.ToString());
        }
    }
}
