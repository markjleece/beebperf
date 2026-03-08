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
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: ExtractLabels <inputfile>");
                return;
            }

            string text;
            try
            {
                text = File.ReadAllText(args[0]).Trim([' ', '\t', '\n', '\r']);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return;
            }

            var nameRegex = new Regex(@"(?<=^| )(?<name>\.[A-Za-z_][A-Za-z0-9_.]*)(?=$| )", RegexOptions.Compiled);
            var addressRegex = new Regex(@"(?<=^| )(?<address>[A-Fa-f0-9]{4})(?=$| )", RegexOptions.Compiled);

            var labels = new List<(string Name, ushort Address)>();
            string name = string.Empty;
            foreach (var line in text.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries))
            {
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
